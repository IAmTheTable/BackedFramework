global using System.Net;
global using System.Net.Sockets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BackedFramework.Resources.Exceptions;
using Thread = BackedFramework.Resources.Extensions.Thread;

namespace BackedFramework.Server
{
    public class BackedServer
    {
        /// <summary>
        /// The static instance of the server.
        /// </summary>
        public static BackedServer Instance { get; private set; }

        /// <summary>
        /// Configuration instance of the server.
        /// </summary>
        public BackedConfig Config { get; set; }

        /// <summary>
        /// Default constructor for creating the instance of the server.
        /// </summary>
        /// <param name="config">Configuration for the server</param>
        /// <exception cref="MultiInstanceException">Thrown when trying to create more than one instance of a BackedServer.</exception>
        public BackedServer(BackedConfig config)
        {
            // only allow a single instance of the server to exist
            if(!Instance.Equals(null))
                throw new MultiInstanceException("Only one instance of BackedServer can be created.");
            
            // set our config and instance
            this.Config = config;
            Instance = this;

            // set the default listening port
            this._server = new(IPAddress.Any, this.Config.ListeningPort);

            // subscribe to the server events
            Events.ServerEvents.ClientConnect += OnClientConnect;
            Events.ServerEvents.ClientRequest += OnClientRequest;

            ConfigureServer();
        }

        /// <summary>
        /// Configure the TcpListener as well as start allowing connections to the server.
        /// </summary>
        private void ConfigureServer()
        {
            this._server.Start();

            new Thread(() =>
            {
                while(true)
                {
                    if (!this._server.Pending())
                        continue;

                    var client = this._server.AcceptTcpClient();
                    Events.ServerEvents.ClientConnect.Invoke(client);
                }
            }).Start();
        }

        private async Task OnClientRequest()
        {

        }

        private async Task OnClientConnect(TcpClient obj)
        {
            
        }

        /// <summary>
        /// The server instance that will recieve and send requests...
        /// </summary>
        private TcpListener _server;
    }
}
