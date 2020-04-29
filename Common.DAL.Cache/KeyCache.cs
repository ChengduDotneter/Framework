using Microsoft.Extensions.Caching.Memory;

namespace Common.DAL.Cache
{
    internal class KeyCache<T> : IKeyCache<T>
        where T : class, IEntity, new()
    {
        private ISearchQuery<T> m_searchQuery;
        private MemoryCache m_memoryCache;

        public KeyCache(ISearchQuery<T> searchQuery)
        {
            m_searchQuery = searchQuery;
            m_memoryCache = CacheFactory<T>.GetKeyMemoryCache();
        }

        public T Get(long id)
        {
            if (!m_memoryCache.TryGetValue(id, out T result))
            {
                result = m_searchQuery.Get(id);

                if (result != null)
                    m_memoryCache.Set(id, result);
            }

            return result;
        }
    }
}
