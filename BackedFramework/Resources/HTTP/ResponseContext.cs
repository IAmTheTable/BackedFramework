using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.HTTP
{
    public class ResponseContext : ResponseBase
    {
        internal ResponseContext(HTTPParser parser) : base(parser)
        {
        }

        /// <summary>
        /// Send a 302(Found) redirect request to the client.
        /// </summary>
        /// <param name="location">The url the client will redirect to.</param>
        /// <remarks>Ex: https://youtube.com</remarks>
        public void Redirect(string location, HttpStatusCode code)
        {
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
