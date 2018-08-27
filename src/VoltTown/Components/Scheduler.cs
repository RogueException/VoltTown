using Hangfire;
using Hangfire.SQLite;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Voltaic.Logging;

namespace VoltTown.Components
{
    public class Scheduler : Component, IDisposable
    {
        private readonly ILogger _logger;
        private readonly BackgroundJobServer _server;
        private readonly Dictionary<string, Task> _tasks;
        private readonly CancellationTokenSource _cts;

        public Scheduler(LogManager log)
        {
            _logger = log.CreateLogger("Scheduler");

            _cts = new CancellationTokenSource();
            _tasks = new Dictionary<string, Task>();

            GlobalConfiguration.Configuration.UseSQLiteStorage("Data Source=./scheduler.db;");
            _server = new BackgroundJobServer();
        }

        public void Schedule(string componentId, string jobId, Expression<Action> action, TimeSpan timeSpan)
        {
            jobId = $"{componentId}.{jobId}";
            if (timeSpan.Days > 0 && timeSpan.Hours == 0 && timeSpan.Minutes == 0 && timeSpan.Seconds == 0 && timeSpan.Milliseconds == 0)
            {
                RecurringJob.AddOrUpdate(jobId, action, Cron.DayInterval(timeSpan.Days));
                _logger.Info($"Scheduled {jobId} to run every {timeSpan.Days} days");
            }
            else if (timeSpan.Days == 0 && timeSpan.Hours > 0 && timeSpan.Minutes == 0 && timeSpan.Seconds == 0 && timeSpan.Milliseconds == 0)
            {
                RecurringJob.AddOrUpdate(jobId, action, Cron.HourInterval(timeSpan.Hours));
                _logger.Info($"Scheduled {jobId} to run every {timeSpan.Hours} hours");
            }
            else if (timeSpan.Days == 0 && timeSpan.Hours == 0 && timeSpan.Minutes > 0 && timeSpan.Seconds == 0 && timeSpan.Milliseconds == 0)
            {
                RecurringJob.AddOrUpdate(jobId, action, Cron.MinuteInterval(timeSpan.Minutes));
                _logger.Info($"Scheduled {jobId} to run every {timeSpan.Minutes} minutes");
            }
            else
                throw new NotSupportedException("Unsupported TimeSpan value");
        }

        public void ScheduleInMemory(string componentId, string jobId, Action action, TimeSpan timeSpan)
        {
            jobId = $"{componentId}.{jobId}";
            if (timeSpan.Days > 0 && timeSpan.Hours == 0 && timeSpan.Minutes == 0 && timeSpan.Seconds == 0 && timeSpan.Milliseconds == 0)
                _logger.Info($"Scheduled {jobId} to run in-memory every {timeSpan.Days} days");
            else if (timeSpan.Days == 0 && timeSpan.Hours > 0 && timeSpan.Minutes == 0 && timeSpan.Seconds == 0 && timeSpan.Milliseconds == 0)
                _logger.Info($"Scheduled {jobId} to run in-memory every {timeSpan.Hours} hours");
            else if (timeSpan.Days == 0 && timeSpan.Hours == 0 && timeSpan.Minutes > 0 && timeSpan.Seconds == 0 && timeSpan.Milliseconds == 0)
                _logger.Info($"Scheduled {jobId} to run in-memory every {timeSpan.Minutes} minutes");
            else
                throw new NotSupportedException("Unsupported TimeSpan value");

            _tasks[jobId] = Task.Run(async () =>
            {
                long iterationTicks = (int)timeSpan.TotalMilliseconds;
                long nextTick = Environment.TickCount;
                try
                {
                    while (true)
                    {
                        if (Environment.TickCount >= nextTick)
                        {
                            action();
                            nextTick += iterationTicks;
                        }
                        else
                        {
                            long sleepTicks = nextTick - Environment.TickCount;
                            if (sleepTicks >= 0)
                                await Task.Delay(sleepTicks > int.MaxValue ? int.MaxValue : (int)sleepTicks);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error($"Job {jobId} crashed", ex);
                }
            });
        }

        public void Dispose()
        {
            _server.Dispose();
        }
    }
}
