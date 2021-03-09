using Common.DAL.Transaction;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

            if (mongoDBTransaction != null && mongoDBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!TransactionResourceHelper.ApplayTableResource(table, mongoDBTransaction.Identity, mongoDBTransaction.Weight))
                    throw new ResourceException($"申请事务资源{table.FullName}失败。");
            }

            return mongoDBTransaction != null;
        }

        private static async Task<bool> ApplyAsync<TResource>(ITransaction transaction) where TResource : class, IEntity
        {
            MongoDBTransaction mongoDBTransaction = transaction as MongoDBTransaction;

            if (mongoDBTransaction != null && mongoDBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!await TransactionResourceHelper.ApplayTableResourceAsync(table, mongoDBTransaction.Identity, mongoDBTransaction.Weight))
                    throw new ResourceException($"申请事务资源{table.FullName}失败。");
            }

            return mongoDBTransaction != null;
        }

        private static bool WriteApply<TResource>(ITransaction transaction, IEnumerable<long> ids) where TResource : class, IEntity
        {
            MongoDBTransaction mongoDBTransaction = transaction as MongoDBTransaction;

            if (mongoDBTransaction != null && mongoDBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!TransactionResourceHelper.ApplayRowResourceWithWrite(table, mongoDBTransaction.Identity, mongoDBTransaction.Weight, ids))
                    throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
            }

            return mongoDBTransaction != null;
        }

        private static async Task<bool> WriteApplyAsync<TResource>(ITransaction transaction, IEnumerable<long> ids) where TResource : class, IEntity
        {
            MongoDBTransaction mongoDBTransaction = transaction as MongoDBTransaction;

            if (mongoDBTransaction != null && mongoDBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!await TransactionResourceHelper.ApplayRowResourceWithWriteAsync(table, mongoDBTransaction.Identity, mongoDBTransaction.Weight, ids))
                    throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
            }

            return mongoDBTransaction != null;
        }

        private static bool ReadApply<TResource>(ITransaction transaction, IEnumerable<long> ids) where TResource : class, IEntity
        {
            MongoDBTransaction mongoDBTransaction = transaction as MongoDBTransaction;

            if (mongoDBTransaction != null && mongoDBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!TransactionResourceHelper.ApplayRowResourceWithRead(table, mongoDBTransaction.Identity, mongoDBTransaction.Weight, ids))
                    throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
            }

            return mongoDBTransaction != null;
        }

        private static async Task<bool> ReadApplyAsync<TResource>(ITransaction transaction, IEnumerable<long> ids) where TResource : class, IEntity
        {
            MongoDBTransaction mongoDBTransaction = transaction as MongoDBTransaction;

            if (mongoDBTransaction != null && mongoDBTransaction.DistributedLock)
            {
                Type table = typeof(TResource);

                if (!await TransactionResourceHelper.ApplayRowResourceWithReadAsync(table, mongoDBTransaction.Identity, mongoDBTransaction.Weight, ids))
                    throw new ResourceException($"申请事务资源{table.FullName}:{string.Join(",", ids)}失败。");
            }

            return mongoDBTransaction != null;
        }

        private static async void Release(string identity)
        {
            await TransactionResourceHelper.ReleaseResourceAsync(identity);
        }

        private class MongoDBTransaction : ITransaction
        {
            public string Identity { get; }
            public int Weight { get; }
            public bool DistributedLock { get; }

            public MongoDBTransaction(int weight, bool distributedLock)
            {
                Identity = Guid.NewGuid().ToString("D");
                Weight = weight;
                DistributedLock = distributedLock;
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

        private class MongoQueryable<T> : ISearchQueryable<T>
        {
            public IQueryable<T> InnerQuery { get; }

            public Type ElementType => InnerQuery.ElementType;

            public Expression Expression => InnerQuery.Expression;

            public IQueryProvider Provider { get; }

            public void Dispose()
            {
            }

            public IEnumerator<T> GetEnumerator()
            {
                IEnumerable<T> results = Provider.Execute<IEnumerable<T>>(Expression);
                return results.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return ((IEnumerable)GetEnumerator()).GetEnumerator();
            }

            public MongoQueryable(IQueryable<T> innerQuery, IQueryProvider queryProvider)
            {
                InnerQuery = innerQuery;
                Provider = queryProvider;
            }
        }

        private class OrderedMongoQueryable<T> : MongoQueryable<T>, IOrderedQueryable<T>
        {
            public OrderedMongoQueryable(IQueryable<T> innerQuery, IQueryProvider queryProvider) : base(innerQuery, queryProvider)
            {
            }
        }

        private class MongoQueryableProvider : IQueryProvider
        {
            private class JoinItem
            {
                public IMongoQueryable MongoQueryable { get; }
                public Expression Expression { get; }

                public JoinItem(IMongoQueryable mongoQueryable, Expression expression)
                {
                    Expression = expression;
                    MongoQueryable = mongoQueryable;
                }
            }

            private IQueryProvider m_queryProvider;
            private IDictionary<string, JoinItem> m_joinItems;

            public MongoQueryableProvider(IQueryProvider queryProvider)
            {
                m_queryProvider = queryProvider;
                m_joinItems = new Dictionary<string, JoinItem>();
            }

            public IQueryable CreateQuery(Expression expression)
            {
                Type elementType = GetSequenceElementType(expression.Type);

                try
                {
                    return (IQueryable)typeof(MongoQueryableProvider).GetMethod("CreateQuery", 1, new Type[] { typeof(Expression) }).MakeGenericMethod(elementType).Invoke(this, new object[] { expression });
                }
                catch (TargetInvocationException tie)
                {
                    throw tie.InnerException;
                }
            }

            public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            {
                MethodCallExpression methodCallExpression = (MethodCallExpression)expression;

                if (methodCallExpression.Method.Name == "Join" ||
                    methodCallExpression.Method.Name == "GroupJoin")
                {
                    (Expression joinExpression, bool hasInnerExpression) = GetJoinExpression(methodCallExpression.Arguments[1]);

                    if (hasInnerExpression)
                    {
                        JoinItem joinItem = new JoinItem((IMongoQueryable)((ConstantExpression)joinExpression).Value, methodCallExpression.Arguments[1]);
                        string key = ((LambdaExpression)((UnaryExpression)methodCallExpression.Arguments[4]).Operand).Parameters[1].Name;
                        m_joinItems.Add(key, joinItem);
                        IList<Expression> arguments = new List<Expression>(methodCallExpression.Arguments);
                        arguments[1] = joinExpression;
                        expression = methodCallExpression.Update(methodCallExpression.Object, arguments);
                    }
                }

                IQueryable<TElement> queryable = m_queryProvider.CreateQuery<TElement>(expression);
                MongoQueryableProvider mongoQueryProvider = new MongoQueryableProvider(queryable.Provider);
                m_joinItems.ForEach(item => mongoQueryProvider.m_joinItems.Add(item));

                if (methodCallExpression.Method.Name == "OrderBy" ||
                    methodCallExpression.Method.Name == "ThenBy" ||
                    methodCallExpression.Method.Name == "OrderByDescending" ||
                    methodCallExpression.Method.Name == "ThenByDescending")
                {
                    return new OrderedMongoQueryable<TElement>(queryable, mongoQueryProvider);
                }
                else
                {
                    return new MongoQueryable<TElement>(queryable, mongoQueryProvider);
                }
            }

            public object Execute(Expression expression)
            {
                return DoExcute(expression, "BuildPlan", false);
            }

            public TResult Execute<TResult>(Expression expression)
            {
                return (TResult)Execute(expression);
            }

            public Task<TResult> ExecuteAsync<TResult>(Expression expression)
            {
                return (Task<TResult>)DoExcute(expression, "BuildAsyncPlan", true);
            }

            private object DoExcute(Expression expression, string buildPlanFuncName, bool hasCancellationToken)
            {
                Type executionPlanBuilderType = Assembly.GetAssembly(typeof(IMongoCollection<>)).GetType("MongoDB.Driver.Linq.ExecutionPlanBuilder");
                MethodInfo transateMethod = m_queryProvider.GetType().GetMethod("Translate", BindingFlags.Instance | BindingFlags.NonPublic);
                object queryableTranslation = transateMethod.Invoke(m_queryProvider, new object[] { expression });
                MethodInfo buildPlanMethod = executionPlanBuilderType.GetMethod(buildPlanFuncName);
                Expression executionPlan;

                if (hasCancellationToken)
                    executionPlan = (Expression)buildPlanMethod.Invoke(null, new object[] { Expression.Constant(m_queryProvider), queryableTranslation, Expression.Constant(CancellationToken.None) });
                else
                    executionPlan = (Expression)buildPlanMethod.Invoke(null, new object[] { Expression.Constant(m_queryProvider), queryableTranslation });

                ConstantExpression constantExpression;

                if (executionPlan is MethodCallExpression methodCall)
                {
                    if (methodCall.Arguments[0] is UnaryExpression unaryExpression)
                    {
                        MethodCallExpression methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
                        constantExpression = (ConstantExpression)methodCallExpression.Arguments[0];
                    }
                    else if (methodCall.Arguments[0] is ConstantExpression constant)
                    {
                        constantExpression = constant;
                    }
                    else
                        throw new NotSupportedException();
                }
                else if (executionPlan is InvocationExpression invocation)
                {
                    if (invocation.Arguments[0] is MethodCallExpression method)
                    {
                        UnaryExpression unaryExpression = (UnaryExpression)method.Arguments[0];
                        MethodCallExpression methodCallExpression = (MethodCallExpression)unaryExpression.Operand;
                        constantExpression = (ConstantExpression)methodCallExpression.Arguments[0];
                    }
                    else if (invocation.Arguments[0] is UnaryExpression unary)
                    {
                        MethodCallExpression methodCallExpression = (MethodCallExpression)unary.Operand;
                        constantExpression = (ConstantExpression)methodCallExpression.Arguments[0];
                    }
                    else
                        throw new NotSupportedException();
                }
                else
                    throw new NotSupportedException();

                QueryableExecutionModel queryableExecutionModel = (QueryableExecutionModel)constantExpression.Value;
                IEnumerable<BsonDocument> stages = (IEnumerable<BsonDocument>)typeof(AggregateQueryableExecutionModel<>).MakeGenericType(queryableExecutionModel.OutputType).GetProperty("Stages").GetValue(queryableExecutionModel);
                UpdateLookup(stages, m_joinItems);

                DaoFactory.LogHelper.Info("mongoDB", queryableExecutionModel.ToString());

                LambdaExpression lambda = Expression.Lambda(executionPlan);

                try
                {
                    return lambda.Compile().DynamicInvoke(null);
                }
                catch (TargetInvocationException tie)
                {
                    throw tie.InnerException;
                }
            }

            private static Tuple<ConstantExpression, bool> GetJoinExpression(Expression joinExpression)
            {
                if (joinExpression is MethodCallExpression methodCallExpression)
                {
                    if (methodCallExpression.Arguments[0] is ConstantExpression constantExpression)
                        return Tuple.Create(constantExpression, true);
                    else
                        return GetJoinExpression(methodCallExpression.Arguments[0]);
                }
                else if (joinExpression is ConstantExpression constantExpression)
                    return Tuple.Create(constantExpression, false);
                else
                    throw new NotSupportedException();
            }

            private static void UpdateLookup(IEnumerable<BsonDocument> stages, IDictionary<string, JoinItem> joinItems)
            {
                foreach (BsonDocument lookup in stages.Where(item => item.ElementCount == 1 && item.Elements.First().Name == "$lookup"))
                {
                    BsonDocument lookupValue = lookup.First().Value.AsBsonDocument;
                    string localField = lookupValue.GetElement("localField").Value.AsString;
                    string foreignField = lookupValue.GetElement("foreignField").Value.AsString;
                    string referenceKey = JsonUtils.PropertyNameToJavaScriptStyle(localField).Replace(".", "_");
                    string key = lookupValue.GetElement("as").Value.AsString;

                    lookupValue.Remove("localField");
                    lookupValue.Remove("foreignField");

                    lookupValue.Add(new BsonElement("let", new BsonDocument
                    {
                        { $"{referenceKey}", $"${localField}" }
                    }));

                    BsonArray pipline = new BsonArray { new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("$eq", new BsonArray { $"${foreignField}", $"$${referenceKey}" })
                    })))};

                    if (joinItems.ContainsKey(key))
                    {
                        JoinItem joinItem = joinItems[key];
                        QueryableExecutionModel queryableExecutionModel = ((IMongoQueryable)joinItem.MongoQueryable.Provider.CreateQuery(joinItem.Expression)).GetExecutionModel();
                        IEnumerable<BsonDocument> joinMatchs = (IEnumerable<BsonDocument>)typeof(AggregateQueryableExecutionModel<>).MakeGenericType(queryableExecutionModel.OutputType).GetProperty("Stages")
                                                               .GetValue(queryableExecutionModel);

                        pipline.AddRange(joinMatchs);
                    }

                    lookupValue.Add(new BsonElement("pipeline",
                        pipline));
                }
            }

            private static Type GetSequenceElementType(Type type)
            {
                Type ienum = FindIEnumerable(type);
                if (ienum == null) { return type; }
                return ienum.GetTypeInfo().GetGenericArguments()[0];
            }

            public static Type FindIEnumerable(Type seqType)
            {
                if (seqType == null || seqType == typeof(string))
                {
                    return null;
                }

                var seqTypeInfo = seqType.GetTypeInfo();
                if (seqTypeInfo.IsGenericType && seqTypeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return seqType;
                }

                if (seqTypeInfo.IsArray)
                {
                    return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
                }

                if (seqTypeInfo.IsGenericType)
                {
                    foreach (Type arg in seqType.GetTypeInfo().GetGenericArguments())
                    {
                        Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                        if (ienum.GetTypeInfo().IsAssignableFrom(seqType))
                        {
                            return ienum;
                        }
                    }
                }

                Type[] ifaces = seqTypeInfo.GetInterfaces();
                if (ifaces != null && ifaces.Length > 0)
                {
                    foreach (Type iface in ifaces)
                    {
                        Type ienum = FindIEnumerable(iface);
                        if (ienum != null) { return ienum; }
                    }
                }

                if (seqTypeInfo.BaseType != null && seqTypeInfo.BaseType != typeof(object))
                {
                    return FindIEnumerable(seqTypeInfo.BaseType);
                }

                return null;
            }
        }

        private class MongoDBDaoInstance<T> : ISearchQuery<T>, IEditQuery<T>
            where T : class, IEntity, new()
        {
            private static readonly Expression<Func<T, bool>> EMPTY_PREDICATE;
            private static readonly IDictionary<IMongoDatabase, IMongoCollection<T>> m_collection;
            private static readonly MethodInfo m_countMethodInfo;

            public ITransaction BeginTransaction(bool distributedLock = true, int weight = 0)
            {
                return new MongoDBTransaction(weight, distributedLock);
            }

            public async Task<ITransaction> BeginTransactionAsync(bool distributedLock = true, int weight = 0)
            {
                return await Task.FromResult(new MongoDBTransaction(weight, distributedLock));
            }

            public void Delete(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = WriteApply<T>(transaction, ids);
                DaoFactory.LogHelper.Info("mongoDB", $"delete ids: {string.Join(",", ids)}");

                if (!inTransaction)
                    GetCollection(m_masterMongoDatabase).DeleteMany(Builders<T>.Filter.In(nameof(IEntity.ID), ids));
                else
                    GetCollection(m_masterMongoDatabase).DeleteMany(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.In(nameof(IEntity.ID), ids));
            }

            public async Task DeleteAsync(ITransaction transaction = null, params long[] ids)
            {
                bool inTransaction = await WriteApplyAsync<T>(transaction, ids);
                await DaoFactory.LogHelper.Info("mongoDB", $"delete ids: {string.Join(",", ids)}");

                if (!inTransaction)
                    await GetCollection(m_masterMongoDatabase).DeleteManyAsync(Builders<T>.Filter.In(nameof(IEntity.ID), ids));
                else
                    await GetCollection(m_masterMongoDatabase).DeleteManyAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.In(nameof(IEntity.ID), ids));
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
                bool inTransaction = Apply<T>(transaction) && WriteApply<T>(transaction, datas.Select(item => item.ID));
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
                bool inTransaction = await ApplyAsync<T>(transaction) && await WriteApplyAsync<T>(transaction, datas.Select(item => item.ID));
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

            public void Update(T data, ITransaction transaction = null)
            {
                bool inTransaction = WriteApply<T>(transaction, new long[] { data.ID });
                DaoFactory.LogHelper.Info("mongoDB", $"update data: {Environment.NewLine}{JObject.FromObject(data)}");

                if (!inTransaction)
                    GetCollection(m_masterMongoDatabase).ReplaceOne(Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
                else
                    GetCollection(m_masterMongoDatabase).ReplaceOne(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
            }

            public void Update(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                DaoFactory.LogHelper.Info("mongoDB", $"update predicate: {predicate}{Environment.NewLine}values: {Environment.NewLine}{string.Join(Environment.NewLine, upateDictionary.Select(item => $"{item.Key}: {item.Value}"))}");

                IEnumerable<long> ids = null;

                if (transaction == null)
                {
                    using (ISearchQueryable<T> queryable = GetQueryable())
                    {
                        ids = queryable.Where(predicate).Select(item => item.ID);
                    }
                }
                else
                {
                    ids = GetQueryable(transaction).Where(predicate).Select(item => item.ID);
                }

                if (ids == null || ids.Count() == 0)
                    return;

                bool inTransaction = WriteApply<T>(transaction, ids);

                if (!inTransaction)
                {
                    foreach (var item in upateDictionary)
                    {
                        GetCollection(m_masterMongoDatabase).UpdateMany(item => ids.Contains(item.ID), Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
                else
                {
                    foreach (var item in upateDictionary)
                    {
                        GetCollection(m_masterMongoDatabase).UpdateMany(((MongoDBTransaction)transaction).ClientSessionHandle, item => ids.Contains(item.ID), Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
            }

            public async Task UpdateAsync(T data, ITransaction transaction = null)
            {
                bool inTransaction = await WriteApplyAsync<T>(transaction, new long[] { data.ID });
                await DaoFactory.LogHelper.Info("mongoDB", $"update data: {Environment.NewLine}{JObject.FromObject(data)}");

                if (!inTransaction)
                    await GetCollection(m_masterMongoDatabase).ReplaceOneAsync(Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
                else
                    await GetCollection(m_masterMongoDatabase).ReplaceOneAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), data.ID), data);
            }

            public async Task UpdateAsync(Expression<Func<T, bool>> predicate, IDictionary<string, object> upateDictionary, ITransaction transaction = null)
            {
                await DaoFactory.LogHelper.Info("mongoDB", $"update predicate: {predicate}{Environment.NewLine}values: {Environment.NewLine}{string.Join(Environment.NewLine, upateDictionary.Select(item => $"{item.Key}: {item.Value}"))}");

                IEnumerable<long> ids = null;

                if (transaction == null)
                {
                    using (ISearchQueryable<T> queryable = await GetQueryableAsync())
                    {
                        ids = queryable.Where(predicate).Select(item => item.ID);
                    }
                }
                else
                {
                    ids = (await GetQueryableAsync(transaction)).Where(predicate).Select(item => item.ID);
                }

                if (ids == null || ids.Count() == 0)
                    return;

                bool inTransaction = await WriteApplyAsync<T>(transaction, ids);

                if (!inTransaction)
                {
                    foreach (var item in upateDictionary)
                    {
                        await GetCollection(m_masterMongoDatabase).UpdateManyAsync(item => ids.Contains(item.ID), Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
                else
                {
                    foreach (var item in upateDictionary)
                    {
                        await GetCollection(m_masterMongoDatabase).UpdateManyAsync(((MongoDBTransaction)transaction).ClientSessionHandle, item => ids.Contains(item.ID), Builders<T>.Update.Set(item.Key, item.Value));
                    }
                }
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
                m_countMethodInfo = typeof(Queryable).GetMethods().Where(item => item.Name == "Count").ElementAt(0);
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

            private static void ValidTransaction(ITransaction transaction, IEnumerable<long> ids, bool forUpdate = false)
            {
                bool inTransaction = false;

                if (ids == null || ids.Count() == 0)
                    return;

                if (forUpdate)
                    inTransaction = WriteApply<T>(transaction, ids);
                else
                    inTransaction = ReadApply<T>(transaction, ids);

                if (!inTransaction)
                    throw new DealException($"当前未查询到事务信息，请先使用{nameof(MongoDBDaoInstance<T>.BeginTransaction)}开启事务。");
            }

            private async static Task ValidTransactionAsync(ITransaction transaction, IEnumerable<long> ids, bool forUpdate = false)
            {
                bool inTransaction = false;

                if (ids == null || ids.Count() == 0)
                    return;

                if (forUpdate)
                    inTransaction = await WriteApplyAsync<T>(transaction, ids);
                else
                    inTransaction = await ReadApplyAsync<T>(transaction, ids);

                if (!inTransaction)
                    throw new DealException($"当前未查询到事务信息，请先使用{nameof(MongoDBDaoInstance<T>.BeginTransaction)}开启事务。");
            }

            public T Get(long id, ITransaction transaction, bool forUpdate = false)
            {
                ValidTransaction(transaction, new long[] { id }, forUpdate);
                DaoFactory.LogHelper.Info("mongoDB", $"get id: {id}");

                return GetCollection(m_masterMongoDatabase).Find(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), id)).FirstOrDefault();
            }

            public T Get(long id, IDBResourceContent dbResourceContent = null)
            {
                DaoFactory.LogHelper.Info("mongoDB", $"get id: {id}");
                return GetCollection(m_slaveMongoDatabase).Find(Builders<T>.Filter.Eq(nameof(IEntity.ID), id)).FirstOrDefault();
            }

            public async Task<T> GetAsync(long id, ITransaction transaction, bool forUpdate = false)
            {
                await ValidTransactionAsync(transaction, new long[] { id }, forUpdate);
                await DaoFactory.LogHelper.Info("mongoDB", $"get id: {id}");

                return await (await GetCollection(m_masterMongoDatabase).FindAsync(((MongoDBTransaction)transaction).ClientSessionHandle, Builders<T>.Filter.Eq(nameof(IEntity.ID), id))).FirstOrDefaultAsync();
            }

            public async Task<T> GetAsync(long id, IDBResourceContent dbResourceContent = null)
            {
                await DaoFactory.LogHelper.Info("mongoDB", $"get id: {id}");

                return await (await GetCollection(m_slaveMongoDatabase).FindAsync(Builders<T>.Filter.Eq(nameof(IEntity.ID), id))).FirstOrDefaultAsync();
            }

            public int Count(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                IEnumerable<long> ids = GetQueryable(transaction).Where(predicate ?? EMPTY_PREDICATE).Select(item => item.ID).ToList();

                ValidTransaction(transaction, ids, forUpdate);

                return ids.Count();
            }

            public int Count(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                DaoFactory.LogHelper.Info("mongoDB", $"count predicate: {predicate}");

                return (int)GetCollection(m_slaveMongoDatabase).CountDocuments(predicate ?? EMPTY_PREDICATE);
            }

            public async Task<int> CountAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, bool forUpdate = false)
            {
                IEnumerable<long> ids = (await GetQueryableAsync(transaction)).Where(predicate ?? EMPTY_PREDICATE).Select(item => item.ID).ToList();

                await DaoFactory.LogHelper.Info("mongoDB", $"count predicate: {predicate}");
                await ValidTransactionAsync(transaction, ids, forUpdate);

                return ids.Count();
            }

            public async Task<int> CountAsync(Expression<Func<T, bool>> predicate = null, IDBResourceContent dbResourceContent = null)
            {
                await DaoFactory.LogHelper.Info("mongoDB", $"count predicate: {predicate}");
                return (int)await GetCollection(m_slaveMongoDatabase).CountDocumentsAsync(predicate ?? EMPTY_PREDICATE);
            }

            public IEnumerable<T> Search(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
            {
                DaoFactory.LogHelper.Info("mongoDB", $"search{Environment.NewLine}predicate: {predicate}{Environment.NewLine}orderBy: {GetOrderByString(queryOrderBies)}{Environment.NewLine}startIndex: {startIndex}{Environment.NewLine}count: {count}");

                IFindFluent<T, T> findFluent = GetCollection(m_masterMongoDatabase).Find(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE).GetSortFluent(queryOrderBies);

                IEnumerable<long> ids = findFluent.GetIDProjectFluent().Skip(startIndex).Limit(count).ToList().Select(item => item.ID);

                ValidTransaction(transaction, ids, forUpdate);

                return GetQueryable(transaction).Where(item => ids.Contains(item.ID));
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
            {
                DaoFactory.LogHelper.Info("mongoDB", $"search{Environment.NewLine}predicate: {predicate}{Environment.NewLine}orderBy: {GetOrderByString(queryOrderBies)}{Environment.NewLine}startIndex: {startIndex}{Environment.NewLine}count: {count}");

                IFindFluent<T, T> findFluent = GetCollection(m_slaveMongoDatabase).Find(predicate ?? EMPTY_PREDICATE).GetSortFluent(queryOrderBies);

                return findFluent.Skip(startIndex).Limit(count).ToList();
            }

            public async Task<IEnumerable<T>> SearchAsync(ITransaction transaction, Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, bool forUpdate = false)
            {
                await DaoFactory.LogHelper.Info("mongoDB", $"search{Environment.NewLine}predicate: {predicate}{Environment.NewLine}orderBy: {GetOrderByString(queryOrderBies)}{Environment.NewLine}startIndex: {startIndex}{Environment.NewLine}count: {count}");

                IFindFluent<T, T> findFluent = GetCollection(m_masterMongoDatabase).Find(((MongoDBTransaction)transaction).ClientSessionHandle, predicate ?? EMPTY_PREDICATE).GetSortFluent(queryOrderBies);

                IEnumerable<long> ids = (await findFluent.GetIDProjectFluent().Skip(startIndex).Limit(count).ToListAsync()).Select(item => item.ID);

                await ValidTransactionAsync(transaction, ids, forUpdate);

                return (await GetQueryableAsync(transaction)).Where(item => ids.Contains(item.ID));
            }

            public async Task<IEnumerable<T>> SearchAsync(Expression<Func<T, bool>> predicate = null, IEnumerable<QueryOrderBy<T>> queryOrderBies = null, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
            {
                await DaoFactory.LogHelper.Info("mongoDB", $"search{Environment.NewLine}predicate: {predicate}{Environment.NewLine}orderBy: {GetOrderByString(queryOrderBies)}{Environment.NewLine}startIndex: {startIndex}{Environment.NewLine}count: {count}");

                IFindFluent<T, T> findFluent = GetCollection(m_slaveMongoDatabase).Find(predicate ?? EMPTY_PREDICATE).GetSortFluent(queryOrderBies);

                return await findFluent.Skip(startIndex).Limit(count).ToListAsync();
            }

            public int Count<TResult>(ITransaction transaction, IQueryable<TResult> query)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return queryable.Count();
            }

            public int Count<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return queryable.Count();
            }

            public async Task<int> CountAsync<TResult>(ITransaction transaction, IQueryable<TResult> query)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return await ((MongoQueryableProvider)queryable.Provider).ExecuteAsync<int>(Expression.Call(m_countMethodInfo.MakeGenericMethod(typeof(TResult)), queryable.Expression));
            }

            public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, IDBResourceContent dbResourceContent = null)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return await ((MongoQueryableProvider)queryable.Provider).ExecuteAsync<int>(Expression.Call(m_countMethodInfo.MakeGenericMethod(typeof(TResult)), queryable.Expression));
            }

            public IEnumerable<TResult> Search<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return queryable.Skip(startIndex).Take(count).ToList();
            }

            public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return queryable.Skip(startIndex).Take(count).ToList();
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(ITransaction transaction, IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                IAsyncCursor<TResult> asyncCursor = await ((MongoQueryableProvider)queryable.Provider).ExecuteAsync<IAsyncCursor<TResult>>(queryable.Expression);
                IList<TResult> results = new List<TResult>();

                while (await asyncCursor.MoveNextAsync(CancellationToken.None))
                    results.AddRange(asyncCursor.Current);

                return results;
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, IDBResourceContent dbResourceContent = null)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                IAsyncCursor<TResult> asyncCursor = await ((MongoQueryableProvider)queryable.Provider).ExecuteAsync<IAsyncCursor<TResult>>(queryable.Expression);
                IList<TResult> results = new List<TResult>();

                while (await asyncCursor.MoveNextAsync(CancellationToken.None))
                    results.AddRange(asyncCursor.Current);

                return results;
            }

            public ISearchQueryable<T> GetQueryable(ITransaction transaction)
            {
                IQueryable<T> queryable = GetCollection(m_masterMongoDatabase).AsQueryable(((MongoDBTransaction)transaction).ClientSessionHandle);
                return new MongoQueryable<T>(queryable, new MongoQueryableProvider(queryable.Provider));
            }

            public ISearchQueryable<T> GetQueryable(IDBResourceContent dbResourceContent = null)
            {
                IQueryable<T> queryable = GetCollection(m_slaveMongoDatabase).AsQueryable();
                return new MongoQueryable<T>(queryable, new MongoQueryableProvider(queryable.Provider));
            }

            public Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction)
            {
                IQueryable<T> queryable = GetCollection(m_masterMongoDatabase).AsQueryable(((MongoDBTransaction)transaction).ClientSessionHandle);
                return Task.FromResult<ISearchQueryable<T>>(new MongoQueryable<T>(queryable, new MongoQueryableProvider(queryable.Provider)));
            }

            public Task<ISearchQueryable<T>> GetQueryableAsync(IDBResourceContent dbResourceContent = null)
            {
                IQueryable<T> queryable = GetCollection(m_slaveMongoDatabase).AsQueryable();
                return Task.FromResult<ISearchQueryable<T>>(new MongoQueryable<T>(GetCollection(m_slaveMongoDatabase).AsQueryable(), new MongoQueryableProvider(queryable.Provider)));
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
                Servers = new HashSet<MongoServerAddress>(ConfigManager.Configuration.GetSection("MongoDBService:Addresses").GetChildren().Select(item => MongoServerAddress.Parse(item.Value)))
            });

            m_slaveMongoClient = new MongoClient(new MongoClientSettings()
            {
                ConnectionMode = ConnectionMode.ReplicaSet,
                ReadPreference = new ReadPreference(ReadPreferenceMode.Secondary),
                ReplicaSetName = ConfigManager.Configuration["MongoDBService:ReplicaSet"],
                Servers = new HashSet<MongoServerAddress>(ConfigManager.Configuration.GetSection("MongoDBService:Addresses").GetChildren().Select(item => MongoServerAddress.Parse(item.Value)))
            });

            m_masterMongoDatabase = m_masterMongoClient.GetDatabase(ConfigManager.Configuration["MongoDBService:Database"]);
            m_slaveMongoDatabase = m_slaveMongoClient.GetDatabase(ConfigManager.Configuration["MongoDBService:Database"]);

            BsonSerializer.RegisterSerializer(typeof(DateTime), new DateTimeSerializer(DateTimeKind.Local, BsonType.DateTime));
        }
    }
}