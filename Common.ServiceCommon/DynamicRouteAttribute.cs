using System;

namespace Common.ServiceCommon
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DynamicRouteAttribute : Attribute
    {
        public string Route { get; set; }

        public DynamicRouteAttribute(string route)
        {
            Route = route;
        }
    }
}
