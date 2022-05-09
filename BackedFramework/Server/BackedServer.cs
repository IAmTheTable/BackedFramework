global using System.Net;
global using System.Net.Sockets;

using System.Diagnostics;
using System.Text;
using BackedFramework.Api.Routing;
using BackedFramework.Resources.Exceptions;
using BackedFramework.Resources.HTTP;
using BackedFramework.Resources.Logging;

using TcpListener = BackedFramework.Resources.Extensions.TcpListener;
using TcpClient = BackedFramework.Resources.Extensions.TcpClient;
using BackedFramework.Resources.Extensions;

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

            // configure the thread pool to use the number of threads specified in the config
            ThreadPool.SetMaxThreads(this.Config.MaxThreads, this.Config.MaxThreads);

            // will be automatically disposed upon leaving the current scope
            using var x = new RouteManager();
            
            // start the server
            this._server.Start();

            // accept connections
            this._server.BeginAcceptTcpClient(OnClientRequestConnection);
        }

        /// <summary>
        /// Called when a client attempts to connect to the server.
        /// </summary>
        /// <param name="ar"></param>
        private void OnClientRequestConnection(IAsyncResult ar)
        {
            new Thread(() =>
            {
                try
                {
                    // continuously accept connections and handle them.
                    var client = this._server.EndAcceptTcpClient(ar);
                    ClientConnect.Invoke(client);
                    this._server.BeginAcceptTcpClient(OnClientRequestConnection, null);
                }
                catch (Exception e)
                {
                    Logger.Log(Logger.LogLevel.Error, e.Message);
                }
            }).Start();
        }

        /// <summary>
        /// Called when a client requests a resource from the server.
        /// </summary>
        /// <param name="client">The TcpClient that requests the resource.</param>
        /// <param name="parser">Http parser instance that contains information about the request.</param>
        private static void OnClientRequest(TcpClient client, HTTPParser parser)
        {
            //Console.WriteLine($"Current thread context: {Environment.CurrentManagedThreadId}");

            using RequestContext reqCtx = new(parser);
            using ResponseContext rspCtx = new();
            rspCtx.DefineRequestContext(reqCtx);
            rspCtx.DefineClient(client); // set the client for the response context.

            // add keep alive connection stuff to the response context, will move into the class later

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
            client.Dispose();
        }

        /// <summary>
        /// Called when a client is connected to the server.
        /// </summary>
        /// <param name="client"></param>
        private static void OnClientConnectTest(TcpClient client)
        {
            // handle fixed buffers
            if (!BackedServer.Instance.Config.DynamicBuffers)
            {
                client.ReceiveBufferSize = BackedServer.Instance.Config.WriteBuffer;
                client.SendBufferSize = BackedServer.Instance.Config.ReadBuffer;
            }

            while (client.Available == 0)
            {
                System.Threading.Thread.Sleep(1);
            }

            client.ReadData((byte[] data) =>
            {
                using HTTPParser parser = new(Encoding.UTF8.GetString(data));
                ClientRequest.Invoke(client, parser);
            });
        }
    }
}
