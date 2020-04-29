using System;

namespace Common.DAL
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IgnoreTableAttribute : Attribute { }
}
