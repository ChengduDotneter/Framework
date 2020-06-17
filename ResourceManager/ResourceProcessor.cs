using Common;
using Common.DAL.Transaction;
using Common.RPC;
using Common.RPC.TransferAdapter;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ResourceManager
{
    /// <summary>
    /// 申请资源处理器，服务发起端（现包括ZeroMQ和UDP）
    /// </summary>
     class ApplyResourceProcessor : ResponseProcessorBase<ApplyRequestData>, IHostedService
    {
        private const int MAX_TIME_OUT = 1000 * 60;
        private ServiceClient m_serviceClient;
        private IResourceManager m_resourceManager;

        public ApplyResourceProcessor(ServiceClient serviceClient, IResourceManager resourceManager) : base(serviceClient)
        {
            m_serviceClient = serviceClient;
            m_resourceManager = resourceManager;
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
        protected override async void ProcessData(SessionContext sessionContext, ApplyRequestData data)
        {
            if (data.TimeOut < 0 ||
                data.TimeOut > MAX_TIME_OUT)
                throw new DealException($"超时时间范围为：{0}-{MAX_TIME_OUT}ms");

            bool successed = await m_resourceManager.GetResource(data.ResourceName).Apply(data.Identity, data.Weight, data.TimeOut);
            SendSessionData(m_serviceClient, sessionContext, new ApplyResponseData() { Success = successed });

            //SendSessionData(m_serviceClient, sessionContext, new ApplyResponseData() { Success = true });
        }
    }

    /// <summary>
    /// 释放资源处理器，服务发起端（现包括ZeroMQ和UDP）
    /// </summary>
     class ReleaseResourceProcessor : ResponseProcessorBase<ReleaseRequestData>, IHostedService
    {
        private ServiceClient m_serviceClient;

        public ReleaseResourceProcessor(ServiceClient serviceClient) : base(serviceClient)
        {
            m_serviceClient = serviceClient;
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
        protected override async void ProcessData(SessionContext sessionContext, ReleaseRequestData data)
        {
            await DeadLockDetection.EnQueued(new EnQueueData() { Identity = data.Identity, QueueDataType = QueueDataTypeEnum.Release });
            SendSessionData(m_serviceClient, sessionContext, new ReleaseResponseData());
        }
    }
}