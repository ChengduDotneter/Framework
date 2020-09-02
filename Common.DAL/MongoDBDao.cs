using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.DAL.Transaction;
using Common.Log;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json.Linq;

namespace Common.DAL
{
    internal static class MongoDBDao
    {
        private static MongoClient m_masterMongoClient;
        private static MongoClient m_slaveMongoClient;
        private static IMongoDatabase m_masterMongoDatabase;
        private static IMongoDatabase m_slaveMongoDatabase;

        private static bool Apply<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            MongoDBTransaction mongoDBTransaction = transaction as MongoDBTransaction;

            if (mongoDBTransaction != null)
            {
                Type table = typeof(TResource);

                if (!mongoDBTransaction.TransactionTables.Contains(table))
                {
                    if (TransactionResourceHelper.ApplayResource(table, mongoDBTransaction.Identity, mongoDBTransaction.Weight))
                        mongoDBTransaction.TransactionTables.Add(table);
                    else
                        throw new DealException($"申请事务资源{table.FullName}失败。");
                }
            }

            return mongoDBTransaction != null;
        }

        private static async Task<bool> ApplyAsync<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            MongoDBTransaction mongoDBTransaction = transaction as MongoDBTransaction;

            if (mongoDBTransaction != null)
            {
                Type table = typeof(TResource);

                if (!mongoDBTransaction.TransactionTables.Contains(table))
                {
                    if (await TransactionResourceHelper.ApplayResourceAsync(table, mongoDBTransaction.Identity, mongoDBTransaction.Weight))
                        mongoDBTransaction.TransactionTables.Add(table);
                    else
                        throw new DealException($"申请事务资源{table.FullName}失败。");
                }
            }

