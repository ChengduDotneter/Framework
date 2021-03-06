using Common.Log;

namespace Common.DAL
{
    /// <summary>
    /// Dao工厂类
    /// </summary>
    public static class DaoFactory
    {
        /// <summary>
        /// 日志处理
        /// </summary>
        public static ILogHelper LogHelper { get; }

        /// <summary>
        /// 获取查询的MongoDB操作实体
        /// </summary>
        /// <typeparam name="T">要查询的实体类</typeparam>
        /// <returns></returns>
        public static ISearchQuery<T> GetSearchMongoDBQuery<T>() where T : class, IEntity, new()
        {
            return MongoDBDao.GetMongoDBSearchQuery<T>();
        }

        /// <summary>
        /// 获取修改的MongoDB操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEditQuery<T> GetEditMongoDBQuery<T>() where T : class, IEntity, new()
        {
            return MongoDBDao.GetMongoDBEditQuery<T>();
        }

        /// <summary>
        /// 获取查询的Linq2DB操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ISearchQuery<T> GetSearchLinq2DBQuery<T>() where T : class, IEntity, new()
        {
            return Linq2DBDao.GetLinq2DBSearchQuery<T>();
        }

        /// <summary>
        /// 获取修改的Linq2DB操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEditQuery<T> GetEditLinq2DBQuery<T>() where T : class, IEntity, new()
        {
            return Linq2DBDao.GetLinq2DBEditQuery<T>();
        }

        /// <summary>
        /// 获取LinqToDB的数据库连接资源上下文
        /// </summary>
        /// <returns></returns>
        public static IDBResourceContent GetLinq2DBResourceContent()
        {
            return Linq2DBDao.GetDBResourceContent();
        }

        /// <summary>
        /// 获取MongoDb的数据库连接资源上下文
        /// </summary>
        /// <returns></returns>
        public static IDBResourceContent GetMongoDbResourceContent()
        {
            return MongoDBDao.GetDBResourceContent();
        }

        /// <summary>
        /// 创建Linq2DB表
        /// </summary>
        /// <returns></returns>
        public static ICreateTableQuery GetLinq2DBCreateTableQuery()
        {
            return Linq2DBDao.GetLinq2DBCreateTableQueryInstance();
        }

        /// <summary>
        /// 创建MongDB表
        /// </summary>
        /// <returns></returns>
        public static ICreateTableQuery CreateMongDbCreateTableQuery()
        {
            return MongoDBDao.GetMongoDBCreateTableQueryInstance();
        }

        static DaoFactory()
        {
            LogHelper = LogHelperFactory.GetDefaultLogHelper();
        }
    }
}