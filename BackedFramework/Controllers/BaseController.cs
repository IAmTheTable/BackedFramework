using BackedFramework.Resources.HTTP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Controllers
{
    /// <summary>
    /// A class to represent a controller, useful for routing requests to the correct controller.
    /// </summary>
    public partial class BaseController : IDisposable
    {
        /// <summary>
        /// An instance of a response context object.
        /// </summary>
        /// <seealso cref="ResponseContext"/>
        public ResponseContext Response { get; internal set; }
        /// <summary>
        /// An instance of the request context the client sent.
        /// </summary>
        /// <seealso cref="RequestContext"/>
        public RequestContext Request { get; internal set; }

        public void Dispose()
        {
            Console.WriteLine("Base controller has been disposed.");
            GC.Collect();
        }
    }
}
