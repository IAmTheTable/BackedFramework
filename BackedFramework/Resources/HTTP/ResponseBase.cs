namespace BackedFramework.Resources.HTTP
{
    public class ResponseBase
    {
        private readonly HTTPParser _parser;
        internal ResponseBase(HTTPParser parser) { this._parser = parser; }

        /// <summary>
        /// The HTTP status code(number) that the browser will recieve.
        /// </summary>
        public HttpStatusCode StatusCode
        {
            get => Enum.Parse<HttpStatusCode>(this._parser.Code);
            set => this._parser.Code = value.ToString();
        }

        /// <summary>
        /// The actual response body that the browser will display.
        /// </summary>
        public string Content
        {
            get => _parser.Content;
            set => _parser.Content = value;
        }

        public Dictionary<string, string> Headers
        {
            get => _parser.Headers;
            set => _parser.Headers = value;
        }
    }
}