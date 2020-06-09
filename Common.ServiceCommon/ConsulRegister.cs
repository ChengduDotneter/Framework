using Common.Model;
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 服务发现辅助类
    /// </summary>
    public static class ConsulRegister
    {
        /// <summary>
        /// 初始化服务发现
        /// </summary>
        /// <param name="app"></param>
        /// <param name="lifetime"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IApplicationBuilder RegisterConsul(this IApplicationBuilder app, IApplicationLifetime lifetime, IConfiguration configuration)
        {
            ConsulServiceEntity serviceEntity = new ConsulServiceEntity();

            //获取配置文件中IdentityServerConfig节点转换为实体identityServerConfig
            configuration.Bind("ConsulService", serviceEntity);

            //请求注册的 Consul 地址
            var consulClient = new ConsulClient(x => x.Address = new Uri($"http://{serviceEntity.ConsulIP}:{serviceEntity.ConsulPort}"));

            var httpCheck = new AgentServiceCheck()
            {
                //服务启动多久后注册
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(serviceEntity.DeregisterCriticalServiceAfter),
                //健康检查时间间隔，或者称为心跳间隔
                Interval = TimeSpan.FromSeconds(serviceEntity.Interval),
                //健康检查地址
                HTTP = $"http://{serviceEntity.IP}:{serviceEntity.Port}/{serviceEntity.HealthPath}",
                Timeout = TimeSpan.FromSeconds(serviceEntity.Timeout)
            };

            // Register service with consul
            var registration = new AgentServiceRegistration()
            {
                Checks = new[] { httpCheck },
                ID = Guid.NewGuid().ToString(),
                Name = serviceEntity.ServiceName,
                Address = serviceEntity.IP,
                Port = serviceEntity.Port,
                //添加 urlprefix-/servicename 格式的 tag 标签，以便 Fabio 识别
                Tags = new[] { $"urlprefix-/{serviceEntity.ServiceName}" }
            };

            //服务启动时注册，内部实现其实就是使用 Consul API 进行注册（HttpClient发起）
            consulClient.Agent.ServiceRegister(registration).Wait();

            lifetime.ApplicationStopping.Register(() =>
            {
                //服务停止时取消注册
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            });

            return app;
        }
    }

}
