using Apache.Ignite.Core;
using Apache.Ignite.Core.Binary;
using Apache.Ignite.Core.Configuration;
using Apache.Ignite.Core.Discovery.Tcp;
using Apache.Ignite.Core.Discovery.Tcp.Static;
using Common.Const;
using Common.DAL;
using Common.DAL.Cache;
using Common.Log;
using Common.Log.LogModel;
using Common.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Common.ServiceCommon
{
    /// <summary>
    /// MVC扩展类
    /// </summary>
    public static class MvcExtentions
    {
        private const int DEFAULT_THREAD_COUNT = 200;
        private static IDictionary<Type, Func<object>> m_defaultSearchQueryProviderDic;
        private static IDictionary<Type, Func<object>> m_defaultEditQueryProviderDic;
        private static IDictionary<Type, Func<object>> m_defaultCacheProviderDic;
        private static string m_clientID;

        static MvcExtentions()
        {
            m_defaultSearchQueryProviderDic = new Dictionary<Type, Func<object>>();
            m_defaultEditQueryProviderDic = new Dictionary<Type, Func<object>>();
            m_defaultCacheProviderDic = new Dictionary<Type, Func<object>>();
        }

        /// <summary>
        /// 配置初始化
        /// </summary>
        /// <param name="hostBuilderContext"></param>
        /// <param name="serviceCollection"></param>
        /// <param name="environmentName"></param>
        /// <returns></returns>
        public static HostBuilderContext ConfigInit(this HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection, string environmentName = null)
        {
            if (string.IsNullOrWhiteSpace(environmentName))
            {
#if DEBUG
                hostBuilderContext.HostingEnvironment.EnvironmentName = Environments.Development;
# elif RELEASE
                hostBuilderContext.HostingEnvironment.EnvironmentName = Environments.Production;
#endif
            }
            else
            {
                hostBuilderContext.HostingEnvironment.EnvironmentName = environmentName;
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ConfigManager.Init(hostBuilderContext.HostingEnvironment.EnvironmentName);
            m_clientID = ConfigManager.Configuration["ClientID"];

            serviceCollection.AddSingleton<IClientAccessTokenManager, ClientAccessTokenManager>();
            serviceCollection.AddHttpClient(Options.DefaultName, (serviceProvider, httpClient) =>
            {
                IHttpContextAccessor httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
                IHttpClientFactory httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

                if (httpContextAccessor != null)
                {
                    SSOUserInfo ssoUserInfo = new SSOUserService(httpContextAccessor, httpClientFactory).GetUser();

                    if (ssoUserInfo != SSOUserInfo.Empty)
                    {
                        httpClient.DefaultRequestHeaders.Add("userName", ssoUserInfo.UserName);
                        httpClient.DefaultRequestHeaders.Add("id", ssoUserInfo.ID.ToString());
                        httpClient.DefaultRequestHeaders.Add("phone", ssoUserInfo.Phone);
                    }
                    else
                    {
                        (bool hasToken, string token) = serviceProvider.GetService<IClientAccessTokenManager>().GetToken();

                        if (hasToken)
                            httpClient.DefaultRequestHeaders.Add(HttpHeaderConst.AUTHORIZATION, token);
                    }
                }

                if (!string.IsNullOrEmpty(m_clientID))
                    httpClient.DefaultRequestHeaders.Add("ClientID", m_clientID);
            });

            if (!int.TryParse(ConfigManager.Configuration["MinThreadCount"], out int threadCount))
                threadCount = DEFAULT_THREAD_COUNT;

            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
            ThreadPool.SetMinThreads(Math.Max(workerThreads, threadCount), Math.Max(completionPortThreads, threadCount));

            if (!bool.Parse(ConfigManager.Configuration["UseKafka"]))
                serviceCollection.DefaultLogHelperConfig(LogHelperTypeEnum.Log4netLog);
            else
                serviceCollection.DefaultLogHelperConfig(LogHelperTypeEnum.KafkaLog);

            serviceCollection.AddJsonSerialize();

            return hostBuilderContext;
        }

        /// <summary>
        /// Ignite初始化配置
        /// </summary>
        /// <param name="hostBuilderContext"></param>
        /// <returns></returns>
        public static HostBuilderContext ConfigIgnite(this HostBuilderContext hostBuilderContext)
        {
            IgniteManager.Init(ConfigIgnite(Path.Combine(hostBuilderContext.HostingEnvironment.ContentRootPath, "spring.xml")));
            return hostBuilderContext;
        }

        private static IgniteConfiguration ConfigIgnite(string springConfigPath)
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
                SpringConfigUrl = springConfigPath,

                DiscoverySpi = new TcpDiscoverySpi()
                {
                    LocalAddress = ConfigManager.Configuration["IgniteService:LocalHost"],
                    LocalPort = Convert.ToInt32(ConfigManager.Configuration["IgniteService:DiscoverPort"]),
                    IpFinder = new TcpDiscoveryStaticIpFinder()
                    {
                        Endpoints = ConfigManager.Configuration.GetSection("IgniteService:TcpDiscoveryStaticIpEndPoints").GetChildren().
                                                  Select(item => item.Value).
                                                  Concat(new[] { $"{ConfigManager.Configuration["IgniteService:LocalHost"]}:{ConfigManager.Configuration["IgniteService:DiscoverPort"]}" }).
                                                  ToArray()
                    }
                },

                DataStorageConfiguration = new DataStorageConfiguration
                {
                    DefaultDataRegionConfiguration = new DataRegionConfiguration
                    {
                        Name = ConfigManager.Configuration["IgniteService:RegionName"],
                        PersistenceEnabled = false
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
            mvcBuilder.AddApplicationPart(typeof(HttpCompute).Assembly);
            serviceCollection.AddScoped<ISSOUserService, SSOUserService>();
            serviceCollection.AddSingleton<ITccNotifyFactory, TccNotifyFactory>();
            serviceCollection.AddSingleton<ITccTransactionManager, TccTransactionManager>();

            return mvcBuilder;
        }

        /// <summary>
        /// 启动ComputeService
        /// </summary>
        /// <param name="_"></param>
        public static void AddHttpCompute(this IServiceCollection _)
        {
            HttpCompute.StartHttpComputeService();
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
        /// <param name="cacheProviderProvider"></param>
        /// <param name="dbResourceContentProvider"></param>
        /// <param name="createTableQueryProvider"></param>
        public static void AddQuerys(this IServiceCollection serviceCollection,
                                     Type[] modelTypes,
                                     Func<Type, object> searchQueryProvider = null,
                                     Func<Type, object> editQueryProvider = null,
                                     Func<Type, object> cacheProviderProvider = null,
                                     Func<IDBResourceContent> dbResourceContentProvider = null,
                                     Func<ICreateTableQuery> createTableQueryProvider = null)
        {
            if (dbResourceContentProvider != null)
                serviceCollection.AddScoped(sp => dbResourceContentProvider.Invoke());
            else
                serviceCollection.AddScoped(sp => DaoFactory.GetLinq2DBResourceContent());

            if (createTableQueryProvider != null)
                serviceCollection.AddScoped(sp => createTableQueryProvider.Invoke());
            else
                serviceCollection.AddScoped(sp => DaoFactory.GetLinq2DBCreateTableQuery());

            for (int i = 0; i < modelTypes.Length; i++)
            {
                Type modelType = modelTypes[i];
                Type searchQueryType = typeof(ISearchQuery<>).MakeGenericType(modelType);
                Type editQueryType = typeof(IEditQuery<>).MakeGenericType(modelType);
                Type cacheProviderType = typeof(ICacheProvider<>).MakeGenericType(modelType);

                if (searchQueryProvider == null)
                {
                    m_defaultSearchQueryProviderDic.Add(modelType, Expression.Lambda<Func<object>>(
                                                                                                   Expression.Call(typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetSearchLinq2DBQuery)).
                                                                                                                       MakeGenericMethod(modelType))).Compile());

                    serviceCollection.AddScoped(searchQueryType, sp => m_defaultSearchQueryProviderDic[modelType].Invoke());
                }
                else
                {
                    serviceCollection.AddScoped(searchQueryType, sp => searchQueryProvider.Invoke(modelType));
                }

                if (editQueryProvider == null)
                {
                    m_defaultEditQueryProviderDic.Add(modelType, Expression.Lambda<Func<object>>(
                                                                                                 Expression.Call(typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetEditLinq2DBQuery)).
                                                                                                                                    MakeGenericMethod(modelType))).Compile());

                    serviceCollection.AddScoped(editQueryType, sp => m_defaultEditQueryProviderDic[modelType].Invoke());
                }
                else
                {
                    serviceCollection.AddScoped(editQueryType, sp => editQueryProvider.Invoke(modelType));
                }

                if (cacheProviderProvider == null)
                {
                    m_defaultCacheProviderDic.Add(modelType, Expression.Lambda<Func<object>>(
                                                                                             Expression.Call(typeof(CacheFactory).GetMethod(nameof(CacheFactory.CreateMemoryCacheProvider)).
                                                                                                                                  MakeGenericMethod(modelType))).Compile());

                    serviceCollection.AddScoped(cacheProviderType, sp => m_defaultCacheProviderDic[modelType].Invoke());
                }
                else
                {
                    serviceCollection.AddScoped(cacheProviderType, sp => cacheProviderProvider.Invoke(modelType));
                }
            }
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
        private static void AddJsonSerialize(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IJObjectSerializeService, JObjectSerializeService>();
            serviceCollection.AddScoped<IJObjectConverter, JObjectConverter>();

            serviceCollection.AddScoped<IJArraySerializeService, JArraySerializeService>();
            serviceCollection.AddScoped<IJArrayConverter, JArrayConverter>();
        }
    }
}