            return mongoDBTransaction != null;
        }

        private static async void Release(string identity)
        {
            await TransactionResourceHelper.ReleaseResourceAsync(identity);
        }

        private class MongoDBTransaction : ITransaction
        {
            public HashSet<Type> TransactionTables { get; }
            public string Identity { get; }
            public int Weight { get; }

            public MongoDBTransaction(int weight)
            {
                Identity = Guid.NewGuid().ToString("D");
                Weight = weight;
                TransactionTables = new HashSet<Type>();
                ClientSessionHandle = m_masterMongoDatabase.Client.StartSession();
                ClientSessionHandle.StartTransaction();
            }

            public IClientSessionHandle ClientSessionHandle { get; set; }

            public object Context { get { return ClientSessionHandle; } }

            public void Dispose()
            {
                ClientSessionHandle.Dispose();
                Release(Identity);
            }

            public void Rollback()
            {
                ClientSessionHandle.AbortTransaction();
            }

            public async Task RollbackAsync()
            {
                await ClientSessionHandle.AbortTransactionAsync();
            }

            public void Submit()
            {
                ClientSessionHandle.CommitTransaction();
            }

            public async Task SubmitAsync()
            {
                await ClientSessionHandle.CommitTransactionAsync();
            }
        }

        private class MongoDBQueryable<T> : ISearchQueryable<T>
            where T : class, IEntity, new()
        {
            private IQueryable<T> m_query;

            public MongoDBQueryable(IQueryable<T> query)
            {
                m_query = query;
            }

            public Type ElementType => m_query.ElementType;

            public Expression Expression => m_query.Expression;

            public IQueryProvider Provider => m_query.Provider;

            public void Dispose() { }

            public IEnumerator<T> GetEnumerator()
            {
                return m_query.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)m_query).GetEnumerator();
            }
        }

        private class MongoDBDaoInstance<T> : ISearchQuery<T>, IEditQuery<T>
            where T : class, IEntity, new()
        {
            private static readonly Expression<Func<T, bool>> EMPTY_PREDICATE;
            private static readonly IDictionary<IMongoDatabase, IMongoCollection<T>> m_collection;

            public ITransaction BeginTransaction(int weight = 0)
            {
                return new MongoDBTransaction(weight);
            }

            public async Task<ITransaction> BeginTransactionAsync(int weight = 0)
            {
                return await Task.FromResult(new MongoDBTransaction(weight));
            }

            public int Count(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);
                DaoFactory.LogHelper.Info("mongoDB", $"count predicate: {predicate}");

                if (!inTransaction)
                    return (int)GetCollection(m_slaveMongoDatabase).CountDocuments(predicate ?? EMPTY_PREDICATE);
                else
                    return (int)GetCollection(m_masterMongoDatabase).CountDocuments(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE);
            }

            public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);
                await DaoFactory.LogHelper.Info("mongoDB", $"count predicate: {predicate}");

                if (!inTransaction)
                    return (int)await GetCollection(m_slaveMongoDatabase).CountDocumentsAsync(predicate ?? EMPTY_PREDICATE);
                else
                    return (int)await GetCollection(m_masterMongoDatabase).CountDocumentsAsync(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE);
            }

            public void Delete(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = Apply<T>(transaction);
                DaoFactory.LogHelper.Info("mongoDB", $"delete ids: {string.Join(",", ids)}");

                if (!inTransaction)
                    GetCollection(m_masterMongoDatabase).DeleteMany(Builders<T>.Filter.In(nameof(IEntity.ID), ids));
                else
                    GetCollection(m_masterMongoDatabase).DeleteMany(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.In(nameof(IEntity.ID), ids));
            }

            public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);
                await DaoFactory.LogHelper.Info("mongoDB", $"delete ids: {string.Join(",", ids)}");

                if (!inTransaction)
                    await GetCollection(m_masterMongoDatabase).DeleteManyAsync(Builders<T>.Filter.In(nameof(IEntity.ID), ids));
                else
                    await GetCollection(m_masterMongoDatabase).DeleteManyAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.In(nameof(IEntity.ID), ids));
            }

            public T Get(long id, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);
                DaoFactory.LogHelper.Info("mongoDB", $"get id: {id}");

                if (!inTransaction)
                    return GetCollection(m_slaveMongoDatabase).Find(Builders<T>.Filter.Eq(nameof(IEntity.ID), id)).FirstOrDefault();
                else
                    return GetCollection(m_masterMongoDatabase).Find(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), id)).FirstOrDefault();
            }

            public async Task<T> GetAsync(long id, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);
                await DaoFactory.LogHelper.Info("mongoDB", $"get id: {id}");

                if (!inTransaction)
                    return await (await GetCollection(m_slaveMongoDatabase).FindAsync(Builders<T>.Filter.Eq(nameof(IEntity.ID), id))).FirstOrDefaultAsync();
                else
                    return await (await GetCollection(m_masterMongoDatabase).FindAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), id))).FirstOrDefaultAsync();
            }

            public void Insert(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction);
                DaoFactory.LogHelper.Info("mongoDB", $"insert datas: {Environment.NewLine}{string.Join(Environment.NewLine, datas.Select(data => JObject.FromObject(data)))}");

                if (!inTransaction)
                    GetCollection(m_masterMongoDatabase).InsertMany(datas);
                else
                    GetCollection(m_masterMongoDatabase).InsertMany(((MongoDBTransaction)transaction).ClientSessionHandle, datas);
            }

            public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);
                await DaoFactory.LogHelper.Info("mongoDB", $"insert datas: {Environment.NewLine}{string.Join(Environment.NewLine, datas.Select(data => JObject.FromObject(data)))}");

                if (!inTransaction)
                    await GetCollection(m_masterMongoDatabase).InsertManyAsync(datas);
                else
                    await GetCollection(m_masterMongoDatabase).InsertManyAsync(((MongoDBTransaction)transaction).ClientSessionHandle, datas);
            }

            public void Merge(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction);
                DaoFactory.LogHelper.Info("mongoDB", $"merge datas: {Environment.NewLine}{string.Join(Environment.NewLine, datas.Select(data => JObject.FromObject(data)))}");

                for (int i = 0; i < datas.Length; i++)
                {
                    if (!inTransaction)
                        GetCollection(m_masterMongoDatabase).FindOneAndReplace(Builders<T>.Filter.Eq(nameof(IEntity.ID), datas[i].ID), datas[i], new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                    else
                        GetCollection(m_masterMongoDatabase).FindOneAndReplace(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), datas[i].ID), datas[i], new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                }
            }

            public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);
                await DaoFactory.LogHelper.Info("mongoDB", $"merge datas: {Environment.NewLine}{string.Join(Environment.NewLine, datas.Select(data => JObject.FromObject(data)))}");

                Task[] tasks = new Task[datas.Length];

                for (int i = 0; i < datas.Length; i++)
                {
                    if (!inTransaction)
                        tasks[i] = GetCollection(m_masterMongoDatabase).FindOneAndReplaceAsync(Builders<T>.Filter.Eq(nameof(IEntity.ID), datas[i].ID), datas[i], new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                    else
                        tasks[i] = GetCollection(m_masterMongoDatabase).FindOneAndReplaceAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), datas[i].ID), datas[i], new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                }

                await Task.WhenAll(tasks);
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                DaoFactory.LogHelper.Info("mongoDB", $"search{Environment.NewLine}predicate: {predicate}{Environment.NewLine}orderBy: {GetOrderByString(queryOrderBies)}{Environment.NewLine}startIndex: {startIndex}{Environment.NewLine}count: {count}");

                IFindFluent<T, T> findFluent;

                if (!inTransaction)
                    findFluent = GetCollection(m_slaveMongoDatabase).Find(predicate ?? EMPTY_PREDICATE);
                else
                    findFluent = GetCollection(m_masterMongoDatabase).Find(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE);

                if (queryOrderBies != null)
                {
                    IList<SortDefinition<T>> sortDefinitions = new List<SortDefinition<T>>();

                    foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                    {
                        if (queryOrderBy.OrderByType == OrderByType.Asc)
                            sortDefinitions.Add(Builders<T>.Sort.Ascending(queryOrderBy.Expression));
                        else
                            sortDefinitions.Add(Builders<T>.Sort.Descending(queryOrderBy.Expression));
                    }

                    return findFluent.Sort(Builders<T>.Sort.Combine(sortDefinitions)).Skip(startIndex).Limit(count).ToList();
                }

                return findFluent.Skip(startIndex).Limit(count).ToList();
            }

            public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null,
                                                          IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                                          int startIndex = 0,
                                                          int count = int.MaxValue,
                                                          ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                await DaoFactory.LogHelper.Info("mongoDB", $"search{Environment.NewLine}predicate: {predicate}{Environment.NewLine}orderBy: {GetOrderByString(queryOrderBies)}{Environment.NewLine}startIndex: {startIndex}{Environment.NewLine}count: {count}");

                IFindFluent<T, T> findFluent;

                if (!inTransaction)
                    findFluent = GetCollection(m_slaveMongoDatabase).Find(predicate ?? EMPTY_PREDICATE);
                else
                    findFluent = GetCollection(m_masterMongoDatabase).Find(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE);

                if (queryOrderBies != null)
                {
                    IList<SortDefinition<T>> sortDefinitions = new List<SortDefinition<T>>();

                    foreach (QueryOrderBy<T> queryOrderBy in queryOrderBies)
                    {
                        if (queryOrderBy.OrderByType == OrderByType.Asc)
                            sortDefinitions.Add(Builders<T>.Sort.Ascending(queryOrderBy.Expression));
                        else
                            sortDefinitions.Add(Builders<T>.Sort.Descending(queryOrderBy.Expression));
                    }

                    return await findFluent.Sort(Builders<T>.Sort.Combine(sortDefinitions)).Skip(startIndex).Limit(count).ToListAsync();
                }

                return await findFluent.Skip(startIndex).Limit(count).ToListAsync();
            }

            public void Update(T data, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);
                DaoFactory.LogHelper.Info("mongoDB", $"update data: {Environment.NewLine}{JObject.FromObject(data)}");

                if (!inTransaction)
                    GetCollection(m_masterMongoDatabase).ReplaceOne(Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
                else
                    GetCollection(m_masterMongoDatabase).ReplaceOne(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
            }

            public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                DaoFactory.LogHelper.Info("mongoDB", $"update predicate: {predicate}{Environment.NewLine}values: {Environment.NewLine}{string.Join(Environment.NewLine, upateDictionary.Select(item => $"{item.Key}: {item.Value}"))}");

                if (!inTransaction)
                {
                    foreach (var item in upateDictionary)
                    {
                        GetCollection(m_masterMongoDatabase).UpdateMany(predicate, Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
                else
                {
                    foreach (var item in upateDictionary)
                    {
                        GetCollection(m_masterMongoDatabase).UpdateMany(((MongoDBTransaction)transaction).ClientSessionHandle, predicate, Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
            }

            public async Task UpdateAsync(T data, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);
                await DaoFactory.LogHelper.Info("mongoDB", $"update data: {Environment.NewLine}{JObject.FromObject(data)}");

                if (!inTransaction)
                    await GetCollection(m_masterMongoDatabase).ReplaceOneAsync(Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
                else
                    await GetCollection(m_masterMongoDatabase).ReplaceOneAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
            }

            public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                await DaoFactory.LogHelper.Info("mongoDB", $"update predicate: {predicate}{Environment.NewLine}values: {Environment.NewLine}{string.Join(Environment.NewLine, upateDictionary.Select(item => $"{item.Key}: {item.Value}"))}");

                if (!inTransaction)
                {
                    foreach (var item in upateDictionary)
                    {
                        await GetCollection(m_masterMongoDatabase).UpdateManyAsync(predicate, Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
                else
                {
                    foreach (var item in upateDictionary)
                    {
                        await GetCollection(m_masterMongoDatabase).UpdateManyAsync(((MongoDBTransaction)transaction).ClientSessionHandle, predicate, Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
            }

            public int Count<TResult>(IQueryable<TResult> query, ITransaction _ = null)
            {
                DaoFactory.LogHelper.Info("mongoDB", $"count: {query}");
                return ((IMongoQueryable<T>)query).Count();
            }

            public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, ITransaction _ = null)
            {
                await DaoFactory.LogHelper.Info("mongoDB", $"count: {query}");
                return await ((IMongoQueryable<T>)query).CountAsync();
            }

            public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction _ = null)
            {
                DaoFactory.LogHelper.Info("mongoDB", $"search: {query}");
                return ((IMongoQueryable<TResult>)query).Skip(startIndex).Take(count).ToList();
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction _ = null)
            {
                await DaoFactory.LogHelper.Info("mongoDB", $"search: {query}");
                return await ((IMongoQueryable<TResult>)query).Skip(startIndex).Take(count).ToListAsync();
            }

            public ISearchQueryable<T> GetQueryable(ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                    return new MongoDBQueryable<T>(GetCollection(m_slaveMongoDatabase).AsQueryable());
                else
                    return new MongoDBQueryable<T>(GetCollection(m_masterMongoDatabase).AsQueryable(((MongoDBTransaction)transaction).ClientSessionHandle));
            }

            public async Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                    return new MongoDBQueryable<T>(GetCollection(m_slaveMongoDatabase).AsQueryable());
                else
                    return new MongoDBQueryable<T>(GetCollection(m_masterMongoDatabase).AsQueryable(((MongoDBTransaction)transaction).ClientSessionHandle));
            }

            private static string GetOrderByString(IEnumerable<QueryOrderBy<T>> queryOrderBies)
            {
                if (queryOrderBies == null)
                    return string.Empty;

                return string.Join(Environment.NewLine, queryOrderBies.Select(queryOrderBy => $"predicate: {queryOrderBy.Expression} {queryOrderBy.OrderByType}"));
            }

            static MongoDBDaoInstance()
            {
                EMPTY_PREDICATE = _ => true;
                m_collection = new Dictionary<IMongoDatabase, IMongoCollection<T>>();
            }

            private static IMongoCollection<T> GetCollection(IMongoDatabase mongoDatabase)
            {
                if (!m_collection.ContainsKey(mongoDatabase))
                {
                    lock (m_collection)
                    {
                        if (!m_collection.ContainsKey(mongoDatabase))
                            m_collection.Add(mongoDatabase, mongoDatabase.GetCollection<T>(typeof(T).Name));
                    }
                }

                return m_collection[mongoDatabase];
            }
        }

        public static ISearchQuery<T> GetMongoDBSearchQuery<T>()
            where T : class, IEntity, new()
        {
            return new MongoDBDaoInstance<T>();
        }

        public static IEditQuery<T> GetMongoDBEditQuery<T>()
           where T : class, IEntity, new()
        {
            return new MongoDBDaoInstance<T>();
        }

        static MongoDBDao()
        {
            m_masterMongoClient = new MongoClient(new MongoClientSettings()
            {
                ConnectionMode = ConnectionMode.ReplicaSet,
                ReadPreference = new ReadPreference(ReadPreferenceMode.Primary),
                ReplicaSetName = ConfigManager.Configuration["MongoDBService:ReplicaSet"],
                Server = new MongoServerAddress(ConfigManager.Configuration["MongoDBService:Address"], Convert.ToInt32(ConfigManager.Configuration["MongoDBService:Port"]))
            });

            m_slaveMongoClient = new MongoClient(new MongoClientSettings()
            {
                ConnectionMode = ConnectionMode.ReplicaSet,
                ReadPreference = new ReadPreference(ReadPreferenceMode.Secondary),
                ReplicaSetName = ConfigManager.Configuration["MongoDBService:ReplicaSet"],
                Server = new MongoServerAddress(ConfigManager.Configuration["MongoDBService:Address"], Convert.ToInt32(ConfigManager.Configuration["MongoDBService:Port"]))
            });

            m_masterMongoDatabase = m_masterMongoClient.GetDatabase(ConfigManager.Configuration["MongoDBService:Database"]);
            m_slaveMongoDatabase = m_slaveMongoClient.GetDatabase(ConfigManager.Configuration["MongoDBService:Database"]);

            BsonSerializer.RegisterSerializer(typeof(DateTime), new DateTimeSerializer(DateTimeKind.Local, BsonType.DateTime));
        }
    }
}
