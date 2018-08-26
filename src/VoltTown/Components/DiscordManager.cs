using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Voltaic;
using Voltaic.Logging;
using VoltTown.Data;
using Wumpus;
using Wumpus.Bot;
using Wumpus.Entities;
using Wumpus.Requests;

namespace VoltTown.Components
{
    public class DiscordManager
    {
        private readonly ILogger _logger;
        private readonly GameDbContext _db;
        private readonly WumpusBotClient _client;
        private readonly CommandManager _commands;

        private readonly Snowflake _adminUserId, _guildId, _residentRoleId;
        private readonly IReadOnlyList<Snowflake> _ignoredChannelIds;

        private readonly Utf8String _okHandUtf8 = new Utf8String("👌");
        private readonly Utf8String _noEntryUtf8 = new Utf8String("⛔");

        public DiscordManager(LogManager log, Config config, GameDbContext db, CommandManager commands)
        {
            _logger = log.CreateLogger("Discord");

            _db = db;
            _commands = commands;

            _adminUserId = config.Discord.AdminUserId;
            _guildId = config.Discord.GuildId;
            _residentRoleId = config.Discord.ResidentRoleId;
            _ignoredChannelIds = config.Discord.IgnoredChannelIds.Select(x => new Snowflake(x)).ToArray();

            _client = new WumpusBotClient(logManager: log)
            {
                Authorization = new AuthenticationHeaderValue("Bot", config.Discord.Token)
            };
            _client.Gateway.GuildCreate += g =>
            {
                if (g.Id != _guildId)
                    return;
                if (g.Members.IsSpecified)
                {
                    SyncMembers(g.Members.Value);
                    _db.SaveChanges();
                }
                if (g.Channels.IsSpecified)
                {
                    SyncChannels(g.Channels.Value);
                    _db.SaveChanges();
                }
            };
            _client.Gateway.GuildMemberAdd += gm =>
            {
                SyncUser(gm);
                _db.SaveChanges();
            };
            _client.Gateway.MessageCreate += m =>
            {
                if (m.Content is null || m.GuildId != _guildId)
                    return;
                var bytes = m.Content.Bytes;
                if (bytes[0] != '!')
                    return;
                bytes = bytes.Slice(1);

                try
                {
                    bool success;
                    if (m.Author.Id == _adminUserId)
                        success = _commands.AdminExecute(m.Author.Id, new Utf8String(bytes).ToString() ?? "");
                    else
                        success = _commands.Execute(m.Author.Id, new Utf8String(bytes).ToString() ?? "");

                    if (success)
                        _client.Rest.CreateReactionAsync(m.ChannelId, m.Id, _okHandUtf8);
                    else
                        _client.Rest.CreateReactionAsync(m.ChannelId, m.Id, _noEntryUtf8);
                }
                catch (Exception ex)
                {
                    _client.Rest.CreateReactionAsync(m.ChannelId, m.Id, _noEntryUtf8);
                    _client.Rest.CreateMessageAsync(m.ChannelId, new CreateMessageParams
                    {
                        Content = new Utf8String($"Error: {ex.Message}")
                    });
                }
            };
        }

        public async Task Run()
        {
            try
            {
                _logger.Debug($"Starting");
                var presence = new UpdateStatusParams
                {
                    Status = UserStatus.Online,
                    Game = new Activity
                    {
                        Name = (Utf8String)$"Wumpus.Net v{WumpusBotClient.Version}",
                        Type = ActivityType.Game
                    }
                };
                await _client.RunAsync(initialPresence: presence);
            }
            catch (Exception ex)
            {
                _logger.Critical($"An unrecoverable error has occured", ex);
            }
            Console.ReadLine();
        }

        public void SyncUser(GuildMember member)
        {
            // Discord state wins over DB
            var dbUser = new Data.Discord.User { UserId = member.User.Id };
            if (member.User.Bot == true)
                _logger.Debug($"Skipped user (is bot): {member.User.Username}#{member.User.Discriminator}");
            else if (!_db.Users.AddIfNotExists(dbUser, x => x.UserId == member.User.Id))
                _logger.Debug($"Skipped user (already exists): {member.User.Username}#{member.User.Discriminator}");
            else
                _logger.Verbose($"Added new user: {member.User.Username}#{member.User.Discriminator}");
        }
        public void SyncMembers(IEnumerable<GuildMember> members)
        {
            foreach (var member in members)
                SyncUser(member);
        }

