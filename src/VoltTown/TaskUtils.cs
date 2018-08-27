using System;
using System.Threading.Tasks;

namespace VoltTown
{
    public static class TaskUtils
    {
        public static Task RunScheduled(Action action, TimeSpan timeSpan)
        {
            return Task.Run(async () =>
            {
                long iterationTicks = (int)timeSpan.TotalMilliseconds;
                long nextTick = Environment.TickCount;
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
            });
        }
        public static void Execute(Func<Task> asyncAction)
            => asyncAction().GetAwaiter().GetResult();
    }
}
