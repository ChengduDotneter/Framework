using Common.DAL;
using TestRedis.CacheQuery;
using TestRedis.CacheSearchQuery;

namespace TestRedis
{
    public static class Extention
    {
        /// <summary>
        /// 缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery"></param>
        /// <returns></returns>
        public static IEditQuery<T> RedisCache<T>(this IEditQuery<T> editQuery) where T : class, IEntity, new()
        {
            return new RedisCacheEditQuery<T>(editQuery);
        }

        /// <summary>
        /// 键值缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public static ISearchQuery<T> RedisCache<T>(this ISearchQuery<T> searchQuery) where T : class, IEntity, new()
        {
            return new RedisCacheSearchQuery<T>(searchQuery);
        }
    }
}