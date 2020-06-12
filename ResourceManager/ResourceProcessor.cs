﻿using Common;
using Common.DAL.Transaction;
using Common.RPC;
using Common.RPC.TransferAdapter;
using Microsoft.Extensions.Hosting;
using Orleans;
using System.Threading;
using System.Threading.Tasks;

namespace ResourceManager
{
    class ApplyResourceProcessor : ResponseProcessorBase<ApplyRequestData>, IHostedService
    {
        private const int MAX_TIME_OUT = 1000 * 60;
        private ServiceClient m_serviceClient;
        private IGrainFactory m_actorClient;

        public ApplyResourceProcessor(ServiceClient serviceClient, IGrainFactory actorClient) : base(serviceClient)
        {
            m_serviceClient = serviceClient;
            m_actorClient = actorClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async void ProcessData(SessionContext sessionContext, ApplyRequestData data)
        {
            if (data.TimeOut < 0 ||
                data.TimeOut > MAX_TIME_OUT)
                throw new DealException($"超时时间范围为：{0}-{MAX_TIME_OUT}ms");

            IResource resource = m_actorClient.GetGrain<IResource>(data.ResourceName);
            bool successed = await resource.Apply(data.Identity, data.Weight, data.TimeOut);

            SendSessionData(m_serviceClient, sessionContext, new ApplyResponseData() { Success = successed });
        }
    }

    class ReleaseResourceProcessor : ResponseProcessorBase<ReleaseRequestData>, IHostedService
    {
        private ServiceClient m_serviceClient;
        private IGrainFactory m_actorClient;

        public ReleaseResourceProcessor(ServiceClient serviceClient, IGrainFactory actorClient) : base(serviceClient)
        {
            m_serviceClient = serviceClient;
            m_actorClient = actorClient;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override async void ProcessData(SessionContext sessionContext, ReleaseRequestData data)
        {
            IResource resource = m_actorClient.GetGrain<IResource>(data.ResourceName);
            await resource.Release(data.Identity);
            SendSessionData(m_serviceClient, sessionContext, new ReleaseResponseData());
        }
    }
}