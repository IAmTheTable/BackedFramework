using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Statistics
{
    /// <summary>
    /// A utility class to help measure program execution timing.
    /// </summary>
    internal class StatisticsManager : IDisposable
    {
        public DateTime startTime;
        public DateTime endTime;

        public void Start() => startTime = DateTime.Now;
        public void End() => endTime = DateTime.Now;
        public void PrintTiming() => Console.WriteLine($"[Statistics] Timing Took: {((endTime - startTime).TotalMilliseconds < 1 ? (((endTime - startTime).TotalMilliseconds * 1000).ToString() + "µs") : ((endTime - startTime).TotalMilliseconds).ToString() + "ms")}");

        public void Dispose()
        {
            if (endTime != DateTime.MinValue)
                PrintTiming();

            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}
