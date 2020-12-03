using Common.DAL;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TestRedis.RedisService;

namespace TestRedis.CacheQuery
{
    public class RedisCacheEditQuery<T> : IEditQuery<T> where T : class, IEntity, new()
    {
        private readonly IEditQuery<T> m_editQuery;
        private readonly static IKeyValuePairCache m_redisValueCache;

        static RedisCacheEditQuery()
        {
            m_redisValueCache = KeyValuePairCacheFactory.GetRedisCache();
        }

        public RedisCacheEditQuery(IEditQuery<T> editQuery)
        {
            m_editQuery = editQuery;
        }

        public ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
        {
            return m_editQuery.BeginTransaction(distributedLock, weight);
        }

        public Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
        {
            return m_editQuery.BeginTransactionAsync(distributedLock, weight);
        }

        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            Task.Factory.StartNew(() =>
            {
                ClearConditionCacheAsync();

                foreach (long id in ids)
                {
                    ClearKeyCacheAsync(id);
                }
            });

            m_editQuery.Delete(transaction, ids);
        }

        public Task DeleteAsync(ITransaction transaction = null, params long[] ids)
        {
            Task.Factory.StartNew(() =>
            {
                ClearConditionCacheAsync();

                foreach (long id in ids)
                {
                    ClearKeyCacheAsync(id);
                }
            });

            return m_editQuery.DeleteAsync(transaction, ids);
        }

        public void Insert(ITransaction transaction = null, params T[] datas)
        {
            ClearConditionCacheAsync();

            m_editQuery.Insert(transaction, datas);
        }

        public Task InsertAsync(ITransaction transaction = null, params T[] datas)
        {
            ClearConditionCacheAsync();

            return m_editQuery.InsertAsync(transaction, datas);
        }

        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            Task.Factory.StartNew(() =>
            {
                ClearConditionCacheAsync();

                foreach (T data in datas)
                {
                    UpdateKeyCacheAsync(data);
                }
            });

            m_editQuery.Merge(transaction, datas);
        }

        public Task MergeAsync(ITransaction transaction = null, params T[] datas)
        {
            Task.Factory.StartNew(() =>
            {
                ClearConditionCacheAsync();

                foreach (T data in datas)
                {
                    UpdateKeyCacheAsync(data);
                }
            });

            return m_editQuery.MergeAsync(transaction, datas);
        }

        public void Update(T data, ITransaction transaction = null)
        {
            ClearConditionCacheAsync();
            UpdateKeyCacheAsync(data);

            m_editQuery.Update(data, transaction);
        }

        public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            ClearConditionCacheAsync();

            m_editQuery.Update(predicate, upateDictionary, transaction);
        }

        public Task UpdateAsync(T data, ITransaction transaction = null)
        {
            ClearConditionCacheAsync();
            UpdateKeyCacheAsync(data);

            return m_editQuery.UpdateAsync(data, transaction);
        }

        public Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
        {
            ClearConditionCacheAsync();

            return m_editQuery.UpdateAsync(predicate, upateDictionary, transaction);
        }

        private Task ClearConditionCacheAsync()
        {
            string cacheKeyProfix = RedisKeyHelper.GetRedisKeyProfix(typeof(T).Name, CacheSaveType.ConditionCache);
            return m_redisValueCache.ClearCacheByKeyAsync(cacheKeyProfix);
        }

        private Task UpdateKeyCacheAsync(T data)
        {
            string keyCacheKey = RedisKeyHelper.GetKeyCacheKey(typeof(T).Name, data.ID);

            if (m_redisValueCache.KeyExists(keyCacheKey))
                return m_redisValueCache.SetValueByKeyAsync(keyCacheKey, data);
            else return Task.CompletedTask;
        }

        private Task ClearKeyCacheAsync(long id)
        {
            string keyCacheKey = RedisKeyHelper.GetKeyCacheKey(typeof(T).Name, id);

            if (m_redisValueCache.KeyExists(keyCacheKey))
                return m_redisValueCache.ClearCacheByKeyAsync(keyCacheKey);
            else return Task.CompletedTask;
        }
    }
}