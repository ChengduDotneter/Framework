using Common.RPC;
using Common.RPC.TransferAdapter;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.RPC
{
    public abstract class ProcessorBase
    {
        protected void SendData(ServiceClient serviceClient, IRPCData data)
        {
            serviceClient.SendData(IDGenerator.NextID(), data);
        }

        protected void SendSessionData(ServiceClient serviceClient, SessionContext sessionContext, IRPCData data)
        {
            serviceClient.SendSessionData(sessionContext, data);
        }
    }

    public abstract class ResponseProcessorBase<TRecieveData> : ProcessorBase, IDisposable
        where TRecieveData : struct, IRPCData
    {
        private ServiceClient[] m_serviceClients;

        public ResponseProcessorBase(ServiceClient serviceClient) : this(new ServiceClient[] { serviceClient }) { }

        public ResponseProcessorBase(ServiceClient[] serviceClients)
        {
            m_serviceClients = serviceClients;

            for (int i = 0; i < m_serviceClients.Length; i++)
                m_serviceClients[i].RegisterProcessor(this);
        }

        public void Dispose()
        {
            for (int i = 0; i < m_serviceClients.Length; i++)
                m_serviceClients[i].UnRegisterProcessor(this);
        }

        protected abstract void ProcessData(SessionContext sessionContext, TRecieveData data);
    }

    public abstract class RequestProcessorBase<TSendData, TRecieveData> : IDisposable
        where TSendData : struct, IRPCData
        where TRecieveData : struct, IRPCData
    {
        private class TaskBody
        {
            public long SessionID { get; set; }
            public Func<TRecieveData, bool> CallBack { get; }
            public bool IsResponses { get; set; }
            public object ResponseObject { get; set; }

            public TaskBody(long sessionID, Func<TRecieveData, bool> callBack)
            {
                SessionID = sessionID;
                CallBack = callBack;
            }
        }

        private class SendRequestProcessor : ResponseProcessorBase<TRecieveData>
        {
            private Action<SessionContext, TRecieveData> m_recieveDataHandler;
            private ServiceClient m_serviceClient;

            public SendRequestProcessor(ServiceClient serviceClient, Action<SessionContext, TRecieveData> recieveDataHandler) : base(serviceClient)
            {
                m_serviceClient = serviceClient;
                m_recieveDataHandler = recieveDataHandler;
            }

            protected override void ProcessData(SessionContext sessionContext, TRecieveData data)
            {
                m_recieveDataHandler(sessionContext, data);
            }

            public void SendSessionData(long sessionID, TSendData data)
            {
                base.SendSessionData(m_serviceClient, new SessionContext(sessionID), data);
            }
        }

        private readonly static TimeSpan TASK_WAIT_TIME_SPAN = TimeSpan.FromMilliseconds(0.01);
        private int m_requestTimeout;
        private ConcurrentDictionary<int, SendRequestProcessor> m_sendProcessors;
        private ConcurrentDictionary<long, TaskBody> m_taskWaits;

        public RequestProcessorBase(int requestTimeout)
        {
            m_requestTimeout = requestTimeout;
            m_taskWaits = new ConcurrentDictionary<long, TaskBody>();
            m_sendProcessors = new ConcurrentDictionary<int, SendRequestProcessor>();
        }

        private void ProcessData(SessionContext sessionContext, TRecieveData data)
        {
            if (!m_taskWaits.ContainsKey(sessionContext.SessionID) || !m_taskWaits.TryGetValue(sessionContext.SessionID, out TaskBody taskBody))
                return;

            taskBody.ResponseObject = data;
            taskBody.IsResponses = true;
        }

        private void Wait(object state)
        {
            TaskBody taskBody = (TaskBody)((object[])state)[0];
            CancellationToken token = ((CancellationTokenSource)((object[])state)[1]).Token;

            int time = Environment.TickCount;

            while (!token.IsCancellationRequested && !taskBody.IsResponses)
                Thread.Sleep(TASK_WAIT_TIME_SPAN);
        }

        private bool Callback(Task task)
        {
            CancellationTokenSource cancellationTokenSource = (CancellationTokenSource)((object[])task.AsyncState)[1];
            bool canceled = cancellationTokenSource.Token.IsCancellationRequested;
            TaskBody taskBody = (TaskBody)((object[])task.AsyncState)[0];
            cancellationTokenSource.Dispose();

            m_taskWaits.TryRemove(taskBody.SessionID, out TaskBody removeTaskBody);

            if (canceled)
                return false;

            return taskBody.CallBack((TRecieveData)taskBody.ResponseObject);
        }

        protected Task<bool> Request(ServiceClient serviceClient, TSendData sendData, Func<TRecieveData, bool> callback)
        {
            if (!m_sendProcessors.ContainsKey(serviceClient.GetHashCode()))
                m_sendProcessors.TryAdd(serviceClient.GetHashCode(), new SendRequestProcessor(serviceClient, ProcessData));

            long sessionID = IDGenerator.NextID();
            m_sendProcessors[serviceClient.GetHashCode()].SendSessionData(sessionID, sendData);
            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(m_requestTimeout);

            TaskBody taskBody = new TaskBody(sessionID, callback);
            m_taskWaits.TryAdd(sessionID, taskBody);
            return Task.Factory.StartNew(Wait, new object[] { taskBody, cancellationTokenSource }, cancellationTokenSource.Token).ContinueWith(Callback);
        }

        public void Dispose()
        {
            int[] keys = m_sendProcessors.Keys.ToArray();

            for (int i = 0; i < keys.Length; i++)
                if (m_sendProcessors.TryGetValue(keys[i], out SendRequestProcessor sendRequestProcessor))
                    sendRequestProcessor.Dispose();
        }
    }

    public abstract class MultipleRequestProcessorBase<TSendData, TRecieveData> : RequestProcessorBase<TSendData, TRecieveData>
        where TSendData : struct, IRPCData
        where TRecieveData : struct, IRPCData
    {
        private ServiceClient[] m_serviceClients;

        public MultipleRequestProcessorBase(ServiceClient[] serviceClients, int requestTimeout) : base(requestTimeout)
        {
            m_serviceClients = serviceClients;
        }

        protected Task<bool> Request(TSendData sendData, Func<TRecieveData, bool> callback)
        {
            Task<bool>[] tasks = new Task<bool>[m_serviceClients.Length];

            for (int i = 0; i < tasks.Length; i++)
                tasks[i] = Request(m_serviceClients[i], sendData, callback);

            return Task.Factory.ContinueWhenAll(tasks, ProcessData);
        }

        private bool ProcessData(Task<bool>[] tasks)
        {
            for (int i = 0; i < tasks.Length; i++)
                if (!tasks[i].Result)
                    return false;

            return true;
        }
    }

    public abstract class PartitionRequestProcessorBase<TSendData, TRecieveData> : RequestProcessorBase<TSendData, TRecieveData>
        where TSendData : struct, IRPCData
        where TRecieveData : struct, IRPCData
    {
        private ServiceClient[] m_serviceClients;
        private long m_requestIndex;

        public PartitionRequestProcessorBase(ServiceClient[] serviceClients, int requestTimeout) : base(requestTimeout)
        {
            m_serviceClients = serviceClients;
        }

        protected Task<bool> Request(TSendData sendData, Func<TRecieveData, bool> callback, out ServiceClient serviceClient)
        {
            int serviceIndex = (int)(m_requestIndex++ % m_serviceClients.Length);
            serviceClient = m_serviceClients[serviceIndex];
            return Request(serviceClient, sendData, callback);
        }
    }
}
