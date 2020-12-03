using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 服务发现辅助类
    /// </summary>
    public static class ConsulRegister
    {
        private const int MONITOR_SPAN = 1000 * 10;

        /// <summary>
        /// 初始化服务发现
        /// </summary>
        /// <param name="app"></param>
        /// <param name="lifetime"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IApplicationBuilder RegisterConsul(this IApplicationBuilder app, Microsoft.Extensions.Hosting.IHostApplicationLifetime lifetime, IConfiguration configuration)
        {
            ConsulServiceEntity serviceEntity = new ConsulServiceEntity();
            configuration.Bind("ConsulService", serviceEntity);

            //Consul访问端口
            ConsulClient consulClient = new ConsulClient(item => item.Address = new Uri($"http://{serviceEntity.ConsulIP}:{serviceEntity.ConsulPort}"));

            // 注册服务发现的
            AgentServiceRegistration registration = new AgentServiceRegistration()
            {
                Checks = new[]
                {
                    new AgentServiceCheck()
                    {
                        //服务启动多久后注册
                        DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(serviceEntity.DeregisterCriticalServiceAfter),
                        //健康检查时间间隔，或者称为心跳间隔
                        Interval = TimeSpan.FromSeconds(serviceEntity.Interval),
                        //健康检查地址
                        HTTP = $"http://{serviceEntity.IP}:{serviceEntity.Port}/{serviceEntity.HealthPath}",
                        //Consul超时时间
                        Timeout = TimeSpan.FromSeconds(serviceEntity.Timeout)
                    }
                },
                //节点ID
                ID = IDGenerator.NextID().ToString(),
                //节点服务名
                Name = serviceEntity.ServiceName,
                //节点地址
                Address = serviceEntity.IP,
                //节点端口号
                Port = serviceEntity.Port,
                //添加 urlprefix-/servicename 格式的 tag 标签，以便 Fabio 识别
                Tags = new[] { $"urlprefix-/{serviceEntity.ServiceName}" }
            };

            //服务启动时注册，内部实现其实就是使用 Consul API 进行注册（HttpClient发起）
            consulClient.Agent.ServiceRegister(registration).Wait();

            //后台监控Consul注册情况 如果Consul失去注册  则退出当前进程。
            new Thread(() =>
            {
                while (true)
                {
                    if (Convert.ToBoolean(ConfigManager.Configuration["ConsulService:IsConsulMonitor"]))
                    {
                        QueryResult<CatalogService[]> queryResult = consulClient.Catalog.Service(registration.Name).Result;

                        if (queryResult != null && queryResult.Response != null && queryResult.Response.Length > 0)
                        {
                            IEnumerable<CatalogService> oldNodes = queryResult.Response.Where(item => item.Address == $"{registration.Address}:{registration.Port}" && item.ServiceID != registration.ID);

                            if (oldNodes.Count() > 0)
                            {
                                foreach (CatalogService oldNode in oldNodes)
                                {
                                    consulClient.Agent.ServiceDeregister(oldNode.ServiceID).Wait();
                                }
                            }
                        }

                        if (queryResult == null || queryResult.Response == null || queryResult.Response.Length == 0 || queryResult.Response.Count(item => item.ServiceID == registration.ID) == 0)
                        {
                            consulClient.Agent.ServiceDeregister(registration.ID).Wait();
                            Environment.Exit(0);
                        }
                    }

                    Thread.Sleep(MONITOR_SPAN);
                }
            })
            {
                IsBackground = true,
                Name = "ConsulMonitor"
            }.Start();

            //程序退出时，注销服务发现。
            lifetime.ApplicationStopping.Register(() =>
            {
                //服务停止时取消注册
                consulClient.Agent.ServiceDeregister(registration.ID).Wait();
            });

            return app;
        }
    }
}