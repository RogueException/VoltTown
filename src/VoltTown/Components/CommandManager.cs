using McMaster.Extensions.CommandLineUtils;
using System;
using Voltaic;
using Voltaic.Logging;
using Wumpus.Requests;

namespace VoltTown.Components
{
    public class CommandManager : Component
    {
        private readonly ILogger _logger;
        private readonly CommandLineApplication _adminApp, _userApp;

        private readonly Utf8String _okHandUtf8 = new Utf8String("👌");
        private readonly Utf8String _noEntryUtf8 = new Utf8String("⛔");

        public CommandManager(LogManager log, Config config, DiscordConnection discord)
        {
            _logger = log.CreateLogger("Commands");

            _adminApp = new CommandLineApplication(false);
            _userApp = new CommandLineApplication(false);

            discord.Gateway.MessageCreate += m =>
            {
                if (m.Content is null || m.GuildId != config.Discord.GuildId || m.Content.Bytes.Length == 0)
                    return;
                var bytes = m.Content.Bytes;
                if (bytes[0] != '!')
                    return;
                bytes = bytes.Slice(1);

                try
                {
                    bool success;
                    if (m.Author.Id == config.Permissions.AdminUserId)
                        success = AdminExecute(m.Author.Id, new Utf8String(bytes).ToString() ?? "");
                    else
                        success = Execute(m.Author.Id, new Utf8String(bytes).ToString() ?? "");

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

        public bool AdminExecute(ulong userId, string input)
        {
            var args = input.Split(' ');

            ClearState(_adminApp);
            if (_adminApp.Execute(args) == 0)
                return true;

            ClearState(_userApp);
            return _userApp.Execute(args) == 0;
        }
        public bool Execute(ulong userId, string input)
        {
            var args = input.Split(' ');

            ClearState(_userApp);
            return _userApp.Execute(args) == 0;
        }

        private void ClearState(CommandLineApplication app)
        {
            // CommandLineApplication wasn't designed to be called multiple times, so we need to clear state ourselves
            foreach (var argument in app.Arguments)
                argument.Values.Clear();
            foreach (var cmd in app.Commands)
                ClearState(cmd);
        }

        public void AdminCommand(string name, Action<CommandLineApplication> configuration)
        {
            _adminApp.Command(name, configuration);
        }
        public void Command(string name, Action<CommandLineApplication> configuration)
        {
            _userApp.Command(name, configuration);
        }
    }
}
