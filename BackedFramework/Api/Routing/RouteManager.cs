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
using BackedFramework.Resources.Logging;

namespace BackedFramework.Api.Routing
{
    /// <summary>
    /// A managing type class to help direct the request to the right route context controller.
    /// </summary>
    internal class RouteManager : IDisposable
    {
        /// <summary>
        /// Map created instances to a dictionary to help manage memory.
        /// </summary>
        private readonly Dictionary<Type, BaseController> _routingInstances = new();

        /// <summary>
        /// Static instance of the RouteManager if I need to use it in another class.
        /// </summary>
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

        /// <summary>
        /// Attempt to execute a mapped route, will return a 404 if a route is not found or if a resource requested is not found.
        /// </summary>
        /// <param name="parser">HTTP Request that will be used</param>
        /// <param name="rspCtx">Response context</param>
        /// <param name="reqCtx">Request context</param>
        /// <returns>True if the route was handled correctly, False if the function failed to properly handle the request.</returns>
        /// <remarks>This should NEVER be False, if the returning result is false, then something REALLY bad must've happend. Could potentially identify security vulnerabilities.</remarks>
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
                if (routeResult is null || routeResult.Count == 0)
                {
                    rspCtx.SendNotFound();
                    return true; // invalid request, handle later.
                }

                // get the routing result
                var route = routeResult.First();

                // the function that the route will use
                var targetRouteFunc = _functionRoutes[route];
                var targetRouteClass = targetRouteFunc.Item1;

                // create the controller
                BaseController controller = null;
                // create a basic caching system for controllers so we dont construct 
                // an un-necessary amount of controllers for no reason.
                if (!_routingInstances.ContainsKey(targetRouteClass))
                {
                    controller = Activator.CreateInstance(targetRouteClass) as BaseController;
                    _routingInstances.Add(targetRouteClass, controller);
                }
                else
                {
                    controller = _routingInstances[targetRouteClass];
                }

                // define controller contexts.
                controller.Response = rspCtx;
                controller.Request = reqCtx;

                try
                {
                    // check if the route and path are the same, if so then return the index, if the index function is not present, then return 404.
                    if (fullpath == route.Route)
                    {
                        var hasIndex = targetRouteFunc.Item2.ToList().Any(x => x.Name.ToLower() == "index");
                        if (!hasIndex)
                        {
                            if (targetRouteFunc.Item2.Length == 1)
                            {
                                // get function parameters so we can cast each object type...
                                var parameters = targetRouteFunc.Item2[0].GetParameters();
                                var targetFunc = targetRouteFunc.Item2[0];

                                List<object> parametersToPass = new();
                                foreach (var param in parameters)
                                {
                                    // get name and type info
                                    var name = param.Name;
                                    var type = param.ParameterType;

                                    // validate that the name exists
                                    if (!reqCtx.FormData.ContainsKey(name))
                                        continue;

                                    // get the value of the parameter
                                    var value = reqCtx.FormData[name];
                                    // convert the base type
                                    var res = Convert.ChangeType(value, type);
                                    // add them to the list
                                    parametersToPass.Add(res);
                                }
                                // check the parameter count, if it is not the same then just invoke the function without the parameters.
                                if (parametersToPass.Count > 0)
                                    targetFunc.Invoke(controller, parametersToPass.ToArray()); // invoke the function with parameters
                                else
                                    targetFunc.Invoke(controller, Array.CreateInstance(typeof(object), parameters.Length).Cast<object>().ToArray()); // invoke the function without parameters

                                return true;
                            }

                            // send the 404
                            rspCtx.SendNotFound();
                            return true;
                        }

                        var queryParams = Array.CreateInstance(typeof(object), targetRouteFunc.Item2.First(x => x.Name.ToLower() == "index").GetParameters().Length).Cast<object>().ToArray();

                        if (reqCtx.IsQueried)
                        {
                            for (int i = 0; i < queryParams.Length; i++)
                                queryParams[0] = reqCtx.QueryParameters.Values.ElementAt(i);
                        }

                        // invoke the Index function for the client to recieve.
                        targetRouteClass.GetMethod("Index").Invoke(controller, queryParams);
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

                            // get function parameter information.
                            var parameters = targetFunc.GetParameters();

                            // if the function has no parameters just execute it normally.
                            if (parameters.Length == 0)
                            {
                                targetFunc.Invoke(controller, Array.Empty<object>());
                                return true;
                            }

                            // check if the request is a queried request.
                            if (reqCtx.IsQueried)
                            {
                                    List<object> parametersToPass = new();
                                    foreach (var param in parameters)
                                    {
                                        // get name and type info
                                        var name = param.Name;
                                        var type = param.ParameterType;

                                        // validate that the name exists
                                        if (!reqCtx.QueryParameters.ContainsKey(name))
                                            continue;

                                        // get the value of the parameter
                                        var value = reqCtx.QueryParameters[name];
                                        // convert the base type
                                        var res = Convert.ChangeType(value, type);
                                        // add them to the list
                                        parametersToPass.Add(res);
                                    }
                                    // check the parameter count, if it is not the same then just invoke the function without the parameters.
                                    if (parametersToPass.Count > 0)
                                        targetFunc.Invoke(controller, parametersToPass.ToArray()); // invoke the function with parameters
                                    else
                                        targetFunc.Invoke(controller, Array.CreateInstance(typeof(object), parameters.Length).Cast<object>().ToArray()); // invoke the function without parameters

                                return true;
                            }

                            // invoke the function without parameters
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
                        // INFO: This is a security issue, and a small bug, but it will be fixed later.
                        //targetRouteFunc.Item2.ToList().Find(x => x.Name.ToLower() == functionName.ToLower()).Invoke(controller, Array.Empty<object>());

                        // solving the issue, temporarily, send a 404.
                        rspCtx.SendNotFound();
                    }
                }
                catch
                {
                    // maybe log this?
                    Logger.Log(Logger.LogLevel.Fatal, "Failed to execute route, this is a fatal error, please check any requests for malicious data.");
                    return false;
                }

                return true;
            }
            else
            {
                // validate the file exists.
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
        public void Dispose()
        {
            GC.Collect();
            GC.SuppressFinalize(this);
        }
    }
}