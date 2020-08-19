﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.DAL.Transaction;
using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Linq;
using Microsoft.Extensions.Configuration;

namespace Common.DAL
{
    internal static class Linq2DBDao
    {
        private const int GET_DATACONNECTION_THREAD_TIME_SPAN = 1;
        private const int DEFAULT_CONNECTION_COUNT = 10;
        private static IDictionary<string, ConcurrentQueue<DataConnection>> m_connectionPool;
        private static ISet<string> m_tableNames;
        private static LinqToDbConnectionOptions m_masterlinqToDbConnectionOptions;
        private static LinqToDbConnectionOptions m_slavelinqToDbConnectionOptions;

        static Linq2DBDao()
        {
            m_tableNames = new HashSet<string>();
            LinqToDbConnectionOptionsBuilder masterLinqToDbConnectionOptionsBuilder = new LinqToDbConnectionOptionsBuilder();
            LinqToDbConnectionOptionsBuilder slaveLinqToDbConnectionOptionsBuilder = new LinqToDbConnectionOptionsBuilder();

            masterLinqToDbConnectionOptionsBuilder.UseMySql(ConfigManager.Configuration.GetConnectionString("MasterConnection"));
            slaveLinqToDbConnectionOptionsBuilder.UseMySql(ConfigManager.Configuration.GetConnectionString("SalveConnection"));

            masterLinqToDbConnectionOptionsBuilder.WithTraceLevel(TraceLevel.Verbose);
            slaveLinqToDbConnectionOptionsBuilder.WithTraceLevel(TraceLevel.Verbose);

            //TODO: 日志
            masterLinqToDbConnectionOptionsBuilder.WithTracing(traceInfo =>
            {
                Console.WriteLine("master " + traceInfo.SqlText);
            });

            //TODO: 日志
            slaveLinqToDbConnectionOptionsBuilder.WithTracing(traceInfo =>
            {
                Console.WriteLine(traceInfo.DataConnection.GetHashCode());
                Console.WriteLine("slave " + traceInfo.SqlText);
            });

            m_masterlinqToDbConnectionOptions = masterLinqToDbConnectionOptionsBuilder.Build();
            m_slavelinqToDbConnectionOptions = slaveLinqToDbConnectionOptionsBuilder.Build();

            m_connectionPool = new Dictionary<string, ConcurrentQueue<DataConnection>>();

            if (!int.TryParse(ConfigManager.Configuration["ConnectionCount"], out int connectionCount))
                connectionCount = DEFAULT_CONNECTION_COUNT;

            if (!m_connectionPool.ContainsKey(m_masterlinqToDbConnectionOptions.ConnectionString))
            {
                m_connectionPool.Add(m_masterlinqToDbConnectionOptions.ConnectionString, new ConcurrentQueue<DataConnection>());

                for (int i = 0; i < connectionCount; i++)
                    m_connectionPool[m_masterlinqToDbConnectionOptions.ConnectionString].Enqueue(new DataConnection(m_masterlinqToDbConnectionOptions));
            }

            if (!m_connectionPool.ContainsKey(m_masterlinqToDbConnectionOptions.ConnectionString))
            {
                m_connectionPool.Add(m_slavelinqToDbConnectionOptions.ConnectionString, new ConcurrentQueue<DataConnection>());

                for (int i = 0; i < connectionCount; i++)
                    m_connectionPool[m_slavelinqToDbConnectionOptions.ConnectionString].Enqueue(new DataConnection(m_slavelinqToDbConnectionOptions));
            }
        }

        private static DataConnection CreateConnection(LinqToDbConnectionOptions linqToDbConnectionOptions)
        {
            if (m_connectionPool[linqToDbConnectionOptions.ConnectionString].IsEmpty)
                throw new Exception("连接池已满。");

            DataConnection dataConnection;

            while (!m_connectionPool[linqToDbConnectionOptions.ConnectionString].TryDequeue(out dataConnection))
                Thread.Sleep(GET_DATACONNECTION_THREAD_TIME_SPAN);

            return dataConnection;
        }

