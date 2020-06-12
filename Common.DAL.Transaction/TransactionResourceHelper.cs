using Common.RPC;
using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    public static class TransactionResourceHelper
    {
        private const int DEFAULT_TIME_OUT = 60 * 1000;
        private const int EMPTY_TIME_OUT = -1;
        private readonly static int m_timeOut;
        private readonly static ServiceClient m_serviceClient;
        private readonly static ApplyResourceProcessor m_applyResourceProcessor;
        private readonly static ReleaseResourceProcessor m_releaseResourceProcessor;

        public static bool ApplayResource(Type table, long identity, int weight, int timeOut = EMPTY_TIME_OUT)
        {
            return true;
            //return ApplayResourceAsync(table, identity, weight, timeOut == EMPTY_TIME_OUT ? m_timeOut : timeOut).Result;
        }

        public static async Task<bool> ApplayResourceAsync(Type table, long identity, int weight, int timeOut = EMPTY_TIME_OUT)
        {
            return true;

            //int time = Environment.TickCount;

            //bool result = await m_applyResourceProcessor.Apply(table, identity, weight, timeOut == EMPTY_TIME_OUT ? m_timeOut : timeOut);

            //Console.WriteLine(Environment.TickCount - time);

            //return result;
        }

        public static void ReleaseResource(Type table, long identity)
        {
         //   ReleaseResourceAsync(table, identity).Wait();
        }

        public static async Task ReleaseResourceAsync(Type table, long identity)
        {
            //int time = Environment.TickCount;

            //if (!await m_releaseResourceProcessor.Release(table, identity))
            //    throw new DealException($"释放事务资源{table.FullName}失败。");

            //Console.WriteLine(Environment.TickCount - time);
        }

        static TransactionResourceHelper()
        {
            string timeOutString = ConfigManager.Configuration["ResourceManager:TimeOut"];
            m_timeOut = string.IsNullOrWhiteSpace(timeOutString) ? DEFAULT_TIME_OUT : Convert.ToInt32(timeOutString);

            m_serviceClient = new ServiceClient(TransferAdapterFactory.CreateZeroMQTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), ZeroMQSocketTypeEnum.Client, ConfigManager.Configuration["RPC:Identity"]), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            m_serviceClient.Start();

            m_applyResourceProcessor = new ApplyResourceProcessor(m_serviceClient);
            m_releaseResourceProcessor = new ReleaseResourceProcessor(m_serviceClient);

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

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