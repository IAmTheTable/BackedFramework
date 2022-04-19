using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackedFramework.Api.Routing
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class RouteAttribute : Attribute
    {
        public string Route { get; private set; }
        public RouteAttribute(string targetRoute)
        {
            this.Route = targetRoute;
        }
    }
}
