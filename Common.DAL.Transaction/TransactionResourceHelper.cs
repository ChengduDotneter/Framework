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
            return ApplayResourceAsync(table, identity, weight, timeOut == EMPTY_TIME_OUT ? m_timeOut : timeOut).Result;
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
            return await m_applyResourceProcessor.Apply(table, identity, weight, timeOut == EMPTY_TIME_OUT ? m_timeOut : timeOut);
        }

        /// <summary>
        /// 释放事务资源
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        public static void ReleaseResource(long identity)
        {
            ReleaseResourceAsync(identity).Wait();
        }

        /// <summary>
        /// 释放事务资源，异步
        /// </summary>
        /// <param name="table">所需申请的表类型</param>
        /// <param name="identity">事务线程ID</param>
        public static async Task ReleaseResourceAsync(long identity)
        {
            if (!await m_releaseResourceProcessor.Release(identity))
                throw new DealException($"释放事务{identity}资源失败。");
        }




        class TestAdapter : ITransferAdapter
        {
            public event OnBufferRecievedHandler OnBufferRecieved;
            public event Action<SessionContext, byte[]> S;

            public void SendBuffer(SessionContext sessionContext, byte[] buffer, int length)
            {
                byte[] d = new byte[length];
                Array.Copy(buffer, d, d.Length);
                S?.Invoke(sessionContext, d);
            }

            public void Strat()
            {

            }

            public void Recieve(SessionContext sessionContext, byte[] d)
            {
                OnBufferRecieved?.Invoke(sessionContext, d);
            }
        }



        /// <summary>
        /// 申请资源处理器，服务发起端（现包括ZeroMQ和UDP）
        /// </summary>
        class AApplyResourceProcessor : ResponseProcessorBase<ApplyRequestData>
        {
            private const int MAX_TIME_OUT = 1000 * 60;
            private ServiceClient m_serviceClient;

            public AApplyResourceProcessor(ServiceClient serviceClient) : base(serviceClient)
            {
                m_serviceClient = serviceClient;
            }

            /// <summary>
            /// 数据处理
            /// </summary>
            /// <param name="sessionContext"></param>
            /// <param name="data"></param>
            protected override async void ProcessData(SessionContext sessionContext, ApplyRequestData data)
            {
                //if (data.TimeOut < 0 ||
                //    data.TimeOut > MAX_TIME_OUT)
                //    throw new DealException($"超时时间范围为：{0}-{MAX_TIME_OUT}ms");

                //IResource resource = m_actorClient.GetGrain<IResource>(data.ResourceName);
                //bool successed = await resource.Apply(data.Identity, data.Weight, data.TimeOut);

                //SendSessionData(m_serviceClient, sessionContext, new ApplyResponseData() { Success = successed });

                SendSessionData(m_serviceClient, sessionContext, new ApplyResponseData() { Success = true });
            }
        }

        /// <summary>
        /// 释放资源处理器，服务发起端（现包括ZeroMQ和UDP）
        /// </summary>
        class AReleaseResourceProcessor : ResponseProcessorBase<ReleaseRequestData>
        {
            private ServiceClient m_serviceClient;

            public AReleaseResourceProcessor(ServiceClient serviceClient) : base(serviceClient)
            {
                m_serviceClient = serviceClient;
            }

            /// <summary>
            /// 数据处理
            /// </summary>
            /// <param name="sessionContext"></param>
            /// <param name="data"></param>
            protected override async void ProcessData(SessionContext sessionContext, ReleaseRequestData data)
            {
                //IResource resource = m_actorClient.GetGrain<IResource>(data.ResourceName);
                //await resource.Release(data.Identity);
                SendSessionData(m_serviceClient, sessionContext, new ReleaseResponseData());
            }
        }



        /// <summary>
        /// 静态构造函数
        /// </summary>
        static TransactionResourceHelper()
        {
            string timeOutString = ConfigManager.Configuration["ResourceManager:TimeOut"];
            m_timeOut = string.IsNullOrWhiteSpace(timeOutString) ? DEFAULT_TIME_OUT : Convert.ToInt32(timeOutString);

            //var a = new TestAdapter();
            //m_serviceClient = new ServiceClient(a, BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            //m_serviceClient = new ServiceClient(TransferAdapterFactory.CreateUDPCRCTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), UDPCRCSocketTypeEnum.Client), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            m_serviceClient = new ServiceClient(TransferAdapterFactory.CreateZeroMQTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), ZeroMQSocketTypeEnum.Client, Guid.NewGuid().ToString()), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));



            m_applyResourceProcessor = new ApplyResourceProcessor(m_serviceClient);
            m_releaseResourceProcessor = new ReleaseResourceProcessor(m_serviceClient);

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;



            //var b = new TestAdapter();
            //ServiceClient c = new ServiceClient(b, BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));
            //AApplyResourceProcessor aApplyResourceProcessor = new AApplyResourceProcessor(c);
            //AReleaseResourceProcessor aReleaseResourceProcessor = new AReleaseResourceProcessor(c);


            //a.S += b.Recieve;
            //b.S += a.Recieve;



            m_serviceClient.Start();
            //c.Start();



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