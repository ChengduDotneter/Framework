using Common.RPC;
using System;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    internal class ApplyResourceProcessor : RequestProcessorBase<ApplyRequestData, ApplyResponseData>
    {
        private ServiceClient m_serviceClient;

        public ApplyResourceProcessor(ServiceClient serviceClient) : base(1000 * 10)
        {
            m_serviceClient = serviceClient;
        }

        public async Task<bool> Apply(Type table, long identity, int weight, int timeOut)
        {
            bool successed = false;

            bool result = await Request(m_serviceClient, new ApplyRequestData()
            {
                ResourceName = table.FullName,
                Identity = identity,
                Weight = weight,
                TimeOut = timeOut
            }, applyResponseData =>
            {
                successed = applyResponseData.Success;
                return true;
            });

            return result ? successed : false;
        }
    }

    internal class ReleaseResourceProcessor : RequestProcessorBase<ReleaseRequestData, ReleaseResponseData>
    {
        private ServiceClient m_serviceClient;

        public ReleaseResourceProcessor(ServiceClient serviceClient) : base(1000 * 10)
        {
            m_serviceClient = serviceClient;
        }

        public async Task<bool> Release(Type table, long identity)
        {
            bool result = await Request(m_serviceClient, new ReleaseRequestData()
            {
                ResourceName = table.FullName,
                Identity = identity
            }, releaseResponseData =>
            {
                return true;
            });

            return result;
        }
    }
}
