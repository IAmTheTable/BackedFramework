using BackedFramework.Resources.Logging;
using BackedFramework.Server;

using TcpClient = BackedFramework.Resources.Extensions.TcpClient;
namespace BackedFramework.Resources.HTTP
{
    /// <summary>
    /// A class that represents an HTTP response, but also provides simple yet helpful methods and properties to help build your response easier.
    /// </summary>
    public class ResponseContext : ResponseBase, IDisposable
    {
        public static event Action OnRequestFinished;
        private static int _instanceCount = 0;
        private readonly CancellationToken _cancellationToken = new();
        private RequestContext _requestContext;

        private TcpClient _client;

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

        /// <summary>
        /// The request context that this response is associated with.
        /// </summary>
        /// <param name="ctx">Context that will be used with the request...</param>
        /// <remarks>Only used for future use, if any...</remarks>
        internal void DefineRequestContext(RequestContext ctx) => this._requestContext = ctx;

        /// <summary>
        /// A function to set the client, easier than constructing... might change later...
        /// </summary>
        /// <param name="client">Client instance</param>
        internal void DefineClient(TcpClient clientIp) => this._client = clientIp;

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


            // write data to client
            this._client.WriteAsync(base.ToBytes(), cancellationToken: _cancellationToken);
        }

        /// <summary>
        /// Write data to the client.
        /// </summary>
        /// <param name="Text">The text to write. Can be any object as long as it converts to a string how YOU want.</param>
        public void Write(object Text) => base.Content = Text.ToString();

        /// <summary>
        /// Append text content to the response.
        /// </summary>
        /// <param name="Text">The text to append.</param>
        public void Append(object Text) => base.Content += Text.ToString();

        /// <summary>
        /// Remove all the data out of the content.
        /// </summary>
        public void Clear() => base.Content = "";

        /// <summary>
        /// Send a file to the client, includes the HTTP header.
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
                this.SendNotFound();
            }
            try
            {
                this.FinishRequest();
            }
            catch (InvalidOperationException)
            {
                throw new Exception("Attempted to finalize a request, but the request was already finalized.");
            }
        }

        /// <summary>
        /// Send a file to the client, only sends the raw file, no http header.
        /// </summary>
        /// <param name="fromBaseDirectory">Is the file location in the root directory, specified in the server config</param>
        /// <param name="path">The location of the file</param>
        /// <exception cref="Exception">Thrown if the server attempts to send a response to the client, but the client is unavailable to send data to.</exception>
        public void SendRawFile(bool fromBaseDirectory, string path = "")
        {
            byte[] data = Array.Empty<byte>();
            bool success = false;
            // validate that the file exists.
            if (File.Exists(fromBaseDirectory == true ? BackedServer.Instance.Config.RootDirectory + "/" + path : path))
            {
                // move all data into the buffer
                data = File.ReadAllBytes(fromBaseDirectory == true ? BackedServer.Instance.Config.RootDirectory + "/" + path : path);
                success = true;
            }
            else
            {
                this.SendNotFound();
            }
            
            // if the file we tried to read was not found...
            if (!success)
            {
                try
                {
                    // write data to client
                    this.FinishRequest();
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

            // send the data to the client
            try
            {
                this._client.WriteAsync(data);
            }
            catch (IOException)
            {
                Logger.Log(Logger.LogLevel.Error, "Failed to finish writing to the client.");
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
                // write data to client
                this.FinishRequest();
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
            // write data to client
            this._client.WriteAsync(base.ToBytes(), cancellationToken: _cancellationToken);
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
            if (OnRequestFinished is not null)
                OnRequestFinished.Invoke();
            
            Logger.Log(Logger.LogLevel.Info, $"The server sent a connection: {base.Headers["Connection"]} value.");
            Logger.Log(Logger.LogLevel.Debug, $"Request completed at {this._requestContext.Path}, there are currently: {_instanceCount} requests in progress and there are {_instanceCount - 1} zombie requests.");

            //ConnectionManager._instance.RemoveConnection(_requestContext.RequestHeaders["Host"]);

            _instanceCount--;

            base.Dispose();

            GC.Collect();
            GC.SuppressFinalize(this);
            GC.WaitForPendingFinalizers();
        }
    }
}
