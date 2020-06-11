using System.Threading.Tasks;
using Common.DAL.Transaction;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Common.ServiceCommon
{
    [ApiController]
    [Route("resource")]
    public class ResourceController
    {
        private const int MAX_TIME_OUT = 1000 * 60;
        private readonly IGrainFactory m_client;

        public ResourceController(IGrainFactory client)
        {
            m_client = client;
        }

        [HttpGet("{resourceName}/{identity}/{weight}/{timeOut}")]
        public Task<bool> Applay(string resourceName, long identity, int weight, int timeOut)
        {
            if (timeOut < 0 ||
                timeOut > MAX_TIME_OUT)
                throw new DealException($"超时时间范围为：{0}-{MAX_TIME_OUT}ms");

            IResource resource = m_client.GetGrain<IResource>(resourceName);
            return resource.Apply(identity, weight, timeOut);
        }

        [HttpDelete("{resourceName}/{identity}")]
        public Task Release(string resourceName, long identity)
        {
            IResource resource = m_client.GetGrain<IResource>(resourceName);
            return resource.Release(identity);
        }
    }
}
