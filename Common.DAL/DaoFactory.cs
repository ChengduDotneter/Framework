namespace Common.DAL
{
    /// <summary>
    /// Dao工厂类
    /// </summary>
    public static class DaoFactory
    {
        /// <summary>
        /// 获取SlaveDatabase的sqlsugar操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="codeFirst"></param>
        /// <returns></returns>
        public static ISearchQuery<T> GetSearchSqlSugarQuery<T>(bool codeFirst) where T : class, IEntity, new()
        {
            return SqlSugarDao.GetSlaveDatabase<T>(codeFirst);
        }

        /// <summary>
        /// 获取MasterDatabase的sqlsugar操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="codeFirst"></param>
        /// <returns></returns>
        public static IEditQuery<T> GetEditSqlSugarQuery<T>(bool codeFirst) where T : class, IEntity, new()
        {
            return SqlSugarDao.GetMasterDatabase<T>(codeFirst);
        }

        /// <summary>
        /// 获取查询的Ignite操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ISearchQuery<T> GetSearchIgniteQuery<T>() where T : class, IEntity, new()
        {
            return IgniteDao.GetIgniteSearchQuery<T>();
        }

        /// <summary>
        /// 获取修改的Ignite操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEditQuery<T> GetEditIgniteQuery<T>() where T : class, IEntity, new()
        {
            return IgniteDao.GetIgniteEditQuery<T>();
        }
    }
}
