using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Server.Events
{
    internal static class ServerEvents
    {
        /// <summary>
        /// Called when a client attempts to connect to the server.
        /// </summary>
        public static event Func<TcpClient, Task> ClientConnect;

        /// <summary>
        /// Called when a valid request is made to the server from a client.
        /// </summary>
        public static event Func<Task> ClientRequest;
    }
}
