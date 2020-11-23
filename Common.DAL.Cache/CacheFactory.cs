namespace Common.DAL.Cache
{
    public static class CacheFactory
    {
        public static ICacheProvider<T> CreateMemoryCacheProvider<T>()
            where T : class, IEntity, new()
        {
            return new MemoryCacheProvider<T>();
        }

        public static ICacheProvider<T> CreateRedisCacheProvider<T>()
             where T : class, IEntity, new()
        {
            return new RedisCacheProvider<T>();
        }
    }
}
