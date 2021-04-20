using System;

namespace Common.DAL

{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DontSplitSystemAttribute : Attribute
    {
    }
}