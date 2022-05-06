using BackedFramework.Resources.HTTP;
using BackedFramework.Resources.Logging;
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
        public BaseController()
        {
            Logger.Log(Logger.LogLevel.Debug, "Base Controller Constructed :)");
            ResponseContext.OnRequestFinished += OnRequestFinished;
        }

        private void OnRequestFinished()
        {
            
        }

        /// <summary>
        /// An instance of a response context object.
        /// </summary>
        /// <seealso cref="ResponseBase"/>
        public ResponseContext Response { get; internal set; }
        
        /// <summary>
        /// An instance of the request context the client sent.
        /// </summary>
        /// <seealso cref="RequestBase"/>
        public RequestContext Request { get; internal set; }

        public void Dispose()
        {
            Console.WriteLine("Base controller has been disposed.");
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}
