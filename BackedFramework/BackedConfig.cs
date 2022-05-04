namespace BackedFramework
{
    /// <summary>
    /// The configuration class for the framework.
    /// </summary>
    public class BackedConfig
    {
        /// <summary>
        /// The default path for routing API calls, must include the trailing slash.
        /// </summary>
        /// <remarks>Example: "/api"</remarks>
        public string ApiPath { get; set; } = "/api";
        
        /// <summary>
        /// The base directory to use when serving files.
        /// </summary>
        public string RootDirectory { get; set; } = "root";

        /// <summary>
        /// The timeout (in seconds) for clients to be connected to the server without sending a request.
        /// </summary>
        /// <remarks>If a client doesnt send a request within the specified time, it will automatically disconnect the client from the server.</remarks>
        public int ConnectionTimeout { get; set; } = 10;

        /// <summary>
        /// The default API version, doesnt have to be used, but still here for completeness.
        /// </summary>
        public string ApiVersion { get; set; } = "v1";
        /// <summary>
        /// The port for the server to listen for requests on.
        /// </summary>
        public int ListeningPort { get; set; } = 80;
        /// <summary>
        /// Enable support for Secure-Socket-Layers...
        /// </summary>
        public bool UseSSL { get; set; } = false;

        /// <summary>
        /// The path to search for SSL certificates.
        /// </summary>
        public string SSLPath { get; set; } = "";

        /// <summary>
        /// Dynamic buffers are buffers created by the server based on the amount of information its sending, cannot gaurauntee that the buffer wont be used for anything malicious but it can be used to increase performance.
        /// </summary>
        public bool DynamicBuffers { get; set; } = true;
        /// <summary>
        /// The max amount of data (in bytes) that the server can recieve in a single request.
        /// </summary>
        /// <remarks>[!] Will not be used if dynamic buffers are enabled.</remarks>
        public int ReadBuffer { get; set; } = 4096;
        /// <summary>
        /// The max amount of data (in bytes)  that the server can send in a single request.
        /// </summary>
        /// <remarks>[!] Will not be used if dynamic buffers are enabled.</remarks>
        public int WriteBuffer { get; set; } = 4096;

        /// <summary>
        /// A request will be put into a new thread, including connections, will increase performance per request, but *WILL* increase cpu usage on high loads.
        /// </summary>
        public bool UseMultiThreading { get; set; } = true;
        /// <summary>
        /// The maximum amount of threads that the server will create, if the server reaches this threshold, newer connections will be slowed down significantly.
        /// </summary>
        /// <remarks>If this value is 0, then it will remain unlimited</remarks>
        public int MaxThreads { get; set; } = 15;
        /// <summary>
        /// A bool to check if we can have unlimited threads or not.
        /// </summary>
        internal bool UnlimitedThreads { get { return MaxThreads == 0; } }

        public string ServerName { get; set; } = $"BackedServer {Environment.Version}";

        public BackedConfig() { }

        /// <summary>
        /// An overload for the default constructor.
        /// </summary>
        /// <param name="config">Create a new config from a previous config</param>
        public BackedConfig(BackedConfig config)
        {
            this.ApiVersion = config.ApiVersion;
            this.ApiPath = config.ApiPath;
            this.ListeningPort = config.ListeningPort;
            this.UseSSL = config.UseSSL;
            this.SSLPath = config.SSLPath;
            this.DynamicBuffers = config.DynamicBuffers;
            this.ReadBuffer = config.ReadBuffer;
            this.WriteBuffer = config.WriteBuffer;
            this.UseMultiThreading = config.UseMultiThreading;
            this.MaxThreads = config.MaxThreads;
            this.RootDirectory = config.RootDirectory;
        }
    }
}