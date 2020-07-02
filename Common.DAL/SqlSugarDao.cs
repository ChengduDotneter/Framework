using Common.DAL.Transaction;
using log4net;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Policy;
using System.Threading;

namespace Common.DAL
{
    internal static class SqlSugarDao
    {
        private static readonly string m_masterConnectionString;
        private static readonly string m_slaveConnectionString;
        private static readonly object m_lockThis;
        private static readonly IDictionary<int, SqlSugerTranscation> m_transactions;
        private static readonly ILog m_log;
        private static bool m_initTables;
        private static DbType m_dbType;

        static SqlSugarDao()
        {
            m_dbType = (DbType)Enum.Parse(typeof(DbType), ConfigManager.Configuration.GetSection("DbType").Value);
            m_lockThis = new object();
            m_log = LogHelper.CreateLog("sql", "error");
            m_masterConnectionString = ConfigManager.Configuration.GetConnectionString("MasterConnection");
            m_slaveConnectionString = ConfigManager.Configuration.GetConnectionString("SalveConnection");
            m_transactions = new Dictionary<int, SqlSugerTranscation>();
        }

        private static SqlSugarClient CreateConnection(string connectionString, bool isShadSanmeThread = false)
        {
            LocalDataStoreSlot localDataStoreSlot = Thread.GetNamedDataSlot("SqlSugarClient");

            if (localDataStoreSlot == null)
                localDataStoreSlot = Thread.AllocateNamedDataSlot("SqlSugarClient");

            if (Thread.GetData(localDataStoreSlot) == null)
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

                Thread.SetData(localDataStoreSlot, sqlSugarClient);
            }

