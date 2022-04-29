using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Reflection;
using BackedFramework.Controllers;
using BackedFramework.Resources.HTTP;
using BackedFramework.Server;
using BackedFramework.Resources.Exceptions;

namespace BackedFramework.Api.Routing
{
    /// <summary>
    /// A managing type class to help direct the request to the right route context controller.
    /// </summary>
    internal class RouteManager : IDisposable
    {
        internal static RouteManager s_instance;
        /// <summary>
        /// A dictionary of all specified routes.
        /// </summary>
        private readonly Dictionary<RouteAttribute, Tuple<Type, MethodInfo[]>> _functionRoutes = new();

        /// <summary>
        /// Default constructor for managing routes, will automatically scan the assembly for routes.
        /// </summary>
        internal RouteManager()
        {
            if (s_instance is not null)
                throw new MultiInstanceException("Only one instance of RouteManager can be created, this could be a bug or you could be trying to create more than one instance.");

            // get the base application assembly
            var appBase = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x => x.IsSubclassOf(typeof(BaseController)));
            // iterate through ALL the modules.(classes)
            foreach (var mod in appBase)
            {
                if (mod.GetCustomAttribute<RouteAttribute>() is not null)
                {
                    // add support for class based routing...
                    foreach (var method in mod.GetMethods())
                    {
                        // make sure the function is user defined.
                        if (method.IsGenericMethod || method.IsSpecialName || method.DeclaringType != mod)
                            continue;

                        // if the route has already been registered, append the new methods to the existing route.
                        if (_functionRoutes.ContainsKey(mod.GetCustomAttribute<RouteAttribute>()))
                        {
                            // append the routes
                            _functionRoutes[mod.GetCustomAttribute<RouteAttribute>()] = new Tuple<Type, MethodInfo[]>(mod, _functionRoutes[mod.GetCustomAttribute<RouteAttribute>()].Item2.Concat(new[] { method }).ToArray());
                        }
                        else
                            _functionRoutes.Add(mod.GetCustomAttribute<RouteAttribute>(), new Tuple<Type, MethodInfo[]>(mod, new[] { method }));
                    }
                }
                else
                {
                    // get the methods from the module
                    var extractedMethods = mod.GetMethods();
                    // iterate through the methods that contain our route attribute
                    foreach (var func in extractedMethods.Where(x => x.GetCustomAttribute<RouteAttribute>() != null))
                        _functionRoutes.Add(func.GetCustomAttribute<RouteAttribute>(), Tuple.Create(mod, new[] { func })); // add the route to the dictionary
                }
            }

            // set the static instance
            s_instance = this;
        }


        internal bool TryExecuteRoute(HTTPParser parser, ResponseContext rspCtx, RequestContext reqCtx)
        {
            // define used values
            var fullpath = parser.Url;
            var path = string.Join("/", parser.Url.Split("/").Take(parser.Url.Split("/").Length - 1)) == "" ? parser.Url : string.Join("/", parser.Url.Split("/").Take(parser.Url.Split("/").Length - 1));
            var method = Enum.Parse<HTTPMethods>(parser.Method);
            var subpath = fullpath[path.Length..];

            if (RouteAttribute.registeredRoutes.ContainsKey(path))
            {
                // try and get the route from the function route directory.
                var routeResult = _functionRoutes.Keys.Where(x => x.HTTPMethod == method && x.Route == path).ToList();
                if (routeResult is null)
                {
                    rspCtx.SendNotFound();
                    return true; // invalid request, handle later.
                }

                // NOTES: Small bug that it wont actually like, find the proper path and method in relation to the function that is being called,
                // - so it will just return without closing the connection properly.
                // - to fix just, be better at finding the proper path and method. - or just be better :troll:


                // NOTES: I need to fix detecting proper routes, using a better method

                var route = routeResult.First();

                // the function that the route will use
                var targetRouteFunc = _functionRoutes[route];
                var targetRouteClass = targetRouteFunc.Item1;

                // create the controller
                var controller = Activator.CreateInstance(targetRouteClass) as BaseController;
                controller.Response = rspCtx;
                controller.Request = reqCtx;

                try
                {
                    // check if the route and path are the same, if so then return the index, if the index function is not present, then return 404.
                    if(fullpath == route.Route)
                    {
                        var hasIndex = targetRouteFunc.Item2.ToList().Any(x => x.Name.ToLower() == "index");
                        if(!hasIndex)
                        {
                            // send the 404
                            rspCtx.SendNotFound();
                            return true;
                        }
                        
                        // invoke the Index function for the client to recieve.
                        targetRouteClass.GetMethod("Index").Invoke(controller, Array.Empty<object>());
                        return true;
                    }
                    else
                    {
                        // substring the path from the route to get the function name.
                        var functionName = fullpath[(route.Route.Length + 1)..];

                        // check if we have a routed function, if not just run the default one, let it be known to prioritize the routed functions over the class implemented ones.
                        if (targetRouteFunc.Item2.ToList().Exists(x => x.CustomAttributes.Any() && x.GetCustomAttribute<RouteAttribute>().Route == $"/{functionName}"))
                        {
                            var functions = targetRouteFunc.Item2.ToList();
                            var targFuncName = $"/{functionName}";
                            var targetFunc = functions.Find(x => x.CustomAttributes.Any() && x.GetCustomAttribute<RouteAttribute>().Route == targFuncName);
                            targetFunc.Invoke(controller, Array.Empty<object>());
                            return true;
                        }

                        // wont lie, this might be a security issue, and a small bug, but it will be fixed later.
                        if (targetRouteFunc.Item2.Length == 1)
                        {
                            // invoke the function
                            targetRouteFunc.Item2[0].Invoke(controller, Array.Empty<object>());
                            return true;
                        }

                        // check if the function exists, if not then return 404.
                        if (!targetRouteFunc.Item2.ToList().Any(x => x.Name.ToLower() == functionName.ToLower()))
                        {
                            // send the 404
                            rspCtx.SendNotFound();
                            return true;
                        }

                        // execute the default function.
                        targetRouteFunc.Item2.ToList().Find(x => x.Name.ToLower() == functionName.ToLower()).Invoke(controller, Array.Empty<object>());
                    }
                }
                catch
                {
                    // maybe log this?
                    return false;
                }

                return true;
            }
            else
            {
                if (File.Exists(BackedServer.Instance.Config.RootDirectory + "/" + path))
                {
                    // send the file
                    rspCtx.SendFile(true, path);
                    return true;
                }
                else
                {
                    // send the 404
                    rspCtx.SendNotFound();
                    return true;
                }
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