global using System.Net;
global using System.Net.Sockets;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BackedFramework.Api.Routing;
using BackedFramework.Resources.Exceptions;
using BackedFramework.Resources.HTTP;
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
        public BackedConfig Config;

        /// <summary>
        /// Default constructor for creating the instance of the server.
        /// </summary>
        /// <param name="config">Configuration for the server</param>
        /// <exception cref="MultiInstanceException">Thrown when trying to create more than one instance of a BackedServer.</exception>
        internal BackedServer(BackedConfig config)
        {
            // only allow a single instance of the server to exist
            if (Instance is not null)
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

        public static BackedServer Initialize(BackedConfig config) => new(config);

        /// <summary>
        /// Configure the TcpListener as well as start allowing connections to the server.
        /// </summary>
        private void ConfigureServer()
        {
            /*
             * TODO: MAKE SURE TO CONFIGURE ALL THE SERVER EXTENSIONS
             * EX; ROUTE MANAGER, CONNECTION MANAGER, THREAD MANAGER, ETC
             */

            // will be automatically disposed upon leaving the current scope
            using var x = new RouteManager();

            // start the server
            this._server.Start();

            new Thread(async () =>
            {
                while (true)
                {
                    if (!this._server.Pending())
                        continue;

                    var client = await this._server.AcceptTcpClientAsync();
                    await Events.ServerEvents.InvokeClientConnect(client);
                }
            }).Start();
        }

        private async Task OnClientRequest(TcpClient client, HTTPParser parser)
        {
            Console.WriteLine($"Current thread context: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

            RequestContext reqCtx = new(parser);
            ResponseContext respCtx = new(parser);

            
        }

        private async Task OnClientConnect(TcpClient client)
        {
            new Thread(async () =>
            {
                var clientStream = client.GetStream();

                if (client.Available < 1)
                {
                    await clientStream.DisposeAsync();
                    return;
                }

                new Resources.Extensions.Thread(() =>
                {
                    Console.WriteLine("hello world@!");
                }).Start();

                // TODO: Add support for dynamic and static buffers
                var buffer = new byte[client.Available];
                var totalRead = await clientStream.ReadAsync(buffer);

                // if we havent read all the data already, then continue to read the buffer.
                if (totalRead != client.Available)
                {
                    var nextToRead = client.Available - totalRead;
                    totalRead += await clientStream.ReadAsync(buffer, totalRead, nextToRead);
                }

                Console.WriteLine($"Current thread context: {System.Threading.Thread.CurrentThread.ManagedThreadId}");

                using HTTPParser parser = new(Encoding.UTF8.GetString(buffer));
                await Events.ServerEvents.InvokeClientRequest(client, parser);
            }).Start();
        }

        /// <summary>
        /// The server instance that will recieve and send requests...
        /// </summary>
        private readonly TcpListener _server;
    }
}
