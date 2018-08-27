using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Voltaic;
using Voltaic.Logging;
using VoltTown.Data;
using Wumpus;
using Wumpus.Entities;
using Wumpus.Requests;

namespace VoltTown.Components
{
    public class DiscordChannelSync : Component
    {
        private readonly ILogger _logger;
        private readonly Config _config;
        private readonly GameDbContext _db;
        private readonly DiscordConnection _discord;

        public DiscordChannelSync(LogManager log, Config config, GameDbContext db, DiscordConnection discord)
        {
            _logger = log.CreateLogger("DiscordChannelSync");

            _config = config;
            _db = db;
            _discord = discord;

            discord.Gateway.GuildCreate += g =>
            {
                if (g.Id != _config.Discord.GuildId)
                    return;
                if (g.Channels.IsSpecified)
                {
                    Sync(g.Channels.Value);
                    _db.SaveChanges();
                }
            };
        }
        
        public void Sync(IEnumerable<Channel> channels)
        {
            // DB state wins over Discord
            var remainingChannels = channels.Where(x => !_config.Discord.IgnoredChannelIds.Contains(x.Id)).ToList();
            var areas = _db.Areas.ToList();
            var plots = _db.Plots.Include(x => x.Area).ToList();

            // Create/update category channels
            for (int i = 0; i < areas.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < remainingChannels.Count; j++)
                {
                    if (areas[i].DiscordCategoryId == remainingChannels[j].Id)
                    {
                        UpdateArea(areas[i], remainingChannels[j]);
                        remainingChannels.RemoveAt(j);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    CreateArea(areas[i]);
            }

            // Create/update text channels
            for (int i = 0; i < plots.Count; i++)
            {
                bool found = false;
                var parentId = areas.Where(x => x.AreaId == plots[i].AreaId).FirstOrDefault()?.DiscordCategoryId;
                for (int j = 0; j < remainingChannels.Count; j++)
                {
                    if (plots[i].DiscordChannelId == remainingChannels[j].Id)
                    {
                        UpdatePlot(plots[i], remainingChannels[j], parentId);
                        remainingChannels.RemoveAt(j);
                        found = true;
                        break;
                    }
                }
                if (!found)
                    CreatePlot(plots[i], parentId);
            }

            foreach (var channel in remainingChannels)
            {
                // Channel's area or plot doesn't exist, delete it
                _discord.Rest.DeleteChannelAsync(channel.Id);
            }
        }

        public void CreateArea(Data.Game.Area area)
        {
            var name = CleanCategoryName(area.Name);
            var channel = _discord.Rest.CreateGuildChannelAsync(_config.Discord.GuildId, new CreateGuildChannelParams(name, ChannelType.Category)
            {
                PermissionOverwrites = GetPermissionOverwrites()
            }).GetAwaiter().GetResult();
            area.DiscordCategoryId = channel.Id;
        }
        private void UpdateArea(Data.Game.Area area, Channel channel)
        {
            var name = CleanCategoryName(area.Name);
            _discord.Rest.ReplaceChannelAsync(channel.Id, new ModifyChannelParams
            {
                Name = name,
                PermissionOverwrites = GetPermissionOverwrites()
            }).GetAwaiter().GetResult();
        }

        public void CreatePlot(Data.Game.Plot plot, ulong? parentId)
        {
            var name = new Utf8String($"{plot.Address.ToString().PadLeft(3, '0')}-{CleanName(plot.Name)}");
            var channel = _discord.Rest.CreateGuildChannelAsync(_config.Discord.GuildId, new CreateGuildChannelParams(name, ChannelType.Text)
            {
                ParentId = parentId.HasValue ? new Snowflake(parentId.Value) : (Snowflake?)null,
                PermissionOverwrites = GetPermissionOverwrites()
            }).GetAwaiter().GetResult();
            plot.DiscordChannelId = channel.Id;
        }
        private void UpdatePlot(Data.Game.Plot plot, Channel channel, ulong? parentId)
        {
            var name = new Utf8String($"{plot.Address.ToString().PadLeft(3, '0')}-{CleanName(plot.Name)}");
            _discord.Rest.ReplaceChannelAsync(channel.Id, new ModifyChannelParams
            {
                Name = name,
                ParentId = parentId.HasValue ? new Snowflake(parentId.Value) : (Snowflake?)null,
                PermissionOverwrites = GetPermissionOverwrites()
            }).GetAwaiter().GetResult();
        }

        private Overwrite[] GetPermissionOverwrites()
        {
            return new Overwrite[]
            {
                new Overwrite { TargetType = PermissionTarget.Role, TargetId = _config.Discord.GuildId, Deny = ChannelPermissions.ViewChannel },
                new Overwrite { TargetType = PermissionTarget.User, TargetId = _discord.BotUserId, Allow = ChannelPermissions.ViewChannel },
                new Overwrite { TargetType = PermissionTarget.Role, TargetId = _config.Entrance.RoleId, Allow = ChannelPermissions.ViewChannel }
            };
        }

        public Utf8String CleanCategoryName(string name)
        {
            name = name.ToLowerInvariant();

            var memory = new ResizableMemory<byte>(name.Length * 2);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c >= 32 && c <= 126)
                    memory.Push((byte)c);
            }
            if (memory.Length != 0)
                return new Utf8String(memory.AsSpan());
            return new Utf8String("Bad Name");
        }
        public Utf8String CleanName(string name)
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
                else if (c >= 32 && c <= 126 && memory.Length != 0)
                    memory.Push((byte)'-');
            }
            if (memory.Length != 0)
                return new Utf8String(memory.AsSpan());
            return new Utf8String("bad-name");
        }
    }
}
