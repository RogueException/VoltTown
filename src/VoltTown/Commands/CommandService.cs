using McMaster.Extensions.CommandLineUtils;
using System;
using Voltaic;
using Voltaic.Logging;
using VoltTown.Discord;
using VoltTown.Game;
using Wumpus.Requests;

namespace VoltTown.Commands
{
    public partial class CommandService
    {
        private readonly ILogger _logger;
        private readonly CommandLineApplication _adminApp, _userApp;
        private readonly GameService _game;

        private readonly Utf8String _okHandUtf8 = new Utf8String("👌");
        private readonly Utf8String _noEntryUtf8 = new Utf8String("⛔");

        public CommandService(LogManager log, Config config, DiscordService discord, GameService game)
        {
            _logger = log.CreateLogger("Commands");

            _game = game;

            _adminApp = new CommandLineApplication(false);
            _userApp = new CommandLineApplication(false);

            AddCreateCommands();

            discord.Gateway.MessageCreate += m =>
            {
                if (m.Content is null || m.GuildId != config.Discord.GuildId || m.Content.Bytes.Length < 2)
                    return;
                var bytes = m.Content.Bytes;
                if (bytes[0] != '!')
                    return;
                bytes = bytes.Slice(1);

                try
                {
                    bool success = false;
                    var args = new Utf8String(bytes).ToString().Split(' ');
                    if (m.Author.Id == config.Permissions.AdminUserId)
                    {
                        ClearState(_adminApp);
                        success = _adminApp.Execute(args) == 0;
                    }
                    if (!success)
                    {
                        ClearState(_userApp);
                        success = _userApp.Execute(args) == 0;
                    }

                    if (success)
                        discord.Rest.CreateReactionAsync(m.ChannelId, m.Id, _okHandUtf8);
                    else
                        discord.Rest.CreateReactionAsync(m.ChannelId, m.Id, _noEntryUtf8);
                }
                catch (Exception ex)
                {
                    discord.Rest.CreateReactionAsync(m.ChannelId, m.Id, _noEntryUtf8);
                    discord.Rest.CreateMessageAsync(m.ChannelId, new CreateMessageParams
                    {
                        Content = new Utf8String($"Error: {ex.Message}")
                    });
                }
            };
        }

        private void ClearState(CommandLineApplication app)
        {
            // CommandLineApplication wasn't designed to be called multiple times, so we need to clear state ourselves
            foreach (var argument in app.Arguments)
                argument.Values.Clear();
            foreach (var cmd in app.Commands)
                ClearState(cmd);
        }

        private void AddCommands()
        {
            AddCreateCommands();
        }
    }
}
