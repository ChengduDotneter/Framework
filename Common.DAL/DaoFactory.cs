namespace Common.DAL
{
    public static class DaoFactory
    {
        public static ISearchQuery<T> GetSearchSqlSugarQuery<T>(bool codeFirst) where T : class, IEntity, new()
        {
            return SqlSugarDao.GetSlaveDatabase<T>(codeFirst);
        }

        public static IEditQuery<T> GetEditSqlSugarQuery<T>(bool codeFirst) where T : class, IEntity, new()
        {
            return SqlSugarDao.GetMasterDatabase<T>(codeFirst);
        }

        public static ISearchQuery<T> GetSearchIgniteQuery<T>() where T : class, IEntity, new()
        {
            return IgniteDao.GetIgniteSearchQuery<T>();
        }

        public static IEditQuery<T> GetEditIgniteQuery<T>() where T : class, IEntity, new()
        {
            return IgniteDao.GetIgniteEditQuery<T>();
        }
    }
}
