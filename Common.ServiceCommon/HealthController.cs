using Microsoft.AspNetCore.Mvc;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 此方法为心跳监测接口类，用于服务发现
    /// </summary>
    [Produces("application/json")]
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get() => Ok("ok");
    }
}
