namespace Common.DAL.Cache
{
    /// <summary>
    /// 缓存工厂
    /// </summary>
    public static class CacheFactory
    {
        /// <summary>
        /// 创建MemoryCache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ICacheProvider<T> CreateMemoryCacheProvider<T>()
            where T : class, IEntity, new()
        {
            return new MemoryCacheProvider<T>();
        }
        /// <summary>
        /// 创建RedisCache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ICacheProvider<T> CreateRedisCacheProvider<T>()
             where T : class, IEntity, new()
        {
            return new RedisCacheProvider<T>();
        }
    }

    internal static class Utils
    {
        public static string ToSystemObjectID(this long id, string systemID)
        {
            string postFix = string.IsNullOrEmpty(systemID) ? systemID : $"_{systemID}";
            return $"{id}{postFix}";
        }
    }
}
