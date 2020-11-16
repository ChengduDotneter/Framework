using System.Collections.Generic;

namespace TestRedis.RedisService
{
    public static class KeyValuePairCacheFactory
    {
        private static IDictionary<string, IKeyValuePairCache> keyValuePairCacheDic;

        static KeyValuePairCacheFactory()
        {
            keyValuePairCacheDic = new Dictionary<string, IKeyValuePairCache>();
        }

        public static IKeyValuePairCache GetRedisCache()
        {
            if (!keyValuePairCacheDic.ContainsKey(nameof(GetRedisCache)))
                lock (keyValuePairCacheDic)
                    if (!keyValuePairCacheDic.ContainsKey(nameof(GetRedisCache)))
                        keyValuePairCacheDic.Add(nameof(GetRedisCache), new RedisCache());

            return keyValuePairCacheDic[nameof(GetRedisCache)];
        }
    }
}