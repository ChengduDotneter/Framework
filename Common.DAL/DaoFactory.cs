﻿namespace Common.DAL
{
    /// <summary>
    /// Dao工厂类
    /// </summary>
    public static class DaoFactory
    {
        /// <summary>
        /// 获取查询的MongoDB操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
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
        /// <param name="codeFirst"></param>
        /// <returns></returns>
        public static ISearchQuery<T> GetSearchLinq2DBQuery<T>(bool codeFirst) where T : class, IEntity, new()
        {
            return Linq2DBDao.GetLinq2DBSearchQuery<T>(codeFirst);
        }

        /// <summary>
        /// 获取修改的Linq2DB操作实体
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="codeFirst"></param>
        /// <returns></returns>
        public static IEditQuery<T> GetEditLinq2DBQuery<T>(bool codeFirst) where T : class, IEntity, new()
        {
            return Linq2DBDao.GetLinq2DBEditQuery<T>(codeFirst);
        }
    }
}