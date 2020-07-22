using System;
using System.Threading;
using System.Threading.Tasks;
using Common.RPC;
using Common.RPC.TransferAdapter;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 申请资源处理器，服务接收端（现包括ZeroMQ和UDP）
    /// </summary>
    internal class ApplyResourceProcessor : RequestProcessorBase<ApplyRequestData, ApplyResponseData>
    {
        private ServiceClient m_serviceClient;
        private long m_hostID;

        public ApplyResourceProcessor(ServiceClient serviceClient, long hostID) : base(1000 * 45)
        {
            m_serviceClient = serviceClient;
            m_hostID = hostID;
        }

        /// <summary>
        /// 异步资源申请
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        public async Task<bool> ApplyAsync(Type table, long identity, int weight, int timeOut)
        {
            bool successed = false;

            bool result = await RequestAsync(m_serviceClient, new ApplyRequestData()
            {
                ResourceName = table.FullName,
                Identity = identity,
                Weight = weight,
                TimeOut = timeOut,
                HostID = m_hostID
            }, applyResponseData =>
            {
                successed = applyResponseData.Success;
                return true;
            });

            return result ? successed : false;
        }

        /// <summary>
        /// 同步资源申请
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        public bool Apply(Type table, long identity, int weight, int timeOut)
        {
            bool successed = false;

            bool result = Request(m_serviceClient, new ApplyRequestData()
            {
                ResourceName = table.FullName,
                Identity = identity,
                Weight = weight,
                TimeOut = timeOut,
                HostID = m_hostID
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
        private long m_hostID;

        public ReleaseResourceProcessor(ServiceClient serviceClient, long hostID) : base(1000 * 45)
        {
            m_serviceClient = serviceClient;
            m_hostID = hostID;
        }

        /// <summary>
        /// 异步资源释放
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        public async Task<bool> ReleaseAsync(long identity)
        {
            bool result = await RequestAsync(m_serviceClient, new ReleaseRequestData()
            {
                Identity = identity,
                HostID = m_hostID
            }, releaseResponseData =>
            {
                return true;
            });

            return result;
        }

        /// <summary>
        /// 同步资源释放
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        public bool Release(long identity)
        {
            bool result = Request(m_serviceClient, new ReleaseRequestData()
            {
                Identity = identity,
                HostID = m_hostID
            }, releaseResponseData =>
            {
                return true;
            });

            return result;
        }
    }

    internal class ResourceHeartBeatProcessor : ProcessorBase, IDisposable
    {
        private const int THREAD_TIME_SPAN = 200;
        private ServiceClient m_serviceClient;
        private bool m_running;
        private Thread m_heartBeatThread;
        private long m_hostID;

        public ResourceHeartBeatProcessor(ServiceClient serviceClient, long hostID)
        {
            m_serviceClient = serviceClient;
            m_running = true;
            m_hostID = hostID;

            m_heartBeatThread = new Thread(HeartBeatCheck);
            m_heartBeatThread.IsBackground = true;
            m_heartBeatThread.Name = "HEARTBEAT_CHECK_THREAD";
            m_heartBeatThread.Start();
        }

        public void Dispose()
        {
            m_running = false;
        }

        private void HeartBeatCheck()
        {
            while (m_running)
            {
                SendSessionData(m_serviceClient, new SessionContext(IDGenerator.NextID()), new ResourceHeartBeatReqesut() { HostID = m_hostID });
                Thread.Sleep(THREAD_TIME_SPAN);
            }
        }
    }
}