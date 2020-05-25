﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Core.Cache.Configuration;
using Apache.Ignite.Core.Cache.Query;
using Apache.Ignite.Core.Configuration;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Multicast;
using Apache.Ignite.Linq;

namespace Common.DAL
{
    internal static class IgniteDao
    {
        private readonly static IIgnite m_ignite;

        private class IgniteITransaction : ITransaction
        {
            private Apache.Ignite.Core.Transactions.ITransaction m_transaction;

            public IgniteITransaction(IIgnite ignite)
            {
                m_transaction = ignite.GetTransactions().TxStart();
            }

            public object Context()
            {
                return m_transaction;
            }

            public void Dispose()
            {
                m_transaction.Dispose();
            }

            public void Rollback()
            {
                m_transaction.Rollback();
            }

            public void Submit()
            {
                m_transaction.Commit();
            }
        }

        private class IgniteJoinQuery<TTable, TJoinTable>
            where TTable : IEntity, new()
            where TJoinTable : IEntity, new()
        {
            private readonly static Expression<Func<TTable, TJoinTable, bool>> m_defaultWhereExpression;
            private readonly static Func<object, Expression<Func<TTable, TJoinTable, bool>>, IQueryable<object>> m_queryableExpressionCreator;
            private readonly static Func<object, Expression<Func<TTable, TJoinTable, bool>>, int> m_countExpressionCreator;
            private readonly static Func<object, JoinResult<TTable, TJoinTable>> m_createDataExpressionCreator;
            private readonly static Func<object, Expression<Func<TTable, TJoinTable, object>>, object> m_orderByAscExpressionCeator;
            private readonly static Func<object, Expression<Func<TTable, TJoinTable, object>>, object> m_orderByDescExpressionCeator;

            private static Expression<Func<TChange, TResult>> ConvertJoinWhereExpression<TLeft, TRight, TChange, TResult>(Expression<Func<TLeft, TRight, TResult>> expression)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(TChange), "item");

                Expression<Func<TChange, TLeft>> leftExpression =
                    Expression.Lambda<Func<TChange, TLeft>>(Expression.Property(Expression.Property(parameter, "left"), "Value"), parameter);
                Expression<Func<TChange, TRight>> rightExpression =
                    Expression.Lambda<Func<TChange, TRight>>(Expression.Property(Expression.Property(parameter, "right"), "Value"), parameter);

                return expression.SingleChangeParameter(leftExpression,
                                                        rightExpression,
                                                        expression.Parameters[0].Name,
                                                        expression.Parameters[1].Name);
            }

            public IEnumerable<JoinResult<TTable, TJoinTable>> Search(IQueryable<ICacheEntry<long, TTable>> tableQuery,
                                                                      IQueryable<ICacheEntry<long, TJoinTable>> joinQuery,
                                                                      Expression<Func<ICacheEntry<long, TTable>, long>> leftJoinExpression,
                                                                      Expression<Func<ICacheEntry<long, TJoinTable>, long>> rightJoinExpression,
                                                                      Expression<Func<TTable, TJoinTable, bool>> predicate,
                                                                      IEnumerable<QueryOrderBy<TTable, TJoinTable>> queryOrderBies,
                                                                      int startIndex,
                                                                      int count)
            {
                var query = tableQuery.Join(joinQuery, leftJoinExpression, rightJoinExpression, (left, right) => new { left, right });
                object result = m_queryableExpressionCreator.Invoke(query, predicate ?? m_defaultWhereExpression);

                if (queryOrderBies != null)
                {
                    foreach (QueryOrderBy<TTable, TJoinTable> queryOrderBy in queryOrderBies)
                    {
                        if (queryOrderBy.OrderByType == OrderByType.Asc)
                            result = m_orderByAscExpressionCeator.Invoke(result, queryOrderBy.Expression);
                        else
                            result = m_orderByDescExpressionCeator.Invoke(result, queryOrderBy.Expression);
                    }
                }

                IQueryable<object> queryable = (IQueryable<object>)result;
                queryable = queryable.Skip(startIndex).Take(count);

                OutpuSql(((ICacheQueryable)queryable).GetFieldsQuery());

                foreach (object data in queryable)
                    yield return m_createDataExpressionCreator.Invoke(data);
            }

