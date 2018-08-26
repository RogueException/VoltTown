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
            [ModelProperty("admin_user_id")]
            public ulong AdminUserId { get; set; }
            [ModelProperty("guild_id")]
            public ulong GuildId { get; set; }
            [ModelProperty("resident_role_id")]
            public ulong ResidentRoleId { get; set; }
            [ModelProperty("ignored_channel_ids")]
            public ulong[] IgnoredChannelIds { get; set; }
        }

        public static Config Read(string path)
        {
            //TODO:  Use Microsoft's Configuration?
            var span = File.ReadAllBytes("./config.json").AsSpan();
            return new JsonSerializer().Read<Config>(span);
        }
    }
}
