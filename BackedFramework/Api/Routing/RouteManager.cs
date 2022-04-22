using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using BackedFramework.Controllers;
using BackedFramework.Resources.HTTP;

namespace BackedFramework.Api.Routing
{
    // make sure this is an internal class
    internal class RouteManager : IDisposable
    {
        internal static RouteManager s_instance;
        /// <summary>
        /// A dictionary of all specified routes.
        /// </summary>
        private Dictionary<RouteAttribute, Tuple<Type, MethodInfo>> _functionRoutes = new();

        /// <summary>
        /// Default constructor for managing routes, will automatically scan the assembly for routes.
        /// </summary>
        internal RouteManager()
        {
            if (s_instance is null)
                throw new Exception("Only one instance of RouteManager can be created, this could be a bug or you could be trying to create more than one instance.");

            // get the base application assembly
            var appBase = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.IsSubclassOf(typeof(BaseController)));
            // iterate through ALL the modules.
            foreach (var mod in appBase)
            {
                // get the methods from the module
                var extractedMethods = mod.GetMethods();
                // iterate through the methods that contain our route attribute
                foreach (var func in extractedMethods.Where(x => x.GetCustomAttribute<RouteAttribute>() != null))
                    _functionRoutes.Add(func.GetCustomAttribute<RouteAttribute>(), Tuple.Create(mod, func)); // add the route to the dictionary
            }

            // set the static instance
            s_instance = this;
        }


        private bool TryExecuteRoute(string path, HTTPMethods method)
        {
            if (RouteAttribute.registeredRoutes[path] is not null)
            {
                // try and get the route from the function route directory.
                var routeResult = _functionRoutes.Keys.Where(x => x.Route == path).ToList();
                if (routeResult is null)
                    return false;

                // if the route with code is not found just return false(failure)
                RouteAttribute route;
                if ((route = routeResult.Find(x => x.HTTPMethod == method && x.Route == path)) is null)
                    return false;

                // the function that the route will use
                var targetRouteFunc = _functionRoutes[route];
                var targetRouteClass = targetRouteFunc.Item1;

                var targetRouteClassInstance = targetRouteClass.GetMethod(".ctr").Invoke(null, new object[] { });

                var responseField = targetRouteClass.GetField("Response");
                var responseFieldInstance = responseField.


                //todo: set the request context.

                _functionRoutes[route].Invoke(null, new object[] { });
            }
        }

        /// <summary>
        /// Only implementing to help free resources from inital instantiation.
        /// </summary>
        void IDisposable.Dispose()
        {
            GC.Collect();
        }
    }
}
