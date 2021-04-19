using Common.DAL.Transaction;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using LinqToDB.SchemaProvider;

namespace Common.DAL
{
    internal static class Linq2DBDao
    {
        private const int DEFAULT_CONNECTION_COUNT = 10; //最大长连接数
        private const int DEFAULT_CONNECTION_WAITTIMEOUT = 8 * 60 * 60 * 1000; //8小时
        private const int DEFAULT_MAX_TEMP_CONNECTION_COUNT = 10; //最大临时连接数
        private const int TEMP_CONNECTION_TIMEOUT = 60 * 10 * 1000; //临时连接数存活时间
        private static IDictionary<int, DataConnectResourcePool> m_connectionPool;
        private static LinqToDbConnectionOptions m_masterlinqToDbConnectionOptions;
        private static LinqToDbConnectionOptions m_slavelinqToDbConnectionOptions;

        private static readonly int m_dataConnectionOutTime;

        private static ITable<T> TableName<T>(this ITable<T> table, string systemID) where T : class, IEntity, new()
        {
            return LinqExtensions.TableName(table, GetPartitionTableName<T>(table.DataContext, systemID));
        }

        private static string GetPartitionTableName<T>(IDataContext dataContext, string systemID) where T : class, IEntity, new()
        {
            string tablePostFix = string.IsNullOrEmpty(systemID) ? string.Empty : $"_{systemID}";
            string tableName = Convert.ToBoolean(ConfigManager.Configuration["IsNotLowerTableName"]) ? $"{typeof(T).Name}{tablePostFix}" : $"{typeof(T).Name}{tablePostFix}".ToLower();
            CreateTable<T>(dataContext, tableName, Convert.ToBoolean(ConfigManager.Configuration["IsCodeFirst"]));

            return tableName;
        }

        static Linq2DBDao()
        {
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

            m_connectionPool = new Dictionary<int, DataConnectResourcePool>();

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

            if (!m_connectionPool.ContainsKey(m_masterlinqToDbConnectionOptions.GetHashCode()))
            {
                m_connectionPool.Add(m_masterlinqToDbConnectionOptions.GetHashCode(),
                                     new DataConnectResourcePool(connectionCount, m_dataConnectionOutTime, maxTempConnectionCount, tempConnectionTimeOut, CreateMasterDataConnection,
                                                                 CloseDataConnection));
            }

            if (!m_connectionPool.ContainsKey(m_slavelinqToDbConnectionOptions.GetHashCode()))
            {
                m_connectionPool.Add(m_slavelinqToDbConnectionOptions.GetHashCode(),
                                     new DataConnectResourcePool(connectionCount, m_dataConnectionOutTime, maxTempConnectionCount, tempConnectionTimeOut, CreateSlaveDataConnection,
                                                                 CloseDataConnection));
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
            return m_connectionPool[linqToDbConnectionOptions.GetHashCode()].ApplyInstance();
        }

        private static void DisposeConnection(IResourceInstance<DataConnectionInstance> resourceInstance)
        {
            resourceInstance.Dispose();
        }

        public static ISearchQuery<T> GetLinq2DBSearchQuery<T>()
            where T : class, IEntity, new()
        {
            return new Linq2DBDaoInstance<T>(m_slavelinqToDbConnectionOptions);
        }

        public static IEditQuery<T> GetLinq2DBEditQuery<T>()
            where T : class, IEntity, new()
        {
            return new Linq2DBDaoInstance<T>(m_masterlinqToDbConnectionOptions);
        }

        public static IDBResourceContent GetDBResourceContent()
        {
            return new Linq2DBResourceContent(m_connectionPool[m_slavelinqToDbConnectionOptions.GetHashCode()].ApplyInstance());
        }

        private static void CreateTable<T>(IDataContext dataContext, string tableName, bool codeFirst) where T : class, IEntity, new()
        {
            if (!codeFirst)
                return;

            DataConnection dataConnection = (DataConnection)dataContext;

            if (!dataConnection.DataProvider.GetSchemaProvider().GetSchema(dataConnection, new GetSchemaOptions
            {
                GetProcedures = false
            }).Tables.Any(item => item.TableName == tableName))
            {
                dataContext.CreateTable<T>(tableName);
            }
        }

        private static bool Apply<TResource>(ITransaction transaction, string systemID) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!TransactionResourceHelper.ApplayTableResource(table, systemID, linq2DBTransaction.Identity, linq2DBTransaction.Weight))
                    throw new ResourceException($"申请事务资源{table.FullName}失败。");
            }

