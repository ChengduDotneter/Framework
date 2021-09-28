using Common.RPC.TransferAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;

namespace Common.RPC
{
    /// <summary>
    /// RPC处理器基类
    /// </summary>
    public abstract class ProcessorBase
    {
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="serviceClient">RPC服务端</param>
        /// <param name="data">所需发送的数据结构体</param>
        protected void SendData(ServiceClient serviceClient, IRPCData data)
        {
            serviceClient.SendData(IDGenerator.NextID(), data);
        }

        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="serviceClient">RPC服务端</param>
        /// <param name="sessionContext"></param>
        /// <param name="data">所需发送的数据结构体</param>
        protected void SendSessionData(ServiceClient serviceClient, SessionContext sessionContext, IRPCData data)
        {
            serviceClient.SendSessionData(sessionContext, data);
        }
    }

    /// <summary>
    /// RPC接收端处理器基类
    /// </summary>
    /// <typeparam name="TRecieveData">接收的数据结构体泛型</typeparam>
    public abstract class ResponseProcessorBase<TRecieveData> : ProcessorBase, IDisposable
        where TRecieveData : struct, IRPCData
    {
        private ServiceClient[] m_serviceClients;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceClient"></param>
        public ResponseProcessorBase(ServiceClient serviceClient) : this(new ServiceClient[] { serviceClient }) { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceClients"></param>
        public ResponseProcessorBase(ServiceClient[] serviceClients)
        {
            m_serviceClients = serviceClients;

            for (int i = 0; i < m_serviceClients.Length; i++)
                m_serviceClients[i].RegisterProcessor(this);
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            for (int i = 0; i < m_serviceClients.Length; i++)
                m_serviceClients[i].UnRegisterProcessor(this);
        }

        /// <summary>
        /// 数据处理方法
        /// </summary>
        /// <param name="sessionContext">RPC请求上下文</param>
        /// <param name="data">接收的数据</param>
        protected abstract void ProcessData(SessionContext sessionContext, TRecieveData data);
    }

    /// <summary>
    /// RPC请求处理器基类
    /// </summary>
    /// <typeparam name="TSendData">发送的数据结构体泛型</typeparam>
    /// <typeparam name="TRecieveData">接收的数据结构体泛型</typeparam>
    public abstract class RequestProcessorBase<TSendData, TRecieveData> : IDisposable
        where TSendData : struct, IRPCData
        where TRecieveData : struct, IRPCData
    {
        /// <summary>
        /// 任务体
        /// </summary>
        private class TaskBody
        {
            /// <summary>
            /// 连接ID
            /// </summary>
            public long SessionID { get; set; }

            /// <summary>
            /// 回调
            /// </summary>
            public Func<TRecieveData, bool> CallBack { get; }

            /// <summary>
            /// 是否有返回的结果
            /// </summary>
            public bool IsResponses { get; set; }

            /// <summary>
            /// 返回的数据
            /// </summary>
            public object ResponseObject { get; set; }

            public TaskBody(long sessionID, Func<TRecieveData, bool> callBack)
            {
                SessionID = sessionID;
                CallBack = callBack;
            }
        }

        /// <summary>
        /// RPC发送请求处理器基类
        /// </summary>
        private class SendRequestProcessor : ResponseProcessorBase<TRecieveData>
        {
            private Action<SessionContext, TRecieveData> m_recieveDataHandler;
            private ServiceClient m_serviceClient;

            public SendRequestProcessor(ServiceClient serviceClient, Action<SessionContext, TRecieveData> recieveDataHandler) : base(serviceClient)
            {
                m_serviceClient = serviceClient;
                m_recieveDataHandler = recieveDataHandler;
            }

            /// <summary>
            /// 数据处理方法
            /// </summary>
            /// <param name="sessionContext">链接上下文</param>
            /// <param name="data">接收的数据</param>
            protected override void ProcessData(SessionContext sessionContext, TRecieveData data)
            {
                m_recieveDataHandler(sessionContext, data);
            }

            /// <summary>
            /// 发送数据
            /// </summary>
            /// <param name="sessionID">链接上下文</param>
            /// <param name="data">接收的数据</param>
            public void SendSessionData(long sessionID, TSendData data)
            {
                SendSessionData(m_serviceClient, new SessionContext(sessionID), data);
            }
        }

        private const int TASK_WAIT_TIME_SPAN = 1;
        private int m_requestTimeout;
        private IDictionary<int, SendRequestProcessor> m_sendProcessors;
        private IDictionary<long, TaskBody> m_taskWaits;
        private AsyncLock m_sendProcessorLock;
        private AsyncLock m_taskWaitLock;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="requestTimeout"></param>
        public RequestProcessorBase(int requestTimeout)
        {
            m_sendProcessorLock = new AsyncLock();
            m_taskWaitLock = new AsyncLock();
            m_requestTimeout = requestTimeout;
            m_taskWaits = new Dictionary<long, TaskBody>();
            m_sendProcessors = new Dictionary<int, SendRequestProcessor>();
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="sessionContext">链接上下文</param>
        /// <param name="data">接收的数据</param>
        private void ProcessData(SessionContext sessionContext, TRecieveData data)
        {
            TaskBody taskBody;

            using (m_taskWaitLock.Lock())
            {
                if (!m_taskWaits.ContainsKey(sessionContext.SessionID) || !m_taskWaits.TryGetValue(sessionContext.SessionID, out taskBody))
                    throw new Exception($"接收数据异常，异常会话ID: {sessionContext.SessionID}");
            }

            taskBody.ResponseObject = data;
            taskBody.IsResponses = true;
        }

        /// <summary>
        /// 等待
        /// </summary>
        /// <param name="state"></param>
        private void Wait(object state)
        {
            TaskBody taskBody = (TaskBody)((object[])state)[0];
            CancellationToken token = ((CancellationTokenSource)((object[])state)[1]).Token;

            while (!token.IsCancellationRequested && !taskBody.IsResponses)
                Thread.Sleep(TASK_WAIT_TIME_SPAN);
        }

        /// <summary>
        /// 回调
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private bool Callback(Task task)
        {
            CancellationTokenSource cancellationTokenSource = (CancellationTokenSource)((object[])task.AsyncState)[1];
            bool canceled = cancellationTokenSource.Token.IsCancellationRequested;
            TaskBody taskBody = (TaskBody)((object[])task.AsyncState)[0];
            cancellationTokenSource.Dispose();

            using (m_taskWaitLock.Lock())
                m_taskWaits.Remove(taskBody.SessionID);

            if (canceled)
                return false;

            return taskBody.CallBack((TRecieveData)taskBody.ResponseObject);
        }

        /// <summary>
        /// 异步请求
        /// </summary>
        /// <param name="serviceClient">RPC服务客户端</param>
        /// <param name="sendData">发送的数据结构体</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        protected async Task<bool> RequestAsync(ServiceClient serviceClient, TSendData sendData, Func<TRecieveData, bool> callback)
        {
            using (await m_sendProcessorLock.LockAsync())
            {
                if (!m_sendProcessors.ContainsKey(serviceClient.GetHashCode()))
                    m_sendProcessors.Add(serviceClient.GetHashCode(), new SendRequestProcessor(serviceClient, ProcessData));
            }

            long sessionID = IDGenerator.NextID();
            TaskBody taskBody = new TaskBody(sessionID, callback);

            using (await m_taskWaitLock.LockAsync())
                m_taskWaits.Add(sessionID, taskBody);

            using (await m_sendProcessorLock.LockAsync())
                m_sendProcessors[serviceClient.GetHashCode()].SendSessionData(sessionID, sendData);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(m_requestTimeout);

            return await Task.Factory.StartNew(Wait, new object[] { taskBody, cancellationTokenSource }, cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Current).
                              ContinueWith(Callback);
        }

        /// <summary>
        /// 同步请求
        /// </summary>
        /// <param name="serviceClient">RPC服务客户端</param>
        /// <param name="sendData">发送的数据结构体</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        protected bool Request(ServiceClient serviceClient, TSendData sendData, Func<TRecieveData, bool> callback)
        {
            using (m_sendProcessorLock.Lock())
            {
                if (!m_sendProcessors.ContainsKey(serviceClient.GetHashCode()))
                    m_sendProcessors.Add(serviceClient.GetHashCode(), new SendRequestProcessor(serviceClient, ProcessData));
            }

            long sessionID = IDGenerator.NextID();
            TaskBody taskBody = new TaskBody(sessionID, callback);

            using (m_taskWaitLock.Lock())
                m_taskWaits.Add(sessionID, taskBody);

            using (m_sendProcessorLock.Lock())
                m_sendProcessors[serviceClient.GetHashCode()].SendSessionData(sessionID, sendData);

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(m_requestTimeout);

            while (!cancellationTokenSource.Token.IsCancellationRequested && !taskBody.IsResponses)
                Thread.Sleep(TASK_WAIT_TIME_SPAN);

            using (m_taskWaitLock.Lock())
                m_taskWaits.Remove(taskBody.SessionID);

            if (cancellationTokenSource.Token.IsCancellationRequested)
                return false;

            return callback.Invoke((TRecieveData)taskBody.ResponseObject);
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            int[] keys = m_sendProcessors.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
                if (m_sendProcessors.TryGetValue(keys[i], out SendRequestProcessor sendRequestProcessor))
                    sendRequestProcessor.Dispose();
        }
    }

    /// <summary>
    /// RPC一推多请求处理器基类
    /// </summary>
    /// <typeparam name="TSendData">发送的数据结构体泛型</typeparam>
    /// <typeparam name="TRecieveData">接收的数据结构体泛型</typeparam>
    public abstract class MultipleRequestProcessorBase<TSendData, TRecieveData> : RequestProcessorBase<TSendData, TRecieveData>
        where TSendData : struct, IRPCData
        where TRecieveData : struct, IRPCData
    {
        private ServiceClient[] m_serviceClients;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceClients"></param>
        /// <param name="requestTimeout"></param>
        public MultipleRequestProcessorBase(ServiceClient[] serviceClients, int requestTimeout) : base(requestTimeout)
        {
            m_serviceClients = serviceClients;
        }

        /// <summary>
        /// 异步请求
        /// </summary>
        /// <param name="sendData">发送的数据</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        protected Task<bool> RequestAsync(TSendData sendData, Func<TRecieveData, bool> callback)
        {
            Task<bool>[] tasks = new Task<bool>[m_serviceClients.Length];

            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = RequestAsync(m_serviceClients[i], sendData, callback);

            return Task.Factory.ContinueWhenAll(tasks, ProcessDataAsync);
        }

        /// <summary>
        /// 同步请求
        /// </summary>
        /// <param name="sendData">发送的数据</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        protected bool Request(TSendData sendData, Func<TRecieveData, bool> callback)
        {
            bool[] results = new bool[m_serviceClients.Length];

            for (int i = 0; i < results.Length; i++)
                results[i] = Request(m_serviceClients[i], sendData, callback);

            return ProcessData(results);
        }

        private bool ProcessDataAsync(Task<bool>[] tasks)
        {
            for (int i = 0; i < tasks.Length; i++)
                if (!tasks[i].Result)
                    return false;

            return true;
        }

        private bool ProcessData(bool[] results)
        {
            for (int i = 0; i < results.Length; i++)
                if (!results[i])
                    return false;

            return true;
        }
    }

    /// <summary>
    /// RPC分区请求处理器基类
    /// </summary>
    /// <typeparam name="TSendData"></typeparam>
    /// <typeparam name="TRecieveData"></typeparam>
    public abstract class PartitionRequestProcessorBase<TSendData, TRecieveData> : RequestProcessorBase<TSendData, TRecieveData>
        where TSendData : struct, IRPCData
        where TRecieveData : struct, IRPCData
    {
        private ServiceClient[] m_serviceClients;
        private long m_requestIndex;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="serviceClients"></param>
        /// <param name="requestTimeout"></param>
        public PartitionRequestProcessorBase(ServiceClient[] serviceClients, int requestTimeout) : base(requestTimeout)
        {
            m_serviceClients = serviceClients;
        }

        /// <summary>
        /// 异步请求
        /// </summary>
        /// <param name="sendData">发送的数据结构体</param>
        /// <param name="callback">回调</param>
        /// <param name="serviceClient">RPC服务客户端</param>
        /// <returns></returns>
        protected Task<bool> RequestAsync(TSendData sendData, Func<TRecieveData, bool> callback, out ServiceClient serviceClient)
        {
            int serviceIndex = (int)(m_requestIndex++ % m_serviceClients.Length);
            serviceClient = m_serviceClients[serviceIndex];
            return RequestAsync(serviceClient, sendData, callback);
        }

        /// <summary>
        /// 同步请求
        /// </summary>
        /// <param name="sendData">发送的数据结构体</param>
        /// <param name="callback">回调</param>
        /// <param name="serviceClient">RPC服务客户端</param>
        /// <returns></returns>
        protected bool Request(TSendData sendData, Func<TRecieveData, bool> callback, out ServiceClient serviceClient)
        {
            int serviceIndex = (int)(m_requestIndex++ % m_serviceClients.Length);
            serviceClient = m_serviceClients[serviceIndex];
            return Request(serviceClient, sendData, callback);
        }
    }
}