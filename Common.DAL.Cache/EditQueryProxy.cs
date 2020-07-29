﻿using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq.Expressions;

namespace Common.DAL.Cache
{
    /// <summary>
    /// 数据修改代理类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EditQueryProxy<T> : IEditQuery<T>
         where T : class, IEntity, new()
    {
        private IEditQuery<T> m_editQuery;
        private MemoryCache m_keyMemoryCache;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="editQuery"></param>
        public EditQueryProxy(IEditQuery<T> editQuery)
        {
            m_editQuery = editQuery;
            m_keyMemoryCache = CacheFactory<T>.GetKeyMemoryCache();
        }

        /// <summary>
        /// 开启事务
        /// </summary>
        /// <returns></returns>
        public ITransaction BeginTransaction(int weight = 0)
        {
            return new TransactionProxy(m_editQuery.BeginTransaction(weight));
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="ids"></param>
        public void Delete(ITransaction transaction = null, params long[] ids)
        {
            m_editQuery.Delete(transaction, ids);

            DoAction(() =>
            {
                for (int i = 0; i < ids.Length; i++)
                    m_keyMemoryCache.Remove(ids[i]);

                CacheFactory<T>.ClearConditionMemoryCache();
            });
        }

        /// <summary>
        /// 插入
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Insert(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Insert(transaction, datas);
            DoAction(CacheFactory<T>.ClearConditionMemoryCache);
        }

        /// <summary>
        /// 合并
        /// </summary>
        /// <param name="transaction"></param>
        /// <param name="datas"></param>
        public void Merge(ITransaction transaction = null, params T[] datas)
        {
            m_editQuery.Merge(transaction, datas);

            DoAction(() =>
            {
                for (int i = 0; i < datas.Length; i++)
                    if (m_keyMemoryCache.TryGetValue(datas[i], out T _))
                        m_keyMemoryCache.Set(datas[i].ID, datas[i]);

                CacheFactory<T>.ClearConditionMemoryCache();
            });
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="data"></param>
        /// <param name="transaction"></param>
        /// <param name="IgnoreColumns"></param>
        public void Update(T data, ITransaction transaction = null, params string[] IgnoreColumns)
        {
            m_editQuery.Update(data, transaction, IgnoreColumns);

            DoAction(() =>
            {
                if (m_keyMemoryCache.TryGetValue(data.ID, out T _))
                    m_keyMemoryCache.Set(data.ID, data);

                CacheFactory<T>.ClearConditionMemoryCache();
            });
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="updateExpression"></param>
        /// <param name="transaction"></param>
        public void Update(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> updateExpression, ITransaction transaction = null)
        {
            m_editQuery.Update(predicate, updateExpression, transaction);

            DoAction(() =>
            {
                CacheFactory<T>.ClearKeyMemoryCache();
                CacheFactory<T>.ClearConditionMemoryCache();
            });
        }

        private static void DoAction(Action action)
        {
            if (TransactionProxy.InTransaction)
                TransactionProxy.Instance.AddAction(action);
            else
                action.Invoke();
        }
    }
}