using System;

namespace Common.Model
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IgnoreBuildControllerAttribute : Attribute
    {
        public bool IgnoreGet { get; private set; }
        public bool IgnoreSearch { get; private set; }
        public bool IgnorePost { get; private set; }
        public bool IgnorePut { get; private set; }
        public bool IgnoreDelete { get; private set; }

        public IgnoreBuildControllerAttribute(bool ignoreGet = false, bool ignoreSearch = false, bool ignorePost = false, bool ignorePut = false, bool ignoreDelete = false)
        {
            IgnoreGet = ignoreGet;
            IgnoreSearch = ignoreSearch;
            IgnorePost = ignorePost;
            IgnorePut = ignorePut;
            IgnoreDelete = ignoreDelete;
        }
    }
}
