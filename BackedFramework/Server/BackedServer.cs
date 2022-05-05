global using System.Net;
global using System.Net.Sockets;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using BackedFramework.Api.Routing;
using BackedFramework.Resources.Exceptions;
using BackedFramework.Resources.HTTP;
using BackedFramework.Resources.Logging;
using BackedFramework.Resources.Statistics;

using Thread = BackedFramework.Resources.Extensions.Thread;
using TcpListener = BackedFramework.Resources.Extensions.TcpListener;
using TcpClient = BackedFramework.Resources.Extensions.TcpClient;

namespace BackedFramework.Server
{
    public class BackedServer
    {
        /// <summary>
        /// Called when a client attempts to connect to the server.
        /// </summary>
        internal static event Action<TcpClient> ClientConnect;

        /// <summary>
        /// Called when a valid request is made to the server from a client.
        /// </summary>
        internal static event Action<TcpClient, HTTPParser> ClientRequest;

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
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            Process.GetCurrentProcess().Exited += BackedServer_Exited;

            // only allow a single instance of the server to exist
            if (Instance is not null)
                throw new MultiInstanceException("Only one instance of BackedServer can be created.");

            // set our config and instance
            this.Config = config;
            Instance = this;

            // set the default listening port
            this._server = new(IPAddress.Any, this.Config.ListeningPort);

            // subscribe to the server events
            ClientConnect += OnClientConnectTest; //OnClientConnect
            ClientRequest += OnClientRequest;

            ConfigureServer();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.IsTerminating)
            {
                if (!Directory.Exists("backed-logs"))
                    Directory.CreateDirectory("backed-logs");

                File.WriteAllLines($"backed-logs/{DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss").Replace(" ", "-").Replace("\\", "-")}.log", Logger.DumpLogs());
                Logger.Log(Logger.LogLevel.Debug, "Dumped all logs");
            }
        }

        private void BackedServer_Exited(object sender, EventArgs e)
        {
            if (!Directory.Exists("backed-logs"))
                Directory.CreateDirectory("backed-logs");

            File.WriteAllLines($"backed-logs/{DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss").Replace(" ", "-").Replace("\\", "-")}.log", Logger.DumpLogs());
            Logger.Log(Logger.LogLevel.Debug, "Dumped all logs");
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (!Directory.Exists("backed-logs"))
                Directory.CreateDirectory("backed-logs");

            File.WriteAllLines($"backed-logs/{DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss").Replace(" ", "-").Replace("\\", "-")}.log", Logger.DumpLogs());
            Logger.Log(Logger.LogLevel.Debug, "Dumped all logs");
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
            ConnectionManager.Initialize();

            // start the server
            this._server.Start();

            // accept connections
            this._server.BeginAcceptTcpClient(OnClientRequestConnection);

            /*new Thread(async () =>
            {
                while (true)
                {
                    if (!this._server.Pending())
                        continue;


                    //var client = await this._server.AcceptTcpClientAsync();
                    //ClientConnect.Invoke(client);
                }
            }).Start();*/

            System.Threading.Thread.Sleep(-1);
        }

        private void OnClientRequestConnection(IAsyncResult ar)
        {
            try
            {
                var client = this._server.EndAcceptTcpClient(ar);
                ClientConnect.Invoke(client);
                this._server.BeginAcceptTcpClient(OnClientRequestConnection, null);
            }
            catch (Exception e)
            {
                Logger.Log(Logger.LogLevel.Error, e.Message);
            }
        }

        private static void OnClientRequest(TcpClient ip, HTTPParser parser)
        {
            //Console.WriteLine($"Current thread context: {Environment.CurrentManagedThreadId}");

            using RequestContext reqCtx = new(parser);
            using ResponseContext rspCtx = new();
            rspCtx.DefineRequestContext(reqCtx);
            rspCtx.DefineClient(ip); // set the client for the response context.

            // add keep alive connection stuff to the response context, will move into the class later

            if (reqCtx.RequestHeaders.ContainsKey("Connection"))
                if (reqCtx.RequestHeaders["Connection"] == "keep-alive")
                    rspCtx.Headers.Add("Connection", "close");

            // run the request through the route manager
            if (!RouteManager.s_instance.TryExecuteRoute(parser, rspCtx, reqCtx))
            {
                Logger.Log(Logger.LogLevel.Warning, "Failed to execute route...");
#if DEBUG
                throw new Exception("failed to execute route...");
#else
                rspCtx.SendNotFound();
#endif
                // maybe log failed requests...
            }
        }

        private static void OnClientConnectTest(TcpClient client)
        {
            // handle fixed buffers
            if (!BackedServer.Instance.Config.DynamicBuffers)
            {
                // TODO: add fixed buffer support later.
                return;
            }

            while (client.Available == 0)
            {
                System.Threading.Thread.Sleep(1);
            }

            ConnectionManager._instance.AddConnection(client);

            // allocate buffer and start reading from the client
            var buffer = new byte[client.Available];
            client.GetStream().BeginRead(buffer, 0, buffer.Length, OnDataRecieved, null);

            // Function to handle the data recieved from the client.
            void OnDataRecieved(IAsyncResult result)
            {
                var countDataRecieved = client.GetStream().EndRead(result);
                // check if the data matches...
                if (buffer.Length == countDataRecieved)
                {
                    Logger.Log(Logger.LogLevel.Debug, "Successfully read data from client.");
                    // convert the buffer into an instance of HTTPParser, then invoke the client request event.
                    using HTTPParser parser = new(Encoding.UTF8.GetString(buffer));
                    ClientRequest.Invoke(client, parser);
                }
            }
        }

        /// <summary>
        /// Called when a client connects to the server.
        /// </summary>
        /// <param name="client">The TcpClient instance of the client that connects to the server.</param>
        /// <returns>None</returns>
        /// TODO: Make sure to handle the threading...
        private static void OnClientConnect(TcpClient client)
        {
            //Console.WriteLine($"Current thread context: {Environment.CurrentManagedThreadId}");
            using StatisticsManager statManager = new();
            new Thread(async () =>
            {
                var clientStream = client.GetStream();

                while (client.Available < 1)
                {
                    await Task.Delay(1);
                    Logger.Log(Logger.LogLevel.Debug, "Waiting for client to send data...");
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
                ClientRequest.Invoke(client, parser);
            }).Start();
        }
    }
}
