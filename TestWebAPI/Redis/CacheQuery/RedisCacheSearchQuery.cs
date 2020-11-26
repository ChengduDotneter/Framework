using Common;
using Common.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using TestRedis.RedisService;

namespace TestRedis.CacheSearchQuery
{
    public class RedisCacheSearchQuery<T> : ISearchQuery<T> where T : class, IEntity, new()
    {
        private const string REDIS_QUERY_SAVE_MILLISECONDS = "RedisQuerySaveMilliseconds";
        private const int MAX_SAVE_MILLISECONDS = 1000 * 60 * 10;

        private readonly ISearchQuery<T> m_searchQuery;
        private readonly static IKeyValuePairCache m_redisValueCache;

        static RedisCacheSearchQuery()
        {
            m_redisValueCache = KeyValuePairCacheFactory.GetRedisCache();
        }

        public RedisCacheSearchQuery(ISearchQuery<T> searchQuery)
        {
            m_searchQuery = searchQuery;
        }

        public T Get(long id, ITransaction transaction)
        {
            string cacheKey = RedisKeyHelper.GetKeyCacheKey(typeof(T).Name, id);

            T data = m_redisValueCache.GetValueByKey<T>(cacheKey);

            if (data == null)
            {
                data = m_searchQuery.Get(id, transaction);

                if (data != null)
                    m_redisValueCache.SetValueByKeyAsync(cacheKey, data, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return data;
        }

        public T Get(long id, IDBResourceContent dbResourceContent = null)
        {
            string cacheKey = RedisKeyHelper.GetKeyCacheKey(typeof(T).Name, id);

            T data = m_redisValueCache.GetValueByKey<T>(cacheKey);

            if (data == null)
            {
                data = m_searchQuery.Get(id, dbResourceContent);

                if (data != null)
                    m_redisValueCache.SetValueByKeyAsync(cacheKey, data, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return data;
        }

        public async Task<T> GetAsync(long id, ITransaction transaction)
        {
            string cacheKey = RedisKeyHelper.GetKeyCacheKey(typeof(T).Name, id);

            T data = await m_redisValueCache.GetValueByKeyAsync<T>(cacheKey);

            if (data == null)
            {
                data = await m_searchQuery.GetAsync(id, transaction);

                if (data != null)
                    await m_redisValueCache.SetValueByKeyAsync(cacheKey, data, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return data;
        }

        public async Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
        {
            string cacheKey = RedisKeyHelper.GetKeyCacheKey(typeof(T).Name, id);

            T data = await m_redisValueCache.GetValueByKeyAsync<T>(cacheKey);

            if (data == null)
            {
                data = await m_searchQuery.GetAsync(id, dbResourceContent);

                if (data != null)
                    await m_redisValueCache.SetValueByKeyAsync(cacheKey, data, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return data;
        }

        public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null)
        {
            string predicateString = nameof(Count) + predicate.ToLamdaString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            int cacheCount = m_redisValueCache.GetValueByKey<int>(cacheKey);

            if (cacheCount == 0)
            {
                cacheCount = m_searchQuery.Count(transaction, predicate);

                if (cacheCount > 0)
                {
                    int saveMilliseconds = Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]);

                    m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheCount, saveMilliseconds > MAX_SAVE_MILLISECONDS ? MAX_SAVE_MILLISECONDS : saveMilliseconds);
                }
            }

            return cacheCount;
        }

        public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            string predicateString = nameof(Count) + predicate.ToLamdaString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            int cacheCount = m_redisValueCache.GetValueByKey<int>(cacheKey);

            if (cacheCount == 0)
            {
                cacheCount = m_searchQuery.Count(predicate, dbResourceContent);

                if (cacheCount > 0)
                {
                    int saveMilliseconds = Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]);

                    m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheCount, saveMilliseconds > MAX_SAVE_MILLISECONDS ? MAX_SAVE_MILLISECONDS : saveMilliseconds);
                }
            }

            return cacheCount;
        }

