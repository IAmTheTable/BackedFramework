using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Extensions
{
    internal class TcpClient : System.Net.Sockets.TcpClient
    {
        internal NetworkStream stream;
        internal DateTimeOffset lastRequest { get; set; } = DateTime.Now;
        private bool _isWriting = false;
        public TcpClient() : base() { }
        public TcpClient(Socket s) : base()
        {
            base.Client = s;
            stream = new NetworkStream(s);
        }

        public new NetworkStream GetStream()
        {
            return this.stream;
        }
        
        public void ReadData(Action<byte[]> callback)
        {
            // allocate the buffer
            byte[] buffer = new byte[base.Available];
            // start reading from the stream
            var result = base.GetStream().BeginRead(buffer, 0, buffer.Length, _callback, null);

            // wait until completion
            if (!result.IsCompleted)
                result.AsyncWaitHandle.WaitOne();

            // handle the callback
            void _callback(IAsyncResult res)
            {
                // read amount of data then compare with the amount the client sent
                var amountRecieved = base.GetStream().EndRead(res);
                if (amountRecieved == buffer.Length)
                {
                    Logging.Logger.Log(Logging.Logger.LogLevel.Debug, "Successfully read all data from client.");
                    lastRequest = DateTime.Now;
                    callback.Invoke(buffer); // send data back to caller.
                }
            }
        }


        /// <summary>
        /// Write data to the connected socket.
        /// </summary>
        /// <param name="data">Buffer of data to send</param>
        /// <param name="offset">Offset index to start sending data.</param>
        /// <param name="count">The amount of data(in bytes) to send.</param>
        /// <returns>None</returns>
        public void WriteAsync(byte[] data, long offset = 0, long count = 0, CancellationToken cancellationToken = default)
        {
            lastRequest = DateTime.Now;

            // return if the client isnt connected
            if (!base.Connected)
                return;
            // write data to the stream
            count = data.Length;
            try
            {
                _isWriting = true;
                var res = base.GetStream().BeginWrite(data, (int)offset, (int)count, OnFinishedWriting, base.GetStream());
                if (!res.IsCompleted)
                    res.AsyncWaitHandle.WaitOne();

                //await base.GetStream().WriteAsync(data, offset, count, cancellationToken);
            }
            catch
            {
                Logging.Logger.Log(Logging.Logger.LogLevel.Warning, "Failed to write data to the stream.");
            }
        }

        public void WriteAsync(MemoryStream dataStream, long offset = 0, long count = 0, CancellationToken cancellationToken = default)
        {
            lastRequest = DateTime.Now;

            if (!base.Connected)
                return;

            try
            {
                this.WriteAsync(dataStream.ToArray());
                dataStream.Dispose();
            }
            catch
            {
                Logging.Logger.Log(Logging.Logger.LogLevel.Warning, "Failed to write data to the stream.");
            }
        }

        private void OnFinishedWriting(IAsyncResult result)
        {
            lastRequest = DateTime.Now;
            Logging.Logger.Log(Logging.Logger.LogLevel.Debug, "Finished writing data to the stream.");
            base.GetStream().EndWrite(result);
            base.GetStream().Dispose();
            _isWriting = false;
        }

        public string GetIp() => base.Client.RemoteEndPoint.ToString().Split(':')[0];

        public new void Dispose()
        {
            Logging.Logger.Log(Logging.Logger.LogLevel.Info, "Disposing TcpClient.");
            if (!_isWriting)
                base.Dispose();
        }
    }
}
