using System;

namespace Common.DAL

{
    /// <summary>
    /// ²»·Ö±í
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DontSplitSystemAttribute : Attribute
    {
    }
}