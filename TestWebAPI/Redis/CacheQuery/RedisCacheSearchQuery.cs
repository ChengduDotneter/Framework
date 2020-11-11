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

        public int Count(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
        {
            string predicateString = nameof(Count) + predicate.ToString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            int cacheCount = m_redisValueCache.GetValueByKey<int>(cacheKey);

            if (cacheCount == 0)
            {
                cacheCount = m_searchQuery.Count(predicate);

                if (cacheCount > 0)
                {
                    int saveMilliseconds = Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]);

                    m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheCount, saveMilliseconds > MAX_SAVE_MILLISECONDS ? MAX_SAVE_MILLISECONDS : saveMilliseconds);
                }
            }

            return cacheCount;
        }

        public int Count<TResult>(IQueryable<TResult> query, ITransaction transaction = null)
        {
            return m_searchQuery.Count(query);
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
        {
            string predicateString = nameof(Count) + predicate.ToString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            int cacheCount = await m_redisValueCache.GetValueByKeyAsync<int>(cacheKey);

            if (cacheCount == 0)
            {
                cacheCount = await m_searchQuery.CountAsync(predicate);

                if (cacheCount > 0)
                {
                    int saveMilliseconds = Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]);

                    await m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheCount, saveMilliseconds > MAX_SAVE_MILLISECONDS ? MAX_SAVE_MILLISECONDS : saveMilliseconds);
                }
            }

            return cacheCount;
        }

        public Task<int> CountAsync<TResult>(IQueryable<TResult> query, ITransaction transaction = null)
        {
            return m_searchQuery.CountAsync(query);
        }

        public T Get(long id, ITransaction transaction = null)
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

        public async Task<T> GetAsync(long id, ITransaction transaction = null)
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

        public ISearchQueryable<T> GetQueryable(ITransaction transaction = null)
        {
            return m_searchQuery.GetQueryable(transaction);
        }

        public Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction = null)
        {
            return m_searchQuery.GetQueryableAsync(transaction);
        }

        public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            string queryOrderBiesString = queryOrderBies == null ? "" : string.Join("_", queryOrderBies.Select(item => item.Expression.ToString() + "_" + item.OrderByType.ToString()));
            string predicateString = nameof(Search) + $"queryOrderBies:{queryOrderBiesString}startIndex:{startIndex}count:{count}" + predicate.ToString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            IEnumerable<T> cacheDatas = m_redisValueCache.GetValueByKey<IEnumerable<T>>(cacheKey);

            if (cacheDatas == null || cacheDatas.Count() < 0)
            {
                cacheDatas = m_searchQuery.Search(predicate, queryOrderBies, startIndex, count);

                if (cacheDatas != null && cacheDatas.Count() > 0)
                    m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheDatas, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return cacheDatas;
        }

        public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            return m_searchQuery.Search(query, startIndex, count, transaction);
        }

        public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            string queryOrderBiesString = queryOrderBies == null ? "" : string.Join("_", queryOrderBies.Select(item => item.Expression.ToString() + "_" + item.OrderByType.ToString()));
            string predicateString = nameof(Search) + $"queryOrderBies:{queryOrderBiesString}startIndex:{startIndex}count:{count}" + predicate.ToString<T>();
            string cacheKey = RedisKeyHelper.GetConditionCacheKey(typeof(T).Name, predicateString);

            IEnumerable<T> cacheDatas = await m_redisValueCache.GetValueByKeyAsync<IEnumerable<T>>(cacheKey);

            if (cacheDatas == null || cacheDatas.Count() < 0)
            {
                cacheDatas = await m_searchQuery.SearchAsync(predicate, queryOrderBies, startIndex, count);

                if (cacheDatas != null && cacheDatas.Count() > 0)
                    await m_redisValueCache.SetValueByKeyAsync(cacheKey, cacheDatas, Convert.ToInt32(ConfigManager.Configuration[REDIS_QUERY_SAVE_MILLISECONDS]));
            }

            return cacheDatas;
        }

        public Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
        {
            return m_searchQuery.SearchAsync(query, startIndex, count, transaction);
        }
    }
}