        public async Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null)
        {
            string predicateString = nameof(Count) + predicate.ToLamdaString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            int cacheCount = m_redisValueCache.GetValueByKey<int>(cacheKey);

            if (cacheCount == 0)
            {
                cacheCount = await m_searchQuery.CountAsync(transaction, predicate);

                if (cacheCount > 0)
                {
                    int saveMilliseconds = Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]);

                    await m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheCount, saveMilliseconds > MAX_SAVE_MILLISECONDS ? MAX_SAVE_MILLISECONDS : saveMilliseconds);
                }
            }

            return cacheCount;
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
        {
            string predicateString = nameof(Count) + predicate.ToLamdaString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            int cacheCount = m_redisValueCache.GetValueByKey<int>(cacheKey);

            if (cacheCount == 0)
            {
                cacheCount = await m_searchQuery.CountAsync(predicate, dbResourceContent);

                if (cacheCount > 0)
                {
                    int saveMilliseconds = Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]);

                    await m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheCount, saveMilliseconds > MAX_SAVE_MILLISECONDS ? MAX_SAVE_MILLISECONDS : saveMilliseconds);
                }
            }

            return cacheCount;
        }

        public IEnumerable<T> Search(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue)
        {
            string queryOrderBiesString = queryOrderBies == null ? "" : string.Join("_", queryOrderBies.Select(item => item.Expression.ToString() + "_" + item.OrderByType.ToString()));
            string predicateString = nameof(Search) + $"queryOrderBies:{queryOrderBiesString}startIndex:{startIndex}count:{count}" + predicate.ToLamdaString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            IEnumerable<T> cacheDatas = m_redisValueCache.GetValueByKey<IEnumerable<T>>(cacheKey);

            if (cacheDatas == null || cacheDatas.Count() < 0)
            {
                cacheDatas = m_searchQuery.Search(transaction, predicate, queryOrderBies, startIndex, count);

                if (cacheDatas != null && cacheDatas.Count() > 0)
                    m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheDatas, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return cacheDatas;
        }

        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            string queryOrderBiesString = queryOrderBies == null ? "" : string.Join("_", queryOrderBies.Select(item => item.Expression.ToString() + "_" + item.OrderByType.ToString()));
            string predicateString = nameof(Search) + $"queryOrderBies:{queryOrderBiesString}startIndex:{startIndex}count:{count}" + predicate.ToLamdaString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            IEnumerable<T> cacheDatas = m_redisValueCache.GetValueByKey<IEnumerable<T>>(cacheKey);

            if (cacheDatas == null || cacheDatas.Count() < 0)
            {
                cacheDatas = m_searchQuery.Search(predicate, queryOrderBies, startIndex, count, dbResourceContent);

                if (cacheDatas != null && cacheDatas.Count() > 0)
                    m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheDatas, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return cacheDatas;
        }

        public async Task<IEnumerable<T>> SearchAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue)
        {
            string queryOrderBiesString = queryOrderBies == null ? "" : string.Join("_", queryOrderBies.Select(item => item.Expression.ToString() + "_" + item.OrderByType.ToString()));
            string predicateString = nameof(Search) + $"queryOrderBies:{queryOrderBiesString}startIndex:{startIndex}count:{count}" + predicate.ToLamdaString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            IEnumerable<T> cacheDatas = await m_redisValueCache.GetValueByKeyAsync<IEnumerable<T>>(cacheKey);

            if (cacheDatas == null || cacheDatas.Count() < 0)
            {
                cacheDatas = await m_searchQuery.SearchAsync(transaction, predicate, queryOrderBies, startIndex, count);

                if (cacheDatas != null && cacheDatas.Count() > 0)
                    await m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheDatas, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return cacheDatas;
        }

        public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            string queryOrderBiesString = queryOrderBies == null ? "" : string.Join("_", queryOrderBies.Select(item => item.Expression.ToString() + "_" + item.OrderByType.ToString()));
            string predicateString = nameof(Search) + $"queryOrderBies:{queryOrderBiesString}startIndex:{startIndex}count:{count}" + predicate.ToLamdaString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            IEnumerable<T> cacheDatas = await m_redisValueCache.GetValueByKeyAsync<IEnumerable<T>>(cacheKey);

            if (cacheDatas == null || cacheDatas.Count() < 0)
            {
                cacheDatas = await m_searchQuery.SearchAsync(predicate, queryOrderBies, startIndex, count, dbResourceContent);

                if (cacheDatas != null && cacheDatas.Count() > 0)
                    await m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheDatas, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return cacheDatas;
        }

        public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return m_searchQuery.Count(transaction, query);
        }

        public int Count<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Count(query, dbResourceContent);
        }

        public async Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
        {
            return await m_searchQuery.CountAsync(transaction, query);
        }

        public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.CountAsync(query, dbResourceContent);
        }

        public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return m_searchQuery.Search(transaction, query, startIndex, count);
        }

        public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.Search(query, startIndex, count, dbResourceContent);
        }

        public async Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
        {
            return await m_searchQuery.SearchAsync(transaction, query, startIndex, count);
        }

        public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.SearchAsync(query, startIndex, count, dbResourceContent);
        }

        public ISearchQueryable<T> GetQueryable(ITransaction transaction)
        {
            return m_searchQuery.GetQueryable(transaction);
        }

        public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
        {
            return m_searchQuery.GetQueryable(dbResourceContent);
        }

        public async Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
        {
            return await m_searchQuery.GetQueryableAsync(transaction);
        }

        public async Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
        {
            return await m_searchQuery.GetQueryableAsync(dbResourceContent);
        }
    }
}