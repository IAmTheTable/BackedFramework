using BackedFramework.Resources.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Server.Events
{
    
    internal class ServerEvents
    {
        internal static async Task InvokeClientConnect(TcpClient client) => await ClientConnect.Invoke(client);
        internal static async Task InvokeClientRequest(TcpClient client, HTTPParser parser) => await ClientRequest.Invoke(client, parser);
        /// <summary>
        /// Called when a client attempts to connect to the server.
        /// </summary>
        internal static event Func<TcpClient, Task> ClientConnect;

        /// <summary>
        /// Called when a valid request is made to the server from a client.
        /// </summary>
        internal static event Func<TcpClient, HTTPParser, Task> ClientRequest;
    }
}