            return (SqlSugarClient)Thread.GetData(localDataStoreSlot);
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
            }
        }

        private static void Apply<TResource>(out bool inTransaction) where TResource : class, IEntity
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            SqlSugerTranscation sqlSugerTranscation = null;

            lock (m_transactions)
                if (m_transactions.ContainsKey(id))
                    sqlSugerTranscation = m_transactions[id];

            if (sqlSugerTranscation != null)
            {
                Type table = typeof(TResource);

                if (!sqlSugerTranscation.TransactionTables.Contains(table))
                {
                    bool status = TransactionResourceHelper.ApplayResource(table, sqlSugerTranscation.Identity, sqlSugerTranscation.Weight);

                    if (status)
                        sqlSugerTranscation.TransactionTables.Add(table);
                    else
                        throw new DealException($"申请事务资源{table.FullName}失败。");
                }
            }

            inTransaction = sqlSugerTranscation != null;
        }

        private static async void Release(long identity)
        {
            await TransactionResourceHelper.ReleaseResourceAsync(identity);
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
                Apply<T>(out _);

                if (ids.Length == 0)
                    return;

                CreateConnection(m_masterConnectionString, true).Deleteable<T>(ids).ExecuteCommand();
            }

            public void Insert(params T[] datas)
            {
                Apply<T>(out _);

                if (datas.Length == 0)
                    return;

                CreateConnection(m_masterConnectionString, true).Insertable(datas).ExecuteCommand();
            }

            public void Merge(params T[] datas)
            {
                Apply<T>(out _);

                if (datas.Length == 0)
                    return;

                CreateConnection(m_masterConnectionString, true).Saveable(new List<T>(datas)).ExecuteCommand();
            }

            public void Update(T data, params string[] ignoreColumns)
            {
                Apply<T>(out _);

                if (data == null)
                    return;

                CreateConnection(m_masterConnectionString, true).Updateable(data).IgnoreColumns(ignoreColumns).ExecuteCommand();
            }

            public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression)
            {
                Apply<T>(out _);

                CreateConnection(m_masterConnectionString, true).Updateable<T>()
                                                                .Where(predicate)
                                                                .SetColumns(updateExpression)
                                                                .ExecuteCommand();
            }

            public int Count(string queryWhere, Dictionary<string, object> parameters = null)
            {
                Apply<T>(out bool inTransaction);
                SqlSugarClient sqlSugarClient = null;

                try
                {
                    if (inTransaction)
                        sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                    else
                        sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                    if (!string.IsNullOrWhiteSpace(queryWhere) && parameters != null)
                        query.Where(queryWhere, parameters);
                    else if (!string.IsNullOrWhiteSpace(queryWhere))
                        query.Where(queryWhere);

                    return query.Count();
                }
                finally
                {
                    if (sqlSugarClient != null && !inTransaction)
                        sqlSugarClient.Dispose();
                }
            }

            public int Count(Expression<Func<T, bool>> predicate = null)
            {
                Apply<T>(out bool inTransaction);
                SqlSugarClient sqlSugarClient = null;

                try
                {
                    if (inTransaction)
                        sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                    else
                        sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                    if (predicate != null)
                        query = query.Where(predicate);

                    return query.Count();
                }
                finally
                {
                    if (sqlSugarClient != null && !inTransaction)
                        sqlSugarClient.Dispose();
                }
            }

            public T Get(long id)
            {
                Apply<T>(out bool inTransaction);
                SqlSugarClient sqlSugarClient = null;

                try
                {
                    if (inTransaction)
                        sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                    else
                        sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    return sqlSugarClient.Queryable<T>().InSingle(id);
                }
                finally
                {
                    if (sqlSugarClient != null && !inTransaction)
                        sqlSugarClient.Dispose();
                }
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue)
            {
                Apply<T>(out bool inTransaction);
                SqlSugarClient sqlSugarClient = null;

                try
                {
                    if (inTransaction)
                        sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                    else
                        sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                    if (predicate != null)
                        query = query.Where(predicate);

                    if (queryOrderBies != null)
                        foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                            query = query.OrderBy(queryOrderBy.Expression, GetOrderByType(queryOrderBy.OrderByType));

                    return query.Skip(startIndex).Take(count).ToArray();
                }
                finally
                {
                    if (sqlSugarClient != null && !inTransaction)
                        sqlSugarClient.Dispose();
                }
            }

            public IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                             Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                             IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                             int startIndex = 0,
                                                                             int count = int.MaxValue)
                where TJoinTable : class, IEntity, new()
            {
                Apply<T>(out bool inTransaction);
                Apply<TJoinTable>(out bool _);
                SqlSugarClient sqlSugarClient = null;

                try
                {
                    if (inTransaction)
                        sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                    else
                        sqlSugarClient = CreateConnection(m_slaveConnectionString);

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
                finally
                {
                    if (sqlSugarClient != null && !inTransaction)
                        sqlSugarClient.Dispose();
                }
            }

            public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                         Expression<Func<T, TJoinTable, bool>> predicate = null)
                where TJoinTable : class, IEntity, new()
            {
                Apply<T>(out bool inTransaction);
                Apply<TJoinTable>(out bool _);
                SqlSugarClient sqlSugarClient = null;

                try
                {
                    if (inTransaction)
                        sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                    else
                        sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    ISugarQueryable<T, TJoinTable> query = sqlSugarClient.Queryable(SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                    if (predicate != null)
                        query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                    return query.Count();
                }
                finally
                {
                    if (sqlSugarClient != null && !inTransaction)
                        sqlSugarClient.Dispose();
                }
            }

            public IEnumerable<T> Search(string queryWhere, Dictionary<string, object> parameters = null, string orderByFields = null, int startIndex = 0, int count = int.MaxValue)
            {
                Apply<T>(out bool inTransaction);
                SqlSugarClient sqlSugarClient = null;

                try
                {
                    if (inTransaction)
                        sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                    else
                        sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                    if (parameters != null)
                        query.Where(queryWhere, parameters);
                    else
                        query.Where(queryWhere);

                    if (!string.IsNullOrWhiteSpace(orderByFields))
                        query.OrderBy(orderByFields);

                    return query.Skip(startIndex).Take(count).ToArray();
                }
                finally
                {
                    if (sqlSugarClient != null && !inTransaction)
                        sqlSugarClient.Dispose();
                }
            }

            public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null)
            {
                Apply<T>(out bool inTransaction);
                SqlSugarClient sqlSugarClient = null;

                try
                {
                    if (inTransaction)
                        sqlSugarClient = CreateConnection(m_masterConnectionString, true);
                    else
                        sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    IEnumerable<IDictionary<string, object>> datas = sqlSugarClient.Ado.SqlQuery<ExpandoObject>(sql, parameters);

                    foreach (IDictionary<string, object> data in datas)
                    {
                        IDictionary<string, object> result = new Dictionary<string, object>();

                        foreach (KeyValuePair<string, object> item in data)
                            result.Add(item.Key.ToUpper(), item.Value);

                        yield return result;
                    }
                }
                finally
                {
                    if (sqlSugarClient != null && !inTransaction)
                        sqlSugarClient.Dispose();
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

            public ITransaction BeginTransaction(int weight = 0)
            {
                int id = Thread.CurrentThread.ManagedThreadId;

                lock (m_transactions)
                {
                    if (!m_transactions.ContainsKey(id))
                        m_transactions.Add(id, new SqlSugerTranscation(weight));
                    else
                        throw new Exception("当前线程存在未释放的事务。");
                }

                return m_transactions[id];
            }
        }

        #region SqlSuger事务处理类

        private class SqlSugerTranscation : ITransaction
        {
            private SqlSugarClient m_sqlSugarClient;
            public HashSet<Type> TransactionTables { get; }
            public long Identity { get; }
            public int Weight { get; }

            public SqlSugerTranscation(int weight)
            {
                Identity = IDGenerator.NextID();
                Weight = weight;
                TransactionTables = new HashSet<Type>();
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

                int id = Thread.CurrentThread.ManagedThreadId;

                lock (m_transactions)
                    m_transactions.Remove(id);

                Release(Identity);
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

        #endregion SqlSuger事务处理类

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