using log4net;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;

namespace Common.DAL
{
    internal static class SqlSugarDao
    {
        private static readonly string m_masterConnectionString;
        private static readonly string m_slaveConnectionString;
        private static readonly object m_lockThis;
        private static readonly ILog m_log;
        private static bool m_initTables;
        private static DbType m_dbType;
        private static string m_currentThread;

        static SqlSugarDao()
        {
            m_dbType = (DbType)Enum.Parse(typeof(DbType), ConfigManager.Configuration.GetSection("DbType").Value);
            m_lockThis = new object();
            m_log = LogHelper.CreateLog("sql", "error");
            m_masterConnectionString = ConfigManager.Configuration.GetConnectionString("MasterConnection");
            m_slaveConnectionString = ConfigManager.Configuration.GetConnectionString("SalveConnection");
        }

        private static SqlSugarClient CreateConnection(string connectionString, bool isShadSanmeThread = false)
        {
            SqlSugarClient sqlSugarClient = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = connectionString,
                DbType = m_dbType,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = isShadSanmeThread,
                //标记该数据库链接是否为线程共享
                IsShardSameThread = isShadSanmeThread
            });

#if OUTPUT_SQL
            sqlSugarClient.Aop.OnLogExecuting = (sql, parameters) =>
            {
                Console.WriteLine(GetSqlLog(sql, parameters));
            };
#endif

            sqlSugarClient.Aop.OnError = (sqlSugarException) =>
            {
                m_log.Error($"message: {sqlSugarException.Message}{Environment.NewLine}stack_trace: {sqlSugarException.StackTrace}{Environment.NewLine}{GetSqlLog(sqlSugarException.Sql, (SugarParameter[])sqlSugarException.Parametres)}");
            };

