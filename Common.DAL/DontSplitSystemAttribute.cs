using System;

namespace Common.DAL
{
    /// <summary>
    /// ����������
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class DontSplitSystemAttribute : Attribute
    {
    }
}