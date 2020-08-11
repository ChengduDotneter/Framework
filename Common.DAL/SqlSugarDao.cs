using Common.DAL.Transaction;
using Common.Log;
using Microsoft.Extensions.Configuration;
using SqlSugar;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Common.DAL
{
    internal static class SqlSugarDao
    {
        private static readonly string m_masterConnectionString;
        private static readonly string m_slaveConnectionString;
        private static readonly object m_lockThis;
        private static readonly ILogHelper m_logHelper;
        private static bool m_initTables;
        private static DbType m_dbType;

        static SqlSugarDao()
        {
            m_dbType = (DbType)Enum.Parse(typeof(DbType), ConfigManager.Configuration.GetSection("DbType").Value);
            m_lockThis = new object();
            m_logHelper = LogHelperFactory.GetKafkaLogHelper();
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
                m_logHelper.SqlError(sqlSugarException.Sql, sqlSugarException.Message, GetParametersLog((SugarParameter[])sqlSugarException.Parametres));
            };

            return sqlSugarClient;
        }

        private static string GetSqlLog(string sql, SugarParameter[] sugarParameters)
        {
            return string.Format("sql: {1}{0}parameter: {0}{2}",
                                 Environment.NewLine,
                                 sql,
                                 GetParametersLog(sugarParameters));
        }

        private static string GetParametersLog(SugarParameter[] sugarParameters)
        {
            return string.Join(Environment.NewLine, sugarParameters.Select(sugarParameter => $"{sugarParameter.ParameterName}: {sugarParameter.Value}"));
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

        private static bool Apply<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            if (transaction != null && !(transaction is SqlSugarTranscation))
                throw new DealException("错误的事务对象。");

            SqlSugarTranscation sqlSugarTrtanscation = transaction as SqlSugarTranscation;

            if (sqlSugarTrtanscation != null)
            {
                Type table = typeof(TResource);

                if (!sqlSugarTrtanscation.TransactionTables.Contains(table))
                {
                    if (TransactionResourceHelper.ApplayResource(table, sqlSugarTrtanscation.Identity, sqlSugarTrtanscation.Weight))
                        sqlSugarTrtanscation.TransactionTables.Add(table);
                    else
                        throw new DealException($"申请事务资源{table.FullName}失败。");
                }
            }

            return sqlSugarTrtanscation != null;
        }

        private static async Task<bool> ApplyAsync<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            if (transaction != null && !(transaction is SqlSugarTranscation))
                throw new DealException("错误的事务对象。");

            SqlSugarTranscation sqlSugarTrtanscation = transaction as SqlSugarTranscation;

            if (sqlSugarTrtanscation != null)
            {
                Type table = typeof(TResource);

                if (!sqlSugarTrtanscation.TransactionTables.Contains(table))
                {
                    if (await TransactionResourceHelper.ApplayResourceAsync(table, sqlSugarTrtanscation.Identity, sqlSugarTrtanscation.Weight))
                        sqlSugarTrtanscation.TransactionTables.Add(table);
                    else
                        throw new DealException($"申请事务资源{table.FullName}失败。");
                }
            }

            return sqlSugarTrtanscation != null;
        }

        private static async void Release(string identity)
        {
            await TransactionResourceHelper.ReleaseResourceAsync(identity);
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
                if (m_running)
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
                bool inTransaction = Apply<T>(transaction);

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

            public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (ids.Length == 0)
                    return;

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        await sqlSugarClient.Deleteable<T>(ids).ExecuteCommandAsync();
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

                    await await sqlSugarTranscation.Do(async () =>
                    {
                        await sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Deleteable<T>(ids).ExecuteCommandAsync();
                    });
                }
            }

            public void Insert(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (datas.Length == 0)
                    return;

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        await sqlSugarClient.Insertable(datas).ExecuteCommandAsync();
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

                    await await sqlSugarTranscation.Do(async () =>
                    {
                        await sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Insertable(datas).ExecuteCommandAsync();
                    });
                }
            }

            public void Merge(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (datas.Length == 0)
                    return;

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        await sqlSugarClient.Saveable(new List<T>(datas)).ExecuteCommandAsync();
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

                    await await sqlSugarTranscation.Do(async () =>
                    {
                        await sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Saveable(new List<T>(datas)).ExecuteCommandAsync();
                    });
                }
            }

            public void Update(T data, ITransaction transaction = null, params string[] ignoreColumns)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task UpdateAsync(T data, ITransaction transaction = null, params string[] ignoreColumns)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (data == null)
                    return;

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        await sqlSugarClient.Updateable(data).IgnoreColumns(ignoreColumns).ExecuteCommandAsync();
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

                    await await sqlSugarTranscation.Do(async () =>
                    {
                        await sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Updateable(data).IgnoreColumns(ignoreColumns).ExecuteCommandAsync();
                    });
                }
            }

            public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task UpdateAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_masterConnectionString);

                    try
                    {
                        await sqlSugarClient.Updateable<T>()
                                      .Where(predicate)
                                      .SetColumns(updateExpression)
                                      .ExecuteCommandAsync();
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

                    await await sqlSugarTranscation.Do(async () =>
                    {
                        await sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Updateable<T>()
                                                                 .Where(predicate)
                                                                 .SetColumns(updateExpression)
                                                                 .ExecuteCommandAsync();
                    });
                }
            }

            public int Count(string queryWhere, Dictionary<string, object> parameters = null, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task<int> CountAsync(string queryWhere, Dictionary<string, object> parameters = null, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

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

                        return await query.CountAsync();
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

                    return await await sqlSugarTranscation.Do(async () =>
                    {
                        ISugarQueryable<T> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>();

                        if (!string.IsNullOrWhiteSpace(queryWhere) && parameters != null)
                            query.Where(queryWhere, parameters);
                        else if (!string.IsNullOrWhiteSpace(queryWhere))
                            query.Where(queryWhere);

                        return await query.CountAsync();
                    });
                }
            }

            public int Count(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        ISugarQueryable<T> query = sqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        return await query.CountAsync();
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

                    return await await sqlSugarTranscation.Do(async () =>
                    {
                        ISugarQueryable<T> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        return await query.CountAsync();
                    });
                }
            }

            public T Get(long id, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task<T> GetAsync(long id, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        return await sqlSugarClient.Queryable<T>().InSingleAsync(id);
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

                    return await await sqlSugarTranscation.Do(async () =>
                    {
                        return await sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>().InSingleAsync(id);
                    });
                }
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

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

                        return await query.Skip(startIndex).Take(count).ToListAsync();
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

                    return await await sqlSugarTranscation.Do(async () =>
                    {
                        ISugarQueryable<T> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>();

                        if (predicate != null)
                            query = query.Where(predicate);

                        if (queryOrderBies != null)
                            foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                                query = query.OrderBy(queryOrderBy.Expression, GetOrderByType(queryOrderBy.OrderByType));

                        return await query.Skip(startIndex).Take(count).ToListAsync();
                    });
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
                bool inTransaction = Apply<T>(transaction) &&
                                     Apply<TJoinTable>(transaction);

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

            public async Task<IEnumerable<JoinResult<T, TJoinTable>>> SearchAsync<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                             Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                             IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                             int startIndex = 0,
                                                                             int count = int.MaxValue,
                                                                             ITransaction transaction = null)
                where TJoinTable : class, IEntity, new()
            {
                bool inTransaction = await ApplyAsync<T>(transaction) &&
                                     await ApplyAsync<TJoinTable>(transaction);

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

                        return (await query.Select((tleft, tright) => new { tleft, tright }).Skip(startIndex).Take(count).ToListAsync()).Select(item => new JoinResult<T, TJoinTable>(item.tleft, item.tright));
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

                    return await await sqlSugarTranscation.Do(async () =>
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable(
                                       SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        if (queryOrderBies != null)
                            foreach (QueryOrderBy<T, TJoinTable> queryOrderBy in queryOrderBies)
                                query = query.OrderBy(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(queryOrderBy.Expression), GetOrderByType(queryOrderBy.OrderByType));

                        return (await query.Select((tleft, tright) => new { tleft, tright }).Skip(startIndex).Take(count).ToListAsync()).Select(item => new JoinResult<T, TJoinTable>(item.tleft, item.tright));
                    });
                }
            }

            public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                         Expression<Func<T, TJoinTable, bool>> predicate = null,
                                         ITransaction transaction = null)
                where TJoinTable : class, IEntity, new()
            {
                bool inTransaction = Apply<T>(transaction) &&
                                     Apply<TJoinTable>(transaction);

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

            public async Task<int> CountAsync<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                         Expression<Func<T, TJoinTable, bool>> predicate = null,
                                         ITransaction transaction = null)
                where TJoinTable : class, IEntity, new()
            {
                bool inTransaction = await ApplyAsync<T>(transaction) &&
                                     await ApplyAsync<TJoinTable>(transaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarClient.Queryable(SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        return await query.CountAsync();
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

                    return await await sqlSugarTranscation.Do(async () =>
                    {
                        ISugarQueryable<T, TJoinTable> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable(SqlSugarJoinQuery<T, TJoinTable>.ConvertJoinExpression(joinCondition.LeftJoinExpression, joinCondition.RightJoinExression));

                        if (predicate != null)
                            query = query.Where(SqlSugarJoinQuery<T, TJoinTable>.ConvertExpression(predicate));

                        return await query.CountAsync();
                    });
                }
            }

            public IEnumerable<T> Search(string queryWhere,
                                         Dictionary<string, object> parameters = null,
                                         string orderByFields = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task<IEnumerable<T>> SearchAsync(string queryWhere,
                                         Dictionary<string, object> parameters = null,
                                         string orderByFields = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

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

                        return await query.Skip(startIndex).Take(count).ToListAsync();
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

                    return await await sqlSugarTranscation.Do(async () =>
                    {
                        ISugarQueryable<T> query = sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Queryable<T>();

                        if (parameters != null)
                            query.Where(queryWhere, parameters);
                        else
                            query.Where(queryWhere);

                        if (!string.IsNullOrWhiteSpace(orderByFields))
                            query.OrderBy(orderByFields);

                        return await query.Skip(startIndex).Take(count).ToListAsync();
                    });
                }
            }

            public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

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

            public async Task<IEnumerable<IDictionary<string, object>>> QueryAsync(string sql, Dictionary<string, object> parameters = null, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    SqlSugarClient sqlSugarClient = CreateConnection(m_slaveConnectionString);

                    try
                    {
                        IEnumerable<IDictionary<string, object>> datas = await sqlSugarClient.Ado.SqlQueryAsync<ExpandoObject>(sql, parameters);

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

                    return await await sqlSugarTranscation.Do(async () =>
                    {
                        IEnumerable<IDictionary<string, object>> datas = await sqlSugarTranscation.SqlSugarTaskScheduler.SqlSugarClient.Ado.SqlQueryAsync<ExpandoObject>(sql, parameters);

                        return datas.Select(data =>
                        {
                            IDictionary<string, object> result = new Dictionary<string, object>();

                            foreach (KeyValuePair<string, object> item in data)
                                result.Add(item.Key.ToUpper(), item.Value);

                            return result;
                        });
                    });
                }
            }

            public ITransaction BeginTransaction(int weight = 0)
            {
                return new SqlSugarTranscation(weight);
            }

            public async Task<ITransaction> BeginTransactionAsync(int weight = 0)
            {
                return await Task.FromResult(new SqlSugarTranscation(weight));
            }

            private static SqlSugar.OrderByType GetOrderByType(OrderByType orderByType)
            {
                return orderByType switch
                {
                    OrderByType.Asc => SqlSugar.OrderByType.Asc,
                    OrderByType.Desc => SqlSugar.OrderByType.Desc,
                    _ => throw new NotSupportedException(),
                };
            }
        }

        #region SqlSuger事务处理类

        private class SqlSugarTranscation : ITransaction
        {
            public HashSet<Type> TransactionTables { get; }
            public string Identity { get; }
            public int Weight { get; }
            public SqlSugarTaskScheduler SqlSugarTaskScheduler { get; }

            public object Context { get { return SqlSugarTaskScheduler; } }

            public SqlSugarTranscation(int weight)
            {
                Identity = Guid.NewGuid().ToString("D");
                Weight = weight;
                TransactionTables = new HashSet<Type>();
                SqlSugarTaskScheduler = new SqlSugarTaskScheduler();
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

            public async Task RollbackAsync()
            {
                await Do(() => { SqlSugarTaskScheduler.SqlSugarClient.RollbackTran(); });
            }

            public void Submit()
            {
                Do(() => { SqlSugarTaskScheduler.SqlSugarClient.CommitTran(); }).Wait();
            }

            public async Task SubmitAsync()
            {
                await Do(() => { SqlSugarTaskScheduler.SqlSugarClient.CommitTran(); });
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