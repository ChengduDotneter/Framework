using System;

namespace Common.Model
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IgnoreColumnAttribute : Attribute
    {
        public string[] IgnoreColumns { get; }
        public IgnoreColumnAttribute(string[] ignoreColumns) => IgnoreColumns = ignoreColumns;
    }
}