            return sqlSugarClient;
        }

        private static string GetSqlLog(string sql, SugarParameter[] sugarParameters)
        {
            return string.Format("sql: {1}{0}parameter: {0}{2}",
                                 Environment.NewLine,
                                 sql,
                                 string.Join(Environment.NewLine, sugarParameters.Select(sugarParameter => $"{sugarParameter.ParameterName}: {sugarParameter.Value}")));
        }

        private static void InitTables()
        {
            using (SqlSugarClient masterSqlSugarClient = CreateConnection(m_masterConnectionString))
            using (SqlSugarClient slaveSqlSugarClient = CreateConnection(m_slaveConnectionString))
            {
                Type[] modelTypes = TypeReflector.ReflectType((type) =>
                {
                    if (type.GetInterface(typeof(IEntity).FullName) == null || type.IsInterface || type.IsAbstract)
                        return false;

                    if (type == typeof(IEntity) || type.GetCustomAttribute<IgnoreTableAttribute>() != null)
                        return false;

                    return true;
                });

                masterSqlSugarClient.CodeFirst.InitTables(modelTypes);
                slaveSqlSugarClient.CodeFirst.InitTables(modelTypes);

                foreach (Type item in modelTypes)
                {
                    foreach (PropertyInfo property in item.GetProperties())
                    {
                        ForeignAttribute foreignAttribute = property.GetCustomAttribute<ForeignAttribute>();

                        if (foreignAttribute != null)
                        {
                            int masterCount = Convert.ToInt32(masterSqlSugarClient.Ado.GetScalar(GetCheckForeginSqlExistsSql(item, property)));

                            if (masterCount == 0)
                                masterSqlSugarClient.Ado.ExecuteCommand(GetForeginSql(item, property, foreignAttribute));

                            int slaveCount = Convert.ToInt32(slaveSqlSugarClient.Ado.GetScalar(GetCheckForeginSqlExistsSql(item, property)));

                            if (slaveCount == 0)
                                slaveSqlSugarClient.Ado.ExecuteCommand(GetForeginSql(item, property, foreignAttribute));
                        }
                    }
                }
            }
        }

        private static string GetForeginSql(Type type, PropertyInfo property, ForeignAttribute foreignAttribute)
        {
            if (m_dbType == DbType.MySql)
                return $"ALTER TABLE {type.Name} ADD FOREIGN KEY FK_{type.Name}_{property.Name} ({property.Name}) REFERENCES {foreignAttribute.ForeignTable}({foreignAttribute.ForeignColumn});";

            return $"ALTER TABLE {type.Name} WITH NOCHECK ADD CONSTRAINT FK_{type.Name}_{property.Name} FOREIGN KEY ({property.Name}) REFERENCES {foreignAttribute.ForeignTable}({foreignAttribute.ForeignColumn}) ON DELETE NO ACTION ON UPDATE NO ACTION;";
        }

        private static string GetCheckForeginSqlExistsSql(Type type, PropertyInfo property)
        {
            if (m_dbType == DbType.MySql)
                return $"SELECT COUNT(*) FROM `information_schema`.`KEY_COLUMN_USAGE` WHERE TABLE_NAME = '{type.Name}' and COLUMN_NAME = '{property.Name}' ";

            return $"SELECT COUNT(*) FROM SYS.ALL_OBJECTS WHERE NAME = 'FK_{type.Name}_{property.Name}' AND PARENT_OBJECT_ID = OBJECT_ID( N'[{type.Name}]' )";
        }

        private static class SqlSugarJoinQuery<TLeft, TRight>
            where TLeft : class, IEntity, new()
            where TRight : class, IEntity, new()
        {
            private static readonly ParameterExpression m_left;
            private static readonly ParameterExpression m_right;
            private static readonly ConstantExpression m_joinType;

            public static Expression<Func<TLeft, TRight, JoinQueryInfos>> ConvertJoinExpression(Expression<Func<TLeft, long>> leftExpression, Expression<Func<TRight, long>> rightExpression)
            {
                return Expression.Lambda<Func<TLeft, TRight, JoinQueryInfos>>(
                           Expression.New(typeof(JoinQueryInfos).GetConstructor(new[] { typeof(JoinType), typeof(bool) }), m_joinType, Expression.Equal(leftExpression.RenameParameter(m_left.Name).Body, rightExpression.RenameParameter(m_right.Name).Body)),
                           m_left, m_right);
            }

            public static Expression<Func<TLeft, TRight, TResult>> ConvertExpression<TResult>(Expression<Func<TLeft, TRight, TResult>> expression)
            {
                return expression.RenameParameter(m_left.Name, m_right.Name);
            }

            static SqlSugarJoinQuery()
            {
                m_left = Expression.Parameter(typeof(TLeft), "tleft");
                m_right = Expression.Parameter(typeof(TRight), "tright");
                m_joinType = Expression.Constant(JoinType.Inner, typeof(JoinType));
            }
        }

        private class SqlSugarDaoInstance<T> : IEditQuery<T>, ISearchQuery<T>
            where T : class, IEntity, new()
        {
            public void Delete(params long[] ids)
            {
                if (ids.Length == 0)
                    return;

                CreateConnection(m_masterConnectionString, true).Deleteable<T>(ids).ExecuteCommand();
            }

            public void Insert(params T[] datas)
            {
                if (datas.Length == 0)
                    return;

                CreateConnection(m_masterConnectionString, true).Insertable(datas).ExecuteCommand();
            }

            public void Merge(params T[] datas)
            {
                if (datas.Length == 0)
                    return;

                CreateConnection(m_masterConnectionString, true).Saveable(new List<T>(datas)).ExecuteCommand();
            }

            public void Update(T data, params string[] ignoreColumns)
            {
                if (data == null)
                    return;

                CreateConnection(m_masterConnectionString, true).Updateable(data).IgnoreColumns(ignoreColumns).ExecuteCommand();
            }

            public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression)
            {
                CreateConnection(m_masterConnectionString, true).Updateable<T>()
                                                                .Where(predicate)
                                                                .SetColumns(updateExpression)
                                                                .ExecuteCommand();
            }

            public int Count(string queryWhere)
            {
                if (m_currentThread == Thread.CurrentThread.ManagedThreadId.ToString())
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString, true))
                    {
                        var query = sqlSugarClient.Queryable<T>();

                        if (!string.IsNullOrWhiteSpace(queryWhere))
                            query.Where(queryWhere);

                        return query.Count();
                    }
                }
                else
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString))
                    {
                        var query = sqlSugarClient.Queryable<T>();

                        if (!string.IsNullOrWhiteSpace(queryWhere))
                            query.Where(queryWhere);

                        return query.Count();
                    }
                }
            }

            public int Count(Expression<Func<T, bool>> predicate = null)
            {
                if (m_currentThread == Thread.CurrentThread.ManagedThreadId.ToString())
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString, true))
                    {
                        ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        return query.Count();
                    }
                }
                else
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString))
                    {
                        ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        return query.Count();
                    }
                }
            }

            public T Get(long id)
            {
                if (m_currentThread == Thread.CurrentThread.ManagedThreadId.ToString())
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString, true))
                    {
                        return sqlSugarClient.Queryable<T>().InSingle(id);
                    }
                }
                else
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString))
                    {
                        return sqlSugarClient.Queryable<T>().InSingle(id);
                    }
                }
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue)
            {
                if (m_currentThread == Thread.CurrentThread.ManagedThreadId.ToString())
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString, true))
                    {
                        ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        if (queryOrderBies != null)
                            foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                                query = query.OrderBy(queryOrderBy.Expression, GetOrderByType(queryOrderBy.OrderByType));

                        return query.Skip(startIndex).Take(count).ToArray();
                    }
                }
                else
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString))
                    {
                        ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        if (queryOrderBies != null)
                            foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                                query = query.OrderBy(queryOrderBy.Expression, GetOrderByType(queryOrderBy.OrderByType));

                        return query.Skip(startIndex).Take(count).ToArray();
                    }
                }
            }

            public IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                             Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                             IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                             int startIndex = 0,
                                                                             int count = int.MaxValue)
                where TJoinTable : class, IEntity, new()
            {
                if (m_currentThread == Thread.CurrentThread.ManagedThreadId.ToString())
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString, true))
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarClient.Queryable(
                            SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        if (queryOrderBies != null)
                            foreach (QueryOrderBy<T, TJoinTable> queryOrderBy in queryOrderBies)
                                query = query.OrderBy(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(queryOrderBy.Expression), GetOrderByType(queryOrderBy.OrderByType));

                        foreach (var data in query.Select((tleft, tright) => new { tleft, tright }).Skip(startIndex).Take(count).ToArray())
                            yield return new JoinResult<T, TJoinTable>(data.tleft, data.tright);
                    }
                }
                else
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString))
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarClient.Queryable(
                            SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        if (queryOrderBies != null)
                            foreach (QueryOrderBy<T, TJoinTable> queryOrderBy in queryOrderBies)
                                query = query.OrderBy(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(queryOrderBy.Expression), GetOrderByType(queryOrderBy.OrderByType));

                        foreach (var data in query.Select((tleft, tright) => new { tleft, tright }).Skip(startIndex).Take(count).ToArray())
                            yield return new JoinResult<T, TJoinTable>(data.tleft, data.tright);
                    }
                }
            }

            public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                         Expression<Func<T, TJoinTable, bool>> predicate = null)
                where TJoinTable : class, IEntity, new()
            {
                if (m_currentThread == Thread.CurrentThread.ManagedThreadId.ToString())
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString, true))
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarClient.Queryable(SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        return query.Count();
                    }
                }
                else
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString))
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarClient.Queryable(SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        return query.Count();
                    }
                }
            }

            public IEnumerable<T> Search(string queryWhere, string orderByFields = null, int startIndex = 0, int count = int.MaxValue)
            {
                if (m_currentThread == Thread.CurrentThread.ManagedThreadId.ToString())
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString, true))
                    {
                        var query = sqlSugarClient.Queryable<T>().Where(queryWhere);

                        if (!string.IsNullOrWhiteSpace(orderByFields))
                            query.OrderBy(orderByFields);

                        return query.Skip(startIndex).Take(count).ToArray();
                    }
                }
                else
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString))
                    {
                        var query = sqlSugarClient.Queryable<T>().Where(queryWhere);

                        if (!string.IsNullOrWhiteSpace(orderByFields))
                            query.OrderBy(orderByFields);

                        return query.Skip(startIndex).Take(count).ToArray();
                    }
                }
            }

            private static SqlSugar.OrderByType GetOrderByType(OrderByType orderByType)
            {
                switch (orderByType)
                {
                    case OrderByType.Asc:
                        return SqlSugar.OrderByType.Asc;
                    case OrderByType.Desc:
                        return SqlSugar.OrderByType.Desc;
                    default:
                        throw new Exception();
                }
            }

            public ITransaction BeginTransaction()
            {
                m_currentThread = Thread.CurrentThread.ManagedThreadId.ToString(); //获取当前开启事物的线程ID

                return new SqlSugerTranscation();
            }

            public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null)
            {
                if (m_currentThread == Thread.CurrentThread.ManagedThreadId.ToString())
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString, true))
                    {
                        return sqlSugarClient.Ado.SqlQuery<ExpandoObject>(sql, parameters);
                    }
                }
                else
                {
                    using (SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString))
                    {
                        return sqlSugarClient.Ado.SqlQuery<ExpandoObject>(sql, parameters);
                    }
                }
            }
        }

        #region SqlSuger事务处理类

        private class SqlSugerTranscation : ITransaction
        {
            private SqlSugarClient m_sqlSugarClient;

            public SqlSugerTranscation()
            {
                m_sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                m_sqlSugarClient.BeginTran();
            }

            public object Context()
            {
                return m_sqlSugarClient.Context;
            }

            public void Dispose()
            {
                m_sqlSugarClient.Dispose();
            }

            public void Rollback()
            {
                m_sqlSugarClient.RollbackTran();
            }

            public void Submit()
            {
                m_sqlSugarClient.CommitTran();
            }
        }

        #endregion 

        internal static ISearchQuery<T> GetSlaveDatabase<T>(bool codeFirst)
            where T : class, IEntity, new()
        {
            if (codeFirst && !m_initTables)
            {
                lock (m_lockThis)
                {
                    if (!m_initTables)
                    {
                        InitTables();
                        m_initTables = true;
                    }
                }
            }

            return new SqlSugarDaoInstance<T>();
        }

        internal static IEditQuery<T> GetMasterDatabase<T>(bool codeFirst)
            where T : class, IEntity, new()
        {
            if (codeFirst && !m_initTables)
            {
                lock (m_lockThis)
                {
                    if (!m_initTables)
                    {
                        InitTables();
                        m_initTables = true;
                    }
                }
            }

            return new SqlSugarDaoInstance<T>();
        }

    }
}
