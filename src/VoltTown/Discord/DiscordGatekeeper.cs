using Microsoft.EntityFrameworkCore;
using Voltaic.Logging;
using VoltTown.Data;

namespace VoltTown.Discord
{
    public class DiscordGatekeeper
    {
        private readonly ILogger _logger;

        public DiscordGatekeeper(LogManager log, Config config, GameDbContext db, DiscordService discord)
        {
            _logger = log.CreateLogger("Gatekeeper");
            discord.Gateway.MessageReactionAdd += mr =>
            {
                if (mr.UserId == discord.BotUserId)
                    return;
                if (mr.MessageId == config.Entrance.MessageId && mr.Emoji.Name == config.Entrance.Reaction)
                {
                    TaskUtils.Execute(async () =>
                    {
                        var character = await db.Characters.SingleOrDefaultAsync(x => x.DiscordUserId == mr.UserId);
                        if (character == null)
                        {
                            db.Characters.Add(new Data.Game.Character { DiscordUserId = mr.UserId });
                            await db.SaveChangesAsync();
                            await discord.Rest.AddGuildMemberRoleAsync(config.Discord.GuildId, mr.UserId, config.Entrance.RoleId);
                            _logger.Info($"User {mr.UserId} created a character");
                        }
                    });
                }
            };
        }
    }
}
