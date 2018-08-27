using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using Voltaic;
using Wumpus;
using Wumpus.Entities;
using Wumpus.Requests;

namespace VoltTown.Discord
{
    public partial class DiscordService
    {
        public void Sync(IEnumerable<Channel> channels)
        {
            // DB state wins over Discord
            var remainingChannels = channels.Where(x => !_config.Discord.IgnoredChannelIds.Contains(x.Id)).ToList();
            var areas = _db.Areas.ToList();
            var plots = _db.Plots.Include(x => x.Area).ToList();

            // Create/update category channels
            foreach (var area in areas)
            {
                bool found = remainingChannels.RemoveAll(x => x.Id == area.DiscordCategoryId) > 0;
                if (!found)
                    area.DiscordCategoryId = null;
                AddOrUpdateAreaCategory(area);
            }

            // Create/update text channels
            foreach (var plot in plots)
            {
                bool found = remainingChannels.RemoveAll(x => x.Id == plot.DiscordChannelId) > 0;
                if (!found)
                    plot.DiscordChannelId = null;
                AddOrUpdatePlotChannel(plot);
            }

            // Remove any remaining channels (aren't tied to area or plot)
            foreach (var channel in remainingChannels)
                Rest.DeleteChannelAsync(channel.Id);
        }

        private void AddOrUpdateAreaCategory(Data.Game.Area area)
        {
            TaskUtils.Execute(async () =>
            {
                var name = CleanCategoryName(area.Name);
                var overwrites = GetPermissionOverwrites();
                if (area.DiscordCategoryId.HasValue)
                {
                    await Rest.ReplaceChannelAsync(area.DiscordCategoryId.Value, new ModifyChannelParams
                    {
                        Name = name,
                        PermissionOverwrites = overwrites
                    });
                    _logger.Verbose($"Updated category for area {area.AreaId}");
                }
                else
                {
                    var category = await Rest.CreateGuildChannelAsync(_config.Discord.GuildId, new CreateGuildChannelParams(name, ChannelType.Category)
                    {
                        PermissionOverwrites = GetPermissionOverwrites()
                    });
                    area.DiscordCategoryId = category.Id;
                    _logger.Verbose($"Created category for area {area.AreaId}");
                }
            });
        }
        
        private void AddOrUpdatePlotChannel(Data.Game.Plot plot)
        {
            TaskUtils.Execute(async () =>
            {
                var name = new Utf8String($"{plot.Address.ToString().PadLeft(3, '0')}-{CleanName(plot.Name)}");
                var parentId = plot.Area.DiscordCategoryId.HasValue ? new Snowflake(plot.Area.DiscordCategoryId.Value) : (Snowflake?)null;
                var overwrites = GetPermissionOverwrites();
                if (plot.DiscordChannelId.HasValue)
                {
                    await Rest.ReplaceChannelAsync(plot.DiscordChannelId.Value, new ModifyChannelParams
                    {
                        Name = name,
                        ParentId = parentId,
                        PermissionOverwrites = overwrites
                    });
                    _logger.Verbose($"Updated channel for area {plot.PlotId}");
                }
                else
                {
                    var channel = await Rest.CreateGuildChannelAsync(_config.Discord.GuildId, new CreateGuildChannelParams(name, ChannelType.Text)
                    {
                        ParentId = parentId,
                        PermissionOverwrites = overwrites
                    });
                    plot.DiscordChannelId = channel.Id;
                    _logger.Verbose($"Created channel for area {plot.PlotId}");
                }
            });
        }

        private Overwrite[] GetPermissionOverwrites()
        {
            return new Overwrite[]
            {
                new Overwrite { TargetType = PermissionTarget.Role, TargetId = _config.Discord.GuildId, Deny = ChannelPermissions.ViewChannel },
                new Overwrite { TargetType = PermissionTarget.User, TargetId = BotUserId, Allow = ChannelPermissions.ViewChannel },
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
