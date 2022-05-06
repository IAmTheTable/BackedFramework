namespace BackedFramework.Resources.Extensions
{
    internal class TcpListener : System.Net.Sockets.TcpListener
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TcpListener"/> class.
        /// </summary>
        /// <param name="localaddr">IPAddress to bind to.</param>
        /// <param name="port">The port to accept connections on</param>
        public TcpListener(IPAddress localaddr, int port) : base(localaddr, port)
        {
        }

        /// <summary>
        /// Accepts a connection.
        /// </summary>
        /// <param name="callback">The method to be called when the function completes.</param>
        /// <param name="state">An object that will be sent to the function when it completes.</param>
        /// <returns></returns>
        public new IAsyncResult BeginAcceptTcpClient(AsyncCallback callback, object state = null) => base.BeginAcceptTcpClient(callback, state);

        /// <summary>
        /// Accepts a connection.
        /// </summary>
        /// <param name="asyncResult">The handle to end the connection request.</param>
        /// <returns>A TcpClient instance.</returns>
        public new TcpClient EndAcceptTcpClient(IAsyncResult asyncResult)
        {
            var client = base.EndAcceptTcpClient(asyncResult);
            return new(client.Client);
        }
    }
}
