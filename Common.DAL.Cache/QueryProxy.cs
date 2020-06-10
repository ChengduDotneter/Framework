namespace Common.DAL.Cache
{
    /// <summary>
    /// IEditQuery的扩展类
    /// </summary>
    public static class IEditQueryExtention
    {
        /// <summary>
        /// 缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="editQuery"></param>
        /// <returns></returns>
        public static IEditQuery<T> Cache<T>(this IEditQuery<T> editQuery) where T : class, IEntity, new()
        {
            return new EditQueryProxy<T>(editQuery);
        }
    }

    /// <summary>
    /// ISearchQuery的扩展类
    /// </summary>
    public static class ISearchQueryExtention
    {
        /// <summary>
        /// 键值缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public static IKeyCache<T> KeyCache<T>(this ISearchQuery<T> searchQuery)
            where T : class, IEntity, new()
        {
            return new KeyCache<T>(searchQuery);
        }

        /// <summary>
        /// 条件缓存
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchQuery"></param>
        /// <returns></returns>
        public static IConditionCache<T> ConditionCache<T>(this ISearchQuery<T> searchQuery)
            where T : class, IEntity, new()
        {
            return new ConditionCache<T>(searchQuery);
        }
    }
}