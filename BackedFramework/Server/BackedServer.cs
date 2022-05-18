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
    /// <summary>
    /// The backbone of the entire server.
    /// Handles the connections and routing indirectly.
    /// </summary>
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

        /// <summary>
        /// Configure the server and initialize its components.
        /// </summary>
        /// <param name="config">The <see cref="BackedConfig"/>config to initialize the server with.</param>
        /// <returns>An instance of the <see cref="BackedServer"/>BackedServer.</returns>
        public static BackedServer Initialize(BackedConfig config) => new(config);

        /// <summary>
        /// Configure the TcpListener as well as start allowing connections to the server.
        /// </summary>
        private void ConfigureServer()
        {
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
        /// <param name="client">The TCPClient instance that the server will use to send data.</param>
        private static void OnClientConnectTest(TcpClient client)
        {
            // handle fixed buffers
            if (!BackedServer.Instance.Config.DynamicBuffers)
            {
                client.ReceiveBufferSize = BackedServer.Instance.Config.WriteBuffer;
                client.SendBufferSize = BackedServer.Instance.Config.ReadBuffer;
            }
            else
            {
                // set the max buffers if the config doesnt define them
                client.ReceiveBufferSize = int.MaxValue;
                client.SendBufferSize = int.MaxValue;
            }

            // last buffer size.
            var last = 0;

            // kind of pointless, but still good to have
            while (client.Available != 0 && client.Available != last)
            {
                System.Threading.Thread.Sleep(1);
                last = client.Available;
            }

            // notes:
            // there are 4 sets of /r/n in the request, /r/n /r/n /r/n /r/n
            client.ReadData(1024, (byte[] data) =>
            {
                // the index of the first set of /r/n,
                var index = data.ToList().IndexOf(0xD);

                // read until we hit our series of /r/n/r/n/r/n/r/n
                // or until we read 1024 bytes, because a header should not be longer than 1024 bytes
                while (index < 1024)
                {
                    // read until the 4 sets of /r/n are present.
                    // should be exactly as follows: "/r/n/r/n/r/n/r/n"
                    if (data[index] == 0xD && data[index + 1] == 0xA && // first set
                        data[index + 2] == 0xD && data[index + 3] == 0xA)
                        break;

                    // set our index with the next set of /r
                    index = data.ToList().IndexOf(0xD, index + 1);
                    if (index == -1)
                    {
                        //todo: drop the connection and flush data because the request is invalid.
                        throw new Exception("Index out of range...");
                    }
                }

                // store the header in the buffer so we know how much data is in the packet...
                List<byte> buffer = data.Take(index).ToList();

                // partially parse the header...
                string val = Encoding.UTF8.GetString(buffer.ToArray());
                using HTTPParser parser = new(val);

                // get the content length header and find out how much data we need to read.
                if (parser.Headers.ContainsKey("Content-Length"))
                {
                    // store that data
                    var len = parser.Headers["Content-Length"];

                    // since we "read 1024 bytes, we need to subtract the index
                    // because the index is the ending index of data we read.
                    // then we add 2 because we skip the /r/n
                    // so now there is only /r/n then the packet data
                    // after that, we subtract this value from the content-length
                    // and that is the remaining bytes to read.
                    var remainingDataToRead = Convert.ToInt64(len) - (1024 - (index + 4));

                    // read all the remaining data...
                    client.ReadData(remainingDataToRead, (byte[] c_buff) =>
                    {
                        // a temporary holder for our data.
                        List<byte> temp = new();
                        // add the remaining bytes of the previous packet we recieved and add it to our list.
                        temp.AddRange(data.TakeLast(data.Length - index - 4));
                        temp.AddRange(c_buff);// concat the remaining data from the current packet to the previous packet.

                        // set the post data.
                        parser.PostData = temp.ToArray();
                        // finally invoke the request.
                        ClientRequest.Invoke(client, parser);
                    });
                }
                else
                {
                    throw new Exception("Client is missing the content-length header.");
                }
            });

        }
    }
}
