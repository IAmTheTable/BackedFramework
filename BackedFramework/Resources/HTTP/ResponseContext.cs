using BackedFramework.Resources.Logging;
using BackedFramework.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TcpClient = BackedFramework.Resources.Extensions.TcpClient;

namespace BackedFramework.Resources.HTTP
{
    /// <summary>
    /// A class that represents an HTTP response, but also provides simple yet helpful methods and properties to help build your response easier.
    /// </summary>
    public class ResponseContext : ResponseBase, IDisposable
    {
        private static int _instanceCount = 0;
        private string _clientAddress;
        private CancellationToken _cancellationToken = new();
        private RequestContext _requestContext;
        
        private TcpClient _client
        {
            get
            {
                return ConnectionManager._instance.GetConnection(_clientAddress);
            }
        }

        internal ResponseContext() : base(new())
        {
            Console.WriteLine("Created response context");
            _instanceCount++;
            base.Headers.Add("BackedFramework Version", BackedServer.Instance.Config.ApiVersion);

            _cancellationToken.Register(() =>
            {
                Logger.Log(Logger.LogLevel.Warning, "Cancellation token has been triggered, closing connection");
                this.Dispose();
            });
        }

        internal void DefineRequestContext(RequestContext ctx) => this._requestContext = ctx;

        /// <summary>
        /// A function to set the client, easier than constructing... might change later...
        /// </summary>
        /// <param name="client">Client instance</param>
        internal void DefineClient(string clientIp) => this._clientAddress = clientIp;

        /// <summary>
        /// Send a 3XX redirect type request to the client.
        /// </summary>
        /// <param name="location">The url the client will redirect to.</param>
        /// <remarks>Ex: https://youtube.com</remarks>
        public void Redirect(string location, HttpStatusCode code = HttpStatusCode.Redirect)
        {
            
            if ((int)code < 300 || (int)code > 308)
            {
                throw new ArgumentException("The code must be a valid redirect code. See http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.3.2");
            }
            base.StatusCode = HttpStatusCode.Found;
            base.Headers.Add("Location", location);
            base.Content = base.ToString();

            // include buffer size support.
            if (!BackedServer.Instance.Config.DynamicBuffers)
            {
                long totalSent = 0;
                var dataLeft = base.ToStream().Length - totalSent;

            writeData:
                this._client.WriteAsync(base.ToStream(), totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer, _cancellationToken);
                totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                if (dataLeft != 0)
                    goto writeData;

                return;
            }

            // write data to client
            this._client.WriteAsync(base.ToStream(), cancellationToken: _cancellationToken);
        }

        /// <summary>
        /// Send a file to the client, does include HTTP headers.
        /// </summary>
        /// <param name="fromBaseDirectory">Should the function look for the file in the defined root directory?</param>
        /// <param name="path">Location of the file.</param>
        public void SendFile(bool fromBaseDirectory, string path = "")
        {
            

            if (File.Exists(fromBaseDirectory == true ? BackedServer.Instance.Config.RootDirectory + "/" + path : path))
            {
                base.Content = File.ReadAllText(fromBaseDirectory == true ? BackedServer.Instance.Config.RootDirectory + "/" + path : path);
            }
            else
            {
                base.Content = "Request resource not found.";
                base.StatusCode = HttpStatusCode.NotFound;
            }
            try
            {
                var dataToSend = base.ToBytes();
                // include buffer size support.
                if (!BackedServer.Instance.Config.DynamicBuffers)
                {
                    int totalSent = 0;
                    int dataLeft = dataToSend.Length;
                writeData:
                    dataLeft = dataToSend.Length - totalSent;
                    this._client.WriteAsync(base.ToStream(), totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer, _cancellationToken);
                    totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                    if (dataLeft != 0)
                        goto writeData;

                    return;
                }

                // write data to client
                this._client.WriteAsync(base.ToStream());
                return;
            }
            catch (InvalidOperationException)
            {
                throw new Exception("Attempted to finalize a request, but the request was already finalized.");
            }
            finally
            {
                GC.Collect();
            }
        }

