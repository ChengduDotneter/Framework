using System.Threading.Tasks;

namespace Common.DAL.Cache
{
    internal class KeyCache<T> : IKeyCache<T>
        where T : class, IEntity, new()
    {
        private ISearchQuery<T> m_searchQuery;
        private ICache m_cache;

        public KeyCache(ISearchQuery<T> searchQuery, ICache cache)
        {
            m_searchQuery = searchQuery;
            m_cache = cache;
        }

        public T Get(long id, IDBResourceContent dbResourceContent = null)
        {
            if (!m_cache.TryGetValue(id, out T result))
            {
                result = m_searchQuery.Get(id, dbResourceContent: dbResourceContent);

                if (result != null)
                    m_cache.Set(id, result);
            }

            return result;
        }

        public T Get(ITransaction transaction, long id)
        {
            return m_searchQuery.Get(id, transaction: transaction);
        }

        public async Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
        {
            if (!await m_cache.TryGetValueAsync(id, out T result))
            {
                result = await m_searchQuery.GetAsync(id, dbResourceContent: dbResourceContent);

                if (result != null)
                    await m_cache.SetAsync(id, result);
            }

            return result;
        }

        public Task<T> GetAsync(ITransaction transaction, long id)
        {
            return m_searchQuery.GetAsync(id, transaction: transaction);
        }
    }
}