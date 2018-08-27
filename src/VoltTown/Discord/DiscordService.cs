using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Voltaic;
using Voltaic.Logging;
using VoltTown.Data;
using VoltTown.Game;
using Wumpus;
using Wumpus.Bot;
using Wumpus.Entities;
using Wumpus.Requests;

namespace VoltTown.Discord
{
    public partial class DiscordService
    {
        private readonly ILogger _logger;
        private readonly WumpusBotClient _client;
        private readonly Config _config;
        private readonly GameDbContext _db;

        public Snowflake BotUserId { get; set; }
        public WumpusRestClient Rest => _client.Rest;
        public WumpusGatewayClient Gateway => _client.Gateway;

        public DiscordService(LogManager log, Config config, GameDbContext db, GameService game)
        {
            _logger = log.CreateLogger("Discord");

            _config = config;
            _db = db;

            _client = new WumpusBotClient(logManager: log)
            {
                Authorization = new AuthenticationHeaderValue("Bot", config.Discord.Token)
            };
            Gateway.Ready += r =>
            {
                BotUserId = r.User.Id;
            };
            Gateway.GuildCreate += g =>
            {
                if (g.Id != config.Discord.GuildId || g.Unavailable != false)
                    return;
                if (g.Channels.IsSpecified)
                {
                    TaskUtils.Execute(async () =>
                    {
                        Sync(g.Channels.Value);
                        await db.SaveChangesAsync();
                    });
                }
            };

            game.AreaAdded += a => AddOrUpdateAreaCategory(a);
            game.AreaUpdated += a => AddOrUpdateAreaCategory(a);
            game.PlotAdded += p => AddOrUpdatePlotChannel(p);
            game.PlotUpdated += p => AddOrUpdatePlotChannel(p);
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
    }
}
