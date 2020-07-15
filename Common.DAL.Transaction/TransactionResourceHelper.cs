using Common.RPC;
using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    /// <summary>
    /// 事务资源帮助类
    /// </summary>
    public static class TransactionResourceHelper
    {
        /// <summary>
        /// 默认超时时间
        /// </summary>
        private const int DEFAULT_TIME_OUT = 60 * 1000;

        /// <summary>
        /// 空的超时时间
        /// </summary>
        private const int EMPTY_TIME_OUT = -1;

        /// <summary>
        /// 超时时间
        /// </summary>
        private readonly static int m_timeOut;

        /// <summary>
        /// RPC服务客户端
        /// </summary>
        private readonly static ServiceClient m_serviceClient;

        /// <summary>
        /// 申请事务资源处理器
        /// </summary>
        private readonly static ApplyResourceProcessor m_applyResourceProcessor;

        /// <summary>
        /// 释放事务资源处理器
        /// </summary>
        private readonly static ReleaseResourceProcessor m_releaseResourceProcessor;

        /// <summary>
        /// 申请事务资源
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        public static bool ApplayResource(Type table, long identity, int weight, int timeOut = EMPTY_TIME_OUT)
        {
            return m_applyResourceProcessor.Apply(table, identity, weight, timeOut == EMPTY_TIME_OUT ? m_timeOut : timeOut);
        }

        /// <summary>
        /// 申请事务资源，异步
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        /// <param name="weight">权重</param>
        /// <param name="timeOut">超时时间</param>
        /// <returns></returns>
        public static async Task<bool> ApplayResourceAsync(Type table, long identity, int weight, int timeOut = EMPTY_TIME_OUT)
        {
            return await m_applyResourceProcessor.ApplyAsync(table, identity, weight, timeOut == EMPTY_TIME_OUT ? m_timeOut : timeOut);
        }

        /// <summary>
        /// 释放事务资源
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        public static void ReleaseResource(long identity)
        {
            if (!m_releaseResourceProcessor.Release(identity))
                throw new DealException($"释放事务{identity}资源失败。");
        }

        /// <summary>
        /// 释放事务资源，异步
        /// </summary>
        /// <param name="identity">事务线程ID</param>
        public static async Task ReleaseResourceAsync(long identity)
        {
            if (!await m_releaseResourceProcessor.ReleaseAsync(identity))
                throw new DealException($"释放事务{identity}资源失败。");
        }

        /// <summary>
        /// 静态构造函数
        /// </summary>
        static TransactionResourceHelper()
        {
            string timeOutString = ConfigManager.Configuration["ResourceManager:Timeout"];
            m_timeOut = string.IsNullOrWhiteSpace(timeOutString) ? DEFAULT_TIME_OUT : Convert.ToInt32(timeOutString);

            m_serviceClient = new ServiceClient(TransferAdapterFactory.CreateUDPCRCTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), UDPCRCSocketTypeEnum.Client), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            //m_serviceClient = new ServiceClient(TransferAdapterFactory.CreateZeroMQTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), ZeroMQSocketTypeEnum.Client, Guid.NewGuid().ToString()), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            m_applyResourceProcessor = new ApplyResourceProcessor(m_serviceClient);
            m_releaseResourceProcessor = new ReleaseResourceProcessor(m_serviceClient);

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            m_serviceClient.Start();
        }

        /// <summary>
        /// 服务退出触发释放事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (m_applyResourceProcessor != null)
                m_applyResourceProcessor.Dispose();

            if (m_releaseResourceProcessor != null)
                m_releaseResourceProcessor.Dispose();

            if (m_serviceClient != null)
                m_serviceClient.Dispose();
        }
    }
}