using System;
using System.Threading;
using System.Threading.Tasks;

namespace Common.DAL.Transaction
{
    public static class TransactionResourceHelper
    {
        private const int DEFAULT_TIME_OUT = 60 * 1000;
        private const int EMPTY_TIME_OUT = -1;
        private readonly static int m_timeOut;

        public static bool ApplayResource(Type table, int timeOut = EMPTY_TIME_OUT)
        {
            return ApplayResourceAsync(table, timeOut).Result;
        }

        public static async Task<bool> ApplayResourceAsync(Type table, int timeOut = EMPTY_TIME_OUT)
        {
            string url = $"http://{ConfigManager.Configuration["ResourceManager:EndPoint"]}/resource/{table.FullName}/{Thread.CurrentThread.ManagedThreadId}/{(timeOut == EMPTY_TIME_OUT ? m_timeOut : timeOut)}";
            HttpWebResponseResult httpWebResponseResult = await HttpWebRequestHelper.JsonGetAsync(url);

            if (httpWebResponseResult.HttpStatus != System.Net.HttpStatusCode.OK)
                throw new DealException($"申请事务资源{table.FullName}失败。");

            return Convert.ToBoolean(httpWebResponseResult.DataString);
        }

        public static void ReleaseResource(Type table)
        {
            ReleaseResourceAsync(table).Wait();
        }

        public static async Task ReleaseResourceAsync(Type table)
        {
            string url = $"http://{ConfigManager.Configuration["ResourceManager:EndPoint"]}/resource/{table.FullName}/{Thread.CurrentThread.ManagedThreadId}";
            HttpWebResponseResult httpWebResponseResult = await HttpWebRequestHelper.JsonDeleteAsync(url);

            if (httpWebResponseResult.HttpStatus != System.Net.HttpStatusCode.OK)
                throw new DealException($"释放事务资源{table.FullName}失败。");
        }

        static TransactionResourceHelper()
        {
            string timeOutString = ConfigManager.Configuration["ResourceManager:TimeOut"];
            m_timeOut = string.IsNullOrWhiteSpace(timeOutString) ? DEFAULT_TIME_OUT : Convert.ToInt32(timeOutString);
        }
    }
}
