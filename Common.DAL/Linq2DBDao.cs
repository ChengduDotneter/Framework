using Common.DAL.Transaction;
using Common.Lock;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace Common.DAL
{
    internal static class Linq2DBDao
    {
        private const int DEFAULT_CONNECTION_COUNT = 10; //最大长连接数
        private const int DEFAULT_CONNECTION_WAITTIMEOUT = 8 * 60 * 60 * 1000;//8小时
        private const int DEFAULT_MAX_TEMP_CONNECTION_COUNT = 10; //最大临时连接数
        private const int TEMP_CONNECTION_TIMEOUT = 60 * 10 * 1000; //临时连接数存活时间
        private static IDictionary<string, DataConnectResourcePool> m_connectionPool;
        private static ISet<string> m_tableNames;
        private static LinqToDbConnectionOptions m_masterlinqToDbConnectionOptions;
        private static LinqToDbConnectionOptions m_slavelinqToDbConnectionOptions;

        private static readonly int m_dataConnectionOutTime;

        static Linq2DBDao()
        {
            m_tableNames = new HashSet<string>();
            LinqToDbConnectionOptionsBuilder masterLinqToDbConnectionOptionsBuilder = new LinqToDbConnectionOptionsBuilder();
            LinqToDbConnectionOptionsBuilder slaveLinqToDbConnectionOptionsBuilder = new LinqToDbConnectionOptionsBuilder();

            masterLinqToDbConnectionOptionsBuilder.UseMySql(ConfigManager.Configuration.GetConnectionString("MasterConnection"));
            slaveLinqToDbConnectionOptionsBuilder.UseMySql(ConfigManager.Configuration.GetConnectionString("SalveConnection"));

            masterLinqToDbConnectionOptionsBuilder.WithTraceLevel(TraceLevel.Verbose);
            slaveLinqToDbConnectionOptionsBuilder.WithTraceLevel(TraceLevel.Verbose);

            masterLinqToDbConnectionOptionsBuilder.WithTracing(traceInfo =>
            {
                if (traceInfo.TraceInfoStep == TraceInfoStep.AfterExecute || traceInfo.TraceInfoStep == TraceInfoStep.Error)
                    DaoFactory.LogHelper.Info("linq2DB_master", traceInfo.SqlText);
            });

            slaveLinqToDbConnectionOptionsBuilder.WithTracing(traceInfo =>
            {
                if (traceInfo.TraceInfoStep == TraceInfoStep.AfterExecute || traceInfo.TraceInfoStep == TraceInfoStep.Error)
                    DaoFactory.LogHelper.Info("linq2DB_slave", traceInfo.SqlText);
            });

            m_masterlinqToDbConnectionOptions = masterLinqToDbConnectionOptionsBuilder.Build();
            m_slavelinqToDbConnectionOptions = slaveLinqToDbConnectionOptionsBuilder.Build();

            m_connectionPool = new Dictionary<string, DataConnectResourcePool>();

            int.TryParse(ConfigManager.Configuration["ConnectionCount"], out int connectionCount);

            if (connectionCount <= 0)
                connectionCount = DEFAULT_CONNECTION_COUNT;

            int.TryParse(ConfigManager.Configuration["ConnectionTimeOut"], out m_dataConnectionOutTime);

            if (m_dataConnectionOutTime <= 0)
                m_dataConnectionOutTime = DEFAULT_CONNECTION_WAITTIMEOUT;

            int.TryParse(ConfigManager.Configuration["MaxTempConnectionCount"], out int maxTempConnectionCount);

            if (maxTempConnectionCount <= 0)
                maxTempConnectionCount = DEFAULT_MAX_TEMP_CONNECTION_COUNT;

            int.TryParse(ConfigManager.Configuration["TempConnectionTimeOut"], out int tempConnectionTimeOut);

            if (tempConnectionTimeOut <= 0)
                tempConnectionTimeOut = TEMP_CONNECTION_TIMEOUT;

            if (!m_connectionPool.ContainsKey(m_masterlinqToDbConnectionOptions.ConnectionString))
            {
                m_connectionPool.Add(m_masterlinqToDbConnectionOptions.ConnectionString, new DataConnectResourcePool(connectionCount, m_dataConnectionOutTime, maxTempConnectionCount, tempConnectionTimeOut, CreateMasterDataConnection, CloseDataConnection));
            }

            if (!m_connectionPool.ContainsKey(m_slavelinqToDbConnectionOptions.ConnectionString))
            {
                m_connectionPool.Add(m_slavelinqToDbConnectionOptions.ConnectionString, new DataConnectResourcePool(connectionCount, m_dataConnectionOutTime, maxTempConnectionCount, tempConnectionTimeOut, CreateSlaveDataConnection, CloseDataConnection));
            }
        }

        private static DataConnectionInstance CreateMasterDataConnection()
        {
            return new DataConnectionInstance(Environment.TickCount + m_dataConnectionOutTime, m_masterlinqToDbConnectionOptions);
        }

        private static DataConnectionInstance CreateSlaveDataConnection()
        {
            return new DataConnectionInstance(Environment.TickCount + m_dataConnectionOutTime, m_slavelinqToDbConnectionOptions);
        }

        private static void CloseDataConnection(DataConnectionInstance dataConnectionInstance)
        {
            dataConnectionInstance.Close();
            dataConnectionInstance.Dispose();
        }

        private static IResourceInstance<DataConnectionInstance> CreateConnection(LinqToDbConnectionOptions linqToDbConnectionOptions)
        {
            return m_connectionPool[linqToDbConnectionOptions.ConnectionString].ApplyInstance();
        }

        private static void DisposeConnection(IResourceInstance<DataConnectionInstance> resourceInstance)
        {
            resourceInstance.Dispose();
        }

        public static ISearchQuery<T> GetLinq2DBSearchQuery<T>(bool codeFirst)
            where T : class, IEntity, new()
        {
            Linq2DBDaoInstance<T> linq2DBDaoInstance = new Linq2DBDaoInstance<T>(m_slavelinqToDbConnectionOptions, codeFirst);

            if (codeFirst)
                CreateTable<T>(m_slavelinqToDbConnectionOptions);

            return linq2DBDaoInstance;
        }

        public static IEditQuery<T> GetLinq2DBEditQuery<T>(bool codeFirst)
             where T : class, IEntity, new()
        {
            Linq2DBDaoInstance<T> linq2DBDaoInstance = new Linq2DBDaoInstance<T>(m_masterlinqToDbConnectionOptions, codeFirst);

            if (codeFirst)
                CreateTable<T>(m_masterlinqToDbConnectionOptions);

            return linq2DBDaoInstance;
        }

        public static IDBResourceContent GetDBResourceContent()
        {
            return new Linq2DBResourceContent(m_connectionPool[m_slavelinqToDbConnectionOptions.ConnectionString].ApplyInstance());
        }

        private static void CreateTable<T>(LinqToDbConnectionOptions linqToDbConnectionOptions)
             where T : class, IEntity, new()
        {
            string tableName = typeof(T).Name;

            if (!m_tableNames.Contains(tableName))
            {
                lock (m_tableNames)
                {
                    if (!m_tableNames.Contains(tableName))
                    {
                        IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(linqToDbConnectionOptions);

                        try
                        {
                            dataConnection.Instance.CreateTable<T>(dataConnection.Instance.MappingSchema.GetEntityDescriptor(typeof(T)).TableName);
                        }
                        catch
                        {
                            return;
                        }
                        finally
                        {
                            DisposeConnection(dataConnection);
                            m_tableNames.Add(tableName);
                        }
                    }
                }
            }
        }

        private static bool Apply<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!linq2DBTransaction.TransactionTables.Contains(table))
                {
                    if (TransactionResourceHelper.ApplayTableResource(table, linq2DBTransaction.Identity, linq2DBTransaction.Weight))
                        linq2DBTransaction.TransactionTables.Add(table);
                    else
                        throw new ResourceException($"申请事务资源{table.FullName}失败。");
                }
            }

            return linq2DBTransaction != null;
        }

        private static async Task<bool> ApplyAsync<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!linq2DBTransaction.TransactionTables.Contains(table))
                {
                    if (await TransactionResourceHelper.ApplayTableResourceAsync(table, linq2DBTransaction.Identity, linq2DBTransaction.Weight))
                        linq2DBTransaction.TransactionTables.Add(table);
                    else
                        throw new ResourceException($"申请事务资源{table.FullName}失败。");
                }
            }

            return linq2DBTransaction != null;
        }

        private static bool WriteApply<TResource>(ITransaction transaction, IEnumerable<long> ids) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!linq2DBTransaction.TransactionTables.Contains(table))
                {
                    if (TransactionResourceHelper.ApplayRowResourceWithWrite(table, linq2DBTransaction.Identity, linq2DBTransaction.Weight, ids))
                        linq2DBTransaction.TransactionTables.Add(table);
                    else
                        throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
                }
            }

            return linq2DBTransaction != null;
        }

        private static async Task<bool> WriteApplyAsync<TResource>(ITransaction transaction, IEnumerable<long> ids) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!linq2DBTransaction.TransactionTables.Contains(table))
                {
                    if (await TransactionResourceHelper.ApplayRowResourceWithWriteAsync(table, linq2DBTransaction.Identity, linq2DBTransaction.Weight, ids))
                        linq2DBTransaction.TransactionTables.Add(table);
                    else
                        throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
                }
            }

            return linq2DBTransaction != null;
        }

        private static bool ReadApply<TResource>(ITransaction transaction, IEnumerable<long> ids) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!linq2DBTransaction.TransactionTables.Contains(table))
                {
                    if (TransactionResourceHelper.ApplayRowResourceWithRead(table, linq2DBTransaction.Identity, linq2DBTransaction.Weight, ids))
                        linq2DBTransaction.TransactionTables.Add(table);
                    else
                        throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
                }
            }

            return linq2DBTransaction != null;
        }

        private static async Task<bool> ReadApplyAsync<TResource>(ITransaction transaction, IEnumerable<long> ids) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!linq2DBTransaction.TransactionTables.Contains(table))
                {
                    if (await TransactionResourceHelper.ApplayRowResourceWithReadAsync(table, linq2DBTransaction.Identity, linq2DBTransaction.Weight, ids))
                        linq2DBTransaction.TransactionTables.Add(table);
                    else
                        throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
                }
            }

            return linq2DBTransaction != null;
        }

        private static async void Release(string identity)
        {
            await TransactionResourceHelper.ReleaseResourceAsync(identity);
        }

        private class Linq2DBResourceContent : IResourceInstance<DataConnectionInstance>, IDBResourceContent
        {
            private IResourceInstance<DataConnectionInstance> m_resourceInstance;

            public Linq2DBResourceContent(IResourceInstance<DataConnectionInstance> resourceInstance)
            {
                m_resourceInstance = resourceInstance;
            }

            public object Content { get { return this; } }

            public DataConnectionInstance Instance { get { return m_resourceInstance.Instance; } }

            public void Dispose()
            {
                m_resourceInstance.Dispose();
            }
        }

        private class Linq2DBTransaction : ITransaction
        {
            private DataConnectionTransaction m_dataConnectionTransaction;
            private readonly IResourceInstance<DataConnectionInstance> m_resourceInstance;

            public HashSet<Type> TransactionTables { get; }
            public string Identity { get; }
            public int Weight { get; }
            public bool DistributedLock { get; }

            public Linq2DBTransaction(DataConnectionTransaction dataConnectionTransaction, bool distributedLock, int weight, IResourceInstance<DataConnectionInstance> resourceInstance)
            {
                Identity = Guid.NewGuid().ToString("D");
                Weight = weight;
                DistributedLock = distributedLock;
                m_resourceInstance = resourceInstance;
                TransactionTables = new HashSet<Type>();
                m_dataConnectionTransaction = dataConnectionTransaction;
            }

            public object Context { get { return m_dataConnectionTransaction; } }

            public object ResourceInstance { get { return m_resourceInstance; } }

            public async void Dispose()
            {
                await m_dataConnectionTransaction.DisposeAsync();
                DisposeConnection(m_resourceInstance);
                Release(Identity);
            }

            public void Rollback()
            {
                m_dataConnectionTransaction.Rollback();
            }

            public async Task RollbackAsync()
            {
                await m_dataConnectionTransaction.RollbackAsync();
            }

            public void Submit()
            {
                m_dataConnectionTransaction.Commit();
            }

            public async Task SubmitAsync()
            {
                await m_dataConnectionTransaction.CommitAsync();
            }
        }

        private class Linq2DBQueryable<T> : ISearchQueryable<T>
            where T : class, IEntity, new()
        {
            private ITable<T> m_table;
            private bool m_notAutoRealse;
            private IResourceInstance<DataConnectionInstance> m_resourceInstance;

            public Linq2DBQueryable(ITable<T> table, bool notAutoRealse, IResourceInstance<DataConnectionInstance> resourceInstance)
            {
                m_notAutoRealse = notAutoRealse;
                m_table = table;
                m_resourceInstance = resourceInstance;
            }

            public Type ElementType => m_table.ElementType;

            public Expression Expression => m_table.Expression;

            public IQueryProvider Provider => m_table.Provider;

            public void Dispose()
            {
                if (!m_notAutoRealse && m_resourceInstance != null)
                    DisposeConnection(m_resourceInstance);
            }

            public IEnumerator<T> GetEnumerator()
            {
                return m_table.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)m_table).GetEnumerator();
            }
        }

        private class Linq2DBDaoInstance<T> : ISearchQuery<T>, IEditQuery<T>
            where T : class, IEntity, new()
        {
            private LinqToDbConnectionOptions m_linqToDbConnectionOptions;
            private static readonly Expression<Func<T, bool>> EMPTY_PREDICATE;
            private bool m_codeFirst;

            public ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);
                return new Linq2DBTransaction(resourceInstance.Instance.BeginTransaction(), distributedLock, weight, resourceInstance);
            }

            public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);
                return new Linq2DBTransaction(await resourceInstance.Instance.BeginTransactionAsync(), distributedLock, weight, resourceInstance);
            }

            public void Delete(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = WriteApply<T>(transaction, ids);

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        dataConnection.Instance.GetTable<T>().Delete(item => ids.Contains(item.ID));
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().Delete(item => ids.Contains(item.ID));
                }
            }

            public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = await WriteApplyAsync<T>(transaction, ids);

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        await dataConnection.Instance.GetTable<T>().DeleteAsync(item => ids.Contains(item.ID));
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    await ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().DeleteAsync(item => ids.Contains(item.ID));
                }
            }

            public void Insert(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        dataConnection.Instance.GetTable<T>().BulkCopy(datas);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    dataConnection.GetTable<T>().BulkCopy(datas);
                }
            }

            public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        await dataConnection.Instance.GetTable<T>().BulkCopyAsync(datas);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    await dataConnection.GetTable<T>().BulkCopyAsync(datas);
                }
            }

            public void Merge(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction) && WriteApply<T>(transaction, datas.Select(item => item.ID));

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        for (int i = 0; i < datas.Length; i++)
                            dataConnection.Instance.InsertOrReplace(datas[i]);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;

                    for (int i = 0; i < datas.Length; i++)
                        dataConnection.InsertOrReplace(datas[i]);
                }
            }

            public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction) && await WriteApplyAsync<T>(transaction, datas.Select(item => item.ID));

                Task[] tasks = new Task[datas.Length];

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        for (int i = 0; i < datas.Length; i++)
                        {
                            tasks[i] = dataConnection.Instance.InsertOrReplaceAsync(datas[i]);
                        }
                        await Task.WhenAll(tasks);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;

                    for (int i = 0; i < datas.Length; i++)
                        tasks[i] = dataConnection.InsertOrReplaceAsync(datas[i]);

                    await Task.WhenAll(tasks);
                }
            }

            public void Update(T data, ITransaction transaction = null)
            {
                bool inTransaction = WriteApply<T>(transaction, new long[] { data.ID });

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        dataConnection.Instance.Update(data);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    dataConnection.Update(data);
                }
            }

            public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                IList<Tuple<Type, LambdaExpression, object>> updates = new List<Tuple<Type, LambdaExpression, object>>();

                foreach (var item in upateDictionary)
                    updates.Add(Tuple.Create(typeof(T).GetProperty(item.Key).PropertyType, Expression.Lambda(Expression.Property(parameter, item.Key), parameter), item.Value));

                IEnumerable<long> ids = null;

                if (transaction == null)
                {
                    using (ISearchQueryable<T> queryable = GetQueryable())
                    {
                        ids = queryable.Where(predicate).Select(item => item.ID).ToList();
                    }
                }
                else
                {
                    ids = GetQueryable(transaction).Where(predicate).Select(item => item.ID).ToList();
                }

                if (ids == null || ids.Count() == 0)
                    return;

                bool inTransaction = WriteApply<T>(transaction, ids);

                if (!inTransaction)
                {
                    using (ISearchQueryable<T> queryable = GetQueryable())
                    {
                        IUpdatable<T> updatable = queryable.Where(item => ids.Contains(item.ID)).AsUpdatable();

                        foreach (var update in updates)
                        {
                            (Type valueType, Expression expression, object value) = update;
                            Type funcType = typeof(Func<,>).MakeGenericType(typeof(T), valueType);
                            Type expressionType = typeof(Expression<>).MakeGenericType(funcType);
                            MethodInfo methodInfo = typeof(LinqExtensions).
                                GetMethods(BindingFlags.Public | BindingFlags.Static).Where(item => item.Name == nameof(LinqExtensions.Set)).ElementAt(5).MakeGenericMethod(typeof(T), valueType);
                            updatable = (IUpdatable<T>)methodInfo.Invoke(null, new object[] { updatable, expression, value });
                        }

                        updatable.Update();
                    };

                }
                else
                {
                    IUpdatable<T> updatable = GetQueryable(transaction).Where(item => ids.Contains(item.ID)).AsUpdatable();

                    foreach (var update in updates)
                    {
                        (Type valueType, Expression expression, object value) = update;
                        Type funcType = typeof(Func<,>).MakeGenericType(typeof(T), valueType);
                        Type expressionType = typeof(Expression<>).MakeGenericType(funcType);
                        MethodInfo methodInfo = typeof(LinqExtensions).
                            GetMethods(BindingFlags.Public | BindingFlags.Static).Where(item => item.Name == nameof(LinqExtensions.Set)).ElementAt(5).MakeGenericMethod(typeof(T), valueType);
                        updatable = (IUpdatable<T>)methodInfo.Invoke(null, new object[] { updatable, expression, value });
                    }

                    updatable.Update();
                }
            }

            public async Task UpdateAsync(T data, ITransaction transaction = null)
            {
                bool inTransaction = await WriteApplyAsync<T>(transaction, new long[] { data.ID });

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        await dataConnection.Instance.UpdateAsync(data);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    await dataConnection.UpdateAsync(data);
                }
            }

            public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                IList<Tuple<Type, LambdaExpression, object>> updates = new List<Tuple<Type, LambdaExpression, object>>();

                foreach (var item in upateDictionary)
                    updates.Add(Tuple.Create(typeof(T).GetProperty(item.Key).PropertyType, Expression.Lambda(Expression.Property(parameter, item.Key), parameter), item.Value));

                IEnumerable<long> ids = null;

                if (transaction == null)
                {
                    using (ISearchQueryable<T> queryable = await GetQueryableAsync())
                    {
                        ids = queryable.Where(predicate).Select(item => item.ID).ToList();
                    }
                }
                else
                {
                    ids = (await GetQueryableAsync(transaction)).Where(predicate).Select(item => item.ID).ToList();
                }

                if (ids == null || ids.Count() == 0)
                    return;

                bool inTransaction = await WriteApplyAsync<T>(transaction, ids);

                if (!inTransaction)
                {
                    using (ISearchQueryable<T> queryable = await GetQueryableAsync())
                    {
                        IUpdatable<T> updatable = queryable.Where(item => ids.Contains(item.ID)).AsUpdatable();

                        foreach (var update in updates)
                        {
                            (Type valueType, Expression expression, object value) = update;
                            Type funcType = typeof(Func<,>).MakeGenericType(typeof(T), valueType);
                            Type expressionType = typeof(Expression<>).MakeGenericType(funcType);
                            MethodInfo methodInfo = typeof(LinqExtensions).
                                GetMethods(BindingFlags.Public | BindingFlags.Static).Where(item => item.Name == nameof(LinqExtensions.Set)).ElementAt(5).MakeGenericMethod(typeof(T), valueType);
                            updatable = (IUpdatable<T>)methodInfo.Invoke(null, new object[] { updatable, expression, value });
                        }

                        await updatable.UpdateAsync();
                    };

                }
                else
                {
                    IUpdatable<T> updatable = (await GetQueryableAsync(transaction)).Where(item => ids.Contains(item.ID)).AsUpdatable();

                    foreach (var update in updates)
                    {
                        (Type valueType, Expression expression, object value) = update;
                        Type funcType = typeof(Func<,>).MakeGenericType(typeof(T), valueType);
                        Type expressionType = typeof(Expression<>).MakeGenericType(funcType);
                        MethodInfo methodInfo = typeof(LinqExtensions).
                            GetMethods(BindingFlags.Public | BindingFlags.Static).Where(item => item.Name == nameof(LinqExtensions.Set)).ElementAt(5).MakeGenericMethod(typeof(T), valueType);
                        updatable = (IUpdatable<T>)methodInfo.Invoke(null, new object[] { updatable, expression, value });
                    }

                    await updatable.UpdateAsync();
                }
            }

            private static void ValidTransaction(ITransaction transaction, IEnumerable<long> ids)
            {
                bool inTransaction = ReadApply<T>(transaction, ids);

                if (!inTransaction)
                    throw new DealException($"当前未查询到事务信息，请先使用{nameof(Linq2DBDaoInstance<T>.BeginTransaction)}开启事务。");
            }

            private async static Task ValidTransactionAsync(ITransaction transaction, IEnumerable<long> ids)
            {
                bool inTransaction = await ReadApplyAsync<T>(transaction, ids);

                if (!inTransaction)
                    throw new DealException($"当前未查询到事务信息，请先使用{nameof(Linq2DBDaoInstance<T>.BeginTransactionAsync)}开启事务。");
            }

            public T Get(long id, ITransaction transaction)
            {
                ValidTransaction(transaction, new long[] { id });

                return ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().SingleOrDefault(item => item.ID == id);
            }

            public T Get(long id, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return dataConnection.Instance.GetTable<T>().SingleOrDefault(item => item.ID == id);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                    return ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().SingleOrDefault(item => item.ID == id);
            }

            public async Task<T> GetAsync(long id, ITransaction transaction)
            {
                await ValidTransactionAsync(transaction, new long[] { id });

                return await ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().SingleOrDefaultAsync(item => item.ID == id);
            }

            public async Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return await dataConnection.Instance.GetTable<T>().SingleOrDefaultAsync(item => item.ID == id);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                    return await ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().SingleOrDefaultAsync(item => item.ID == id);
            }

            public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null)
            {
                IEnumerable<long> ids = GetQueryable(transaction).Where(predicate ?? EMPTY_PREDICATE).Select(item => item.ID);

                ValidTransaction(transaction, ids);

                return ids.Count();
            }

            public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return dataConnection.Instance.GetTable<T>().Count(predicate ?? EMPTY_PREDICATE);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                    return ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().Count(predicate ?? EMPTY_PREDICATE);
            }

            public async Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null)
            {
                IEnumerable<long> ids = (await GetQueryableAsync(transaction)).Where(predicate ?? EMPTY_PREDICATE).Select(item => item.ID);

                await ValidTransactionAsync(transaction, ids);

                return ids.Count();
            }

            public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return await dataConnection.Instance.GetTable<T>().CountAsync(predicate ?? EMPTY_PREDICATE);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                    return await ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().CountAsync(predicate ?? EMPTY_PREDICATE);
            }

            public IEnumerable<T> Search(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue)
            {
                IQueryable<T> query = GetQueryable(transaction).Where(predicate ?? EMPTY_PREDICATE);

                bool orderd = false;

                if (queryOrderBies != null)
                {
                    foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                    {
                        if (queryOrderBy.OrderByType == OrderByType.Asc)
                        {
                            if (!orderd)
                                query = query.OrderBy(queryOrderBy.Expression);
                            else
                                query = query.ThenOrBy(queryOrderBy.Expression);
                        }
                        else
                        {
                            if (!orderd)
                                query = query.OrderByDescending(queryOrderBy.Expression);
                            else
                                query = query.ThenOrByDescending(queryOrderBy.Expression);
                        }

                        orderd = true;
                    }
                }

                IEnumerable<long> ids = query.Select(item => item.ID).Skip(startIndex).Take(count).ToList();

                ValidTransaction(transaction, ids);

                return GetQueryable(transaction).Where(item => ids.Contains(item.ID)).ToList();
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        IQueryable<T> query = dataConnection.Instance.GetTable<T>().Where(predicate ?? EMPTY_PREDICATE);
                        bool orderd = false;

                        if (queryOrderBies != null)
                        {
                            foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                            {
                                if (queryOrderBy.OrderByType == OrderByType.Asc)
                                {
                                    if (!orderd)
                                        query = query.OrderBy(queryOrderBy.Expression);
                                    else
                                        query = query.ThenOrBy(queryOrderBy.Expression);
                                }
                                else
                                {
                                    if (!orderd)
                                        query = query.OrderByDescending(queryOrderBy.Expression);
                                    else
                                        query = query.ThenOrByDescending(queryOrderBy.Expression);
                                }

                                orderd = true;
                            }
                        }

                        return query.Skip(startIndex).Take(count).ToList();
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    IQueryable<T> query = ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().Where(predicate ?? EMPTY_PREDICATE);
                    bool orderd = false;

                    if (queryOrderBies != null)
                    {
                        foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                        {
                            if (queryOrderBy.OrderByType == OrderByType.Asc)
                            {
                                if (!orderd)
                                    query = query.OrderBy(queryOrderBy.Expression);
                                else
                                    query = query.ThenOrBy(queryOrderBy.Expression);
                            }
                            else
                            {
                                if (!orderd)
                                    query = query.OrderByDescending(queryOrderBy.Expression);
                                else
                                    query = query.ThenOrByDescending(queryOrderBy.Expression);
                            }

                            orderd = true;
                        }
                    }

                    return query.Skip(startIndex).Take(count).ToList();
                }
            }

            public async Task<IEnumerable<T>> SearchAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue)
            {
                IQueryable<T> query = (await GetQueryableAsync(transaction)).Where(predicate ?? EMPTY_PREDICATE);

                bool orderd = false;

                if (queryOrderBies != null)
                {
                    foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                    {
                        if (queryOrderBy.OrderByType == OrderByType.Asc)
                        {
                            if (!orderd)
                                query = query.OrderBy(queryOrderBy.Expression);
                            else
                                query = query.ThenOrBy(queryOrderBy.Expression);
                        }
                        else
                        {
                            if (!orderd)
                                query = query.OrderByDescending(queryOrderBy.Expression);
                            else
                                query = query.ThenOrByDescending(queryOrderBy.Expression);
                        }

                        orderd = true;
                    }
                }

                IEnumerable<long> ids = query.Select(item => item.ID).Skip(startIndex).Take(count).ToList();

                await ValidTransactionAsync(transaction, ids);

                return (await GetQueryableAsync(transaction)).Where(item => ids.Contains(item.ID)).ToList();
            }

            public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        IQueryable<T> query = dataConnection.Instance.GetTable<T>().Where(predicate ?? EMPTY_PREDICATE);
                        bool orderd = false;

                        if (queryOrderBies != null)
                        {
                            foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                            {
                                if (queryOrderBy.OrderByType == OrderByType.Asc)
                                {
                                    if (!orderd)
                                        query = query.OrderBy(queryOrderBy.Expression);
                                    else
                                        query = query.ThenOrBy(queryOrderBy.Expression);
                                }
                                else
                                {
                                    if (!orderd)
                                        query = query.OrderByDescending(queryOrderBy.Expression);
                                    else
                                        query = query.ThenOrByDescending(queryOrderBy.Expression);
                                }

                                orderd = true;
                            }
                        }

                        return await query.Skip(startIndex).Take(count).ToListAsync();
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    IQueryable<T> query = ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().Where(predicate ?? EMPTY_PREDICATE);
                    bool orderd = false;

                    if (queryOrderBies != null)
                    {
                        foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                        {
                            if (queryOrderBy.OrderByType == OrderByType.Asc)
                            {
                                if (!orderd)
                                    query = query.OrderBy(queryOrderBy.Expression);
                                else
                                    query = query.ThenOrBy(queryOrderBy.Expression);
                            }
                            else
                            {
                                if (!orderd)
                                    query = query.OrderByDescending(queryOrderBy.Expression);
                                else
                                    query = query.ThenOrByDescending(queryOrderBy.Expression);
                            }

                            orderd = true;
                        }
                    }

                    return await query.Skip(startIndex).Take(count).ToListAsync();
                }
            }

            public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
            {
                return query.Count();
            }

            public int Count<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
            {
                return query.Count();
            }

            public async Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
            {
                return await query.CountAsync();
            }

            public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
            {
                return await query.CountAsync();
            }

            public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return query.Skip(startIndex).Take(count).ToList();
            }

            public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
            {
                return query.Skip(startIndex).Take(count).ToList();
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return await query.Skip(startIndex).Take(count).ToListAsync();
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
            {
                return await query.Skip(startIndex).Take(count).ToListAsync();
            }

            public ISearchQueryable<T> GetQueryable(ITransaction transaction)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)((Linq2DBTransaction)transaction).ResourceInstance;

                return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>(), true, resourceInstance);
            }

            public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);

                    return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>(), false, resourceInstance);
                }
                else
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)dbResourceContent;

                    return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>(), true, resourceInstance);
                }
            }

            public async Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)((Linq2DBTransaction)transaction).ResourceInstance;

                return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>(), true, resourceInstance);
            }

            public Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);

                    return Task.FromResult<ISearchQueryable<T>>(new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>(), false, resourceInstance));
                }
                else
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)dbResourceContent;
                    return Task.FromResult<ISearchQueryable<T>>(new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>(), true, resourceInstance));
                }
            }

            public Linq2DBDaoInstance(LinqToDbConnectionOptions linqToDbConnectionOptions, bool codeFirst)
            {
                m_linqToDbConnectionOptions = linqToDbConnectionOptions;
                m_codeFirst = codeFirst;
            }

            static Linq2DBDaoInstance()
            {
                EMPTY_PREDICATE = _ => true;
            }
        }
    }
}