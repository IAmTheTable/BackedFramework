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
using BackedFramework.Resources.Statistics;
using Thread = BackedFramework.Resources.Extensions.Thread;

namespace BackedFramework.Server
{
    public class BackedServer
    {
        /// <summary>
        /// Called when a client attempts to connect to the server.
        /// </summary>
        internal static event Func<TcpClient, Task> ClientConnect;

        /// <summary>
        /// Called when a valid request is made to the server from a client.
        /// </summary>
        internal static event Func<TcpClient, HTTPParser, Task> ClientRequest;

        /// <summary>
        /// The static instance of the server.
        /// </summary>
        public static BackedServer Instance { get; private set; }

        /// <summary>
        /// Configuration instance of the server.
        /// </summary>
        public BackedConfig Config;

        /// <summary>
        /// The server instance that will recieve and send requests...
        /// </summary>
        private readonly TcpListener _server;

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
            ClientConnect += OnClientConnect;
            ClientRequest += OnClientRequest;

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
                    await ClientConnect.Invoke(client);
                }
            }).Start();
            
            System.Threading.Thread.Sleep(-1);
        }
        
        private static async Task OnClientRequest(TcpClient client, HTTPParser parser)
        {
            //Console.WriteLine($"Current thread context: {Environment.CurrentManagedThreadId}");

            RequestContext reqCtx = new(parser);
            ResponseContext rspCtx = new();
            rspCtx.DefineClient(client); // set the client for the response context.

            if (!RouteManager.s_instance.TryExecuteRoute(parser, rspCtx, reqCtx))
            {
#if DEBUG
                throw new Exception("failed to execute route...");
#else
                rspCtx.SetStatusCode(StatusCode.NotFound);
                rspCtx.Content = "Requested Resource Not Found";
#endif
                // maybe log failed requests...
            }
        }


        /// <summary>
        /// Called when a client connects to the server.
        /// </summary>
        /// <param name="client">The TcpClient instance of the client that connects to the server.</param>
        /// <returns>None</returns>
        /// TODO: Make sure to handle the threading...
        private static async Task OnClientConnect(TcpClient client)
        {
            //Console.WriteLine($"Current thread context: {Environment.CurrentManagedThreadId}");
            using StatisticsManager statManager = new();
            await Task.Delay(0);
            new Thread(async () =>
            {
                var clientStream = client.GetStream();
                if (client.Available < 1)
                {
                    await clientStream.DisposeAsync();
                    return;
                }

                // determine the buffer size per request
                var bufferSize = Instance.Config.DynamicBuffers ? client.Available : Instance.Config.ReadBuffer;
#if DEBUG
                statManager.Start(); // get statistics
#endif
                // allocate the buffer
                var buffer = new byte[bufferSize];
                var totalRead = await clientStream.ReadAsync(buffer);

                // if we havent read all the data already, then continue to read the buffer.
                readData:
                if (client.Available != 0)
                {
                    var nextToRead = client.Available - totalRead;
                    var oldRead = totalRead;
                    Array.Resize(ref buffer, buffer.Length + bufferSize);
                    totalRead += await clientStream.ReadAsync(buffer, oldRead, bufferSize);
                    goto readData;
                }
                Array.Resize(ref buffer, totalRead);

#if DEBUG
                //Console.WriteLine($"Current thread context: {Environment.CurrentManagedThreadId}");
                statManager.End();
                statManager.PrintTiming();
#endif

                using HTTPParser parser = new(Encoding.UTF8.GetString(buffer));
                await ClientRequest.Invoke(client, parser);
            }).Start();
        }
    }
}
