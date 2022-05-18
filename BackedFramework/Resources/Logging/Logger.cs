using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Logging
{
    /// <summary>
    /// A basic logging utility class.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// An event that is raised when a message is logged.
        /// </summary>
        public static Action<LogLevel, string> OnLog;
        private static readonly Dictionary<LogLevel, string[]> _logs = new();
        /// <summary>
        /// The level of the log from the most severe to nothing.
        /// </summary>
        public enum LogLevel
        {
            /// <summary>
            /// Do NOT use, this is meant for internal use only.
            /// </summary>
            None,
            /// <summary>
            /// Show debug messages. (Extremely spammy)
            /// </summary>
            Debug,
            /// <summary>
            /// Show informational messages. (Spammy)
            /// </summary>
            Info,
            /// <summary>
            /// Show warning messages.
            /// </summary>
            Warning,
            /// <summary>
            /// Show error messages.
            /// </summary>
            Error,
            /// <summary>
            /// Show fatal messages.
            /// Used for near application crash messages...
            /// </summary>
            Fatal
        }
        /// <summary>
        /// Logs a message to the event log.
        /// </summary>
        /// <param name="level">The severity of the log.</param>
        /// <param name="args">The content of the log itself.</param>
        internal static void LogInt(LogLevel level, params string[] args)
        {
            OnLog.Invoke(level, string.Join(" ", args));

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
        /// Logs a message to the console.
        /// </summary>
        /// <param name="level">The severity of the log.</param>
        /// <param name="args">The content of the log itself.</param>
        public static void Log(LogLevel level, params string[] args)
        {
            Console.WriteLine($"[{Enum.GetName(level)}] [{DateTime.Now:G}] {string.Join(" ", args)}");
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
