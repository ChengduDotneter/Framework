using Common.DAL.Transaction;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.SchemaProvider;
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
using Newtonsoft.Json;

namespace Common.DAL
{
    internal static class Linq2DBDao
    {
        private const int DEFAULT_CONNECTION_COUNT = 10; //最大长连接数
        private const int DEFAULT_CONNECTION_WAITTIMEOUT = 8 * 60 * 60 * 1000; //8小时
        private const int DEFAULT_MAX_TEMP_CONNECTION_COUNT = 10; //最大临时连接数
        private const int TEMP_CONNECTION_TIMEOUT = 60 * 10 * 1000; //临时连接数存活时间
        private static IDictionary<int, DataConnectResourcePool> m_connectionPool;
        private static LinqToDbConnectionOptions m_masterlinqToDbConnectionOptions;//修改用连接
        private static LinqToDbConnectionOptions m_slavelinqToDbConnectionOptions;//查询用连接

        private static readonly int m_dataConnectionOutTime;//连接超时时间
        /// <summary>
        /// 设置表名
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="table">数据表</param>
        /// <param name="systemID">分区id</param>
        /// <returns></returns>
        private static ITable<T> TableName<T>(this ITable<T> table, string systemID) where T : class, IEntity, new()
        {
            return LinqExtensions.TableName(table, GetPartitionTableName<T>(systemID));
        }
        /// <summary>
        /// 获取分区表名
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="systemID"></param>
        /// <returns></returns>
        private static string GetPartitionTableName<T>(string systemID) where T : class, IEntity, new()
        {
            string tablePostFix = string.IsNullOrEmpty(systemID) ? string.Empty : $"_{systemID}";
            string tableName = Convert.ToBoolean(ConfigManager.Configuration["IsNotLowerTableName"]) ? $"{typeof(T).Name}{tablePostFix}" : $"{typeof(T).Name}{tablePostFix}".ToLower();

            return tableName;
        }
        /// <summary>
        /// 构造函数
        /// </summary>
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
                if (traceInfo.TraceInfoStep == TraceInfoStep.Error)
                    DaoFactory.LogHelper.Error("linq2DB_master", traceInfo.SqlText);
            });

            slaveLinqToDbConnectionOptionsBuilder.WithTracing(traceInfo =>
            {
                if (traceInfo.TraceInfoStep == TraceInfoStep.Error)
                    DaoFactory.LogHelper.Error("linq2DB_slave", traceInfo.SqlText);
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
        /// <summary>
        /// 创建修改用数据库连接对象
        /// </summary>
        /// <returns></returns>
        private static DataConnectionInstance CreateMasterDataConnection()
        {
            return new DataConnectionInstance(Environment.TickCount + m_dataConnectionOutTime, m_masterlinqToDbConnectionOptions);
        }
        /// <summary>
        /// 创建查询用数据库连接对象
        /// </summary>
        /// <returns></returns>
        private static DataConnectionInstance CreateSlaveDataConnection()
        {
            return new DataConnectionInstance(Environment.TickCount + m_dataConnectionOutTime, m_slavelinqToDbConnectionOptions);
        }
        /// <summary>
        /// 关闭数据库连接
        /// </summary>
        /// <param name="dataConnectionInstance"></param>
        private static void CloseDataConnection(DataConnectionInstance dataConnectionInstance)
        {
            dataConnectionInstance.Close();
            dataConnectionInstance.Dispose();
        }
        /// <summary>
        /// 获取数据库连接资源
        /// </summary>
        //TODO: 连接池BUG
        private class ConnectionInstance : IResourceInstance<DataConnectionInstance>
        {
            public ConnectionInstance(LinqToDbConnectionOptions linqToDbConnectionOptions)
            {
                Instance = new DataConnectionInstance(0, linqToDbConnectionOptions);
            }

            public void Dispose()
            {
                Instance.Dispose();
            }

            public DataConnectionInstance Instance { get; private set; }
        }

        //TODO: 连接池BUG
        private static IResourceInstance<DataConnectionInstance> CreateConnection(LinqToDbConnectionOptions linqToDbConnectionOptions)
        {
            //return m_connectionPool[linqToDbConnectionOptions.GetHashCode()].ApplyInstance();
            return new ConnectionInstance(linqToDbConnectionOptions);//返回数据库连接资源
        }
        /// <summary>
        /// 释放数据库连接资源
        /// </summary>
        /// <param name="resourceInstance"></param>
        private static void DisposeConnection(IResourceInstance<DataConnectionInstance> resourceInstance)
        {
            resourceInstance.Dispose();
        }
        /// <summary>
        /// 获取数据查询接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static ISearchQuery<T> GetLinq2DBSearchQuery<T>()
            where T : class, IEntity, new()
        {
            return new Linq2DBDaoInstance<T>(m_slavelinqToDbConnectionOptions);
        }
        /// <summary>
        /// 获取数据操作接口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEditQuery<T> GetLinq2DBEditQuery<T>()
            where T : class, IEntity, new()
        {
            return new Linq2DBDaoInstance<T>(m_masterlinqToDbConnectionOptions);
        }
        //创建表
        private class Linq2DBCreateTableQueryInstance : ICreateTableQuery
        {
            public Task CreateTable(string systemID, IEnumerable<Type> tableTypes)
            {
                return CreateTables(systemID, tableTypes);
            }
        }
        /// <summary>
        /// 创建表接口 返回创建表的实现
        /// </summary>
        /// <returns></returns>
        public static ICreateTableQuery GetLinq2DBCreateTableQueryInstance()
        {
            return new Linq2DBCreateTableQueryInstance();
        }
        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="systemID">分区id</param>
        /// <param name="tableTypes">实体集合</param>
        /// <returns></returns>
        public static Task CreateTables(string systemID, IEnumerable<Type> tableTypes)
        {
            if (tableTypes.IsNullOrEmpty())//如果表集合为空 则返回
                return Task.CompletedTask;

            return Task.Factory.StartNew(() =>
            {
                IResourceInstance<DataConnectionInstance> dataConnection = null;
                bool codeFirst = Convert.ToBoolean(ConfigManager.Configuration["IsCodeFirst"]);//获取配置 是否创建表

                if (!codeFirst)
                    return;

                try
                {
                    dataConnection = CreateConnection(m_slavelinqToDbConnectionOptions);//创建数据库连接对象

                    foreach (Type tableType in tableTypes)//遍历实体集合
                    {
                        //判断是否为class 是否有构造函数 是否继承IEntity接口
                        if (!tableType.IsClass || tableType.GetConstructor(new Type[0]) == null || tableType.GetInterface(typeof(IEntity).Name) == null)
                            continue;
                        //是否有不分区建表特性
                        DontSplitSystemAttribute dontSplitSystemAttribute = tableType.GetCustomAttribute<DontSplitSystemAttribute>();

                        //调用Linq2DBDao.GetPartitionTableName Static方法 NonPublic不公开，泛型为 tableType 最后执行并传参 dontSplitSystemAttribute为null则穿systemID 否则传空
                        string tableName = (string)typeof(Linq2DBDao).GetMethod(nameof(GetPartitionTableName), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(tableType).
                                                                      Invoke(null, new object[] { dontSplitSystemAttribute == null ? systemID : string.Empty });
                        //调用 Linq2DBDao.CreateTable Static方法 访问修饰符NonPublic，泛型 tableType，传参 执行
                        typeof(Linq2DBDao).GetMethod(nameof(CreateTable), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(tableType).Invoke(null, new object[]
                        {
                            dataConnection.Instance,
                            tableName
                        });
                    }
                }
                finally
                {
                    if (dataConnection != null)
                        DisposeConnection(dataConnection);
                }
            });
        }
        /// <summary>
        /// 创建表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataContext">数据库操作对象</param>
        /// <param name="tableName">表名字</param>
        private static void CreateTable<T>(IDataContext dataContext, string tableName) where T : class, IEntity, new()
        {
            DataConnection dataConnection = (DataConnection)dataContext;
            //判断这个表名是否存在于数据库
            if (!dataConnection.DataProvider.GetSchemaProvider().GetSchema(dataConnection, new GetSchemaOptions
            {
                GetProcedures = false
            }).Tables.Any(item => item.TableName == tableName))
            {
                dataContext.CreateTable<T>(tableName);
            }
        }
        /// <summary>
        /// 获取数据库资源
        /// </summary>
        /// <returns></returns>
        //TODO: 连接池BUG
        public static IDBResourceContent GetDBResourceContent()
        {
            //return new Linq2DBResourceContent(m_connectionPool[m_slavelinqToDbConnectionOptions.GetHashCode()].ApplyInstance());
            return new Linq2DBResourceContent(new ConnectionInstance(m_slavelinqToDbConnectionOptions));
        }
        /// <summary>
        /// 申请表锁事务资源
        /// </summary>
        /// <typeparam name="TResource">表</typeparam>
        /// <param name="transaction">执行的事务</param>
        /// <param name="systemID">分区id</param>
        /// <returns></returns>
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
        /// <summary>
        /// 申请异步表锁事务
        /// </summary>
        /// <typeparam name="TResource">申请事务的表</typeparam>
        /// <param name="transaction">执行的事务</param>
        /// <param name="systemID">分区id</param>
        /// <returns></returns>
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
        /// <summary>
        /// 申请行写锁事务
        /// </summary>
        /// <typeparam name="TResource">申请事务的表</typeparam>
        /// <param name="transaction">执行的事务</param>
        /// <param name="systemID">分区id</param>
        /// <param name="ids">数据id集合 用于行写锁</param>
        /// <returns></returns>
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
        /// <summary>
        /// 申请异步行写锁事务
        /// </summary>
        /// <typeparam name="TResource">申请事务的表</typeparam>
        /// <param name="transaction">执行的事务</param>
        /// <param name="systemID">分区id</param>
        /// <param name="ids">数据id集合 用于行写锁</param>
        /// <returns></returns>
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
        /// <summary>
        /// 申请行读锁事务
        /// </summary>
        /// <typeparam name="TResource">申请事务的表</typeparam>
        /// <param name="transaction">执行的事务</param>
        /// <param name="systemID">分区id</param>
        /// <param name="ids">加锁的行数据id</param>
        /// <returns></returns>
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
        /// <summary>
        /// 申请异步行读锁
        /// </summary>
        /// <typeparam name="TResource">加锁的表</typeparam>
        /// <param name="transaction">执行的事务</param>
        /// <param name="systemID">分区id</param>
        /// <param name="ids">加锁的行数据 i</param>
        /// <returns></returns>
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
        /// <summary>
        /// 释放事务资源
        /// </summary>
        /// <param name="identity">事务标识</param>
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
        /// <summary>
        /// 事务具体操作类
        /// </summary>
        private class Linq2DBTransaction : ITransaction
        {
            //数据库连接
            private DataConnectionTransaction m_dataConnectionTransaction;
            //资源
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
                try
                {
                    await m_dataConnectionTransaction.DisposeAsync();
                    DisposeConnection(m_resourceInstance);
                }
                catch (Exception exception)
                {
                    await DaoFactory.LogHelper.Error($"{nameof(Linq2DBTransaction)}_{nameof(Dispose)}", JsonConvert.SerializeObject(exception));
                }
                finally
                {
                    if (DistributedLock)
                        Release(Identity);
                }
            }

            public void Rollback()
            {
                try
                {
                    m_dataConnectionTransaction.Rollback();
                }
                catch (Exception exception)
                {
                    DaoFactory.LogHelper.Error($"{nameof(Linq2DBTransaction)}_{nameof(Rollback)}", JsonConvert.SerializeObject(exception));
                }
            }

            public async Task RollbackAsync()
            {
                try
                {
                    await m_dataConnectionTransaction.RollbackAsync();
                }
                catch (Exception exception)
                {
                    await DaoFactory.LogHelper.Error($"{nameof(Linq2DBTransaction)}_{nameof(RollbackAsync)}", JsonConvert.SerializeObject(exception));
                }
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
        /// <summary>
        /// 数据查询接口实现类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        private class Linq2DBQueryable<T> : ISearchQueryable<T>
            where T : class, IEntity, new()
        {
            private ITable<T> m_table;//操作的表
            private bool m_notAutoRealse;
            private IResourceInstance<DataConnectionInstance> m_resourceInstance;//数据库连接资源

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
            private LinqToDbConnectionOptions m_linqToDbConnectionOptions;//数据库连接配置项
            private static readonly Expression<Func<T, bool>> EMPTY_PREDICATE;
            public Linq2DBDaoInstance(LinqToDbConnectionOptions linqToDbConnectionOptions)
            {
                m_linqToDbConnectionOptions = linqToDbConnectionOptions;
            }

            static Linq2DBDaoInstance()
            {
                EMPTY_PREDICATE = _ => true;
            }
            public ITransaction BeginTransaction(bool distributedLock = false, int weight = 0)
            {
                //获取数据库连接资源
                IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);
                //共享锁，是否启用分布式事务，事务权重，数据库连接资源
                return new Linq2DBTransaction(resourceInstance.Instance.BeginTransaction(IsolationLevel.ReadCommitted), distributedLock, weight, resourceInstance);
            }

            public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = false, int weight = 0)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);
                return new Linq2DBTransaction(await resourceInstance.Instance.BeginTransactionAsync(IsolationLevel.ReadCommitted), distributedLock, weight, resourceInstance);
            }
            /// <summary>
            /// 删除操作
            /// </summary>
            /// <param name="systemID">分区id</param>
            /// <param name="transaction">执行的事务</param>
            /// <param name="ids">加锁的行数据id</param>
            public void Delete(string systemID, ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = WriteApply<T>(transaction, systemID, ids);
                //申请行写事务
                if (!inTransaction)
                {
                    //申请失败则根据连接配置项获取数据库连接资源
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        //TableName(systemID) 是获取表名 是否分区等
                        dataConnection.Instance.GetTable<T>().TableName(systemID).Delete(item => ids.Contains(item.ID));
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);//释放资源
                    }
                }
                else
                {
                    //事务申请成功transaction 转为 Linq2DBTransaction 在点Context 再转为DataConnectionTransaction
                    ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().TableName(systemID).Delete(item => ids.Contains(item.ID));
                }
            }
            /// <summary>
            /// 不分区的删除操作
            /// </summary>
            /// <param name="transaction">执行的事务</param>
            /// <param name="ids"></param>
            public void Delete(ITransaction transaction = null, params long[] ids)
            {
                Delete(string.Empty, transaction, ids);
            }
            /// <summary>
            /// 分区异步删除数据
            /// </summary>
            /// <param name="systemID">分区id</param>
            /// <param name="transaction">执行的事务</param>
            /// <param name="ids"></param>
            /// <returns></returns>
            public async Task DeleteAsync(string systemID, ITransaction transaction = null, params long[] ids)
            {
                //申请行写数据
                bool inTransaction = await WriteApplyAsync<T>(transaction, systemID, ids);

                if (!inTransaction)
                {
                    //申请失败 创建数据库连接资源
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
                    //事务申请成功transaction 转为 Linq2DBTransaction 在点Context 再转为DataConnectionTransaction
                    await ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().TableName(systemID).DeleteAsync(item => ids.Contains(item.ID));
                }
            }
            /// <summary>
            /// 不分区异步删除数据
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="ids"></param>
            /// <returns></returns>
            public Task DeleteAsync(ITransaction transaction = null, params long[] ids)
            {
                return DeleteAsync(string.Empty, transaction, ids);
            }
            /// <summary>
            /// 分区插入数据
            /// </summary>
            /// <param name="systemID">分区id</param>
            /// <param name="transaction">执行的事务</param>
            /// <param name="datas">插入的数据</param>
            public void Insert(string systemID, ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction, systemID);//申请事务表锁资源 因为插入时防止冲突 导致大家插入同一条数据 所以锁表

                if (!inTransaction)//判断事务申请成功没
                {
                    //创建数据库连接对象资源
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
            /// <summary>
            /// 不分区插入数据
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="datas"></param>
            public void Insert(ITransaction transaction = null, params T[] datas)
            {
                Insert(string.Empty, transaction, datas);
            }
            /// <summary>
            /// 分区异步插入
            /// </summary>
            /// <param name="systemID">分区id</param>
            /// <param name="transaction">执行的事务</param>
            /// <param name="datas"></param>
            /// <returns></returns>
            public async Task InsertAsync(string systemID, ITransaction transaction = null, params T[] datas)
            {
                //申请异步表锁事务
                bool inTransaction = await ApplyAsync<T>(transaction, systemID);

                if (!inTransaction)
                {
                    //获取数据库连接资源
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        await dataConnection.Instance.GetTable<T>().TableName(systemID).BulkCopyAsync(datas);//异步对表执行大容量操作 由表标识
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
            /// <summary>
            /// 异步不分区插入
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="datas"></param>
            /// <returns></returns>
            public Task InsertAsync(ITransaction transaction = null, params T[] datas)
            {
                return InsertAsync(string.Empty, transaction, datas);
            }

            /// <summary>
            /// 合并 存在则替换 不存在则更新
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <param name="datas"></param>
            public void Merge(string systemID, ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction, systemID) && WriteApply<T>(transaction, systemID, datas.Select(item => item.ID));

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        for (int i = 0; i < datas.Length; i++)
                            dataConnection.Instance.InsertOrReplace(datas[i], GetPartitionTableName<T>(systemID));
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
                        dataConnection.InsertOrReplace(datas[i], GetPartitionTableName<T>(systemID));
                }
            }
            /// <summary>
            /// 不用传系统id的合并 存在则替换 不存在更新
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="datas"></param>
            public void Merge(ITransaction transaction = null, params T[] datas)
            {
                Merge(string.Empty, transaction, datas);
            }
            /// <summary>
            /// 异步合并 存在则更新不存在则插入
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <param name="datas"></param>
            /// <returns></returns>
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
                            tasks[i] = dataConnection.Instance.InsertOrReplaceAsync(datas[i], GetPartitionTableName<T>(systemID));
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
                        tasks[i] = dataConnection.InsertOrReplaceAsync(datas[i], GetPartitionTableName<T>(systemID));

                    await Task.WhenAll(tasks);
                }
            }
            /// <summary>
            /// 不用传系统id的合并 存在则更新 不存在插入
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="datas"></param>
            /// <returns></returns>
            public Task MergeAsync(ITransaction transaction = null, params T[] datas)
            {
                return MergeAsync(string.Empty, transaction, datas);
            }
            /// <summary>
            /// 修改
            /// </summary>
            /// <param name="systemID">系统id</param>
            /// <param name="data">修改的数据</param>
            /// <param name="transaction">执行的事务</param>
            public void Update(string systemID, T data, ITransaction transaction = null)
            {
                bool inTransaction = WriteApply<T>(transaction, systemID, new long[] { data.ID });//根据数据中的id 申请行写事务

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);//创建数据库连接资源

                    try
                    {
                        dataConnection.Instance.Update(data, GetPartitionTableName<T>(systemID));//修改数据 根据表名
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;
                    dataConnection.Update(data, GetPartitionTableName<T>(systemID));//在事务中根据表名修改数据
                }
            }
            /// <summary>
            /// 不用传系统id修改
            /// </summary>
            /// <param name="data"></param>
            /// <param name="transaction"></param>
            public void Update(T data, ITransaction transaction = null)
            {
                Update(string.Empty, data, transaction);
            }
            /// <summary>
            /// 修改
            /// </summary>
            /// <param name="systemID">系统id</param>
            /// <param name="predicate">修改条件</param>
            /// <param name="updateDictionary">要修改的数据键值对</param>
            /// <param name="transaction">执行的事务</param>
            public void Update(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                IList<Tuple<Type, LambdaExpression, object>> updates = new List<Tuple<Type, LambdaExpression, object>>();

                foreach (var item in updateDictionary)
                    updates.Add(Tuple.Create(typeof(T).GetProperty(item.Key).PropertyType, Expression.Lambda(Expression.Property(parameter, item.Key), parameter), item.Value));

                IEnumerable<long> ids = null;//id

                if (transaction == null)//判断事务是否为空
                {
                    using (ISearchQueryable<T> queryable = GetQueryable(systemID))
                    {
                        //获取queryable中的id
                        ids = queryable.Where(predicate).Select(item => item.ID).ToList();
                    }
                }
                else
                {
                    //在事务中获取
                    ids = GetQueryable(systemID, transaction).Where(predicate).Select(item => item.ID).ToList();
                }

                if (ids.Count() == 0)
                    return;

                bool inTransaction = WriteApply<T>(transaction, systemID, ids);//申请行写事务资源

                if (!inTransaction)//判断是否申请成功
                {
                    using (ISearchQueryable<T> queryable = GetQueryable(systemID))
                    {
                        //把idqueryable集合转为 IUpdatable对象
                        IUpdatable<T> updatable = queryable.Where(item => ids.Contains(item.ID)).AsUpdatable();
                        //遍历他
                        foreach (var update in updates)
                        {
                            (Type valueType, Expression expression, object value) = update;
                            MethodInfo methodInfo = typeof(LinqExtensions).//LinqExtensions这个类Public级别 Static方法 查找 Set方法 第五个 并设置他的泛型为t 和valueType
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
            /// <summary>
            /// 修改不传系统id
            /// </summary>
            /// <param name="predicate"></param>
            /// <param name="updateDictionary"></param>
            /// <param name="transaction"></param>
            public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
            {
                Update(string.Empty, predicate, updateDictionary, transaction);
            }
            /// <summary>
            /// 异步修改
            /// </summary>
            /// <param name="systemID">系统id</param>
            /// <param name="data">修改的数据</param>
            /// <param name="transaction">执行的事务</param>
            /// <returns></returns>
            public async Task UpdateAsync(string systemID, T data, ITransaction transaction = null)
            {
                bool inTransaction = await WriteApplyAsync<T>(transaction, systemID, new long[] { data.ID });//根据数据集合的id申请行写锁

                if (!inTransaction)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);//创建数据库连接资源

                    try
                    {
                        await dataConnection.Instance.UpdateAsync(data, GetPartitionTableName<T>(systemID));//修改数据
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    DataConnection dataConnection = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection;//在事务中修改
                    await dataConnection.UpdateAsync(data, GetPartitionTableName<T>(systemID));
                }
            }
            /// <summary>
            /// 异步修改 不用传系统id
            /// </summary>
            /// <param name="data"></param>
            /// <param name="transaction"></param>
            /// <returns></returns>
            public Task UpdateAsync(T data, ITransaction transaction = null)
            {
                return UpdateAsync(string.Empty, data, transaction);
            }
            /// <summary>
            /// 传系统id的异步修改
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="predicate"></param>
            /// <param name="updateDictionary"></param>
            /// <param name="transaction"></param>
            /// <returns></returns>
            public async Task UpdateAsync(string systemID, Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));//条件
                IList<Tuple<Type, LambdaExpression, object>> updates = new List<Tuple<Type, LambdaExpression, object>>();

                foreach (var item in updateDictionary)
                    updates.Add(Tuple.Create(typeof(T).GetProperty(item.Key).PropertyType, Expression.Lambda(Expression.Property(parameter, item.Key), parameter), item.Value));

                IEnumerable<long> ids = null;

                if (transaction == null)
                {
                    using (ISearchQueryable<T> queryable = await GetQueryableAsync(systemID))//根基系统id获取查询queryable查询接口
                    {
                        ids = queryable.Where(predicate).Select(item => item.ID).ToList();//获取要加锁的行数据id
                    }
                }
                else
                {
                    ids = (await GetQueryableAsync(systemID, transaction)).Where(predicate).Select(item => item.ID).ToList();
                }

                if (ids.Count() == 0)
                    return;

                bool inTransaction = await WriteApplyAsync<T>(transaction, systemID, ids);//申请异步行写锁事务

                if (!inTransaction)
                {
                    using (ISearchQueryable<T> queryable = await GetQueryableAsync(systemID))//获取查询queryable
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
            /// <summary>
            /// 不传系统id的异步修改
            /// </summary>
            /// <param name="predicate"></param>
            /// <param name="updateDictionary"></param>
            /// <param name="transaction"></param>
            /// <returns></returns>
            public Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> updateDictionary, ITransaction transaction = null)
            {
                return UpdateAsync(string.Empty, predicate, updateDictionary, transaction);
            }
            /// <summary>
            /// 事务验证 get search count申请行读锁 修改时申请行写锁
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <param name="ids"></param>
            /// <param name="forUpdate"></param>
            private static void ValidTransaction(string systemID, ITransaction transaction, IEnumerable<long> ids, bool forUpdate = false)
            {
                bool inTransaction = false;

                if (forUpdate)
                    inTransaction = WriteApply<T>(transaction, systemID, ids);
                else inTransaction = ReadApply<T>(transaction, systemID, ids);

                if (!inTransaction)
                    throw new DealException($"当前未查询到事务信息，请先使用{nameof(Linq2DBDaoInstance<T>.BeginTransaction)}开启事务。");
            }
            /// <summary>
            ///  事务验证 get search count 修改时异步申请行写锁
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <param name="ids"></param>
            /// <param name="forUpdate"></param>
            private async static Task ValidTransactionAsync(string systemID, ITransaction transaction, IEnumerable<long> ids, bool forUpdate = false)
            {
                bool inTransaction = false;

                if (forUpdate)//修改操作时 行写锁
                    inTransaction = await WriteApplyAsync<T>(transaction, systemID, ids);
                else inTransaction = await ReadApplyAsync<T>(transaction, systemID, ids);//查询操作室行读锁

                if (!inTransaction)
                    throw new DealException($"当前未查询到事务信息，请先使用{nameof(Linq2DBDaoInstance<T>.BeginTransactionAsync)}开启事务。");
            }
            /// <summary>
            /// 事务中单个查询 行读锁
            /// </summary>
            /// <param name="systemID">系统id</param>
            /// <param name="id"></param>
            /// <param name="transaction"></param>
            /// <param name="forUpdate">是否为修改操作</param>
            /// <returns></returns>
            public T Get(string systemID, long id, ITransaction transaction, bool forUpdate = false)
            {
                ValidTransaction(systemID, transaction, new long[] { id }, forUpdate);//事务验证与申请
                //事务中查询并返回数据
                return ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().TableName(systemID).SingleOrDefault(item => item.ID == id);
            }
            /// <summary>
            /// 查询单个数据
            /// </summary>
            /// <param name="id"></param>
            /// <param name="transaction">执行的事务</param>
            /// <param name="forUpdate">是否为修改操作</param>
            /// <returns></returns>
            public T Get(long id, ITransaction transaction, bool forUpdate = false)
            {
                return Get(string.Empty, id, transaction, forUpdate);
            }
            /// <summary>
            /// 使用同一个数据库连接的get 
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="id"></param>
            /// <param name="dbResourceContent">数据库连接资源</param>
            /// <returns></returns>
            public T Get(string systemID, long id, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)//判断数据库资源链接是否为空
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);//为空重新创建

                    try
                    {
                        return dataConnection.Instance.GetTable<T>().TableName(systemID).SingleOrDefault(item => item.ID == id);//获取单个数据并返回去
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else //不为空则使用传递的数据库资源访问数据库
                    return ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).SingleOrDefault(item => item.ID == id);
            }
            /// <summary>
            /// 不传系统id的get
            /// </summary>
            /// <param name="id"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
            public T Get(long id, IDBResourceContent dbResourceContent = null)
            {
                return Get(string.Empty, id, dbResourceContent);
            }
            /// <summary>
            /// 传系统id的异步get
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="id"></param>
            /// <param name="transaction">事务</param>
            /// <param name="forUpdate">是否为修改</param>
            /// <returns></returns>
            public async Task<T> GetAsync(string systemID, long id, ITransaction transaction, bool forUpdate = false)
            {
                await ValidTransactionAsync(systemID, transaction, new long[] { id }, forUpdate);//事务验证 申请异步行读锁
                return await ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().TableName(systemID).SingleOrDefaultAsync(item => item.ID == id);
            }
            /// <summary>
            /// 异步get方法
            /// </summary>
            /// <param name="id"></param>
            /// <param name="transaction"></param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public Task<T> GetAsync(long id, ITransaction transaction, bool forUpdate = false)
            {
                return GetAsync(string.Empty, id, transaction, forUpdate);
            }
            /// <summary>
            /// 同一个数据库连接的异步get
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="id"></param>
            /// <param name="dbResourceContent">数据库连接资源</param>
            /// <returns></returns>
            public async Task<T> GetAsync(string systemID, long id, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)//判断数据库连接资源是否为空
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);//为空创建

                    try
                    {
                        return await dataConnection.Instance.GetTable<T>().TableName(systemID).SingleOrDefaultAsync(item => item.ID == id);//使用新创建的查询
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else//不为空则使用传递的去查询
                    return await ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).SingleOrDefaultAsync(item => item.ID == id);
            }
            /// <summary>
            /// 异步get方法 同一个连接资源下
            /// </summary>
            /// <param name="id"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
            public Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
            {
                return GetAsync(string.Empty, id, dbResourceContent);
            }
            /// <summary>
            /// 获取数据条数
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <param name="predicate">统计的条件</param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public int Count(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                IEnumerable<long> ids = GetQueryable(systemID, transaction).Where(predicate ?? EMPTY_PREDICATE).Select(item => item.ID);

                ValidTransaction(systemID, transaction, ids, forUpdate);

                return ids.Count();
            }
            /// <summary>
            /// 根据条件统计总数
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="predicate"></param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                return Count(string.Empty, transaction, predicate, forUpdate);
            }
            /// <summary>
            /// 同一个连接资源下统计
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="predicate"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
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
            /// <summary>
            /// 同一个连接资源下统计 不传系统id
            /// </summary>
            /// <param name="predicate"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
            public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                return Count(string.Empty, predicate, dbResourceContent);
            }
            /// <summary>
            /// 事务中 异步统计
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <param name="predicate"></param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public async Task<int> CountAsync(string systemID, ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                IEnumerable<long> ids = (await GetQueryableAsync(systemID, transaction)).Where(predicate ?? EMPTY_PREDICATE).Select(item => item.ID);

                await ValidTransactionAsync(systemID, transaction, ids, forUpdate);

                return ids.Count();
            }
            /// <summary>
            /// 事务中异步统计
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="predicate"></param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                return CountAsync(string.Empty, transaction, predicate, forUpdate);
            }
            /// <summary>
            /// 同一个连接资源下 异步统计
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="predicate"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
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
            /// <summary>
            /// 同一个资源下异步统计
            /// </summary>
            /// <param name="predicate"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
            public Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                return CountAsync(string.Empty, predicate, dbResourceContent);
            }
            /// <summary>
            /// 查询
            /// </summary>
            /// <param name="systemID">系统id</param>
            /// <param name="transaction">执行的事务</param>
            /// <param name="predicate">查询条件</param>
            /// <param name="queryOrderBies">排序条件</param>
            /// <param name="startIndex">开始页码</param>
            /// <param name="count">返回数量</param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public IEnumerable<T> Search(string systemID,
                                         ITransaction transaction,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         bool forUpdate = false)
            {
                IQueryable<T> query = GetQueryable(systemID, transaction).Where(predicate ?? EMPTY_PREDICATE);//获取查询querylable

                bool orderd = false;

                if (queryOrderBies != null)
                {
                    foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                    {
                        if (queryOrderBy.OrderByType == OrderByType.Asc)
                        {
                            if (!orderd)//判断是否已经排序过了
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

                IQueryable<long> queryaa = query.Select(item => item.ID).Skip(startIndex).Take(count);//查询要加锁的数据行id

                IEnumerable<long> ids = queryaa.ToList();

                ValidTransaction(systemID, transaction, ids, forUpdate);//事务验证并加读锁

                return GetQueryable(systemID, transaction).Where(item => ids.Contains(item.ID)).ToList();//返回查询数据
            }
            /// <summary>
            /// 同上 区别在于不传系统id
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="predicate"></param>
            /// <param name="queryOrderBies"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public IEnumerable<T> Search(ITransaction transaction,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         bool forUpdate = false)
            {
                return Search(string.Empty, transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
            }
            /// <summary>
            /// 查询
            /// </summary>
            /// <param name="systemID">系统id</param>
            /// <param name="predicate">查询条件</param>
            /// <param name="queryOrderBies">排序条件</param>
            /// <param name="startIndex">开始页</param>
            /// <param name="count">总条数</param>
            /// <param name="dbResourceContent">数据库连接资源 目的查询合并使用同一个连接</param>
            /// <returns></returns>
            public IEnumerable<T> Search(string systemID,
                                         Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> dataConnection = CreateConnection(m_linqToDbConnectionOptions);//获取数据库连接资源

                    try
                    {
                        IQueryable<T> query = dataConnection.Instance.GetTable<T>().TableName(systemID).Where(predicate ?? EMPTY_PREDICATE);//获取查询用query
                        bool orderd = false;

                        if (queryOrderBies != null)//添加排序条件
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

                        return query.Skip(startIndex).Take(count).ToList();//返回查询的数据
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else//
                {
                    IQueryable<T> query = ((IResourceInstance<DataConnectionInstance>)dbResourceContent).Instance.GetTable<T>().TableName(systemID).Where(predicate ?? EMPTY_PREDICATE);//获取查询queryable
                    bool orderd = false;//初始为false 排序过一次为true

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

                    return query.Skip(startIndex).Take(count).ToList();//直接返回
                }
            }
            /// <summary>
            /// 同上
            /// </summary>
            /// <param name="predicate"></param>
            /// <param name="queryOrderBies"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue,
                                         IDBResourceContent dbResourceContent = null)
            {
                return Search(string.Empty, predicate, queryOrderBies, startIndex, count, dbResourceContent);
            }
            /// <summary>
            /// 异步查询
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <param name="predicate"></param>
            /// <param name="queryOrderBies"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public async Task<IEnumerable<T>> SearchAsync(string systemID,
                                                          ITransaction transaction,
                                                          Expression<Func<T, bool>> predicate = null,
                                                          IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                          int startIndex = 0,
                                                          int count = int.MaxValue,
                                                          bool forUpdate = false)
            {
                IQueryable<T> query = (await GetQueryableAsync(systemID, transaction)).Where(predicate ?? EMPTY_PREDICATE);//获取查询query

                bool orderd = false;//是否以排污

                if (queryOrderBies != null)//是否有排序条件
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

                IEnumerable<long> ids = query.Select(item => item.ID).Skip(startIndex).Take(count).ToList();//把要查询的数据行加读锁

                await ValidTransactionAsync(systemID, transaction, ids, forUpdate);//事务验证 并加读锁

                return (await GetQueryableAsync(systemID, transaction)).Where(item => ids.Contains(item.ID)).ToList();//查询并返回数据
            }
            /// <summary>
            /// 同上
            /// </summary>
            /// <param name="transaction"></param>
            /// <param name="predicate"></param>
            /// <param name="queryOrderBies"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <param name="forUpdate"></param>
            /// <returns></returns>
            public Task<IEnumerable<T>> SearchAsync(ITransaction transaction,
                                                    Expression<Func<T, bool>> predicate = null,
                                                    IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                    int startIndex = 0,
                                                    int count = int.MaxValue,
                                                    bool forUpdate = false)
            {
                return SearchAsync(string.Empty, transaction, predicate, queryOrderBies, startIndex, count, forUpdate);
            }
            /// <summary>
            /// 异步的查询 与同步查询一样 区别在于这是异步
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="predicate"></param>
            /// <param name="queryOrderBies"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
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
            //同上
            public Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null,
                                                    IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                    int startIndex = 0,
                                                    int count = int.MaxValue,
                                                    IDBResourceContent dbResourceContent = null)
            {
                return SearchAsync(string.Empty, predicate, queryOrderBies, startIndex, count, dbResourceContent);
            }
            /// <summary>
            /// 事务中获取数据的总条数
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="transaction"></param>
            /// <param name="query"></param>
            /// <returns></returns>
            public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
            {
                return query.Count();
            }
            /// <summary>
            /// 获取事务的条数
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="query"></param>
            /// <returns></returns>
            public int Count<TResult>(IQueryable<TResult> query)
            {
                return query.Count();
            }
            /// <summary>
            /// 一步获取
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="transaction"></param>
            /// <param name="query"></param>
            /// <returns></returns>
            public async Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
            {
                return await query.CountAsync();
            }
            /// <summary>
            /// 异步获取数据条数
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="query"></param>
            /// <returns></returns>
            public async Task<int> CountAsync<TResult>(IQueryable<TResult> query)
            {
                return await query.CountAsync();
            }
            /// <summary>
            /// 没有查询条件的查询
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="transaction"></param>
            /// <param name="query"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return query.Skip(startIndex).Take(count).ToList();
            }
            /// <summary>
            /// 没有事务参数的查询
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="query"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return query.Skip(startIndex).Take(count).ToList();
            }
            /// <summary>
            /// 异步查询 没有查询条件
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="transaction"></param>
            /// <param name="query"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return await query.Skip(startIndex).Take(count).ToListAsync();
            }
            /// <summary>
            /// 没有查询条件
            /// </summary>
            /// <typeparam name="TResult"></typeparam>
            /// <param name="query"></param>
            /// <param name="startIndex"></param>
            /// <param name="count"></param>
            /// <returns></returns>
            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                return await query.Skip(startIndex).Take(count).ToListAsync();
            }
            /// <summary>
            /// 事务中获取linq查询接口
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <returns></returns>
            public ISearchQueryable<T> GetQueryable(string systemID, ITransaction transaction)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)((Linq2DBTransaction)transaction).ResourceInstance;
                return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), true, resourceInstance);
            }
            /// <summary>
            /// 同上 但是不传系统id
            /// </summary>
            /// <param name="transaction"></param>
            /// <returns></returns>
            public ISearchQueryable<T> GetQueryable(ITransaction transaction)
            {
                return GetQueryable(string.Empty, transaction);
            }
            /// <summary>
            ///根据数据库资源实例获取linq查询接口
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
            public ISearchQueryable<T> GetQueryable(string systemID, IDBResourceContent dbResourceContent = null)
            {
                if (dbResourceContent == null)
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = CreateConnection(m_linqToDbConnectionOptions);//获取数据库连接资源
                    return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), false, resourceInstance);//返回linq查询接口
                }
                else
                {
                    IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)dbResourceContent;//获取数据库连接资源
                    return new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), true, resourceInstance);//返回linq查询接口
                }
            }
            /// <summary>
            /// 同上但是不传系统id
            /// </summary>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
            public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
            {
                return GetQueryable(string.Empty, dbResourceContent);
            }
            /// <summary>
            /// 事务中获取异步查询linq
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="transaction"></param>
            /// <returns></returns>
            public Task<ISearchQueryable<T>> GetQueryableAsync(string systemID, ITransaction transaction)
            {
                IResourceInstance<DataConnectionInstance> resourceInstance = (IResourceInstance<DataConnectionInstance>)((Linq2DBTransaction)transaction).ResourceInstance;
                return Task.FromResult<ISearchQueryable<T>>(new Linq2DBQueryable<T>(resourceInstance.Instance.GetTable<T>().TableName(systemID), true, resourceInstance));
            }
            /// <summary>
            /// 事务中异步获取查询linq
            /// </summary>
            /// <param name="transaction"></param>
            /// <returns></returns>
            public Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
            {
                return GetQueryableAsync(string.Empty, transaction);
            }
            /// <summary>
            /// 异步获取查询linq 同一个连接
            /// </summary>
            /// <param name="systemID"></param>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
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
            /// <summary>
            /// 同上
            /// </summary>
            /// <param name="dbResourceContent"></param>
            /// <returns></returns>
            public Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
            {
                return GetQueryableAsync(string.Empty, dbResourceContent);
            }


        }
    }
}