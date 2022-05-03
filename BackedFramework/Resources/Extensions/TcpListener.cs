using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Extensions
{
    internal class TcpListener : System.Net.Sockets.TcpListener
    {
        public TcpListener(IPEndPoint localEP) : base(localEP)
        {
        }

        public TcpListener(IPAddress localaddr, int port) : base(localaddr, port)
        {
        }

        public new IAsyncResult BeginAcceptTcpClient(AsyncCallback callback, object state = null) => base.BeginAcceptTcpClient(callback, state);

        public new TcpClient EndAcceptTcpClient(IAsyncResult asyncResult)
        {
            var client = base.EndAcceptTcpClient(asyncResult);
            return new(client.Client);
        }

        public new async Task<TcpClient> AcceptTcpClientAsync()
        {
            var client = await base.AcceptTcpClientAsync();
            return new(client.Client);
        }
    }
}
