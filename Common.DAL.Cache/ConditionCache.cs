using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace Common.DAL.Cache
{
    public class ConditionCache<T> : IConditionCache<T>
        where T : class, IEntity, new()
    {
        private ISearchQuery<T> m_searchQuery;
        private MemoryCache m_memoryCache;

        public ConditionCache(ISearchQuery<T> searchQuery)
        {
            m_searchQuery = searchQuery;
            m_memoryCache = CacheFactory<T>.GetConditionMemoryCache();
        }

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
