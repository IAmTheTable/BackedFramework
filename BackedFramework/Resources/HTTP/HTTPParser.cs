/*********************************************
 * Writer: Aiden Sanchez                     *
 *                                           *
 ******************************************* *
 * Date: 4/18/22                             *
 *                                           *
 ******************************************* *
 * Last Modified: 4/18/22 at 10:28AM         *
 *                                           *
 ******************************************* *
 * Purpose: A dual purpose class to parse/...*
 * - create HTTP packets. Can parse formdata *
 * - and x-www-form-urlencoded bodies as of  *
 * - now.                                    *
 *                                           *
 ******************************************* *
 * Description: The client request class     *
 * - provides me ease of use for converting  *
 * - and even parsing the HTTP packet.       *
 *********************************************/

using System.Text;

namespace BackedFramework.Resources.HTTP
{
    internal class HTTPParser : IDisposable
    {
        internal HTTPParser() { }
        internal HTTPParser(string Input)
        {
            // remove all /r Format from Input
            Input = Input.Replace("\r", "");
            // convert each line as a "header"
            var lines = Input.Split('\n');
            Method = lines[0].Split(' ')[0]; // extract the HTTP methods
            Url = lines[0].Split(' ')[1]; // extract the url 
            Version = lines[0].Split(' ')[2]; // extract the HTTP version

            var headers = lines.Skip(1).ToList(); // skip the first line

            // flags for reading form based requests
            bool formDataMode = false;
            bool waitForFormData = false;
            bool dataMode = false;
            bool formUrlEnc = false;
            // used for temporarily storing a form Key name
            string formKeyName = "";
            // current index of headers
            int i = 0;
            foreach (var header in headers)
            {
                i++;
                // check if the line is '("")' (empty)
                if (string.IsNullOrEmpty(header))
                    continue;
                // extract form data from the request, using the boundary specified in the content-type header
                if (!string.IsNullOrEmpty(this.Boundary) && dataMode)
                {
                    if (header == this.Boundary)
                    {
                        formDataMode = true;
                        continue;
                    }
                    else if (header == this.Boundary + "--")
                    {
                        formDataMode = false;
                        continue;
                    }

                    if (waitForFormData)
                    {
                        FormData.Add(formKeyName, header);
                        waitForFormData = false;
                        continue;
                    }

                    var _key = header.Contains(':') ? header.Split(':')[0] : header;
                    var _value = header.Contains(':') ? header.Split(':')[1][1..] : "";
                    if (_key == "Content-Disposition" && !waitForFormData)
                    {
                        if (_value.Split(";")[0] == "form-data")
                        {
                            formKeyName = _value.Split("=")[1];
                            waitForFormData = true;
                        }
                    }
                }
                // extract url form encoded parameters
                if (!header.Contains(':') && formUrlEnc)
                {
                    var formValues = header.Split('&');
                    foreach (var formValue in formValues)
                    {
                        var _key = formValue.Split('=')[0];
                        var _value = formValue.Split('=')[1][1..];
                        FormData.Add(_key, _value);
                    }
                    continue;
                }
                // extract headers
                if (!formDataMode && !dataMode)
                {
                    var key = header.Split(':')[0]; // header name
                    var value = header.Split(':')[1][1..]; // header value
                    // Content-Type: application/html
                    // key^            Value^

                    // compare content type or content length, each useful for form data replies and whatnot
                    if (key == "Content-Type")
                    {
                        if (value.StartsWith(" multipart/form-data"))
                            this.Boundary = value.Split('=')[1]; // extract the boundary from the HTTP request
                        else if (value.StartsWith(" application/x-www-form-urlencoded"))
                            formUrlEnc = true; // enable form url encoded mode
                    }
                    else if (key == "Content-Length")
                        dataMode = true;

                    Headers.Add(key, value);
                }
            }
        }
        /// <summary>
        /// boundary usage for multipart/form-data request types
        /// </summary>
        public string Boundary { get; set; }
        /// <summary>
        /// key values for any form based request 
        /// </summary>
        public Dictionary<string, string> FormData { get; set; } = new();
        /// <summary>
        /// any headers used in the request, can be request parameters too
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new();

        /// <summary>
        /// The time of the request being sent or recieved by the server
        /// </summary>
        public DateTime TimeOfReq { get; } = DateTime.Now;

        /// <summary>
        /// HTTP method of the request
        /// </summary>
        public string Method { get; set; } = "";
        /// <summary>
        /// the url of the request, not including the host; Ex: /home
        /// </summary>
        public string Url { get; set; } = "";
        /// <summary>
        /// HTTP version of the request
        /// </summary>
        public string Version { get; set; } = "HTTP/1.1";
        /// <summary>
        /// HTTP Status code for the request response
        /// </summary>
        public string Code { get; set; } = "200";
        /// <summary>
        /// HTTP Status message for the request response
        /// </summary>
        public string Message { get; set; } = "OK";
        /// <summary>
        /// Actual body of the request, like the HTML page content
        /// </summary>
        public string Content { get; set; } = "";
        /// <summary>
        /// Converts all the request data to a string that is valid for an HTTP request
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(Version + " " + Code + " " + Message);
            foreach (var header in Headers)
            {
                sb.AppendLine(header.Key + ":" + header.Value);
            }
            sb.AppendLine("\r\n" + Content);
            return sb.ToString();
        }
        /// <summary>
        /// Convert the packet to a byte array
        /// </summary>
        /// <returns>An array of bytes of the packet.</returns>
        public byte[] ToBytes() => Encoding.UTF8.GetBytes(this.ToString());

        /// <summary>
        /// Free up memory
        /// </summary>
        public void Dispose()
        {
            this.Headers.Clear();

            this.Headers = null;
            this.Content = null;
            this.Message = null;
            this.Method = null;
            this.Url = null;
            this.Code = null;

            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}
