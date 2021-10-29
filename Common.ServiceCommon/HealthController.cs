using Common.Const;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 此方法为心跳监测接口类，用于服务发现
    /// </summary>
    [ApiController]
    [Route("health")]
    public class HealthController : ControllerBase
    {
        /// <summary>
        /// Get
        /// </summary>
        /// <returns></returns>
        [HttpGet("{serviceID}")]
        public string Get(string serviceID)
        {
            if (serviceID != ConsulRegister.RegistrationID)
                throw new DealException("服务ID不匹配。");

            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}