using System;
using System.Diagnostics;

namespace Fintech.Infrastructure.Logging
{
    public static class LoggingHelper
    {
        public static Stopwatch StartTimer()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            return stopwatch;
        }

        public static long StopTimer(Stopwatch stopwatch)
        {
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds;
        }
    }
}