        /// <summary>
        /// Send a file to the client, only sends the raw file, no http header.
        /// </summary>
        /// <param name="fromBaseDirectory">Is the file location in the root directory, specified in the server config</param>
        /// <param name="path">The location of the file</param>
        /// <exception cref="Exception">Thrown if the server attempts to send a response to the client, but the client is unavailable to send data to.</exception>
        public async void SendRawFile(bool fromBaseDirectory, string path = "")
        {

            byte[] data = Array.Empty<byte>();
            bool success = false;
            if (File.Exists(fromBaseDirectory == true ? BackedServer.Instance.Config.RootDirectory + "/" + path : path))
            {
                data = await File.ReadAllBytesAsync(fromBaseDirectory == true ? BackedServer.Instance.Config.RootDirectory + "/" + path : path, _cancellationToken);
                success = true;
            }
            else
            {
                base.Content = "Request resource not found.";
                base.StatusCode = HttpStatusCode.NotFound;
            }
            try
            {
                // if the file we tried to read was not found...
                if (!success)
                {
                    try
                    {
                        var dataToSend = base.ToBytes();
                        // include buffer size support.
                        if (!BackedServer.Instance.Config.DynamicBuffers)
                        {
                            int totalSent = 0;
                            int dataLeft = dataToSend.Length;

                        writeData:
                            dataLeft = dataToSend.Length - totalSent;
                            this._client.WriteAsync(base.ToStream(), totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer, _cancellationToken);
                            totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                            if (dataLeft != 0)
                                goto writeData;

                            return;
                        }
                        // write data to client
                        this._client.WriteAsync(base.ToStream(), cancellationToken: _cancellationToken);
                        return;
                    }
                    catch (InvalidOperationException)
                    {
                        throw new Exception("Attempted to finalize a request, but the request was already finalized.");
                    }
                    finally
                    {
                        GC.Collect();
                    }
                }
                // if we have fixed buffers, use them.
                if (!BackedServer.Instance.Config.DynamicBuffers)
                {
                    // define variables that help us calculate how much data we need to send with fixed buffers.
                    int totalSent = 0;
                    int dataLeft = data.Length;

                // continue writing data in segments.
                writeData:
                    dataLeft = data.Length - totalSent;
                    try
                    {
                        this._client.WriteAsync(base.ToStream(), totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer, _cancellationToken);
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Failed to finish writing to the client.");
                    }
                    // increase our total data sent count...
                    totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                    if (dataLeft != 0)
                        goto writeData;

                    if (_requestContext.RequestHeaders.ContainsKey("Connection"))
                        if (_requestContext.RequestHeaders["Connection"] != "keep-alive")
                            await this._client.GetStream().DisposeAsync();
                    return;
                }

                // send the data to the client
                try
                {
                    this._client.WriteAsync(base.ToStream(), cancellationToken: _cancellationToken);
                }
                catch (IOException)
                {
                    Logger.Log(Logger.LogLevel.Error, "Failed to finish writing to the client.");
                }
            }
            catch (InvalidOperationException)
            {
                throw new Exception("Attempted to finalize a request, but the request was already finalized.");
            }
        }

        /// <summary>
        /// A pre-written function to help send a 404 response to the client.
        /// </summary>
        /// <exception cref="Exception">Thrown when the client has already been sent a response or the client's stream is invalid.</exception>
        public void SendNotFound()
        {
            

            base.Content = "Request resource not found.";
            base.StatusCode = HttpStatusCode.NotFound;
            try
            {
                var dataToSend = base.ToBytes();
                // include buffer size support.
                if (!BackedServer.Instance.Config.DynamicBuffers)
                {
                    int totalSent = 0;
                    int dataLeft = dataToSend.Length;
                writeData:
                    dataLeft = dataToSend.Length - totalSent;
                    this._client.WriteAsync(base.ToStream(), totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer, _cancellationToken);
                    totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                    if (dataLeft != 0)
                        goto writeData;

                    return;
                }

                // write data to client
                this._client.WriteAsync(base.ToStream(), cancellationToken: _cancellationToken);
            }
            catch (InvalidOperationException)
            {
                throw new Exception("Attempted to finalize a request, but the request was already finalized.");
            }
            finally
            {
                GC.Collect();
            }
        }


        /// <summary>
        /// Write the response to the client.
        /// </summary>
        /// <param name="content">The string content to send.</param>
        public void FinishRequest()
        {
            

            var dataToSend = base.ToBytes();
            // include buffer size support.
            if (!BackedServer.Instance.Config.DynamicBuffers)
            {
                int totalSent = 0;
                int dataLeft;

            writeData:
                dataLeft = dataToSend.Length - totalSent;

                this._client.WriteAsync(base.ToStream(), totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer, _cancellationToken);

                totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;

                if (dataLeft != 0)
                    goto writeData;

                return;
            }

            // write data to client
            this._client.WriteAsync(base.ToStream(), cancellationToken: _cancellationToken);
        }

        /// <summary>
        /// Add a header to the client.
        /// </summary>
        /// <param name="key">Name of the header.</param>
        /// <param name="value">Value of the header.</param>
        public void AddHeader(string key, string value) => base.Headers.Add(key, value);

        /// <summary>
        /// Send a Set-Cookie header to the client, which sets the cookie to its corresponding key=value pair.
        /// </summary>
        /// <param name="name">Name of the cookie</param>
        /// <param name="value">Value of the cookie</param>
        /// <param name="secure"></param>
        /// <param name="httpOnly"></param>
        /// <param name="expires"></param>
        public void SetCookie(string name, string value, bool secure = false, bool httpOnly = false, DateTime? expires = null)
        {
            var cookie = new Cookie(name, value)
            {
                Secure = secure,
                HttpOnly = httpOnly,
                Expires = expires ?? expires.Value,
                Discard = !expires.HasValue,
            };
            base.Headers.Add("Set-Cookie", cookie.ToString());
        }

        public new void Dispose()
        {
            Logger.Log(Logger.LogLevel.Debug, $"Request completed at {this._requestContext.Path}, there are currently: {_instanceCount} requests in progress and there are {_instanceCount - 1} zombie requests.");

            _instanceCount--;

            GC.Collect();
            GC.SuppressFinalize(this);
            GC.WaitForPendingFinalizers();
        }
    }
}
