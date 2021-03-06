using Common.Compute;
using Common.Log;
using Consul;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Common.ServiceCommon
{
    [Route("compute")]
    [ApiController]
    public class HttpCompute : ControllerBase
    {
        private class ComputeAssembly
        {
            public string Version { get; }//版本
            public IDictionary<string, ComputeFuncType> ComputeTypes { get; }//并行计算类型

            public ComputeAssembly(string version)
            {
                Version = version;
                ComputeTypes = new Dictionary<string, ComputeFuncType>();
            }
        }

        private class ComputeFuncType//并行计算函数类型
        {
            public Type FuncType { get; }//方法类型
            public Type ParameterType { get; }//参数类型

            public ComputeFuncType(Type type, Type parameterType)
            {
                FuncType = type;
                ParameterType = parameterType;
            }
        }

        private static ILogHelper m_logHelper;//日志
        private static IDictionary<string, ComputeAssembly> m_computeAssemblys;//
        private IHttpContextAccessor m_httpContextAccessor;//httpcontext
        private IComputeFactory m_computeFactory;//并行计算工厂

        public HttpCompute(IHttpContextAccessor httpContextAccessor, IComputeFactory computeFactory)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_computeFactory = computeFactory;
        }

        static HttpCompute()//http并行计算
        {
            m_logHelper = LogHelperFactory.GetDefaultLogHelper();//创建日志对象
            m_computeAssemblys = new Dictionary<string, ComputeAssembly>();//实例化

            TypeReflector.ReflectType((type) =>//类型反射期 筛选符合条件的对象
            {
                Type interfaceType = type.GetInterfaces().FirstOrDefault(item => item.IsGenericType &&//判断是否为泛型 且当前泛型IComputeFunc是否为两个参数或者一个参数的函数
                                                                                 (item.GetGenericTypeDefinition() == typeof(IComputeFunc<,>) ||
                                                                                  item.GetGenericTypeDefinition() == typeof(IComputeFunc<>)));

                if (interfaceType != null)
                {
                    AssemblyName assemblyName = type.Assembly.GetName();
                    Type[] genericTypeArguments = interfaceType.GenericTypeArguments;

                    if (!m_computeAssemblys.ContainsKey(assemblyName.FullName))
                        m_computeAssemblys.Add(assemblyName.FullName, new ComputeAssembly(assemblyName.Version.ToString()));

                    m_computeAssemblys[assemblyName.FullName].ComputeTypes.Add(type.FullName, new ComputeFuncType(type, genericTypeArguments.Length > 1 ? genericTypeArguments[0] : null));

                    return true;
                }

                return false;
            });
        }

        public static void StartHttpComputeService()
        {
            ConsulServiceEntity serviceEntity = new ConsulServiceEntity();
            ConfigManager.Configuration.Bind("ConsulService", serviceEntity);

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

            m_computeAssemblys.Values.SelectMany(item => item.ComputeTypes.Values).ForEach((type) =>
            {
                // Register service with consul
                var registration = new AgentServiceRegistration()
                {
                    Checks = new[] { httpCheck },
                    ID = Guid.NewGuid().ToString(),
                    Name = HttpComputeServiceNameGenerator.GeneratName(type.FuncType),
                    Address = serviceEntity.IP,
                    Port = serviceEntity.Port,
                    //添加 urlprefix-/servicename 格式的 tag 标签，以便 Fabio 识别
                    Tags = new[] { $"urlprefix-/compute_service/{type.FuncType.FullName}" }
                };

                //服务启动时注册，内部实现其实就是使用 Consul API 进行注册（HttpClient发起）
                consulClient.Agent.ServiceRegister(registration).Wait();

                AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                {
                    consulClient.Agent.ServiceDeregister(registration.ID).Wait();
                };
            });
        }

        private static bool AssemblyCheck(string assemblyName, string assemblyVersion, string className)
        {
            return m_computeAssemblys.ContainsKey(assemblyName) &&
                   m_computeAssemblys[assemblyName].Version == assemblyVersion &&
                   m_computeAssemblys[assemblyName].ComputeTypes.ContainsKey(className);
        }

        [HttpPost("mapReduce")]
        public Task<HttpComputeResult> MapReduce(HttpComputeParameter httpComputeParameter)
        {
            return ExcuteWithParameter(httpComputeParameter);
        }

        [HttpPost("boardcast")]
        public Task<HttpComputeResult> Boardcast(HttpComputeParameter httpComputeParameter)
        {
            return ExcuteWithParameter(httpComputeParameter);
        }

        [HttpPost("apply")]
        public Task<HttpComputeResult> Apply(HttpComputeParameter httpComputeParameter)
        {
            return ExcuteWithParameter(httpComputeParameter);
        }

        [HttpPost("call")]
        public Task<HttpComputeResult> Call(HttpComputeParameter httpComputeParameter)
        {
            return Excute(httpComputeParameter);
        }

        private Task<HttpComputeResult> Excute(HttpComputeParameter httpComputeParameter)
        {
            ConnectionInfo connectionInfo = m_httpContextAccessor.HttpContext.Connection;
            string method = m_httpContextAccessor.HttpContext.Request.RouteValues["action"].ToString().ToLower();

            if (!AssemblyCheck(httpComputeParameter.AssemblyName, httpComputeParameter.AssemblyVersion, httpComputeParameter.ClassName))
                throw new DealException($"{connectionInfo.LocalIpAddress}:{connectionInfo.LocalPort}未找到指向的Compute类型或Compute类型版本不匹配。");

            ComputeFuncType computeFuncType = m_computeAssemblys[httpComputeParameter.AssemblyName].ComputeTypes[httpComputeParameter.ClassName];
            object computeFunc = m_computeFactory.CreateComputeFunc(computeFuncType.FuncType);

            return Task.Factory.StartNew(() =>
            {
                m_logHelper.Info("httpCompute", $"method: {method}{Environment.NewLine}parameter: {httpComputeParameter.Parameter}");
                object result = computeFuncType.FuncType.GetMethod("Excute").Invoke(computeFunc, null);

                return new HttpComputeResult()
                {
                    ResponseEndpoint = $"{connectionInfo.LocalIpAddress}:{connectionInfo.LocalPort}",
                    Result = JObject.FromObject(result)
                };
            });
        }

        private Task<HttpComputeResult> ExcuteWithParameter(HttpComputeParameter httpComputeParameter)
        {
            ConnectionInfo connectionInfo = m_httpContextAccessor.HttpContext.Connection;
            string method = m_httpContextAccessor.HttpContext.Request.RouteValues["action"].ToString().ToLower();

            if (!AssemblyCheck(httpComputeParameter.AssemblyName, httpComputeParameter.AssemblyVersion, httpComputeParameter.ClassName))
                throw new DealException($"{connectionInfo.LocalIpAddress}:{connectionInfo.LocalPort}未找到指向的Compute类型或Compute类型版本不匹配。");

            ComputeFuncType computeFuncType = m_computeAssemblys[httpComputeParameter.AssemblyName].ComputeTypes[httpComputeParameter.ClassName];
            object computeFunc = m_computeFactory.CreateComputeFunc(computeFuncType.FuncType);
            object parameter = httpComputeParameter.Parameter.ToObject(computeFuncType.ParameterType);

            return Task.Factory.StartNew(() =>
            {
                m_logHelper.Info("httpCompute", $"method: {method}{Environment.NewLine}parameter: {httpComputeParameter.Parameter}");
                object result = computeFuncType.FuncType.GetMethod("Excute").Invoke(computeFunc, new object[] { parameter });

                return new HttpComputeResult()
                {
                    ResponseEndpoint = $"{connectionInfo.LocalIpAddress}:{connectionInfo.LocalPort}",
                    Result = JObject.FromObject(result)
                };
            });
        }
    }
}