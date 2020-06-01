using System;
using System.Collections.Generic;

namespace Common.Model
{
    public class PageQuery<T> where T : new()
    {
        public int PageIndex { get; set; }
        public int StartIndex { get { return PageIndex * PageCount; } }
        public int PageCount { get; set; }
        public T Condition { get; set; }
    }

    public class PageQueryResult<T> where T : new()
    {
        public int TotalCount { get; set; }
        public IEnumerable<T> Datas { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class LinqSearchAttribute : Attribute
    {
        public Type SearchType { get; }
        public string GetLinqFunctionName { get; }

        public LinqSearchAttribute(Type searchType, string getLinqFunctionName)
        {
            SearchType = searchType;
            GetLinqFunctionName = getLinqFunctionName;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class LinqJoinTableSearchAttribute : Attribute
    {
        public Type SearchType { get; }
        public string GetLinqJoinTableSearchFunctionName { get; }

        public LinqJoinTableSearchAttribute(Type searchType, string getLinqJoinTableSearchFunctionName)
        {
            SearchType = searchType;
            GetLinqJoinTableSearchFunctionName = getLinqJoinTableSearchFunctionName;
        }
    }
}
