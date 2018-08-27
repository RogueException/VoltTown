using System;
using System.Threading.Tasks;
using Voltaic.Logging;
using VoltTown.Commands;
using VoltTown.Data;
using VoltTown.Discord;
using VoltTown.Game;
using VoltTown.Logging;

namespace VoltTown
{
    public class Program
    {
        static async Task Main(string[] args) => await new Program().RunAsync();
        
        public async Task RunAsync()
        {
            var config = Config.Read("./config.json");
            var db = GameDbContext.Load();

            var log = new LogManager(LogSeverity.Debug);
            var console = new ConsoleLogger(log);

            var game = new GameService(log, db);

            var discord = new DiscordService(log, config, db, game);
            var gatekeeper = new DiscordGatekeeper(log, config, db, discord);

            var commands = new CommandService(log, config, discord, game);

            var task = await Task.WhenAny(discord.Run());
            await task.ConfigureAwait(false); // Throw if exception
            Console.ReadLine();
        }
    }
}
