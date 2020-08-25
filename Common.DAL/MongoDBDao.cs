using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Common.DAL.Transaction;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Common.DAL
{
    //TODO: 日志
    internal static class MongoDBDao
    {
        private static MongoClient m_mongoClient;
        private static IMongoDatabase m_mongoDatabase;

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
                ClientSessionHandle = m_mongoDatabase.Client.StartSession();
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

        private class MongoDBDaoInstance<T> : ISearchQuery<T>, IEditQuery<T>
            where T : class, IEntity, new()
        {
            private static readonly Expression<Func<T, bool>> EMPTY_PREDICATE;
            private static IMongoCollection<T> m_mongoCollection;

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

                if (!inTransaction)
                    return (int)m_mongoCollection.CountDocuments(predicate ?? EMPTY_PREDICATE);
                else
                    return (int)m_mongoCollection.CountDocuments(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE);
            }

            public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                    return (int)await m_mongoCollection.CountDocumentsAsync(predicate ?? EMPTY_PREDICATE);
                else
                    return (int)await m_mongoCollection.CountDocumentsAsync(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE);
            }

            public void Delete(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                    m_mongoCollection.DeleteMany(Builders<T>.Filter.In(nameof(IEntity.ID), ids));
                else
                    m_mongoCollection.DeleteMany(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.In(nameof(IEntity.ID), ids));
            }

            public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                    await m_mongoCollection.DeleteManyAsync(Builders<T>.Filter.In(nameof(IEntity.ID), ids));
                else
                    await m_mongoCollection.DeleteManyAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.In(nameof(IEntity.ID), ids));
            }

            public T Get(long id, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                    return m_mongoCollection.Find(Builders<T>.Filter.Eq(nameof(IEntity.ID), id)).FirstOrDefault();
                else
                    return m_mongoCollection.Find(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), id)).FirstOrDefault();
            }

            public async Task<T> GetAsync(long id, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                    return await (await m_mongoCollection.FindAsync(Builders<T>.Filter.Eq(nameof(IEntity.ID), id))).FirstOrDefaultAsync();
                else
                    return await (await m_mongoCollection.FindAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), id))).FirstOrDefaultAsync();
            }

            public void Insert(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                    m_mongoCollection.InsertMany(datas);
                else
                    m_mongoCollection.InsertMany(((MongoDBTransaction)transaction).ClientSessionHandle, datas);
            }

            public async Task InsertAsync(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                    await m_mongoCollection.InsertManyAsync(datas);
                else
                    await m_mongoCollection.InsertManyAsync(((MongoDBTransaction)transaction).ClientSessionHandle, datas);
            }

            public void Merge(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = Apply<T>(transaction);

                for (int i = 0; i < datas.Length; i++)
                {
                    if (!inTransaction)
                        m_mongoCollection.FindOneAndReplace(Builders<T>.Filter.Eq(nameof(IEntity.ID), datas[i].ID), datas[i], new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                    else
                        m_mongoCollection.FindOneAndReplace(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), datas[i].ID), datas[i], new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                }
            }

            public async Task MergeAsync(ITransaction transaction = null, params T[] datas)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                Task[] tasks = new Task[datas.Length];

                for (int i = 0; i < datas.Length; i++)
                {
                    if (!inTransaction)
                        tasks[i] = m_mongoCollection.FindOneAndReplaceAsync(Builders<T>.Filter.Eq(nameof(IEntity.ID), datas[i].ID), datas[i], new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                    else
                        tasks[i] = m_mongoCollection.FindOneAndReplaceAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), datas[i].ID), datas[i], new FindOneAndReplaceOptions<T, T> { IsUpsert = true });
                }

                await Task.WhenAll(tasks);
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);
                IFindFluent<T, T> findFluent;

                if (!inTransaction)
                    findFluent = m_mongoCollection.Find(predicate ?? EMPTY_PREDICATE);
                else
                    findFluent = m_mongoCollection.Find(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE);

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
                IFindFluent<T, T> findFluent;

                if (!inTransaction)
                    findFluent = m_mongoCollection.Find(predicate ?? EMPTY_PREDICATE);
                else
                    findFluent = m_mongoCollection.Find(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE);

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

                if (!inTransaction)
                    m_mongoCollection.ReplaceOne(Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
                else
                    m_mongoCollection.ReplaceOne(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
            }

            public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                {
                    foreach (var item in upateDictionary)
                    {
                        m_mongoCollection.UpdateMany(predicate, Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
                else
                {
                    foreach (var item in upateDictionary)
                    {
                        m_mongoCollection.UpdateMany(((MongoDBTransaction)transaction).ClientSessionHandle, predicate, Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
            }

            public async Task UpdateAsync(T data, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                    await m_mongoCollection.ReplaceOneAsync(Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
                else
                    await m_mongoCollection.ReplaceOneAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
            }

            public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    foreach (var item in upateDictionary)
                    {
                        await m_mongoCollection.UpdateManyAsync(predicate, Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
                else
                {
                    foreach (var item in upateDictionary)
                    {
                        await m_mongoCollection.UpdateManyAsync(((MongoDBTransaction)transaction).ClientSessionHandle, predicate, Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
            }

            public int Count<TResult>(IQueryable<TResult> query, ITransaction _ = null)
            {
                return ((IMongoQueryable<T>)query).Count();
            }

            public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, ITransaction _ = null)
            {
                return await ((IMongoQueryable<T>)query).CountAsync();
            }

            public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction _ = null)
            {
                return ((IMongoQueryable<TResult>)query).Skip(startIndex).Take(count).ToList();
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction _ = null)
            {
                return await ((IMongoQueryable<TResult>)query).Skip(startIndex).Take(count).ToListAsync();
            }

            public IQueryable<TResult> GetQueryable<TResult>(ITransaction transaction = null)
                where TResult : class, IEntity, new()
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                    return MongoDBDaoInstance<TResult>.m_mongoCollection.AsQueryable();
                else
                    return MongoDBDaoInstance<TResult>.m_mongoCollection.AsQueryable(((MongoDBTransaction)transaction).ClientSessionHandle);
            }

            public async Task<IQueryable<TResult>> GetQueryableAsync<TResult>(ITransaction transaction = null)
                where TResult : class, IEntity, new()
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                    return MongoDBDaoInstance<TResult>.m_mongoCollection.AsQueryable();
                else
                    return MongoDBDaoInstance<TResult>.m_mongoCollection.AsQueryable(((MongoDBTransaction)transaction).ClientSessionHandle);
            }

            static MongoDBDaoInstance()
            {
                EMPTY_PREDICATE = _ => true;
                m_mongoCollection = m_mongoDatabase.GetCollection<T>(typeof(T).Name);
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
            m_mongoClient = new MongoClient($"mongodb://{ConfigManager.Configuration["MongoDBService:EndPoint"]}");
            m_mongoDatabase = m_mongoClient.GetDatabase(ConfigManager.Configuration["MongoDBService:Database"]);

            BsonSerializer.RegisterSerializer(typeof(DateTime), new DateTimeSerializer(DateTimeKind.Local, BsonType.DateTime));
        }
    }
}