        private static void DisposeConnection(DataConnection dataConnection)
        {
            if (!m_connectionPool[dataConnection.ConnectionString].Contains(dataConnection))
                m_connectionPool[dataConnection.ConnectionString].Enqueue(dataConnection);
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
                        DataConnection dataConnection = CreateConnection(linqToDbConnectionOptions);

                        try
                        {
                            dataConnection.CreateTable<T>(tableName);
                        }
                        catch
                        {
                            return;
                        }
                        finally
                        {
                            dataConnection.Dispose();
                            m_tableNames.Add(tableName);
                        }
                    }
                }
            }
        }

        private static bool Apply<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null)
            {
                Type table = typeof(TResource);

                if (!linq2DBTransaction.TransactionTables.Contains(table))
                {
                    if (TransactionResourceHelper.ApplayResource(table, linq2DBTransaction.Identity, linq2DBTransaction.Weight))
                        linq2DBTransaction.TransactionTables.Add(table);
                    else
                        throw new DealException($"申请事务资源{table.FullName}失败。");
                }
            }

            return linq2DBTransaction != null;
        }

        private static async Task<bool> ApplyAsync<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            Linq2DBTransaction linq2DBTransaction = transaction as Linq2DBTransaction;

            if (linq2DBTransaction != null)
            {
                Type table = typeof(TResource);

                if (!linq2DBTransaction.TransactionTables.Contains(table))
                {
                    if (await TransactionResourceHelper.ApplayResourceAsync(table, linq2DBTransaction.Identity, linq2DBTransaction.Weight))
                        linq2DBTransaction.TransactionTables.Add(table);
                    else
                        throw new DealException($"申请事务资源{table.FullName}失败。");
                }
            }

            return linq2DBTransaction != null;
        }

        private static async void Release(string identity)
        {
            await TransactionResourceHelper.ReleaseResourceAsync(identity);
        }

        private class Linq2DBTransaction : ITransaction
        {
            private DataConnectionTransaction m_dataConnectionTransaction;

            public HashSet<Type> TransactionTables { get; }
            public string Identity { get; }
            public int Weight { get; }

            public Linq2DBTransaction(DataConnectionTransaction dataConnectionTransaction, int weight)
            {
                Identity = Guid.NewGuid().ToString("D");
                Weight = weight;
                TransactionTables = new HashSet<Type>();
                m_dataConnectionTransaction = dataConnectionTransaction;
            }

            public object Context { get { return m_dataConnectionTransaction; } }

            public async void Dispose()
            {
                DisposeConnection(m_dataConnectionTransaction.DataConnection);
                await m_dataConnectionTransaction.DisposeAsync();
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

        private class Linq2DBDaoInstance<T> : ISearchQuery<T>, IEditQuery<T>
            where T : class, IEntity, new()
        {
            private LinqToDbConnectionOptions m_linqToDbConnectionOptions;
            private static readonly Expression<Func<T, bool>> EMPTY_PREDICATE;
            private static readonly object m_lockThis;
            private bool m_codeFirst;
            private DataConnection m_queryableDataConnection;

            public ITransaction BeginTransaction(int weight = 0)
            {
                return new Linq2DBTransaction(CreateConnection(m_linqToDbConnectionOptions).BeginTransaction(), weight);
            }

            public async Task<ITransaction> BeginTransactionAsync(int weight = 0)
            {
                return new Linq2DBTransaction(await CreateConnection(m_linqToDbConnectionOptions).BeginTransactionAsync(), weight);
            }

            public int Count(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                {
                    DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return dataConnection.GetTable<T>().Count(predicate ?? EMPTY_PREDICATE);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    return ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().Count(predicate ?? EMPTY_PREDICATE);
                }
            }

            public int Count<TResult>(IQueryable<TResult> query, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                try
                {
                    return query.Count();
                }
                finally
                {
                    if (!inTransaction && query is ITable<T> table && table.DataContext is DataConnection dataConnection)
                        DisposeConnection(dataConnection);
                    else if (!inTransaction && query is IExpressionQuery expressionQuery && expressionQuery.DataContext is DataConnection expressionQueryDataConnection)
                        DisposeConnection(expressionQueryDataConnection);
                }
            }

            public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return await dataConnection.GetTable<T>().CountAsync(predicate ?? EMPTY_PREDICATE);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    return await ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().CountAsync(predicate ?? EMPTY_PREDICATE);
                }
            }

            public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                try
                {
                    return await query.CountAsync();
                }
                finally
                {
                    if (!inTransaction && query is ITable<T> table && table.DataContext is DataConnection dataConnection)
                        DisposeConnection(dataConnection);
                    else if (!inTransaction && query is IExpressionQuery expressionQuery && expressionQuery.DataContext is DataConnection expressionQueryDataConnection)
                        DisposeConnection(expressionQueryDataConnection);
                }
            }

            public void Delete(ITransaction transaction = null, params long[] ids)
            {
                Apply<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    dataConnection.GetTable<T>().Delete(item => ids.Contains(item.ID));
                }
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
            {
                await ApplyAsync<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    await dataConnection.GetTable<T>().DeleteAsync(item => ids.Contains(item.ID));
                }
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public T Get(long id, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                {
                    DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return dataConnection.GetTable<T>().SingleOrDefault(item => item.ID == id);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    Linq2DBDaoInstance<T> linq2DBDaoInstance = (Linq2DBDaoInstance<T>)GetLinq2DBEditQuery<T>(m_codeFirst);
                    return ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().SingleOrDefault(item => item.ID == id);
                }
            }

            public async Task<T> GetAsync(long id, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        return await dataConnection.GetTable<T>().SingleOrDefaultAsync(item => item.ID == id);
                    }
                    finally
                    {
                        DisposeConnection(dataConnection);
                    }
                }
                else
                {
                    return await ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().SingleOrDefaultAsync(item => item.ID == id);
                }
            }

            public IQueryable<TResult> GetQueryable<TResult>(ITransaction transaction = null)
                where TResult : class, IEntity, new()
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                {
                    if (m_queryableDataConnection == null)
                        lock (m_lockThis)
                            if (m_queryableDataConnection == null)
                                m_queryableDataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    return m_queryableDataConnection.GetTable<TResult>();
                }
                else
                {
                    return ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<TResult>();
                }
            }

            public async Task<IQueryable<TResult>> GetQueryableAsync<TResult>(ITransaction transaction = null)
                where TResult : class, IEntity, new()
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    if (m_queryableDataConnection == null)
                        lock (m_lockThis)
                            if (m_queryableDataConnection == null)
                                m_queryableDataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    return m_queryableDataConnection.GetTable<TResult>();
                }
                else
                {
                    return ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<TResult>();
                }
            }

            public void Insert(ITransaction transaction = null, params T[] datas)
            {
                Apply<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    dataConnection.GetTable<T>().BulkCopy(datas);
                }
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
            {
                await ApplyAsync<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    dataConnection.GetTable<T>().BulkCopy(datas);
                }
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public void Merge(ITransaction transaction = null, params T[] datas)
            {
                Apply<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    for (int i = 0; i < datas.Length; i++)
                        dataConnection.InsertOrReplace(datas[i]);
                }
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
            {
                await ApplyAsync<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    for (int i = 0; i < datas.Length; i++)
                        await dataConnection.InsertOrReplaceAsync(datas[i]);
                }
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                {
                    DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        IQueryable<T> query = dataConnection.GetTable<T>().Where(predicate ?? EMPTY_PREDICATE);
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
                    IQueryable<T> query = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().Where(predicate ?? EMPTY_PREDICATE);
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

            public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                try
                {
                    return query.Skip(startIndex).Take(count).ToList();
                }
                finally
                {
                    if (!inTransaction && query is ITable<T> table && table.DataContext is DataConnection dataConnection)
                        DisposeConnection(dataConnection);
                    else if (!inTransaction && query is IExpressionQuery expressionQuery && expressionQuery.DataContext is DataConnection expressionQueryDataConnection)
                        DisposeConnection(expressionQueryDataConnection);
                }
            }

            public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                    try
                    {
                        IQueryable<T> query = dataConnection.GetTable<T>().Where(predicate ?? EMPTY_PREDICATE);
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
                    IQueryable<T> query = ((DataConnectionTransaction)((Linq2DBTransaction)transaction).Context).DataConnection.GetTable<T>().Where(predicate ?? EMPTY_PREDICATE);
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

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                try
                {
                    return await query.Skip(startIndex).Take(count).ToListAsync();
                }
                finally
                {
                    if (!inTransaction && query is ITable<T> table && table.DataContext is DataConnection dataConnection)
                        DisposeConnection(dataConnection);
                    else if (!inTransaction && query is IExpressionQuery expressionQuery && expressionQuery.DataContext is DataConnection expressionQueryDataConnection)
                        DisposeConnection(expressionQueryDataConnection);
                }
            }

            public void Update(T data, ITransaction transaction = null)
            {
                Apply<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    dataConnection.Update<T>(data);
                }
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                IList<Tuple<Type, LambdaExpression, object>> updates = new List<Tuple<Type, LambdaExpression, object>>();

                foreach (var item in upateDictionary)
                    updates.Add(Tuple.Create(typeof(T).GetProperty(item.Key).PropertyType, Expression.Lambda(Expression.Property(parameter, item.Key), parameter), item.Value));

                Apply<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    IUpdatable<T> updatable = dataConnection.GetTable<T>().Where(predicate).AsUpdatable();

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
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public async Task UpdateAsync(T data, ITransaction transaction = null)
            {
                await ApplyAsync<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    await dataConnection.UpdateAsync<T>(data);
                }
                finally
                {
                    DisposeConnection(dataConnection);
                }
            }

            public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(T));
                IList<Tuple<Type, LambdaExpression, object>> updates = new List<Tuple<Type, LambdaExpression, object>>();

                foreach (var item in upateDictionary)
                    updates.Add(Tuple.Create(typeof(T).GetProperty(item.Key).PropertyType, Expression.Lambda(Expression.Property(parameter, item.Key), parameter), item.Value));

                await ApplyAsync<T>(transaction);
                DataConnection dataConnection = CreateConnection(m_linqToDbConnectionOptions);

                try
                {
                    IUpdatable<T> updatable = dataConnection.GetTable<T>().Where(predicate).AsUpdatable();

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
                finally
                {
                    DisposeConnection(dataConnection);
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
                m_lockThis = new object();
            }

            ~Linq2DBDaoInstance()
            {
                if (m_queryableDataConnection != null)
                    DisposeConnection(m_queryableDataConnection);
            }
        }
    }
}
