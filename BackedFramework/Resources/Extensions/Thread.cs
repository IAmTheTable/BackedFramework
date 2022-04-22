using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Extensions
{
    /// <summary>
    /// An extended class wrapped around the System.Threading.Thread class to help ease of use with limiting thread counts.
    /// </summary>
    internal class Thread : IDisposable
    {
        
        /// <summary>
        /// A list of threads that need to be executed.
        /// </summary>
        internal static List<Thread> QueuedThreads = new();
        /// <summary>
        /// A list of current active threads.
        /// </summary>
        internal static List<Thread> ActiveThreads = new();
        /// <summary>
        /// The instance of the real thread.
        /// </summary>
        internal System.Threading.Thread _threadInstance;

        public Thread(ThreadStart start)
        {
            _threadInstance = new System.Threading.Thread(start);
            NewThreadRoutine();
        }
        public Thread(ParameterizedThreadStart start)
        {
            _threadInstance = new System.Threading.Thread(start);

            NewThreadRoutine();
        }

        /// <summary>
        /// A routine to manage threads, check if we need to queue them or run them.
        /// </summary>
        private void NewThreadRoutine()
        {
            var serverConfig = Server.BackedServer.Instance.Config;
            
            if (ActiveThreads.Count + 1 < serverConfig.MaxThreads)
                ActiveThreads.Add(this);
            else
                QueuedThreads.Add(this);
        }

        /// <summary>
        /// Start the thread.
        /// </summary>
        public void Start() => _threadInstance.Start();
        
        void IDisposable.Dispose()
        {
            Console.WriteLine("thread is disposed");
        }
    }
}
