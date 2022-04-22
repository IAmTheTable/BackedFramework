using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackedFramework.Resources.HTTP;

namespace BackedFramework.Api.Routing
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RouteAttribute : Attribute
    {
        internal static Dictionary<string, HTTPMethods[]> registeredRoutes = new();

        /// <summary>
        /// The route directory to match
        /// </summary>
        public string Route { get; private set; }
        /// <summary>
        /// The HTTP method the client needs to send for the route to be valid.
        /// </summary>
        public HTTPMethods HTTPMethod { get; private set; }
        
        public RouteAttribute(string targetRoute, HTTPMethods method)
        {
            this.Route = targetRoute;
            this.HTTPMethod = method;

            if(!registeredRoutes.ContainsKey(targetRoute))
                registeredRoutes.Add(targetRoute, new[] { method });
            else
            {
                if(registeredRoutes[targetRoute].Contains(method))
                    throw new Exception($"The route {targetRoute} has already been registered with the method {method}");
                else
                    registeredRoutes[targetRoute] = registeredRoutes[targetRoute].Concat(new[] { method }).ToArray();
            }
        }
    }
}
