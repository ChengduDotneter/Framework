using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Common.DAL.Cache
{
    internal static class CacheFactory<T>
        where T : IEntity
    {
        private const int CACHE_EXPIRATION_SCAN = 3;
        private const int CACHE_MAX_SIZE = 10000;
        private const double CACHE_COMPACTION_PERCENTAGE = 0.2;
        private static MemoryCache m_keyMemoryCache;
        private static MemoryCache m_conditionMemoryCache;
        private static object m_lockKeyMemoryCache;
        private static object m_lockConditionMemoryCache;

        static CacheFactory()
        {
            m_lockKeyMemoryCache = new object();
            m_lockConditionMemoryCache = new object();
        }

        public static void ClearKeyMemoryCache()
        {
            if (m_keyMemoryCache == null ||
                m_keyMemoryCache.Count == 0)
                return;

            m_keyMemoryCache.Dispose();
            m_keyMemoryCache = null;
            GetKeyMemoryCache();
        }

        public static void ClearConditionMemoryCache()
        {
            if (m_conditionMemoryCache == null ||
                m_conditionMemoryCache.Count == 0)
                return;

            m_conditionMemoryCache.Dispose();
            m_conditionMemoryCache = null;
            GetKeyMemoryCache();
        }

        public static MemoryCache GetKeyMemoryCache()
        {
            if (m_keyMemoryCache == null)
                lock (m_lockKeyMemoryCache)
                    if (m_keyMemoryCache == null)
                        m_keyMemoryCache = CreateCache();

            return m_keyMemoryCache;
        }

        public static MemoryCache GetConditionMemoryCache()
        {
            if (m_conditionMemoryCache == null)
                lock (m_lockConditionMemoryCache)
                    if (m_conditionMemoryCache == null)
                        m_conditionMemoryCache = CreateCache();

            return m_conditionMemoryCache;
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
    }

    internal static class MemoryCacheExtention
    {
        private const int CACHE_EXPIRATION = 60 * 2;
        private const int CACHE_SIZE = 1;

        public static IEnumerable<T> Set<T>(this MemoryCache memoryCache, object key, IEnumerable<T> value)
            where T : IEntity
        {
            return memoryCache.Set(key, value, new MemoryCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromSeconds(CACHE_EXPIRATION),
                Size = CACHE_SIZE * value.Count()
            });
        }

        public static T Set<T>(this MemoryCache memoryCache, object key, T value)
            where T : IEntity
        {
            return memoryCache.Set(key, value, new MemoryCacheEntryOptions()
            {
                SlidingExpiration = TimeSpan.FromSeconds(CACHE_EXPIRATION),
                Size = CACHE_SIZE
            });
        }
    }
}