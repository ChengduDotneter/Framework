using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Common.DAL;
using Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;

namespace Common.ServiceCommon
{
    /// <summary>
    /// TCC事务日志数据
    /// </summary>
    public class TCCTransaction
    {
        /// <summary>
        /// TCC原始请求IP
        /// </summary>
        public string RequestIP { get; set; }

        /// <summary>
        /// TCC原始请求端口
        /// </summary>
        public int RequestPort { get; set; }
    }

    /// <summary>
    /// TCCController
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="Request"></typeparam>
    [ApiController]
    public abstract class TCCController<T, Request> : ControllerBase
        where T : ViewModelBase, new()
    {
        /// <summary>
        /// Try
        /// </summary>
        /// <param name="tccID"></param>
        /// <param name="timeOut"></param>
        /// <param name="data"></param>
        [HttpPost("try/{tccID:required:long}/{timeOut:required:int}")]
        public abstract Task Try(long tccID, int timeOut, [FromBody] Request data);

        /// <summary>
        /// Cancel
        /// </summary>
        /// <param name="tccID"></param>
        [HttpPost("cancel/{tccID:required:long}")]
        public abstract Task Cancel(long tccID);

        /// <summary>
        /// Commit
        /// </summary>
        /// <param name="tccID"></param>
        [HttpPost("commit/{tccID:required:long}")]
        public abstract Task Commit(long tccID);
    }

