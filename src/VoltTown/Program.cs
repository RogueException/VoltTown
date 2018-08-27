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
            var scheduler = new Scheduler(log);
            var console = new ConsoleLogger(log, scheduler);

            var discord = new DiscordConnection(log, config);
            var channels = new DiscordChannelSync(log, config, db, discord);
            var members = new DiscordMemberSync(log, config, db, discord);

            var commands = new CommandManager(log, config, discord);
            var game = new GameManager(log, db, channels, members, commands);

            var task = await Task.WhenAny(discord.Run());
            await task.ConfigureAwait(false); // Throw if exception
        }
    }
}
