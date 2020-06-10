using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 条件缓存类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConditionCache<T> : IConditionCache<T>
        where T : class, IEntity, new()
    {
        private ISearchQuery<T> m_searchQuery;
        private MemoryCache m_memoryCache;

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
        /// 查询条件
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public IEnumerable<T> Get(string condition)
        {
            if (!m_memoryCache.TryGetValue(condition, out IEnumerable<T> result))
            {
                result = m_searchQuery.Search(condition);
                m_memoryCache.Set(condition, result);
            }

            return result;
        }
    }
}