            public int Count(IQueryable<ICacheEntry<long, TTable>> tableQuery,
                             IQueryable<ICacheEntry<long, TJoinTable>> joinQuery,
                             Expression<Func<ICacheEntry<long, TTable>, long>> leftJoinExpression,
                             Expression<Func<ICacheEntry<long, TJoinTable>, long>> rightJoinExpression,
                             Expression<Func<TTable, TJoinTable, bool>> predicate)
            {
                var query = tableQuery.Join(joinQuery, leftJoinExpression, rightJoinExpression, (left, right) => new { left, right });
                return m_countExpressionCreator.Invoke(query, predicate ?? m_defaultWhereExpression);
            }

            static IgniteJoinQuery()
            {
                m_defaultWhereExpression = (left, right) => true;
                Type reultType = GetResultType();

                ParameterExpression item = Expression.Parameter(typeof(object), "item");
                ParameterExpression query = Expression.Parameter(typeof(object), "query");
                ParameterExpression orderBy = Expression.Parameter(typeof(Expression<Func<TTable, TJoinTable, object>>), "orderBy");
                ParameterExpression predicate = Expression.Parameter(typeof(Expression<Func<TTable, TJoinTable, bool>>), "predicate");
                Expression convertPredicate = Expression.Call(typeof(IgniteJoinQuery<TTable, TJoinTable>).GetMethod(nameof(ConvertJoinWhereExpression), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(typeof(TTable), typeof(TJoinTable), reultType, typeof(bool)), predicate);
                Expression convertOrderBy = Expression.Call(typeof(IgniteJoinQuery<TTable, TJoinTable>).GetMethod(nameof(ConvertJoinWhereExpression), BindingFlags.NonPublic | BindingFlags.Static).MakeGenericMethod(typeof(TTable), typeof(TJoinTable), reultType, typeof(object)), orderBy);
                Expression queryable = Expression.Convert(query, typeof(IQueryable<>).MakeGenericType(reultType));
                Expression data = Expression.Convert(item, reultType);

                m_queryableExpressionCreator =
                    Expression.Lambda<Func<object, Expression<Func<TTable, TJoinTable, bool>>, IQueryable<object>>>(
                        Expression.Call(typeof(Queryable).GetMethods().Where(method => method.Name == nameof(Queryable.Where)).ElementAt(0).MakeGenericMethod(reultType), queryable, convertPredicate),
                                        query, predicate).Compile();

                m_countExpressionCreator =
                    Expression.Lambda<Func<object, Expression<Func<TTable, TJoinTable, bool>>, int>>(
                        Expression.Call(typeof(Queryable).GetMethods().Where(method => method.Name == nameof(Queryable.Count)).ElementAt(1).MakeGenericMethod(reultType), queryable, convertPredicate),
                                        query, predicate).Compile();

                m_createDataExpressionCreator =
                    Expression.Lambda<Func<object, JoinResult<TTable, TJoinTable>>>(
                        Expression.New(typeof(JoinResult<TTable, TJoinTable>).GetConstructor(new[] { typeof(TTable), typeof(TJoinTable) }),
                                       Expression.Property(Expression.Property(data, "left"), "Value"),
                                       Expression.Property(Expression.Property(data, "right"), "Value")),
                                       item).Compile();

                m_orderByAscExpressionCeator =
                    Expression.Lambda<Func<object, Expression<Func<TTable, TJoinTable, object>>, object>>(
                        Expression.Call(typeof(Queryable).GetMethods().Where(method => method.Name == nameof(Queryable.OrderBy)).ElementAt(0).MakeGenericMethod(reultType, typeof(object)), queryable, convertOrderBy),
                                        query, orderBy).Compile();

                m_orderByDescExpressionCeator =
                    Expression.Lambda<Func<object, Expression<Func<TTable, TJoinTable, object>>, object>>(
                        Expression.Call(typeof(Queryable).GetMethods().Where(method => method.Name == nameof(Queryable.OrderByDescending)).ElementAt(0).MakeGenericMethod(reultType, typeof(object)), queryable, convertOrderBy),
                                        query, orderBy).Compile();
            }

            static Type GetResultType(ICacheEntry<long, TTable> left = null, ICacheEntry<long, TJoinTable> right = null)
            {
                return new { left, right }.GetType();
            }
        }

