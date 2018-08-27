using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Voltaic;
using Voltaic.Logging;
using Wumpus;
using Wumpus.Bot;
using Wumpus.Entities;
using Wumpus.Requests;

namespace VoltTown.Components
{
    public class DiscordConnection : Component
    {
        private readonly ILogger _logger;
        private readonly WumpusBotClient _client;

        public Snowflake BotUserId { get; set; }
        public WumpusRestClient Rest => _client.Rest;
        public WumpusGatewayClient Gateway => _client.Gateway;

        public DiscordConnection(LogManager log, Config config)
        {
            _logger = log.CreateLogger("Discord");

            _client = new WumpusBotClient(logManager: log)
            {
                Authorization = new AuthenticationHeaderValue("Bot", config.Discord.Token)
            };
            Gateway.Ready += r =>
            {
                BotUserId = r.User.Id;
            };
        }

        public override async Task Run()
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
    }
}
