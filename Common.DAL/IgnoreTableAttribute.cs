 using System;

namespace Common.DAL
{
    /// <summary>
    /// 忽略表创建特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IgnoreTableAttribute : Attribute { }
}