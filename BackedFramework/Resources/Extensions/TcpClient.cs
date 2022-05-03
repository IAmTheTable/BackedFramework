using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Extensions
{
    internal class TcpClient : System.Net.Sockets.TcpClient
    {
        private bool _isWriting = false;
        public TcpClient() : base() { }
        public TcpClient(Socket s) : base()
        {
            this.Client = s;
        }

        /// <summary>
        /// Write data to the connected socket.
        /// </summary>
        /// <param name="data">Buffer of data to send</param>
        /// <param name="offset">Offset index to start sending data.</param>
        /// <param name="count">The amount of data(in bytes) to send.</param>
        /// <returns>None</returns>
        public void WriteAsync(byte[] data, int offset = 0, int count = 0, CancellationToken cancellationToken = default(CancellationToken))
        {
            // return if the client isnt connected
            if (!base.Connected)
                return;
            // write data to the stream
            count = data.Length;
            try
            {
                _isWriting = true;
                var res = base.GetStream().BeginWrite(data, offset, count, OnFinishedWriting, base.GetStream());
                if (!res.IsCompleted)
                    res.AsyncWaitHandle.WaitOne();

                //await base.GetStream().WriteAsync(data, offset, count, cancellationToken);
            }
            catch
            {
                Logging.Logger.Log(Logging.Logger.LogLevel.Warning, "Failed to write data to the stream.");
            }
        }

        private void OnFinishedWriting(IAsyncResult result)
        {
            Logging.Logger.Log(Logging.Logger.LogLevel.Debug, "Finished writing data to the stream.");
            base.GetStream().EndWrite(result);
            base.GetStream().Flush();
            _isWriting = false;
        }

        public new void Dispose()
        {
            if (!_isWriting)
                base.Dispose();
        }
    }
}
