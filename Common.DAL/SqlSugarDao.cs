using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.DAL.Transaction;
using log4net;
using Microsoft.Extensions.Configuration;
using SqlSugar;

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

        static SqlSugarDao()
        {
            m_dbType = (DbType)Enum.Parse(typeof(DbType), ConfigManager.Configuration.GetSection("DbType").Value);
            m_lockThis = new object();
            m_log = LogHelper.CreateLog("sql", "error");
            m_masterConnectionString = ConfigManager.Configuration.GetConnectionString("MasterConnection");
            m_slaveConnectionString = ConfigManager.Configuration.GetConnectionString("SalveConnection");
        }

        private static SqlSugarClient CreateConnection(string connectionString)
        {
            SqlSugarClient sqlSugarClient = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = connectionString,
                DbType = m_dbType,
                InitKeyType = InitKeyType.Attribute,
                IsAutoCloseConnection = false
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
            }
        }

        private static void Apply<TResource>(ITransaction transaction, out bool inTransaction) where TResource : class, IEntity
        {
            if (transaction != null && !(transaction is SqlSugarTranscation))
                throw new DealException("错误的事务对象。");

            SqlSugarTranscation sqlSugarTrtanscation = transaction as SqlSugarTranscation;

            if (sqlSugarTrtanscation != null)
            {
                Type table = typeof(TResource);

                if (!sqlSugarTrtanscation.TransactionTables.Contains(table))
                {
                    bool status = TransactionResourceHelper.ApplayResource(table, sqlSugarTrtanscation.Identity, sqlSugarTrtanscation.Weight);

                    if (status)
                        sqlSugarTrtanscation.TransactionTables.Add(table);
                    else
                        throw new DealException($"申请事务资源{table.FullName}失败。");
                }
            }

            inTransaction = sqlSugarTrtanscation != null;
        }

        private static void Release(string identity)
        {
            TransactionResourceHelper.ReleaseResource(identity);
        }

        private class SqlSugarTaskScheduler : TaskScheduler, IDisposable
        {
            private const int THREAD_TIME_SPAN = 1;
            private Thread m_thread;
            private BlockingCollection<Task> m_tasks;
            private bool m_running;
            private CancellationTokenSource m_cancellationTokenSource;
            public SqlSugarClient SqlSugarClient { get; private set; }

            private void DoWork()
            {
                bool isOpen = false;

                while (m_running || m_tasks.Count > 0)
                {
                    try
                    {
                        Task task = m_tasks.Take(m_cancellationTokenSource.Token);

                        if (!isOpen)
                        {
                            SqlSugarClient = CreateConnection(m_masterConnectionString);
                            SqlSugarClient.BeginTran();
                            isOpen = true;
                        }

                        TryExecuteTask(task);
                    }
                    catch
                    {
                        continue;
                    }
                }

                m_tasks.Dispose();

                if (isOpen)
                    SqlSugarClient.Dispose();
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return m_tasks;
            }

            protected override void QueueTask(Task task)
            {
                m_tasks.Add(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false;
            }

            public void Dispose()
            {
                m_running = false;

                while (m_tasks.Count > 0)
                    Thread.Sleep(THREAD_TIME_SPAN);

                m_cancellationTokenSource.Cancel(false);
                m_cancellationTokenSource.Dispose();
            }

            public SqlSugarTaskScheduler()
            {
                m_cancellationTokenSource = new CancellationTokenSource();
                m_running = true;
                m_tasks = new BlockingCollection<Task>();
                m_thread = new Thread(DoWork);
                m_thread.IsBackground = false;
                m_thread.Name = $"SQLSUGAR_TRANSACTION_THREAD_{Guid.NewGuid():D}";
                m_thread.Start();
            }
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
            public void Delete(ITransaction transaction = null, params long[] ids)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (ids.Length == 0)
                    return;

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        sqlSugarClient.Deleteable<T>(ids).ExecuteCommand();
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    sqlSugarTranscation.Do(() =>
                    {
                        sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Deleteable<T>(ids).ExecuteCommand();
                    }).Wait();
                }
            }

            public void Insert(ITransaction transaction = null, params T[] datas)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (datas.Length == 0)
                    return;

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        sqlSugarClient.Insertable(datas).ExecuteCommand();
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    sqlSugarTranscation.Do(() =>
                    {
                        sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Insertable(datas).ExecuteCommand();
                    }).Wait();
                }
            }

            public void Merge(ITransaction transaction = null, params T[] datas)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (datas.Length == 0)
                    return;

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        sqlSugarClient.Saveable(new List<T>(datas)).ExecuteCommand();
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    sqlSugarTranscation.Do(() =>
                    {
                        sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Saveable(new List<T>(datas)).ExecuteCommand();
                    }).Wait();
                }
            }

            public void Update(T data, ITransaction transaction = null, params string[] ignoreColumns)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (data == null)
                    return;

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        sqlSugarClient.Updateable(data).IgnoreColumns(ignoreColumns).ExecuteCommand();
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    sqlSugarTranscation.Do(() =>
                    {
                        sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Updateable(data).IgnoreColumns(ignoreColumns).ExecuteCommand();
                    }).Wait();
                }
            }

            public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression, ITransaction transaction = null)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        sqlSugarClient.Updateable<T>()
                                      .Where(predicate)
                                      .SetColumns(updateExpression)
                                      .ExecuteCommand();
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    sqlSugarTranscation.Do(() =>
                    {
                        sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Updateable<T>()
                                                                .Where(predicate)
                                                                .SetColumns(updateExpression)
                                                                .ExecuteCommand();
                    }).Wait();
                }
            }

            public int Count(string queryWhere, Dictionary<string, object> parameters = null, ITransaction transaction = null)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                        if (!string.IsNullOrWhiteSpace(queryWhere) && parameters != null)
                            query.Where(queryWhere, parameters);
                        else if (!string.IsNullOrWhiteSpace(queryWhere))
                            query.Where(queryWhere);

                        return query.Count();
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    return sqlSugarTranscation.Do(() =>
                    {
                        ISugarQueryable<T> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>();

                        if (!string.IsNullOrWhiteSpace(queryWhere) && parameters != null)
                            query.Where(queryWhere, parameters);
                        else if (!string.IsNullOrWhiteSpace(queryWhere))
                            query.Where(queryWhere);

                        return query.Count();
                    }).Result;
                }
            }

            public int Count(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        return query.Count();
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    return sqlSugarTranscation.Do(() =>
                    {
                        ISugarQueryable<T> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        return query.Count();
                    }).Result;
                }
            }

            public T Get(long id, ITransaction transaction = null)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        return sqlSugarClient.Queryable<T>().InSingle(id);
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    return sqlSugarTranscation.Do(() =>
                    {
                        return sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>().InSingle(id);
                    }).Result;
                }
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         ITransaction transaction = null)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
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
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    return sqlSugarTranscation.Do(() =>
                    {
                        ISugarQueryable<T> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        if (queryOrderBies != null)
                            foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                                query = query.OrderBy(queryOrderBy.Expression, GetOrderByType(queryOrderBy.OrderByType));

                        return query.Skip(startIndex).Take(count).ToArray();
                    }).Result;
                }
            }

            public IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                             Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                             IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                             int startIndex = 0,
                                                                             int count = int.MaxValue,
                                                                             ITransaction transaction = null)
                where TJoinTable : class, IEntity, new()
            {
                Apply<T>(transaction, out bool inTransaction);
                Apply<TJoinTable>(transaction, out bool _);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarClient.Queryable(
                                    SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        if (queryOrderBies != null)
                            foreach (QueryOrderBy<T, TJoinTable> queryOrderBy in queryOrderBies)
                                query = query.OrderBy(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(queryOrderBy.Expression), GetOrderByType(queryOrderBy.OrderByType));

                        return query.Select((tleft, tright) => new { tleft, tright }).Skip(startIndex).Take(count).ToArray().Select(item => new JoinResult<T, TJoinTable>(item.tleft, item.tright));
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    return sqlSugarTranscation.Do(() =>
                     {
                         ISugarQueryable<T, TJoinTable> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable(
                                        SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                         if (predicate != null)
                             query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                         if (queryOrderBies != null)
                             foreach (QueryOrderBy<T, TJoinTable> queryOrderBy in queryOrderBies)
                                 query = query.OrderBy(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(queryOrderBy.Expression), GetOrderByType(queryOrderBy.OrderByType));

                         return query.Select((tleft, tright) => new { tleft, tright }).Skip(startIndex).Take(count).ToArray().Select(item => new JoinResult<T, TJoinTable>(item.tleft, item.tright));

                     }).Result;
                }
            }

            public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                         Expression<Func<T, TJoinTable, bool>> predicate = null,
                                         ITransaction transaction = null)
                where TJoinTable : class, IEntity, new()
            {
                Apply<T>(transaction, out bool inTransaction);
                Apply<TJoinTable>(transaction, out bool _);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarClient.Queryable(SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        return query.Count();
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    return sqlSugarTranscation.Do(() =>
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable(SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        return query.Count();
                    }).Result;
                }
            }

            public IEnumerable<T> Search(string queryWhere,
                                         Dictionary<string, object> parameters = null,
                                         string orderByFields = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         ITransaction transaction = null)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
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
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    return sqlSugarTranscation.Do(() =>
                    {
                        ISugarQueryable<T> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>();

                        if (parameters != null)
                            query.Where(queryWhere, parameters);
                        else
                            query.Where(queryWhere);

                        if (!string.IsNullOrWhiteSpace(orderByFields))
                            query.OrderBy(orderByFields);

                        return query.Skip(startIndex).Take(count).ToArray();
                    }).Result;
                }
            }

            public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null, ITransaction transaction = null)
            {
                Apply<T>(transaction, out bool inTransaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        IEnumerable<IDictionary<string, object>> datas = sqlSugarClient.Ado.SqlQuery<ExpandoObject>(sql, parameters);

                        return datas.Select(data =>
                        {
                            IDictionary<string, object> result = new Dictionary<string, object>();

                            foreach (KeyValuePair<string, object> item in data)
                                result.Add(item.Key.ToUpper(), item.Value);

                            return result;
                        });
                    }
                    finally
                    {
                        if (sqlSugarClient != null)
                            sqlSugarClient.Dispose();
                    }
                }
                else
                {
                    SqlSugarTranscation sqlSugarTranscation = (SqlSugarTranscation)transaction;

                    return sqlSugarTranscation.Do(() =>
                    {
                        IEnumerable<IDictionary<string, object>> datas = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Ado.SqlQuery<ExpandoObject>(sql, parameters);

                        return datas.Select(data =>
                        {
                            IDictionary<string, object> result = new Dictionary<string, object>();

                            foreach (KeyValuePair<string, object> item in data)
                                result.Add(item.Key.ToUpper(), item.Value);

                            return result;
                        });
                    }).Result;
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
                return new SqlSugarTranscation(weight);
            }
        }

        #region SqlSuger事务处理类

        private class SqlSugarTranscation : ITransaction
        {
            public HashSet<Type> TransactionTables { get; }
            public string Identity { get; }
            public int Weight { get; }
            public SqlSugarTaskScheduler SqlSugarTaskScheduler { get; }

            public SqlSugarTranscation(int weight)
            {
                Identity = Guid.NewGuid().ToString("D");
                Weight = weight;
                TransactionTables = new HashSet<Type>();
                SqlSugarTaskScheduler = new SqlSugarTaskScheduler();
            }

            public object Context()
            {
                return SqlSugarTaskScheduler;
            }

            public void Dispose()
            {
                SqlSugarTaskScheduler.Dispose();
                Release(Identity);
            }

            public void Rollback()
            {
                Do(() => { SqlSugarTaskScheduler.SqlSugarClient.RollbackTran(); }).Wait();
            }

            public void Submit()
            {
                Do(() => { SqlSugarTaskScheduler.SqlSugarClient.CommitTran(); }).Wait();
            }

            public async Task<T> Do<T>(Func<T> func)
            {
                return await Task.Factory.StartNew(func, CancellationToken.None, TaskCreationOptions.None, SqlSugarTaskScheduler);
            }

            public async Task Do(Action action)
            {
                await Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, SqlSugarTaskScheduler);
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