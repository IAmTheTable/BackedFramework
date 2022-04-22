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
        internal ResponseContext(HTTPParser parser) : base(parser)
        {
        }

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
            base.Headers.Add("location", location);
        }
        /// <summary>
        /// Write the response to the client.
        /// </summary>
        /// <param name="content">The string content to send.</param>
        public void Write(string content) => base.Content = content;

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
