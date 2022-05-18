using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Resources.HTTP
{
    /// <summary>
    /// Http method types
    /// </summary>
    public enum HTTPMethods
    {
        /// <summary>
        /// The HTTP POST method sends data to the server. The type of the body of the request is indicated by the Content-Type header.
        /// </summary>
        GET,
        /// <summary>
        /// The HTTP HEAD method requests the headers that would be returned if the HEAD request's URL was instead requested with the HTTP GET method. For example, if a URL might produce a large download, a HEAD request could read its Content-Length header to check the filesize without actually downloading the file.
        /// </summary>
        HEAD,
        /// <summary>
        /// The HTTP POST method sends data to the server. The type of the body of the request is indicated by the Content-Type header.
        /// </summary>
        POST,
        /// <summary>
        /// The HTTP PUT request method creates a new resource or replaces a representation of the target resource with the request payload.
        /// </summary>
        PUT,
        /// <summary>
        /// The HTTP DELETE request method deletes the specified resource.
        /// </summary>
        DELETE,
        /// <summary>
        /// The HTTP CONNECT method starts two-way communications with the requested resource. It can be used to open a tunnel.
        /// </summary>
        CONNECT,
        /// <summary>
        /// The HTTP OPTIONS method requests permitted communication options for a given URL or server. A client can specify a URL with this method, or an asterisk (*) to refer to the entire server.
        /// </summary>
        OPTIONS,
        /// <summary>
        /// The HTTP TRACE method performs a message loop-back test along the path to the target resource, providing a useful debugging mechanism.
        /// </summary>
        TRACE,
        /// <summary>
        /// The HTTP PATCH request method applies partial modifications to a resource.
        /// </summary>
        PATCH
    }
}
