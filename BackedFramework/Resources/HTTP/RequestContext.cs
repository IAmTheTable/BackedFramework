using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.HTTP
{
    /// <summary>
    /// A class that helps with reading and writing HTTP requests.
    /// </summary>
    public class RequestContext
    {
        private readonly HTTPParser _parser;
        internal RequestContext(HTTPParser parser)
        {
            this._parser = parser;
        }
        /// <summary>
        /// Path of the request, excluding the FQDN, "/static/404.html" for exmaple.
        /// </summary>
        public string Path
        {
            get
            {
                return this._parser.Url;
            }
        }
        
        /// <summary>
        /// The HTTP method used for the request.
        /// </summary>
        public HTTPMethods Method
        {
            get
            {
                return Enum.Parse<HTTPMethods>(this._parser.Method);
            }
        }

        public Dictionary<string, string> FormData
        {
            get
            {
                return this._parser.FormData;
            }
        }

        /// <summary>
        /// All headers sent from the client in the request.
        /// </summary>
        public Dictionary<string, string> RequestHeaders
        {
            get
            {
                return this._parser.Headers;
            }
        }
    }
}