        public void SyncChannels(IEnumerable<Channel> channels)
        {
            // DB state wins over Discord
            var remainingChannels = channels.Where(x => !_ignoredChannelIds.Contains(x.Id)).ToList();
            var areas = _db.Areas.ToList();
            var plots = _db.Plots.Include(x => x.Area).ToList();

            for (int i = 0; i < areas.Count; i++)
            {
                bool found = false;
                var name = CleanChannelName(areas[i].Name);
                for (int j = 0; j < remainingChannels.Count; j++)
                {
                    if (areas[i].DiscordCategoryId == remainingChannels[j].Id)
                    {
                        UpdateAreaChannel(areas[i], remainingChannels[j]);
                        remainingChannels.RemoveAt(j);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    CreateAreaChannel(areas[i]);
            }

            for (int i = 0; i < plots.Count; i++)
            {
                bool found = false;
                var parentId = areas.Where(x => x.AreaId == plots[i].AreaId).FirstOrDefault()?.DiscordCategoryId;
                for (int j = 0; j < remainingChannels.Count; j++)
                {
                    if (plots[i].DiscordChannelId == remainingChannels[j].Id)
                    {
                        UpdatePlotChannel(plots[i], remainingChannels[j], parentId);
                        remainingChannels.RemoveAt(j);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    CreatePlotChannel(plots[i], parentId);
            }

            foreach (var channel in remainingChannels)
            {
                // Channel's area or plot doesn't exist, delete it
                _client.Rest.DeleteChannelAsync(channel.Id);
            }
        }

        public void CreateAreaChannel(Data.Game.Area area)
        {
            var name = CleanChannelName(area.Name);
            var channel = _client.Rest.CreateGuildChannelAsync(_guildId, new CreateGuildChannelParams(name, ChannelType.Category)
            {
                PermissionOverwrites = GetChannelPermissions()
            }).GetAwaiter().GetResult();
            area.DiscordCategoryId = channel.Id;
        }
        private void UpdateAreaChannel(Data.Game.Area area, Channel channel)
        {
            var name = CleanChannelName(area.Name);
            _client.Rest.ReplaceChannelAsync(channel.Id, new ModifyChannelParams
            {
                Name = name,
                PermissionOverwrites = GetChannelPermissions()
            }).GetAwaiter().GetResult();
        }
        
        public void CreatePlotChannel(Data.Game.Plot plot, ulong? parentId)
        {
            var name = new Utf8String($"{plot.Address.ToString().PadLeft(3, '0')}-{CleanChannelName(plot.Name)}");
            var channel = _client.Rest.CreateGuildChannelAsync(_guildId, new CreateGuildChannelParams(name, ChannelType.Text)
            {
                ParentId = parentId.HasValue ? new Snowflake(parentId.Value) : (Snowflake?)null,
                PermissionOverwrites = GetChannelPermissions()
            }).GetAwaiter().GetResult();
            plot.DiscordChannelId = channel.Id;
        }
        private void UpdatePlotChannel(Data.Game.Plot plot, Channel channel, ulong? parentId)
        {
            var name = new Utf8String($"{plot.Address.ToString().PadLeft(3, '0')}-{CleanChannelName(plot.Name)}");
            _client.Rest.ReplaceChannelAsync(channel.Id, new ModifyChannelParams
            {
                Name = name,
                ParentId = parentId.HasValue ? new Snowflake(parentId.Value) : (Snowflake?)null,
                PermissionOverwrites = GetChannelPermissions()
            }).GetAwaiter().GetResult();
        }

        private Utf8String CleanChannelName(string name)
        {
            name = name.ToLowerInvariant();

            var memory = new ResizableMemory<byte>(name.Length * 2);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if ((c >= 48 && c <= 57) || // Digits
                    (c >= 97 && c <= 122) || // Lowercase
                    c == 45) // Dash
                    memory.Push((byte)name[i]);
            }
            return new Utf8String(memory.AsSpan());
        }
        private Overwrite[] GetChannelPermissions()
        {
            return new Overwrite[]
            {
                new Overwrite { TargetType = PermissionTarget.Role, TargetId = _guildId, Deny = ChannelPermissions.ViewChannel },
                new Overwrite { TargetType = PermissionTarget.Role, TargetId = _residentRoleId, Allow = ChannelPermissions.ViewChannel }
            };
        }
    }
}
