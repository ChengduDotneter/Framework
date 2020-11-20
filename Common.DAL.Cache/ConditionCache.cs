using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 条件缓存类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConditionCache<T> : IConditionCache<T>
        where T : class, IEntity, new()
    {
        private readonly ISearchQuery<T> m_searchQuery;
        private readonly MemoryCache m_memoryCache;

        /// <summary>
        /// 构造方法
        /// </summary>
        /// <param name="searchQuery"></param>
        public ConditionCache(ISearchQuery<T> searchQuery)
        {
            m_searchQuery = searchQuery;
            m_memoryCache = CacheFactory<T>.GetConditionMemoryCache();
        }

        /// <summary>
        /// 根据筛选条件匹配数据
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <returns></returns>
        public IEnumerable<T> Get(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue,IDBResourceContent dbResourceContent = null)
        {
            string conditionKey = $"{condition.ToString<T>()}_{startIndex}_{count}";

            if (!m_memoryCache.TryGetValue(conditionKey, out IEnumerable<T> result))
            {
                result = m_searchQuery.Search(condition, startIndex: startIndex, count: count, dbResourceContent: dbResourceContent);
                m_memoryCache.Set(conditionKey, result);
            }

            return result;
        }

        public IEnumerable<T> Get(ITransaction transaction, Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(transaction, condition, startIndex: startIndex, count: count);
        }
    }
}