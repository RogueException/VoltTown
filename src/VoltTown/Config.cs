using System;
using System.IO;
using Voltaic.Serialization;
using Voltaic.Serialization.Json;

namespace VoltTown
{
    public class Config
    {
        [ModelProperty("discord")]
        public DiscordConfig Discord { get; set; }
        public class DiscordConfig
        {
            [ModelProperty("token")]
            public string Token { get; set; }
            [ModelProperty("guild_id")]
            public ulong GuildId { get; set; }
            [ModelProperty("ignored_channel_ids")]
            public ulong[] IgnoredChannelIds { get; set; }
        }

        [ModelProperty("permissions")]
        public PermissionsConfig Permissions { get; set; }
        public class PermissionsConfig
        {
            [ModelProperty("admin_user_id")]
            public ulong AdminUserId { get; set; }
        }

        [ModelProperty("entrance")]
        public EntranceConfig Entrance { get; set; }
        public class EntranceConfig
        {
            [ModelProperty("role_id")]
            public ulong RoleId { get; set; }
            [ModelProperty("message_id")]
            public ulong MessageId { get; set; }
            [ModelProperty("reaction")]
            public string Reaction { get; set; }
        }

        public static Config Read(string path)
        {
            //TODO:  Use Microsoft's Configuration?
            var span = File.ReadAllBytes("./config.json").AsSpan();
            return new JsonSerializer().Read<Config>(span);
        }
    }
}
