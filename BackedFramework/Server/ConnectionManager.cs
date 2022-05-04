using TcpClient = BackedFramework.Resources.Extensions.TcpClient;
using BackedFramework.Resources.Logging;
namespace BackedFramework.Server
{
    /// <summary>
    /// A managing class to help optimize server connections for requests.
    /// </summary>
    internal class ConnectionManager
    {
        /// <summary>
        /// The instance of the connection manager
        /// </summary>
        internal static ConnectionManager _instance;
        /// <summary>
        /// The mapping of connections, keyed by the connection's ip address...
        /// </summary>
        private Dictionary<string, TcpClient> _connections = new();

        internal ConnectionManager()
        {
            // Only allow one construction of this class.
            if(_instance is not null)
                throw new Exception("Only one instance of ConnectionManager is allowed");
            
            _instance = this;

            Task.Run(CheckConnections);
        }

        public static ConnectionManager Initialize() => new();

        private void CheckConnections()
        {
            start:
            foreach(var key in this._connections.Keys)
            {
                var connection = this._connections[key];
                
                bool isTimeout = connection.lastRequest.ToUnixTimeSeconds() + BackedServer.Instance.Config.ConnectionTimeout < DateTimeOffset.Now.ToUnixTimeSeconds();
                bool invalidInstance = connection is null;

                if (isTimeout || invalidInstance && connection.Connected)
                    this.RemoveConnection(key);
            }
            Task.Delay(100);
            goto start;
        }

        /// <summary>
        /// Retrieve a connection from the manager.
        /// </summary>
        /// <param name="ip">The IP of the client you wish to get</param>
        /// <returns>The TcpClient instance of the target client.</returns>
        internal TcpClient GetConnection(string ip)
        {
            if (!_connections.ContainsKey(ip))
                return null;
            
            return _connections[ip];
        }

        /// <summary>
        /// Insert a connection into the manager.
        /// </summary>
        /// <param name="client">TcpClient instance of the connection.</param>
        internal void AddConnection(TcpClient client)
        {
            if (!_connections.ContainsKey(client.Client.RemoteEndPoint.ToString().Split(':')[0]))
            {
                Logger.Log(Logger.LogLevel.Debug, $"Created connection for client: {client.Client.RemoteEndPoint.ToString().Split(':')[0]}");
                _connections.Add(client.Client.RemoteEndPoint.ToString().Split(':')[0], client);
                return;
            }

            Logger.Log(Logger.LogLevel.Info, $"The connection has already been mapped for client: {client.Client.RemoteEndPoint.ToString().Split(':')[0]}");
        }

        /// <summary>
        /// Remove a connection from the manager.
        /// </summary>
        /// <param name="ip">Ip of the connection.</param>
        internal void RemoveConnection(string ip)
        {
            if (!_connections.ContainsKey(ip))
                return;

            Logger.Log(Logger.LogLevel.Debug, $"Removed connection for client: {ip}");

            _connections.Remove(ip);
        }
    }
}
