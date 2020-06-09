using System;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 动态路由
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DynamicRouteAttribute : Attribute
    {
        /// <summary>
        /// 动态路由参数
        /// </summary>
        public string Route { get; set; }

        /// <summary>
        /// 动态路由
        /// </summary>
        /// <param name="route"></param>
        public DynamicRouteAttribute(string route)
        {
            Route = route;
        }
    }
}
