using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 条件缓存具体实现
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class ConditionCache<T> : IConditionCache<T>
        where T : class, IEntity, new()
    {
        private ISearchQuery<T> m_searchQuery;//数据查询接口
        private ICache m_cache;//缓存接口
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="cache"></param>
        public ConditionCache(ISearchQuery<T> searchQuery, ICache cache)
        {
            m_searchQuery = searchQuery;
            m_cache = cache;
        }
        /// <summary>
        /// 获取md5加密的key
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="systemID"></param>
        /// <returns></returns>
        private static string GetConditionMd5Key(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, string systemID = null)
        {
            return MD5Encryption.GetMD5($"{condition.ToLamdaString()}_{startIndex}_{count}_{systemID ?? string.Empty}");
        }
        /// <summary>
        /// 根据条件获取缓存的数据 不存在则从数据库查找 并加入缓存
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <param name="systemID"></param>
        /// <returns></returns>
        public IEnumerable<T> Get(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null, string systemID = null)
        {
            string conditionKey = GetConditionMd5Key(condition, startIndex, count, systemID);
            (bool exists, IEnumerable<T> result) = m_cache.TryGetValue<IEnumerable<T>>(conditionKey);

            if (!exists)
            {
                result = m_searchQuery.Search(systemID ?? string.Empty, condition, startIndex: startIndex, count: count, dbResourceContent: dbResourceContent);
                m_cache.Set(conditionKey, result);
            }

            return result;
        }
        /// <summary>
        /// 根据条件从数据库查找数据
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="systemID"></param>
        /// <returns></returns>
        public IEnumerable<T> Get(ITransaction transaction, Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, string systemID = null)
        {
            if (transaction is TransactionProxy transactionProxy)
                transaction = transactionProxy.Transaction;

            return m_searchQuery.Search(systemID ?? string.Empty, transaction, condition, startIndex: startIndex, count: count);
        }
        /// <summary>
        /// 异步从缓存或者数据库获取数据
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="dbResourceContent"></param>
        /// <param name="systemID"></param>
        /// <returns></returns>
        public async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null, string systemID = null)
        {
            string conditionKey = GetConditionMd5Key(condition, startIndex, count, systemID);
            (bool exists, IEnumerable<T> result) = await m_cache.TryGetValueAsync<IEnumerable<T>>(conditionKey);

            if (!exists)
            {
                result = await m_searchQuery.SearchAsync(systemID ?? string.Empty, condition, startIndex: startIndex, count: count, dbResourceContent: dbResourceContent);
                await m_cache.SetAsync(conditionKey, result);
            }

            return result;
        }
        /// <summary>
        /// 异步从数据库获取数据
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="condition"></param>
        /// <param name="startIndex"></param>
        /// <param name="count"></param>
        /// <param name="systemID"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> GetAsync(ITransaction transaction, Expression<Func<T, bool>> condition, int startIndex = 0, int count = int.MaxValue, string systemID = null)
        {
            if (transaction is TransactionProxy transactionProxy)
                transaction = transactionProxy.Transaction;

            return m_searchQuery.SearchAsync(systemID ?? string.Empty, transaction, condition, startIndex: startIndex, count: count);
        }
    }
}