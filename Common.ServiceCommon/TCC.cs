﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Apache.Ignite.Core.Cache.Configuration;
using Common.DAL;
using Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SqlSugar;

namespace Common.ServiceCommon
{
    /// <summary>
    /// TCC事务日志数据
    /// </summary>
    public class TCCTransaction : ViewModelBase
    {
        /// <summary>
        /// TCC超时
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "TCC请求超时时间")]
        [QuerySqlField(NotNull = true)]
        public int TimeOut { get; set; }

        /// <summary>
        /// TCC取消时间
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "TCC请求失效时间")]
        [QuerySqlField(NotNull = true)]
        public DateTime CancelTime { get; set; }

        /// <summary>
        /// TCC原始请求IP
        /// </summary>
        [SugarColumn(IsNullable = false, Length = 255, ColumnDescription = "TCC请求IP")]
        [QuerySqlField(NotNull = true)]
        public string RequestIP { get; set; }

        /// <summary>
        /// TCC原始请求端口
        /// </summary>
        [SugarColumn(IsNullable = false, ColumnDescription = "TCC请求端口")]
        [QuerySqlField(NotNull = true)]
        public int RequestPort { get; set; }

        /// <summary>
        /// TCC取消URL
        /// </summary>
        [SugarColumn(IsNullable = false, Length = 255, ColumnDescription = "TCC请求CancelURL")]
        [QuerySqlField(NotNull = true)]
        public string RequestCancelUrl { get; set; }

        /// <summary>
        /// TCC提交URL
        /// </summary>
        [SugarColumn(IsNullable = false, Length = 255, ColumnDescription = "TCC请求CommitURL")]
        [QuerySqlField(NotNull = true)]
        public string RequestCommitlUrl { get; set; }
    }

    /// <summary>
    /// TCCController
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [ApiController]
    public abstract class TCCController<T> : ControllerBase
        where T : ViewModelBase, new()
    {
        /// <summary>
        /// Try
        /// </summary>
        /// <param name="tccID"></param>
        /// <param name="timeOut"></param>
        /// <param name="requestCancelUrl"></param>
        /// <param name="requestCommitUrl"></param>
        /// <param name="data"></param>
        [HttpPost("try/{tccID:required:long}/{timeOut:required:int}/{requestCancelUrl:required}/{requestCommitUrl:required}")]
        public abstract void Try(long tccID, int timeOut, string requestCancelUrl, string requestCommitUrl, [FromBody] T data);

        /// <summary>
        /// Cancel
        /// </summary>
        /// <param name="tccID"></param>
        [HttpPost("cancel/{tccID:required:long}")]
        public abstract void Cancel(long tccID);

        /// <summary>
        /// Commit
        /// </summary>
        /// <param name="tccID"></param>
        [HttpPost("commit/{tccID:required:long}")]
        public abstract void Commit(long tccID);
    }

    /// <summary>
    /// 事务型TCCController
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class TransactionTCCController<T> : TCCController<T>
        where T : ViewModelBase, new()
    {
        private ISearchQuery<TCCTransaction> m_searchQuery;
        private IEditQuery<TCCTransaction> m_editQuery;
        private IHttpContextAccessor m_httpContextAccessor;
        private ISSOUserService m_ssoUserService;
        private ITccTransactionManager m_tccTransactionManager;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="searchQuery"></param>
        /// <param name="editQuery"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="ssoUserService"></param>
        /// <param name="tccTransactionManager"></param>
        protected TransactionTCCController(
            ISearchQuery<TCCTransaction> searchQuery,
            IEditQuery<TCCTransaction> editQuery,
            IHttpContextAccessor httpContextAccessor,
            ISSOUserService ssoUserService,
            ITccTransactionManager tccTransactionManager)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
            m_httpContextAccessor = httpContextAccessor;
            m_ssoUserService = ssoUserService;
            m_tccTransactionManager = tccTransactionManager;
        }

        /// <summary>
        /// Try
        /// </summary>
        /// <param name="tccID"></param>
        /// <param name="timeOut"></param>
        /// <param name="requestCancelUrl"></param>
        /// <param name="requestCommitUrl"></param>
        /// <param name="data"></param>
        public override void Try(long tccID, int timeOut, string requestCancelUrl, string requestCommitUrl, T data)
        {
            ConnectionInfo connectionInfo = m_httpContextAccessor.HttpContext.Connection;

            m_editQuery.Insert(datas: new TCCTransaction()
            {
                ID = tccID,
                CreateTime = DateTime.Now,
                CreateUserID = m_ssoUserService.GetUser().ID,
                TimeOut = timeOut,
                RequestIP = connectionInfo.LocalIpAddress.ToString(),
                RequestPort = connectionInfo.LocalPort,
                CancelTime = DateTime.Now.AddMilliseconds(timeOut),
                RequestCancelUrl = WebUtility.UrlDecode(requestCancelUrl),
                RequestCommitlUrl = WebUtility.UrlDecode(requestCommitUrl)
            });

            ITransaction transaction = m_editQuery.BeginTransaction();

            try
            {
                DoTry(tccID, transaction, data);
                m_tccTransactionManager.AddTransaction(tccID, timeOut, transaction);
            }
            catch (Exception exception)
            {
                transaction.Rollback();
                transaction.Dispose();
                throw new DealException("TCC事务异常。", exception);
            }
        }

        /// <summary>
        /// Do实现
        /// </summary>
        /// <param name="tccID"></param>
        /// <param name="transaction"></param>
        /// <param name="data"></param>
        protected abstract void DoTry(long tccID, ITransaction transaction, T data);

        /// <summary>
        /// Cancel
        /// </summary>
        /// <param name="tccID"></param>
        public override void Cancel(long tccID)
        {
            if (!IsLocalRequest(m_httpContextAccessor, m_searchQuery, m_editQuery, tccID, out TCCTransaction tccTransaction) && tccTransaction != null)
            {
                RouteValueDictionary routeValues = m_httpContextAccessor.HttpContext.Request.RouteValues;
                Post(tccTransaction.RequestIP, tccTransaction.RequestPort, $"{routeValues["controller"]}/{ routeValues["action"]}/{routeValues["tccID"]}", m_httpContextAccessor);
            }
            else if (tccTransaction != null)
            {
                m_tccTransactionManager.Rollback(tccID);
            }
        }

        /// <summary>
        /// Commit
        /// </summary>
        /// <param name="tccID"></param>
        public override void Commit(long tccID)
        {
            if (!IsLocalRequest(m_httpContextAccessor, m_searchQuery, m_editQuery, tccID, out TCCTransaction tccTransaction) && tccTransaction != null)
            {
                RouteValueDictionary routeValues = m_httpContextAccessor.HttpContext.Request.RouteValues;
                Post(tccTransaction.RequestIP, tccTransaction.RequestPort, $"{routeValues["controller"]}/{ routeValues["action"]}/{routeValues["tccID"]}", m_httpContextAccessor);
            }
            else if (tccTransaction != null)
            {
                m_tccTransactionManager.Submit(tccID);
            }
            else
            {
                throw new DealException($"未找到ID为：{tccID}的TCC事务。");
            }
        }

        private static bool IsLocalRequest(IHttpContextAccessor httpContextAccessor, ISearchQuery<TCCTransaction> searchQuery, IEditQuery<TCCTransaction> editQuery, long tccID, out TCCTransaction tccTransaction)
        {
            ConnectionInfo connectionInfo = httpContextAccessor.HttpContext.Connection;
            tccTransaction = searchQuery.Get(tccID);

            if (tccTransaction != null)
                return tccTransaction.RequestIP == connectionInfo.LocalIpAddress.ToString() && tccTransaction.RequestPort == connectionInfo.LocalPort;

            return false;
        }

        private static void Post(string ip, int port, string url, IHttpContextAccessor httpContextAccessor)
        {
            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPostByAbsoluteUri($"http://{ip}:{port}/{url}");

            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                throw new DealException(httpResponseMessage.Content.ReadAsStringAsync().Result);
        }
    }

    /// <summary>
    /// 事务型TCC管理
    /// </summary>
    public interface ITccTransactionManager
    {
        /// <summary>
        /// 添加TCC事务
        /// </summary>
        /// <param name="tccID"></param>
        /// <param name="timeOut"></param>
        /// <param name="transaction"></param>
        void AddTransaction(long tccID, int timeOut, ITransaction transaction);

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <param name="tccID"></param>
        void Submit(long tccID);

        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <param name="tccID"></param>
        void Rollback(long tccID);
    }

    internal class TccTransactionManager : ITccTransactionManager
    {
        private class TCCTransactionInstance
        {
            public ITransaction Transaction { get; }
            public int TimeOut { get; }
            public int StartTime { get; }

            public TCCTransactionInstance(ITransaction transaction, int timeOut)
            {
                Transaction = transaction;
                TimeOut = timeOut;
                StartTime = Environment.TickCount;
            }
        }

        private const int THREAD_TIME_SPAN = 100;
        private IDictionary<long, TCCTransactionInstance> m_tccTransactionInstances;
        private Thread m_thread;

        public TccTransactionManager()
        {
            m_tccTransactionInstances = new Dictionary<long, TCCTransactionInstance>();
            m_thread = new Thread(DoClear);
            m_thread.IsBackground = true;
            m_thread.Name = "TCC_TRANSACTION_THREAD";
            m_thread.Start();
        }

        public void AddTransaction(long tccID, int timeOut, ITransaction transaction)
        {
            lock (m_tccTransactionInstances)
                m_tccTransactionInstances.Add(tccID, new TCCTransactionInstance(transaction, timeOut));
        }

        public void Rollback(long tccID)
        {
            lock (m_tccTransactionInstances)
            {
                if (!m_tccTransactionInstances.ContainsKey(tccID))
                    return;
            }

            TCCTransactionInstance tccTransactionInstance;

            lock (m_tccTransactionInstances)
                tccTransactionInstance = m_tccTransactionInstances[tccID];

            lock (tccTransactionInstance)
            {
                try
                {
                    tccTransactionInstance.Transaction.Rollback();
                }
                finally
                {
                    tccTransactionInstance.Transaction.Dispose();

                    lock (m_tccTransactionInstances)
                        m_tccTransactionInstances.Remove(tccID);
                }
            }
        }

        public void Submit(long tccID)
        {
            lock (m_tccTransactionInstances)
            {
                if (!m_tccTransactionInstances.ContainsKey(tccID))
                    throw new DealException($"未找到ID为：{tccID}的TCC事务。");
            }

            TCCTransactionInstance tccTransactionInstance;

            lock (m_tccTransactionInstances)
                tccTransactionInstance = m_tccTransactionInstances[tccID];

            lock (tccTransactionInstance)
            {
                try
                {
                    tccTransactionInstance.Transaction.Submit();
                }
                finally
                {
                    tccTransactionInstance.Transaction.Dispose();

                    lock (m_tccTransactionInstances)
                        m_tccTransactionInstances.Remove(tccID);
                }
            }
        }

        private void DoClear()
        {
            while (true)
            {
                long[] tccIDs;

                lock (m_tccTransactionInstances)
                    tccIDs = m_tccTransactionInstances.Keys.ToArray();

                for (int i = 0; i < tccIDs.Length; i++)
                {
                    TCCTransactionInstance tccTransactionInstance;

                    lock (m_tccTransactionInstances)
                    {
                        if (m_tccTransactionInstances.ContainsKey(tccIDs[i]))
                            tccTransactionInstance = m_tccTransactionInstances[tccIDs[i]];
                        else
                            continue;
                    }

                    lock (tccTransactionInstance)
                    {
                        if (Environment.TickCount - tccTransactionInstance.StartTime > tccTransactionInstance.TimeOut)
                        {
                            try
                            {
                                tccTransactionInstance.Transaction.Rollback();
                            }
                            finally
                            {
                                tccTransactionInstance.Transaction.Dispose();

                                lock (m_tccTransactionInstances)
                                    m_tccTransactionInstances.Remove(tccIDs[i]);
                            }
                        }
                    }
                }

                Thread.Sleep(THREAD_TIME_SPAN);
            }
        }
    }
}