using Common.RPC;
using System;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 申请资源处理器，服务接收端（现包括ZeroMQ和UDP）
    /// </summary>
    internal class ApplyResourceProcessor : RequestProcessorBase<ApplyRequestData, ApplyResponseData>
    {
        private ServiceClient m_serviceClient;

        public ApplyResourceProcessor(ServiceClient serviceClient) : base(1000 * 10)
        {
            m_serviceClient = serviceClient;
        }

        /// <summary>
        /// 资源申请
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
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

    /// <summary>
    /// 释放资源处理器，服务接收端（现包括ZeroMQ和UDP）
    /// </summary>
    internal class ReleaseResourceProcessor : RequestProcessorBase<ReleaseRequestData, ReleaseResponseData>
    {
        private ServiceClient m_serviceClient;

        public ReleaseResourceProcessor(ServiceClient serviceClient) : base(1000 * 10)
        {
            m_serviceClient = serviceClient;
        }

        /// <summary>
        /// 资源释放
        /// </summary>
        /// <param name="table">所需释放的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <returns></returns>
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
