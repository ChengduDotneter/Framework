using Common.RPC;
using Common.RPC.TransferAdapter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 申请资源处理器，服务接收端（现包括ZeroMQ和UDP）
    /// </summary>
    internal class ApplyResourceProcessor : RequestProcessorBase<ApplyRequestData, ApplyResponseData>
    {
        private ServiceClient m_serviceClient;

        public ApplyResourceProcessor(ServiceClient serviceClient) : base(1000 * 90)
        {
            m_serviceClient = serviceClient;
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
                TimeOut = timeOut
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

        public ReleaseResourceProcessor(ServiceClient serviceClient) : base(1000 * 90)
        {
            m_serviceClient = serviceClient;
        }

        /// <summary>
        /// 异步资源释放
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        public async Task<bool> ReleaseAsync(long identity)
        {
            bool result = await RequestAsync(m_serviceClient, new ReleaseRequestData()
            {
                Identity = identity
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
                Identity = identity
            }, releaseResponseData =>
            {
                return true;
            });

            return result;
        }
    }

    internal class ResourceHeartBeatProcessor : ProcessorBase, IDisposable
    {
        private const int THREAD_TIME_SPAN = 20;
        private ServiceClient m_serviceClient;
        private ISet<long> m_identitys;
        private bool m_running;
        private Thread m_heartBeatThread;

        public ResourceHeartBeatProcessor(ServiceClient serviceClient)
        {
            m_serviceClient = serviceClient;
            m_identitys = new HashSet<long>();
            m_running = true;

            m_heartBeatThread = new Thread(HeartBeatCheck);
            m_heartBeatThread.IsBackground = true;
            m_heartBeatThread.Name = "HEARTBEAT_CHECK_THREAD";
            m_heartBeatThread.Start();
        }

        public void Dispose()
        {
            m_running = false;

            lock (m_identitys)
                m_identitys.Clear();
        }

        public void RegisterIdentity(long identity)
        {
            lock (m_identitys)
                m_identitys.Add(identity);
        }

        public void UnRegisterIdentity(long identity)
        {
            lock (m_identitys)
                m_identitys.Remove(identity);
        }

        private void HeartBeatCheck()
        {
            while (m_running)
            {
                long[] identitys;

                lock (m_identitys)
                    identitys = m_identitys.ToArray();

                for (int i = 0; i < identitys.Length; i++)
                    SendSessionData(m_serviceClient, new SessionContext(IDGenerator.NextID()), new ResourceHeartBeatReqesut() { Identity = identitys[i] });

                Thread.Sleep(THREAD_TIME_SPAN);
            }
        }
    }
}