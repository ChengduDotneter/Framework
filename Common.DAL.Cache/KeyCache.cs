using System;
using System.Threading.Tasks;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 键值缓存具体实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
        /// <summary>
        /// 从缓存家在数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dbResourceContent"></param>
        /// <param name="systemID"></param>
        /// <returns></returns>
        public T Get(long id, IDBResourceContent dbResourceContent = null, string systemID = null)
        {
            (bool exists, T result) = m_cache.TryGetValue<T>(id.ToSystemObjectID(systemID));//从缓存获取

            if (!exists)//没获取到则从数据库查询
            {
                result = m_searchQuery.Get(systemID ?? string.Empty, id, dbResourceContent: dbResourceContent);

                if (result != null)//并加入缓存
                    m_cache.Set(id.ToSystemObjectID(systemID), result);
            }
            //返回数据
            return result;
        }

        public T Get(ITransaction transaction, long id, string systemID = null)
        {
            if (transaction is TransactionProxy transactionProxy)
                transaction = transactionProxy.Transaction;

            return m_searchQuery.Get(systemID ?? string.Empty, id, transaction: transaction);
        }

        public async Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null, string systemID = null)
        {
            (bool exists, T result) = await m_cache.TryGetValueAsync<T>(id.ToSystemObjectID(systemID));

            if (!exists)
            {
                result = await m_searchQuery.GetAsync(systemID ?? string.Empty, id, dbResourceContent: dbResourceContent);

                if (result != null)
                    await m_cache.SetAsync(id.ToSystemObjectID(systemID), result);
            }

            return result;
        }

        public Task<T> GetAsync(ITransaction transaction, long id, string systemID = null)
        {
            if (transaction is TransactionProxy transactionProxy)
                transaction = transactionProxy.Transaction;

            return m_searchQuery.GetAsync(systemID ?? string.Empty, id, transaction: transaction);
        }
    }
}