using System;

namespace Common.Model
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DontSplitSystemAttribute : Attribute
    {
    }
}