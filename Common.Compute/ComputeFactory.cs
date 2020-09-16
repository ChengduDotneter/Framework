using System.Net.Http;

namespace Common.Compute
{
    /// <summary>
    /// 并行计算工厂
    /// </summary>
    public static class ComputeFactory
    {
        /// <summary>
        /// 创建Ignite并行计算
        /// </summary>
        public static ICompute GetIgniteCompute()
        {
            return IgniteTask.CreateCompute();
        }

        /// <summary>
        /// 创建Http并行计算
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <returns></returns>
        public static ICompute GetHttpCompute(IHttpClientFactory httpClientFactory)
        {
            return HttpTask.CreateCompute(httpClientFactory);
        }

        /// <summary>
        /// 创建同步IgniteMapReduce
        /// </summary>
        public static IMapReduce GetIgniteMapReduce()
        {
            return IgniteTask.CreateMapReduce();
        }

        /// <summary>
        /// 创建异步IgniteMapReduce
        /// </summary>
        public static IAsyncMapReduce GetIgniteAsyncMapReduce()
        {
            return IgniteTask.CreateAsyncMapReduce();
        }

        /// <summary>
        /// 创建同步HttpMapReduce
        /// </summary>
        /// <param name="httpClientFactory"></param>
        public static IMapReduce GetHttpMapReduce(IHttpClientFactory httpClientFactory)
        {
            return HttpTask.CreateMapReduce(httpClientFactory);
        }

        /// <summary>
        /// 创建异步HttpMapReduce
        /// </summary>
        /// <param name="httpClientFactory"></param>
        public static IAsyncMapReduce GetHttpAsyncMapReduce(IHttpClientFactory httpClientFactory)
        {
            return HttpTask.CreateAsyncMapReduce(httpClientFactory);
        }
    }
}
