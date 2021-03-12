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

    internal static class Utils
    {
        public static string ToSystemObjectID(this long id, string systemID)
        {
            string postFix = string.IsNullOrEmpty(systemID) ? systemID : $"_{systemID}";
            return $"{id}{postFix}";
        }
    }
}
