using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using Common.DAL;
using Common.DAL.Transaction;
using Common.MessageQueueClient;
using Common.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Hosting;
using HostBuilderContext = Microsoft.Extensions.Hosting.HostBuilderContext;

namespace Common.ServiceCommon
{
    /// <summary>
    /// MVC扩展类
    /// </summary>
    public static class MvcExtentions
    {
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
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ConfigManager.Init(hostBuilderContext.HostingEnvironment.EnvironmentName);
            m_isCodeFirst = Convert.ToBoolean(ConfigManager.Configuration["IsCodeFirst"]);

            return hostBuilderContext;
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
            serviceCollection.AddScoped(typeof(IPublisher<MessageBody>), sp => MessageQueueFactory.GetPublisherContext<MessageBody>());
            serviceCollection.AddScoped(typeof(ISubscriber<MessageBody>), sp => MessageQueueFactory.GetSubscriberContext<MessageBody>());
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

        public static IHostBuilder UseOrleans(this IHostBuilder hostBuilder)
        {
            return hostBuilder.ConfigureServices(services =>
            {
                services.AddControllers().
                AddApplicationPart(typeof(ResourceController).Assembly).
                AddApplicationPart(typeof(Resource).Assembly);
            })
            .UseOrleans(siloBuilder =>
            {
                siloBuilder.UseLocalhostClustering()
                 .Configure<ClusterOptions>(opts =>
                 {
                     opts.ClusterId = "ResourceManager";
                     opts.ServiceId = "ResourceManager";
                 })
                 .Configure<EndpointOptions>(opts =>
                 {
                     opts.AdvertisedIPAddress = IPAddress.Loopback;
                 });
            });
        }
    }
}