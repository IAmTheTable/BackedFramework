namespace BackedFramework.Resources.HTTP
{
    public class ResponseBase
    {
        private HTTPParser _parser;
        internal ResponseBase(HTTPParser parser) { this._parser = parser; }
        /// <summary>
        /// The HTTP status code that the browser will recieve
        /// </summary>
        public HttpStatusCode Method
        {
            get => Enum.Parse<HttpStatusCode>(_parser.Method);
            set => _parser.Method = value.ToString();
        }
        /// <summary>
        /// The HTTP status code(number) that the browser will recieve.
        /// </summary>
        public int StatusCode
        {
            get => (int)this.Method;
            set => _parser.Code = value.ToString();
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