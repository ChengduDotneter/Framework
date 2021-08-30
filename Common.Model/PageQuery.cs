using System;
using System.Collections.Generic;

namespace Common.Model
{/// <summary>
/// 分页Search查询
/// </summary>
/// <typeparam name="T"></typeparam>
    public class PageQuery<T> where T : new()
    {
        public int PageIndex { get; set; }
        public int StartIndex { get { return PageIndex * PageCount; } }
        public int PageCount { get; set; }
        public T Condition { get; set; }
    }
    /// <summary>
    /// Search查询返回结果
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PageQueryResult<T> where T : new()
    {
        public int TotalCount { get; set; }
        public IEnumerable<T> Datas { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class LinqSearchAttribute : Attribute//linq查询特性
    {
        public Type SearchType { get; }//实体
        public string GetLinqFunctionName { get; }//linq实现方法名

        public LinqSearchAttribute(Type searchType, string getLinqFunctionName)
        {
            SearchType = searchType;
            GetLinqFunctionName = getLinqFunctionName;
        }
    }
}