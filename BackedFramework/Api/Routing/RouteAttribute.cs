using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackedFramework.Resources.HTTP;

namespace BackedFramework.Api.Routing
{
    /// <summary>
    /// A route attribute to work in conjunction with the <see cref="RouteManager"/> class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RouteAttribute : Attribute
    {
        internal static Dictionary<string, HTTPMethods[]> registeredRoutes = new()
        {
            ["/"] = new[] { HTTPMethods.GET },
        };


        /// <summary>
        /// The route directory to match
        /// </summary>
        public string Route { get; private set; }
        /// <summary>
        /// The HTTP method the client needs to send for the route to be valid.
        /// </summary>
        public HTTPMethods HTTPMethod { get; private set; }

        /// <summary>
        /// Construct a new route attribute defining at a url path and http method.
        /// </summary>
        /// <param name="targetRoute">The url that will be routed</param>
        /// <param name="method">The http method used to accept the request.</param>
        public RouteAttribute(string targetRoute, HTTPMethods method)
        {
            this.Route = targetRoute;
            this.HTTPMethod = method;

            if (!registeredRoutes.ContainsKey(targetRoute))
                registeredRoutes.Add(targetRoute, new[] { method });
            else
            {
                if (registeredRoutes[targetRoute].Contains(method))
                {
                    //Console.WriteLine($"The route {targetRoute} has already been registered with the method {method}");
                }
                else
                    registeredRoutes[targetRoute] = registeredRoutes[targetRoute].Concat(new[] { method }).ToArray();
            }
        }
    }
}
