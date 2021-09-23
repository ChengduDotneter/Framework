using System.Net.Http;

namespace Common.Compute
{
    /// <summary>
    /// 并行计算工厂
    /// </summary>
    public static class ComputeFactory
    {
        /// <summary>
        /// 创建Http并行计算
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <param name="consulServiceEntity"></param>
        /// <returns></returns>
        public static ICompute GetHttpCompute(IHttpClientFactory httpClientFactory, ConsulServiceEntity consulServiceEntity)
        {
            return HttpTask.CreateCompute(httpClientFactory, consulServiceEntity);
        }

        /// <summary>
        /// 创建同步HttpMapReduce
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <param name="consulServiceEntity"></param>
        public static IMapReduce GetHttpMapReduce(IHttpClientFactory httpClientFactory, ConsulServiceEntity consulServiceEntity)
        {
            return HttpTask.CreateMapReduce(httpClientFactory, consulServiceEntity);
        }

        /// <summary>
        /// 创建异步HttpMapReduce
        /// </summary>
        /// <param name="httpClientFactory"></param>
        /// <param name="consulServiceEntity"></param>
        public static IAsyncMapReduce GetHttpAsyncMapReduce(IHttpClientFactory httpClientFactory, ConsulServiceEntity consulServiceEntity)
        {
            return HttpTask.CreateAsyncMapReduce(httpClientFactory, consulServiceEntity);
        }
    }
}