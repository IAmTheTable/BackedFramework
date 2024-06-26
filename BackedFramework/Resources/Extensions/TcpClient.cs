﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.Extensions
{
    internal class TcpClient : System.Net.Sockets.TcpClient
    {
        internal DateTimeOffset LastRequest { get; set; } = DateTime.Now;
        private bool _isWriting = false;

        /// <summary>
        /// A list of tasks that the client is currently running, used for waiting on disposal on the client.
        /// </summary>
        private readonly List<Task> _currentOperations = new();

        public TcpClient() : base() { }
        public TcpClient(Socket s) : base()
        {
            base.Client = s;
        }

        public void ReadData(Action<byte[]> callback)
        {
            while (Available < 1)
                Thread.Sleep(1);

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
                    Logging.Logger.LogInt(Logging.Logger.LogLevel.Debug, "Successfully read all data from client.");
                    LastRequest = DateTime.Now;
                    callback.Invoke(buffer); // send data back to caller.
                }
            }
        }

        public void ReadData(long amt, Action<byte[]> callback)
        {
            while (Available < 1)
                Thread.Sleep(1);

            amt = amt > this.Available ? this.Available : amt;
            // allocate the buffer
            byte[] buffer = new byte[amt];

            /*while (Client.Available < amt)
            {
                Thread.Sleep(50);
            }*/

            // start reading from the stream
            var result = base.GetStream().BeginRead(buffer, 0, (int)amt, _callback, null);

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
                    Logging.Logger.LogInt(Logging.Logger.LogLevel.Debug, "Successfully read all data from client.");
                    LastRequest = DateTime.Now;
                    callback.Invoke(buffer); // send data back to caller.
                }
                else if (amountRecieved < buffer.Length)
                {
                    Logging.Logger.LogInt(Logging.Logger.LogLevel.Warning, "Recieved less data than requested...");
                    LastRequest = DateTime.Now;

                    ReadData(amt - amountRecieved, (byte[] buff) =>
                    {
                        buff.CopyTo(buffer, amountRecieved);
                        Logging.Logger.LogInt(Logging.Logger.LogLevel.Debug, "Read the remaining data?");
                        if (Client.Available == 0)
                        {
                            Logging.Logger.LogInt(Logging.Logger.LogLevel.Debug, "Confirmed all data!");
                            callback.Invoke(buffer);
                        }
                    });
                }
                else if (amountRecieved > buffer.Length)
                {
                    Logging.Logger.LogInt(Logging.Logger.LogLevel.Debug, "Got more data than requested?");
                    LastRequest = DateTime.Now;
                }
            }
        }


        /// <summary>
        /// Write data to the connected socket.
        /// </summary>
        /// <param name="data">Buffer of data to send</param>
        /// <param name="offset">Offset index to start sending data.</param>
        /// <param name="count">The amount of data(in bytes) to send.</param>
        /// <param name="cancellationToken">A cancellationToken that can be used.</param>
        /// <returns>None</returns>
        public void WriteAsync(byte[] data, long offset = 0, long count = 0, CancellationToken cancellationToken = default)
        {
            var task = Task.Run(() =>
            {
                LastRequest = DateTime.Now;

                // return if the client isnt connected
                if (!base.Connected)
                    return;
                // write data to the stream
                count = data.Length;
                try
                {
                    _isWriting = true;
                    var res = base.GetStream().BeginWrite(data, (int)offset, (int)count, OnFinishedWriting, data);
                    if (!res.IsCompleted)
                        res.AsyncWaitHandle.WaitOne();
                }
                catch
                {
                    Logging.Logger.LogInt(Logging.Logger.LogLevel.Warning, "Failed to write data to the stream.");
                }
            }, cancellationToken);

            _currentOperations.Add(task);
        }

        private void OnFinishedWriting(IAsyncResult result)
        {
            var task = Task.Run(() =>
            {
                LastRequest = DateTime.Now;
                Logging.Logger.LogInt(Logging.Logger.LogLevel.Debug, "Finished writing data to the stream.");
                try
                {
                    base.GetStream().EndWrite(result);
                }
                catch (ObjectDisposedException) { }
                _isWriting = false;
            });

            _currentOperations.Add(task);
        }

        public string GetIp() => base.Client.RemoteEndPoint.ToString().Split(':')[0];

        public new void Dispose()
        {
            Logging.Logger.LogInt(Logging.Logger.LogLevel.Info, "Disposing TcpClient.");

            Task.WaitAll(_currentOperations.ToArray());

            if (this._isWriting)
            {
                while (this._isWriting)
                    Task.Delay(1);
            }

            base.Dispose();
            Logging.Logger.LogInt(Logging.Logger.LogLevel.Info, "Finished Disposition.");
        }
    }
}
