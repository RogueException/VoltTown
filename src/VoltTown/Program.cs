using System.Threading.Tasks;
using Voltaic.Logging;
using VoltTown.Components;
using VoltTown.Data;

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
            var scheduler = new JobManager(log);
            var console = new ConsoleManager(log, scheduler);
            var commands = new CommandManager(log);
            var discord = new DiscordManager(log, config, db, commands);
            var game = new GameManager(log, db, discord, commands);

            var task = await Task.WhenAny(discord.Run());
            await task.ConfigureAwait(false); // Throw if exception
        }
    }
}
