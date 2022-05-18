using BackedFramework.Resources.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.HTTP
{
    internal class RequestBase : IDisposable
    {
        private readonly HTTPParser _parser;
        internal RequestBase(HTTPParser parser)
        {
            this._parser = parser;
        }

        /// <summary>
        /// The method of the request.
        /// </summary>
        public HTTPMethods Method
        {
            get
            {
                return Enum.Parse<HTTPMethods>(this._parser.Method);
            }
        }
        /// <summary>
        /// The path of the request.
        /// </summary>
        public string Path
        {
            get => this._parser.Url;
        }
        
        /// <summary>
        /// Get the value of a header.
        /// </summary>
        /// <param name="header">The name of the header.</param>
        /// <returns>The value of the named header specified.</returns>
        public string GetHeader(string header) => this._parser.Headers[header];

        /// <summary>
        /// Obtain the dictionary collection of key/value pairs of the headers.
        /// </summary>
        /// <returns>The dictionary pair of headers</returns>
        public Dictionary<string, string> Headers
        {
            get => this._parser.Headers;
        }

        /// <summary>
        /// Obtain the form body of a request.
        /// </summary>
        public Dictionary<string, string> FormData
        {
            get
            {
                return this._parser.FormData;
            }
        }

        /// <summary>
        /// Obtain the value of the form body.
        /// </summary>
        /// <param name="key">Key of the requested value.</param>
        /// <returns>The value of the requested key.</returns>
        public string GetFormValue(string key) => this._parser.FormData[key];

        /// <summary>
        /// Returns the time of the request, most likely innaccurate because the value is set when constructing.
        /// </summary>
        public DateTime TimeOfRequest
        {
            get => this._parser.TimeOfReq;
        }

        public void Dispose()
        {
            Logger.LogInt(Logger.LogLevel.Debug, "Disposing Request Base");

            GC.Collect();
            GC.SuppressFinalize(this);            
        }
    }
}
