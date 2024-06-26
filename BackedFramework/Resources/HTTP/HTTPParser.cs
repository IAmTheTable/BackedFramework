﻿/*********************************************
 * Writer: Aiden Sanchez                     *
 *                                           *
 ******************************************* *
 * Date: 4/18/22                             *
 *                                           *
 ******************************************* *
 * Last Modified: 5/06/22 at 10:04AM         *
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

using BackedFramework.Resources.Logging;
using System.Text;

namespace BackedFramework.Resources.HTTP
{
    /// <summary>
    /// Parses and creates HTTP packets.
    /// </summary>
    /// <remarks>The backbone of the entire project, without this class, nothing would work.</remarks>
    internal class HTTPParser : IDisposable
    {
        internal HTTPParser() { }
        internal HTTPParser(string Input)
        {
            if (string.IsNullOrWhiteSpace(Input))
            {
                Logger.LogInt(Logger.LogLevel.Error, "Failed parse HTTP packet, input is null or empty.");
                return;
            }

            // remove all /r Format from Input
            Input = Input.Replace("\r", "");
            // convert each line as a "header"
            var lines = Input.Split('\n');
            this.Method = lines[0].Split(' ')[0]; // extract the HTTP methods
            this.Url = System.Web.HttpUtility.UrlDecode(lines[0].Split(' ')[1]);  // extract the url 
            this.Version = lines[0].Split(' ')[2]; // extract the HTTP version


            // extract query parameters if they exist
            if (this.Url.Contains('?') && this.Url.IndexOf('?') < this.Url.Length - 1)
            {
                this.IsQueried = true;

                // split the queried string...
                var query = Url.Split('?')[1];
                var query_params = query.Split('&');

                // iterate the queried strings...
                foreach (var param in query_params)
                {
                    // retrieve the key value pairs

                    if (param.Split('=').Length < 2)
                    {
                        this.QueryParameters.Add(param.Split('=')[0], "");
                        continue;
                    }
                    // get the key value pairs
                    var key = param.Split('=')[0];
                    var value = param.Split('=')[1];

                    // add the values to the query parameters dictionary
                    QueryParameters.Add(key, value);
                }

                // fix the url because it still contians the url parameters
                this.Url = Url.Split('?')[0];
            }

            var headers = lines.Skip(1).ToList(); // skip the first line

            // parse the headers per method

            if (this.Method == "GET")
            {
                headers.RemoveAll(x => x == "");// clear null entries
                foreach (var header in headers)
                    this.Headers.Add(header.Split(": ")[0], header.Split(": ")[1]);
            }
            else if (this.Method == "POST")
            {
                if (!RequireHeader(headers, "Content-Length"))
                {
                    Logger.LogInt(Logger.LogLevel.Error, "Failed parse HTTP packet, missing Content-Length header.");
                    return;
                }

                if (!RequireHeader(headers, "Content-Type"))
                {
                    Logger.LogInt(Logger.LogLevel.Error, "Failed parse HTTP packet, missing Content-Type header.");
                    return;
                }

                var contentType = headers.Find(x => x.ToLower().Split(':')[0] == "content-type").Split(": ")[1].ToLower();

                if (contentType == "application/x-www-form-urlencoded")
                {
                    ParseUrlEncodedBody(headers);
                    return;
                }
                else if (contentType.Contains("multipart/form-data"))
                {
                    ParseMultipartBody(headers);
                    return;
                }
                else
                {
                    ParseNormalBody(headers);
                }
            }
        }

        private void ParseNormalBody(List<string> headers)
        {
            int i = 0;
            // read headers until it reaches the "null" header, which is usually the line before the body.
            foreach (var header in headers)
            {
                i++;
                if (header == "")
                    break;

                var splitHeader = header.Split(": ");
                this.Headers.Add(splitHeader[0], splitHeader[1]);
            }

            // append the body from the end of the headers.
            headers.Skip(i).ToList().ForEach(x =>
            {
                this.Body += (x == "" ? "" : x + '\n');
            });

            if (this.Body != "")
            {
                this.Body = this.Body[0..^1];
                int lineCount = this.Body.Split('\n').Length - 1;
                if ((this.Body.Length + lineCount).ToString() != this.Headers["Content-Length"])
                {
                    Logger.LogInt(Logger.LogLevel.Info, "Body length does not match header length.");
                }
            }
        }


        /// <summary>
        /// Wrapper for decoding HTML encoded strings.
        /// </summary>
        /// <param name="input">Input string to decode</param>
        /// <returns>The HTML decoded string.</returns>
        private static string HtmlDecode(string input) => System.Net.WebUtility.UrlDecode(input);

        /// <summary>
        /// parse multipart form data post requests via remaining headers.
        /// </summary>
        /// <param name="headers">List of headers to use for parsing...</param>
        private void ParseMultipartBody(List<string> headers)
        {
            int i = 0;

            var cleanHeaders = headers.TakeWhile(x => x.ToLower().Split(':')[0] != "content-type").ToList();
            cleanHeaders.ForEach(x =>
            {
                i++;
                var header = x.Split(": "); // split the header to get the key and value
                this.Headers.Add(header[0], header[1]);
            });

            // retrieve all the non used headers
            var remainingHeaders = headers.Skip(i).ToList();

            // parse the boundary out of the content type header
            var content = remainingHeaders[0].Split(": ");
            var boundary = content[1].Split("; ");

            // add the content-type and boundary header
            this.Headers.Add(content[0], boundary[0]);
            this.Boundary = "--" + boundary[1].Split('=')[1];


            // add content length
            var contentLength = remainingHeaders[1].Split(": ");
            this.Headers.Add(contentLength[0], contentLength[1]);

            remainingHeaders = remainingHeaders.Skip(3).ToList();

            bool end = false; // is the end of the current section
            bool data = false;
            string sectionData = "";

            remainingHeaders.ForEach(x =>
            {
                // if its just the beginning of parsing.
                if ((x != this.Boundary && x != this.Boundary + "--") && !end) // not boundary and not end
                {
                    data = true;
                    sectionData += (x == "" ? "" : x);
                }
                else if ((x == this.Boundary || x == this.Boundary + "--") && data) // boundary and (not end and data) - was able to read data
                {
                    data = !data;
                    Logger.LogInt(Logger.LogLevel.Debug, sectionData);

                    // literal result 

                    /* Content-Disposition: form-data; name="firstName"

                        nameDataHere
                    */

                    var dataName = sectionData.Split("name=\"")[1].Split('"')[0];
                    var dataResult = sectionData.Split("name=\"")[1].Split('"')[1];

                    var result = HtmlDecode(dataResult);

                    this.FormData.Add(dataName, result);

                    sectionData = "";
                }
            });
        }

        /// <summary>
        /// Parse url encoded form data post requests via remaining headers.
        /// </summary>
        /// <param name="headers">The remaining headers to use when parsing.</param>
        private void ParseUrlEncodedBody(List<string> headers)
        {
            int i = 0;
            // read headers until it reaches the "null" header, which is usually the line before the body.
            foreach (var header in headers)
            {
                i++;

                if (header == "")
                    break;

                var splitHeader = header.Split(": ");
                this.Headers.Add(splitHeader[0], splitHeader[1]);
            }

            var contentBody = string.Join("", headers.Skip(i));
            var instances = contentBody.Split('&');
            instances.ToList().ForEach(x =>
            {
                var y = x.Split('='); // split the header to get the key and value

                var res = HtmlDecode(y[1]);

                this.FormData.Add(y[0], res); // add the key values to the form data dictionary
            });
        }

        /// <summary>
        /// Function that checks if a header exists rather than having to write this multiple times.
        /// </summary>
        /// <param name="headers">A list of headers.</param>
        /// <param name="header">The header to check for, not case sensitive.</param>
        private bool RequireHeader(IEnumerable<string> headers, string header) => headers.Any(x => x.ToLower().Split(':')[0] == header.ToLower());

        /// <summary>
        /// If the request has query parameters, this is true...
        /// </summary>
        public bool IsQueried { get; internal set; } = false;

        /// <summary>
        /// A key/value pair for query string parameters.
        /// </summary>
        public Dictionary<string, string> QueryParameters { get; internal set; } = new();

        /// <summary>
        /// Contains the raw value of POST requests as a string...
        /// </summary>
        public string Body { get; internal set; } = "";

        /// <summary>
        /// boundary usage for multipart/form-data request types
        /// </summary>
        public string Boundary { get; set; }
        /// <summary>
        /// key values for any form based request 
        /// </summary>
        public Dictionary<string, string> FormData { get; internal set; } = new();
        /// <summary>
        /// any headers used in the request, can be request parameters too
        /// </summary>
        public Dictionary<string, string> Headers { get; internal set; } = new();

        /// <summary>
        /// The time of the request being sent or recieved by the server
        /// </summary>
        public DateTime TimeOfReq { get; internal set; } = DateTime.Now;

        /// <summary>
        /// HTTP method of the request
        /// </summary>
        public string Method { get; internal set; } = "";
        /// <summary>
        /// the url of the request, not including the host; Ex: /home
        /// </summary>
        public string Url { get; internal set; } = "";
        /// <summary>
        /// HTTP version of the request
        /// </summary>
        public string Version { get; internal set; } = "HTTP/1.1";
        /// <summary>
        /// HTTP Status code for the request response
        /// </summary>
        public string Code { get; internal set; } = "200";
        /// <summary>
        /// HTTP Status message for the request response
        /// </summary>
        public string Message { get; internal set; } = "OK";
        /// <summary>
        /// Actual body of the request, like the HTML page content
        /// </summary>
        public string Content { get; internal set; } = "";

        /// <summary>
        /// Data body of the post request in bytes, will not always be set...
        /// </summary>
        public byte[] PostData { get; internal set; }

        /// <summary>
        /// Converts all the request data to a string that is valid for an HTTP request
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{Version} {Code} {Message}\r\n");
            foreach (var header in Headers)
            {
                sb.Append(header.Key + ": " + header.Value + "\r\n");
            }
            sb.Append("\r\n" + Content + "\r\n\r\n");
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

            Logger.LogInt(Logger.LogLevel.Debug, "Disposing HTTP Parser");

            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}
