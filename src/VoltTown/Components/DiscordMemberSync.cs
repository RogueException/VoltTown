using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Voltaic.Logging;
using VoltTown.Data;
using Wumpus.Entities;

namespace VoltTown.Components
{
    public class DiscordMemberSync : Component
    {
        private readonly ILogger _logger;
        private readonly GameDbContext _db;
        private readonly Config _config;
        private readonly DiscordConnection _discord;

        public DiscordMemberSync(LogManager log, Config config, GameDbContext db, DiscordConnection discord)
        {
            _logger = log.CreateLogger("DiscordMemberSync");

            _db = db;
            _config = config;
            _discord = discord;

            discord.Gateway.GuildCreate += g =>
            {
                if (g.Id != config.Discord.GuildId)
                    return;
                if (g.Members.IsSpecified)
                {
                    Sync(g.Members.Value);
                    db.SaveChanges();
                }
            };
            discord.Gateway.GuildMemberAdd += gm =>
            {
                if (gm.User.Id == discord.BotUserId)
                    return;
                Sync(gm);
                db.SaveChanges();
            };
            discord.Gateway.MessageReactionAdd += mr =>
            {
                if (mr.UserId == discord.BotUserId)
                    return;
                if (mr.MessageId == config.Entrance.MessageId && mr.Emoji.Name == config.Entrance.Reaction)
                {
                    var user = _db.Users.SingleAsync(x => x.UserId == mr.UserId) .GetAwaiter().GetResult();
                    if (user.CharacterId == null)
                    {
                        user.Character = new Data.Game.Character();
                        db.SaveChanges();
                        discord.Rest.AddGuildMemberRoleAsync(config.Discord.GuildId, mr.UserId, config.Entrance.RoleId);
                    }
                }
            };
        }

        public void Sync(GuildMember member)
        {
            var dbUser = new Data.Discord.User { UserId = member.User.Id };
            if (member.User.Bot == true)
            {
                _logger.Debug($"Skipped user (is bot): {member.User.Username}#{member.User.Discriminator}");
                return;
            }
            else if (!_db.Users.AddIfNotExists(dbUser, x => x.UserId == member.User.Id))
                _logger.Debug($"Skipped user (already exists): {member.User.Username}#{member.User.Discriminator}");
            else
                _logger.Verbose($"Added new user: {member.User.Username}#{member.User.Discriminator}");

            var user = _db.Users.SingleAsync(x => x.UserId == member.User.Id).GetAwaiter().GetResult();
            if (user.CharacterId != null)
                _discord.Rest.AddGuildMemberRoleAsync(_config.Discord.GuildId, member.User.Id, _config.Entrance.RoleId);
        }
        public void Sync(IEnumerable<GuildMember> members)
        {
            foreach (var member in members)
                Sync(member);
        }
    }
}
