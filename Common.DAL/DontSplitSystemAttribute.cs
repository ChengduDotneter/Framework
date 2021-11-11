using System;

namespace Common.DAL
{
    /// <summary>
    /// 不分区特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DontSplitSystemAttribute : Attribute
    {
    }
}