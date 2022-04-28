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
    public class ResponseContext : ResponseBase
    {
        private TcpClient _client;
        internal ResponseContext() : base(new())
        {
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
        public void Redirect(string location, HttpStatusCode code = HttpStatusCode.Redirect)
        {
            if ((int)code < 300 || (int)code > 308)
            {
                throw new ArgumentException("The code must be a valid redirect code. See http://www.w3.org/Protocols/rfc2616/rfc2616-sec10.html#sec10.3.2");
            }
            base.StatusCode = HttpStatusCode.Found;
            base.Headers.Add("Location", location);
            base.Content = base.ToString();
            this._client.GetStream().Write(base.ToBytes());
            this._client.GetStream().Dispose();
        }

        /// <summary>
        /// Send a file to the client.
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
                this._client.GetStream().Write(base.ToBytes());
                this._client.GetStream().Dispose();
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
        public void SendNotFound()
        {
            base.Content = "Request resource not found.";
            base.StatusCode = HttpStatusCode.NotFound;
            try
            {
                this._client.GetStream().Write(base.ToBytes());
                this._client.GetStream().Dispose();
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
        public void Finalize()
        {
            this._client.GetStream().Write(base.ToBytes());
            this._client.GetStream().Dispose();

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
    }
}
