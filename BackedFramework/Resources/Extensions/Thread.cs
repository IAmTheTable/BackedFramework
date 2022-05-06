namespace BackedFramework.Resources.Extensions
{
    /// <summary>
    /// A utility class to help manage threaded actions...
    /// </summary>
    internal class Thread
    {
        /// <summary>
        /// The tassk instance that will run.
        /// </summary>
        private Task _currentTask;
        internal Thread(Action action)
        {
            _currentTask = new Task(action);
        }
        /// <summary>
        /// Execute the task.
        /// </summary>
        internal void Start()
        {
            _currentTask.Start();
            var result = Task.WhenAny(_currentTask);
            
            if(result.IsCompletedSuccessfully)
                Logging.Logger.Log(Logging.Logger.LogLevel.Debug, "Thread finished successfully.");
        }
        /// <summary>
        /// Make the current thread sleep for the specified amount of time.
        /// </summary>
        /// <param name="ms">The amount of time in ms to sleep.</param>
        internal static void Sleep(int ms) => Task.Delay(ms);
    }
}
