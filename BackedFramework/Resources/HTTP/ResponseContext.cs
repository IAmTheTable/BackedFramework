using BackedFramework.Resources.Logging;
using BackedFramework.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.HTTP
{
    /// <summary>
    /// A class that represents an HTTP response, but also provides simple yet helpful methods and properties to help build your response easier.
    /// </summary>
    public class ResponseContext : ResponseBase, IDisposable
    {
        private static int _instanceCount = 0;
        private TcpClient _client;
        internal ResponseContext() : base(new())
        {
            Console.WriteLine("Created response context");
            _instanceCount++;
            base.Headers.Add("BackedFramework Version", BackedServer.Instance.Config.ApiVersion);
        }

        /// <summary>
        /// A function to set the client, easier than constructing... might change later...
        /// </summary>
        /// <param name="client">Client instance</param>
        internal void DefineClient(TcpClient client) => this._client = client;

        /// <summary>
        /// Send a 3XX redirect type request to the client.
        /// </summary>
        /// <param name="location">The url the client will redirect to.</param>
        /// <remarks>Ex: https://youtube.com</remarks>
        public async void Redirect(string location, HttpStatusCode code = HttpStatusCode.Redirect)
        {
            if ((int)code < 300 || (int)code > 308)
            {
                throw new ArgumentException("The code must be a valid redirect code. See http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.3.2");
            }
            base.StatusCode = HttpStatusCode.Found;
            base.Headers.Add("Location", location);
            base.Content = base.ToString();

            var dataToSend = base.ToBytes();
            // include buffer size support.
            if (!BackedServer.Instance.Config.DynamicBuffers)
            {
                int totalSent = 0;
                int dataLeft = dataToSend.Length - totalSent;

            writeData:
                await this._client.GetStream().WriteAsync(dataToSend, totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer);
                totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                if (dataLeft != 0)
                    goto writeData;

                if (base.Headers.ContainsKey("Connection"))
                    if (base.Headers["Connection"] != "keep-alive")
                        await this._client.GetStream().DisposeAsync();

                this.Dispose();
                return;
            }
            // write data to client
            await this._client.GetStream().WriteAsync(dataToSend);
            if (base.Headers.ContainsKey("Connection"))
                if (base.Headers["Connection"] != "keep-alive")
                    await this._client.GetStream().DisposeAsync();
            this.Dispose();
        }

        /// <summary>
        /// Send a file to the client, does include HTTP headers.
        /// </summary>
        /// <param name="fromBaseDirectory">Should the function look for the file in the defined root directory?</param>
        /// <param name="path">Location of the file.</param>
        public async void SendFile(bool fromBaseDirectory, string path = "")
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
                    await this._client.GetStream().WriteAsync(dataToSend, totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer);
                    totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                    if (dataLeft != 0)
                        goto writeData;

                    if (base.Headers.ContainsKey("Connection"))
                        if (base.Headers["Connection"] != "keep-alive")
                            await this._client.GetStream().DisposeAsync(); this.Dispose();
                    return;
                }
                // write data to client
                await this._client.GetStream().WriteAsync(dataToSend);
                if (base.Headers.ContainsKey("Connection"))
                    if (base.Headers["Connection"] != "keep-alive")
                        await this._client.GetStream().DisposeAsync(); this.Dispose();
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
                data = File.ReadAllBytes(fromBaseDirectory == true ? BackedServer.Instance.Config.RootDirectory + "/" + path : path);
                success = true;
            }
            else
            {
                base.Content = "Request resource not found.";
                base.StatusCode = HttpStatusCode.NotFound;
            }
            try
            {
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
                            await this._client.GetStream().WriteAsync(dataToSend, totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer);
                            totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                            if (dataLeft != 0)
                                goto writeData;
                            if (base.Headers.ContainsKey("Connection"))
                                if (base.Headers["Connection"] != "keep-alive")
                                    await this._client.GetStream().DisposeAsync();
                            this.Dispose();
                            return;
                        }
                        // write data to client
                        await this._client.GetStream().WriteAsync(dataToSend);
                        if (base.Headers.ContainsKey("Connection"))
                            if (base.Headers["Connection"] != "keep-alive")
                                await this._client.GetStream().DisposeAsync();
                        this.Dispose();
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
                    int totalSent = 0;

                    int dataLeft = data.Length;
                writeData:
                    dataLeft = data.Length - totalSent;
                    try
                    {
                        await this._client.GetStream().WriteAsync(data, totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer);
                    }
                    catch (IOException)
                    {
                        Console.WriteLine("Failed to finish writing to the client.");
                    }
                    totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                    if (dataLeft != 0)
                        goto writeData;
                    if (base.Headers.ContainsKey("Connection"))
                        if (base.Headers["Connection"] != "keep-alive")
                            await this._client.GetStream().DisposeAsync();
                    return;
                }

                // send the data to the client
                try
                {
                    await this._client.GetStream().WriteAsync(data);
                }
                catch (IOException)
                {
                    Logger.Log(Logger.LogLevel.Error, "Failed to finish writing to the client.");
                }
                if (base.Headers.ContainsKey("Connection"))
                    if (base.Headers["Connection"] != "keep-alive")
                        await this._client.GetStream().DisposeAsync();
                this.Dispose();
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
        /// A pre-written function to help send a 404 response to the client.
        /// </summary>
        /// <exception cref="Exception">Thrown when the client has already been sent a response or the client's stream is invalid.</exception>
        public async void SendNotFound()
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
                    await this._client.GetStream().WriteAsync(dataToSend, totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer);
                    totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                    if (dataLeft != 0)
                        goto writeData;

                    if (base.Headers.ContainsKey("Connection"))
                        if (base.Headers["Connection"] != "keep-alive")
                            await this._client.GetStream().DisposeAsync();

                    this.Dispose();
                    return;
                }
                // write data to client
                await this._client.GetStream().WriteAsync(dataToSend);
                if (base.Headers.ContainsKey("Connection"))
                    if (base.Headers["Connection"] != "keep-alive")
                        await this._client.GetStream().DisposeAsync();

                this.Dispose();
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
        public async void FinishRequest()
        {
            var dataToSend = base.ToBytes();
            // include buffer size support.
            if (!BackedServer.Instance.Config.DynamicBuffers)
            {
                int totalSent = 0;
                int dataLeft = dataToSend.Length;
            writeData:
                dataLeft = dataToSend.Length - totalSent;
                await this._client.GetStream().WriteAsync(dataToSend, totalSent, dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer);
                totalSent += dataLeft < BackedServer.Instance.Config.WriteBuffer ? dataLeft : BackedServer.Instance.Config.WriteBuffer;
                if (dataLeft != 0)
                    goto writeData;
                if (base.Headers.ContainsKey("Connection"))
                    if (base.Headers["Connection"] != "keep-alive")
                        await this._client.GetStream().DisposeAsync();
                this.Dispose();
            }
            else
            {
                // write data to client
                await this._client.GetStream().WriteAsync(dataToSend);
                if (base.Headers.ContainsKey("Connection"))
                    if (base.Headers["Connection"] != "keep-alive")
                        await this._client.GetStream().DisposeAsync();
                this.Dispose();
            }
            GC.Collect();
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

        public async void Dispose()
        {
            Logger.Log(Logger.LogLevel.Debug, $"Request completed, there are currently: {_instanceCount} requests in progress and there are {_instanceCount - 1} zombie requests.");

            _instanceCount--;

            await this._client.GetStream().DisposeAsync();
            this._client.Dispose();
            GC.Collect();
            GC.SuppressFinalize(this);
            GC.WaitForPendingFinalizers();
        }
    }
}