        private class IgniteDaoInstance<T> : ISearchQuery<T>, IEditQuery<T>
            where T : class, IEntity, new()
        {
            private const string SQL_PARAMETER_KEYWORD = "?";
            private readonly static IReadOnlyDictionary<string, Action<T, object>> m_propertyDic;
            private ICache<long, T> m_cache;

            private static CacheConfiguration CacheConfiguration { get; }

            private static Expression<Func<ICacheEntry<long, TTable>, TResult>> ConvertExpression<TTable, TResult>(Expression<Func<TTable, TResult>> expression)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(ICacheEntry<long, TTable>), "item");

                Expression<Func<ICacheEntry<long, TTable>, TTable>> valueExpression =
                    Expression.Lambda<Func<ICacheEntry<long, TTable>, TTable>>(Expression.Property(parameter, "Value"), parameter);

                return expression.ChangeParameter(valueExpression);
            }

            private static Expression<Func<ICacheEntry<long, TLeft>, ICacheEntry<long, TRight>, TResult>> ConvertExpression<TLeft, TRight, TResult>(Expression<Func<TLeft, TRight, TResult>> expression)
            {
                ParameterExpression parameter = Expression.Parameter(typeof(TResult), "item");

                Expression<Func<ICacheEntry<long, TLeft>, TLeft>> leftExpression =
                    Expression.Lambda<Func<ICacheEntry<long, TLeft>, TLeft>>(Expression.Property(parameter, "Value"), parameter);
                Expression<Func<ICacheEntry<long, TRight>, TRight>> rightExpression =
                    Expression.Lambda<Func<ICacheEntry<long, TRight>, TRight>>(Expression.Property(parameter, "Value"), parameter);

                return expression.MultiChangeParameter(leftExpression,
                                                       rightExpression,
                                                       expression.Parameters[0].Name,
                                                       expression.Parameters[1].Name);
            }

            public ITransaction BeginTransaction()
            {
                return new IgniteITransaction(m_cache.Ignite);
            }

            public int Count(Expression<Func<T, bool>> predicate = null)
            {
                if (predicate != null)
                    return m_cache.AsCacheQueryable().Count(ConvertExpression(predicate));
                else
                    return m_cache.AsCacheQueryable().Count();
            }

            public int Count(string queryWhere, Dictionary<string, object> parameters = null)
            {
                object[] args = new object[parameters?.Count ?? 0];
                int index = 0;

                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> item in parameters)
                    {
                        queryWhere = queryWhere.Replace(item.Key, SQL_PARAMETER_KEYWORD);
                        args[index++] = item.Value;
                    }
                }

                SqlFieldsQuery sqlFieldsQuery = null;

                if (args.Length > 0)
                    sqlFieldsQuery = new SqlFieldsQuery($"SELECT COUNT(*) FROM {typeof(T).Name} WHERE {queryWhere}", args);
                else
                    sqlFieldsQuery = new SqlFieldsQuery($"SELECT COUNT(*) FROM {typeof(T).Name} WHERE {queryWhere}");

                OutpuSql(sqlFieldsQuery);

                IList<object> data = m_cache.Query(sqlFieldsQuery).FirstOrDefault();
                return Convert.ToInt32(data?[0]);
            }

            public void Delete(params long[] ids)
            {
                m_cache.RemoveAll(ids);
            }

            public T Get(long id)
            {
                if (m_cache.TryGet(id, out T data))
                    return data;
                else
                    return null;
            }

            public void Insert(params T[] datas)
            {
                m_cache.PutAll(datas.Select(data => new KeyValuePair<long, T>(data.ID, data)));
            }

            public void Merge(params T[] datas)
            {
                for (int i = 0; i < datas.Length; i++)
                    m_cache.Put(datas[i].ID, datas[i]);
            }

            public IEnumerable<T> Search(Expression<Func<T, bool>> predicate = null,
                                         IEnumerable<QueryOrderBy<T>> queryOrderBies = null,
                                         int startIndex = 0,
                                         int count = int.MaxValue)
            {
                IQueryable<ICacheEntry<long, T>> query = m_cache.AsCacheQueryable();

                if (predicate != null)
                    query = query.Where(ConvertExpression(predicate));

                if (queryOrderBies != null)
                {
                    foreach (QueryOrderBy<T> queryOrdery in queryOrderBies)
                    {
                        if (queryOrdery.OrderByType == OrderByType.Asc)
                            query = query.OrderBy(ConvertExpression(queryOrdery.Expression));
                        else
                            query = query.OrderByDescending(ConvertExpression(queryOrdery.Expression));
                    }
                }

                OutpuSql(((ICacheQueryable)query).GetFieldsQuery());

                foreach (ICacheEntry<long, T> item in query.Skip(startIndex).Take(count))
                    yield return item.Value;
            }

            public IEnumerable<T> Search(string queryWhere, Dictionary<string, object> parameters = null, string orderByFields = null, int startIndex = 0, int count = int.MaxValue)
            {
                object[] args = new object[parameters?.Count ?? 0];
                int index = 0;

                if (parameters != null)
                {
                    foreach (KeyValuePair<string, object> item in parameters)
                    {
                        queryWhere = queryWhere.Replace(item.Key, SQL_PARAMETER_KEYWORD);
                        args[index++] = item.Value;
                    }
                }

                string sql = string.Format("SELECT * FROM {0} {1} {2} LIMIT {4} OFFSET {3}",
                                           typeof(T).Name,
                                           string.IsNullOrWhiteSpace(queryWhere) ? string.Empty : string.Format("WHERE {0}", queryWhere),
                                           string.IsNullOrWhiteSpace(orderByFields) ? string.Empty : string.Format("ORDER BY {0}", orderByFields),
                                           startIndex,
                                           count);

                SqlFieldsQuery sqlFieldsQuery = null;

                if (args.Length > 0)
                     sqlFieldsQuery = new SqlFieldsQuery(sql, args);
                else
                    sqlFieldsQuery = new SqlFieldsQuery(sql);

                OutpuSql(sqlFieldsQuery);

                IFieldsQueryCursor fieldsQueryCursor = m_cache.Query(sqlFieldsQuery);
                return fieldsQueryCursor.Select(row => ConvertData(fieldsQueryCursor, row));
            }

            public void Update(T data, params string[] ignoreColumns)
            {
                T oldData = m_cache.Get(data.ID);

                if (oldData == null)
                    return;

                if (ignoreColumns != null)
                {
                    for (int i = 0; i < ignoreColumns.Length; i++)
                    {
                        PropertyInfo propertyInfo = typeof(T).GetProperty(ignoreColumns[i]);

                        if (propertyInfo == null || !propertyInfo.CanWrite)
                            continue;

                        propertyInfo.SetValue(data, propertyInfo.GetValue(oldData));
                    }
                }

                m_cache.Replace(data.ID, data);
            }

            public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression)
            {
                Func<T, bool> updateHandler = updateExpression.ReplaceAssign().Compile();

                foreach (ICacheEntry<long, T> entity in m_cache.AsCacheQueryable().Where(ConvertExpression(predicate)))
                {
                    if (updateHandler(entity.Value))
                        m_cache.Replace(entity.Key, entity.Value);
                }
            }

            public IEnumerable<JoinResult<T, TJoinTable>> Search<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                                                             Expression<Func<T, TJoinTable, bool>> predicate = null,
                                                                             IEnumerable<QueryOrderBy<T, TJoinTable>> queryOrderBies = null,
                                                                             int startIndex = 0,
                                                                             int count = int.MaxValue)
                where TJoinTable : class, IEntity, new()
            {
                return new IgniteJoinQuery<T, TJoinTable>().Search(m_cache.AsCacheQueryable(),
                                                                   m_cache.Ignite.GetOrCreateCache<long, TJoinTable>(IgniteDaoInstance<TJoinTable>.CacheConfiguration).AsCacheQueryable(),
                                                                   ConvertExpression(joinCondition.LeftJoinExpression),
                                                                   ConvertExpression(joinCondition.RightJoinExression),
                                                                   predicate,
                                                                   queryOrderBies,
                                                                   startIndex,
                                                                   count);
            }

            public int Count<TJoinTable>(JoinCondition<T, TJoinTable> joinCondition,
                                         Expression<Func<T, TJoinTable, bool>> predicate = null)
                where TJoinTable : class, IEntity, new()
            {
                return new IgniteJoinQuery<T, TJoinTable>().Count(m_cache.AsCacheQueryable(),
                                                                  m_cache.Ignite.GetOrCreateCache<long, TJoinTable>(IgniteDaoInstance<TJoinTable>.CacheConfiguration).AsCacheQueryable(),
                                                                  ConvertExpression(joinCondition.LeftJoinExpression),
                                                                  ConvertExpression(joinCondition.RightJoinExression),
                                                                  predicate);
            }

            private static T ConvertData(IFieldsQueryCursor fieldsQueryCursor, IList<object> row)
            {
                T data = new T();

                for (int i = 0; i < fieldsQueryCursor.FieldNames.Count; i++)
                {
                    if (!m_propertyDic.ContainsKey(fieldsQueryCursor.FieldNames[i]))
                        continue;

                    m_propertyDic[fieldsQueryCursor.FieldNames[i]].Invoke(data, row[i]);
                }

                return data;
            }

            private static object GetPropertyValue(Type propertyType, object value)
            {
                if (propertyType.IsGenericType &&
                    propertyType.IsValueType &&
                    propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (value == null)
                        return null;
                    else if (propertyType.GenericTypeArguments[0].IsEnum)
                        return Enum.ToObject(propertyType.GenericTypeArguments[0], value);
                    else if (propertyType.GenericTypeArguments[0] == typeof(bool))
                        return Convert.ToBoolean(value);
                    else if (propertyType.GenericTypeArguments[0] == typeof(DateTime))
                        return ((DateTime)value).ToLocalTime();
                    else
                        return value;
                }
                else if (propertyType.IsEnum)
                    return Enum.ToObject(propertyType, value);
                else if (propertyType == typeof(bool))
                    return Convert.ToBoolean(value);
                else if (propertyType == typeof(DateTime))
                    return ((DateTime)value).ToLocalTime();
                else
                    return value;
            }

            static IgniteDaoInstance()
            {
                m_propertyDic = new Dictionary<string, Action<T, object>>();
                IList<QueryField> queryFields = new List<QueryField>();
                IList<QueryIndex> queryIndices = new List<QueryIndex>();

                ParameterExpression data = Expression.Parameter(typeof(T), "data");
                ParameterExpression propertyValue = Expression.Parameter(typeof(object), "propertyValue");

                foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
                {
                    if (propertyInfo.CanWrite)
                    {
                        Expression expression = Expression.Assign(Expression.Property(data, propertyInfo),
                           Expression.Convert(Expression.Call(typeof(IgniteDaoInstance<T>).GetMethod(nameof(IgniteDaoInstance<T>.GetPropertyValue),
                           BindingFlags.Static | BindingFlags.NonPublic), Expression.Constant(propertyInfo.PropertyType, typeof(Type)), propertyValue), propertyInfo.PropertyType));

                        ((Dictionary<string, Action<T, object>>)m_propertyDic).Add(propertyInfo.Name.ToUpper(),
                                                                                   Expression.Lambda<Action<T, object>>(expression, data, propertyValue).Compile());
                    }

                    QuerySqlFieldAttribute querySqlFieldAttribute = propertyInfo.GetCustomAttribute<QuerySqlFieldAttribute>();

                    if (querySqlFieldAttribute != null)
                    {
                        queryFields.Add(new QueryField()
                        {
                            DefaultValue = querySqlFieldAttribute.DefaultValue,
                            Scale = querySqlFieldAttribute.Scale,
                            NotNull = querySqlFieldAttribute.NotNull,
                            Precision = querySqlFieldAttribute.Precision,
                            Name = propertyInfo.Name,
                            FieldType = GetFieldType(propertyInfo.PropertyType),
                        });

                        if (querySqlFieldAttribute.IsIndexed)
                        {
                            queryIndices.Add(new QueryIndex()
                            {
                                Fields = querySqlFieldAttribute.IndexGroups?.Select(fieldName => new QueryIndexField(fieldName)).ToArray() ?? new[] { new QueryIndexField(propertyInfo.Name) },
                                Name = string.Format("{0}_{1}_INDEX", typeof(T).Name, string.Join("_", querySqlFieldAttribute.IndexGroups?.Select(fieldName => fieldName.ToUpper()) ?? new[] { propertyInfo.Name })),
                                IndexType = QueryIndexType.Sorted
                            });
                        }
                    }
                }

                CacheConfiguration = new CacheConfiguration()
                {
                    Name = typeof(T).Name,
                    CacheMode = CacheMode.Replicated,
                    QueryEntities = new[] { new QueryEntity(typeof(long), typeof(T)) { Fields = queryFields, Indexes = queryIndices } },
                    SqlSchema = string.Format("\"{0}\"", ConfigManager.Configuration["IgniteService:RegionName"])
                };
            }

            private static Type GetFieldType(Type propertyType)
            {
                if (propertyType.IsGenericType &&
                    propertyType.IsValueType &&
                    propertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
                {
                    if (propertyType.GenericTypeArguments[0].IsEnum || propertyType.GenericTypeArguments[0] == typeof(bool))
                        return typeof(int);
                    else
                        return propertyType;
                }
                else if (propertyType.IsEnum || propertyType == typeof(bool))
                    return typeof(int);
                else
                    return propertyType;
            }

            public IEnumerable<IDictionary<string, object>> Query(string sql, Dictionary<string, object> parameters = null)
            {
                IFieldsQueryCursor fieldsQueryCursor = m_cache.Query(new SqlFieldsQuery(PreperSql(sql, parameters), parameters?.Values));

                foreach (IList<object> row in fieldsQueryCursor)
                {
                    IDictionary<string, object> data = new Dictionary<string, object>();

                    for (int i = 0; i < fieldsQueryCursor.FieldNames.Count; i++)
                        data.Add(fieldsQueryCursor.FieldNames[i], row[i]);

                    yield return data;
                }
            }

            public IgniteDaoInstance(IIgnite ignite)
            {
                m_cache = ignite.GetOrCreateCache<long, T>(CacheConfiguration);
            }
        }

        private static string PreperSql(string sql, Dictionary<string, object> parameters)
        {
            if (parameters != null)
                foreach (KeyValuePair<string, object> parameter in parameters)
                    sql.Replace($"@{parameter.Key}", "?");

            return sql;
        }

        private static void OutpuSql(SqlFieldsQuery sqlFieldsQuery)
        {
#if OUTPUT_SQL
            Console.WriteLine(GetSqlLog(sqlFieldsQuery.Sql, sqlFieldsQuery.Arguments));
#endif
        }

        private static string GetSqlLog(string sql, object[] arguments)
        {
            return string.Format("sql: {1}{0}parameter: {0}{2}",
                                 Environment.NewLine,
                                 sql,
                                 string.Join(Environment.NewLine, arguments));
        }

        static IgniteDao()
        {
            IList<BinaryTypeConfiguration> binaryTypeConfigurations = new List<BinaryTypeConfiguration>();

            Type[] modelTypes = TypeReflector.ReflectType((type) =>
            {
                if (type.GetInterface(typeof(IEntity).FullName) == null || type.IsInterface || type.IsAbstract)
                    return false;

                if (type.GetCustomAttribute<IgnoreTableAttribute>() != null)
                    return false;

                return true;
            });

            for (int i = 0; i < modelTypes.Length; i++)
            {
                binaryTypeConfigurations.Add(new BinaryTypeConfiguration(modelTypes[i])
                {
                    Serializer = (IBinarySerializer)Activator.CreateInstance(typeof(BinaryBufferSerializer<>).MakeGenericType(modelTypes[i])),
                });
            }

            IgniteConfiguration igniteConfiguration = new IgniteConfiguration()
            {
                Localhost = ConfigManager.Configuration["IgniteService:LocalHost"],

                DiscoverySpi = new TcpDiscoverySpi()
                {
                    IpFinder = new TcpDiscoveryMulticastIpFinder()
                    {
                        Endpoints = new[] { ConfigManager.Configuration["IgniteService:TcpDiscoveryMulticastIpFinderEndPoint"] }
                    }
                },

                DataStorageConfiguration = new DataStorageConfiguration
                {
                    DefaultDataRegionConfiguration = new DataRegionConfiguration
                    {
                        Name = ConfigManager.Configuration["IgniteService:RegionName"],
                        PersistenceEnabled = true
                    }
                },

                BinaryConfiguration = new BinaryConfiguration
                {
                    TypeConfigurations = binaryTypeConfigurations
                }
            };

            m_ignite = Ignition.Start(igniteConfiguration);

            //TODO: 基线拓扑
            m_ignite.GetCluster().SetActive(true);
        }

        internal static ISearchQuery<T> GetIgniteSearchQuery<T>()
            where T : class, IEntity, new()
        {
            return new IgniteDaoInstance<T>(m_ignite);
        }

        internal static IEditQuery<T> GetIgniteEditQuery<T>()
            where T : class, IEntity, new()
        {
            return new IgniteDaoInstance<T>(m_ignite);
        }
    }
}
