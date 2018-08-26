using System;
using Voltaic.Logging;
using Wumpus.Bot;

namespace VoltTown.Components
{
    public class ConsoleManager
    {
        private readonly object _consoleLock = new object();
        private readonly ILogger _logger;

        public ConsoleManager(LogManager log, JobManager scheduleManager)
        {
            _logger = log.CreateLogger("Console");
            log.Output += msg => Write(msg);

            scheduleManager.ScheduleInMemory("Console", "UpdateStatus", () => UpdateStatus(), TimeSpan.FromMinutes(1));
            UpdateStatus();
        }

        private void Write(LogMessage msg)
        {
            lock (_consoleLock)
            {
                if (msg.Severity == LogSeverity.Critical)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine();
                    Console.WriteLine(msg.ToString());
                    Console.ResetColor();
                    Console.ReadLine();
                }
                else
                {
                    switch (msg.Severity)
                    {
                        case LogSeverity.Error:
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;
                        case LogSeverity.Warning:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;
                        case LogSeverity.Info:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case LogSeverity.Verbose:
                            Console.ForegroundColor = ConsoleColor.Gray;
                            break;
                        case LogSeverity.Debug:
                            Console.ForegroundColor = ConsoleColor.DarkGray;
                            break;
                    }
                    Console.WriteLine(msg.ToString());
                    Console.ResetColor();
                }
            }
        }

        public void UpdateStatus()
        {
            long bytes = GC.GetTotalMemory(false);
            Console.Title = $"VoltTown (Wumpus.Net v{WumpusBotClient.Version}) - {bytes} bytes";
        }
    }
}