    /// <summary>
    /// 事务型TCCController
    /// </summary>
    /// <typeparam name="TLock">开启事务的viewmodel</typeparam>
    /// <typeparam name="Request">请求参数</typeparam>
    public abstract class TransactionTCCController<TLock, Request> : TCCController<TLock, Request>
        where TLock : ViewModelBase, new()
    {
        private const string TRANSACTION_PREFIX = "transaction";
        private string m_typeNameSpace;
        private string m_typeName;
        private IEditQuery<TLock> m_editQuery;
        private IHttpClientFactory m_httpClientFactory;
        private IHttpContextAccessor m_httpContextAccessor;
        private ITccTransactionManager m_tccTransactionManager;
        private readonly static ConnectionMultiplexer m_connectionMultiplexer;
        private readonly static IDatabase m_redisClient;

        static TransactionTCCController()
        {
            ConfigurationOptions configurationOptions = new ConfigurationOptions();
            configurationOptions.EndPoints.Add(ConfigManager.Configuration["RedisEndPoint"]);
            configurationOptions.Password = ConfigManager.Configuration["RedisPassWord"];
            m_connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
            m_redisClient = m_connectionMultiplexer.GetDatabase();
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="editQuery"></param>
        /// <param name="httpClientFactory"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="tccTransactionManager"></param>
        protected TransactionTCCController(
            IEditQuery<TLock> editQuery,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ITccTransactionManager tccTransactionManager)
        {
            m_editQuery = editQuery;
            m_httpClientFactory = httpClientFactory;
            m_httpContextAccessor = httpContextAccessor;
            m_tccTransactionManager = tccTransactionManager;
            m_typeNameSpace = GetType().Namespace;
            m_typeName = GetType().Name;
        }

        /// <summary>
        /// Try
        /// </summary>
        /// <param name="tccID"></param>
        /// <param name="timeOut"></param>
        /// <param name="data"></param>
        public override async Task Try(long tccID, int timeOut, Request data)
        {
            ConnectionInfo connectionInfo = m_httpContextAccessor.HttpContext.Connection;

            await m_redisClient.StringSetAsync(new RedisKey(GetKVKey(m_typeNameSpace, m_typeName, tccID)), JObject.FromObject(new TCCTransaction()
            {
                RequestIP = connectionInfo.LocalIpAddress.ToString(),
                RequestPort = connectionInfo.LocalPort,
            }).ToString(), TimeSpan.FromSeconds(10));

            DAL.ITransaction transaction = await m_editQuery.BeginTransactionAsync();

            try
            {
                object endData = await DoTry(tccID, transaction, data);
                m_tccTransactionManager.AddTransaction(tccID, timeOut, transaction, endData);
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
        protected abstract Task<object> DoTry(long tccID, DAL.ITransaction transaction, Request data);

        /// <summary>
        /// Cancel
        /// </summary>
        /// <param name="tccID"></param>
        public override async Task Cancel(long tccID)
        {
            (bool isLocal, TCCTransaction tccTransaction) = await IsLocalRequest(m_httpContextAccessor, m_typeNameSpace, m_typeName, tccID);

            if (!isLocal && tccTransaction != null)
            {
                RouteValueDictionary routeValues = m_httpContextAccessor.HttpContext.Request.RouteValues;
                await Post(tccTransaction.RequestIP, tccTransaction.RequestPort, $"{routeValues["controller"]}/{ routeValues["action"]}/{routeValues["tccID"]}", m_httpClientFactory, m_httpContextAccessor);
            }
            else if (tccTransaction != null)
            {
                m_tccTransactionManager.Rollback(tccID);
                m_redisClient.KeyDelete(new RedisKey(GetKVKey(m_typeNameSpace, m_typeName, tccID)));
            }
        }

        /// <summary>
        /// Commit
        /// </summary>
        /// <param name="tccID"></param>
        public override async Task Commit(long tccID)
        {
            (bool isLocal, TCCTransaction tccTransaction) = await IsLocalRequest(m_httpContextAccessor, m_typeNameSpace, m_typeName, tccID);

            if (!isLocal && tccTransaction != null)
            {
                RouteValueDictionary routeValues = m_httpContextAccessor.HttpContext.Request.RouteValues;
                await Post(tccTransaction.RequestIP, tccTransaction.RequestPort, $"{routeValues["controller"]}/{ routeValues["action"]}/{routeValues["tccID"]}", m_httpClientFactory, m_httpContextAccessor);
            }
            else if (tccTransaction != null)
            {
                m_tccTransactionManager.Submit(tccID);
                m_redisClient.KeyDelete(new RedisKey(GetKVKey(m_typeNameSpace, m_typeName, tccID)));
            }
            else
            {
                throw new DealException($"未找到ID为：{tccID}的TCC事务。");
            }
        }

        private static async Task<Tuple<bool, TCCTransaction>> IsLocalRequest(IHttpContextAccessor httpContextAccessor, string typeNameSapce, string typeName, long tccID)
        {
            TCCTransaction tccTransaction = null;
            string value = await m_redisClient.StringGetAsync(GetKVKey(typeNameSapce, typeName, tccID));

            if (string.IsNullOrWhiteSpace(value))
                return Tuple.Create(false, tccTransaction);

            ConnectionInfo connectionInfo = httpContextAccessor.HttpContext.Connection;
            tccTransaction = JObject.Parse(value).ToObject<TCCTransaction>();

            return Tuple.Create(tccTransaction.RequestIP == connectionInfo.LocalIpAddress.ToString() && tccTransaction.RequestPort == connectionInfo.LocalPort, tccTransaction);
        }

        private static async Task Post(string ip, int port, string url, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            HttpResponseMessage httpResponseMessage = await HttpJsonHelper.HttpPostByAbsoluteUriAsync(httpClientFactory, $"http://{ip}:{port}/{url}", httpContextAccessor.HttpContext?.Request.Headers["Authorization"]);

            if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                throw new DealException(httpResponseMessage.Content.ReadAsStringAsync().Result);
        }

        private static string GetKVKey(string typeNameSapce, string typeName, long tccID)
        {
            return $"{TRANSACTION_PREFIX}/{typeNameSapce}/{typeName}/{tccID}";
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
        /// <param name="data"></param>
        void AddTransaction(long tccID, int timeOut, DAL.ITransaction transaction, object data);

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

    /// <summary>
    /// TCC通知工厂
    /// </summary>
    public interface ITccNotifyFactory
    {
        /// <summary>
        /// 注册TCC通知
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tccNotify"></param>
        void RegisterNotify<T>(ITccNotify<T> tccNotify);

        /// <summary>
        /// 反注册TCC通知
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tccNotify"></param>
        void UnRegisterNotify<T>(ITccNotify<T> tccNotify);

        /// <summary>
        /// 获取TCC通知实例
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        ITccNotify<T> GetNotify<T>();
    }

    /// <summary>
    /// TCC通知
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITccNotify<T>
    {
        /// <summary>
        /// 通知
        /// </summary>
        /// <param name="tccID"></param>
        /// <param name="successed"></param>
        /// <param name="data"></param>
        void Notify(long tccID, bool successed, T data);
    }

    internal class TccNotifyFactory : ITccNotifyFactory
    {
        private IDictionary<Type, object> m_notifys;

        public ITccNotify<T> GetNotify<T>()
        {
            Type type = typeof(T);

            lock (m_notifys)
            {
                if (m_notifys.ContainsKey(type))
                    return (ITccNotify<T>)m_notifys[type];
                else
                    return null;
            }
        }

        public void RegisterNotify<T>(ITccNotify<T> tccNotify)
        {
            Type type = typeof(T);

            if (!m_notifys.ContainsKey(type))
            {
                lock (m_notifys)
                {
                    if (!m_notifys.ContainsKey(type))
                        m_notifys.Add(type, tccNotify);
                }
            }
        }

        public void UnRegisterNotify<T>(ITccNotify<T> tccNotify)
        {
            Type type = typeof(T);

            lock (m_notifys)
            {
                if (m_notifys.ContainsKey(type) && m_notifys[type] == tccNotify)
                    m_notifys.Remove(type);
            }
        }

        public TccNotifyFactory()
        {
            m_notifys = new Dictionary<Type, object>();
        }
    }

    internal class TccTransactionManager : ITccTransactionManager
    {
        private class TCCTransactionInstance
        {
            public DAL.ITransaction Transaction { get; }
            public int TimeOut { get; }
            public int StartTime { get; }
            public object Data { get; }

            public TCCTransactionInstance(DAL.ITransaction transaction, int timeOut, object data)
            {
                Transaction = transaction;
                TimeOut = timeOut;
                StartTime = Environment.TickCount;
                Data = data;
            }
        }

        private const int THREAD_TIME_SPAN = 100;
        private IDictionary<long, TCCTransactionInstance> m_tccTransactionInstances;
        private ITccNotifyFactory m_tccNotifyFactory;
        private Thread m_thread;

        public TccTransactionManager(ITccNotifyFactory tccNotifyFactory)
        {
            m_tccNotifyFactory = tccNotifyFactory;
            m_tccTransactionInstances = new Dictionary<long, TCCTransactionInstance>();
            m_thread = new Thread(DoClear);
            m_thread.IsBackground = true;
            m_thread.Name = "TCC_TRANSACTION_THREAD";
            m_thread.Start();
        }

        public void AddTransaction(long tccID, int timeOut, DAL.ITransaction transaction, object data)
        {
            lock (m_tccTransactionInstances)
                m_tccTransactionInstances.Add(tccID, new TCCTransactionInstance(transaction, timeOut, data));
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
                    End(tccID, false, tccTransactionInstance.Data);
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
                    End(tccID, true, tccTransactionInstance.Data);
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
                                End(tccIDs[i], false, tccTransactionInstance.Data);
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

        private void End(long tccID, bool successed, object data)
        {
            ThreadPool.QueueUserWorkItem((state) =>
            {
                object tccNotify = typeof(ITccNotifyFactory).GetMethod(nameof(ITccNotifyFactory.GetNotify)).MakeGenericMethod(data.GetType()).Invoke(m_tccNotifyFactory, null);

                if (tccNotify != null)
                    typeof(ITccNotify<>).MakeGenericType(data.GetType()).GetMethod("Notify").Invoke(tccNotify, (object[])state);

            }, new object[] { tccID, successed, data });
        }
    }
}
