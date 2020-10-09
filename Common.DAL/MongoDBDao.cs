using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.DAL.Transaction;
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

        private class MongoQueryable<T> : ISearchQueryable<T>
        {
            public IQueryable<T> InnerQuery { get; }

            public Type ElementType => InnerQuery.ElementType;

            public Expression Expression => InnerQuery.Expression;

            public IQueryProvider Provider { get; }

            public void Dispose() { }

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
            public OrderedMongoQueryable(IQueryable<T> innerQuery, IQueryProvider queryProvider) : base(innerQuery, queryProvider) { }
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
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return queryable.Count();
            }

            public async Task<int> CountAsync<TResult>(IQueryable<TResult> query, ITransaction _ = null)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return await ((MongoQueryableProvider)queryable.Provider).ExecuteAsync<int>(Expression.Call(m_countMethodInfo.MakeGenericMethod(typeof(TResult)), queryable.Expression));
            }

            public IEnumerable<TResult> Search<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction _ = null)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                return queryable.Skip(startIndex).Take(count).ToList();
            }

            public async Task<IEnumerable<TResult>> SearchAsync<TResult>(IQueryable<TResult> query, int startIndex = 0, int count = int.MaxValue, ITransaction _ = null)
            {
                MongoQueryable<TResult> queryable = (MongoQueryable<TResult>)query;
                IAsyncCursor<TResult> asyncCursor = await ((MongoQueryableProvider)queryable.Provider).ExecuteAsync<IAsyncCursor<TResult>>(queryable.Expression);
                IList<TResult> results = new List<TResult>();

                while (await asyncCursor.MoveNextAsync(CancellationToken.None))
                    results.AddRange(asyncCursor.Current);

                return results;
            }

            public ISearchQueryable<T> GetQueryable(ITransaction transaction = null)
            {
                bool inTransaction = Apply<T>(transaction);

                if (!inTransaction)
                {
                    IQueryable<T> queryable = GetCollection(m_slaveMongoDatabase).AsQueryable();
                    return new MongoQueryable<T>(queryable, new MongoQueryableProvider(queryable.Provider));
                }
                else
                {
                    IQueryable<T> queryable = GetCollection(m_masterMongoDatabase).AsQueryable(((MongoDBTransaction)transaction).ClientSessionHandle);
                    return new MongoQueryable<T>(queryable, new MongoQueryableProvider(queryable.Provider));
                }
            }

            public async Task<ISearchQueryable<T>> GetQueryableAsync(ITransaction transaction = null)
            {
                bool inTransaction = await ApplyAsync<T>(transaction);

                if (!inTransaction)
                {
                    IQueryable<T> queryable = GetCollection(m_slaveMongoDatabase).AsQueryable();
                    return new MongoQueryable<T>(GetCollection(m_slaveMongoDatabase).AsQueryable(), new MongoQueryableProvider(queryable.Provider));
                }
                else
                {
                    IQueryable<T> queryable = GetCollection(m_masterMongoDatabase).AsQueryable(((MongoDBTransaction)transaction).ClientSessionHandle);
                    return new MongoQueryable<T>(queryable, new MongoQueryableProvider(queryable.Provider));
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
