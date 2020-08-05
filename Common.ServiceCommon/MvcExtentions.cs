using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Configuration;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Multicast;
using Common.DAL;
using Common.Log;
using Common.MessageQueueClient;
using Common.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Common.ServiceCommon
{
    /// <summary>
    /// MVC扩展类
    /// </summary>
    public static class MvcExtentions
    {
        private const int DEFAULT_THREAD_COUNT = 200;
        private static bool m_isCodeFirst;
        private static IDictionary<Type, Func<object>> m_defaultSearchQueryProviderDic;
        private static IDictionary<Type, Func<object>> m_defaultEditQueryProviderDic;

        static MvcExtentions()
        {
            m_defaultSearchQueryProviderDic = new Dictionary<Type, Func<object>>();
            m_defaultEditQueryProviderDic = new Dictionary<Type, Func<object>>();
        }

        /// <summary>
        /// 配置初始化
        /// </summary>
        /// <param name="hostBuilderContext"></param>
        /// <returns></returns>
        public static HostBuilderContext ConfigInit(this HostBuilderContext hostBuilderContext)
        {
#if DEBUG
            hostBuilderContext.HostingEnvironment.EnvironmentName = "Development";
# elif RELEASE
            hostBuilderContext.HostingEnvironment.EnvironmentName = "Production";
#endif

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ConfigManager.Init(hostBuilderContext.HostingEnvironment.EnvironmentName);
            m_isCodeFirst = Convert.ToBoolean(ConfigManager.Configuration["IsCodeFirst"]);

            if (!int.TryParse(ConfigManager.Configuration["MinThreadCount"], out int threadCount))
                threadCount = DEFAULT_THREAD_COUNT;

            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
            ThreadPool.SetMinThreads(Math.Max(workerThreads, threadCount), Math.Max(completionPortThreads, threadCount));

            return hostBuilderContext;
        }

        /// <summary>
        /// Ignite初始化配置
        /// </summary>
        /// <param name="hostBuilderContext"></param>
        /// <returns></returns>
        public static HostBuilderContext ConfigIgnite(this HostBuilderContext hostBuilderContext)
        {
            IgniteManager.Init(ConfigIgnite());
            return hostBuilderContext;
        }

        private static IgniteConfiguration ConfigIgnite()
        {
            IList<BinaryTypeConfiguration> binaryTypeConfigurations = new List<BinaryTypeConfiguration>();

            Type[] modelTypes = TypeReflector.ReflectType((type) =>
            {
                if (type.GetInterface(typeof(IEntity).FullName) == null || type.IsInterface || type.IsAbstract)
                    return false;

                if (type.GetCustomAttribute<IgnoreTableAttribute>() != null)
                    return false;

                return true;
            });

            for (int i = 0; i < modelTypes.Length; i++)
            {
                binaryTypeConfigurations.Add(new BinaryTypeConfiguration(modelTypes[i])
                {
                    Serializer = (IBinarySerializer)Activator.CreateInstance(typeof(IgniteBinaryBufferSerializer<>).MakeGenericType(modelTypes[i])),
                });
            }

            IgniteConfiguration igniteConfiguration = new IgniteConfiguration()
            {
                Localhost = ConfigManager.Configuration["IgniteService:LocalHost"],

                DiscoverySpi = new TcpDiscoverySpi()
                {
                    IpFinder = new TcpDiscoveryMulticastIpFinder()
                    {
                        Endpoints = new[] { ConfigManager.Configuration["IgniteService:TcpDiscoveryMulticastIpFinderEndPoint"] }
                    }
                },

                DataStorageConfiguration = new DataStorageConfiguration
                {
                    DefaultDataRegionConfiguration = new DataRegionConfiguration
                    {
                        Name = ConfigManager.Configuration["IgniteService:RegionName"],
                        PersistenceEnabled = true
                    }
                },

                BinaryConfiguration = new BinaryConfiguration
                {
                    TypeConfigurations = binaryTypeConfigurations
                }
            };

            return igniteConfiguration;
        }

        /// <summary>
        /// 添加Controller
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="modelTypes"></param>
        /// <param name="dynamicControllerTypes"></param>
        /// <returns></returns>
        public static IMvcBuilder AddControllers(this IServiceCollection serviceCollection, Type[] modelTypes, Type[] dynamicControllerTypes)
        {
            IMvcBuilder mvcBuilder = serviceCollection.AddControllers(options =>
            {
                options.OutputFormatters.Insert(0, new JArrayOutputFormatter());
                options.OutputFormatters.Insert(1, new JObjectOutputFormatter());
            });

            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddSingleton<IPageQueryParameterService, HttpContextQueryStringPageQueryParameterService>();
            mvcBuilder.AddApplicationPart(ModelTypeControllerManager.GenerateModelTypeControllerToAssembly(modelTypes));
            mvcBuilder.AddApplicationPart(DynamicControllerManager.GenerateDynamicControllerToAssembly(dynamicControllerTypes));
            mvcBuilder.AddApplicationPart(typeof(HealthController).Assembly);
            serviceCollection.AddScoped<ISSOUserService, SSOUserService>();
            serviceCollection.AddSingleton<ITccTransactionManager, TccTransactionManager>();

            return mvcBuilder;
        }

        /// <summary>
        /// 配置验证器
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="mvcBuilder"></param>
        /// <param name="maxErrorCount"></param>
        /// <returns></returns>
        public static IMvcBuilder ConfigureValidation(this IServiceCollection serviceCollection, IMvcBuilder mvcBuilder, int maxErrorCount)
        {
            serviceCollection.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            mvcBuilder.AddMvcOptions((options) =>
            {
                options.MaxModelValidationErrors = maxErrorCount;
                options.Filters.Add<ApiValidationFilter>();
            });

            return mvcBuilder;
        }

        /// <summary>
        /// 查询操作初始化
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="modelTypes"></param>
        /// <param name="searchQueryProvider"></param>
        /// <param name="editQueryProvider"></param>
        public static void AddQuerys(this IServiceCollection serviceCollection, Type[] modelTypes, Func<Type, object> searchQueryProvider = null, Func<Type, object> editQueryProvider = null)
        {
            for (int i = 0; i < modelTypes.Length; i++)
            {
                Type modelType = modelTypes[i];
                Type searchQueryType = typeof(ISearchQuery<>).MakeGenericType(modelType);
                Type editQueryType = typeof(IEditQuery<>).MakeGenericType(modelType);

                if (searchQueryProvider == null)
                {
                    m_defaultSearchQueryProviderDic.Add(modelType, Expression.Lambda<Func<object>>(
                           Expression.Call(typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetSearchSqlSugarQuery)).MakeGenericMethod(modelType),
                                           Expression.Constant(m_isCodeFirst, typeof(bool)))).Compile());

                    serviceCollection.AddScoped(searchQueryType, sp => m_defaultSearchQueryProviderDic[modelType].Invoke());
                }
                else
                {
                    serviceCollection.AddScoped(searchQueryType, sp => searchQueryProvider.Invoke(modelType));
                }

                if (editQueryProvider == null)
                {
                    m_defaultEditQueryProviderDic.Add(modelType, Expression.Lambda<Func<object>>(
                           Expression.Call(typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetEditSqlSugarQuery)).MakeGenericMethod(modelType),
                                           Expression.Constant(m_isCodeFirst, typeof(bool)))).Compile());

                    serviceCollection.AddScoped(editQueryType, sp => m_defaultEditQueryProviderDic[modelType].Invoke());
                }
                else
                {
                    serviceCollection.AddScoped(editQueryType, sp => editQueryProvider.Invoke(modelType));
                }
            }
        }

        /// <summary>
        /// RBMQ相关接口依赖注册
        /// </summary>
        /// <param name="serviceCollection"></param>
        public static void AddTransfers(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped(typeof(IMQProducer<MessageBody>), sp => MessageQueueFactory.GetRabbitMQProducer<MessageBody>());
            serviceCollection.AddScoped(typeof(IMQConsumer<MessageBody>), sp => MessageQueueFactory.GetRabbitMQConsumer<MessageBody>());
        }

        /// <summary>
        /// 并行计算任务工厂依赖注入
        /// </summary>
        /// <param name="serviceCollection"></param>
        public static void AddComputeFactory(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IComputeFactory, ComputeFactory>();
        }

        /// <summary>
        /// JSON序列化相关接口依赖注册
        /// </summary>
        /// <param name="serviceCollection"></param>
        public static void AddJsonSerialize(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IJObjectSerializeService, JObjectSerializeService>();
            serviceCollection.AddScoped<IJObjectConverter, JObjectConverter>();

            serviceCollection.AddScoped<IJArraySerializeService, JArraySerializeService>();
            serviceCollection.AddScoped<IJArrayConverter, JArrayConverter>();
        }

        /// <summary>
        /// 日志Kafka接口依赖注入
        /// </summary>
        /// <param name="serviceCollection"></param>
        public static void AddKafkaLogHelper(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ILogHelper, KafkaLogHelper>();
        }

        /// <summary>
        /// 日志Kafka接口依赖注入
        /// </summary>
        /// <param name="serviceCollection"></param>
        public static void AddLog4NetLogHelper(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ILogHelper, Log4netLogHelper>();
        }
    }
}