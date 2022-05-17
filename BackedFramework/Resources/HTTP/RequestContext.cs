using BackedFramework.Resources.Logging;
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
    public class RequestContext : IDisposable
    {
        private readonly HTTPParser _parser;
        internal RequestContext(HTTPParser parser)
        {
            this._parser = parser;
        }
        /// <summary>
        /// If the request contains url parameters, this will return true, if not false.
        /// </summary>
        public bool IsQueried
        {
            get
            {
                return this._parser.IsQueried;
            }
        }

        /// <summary>
        /// Bytes of the post body, will not always be set...
        /// </summary>
        public byte[] PostData
        {
            get
            {
                return this._parser.PostData;
            }
        }

        /// <summary>
        /// The query parameters in the request.
        /// </summary>
        public Dictionary<string, string> QueryParameters
        {
            get
            {
                return this._parser.QueryParameters;
            }
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

        public void Dispose()
        {
            Logger.Log(Logger.LogLevel.Debug, "Disposing Request Context");

            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}