            return linq2DBTransaction != null;
        }

        private static async Task<bool> ApplyAsync<TResource>(ITransaction transaction, string systemID) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!await TransactionResourceHelper.ApplayTableResourceAsync(table, systemID, linq2DBTransaction.Identity, linq2DBTransaction.Weight))
                    throw new ResourceException($"申请事务资源{table.FullName}失败。");
            }

            return linq2DBTransaction != null;
        }

        private static bool WriteApply<TResource>(ITransaction transaction, string systemID, IEnumerable<long> ids) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!TransactionResourceHelper.ApplayRowResourceWithWrite(table, systemID, linq2DBTransaction.Identity, linq2DBTransaction.Weight, ids))
                    throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");

            }

            return linq2DBTransaction != null;
        }

        private static async Task<bool> WriteApplyAsync<TResource>(ITransaction transaction, string systemID, IEnumerable<long> ids) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!await TransactionResourceHelper.ApplayRowResourceWithWriteAsync(table, systemID, linq2DBTransaction.Identity, linq2DBTransaction.Weight, ids))
                    throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
            }

            return linq2DBTransaction != null;
        }

        private static bool ReadApply<TResource>(ITransaction transaction, string systemID, IEnumerable<long> ids) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!TransactionResourceHelper.ApplayRowResourceWithRead(table, systemID, linq2DBTransaction.Identity, linq2DBTransaction.Weight, ids))
                    throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
            }

            return linq2DBTransaction != null;
        }

        private static async Task<bool> ReadApplyAsync<TResource>(ITransaction transaction, string systemID, IEnumerable<long> ids) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null && linq2DBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!await TransactionResourceHelper.ApplayRowResourceWithReadAsync(table, systemID, linq2DBTransaction.Identity, linq2DBTransaction.Weight, ids))
                    throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
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

            public string Identity { get; }
            public int Weight { get; }
            public bool DistributedLock { get; }

            public Linq2DBTransaction(DataConnectionTransaction dataConnectionTransaction, bool distributedLock, int weight, IResourceInstance<DataConnectionInstance> resourceInstance)
            {
                Identity = Guid.NewGuid().ToString("D");
                Weight = weight;
                DistributedLock = distributedLock;
                m_resourceInstance = resourceInstance;
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

            public ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);
                return new Linq2DBTransaction(resourceInstance.Instance.BeginTransaction(IsolationLevel.ReadCommitted), distributedLock, weight, resourceInstance);
            }

            public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);
                return new Linq2DBTransaction(await resourceInstance.Instance.BeginTransactionAsync(IsolationLevel.ReadCommitted), distributedLock, weight, resourceInstance);
            }

            public void Delete(string systemID, ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = WriteApply<T>(transaction, systemID, ids);

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        dataConnection.Instance.GetTable<T>().TableName(systemID).Delete(item => ids.Contains(item.ID));
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().TableName(systemID).Delete(item => ids.Contains(item.ID));
                }
            }

            public void Delete(ITransaction transaction = null, params long[] ids)
            {
                Delete(string.Empty, transaction, ids);
            }

            public async Task DeleteAsync(string systemID, ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = await WriteApplyAsync<T>(transaction, systemID, ids);

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        await dataConnection.Instance.GetTable<T>().TableName(systemID).DeleteAsync(item => ids.Contains(item.ID));
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    await ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().TableName(systemID).DeleteAsync(item => ids.Contains(item.ID));
                }
            }

            public Task DeleteAsync(ITransaction transaction = null, params long[] ids)
            {
                return DeleteAsync(string.Empty, transaction, ids);
            }

            public void Insert(string systemID, ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction, systemID);

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        dataConnection.Instance.GetTable<T>().TableName(systemID).BulkCopy(datas);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    dataConnection.GetTable<T>().TableName(systemID).BulkCopy(datas);
                }
            }

            public void Insert(ITransaction transaction = null, params T[] datas)
            {
                Insert(string.Empty, transaction, datas);
            }

            public async Task InsertAsync(string systemID, ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction, systemID);

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        await dataConnection.Instance.GetTable<T>().TableName(systemID).BulkCopyAsync(datas);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    await dataConnection.GetTable<T>().TableName(systemID).BulkCopyAsync(datas);
                }
            }

            public Task InsertAsync(ITransaction transaction = null, params T[] datas)
            {
                return InsertAsync(string.Empty, transaction, datas);
            }

            public void Merge(string systemID, ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction, systemID) && WriteApply<T>(transaction, systemID, datas.Select(item => item.ID));

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        for (int i = 0; i < datas.Length; i++)
                            dataConnection.Instance.InsertOrReplace(datas[i], GetPartitionTableName<T>(dataConnection.Instance, systemID));
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
                        dataConnection.InsertOrReplace(datas[i], GetPartitionTableName<T>(dataConnection, systemID));
                }
            }

            public void Merge(ITransaction transaction = null, params T[] datas)
            {
                Merge(string.Empty, transaction, datas);
            }

            public async Task MergeAsync(string systemID, ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction, systemID) && await WriteApplyAsync<T>(transaction, systemID, datas.Select(item => item.ID));

                Task[] tasks = new Task[datas.Length];

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        for (int i = 0; i < datas.Length; i++)
                        {
                            tasks[i] = dataConnection.Instance.InsertOrReplaceAsync(datas[i], GetPartitionTableName<T>(dataConnection.Instance, systemID));
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
                        tasks[i] = dataConnection.InsertOrReplaceAsync(datas[i], GetPartitionTableName<T>(dataConnection, systemID));

                    await Task.WhenAll(tasks);
                }
            }

            public Task MergeAsync(ITransaction transaction = null, params T[] datas)
            {
                return MergeAsync(string.Empty, transaction, datas);
            }

            public void Update(string systemID, T data, ITransaction transaction = null)
            {
                bool inTransaction = WriteApply<T>(transaction, systemID, new long[] { data.ID });

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        dataConnection.Instance.Update(data, GetPartitionTableName<T>(dataConnection.Instance, systemID));
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    dataConnection.Update(data, GetPartitionTableName<T>(dataConnection, systemID));
                }
            }

            public void Update(T data, ITransaction transaction = null)
            {
                Update(string.Empty, data, transaction);
            }

            public void Update(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                IList<Tuple<Type, LambdaExpression, object>> updates = new List<Tuple<Type, LambdaExpression, object>>();

                foreach (var item in updateDictionary)
                    updates.Add(Tuple.Create(typeof(T).GetProperty(item.Key).PropertyType, Expression.Lambda(Expression.Property(parameter, item.Key), parameter), item.Value));

                IEnumerable<long> ids = null;

                if (transaction == null)
                {
                    using (ISearchQueryable<T> queryable = GetQueryable(systemID))
                    {
                        ids = queryable.Where(predicate).Select(item => item.ID).ToList();
                    }
                }
                else
                {
                    ids = GetQueryable(systemID, transaction).Where(predicate).Select(item => item.ID).ToList();
                }

                if (ids.Count() == 0)
                    return;

                bool inTransaction = WriteApply<T>(transaction, systemID, ids);

                if (!inTransaction)
                {
                    using (ISearchQueryable<T> queryable = GetQueryable(systemID))
                    {
                        IUpdatable<T> updatable = queryable.Where(item => ids.Contains(item.ID)).AsUpdatable();

                        foreach (var update in updates)
                        {
                            (Type valueType, Expression expression, object value) = update;
                            MethodInfo methodInfo = typeof(LinqExtensions).
                                                    GetMethods(BindingFlags.Public | BindingFlags.Static).Where(item => item.Name == nameof(LinqExtensions.Set)).ElementAt(5).
                                                    MakeGenericMethod(typeof(T), valueType);
                            updatable = (IUpdatable<T>)methodInfo.Invoke(null, new object[] { updatable, expression, value });
                        }

                        updatable.Update();
                    }
                }
                else
                {
                    IUpdatable<T> updatable = GetQueryable(systemID, transaction).Where(item => ids.Contains(item.ID)).AsUpdatable();

                    foreach (var update in updates)
                    {
                        (Type valueType, Expression expression, object value) = update;
                        MethodInfo methodInfo = typeof(LinqExtensions).
                                                GetMethods(BindingFlags.Public | BindingFlags.Static).Where(item => item.Name == nameof(LinqExtensions.Set)).ElementAt(5).
                                                MakeGenericMethod(typeof(T), valueType);
                        updatable = (IUpdatable<T>)methodInfo.Invoke(null, new object[] { updatable, expression, value });
                    }

                    updatable.Update();
                }
            }

            public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
            {
                Update(string.Empty, predicate, updateDictionary, transaction);
            }

            public async Task UpdateAsync(string systemID, T data, ITransaction transaction = null)
            {
                bool inTransaction = await WriteApplyAsync<T>(transaction, systemID, new long[] { data.ID });

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        await dataConnection.Instance.UpdateAsync(data, GetPartitionTableName<T>(dataConnection.Instance, systemID));
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    await dataConnection.UpdateAsync(data, GetPartitionTableName<T>(dataConnection, systemID));
                }
            }

            public Task UpdateAsync(T data, ITransaction transaction = null)
            {
                return UpdateAsync(string.Empty, data, transaction);
            }

            public async Task UpdateAsync(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                IList<Tuple<Type, LambdaExpression, object>> updates = new List<Tuple<Type, LambdaExpression, object>>();

                foreach (var item in updateDictionary)
                    updates.Add(Tuple.Create(typeof(T).GetProperty(item.Key).PropertyType, Expression.Lambda(Expression.Property(parameter, item.Key), parameter), item.Value));

                IEnumerable<long> ids = null;

                if (transaction == null)
                {
                    using (ISearchQueryable<T> queryable = await GetQueryableAsync(systemID))
                    {
                        ids = queryable.Where(predicate).Select(item => item.ID).ToList();
                    }
                }
                else
                {
                    ids = (await GetQueryableAsync(systemID, transaction)).Where(predicate).Select(item => item.ID).ToList();
                }

                if (ids.Count() == 0)
                    return;

                bool inTransaction = await WriteApplyAsync<T>(transaction, systemID, ids);

                if (!inTransaction)
                {
                    using (ISearchQueryable<T> queryable = await GetQueryableAsync(systemID))
                    {
                        IUpdatable<T> updatable = queryable.Where(item => ids.Contains(item.ID)).AsUpdatable();

                        foreach (var update in updates)
                        {
                            (Type valueType, Expression expression, object value) = update;
                            MethodInfo methodInfo = typeof(LinqExtensions).
                                                    GetMethods(BindingFlags.Public | BindingFlags.Static).Where(item => item.Name == nameof(LinqExtensions.Set)).ElementAt(5).
                                                    MakeGenericMethod(typeof(T), valueType);
                            updatable = (IUpdatable<T>)methodInfo.Invoke(null, new object[] { updatable, expression, value });
                        }

                        await updatable.UpdateAsync();
                    }
                }
                else
                {
                    IUpdatable<T> updatable = (await GetQueryableAsync(systemID, transaction)).Where(item => ids.Contains(item.ID)).AsUpdatable();

                    foreach (var update in updates)
                    {
                        (Type valueType, Expression expression, object value) = update;
                        MethodInfo methodInfo = typeof(LinqExtensions).
                                                GetMethods(BindingFlags.Public | BindingFlags.Static).Where(item => item.Name == nameof(LinqExtensions.Set)).ElementAt(5).
                                                MakeGenericMethod(typeof(T), valueType);
                        updatable = (IUpdatable<T>)methodInfo.Invoke(null, new object[] { updatable, expression, value });
                    }

                    await updatable.UpdateAsync();
                }
            }

            public Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
            {
                return UpdateAsync(string.Empty, predicate, updateDictionary, transaction);
            }

            private static void ValidTransaction(string systemID, ITransaction transaction, IEnumerable<long> ids, bool forUpdate = false)
            {
                bool inTransaction = false;

                if (forUpdate)
                    inTransaction = WriteApply<T>(transaction, systemID, ids);
                else inTransaction = ReadApply<T>(transaction, systemID, ids);

                if (!inTransaction)
                    throw new DealException($"当前未查询到事务信息，请先使用{nameof(Linq2DBDaoInstance<T>.BeginTransaction)}开启事务。");
            }

            private async static Task ValidTransactionAsync(string systemID, ITransaction transaction, IEnumerable<long> ids, bool forUpdate = false)
            {
                bool inTransaction = false;

                if (forUpdate)
                    inTransaction = await WriteApplyAsync<T>(transaction, systemID, ids);
                else inTransaction = await ReadApplyAsync<T>(transaction, systemID, ids);

                if (!inTransaction)
                    throw new DealException($"当前未查询到事务信息，请先使用{nameof(Linq2DBDaoInstance<T>.BeginTransactionAsync)}开启事务。");
            }

            public T Get(string systemID, long id, ITransaction transaction, bool forUpdate = false)
            {
                ValidTransaction(systemID, transaction, new long[] { id }, forUpdate);

                return ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().TableName(systemID).SingleOrDefault(item => item.ID == id);
            }

            public T Get(long id, ITransaction transaction, bool forUpdate = false)
            {
                return Get(string.Empty, id, transaction, forUpdate);
            }

            public T Get(string systemID, long id, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return dataConnection.Instance.GetTable<T>().TableName(systemID).SingleOrDefault(item => item.ID == id);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                    return ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).SingleOrDefault(item => item.ID == id);
            }

            public T Get(long id, IDBResourceContent dbResourceContent = null)
            {
                return Get(string.Empty, id, dbResourceContent);
            }

            public async Task<T> GetAsync(string systemID, long id, ITransaction transaction, bool forUpdate = false)
            {
                await ValidTransactionAsync(systemID, transaction, new long[] { id }, forUpdate);
                return await ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().TableName(systemID).SingleOrDefaultAsync(item => item.ID == id);
            }

            public Task<T> GetAsync(long id, ITransaction transaction, bool forUpdate = false)
            {
                return GetAsync(string.Empty, id, transaction, forUpdate);
            }

            public async Task<T> GetAsync(string systemID, long id, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return await dataConnection.Instance.GetTable<T>().TableName(systemID).SingleOrDefaultAsync(item => item.ID == id);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                    return await ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).SingleOrDefaultAsync(item => item.ID == id);
            }

            public Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
            {
                return GetAsync(string.Empty, id, dbResourceContent);
            }

            public int Count(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                IEnumerable<long> ids = GetQueryable(systemID, transaction).Where(predicate ?? EMPTY_PREDICATE).Select(item => item.ID);

                ValidTransaction(systemID, transaction, ids, forUpdate);

                return ids.Count();
            }

            public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                return Count(string.Empty, transaction, predicate, forUpdate);
            }

            public int Count(string systemID, Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return dataConnection.Instance.GetTable<T>().TableName(systemID).Count(predicate ?? EMPTY_PREDICATE);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                    return ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).Count(predicate ?? EMPTY_PREDICATE);
            }

            public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                return Count(string.Empty, predicate, dbResourceContent);
            }

            public async Task<int> CountAsync(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                IEnumerable<long> ids = (await GetQueryableAsync(systemID, transaction)).Where(predicate ?? EMPTY_PREDICATE).Select(item => item.ID);

                await ValidTransactionAsync(systemID, transaction, ids, forUpdate);

                return ids.Count();
            }

            public Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                return CountAsync(string.Empty, transaction, predicate, forUpdate);
            }

            public async Task<int> CountAsync(string systemID, Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return await dataConnection.Instance.GetTable<T>().TableName(systemID).CountAsync(predicate ?? EMPTY_PREDICATE);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                    return await ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).CountAsync(predicate ?? EMPTY_PREDICATE);
            }

            public Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                return CountAsync(string.Empty, predicate, dbResourceContent);
            }

            public IEnumerable<T> Search(string systemID,
                                         ITransaction transaction,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         bool forUpdate = false)
            {
                IQueryable<T> query = GetQueryable(systemID, transaction).Where(predicate ?? EMPTY_PREDICATE);

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

                IQueryable<long> queryaa = query.Select(item => item.ID).Skip(startIndex).Take(count);

                IEnumerable<long> ids = queryaa.ToList();

                ValidTransaction(systemID, transaction, ids, forUpdate);

                return GetQueryable(systemID, transaction).Where(item => ids.Contains(item.ID)).ToList();
            }

            public IEnumerable<T> Search(ITransaction transaction,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         bool forUpdate = false)
            {
                return Search(string.Empty, transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
            }

            public IEnumerable<T> Search(string systemID,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        IQueryable<T> query = dataConnection.Instance.GetTable<T>().TableName(systemID).Where(predicate ?? EMPTY_PREDICATE);
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
                    IQueryable<T> query = ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).Where(predicate ?? EMPTY_PREDICATE);
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

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         IDBResourceContent dbResourceContent = null)
            {
                return Search(string.Empty, predicate, queryOrderBies, startIndex, count, dbResourceContent);
            }

            public async Task<IEnumerable<T>> SearchAsync(string systemID,
                                                          ITransaction transaction,
                                                          Expression<Func<T, bool>> predicate = null,
                                                          IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                          int startIndex = 0,
                                                          int count = int.MaxValue,
                                                          bool forUpdate = false)
            {
                IQueryable<T> query = (await GetQueryableAsync(systemID, transaction)).Where(predicate ?? EMPTY_PREDICATE);

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

                await ValidTransactionAsync(systemID, transaction, ids, forUpdate);

                return (await GetQueryableAsync(systemID, transaction)).Where(item => ids.Contains(item.ID)).ToList();
            }

            public Task<IEnumerable<T>> SearchAsync(ITransaction transaction,
                                                    Expression<Func<T, bool>> predicate = null,
                                                    IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                    int startIndex = 0,
                                                    int count = int.MaxValue,
                                                    bool forUpdate = false)
            {
                return SearchAsync(string.Empty, transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
            }

            public async Task<IEnumerable<T>> SearchAsync(string systemID,
                                                          Expression<Func<T, bool>> predicate = null,
                                                          IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                          int startIndex = 0,
                                                          int count = int.MaxValue,
                                                          IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        IQueryable<T> query = dataConnection.Instance.GetTable<T>().TableName(systemID).Where(predicate ?? EMPTY_PREDICATE);
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
                    IQueryable<T> query = ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).Where(predicate ?? EMPTY_PREDICATE);
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

            public Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null,
                                                    IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                    int startIndex = 0,
                                                    int count = int.MaxValue,
                                                    IDBResourceContent dbResourceContent = null)
            {
                return SearchAsync(string.Empty, predicate, queryOrderBies, startIndex, count, dbResourceContent);
            }

            public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
            {
                return query.Count();
            }

            public int Count<TResult>(IQueryable<TResult> query)
            {
                return query.Count();
            }

            public async Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
            {
                return await query.CountAsync();
            }

            public async Task<int> CountAsync<TResult>(IQueryable<TResult> query)
            {
                return await query.CountAsync();
            }

            public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return query.Skip(startIndex).Take(count).ToList();
            }

            public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return query.Skip(startIndex).Take(count).ToList();
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return await query.Skip(startIndex).Take(count).ToListAsync();
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return await query.Skip(startIndex).Take(count).ToListAsync();
            }

            public ISearchQueryable<T> GetQueryable(string systemID, ITransaction transaction)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)((Linq2DBTransaction)transaction).ResourceInstance;
                return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), true, resourceInstance);
            }

            public ISearchQueryable<T> GetQueryable(ITransaction transaction)
            {
                return GetQueryable(string.Empty, transaction);
            }

            public ISearchQueryable<T> GetQueryable(string systemID, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);
                    return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), false, resourceInstance);
                }
                else
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)dbResourceContent;
                    return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), true, resourceInstance);
                }
            }

            public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
            {
                return GetQueryable(string.Empty, dbResourceContent);
            }

            public Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, ITransaction transaction)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)((Linq2DBTransaction)transaction).ResourceInstance;
                return Task.FromResult<ISearchQueryable<T>>(new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), true, resourceInstance));
            }

            public Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
            {
                return GetQueryableAsync(string.Empty, transaction);
            }

            public Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);
                    return Task.FromResult<ISearchQueryable<T>>(new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), false, resourceInstance));
                }
                else
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)dbResourceContent;
                    return Task.FromResult<ISearchQueryable<T>>(new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), true, resourceInstance));
                }
            }

            public Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
            {
                return GetQueryableAsync(string.Empty, dbResourceContent);
            }

            public Linq2DBDaoInstance(LinqToDbConnectionOptions linqToDbConnectionOptions)
            {
                m_linqToDbConnectionOptions = linqToDbConnectionOptions;
            }

            static Linq2DBDaoInstance()
            {
                EMPTY_PREDICATE = _ => true;
            }
        }
    }
}