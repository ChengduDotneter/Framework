using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Common.DAL.Cache
{
    internal class MemoryCacheProvider<T> : ICacheProvider<T>
        where T : class, IEntity, new()
    {
        private static MemoryCacheInstance m_keyCacheInstance;
        private static MemoryCacheInstance m_conditionCacheInstance;

        static MemoryCacheProvider()
        {
            m_keyCacheInstance = new MemoryCacheInstance();
            m_conditionCacheInstance = new MemoryCacheInstance();
        }

        public IKeyCache<T> CreateKeyCache(ISearchQuery<T> searchQuery)
        {
            return new KeyCache<T>(searchQuery, m_keyCacheInstance);
        }

        public IConditionCache<T> CreateConditionCache(ISearchQuery<T> searchQuery)
        {
            return new ConditionCache<T>(searchQuery, m_conditionCacheInstance);
        }

        public IEditQuery<T> CreateEditQueryCache(IEditQuery<T> editQuery)
        {
            return new EditQueryProxy<T>(editQuery, m_keyCacheInstance, m_conditionCacheInstance);
        }
    }

    internal class MemoryCacheInstance : ICache
    {
        private const int CACHE_EXPIRATION_SCAN = 3;
        private const int CACHE_MAX_SIZE = 10000;
        private const double CACHE_COMPACTION_PERCENTAGE = 0.2;
        private const int CACHE_EXPIRATION = 60 * 2;
        private const int CACHE_SIZE = 1;
        private MemoryCache m_cache;

        public MemoryCacheInstance()
        {
            m_cache = CreateCache();
        }

        private static MemoryCache CreateCache()
        {
            MemoryCacheOptions cacheOps = new MemoryCacheOptions()
            {
                //##注意netcore中的缓存是没有单位的，缓存项和缓存的相对关系
                SizeLimit = CACHE_MAX_SIZE,
                //缓存满了时，压缩20%（即删除20份优先级低的缓存项）
                CompactionPercentage = CACHE_COMPACTION_PERCENTAGE,
                //两秒钟查找一次过期项
                ExpirationScanFrequency = TimeSpan.FromSeconds(CACHE_EXPIRATION_SCAN)
            };

            return new MemoryCache(cacheOps);
        }

        public Tuple<bool, T> TryGetValue<T>(object key)
        {
            return Tuple.Create(m_cache.TryGetValue(key, out T value), value);
        }

        public Task<Tuple<bool, T>> TryGetValueAsync<T>(object key)
        {
            return Task.FromResult(Tuple.Create(m_cache.TryGetValue(key, out T value), value));
        }

        public T Set<T>(object key, T value)
        {
            return m_cache.Set(key, value, new MemoryCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromSeconds(CACHE_EXPIRATION),
                Size = CACHE_SIZE
            });
        }

        public Task<T> SetAsync<T>(object key, T value)
        {
            return Task.FromResult(m_cache.Set(key, value, new MemoryCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromSeconds(CACHE_EXPIRATION),
                Size = CACHE_SIZE
            }));
        }

        public void Remove(object key)
        {
            m_cache.Remove(key);
        }

        public Task RemoveAsync(object key)
        {
            m_cache.Remove(key);
            return Task.CompletedTask;
        }

        public void Clear()
        {
            lock (this)
            {
                if (m_cache == null || m_cache.Count == 0)
                    return;

                m_cache.Dispose();
                m_cache = null;
                m_cache = CreateCache();
            }
        }

        public Task ClearAsync()
        {
            lock (this)
            {
                if (m_cache != null && m_cache.Count > 0)
                {
                    m_cache.Dispose();
                    m_cache = null;
                    m_cache = CreateCache();
                }
            }

            return Task.CompletedTask;
        }
    }
}