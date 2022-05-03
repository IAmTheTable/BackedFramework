using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Logging
{
    internal static class Logger
    {
        private static readonly Dictionary<LogLevel, string[]> _logs = new();
        internal enum LogLevel
        {
            None,
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }
        internal static void Log(LogLevel level, params string[] args)
        {
            Console.WriteLine($"[{Enum.GetName(level)}] [{DateTime.Now:G}] {string.Join(" ", args)}");

            // if the log level already exists, add the new args to the existing list
            if (_logs.ContainsKey(level))
            {
                _logs[level] = _logs[level].Concat(args).ToArray();
            }
            else
            {
                _logs.Add(level, new[] { $"[{Enum.GetName(level)}] [{DateTime.Now:G}] {string.Join(" ", args)}" });
            }
        }

        /// <summary>
        /// Return a list of all logs from a specified log level.
        /// </summary>
        /// <param name="type">The log level you wish to take from.</param>
        /// <returns>A list of logs</returns>
        internal static List<string> DumpLogs(LogLevel type = LogLevel.None)
        {
            var logs = type == LogLevel.None ? _logs.Values.ToList() : _logs.Where(x => x.Key == type).Select(x => x.Value);

            List<string> logLists = new();

            for (int i = 0; i < logs.Count(); i++)
                logLists.AddRange(logs.ElementAt(i));
            
            return logLists;
        }
    }
}
