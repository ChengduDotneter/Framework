using System;

namespace Common.Model
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class IgnoreBuildControllerAttribute : Attribute
    {
        public bool IgnoreGet { get; private set; }//是否忽略创建get方法
        public bool IgnoreSearch { get; private set; }//是否忽略创建search方法
        public bool IgnorePost { get; private set; }//是否忽略创建post方法
        public bool IgnorePut { get; private set; }//是否忽略创建put方法
        public bool IgnoreDelete { get; private set; }//是否忽略创建delete方法

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