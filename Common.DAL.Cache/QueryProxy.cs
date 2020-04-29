namespace Common.DAL.Cache
{
    public static class IEditQueryExtention
    {
        public static IEditQuery<T> Cache<T>(this IEditQuery<T> editQuery) where T : class, IEntity, new()
        {
            return new EditQueryProxy<T>(editQuery);
        }
    }

    public static class ISearchQueryExtention
    {
        public static IKeyCache<T> KeyCache<T>(this ISearchQuery<T> searchQuery)
            where T : class, IEntity, new()
        {
            return new KeyCache<T>(searchQuery);
        }

        public static IConditionCache<T> ConditionCache<T>(this ISearchQuery<T> searchQuery)
            where T : class, IEntity, new()
        {
            return new ConditionCache<T>(searchQuery);
        }
    }
}
