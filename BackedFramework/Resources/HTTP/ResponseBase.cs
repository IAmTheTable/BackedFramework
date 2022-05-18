using BackedFramework.Resources.Logging;
namespace BackedFramework.Resources.HTTP
{
    /// <summary>
    /// A base class for HTTP server resources.
    /// </summary>
    public class ResponseBase : IDisposable
    {
        private readonly HTTPParser _parser;
        internal ResponseBase(HTTPParser parser) { this._parser = parser; }

        internal HTTPParser Parser { get { return this._parser; } }

        /// <summary>
        /// The HTTP status code(number) that the browser will recieve.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get => Enum.Parse<HttpStatusCode>(this._parser.Code);
            set
            {
                this._parser.Code = ((int)value).ToString();
                this._parser.Message = value.ToString();
            }
        }

        /// <summary>
        /// The actual response body that the browser will display.
        /// </summary>
        public string Content
        {
            get => this._parser.Content;
            set => this._parser.Content = value;
        }

        /// <summary>
        /// A list of headers that will be sent to the browser.
        /// </summary>
        public Dictionary<string, string> Headers
        {
            get => this._parser.Headers;
            set => this._parser.Headers = value;
        }

        /// <summary>
        /// Convert the response to a byte array.
        /// </summary>
        /// <returns>A byte array containing the data.</returns>
        public byte[] ToBytes() => _parser.ToBytes();
        /// <summary>
        /// Convert the response to a valid HTTP packet.
        /// </summary>
        /// <returns>A string representation of the HTTP packet.</returns>
        public override string ToString() => this._parser.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void Dispose()
        {
            Logger.LogInt(Logger.LogLevel.Debug, "Disposing Response Base");
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}