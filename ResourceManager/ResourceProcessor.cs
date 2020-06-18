using Common;
using Common.DAL.Transaction;
using Common.RPC;
using Common.RPC.TransferAdapter;
using Microsoft.Extensions.Hosting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ResourceManager
{
    /// <summary>
    /// 申请资源处理器，服务发起端（现包括ZeroMQ和UDP）
    /// </summary>
    internal class ApplyResourceProcessor : ResponseProcessorBase<ApplyRequestData>, IHostedService
    {
        private const int MAX_TIME_OUT = 1000 * 60;
        private ServiceClient m_serviceClient;
        private IDeadlockDetection m_deadlockDetection;
        private IDictionary<long, IDictionary<string, SessionContext>> m_sessionContexts;

        public ApplyResourceProcessor(ServiceClient serviceClient, IDeadlockDetection deadlockDetection) : base(serviceClient)
        {
            m_serviceClient = serviceClient;
            m_deadlockDetection = deadlockDetection;
            m_sessionContexts = new Dictionary<long, IDictionary<string, SessionContext>>();
            m_deadlockDetection.ApplyResponsed += ApplyResponsed;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="sessionContext"></param>
        /// <param name="data"></param>
        protected override void ProcessData(SessionContext sessionContext, ApplyRequestData data)
        {
            if (data.TimeOut < 0 ||
                data.TimeOut > MAX_TIME_OUT)
                throw new DealException($"超时时间范围为：{0}-{MAX_TIME_OUT}ms");

            m_deadlockDetection.ApplyRequest(data.Identity, data.ResourceName, data.Weight, data.TimeOut);

            lock (m_sessionContexts)
            {
                if (!m_sessionContexts.ContainsKey(data.Identity))
                    m_sessionContexts.Add(data.Identity, new Dictionary<string, SessionContext>());

                if (!m_sessionContexts[data.Identity].ContainsKey(data.ResourceName))
                    m_sessionContexts[data.Identity].Add(data.ResourceName, sessionContext);
            }
        }

        private void ApplyResponsed(long identity, string resourceName, bool successed)
        {
            lock (m_sessionContexts)
            {
                if (!m_sessionContexts.ContainsKey(identity) || !m_sessionContexts[identity].ContainsKey(resourceName))
                    return;

                SendSessionData(m_serviceClient, m_sessionContexts[identity][resourceName], new ApplyResponseData() { Success = successed });

                m_sessionContexts[identity].Remove(resourceName);

                if (m_sessionContexts[identity].Count == 0)
                    m_sessionContexts.Remove(identity);
            }
        }
    }

    /// <summary>
    /// 释放资源处理器，服务发起端（现包括ZeroMQ和UDP）
    /// </summary>
    internal class ReleaseResourceProcessor : ResponseProcessorBase<ReleaseRequestData>, IHostedService
    {
        private ServiceClient m_serviceClient;
        private IDeadlockDetection m_deadlockDetection;

        public ReleaseResourceProcessor(ServiceClient serviceClient, IDeadlockDetection deadlockDetection) : base(serviceClient)
        {
            m_serviceClient = serviceClient;
            m_deadlockDetection = deadlockDetection;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// 数据处理
        /// </summary>
        /// <param name="sessionContext"></param>
        /// <param name="data"></param>
        protected override void ProcessData(SessionContext sessionContext, ReleaseRequestData data)
        {
            m_deadlockDetection.RemoveTranResource(data.Identity);
            SendSessionData(m_serviceClient, sessionContext, new ReleaseResponseData());
        }
    }
}