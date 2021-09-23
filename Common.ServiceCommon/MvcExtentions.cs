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
using System.Linq.Expressions;
using System.Net.Http;
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
        /// <param name="environmentName">自定义配置文件名</param>
        /// <returns></returns>
        public static HostBuilderContext ConfigInit(this HostBuilderContext hostBuilderContext, IServiceCollection serviceCollection, string environmentName = null)
        {
            if (string.IsNullOrWhiteSpace(environmentName))//判断是否传了文件名 如果没传则根据运行模式默认加载配置文件 否则根据传递的名字加载
            {
#if DEBUG
                hostBuilderContext.HostingEnvironment.EnvironmentName = Environments.Development;//调试模式加载这个配置
# elif RELEASE
                hostBuilderContext.HostingEnvironment.EnvironmentName = Environments.Production;//生产模式这个配置
#endif
            }
            else
            {
                hostBuilderContext.HostingEnvironment.EnvironmentName = environmentName;//自己传文件名决定是那个配置文件
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);//注册编码提供程序。
            ConfigManager.Init(hostBuilderContext.HostingEnvironment.EnvironmentName);//初始化配置文件
            m_clientID = ConfigManager.Configuration["ClientID"];//从配置文件加载

            serviceCollection.AddSingleton<IClientAccessTokenManager, ClientAccessTokenManager>();//注入token签发获取程序
            serviceCollection.AddHttpClient(Options.DefaultName, (serviceProvider, httpClient) =>
            {
                IHttpContextAccessor httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
                IHttpClientFactory httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();

                if (httpContextAccessor != null)
                {
                    SSOUserInfo ssoUserInfo = new SSOUserService(httpContextAccessor, httpClientFactory).GetUser();//获取用户认证信息

                    if (ssoUserInfo != SSOUserInfo.Empty)
                    {
                        httpClient.DefaultRequestHeaders.Add("userName", ssoUserInfo.UserName);//把用户信息添加到请求头里面
                        httpClient.DefaultRequestHeaders.Add("id", ssoUserInfo.ID.ToString());
                        httpClient.DefaultRequestHeaders.Add("phone", ssoUserInfo.Phone);
                    }
                    else
                    {
                        (bool hasToken, string token) = serviceProvider.GetService<IClientAccessTokenManager>().GetToken();//获取生成的token

                        if (hasToken)
                            httpClient.DefaultRequestHeaders.Add(HttpHeaderConst.AUTHORIZATION, token);//把token添加到请求头里
                    }
                }

                if (!string.IsNullOrEmpty(m_clientID))//当ClientID不为null时 把他添加到请求头
                    httpClient.DefaultRequestHeaders.Add("ClientID", m_clientID);//添加到请求头
            });

            if (!int.TryParse(ConfigManager.Configuration["MinThreadCount"], out int threadCount))//线程池创建的最小线程数
                threadCount = DEFAULT_THREAD_COUNT;

            ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);//获取线程池线程池创建的最小线程数
            ThreadPool.SetMinThreads(Math.Max(workerThreads, threadCount), Math.Max(completionPortThreads, threadCount));//重新设置获取线程池线程池创建的最小线程数

            if (!bool.Parse(ConfigManager.Configuration["UseKafka"]))//判断配置文件使用哪种日志记录
                serviceCollection.DefaultLogHelperConfig(LogHelperTypeEnum.Log4netLog);
            else
                serviceCollection.DefaultLogHelperConfig(LogHelperTypeEnum.KafkaLog);

            serviceCollection.AddJsonSerialize();//注入JSON序列化相关接口

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
            {//向控制器添加数组对象序列化
                options.OutputFormatters.Insert(0, new JArrayOutputFormatter());
                options.OutputFormatters.Insert(1, new JObjectOutputFormatter());
            });

            serviceCollection.AddHttpContextAccessor();
            serviceCollection.AddSingleton<IPageQueryParameterService, HttpContextQueryStringPageQueryParameterService>();//分页注入
            mvcBuilder.AddApplicationPart(ModelTypeControllerManager.GenerateModelTypeControllerToAssembly(modelTypes));//动态加载controller
            mvcBuilder.AddApplicationPart(DynamicControllerManager.GenerateDynamicControllerToAssembly(dynamicControllerTypes));//动态加载控制器配置
            mvcBuilder.AddApplicationPart(typeof(HealthController).Assembly);//动态加载心跳配置
            mvcBuilder.AddApplicationPart(typeof(HttpCompute).Assembly);//动态加载http并行计算
            serviceCollection.AddScoped<ISSOUserService, SSOUserService>();//用户认证依赖注入
            serviceCollection.AddSingleton<ITccNotifyFactory, TccNotifyFactory>();//tcc通知注入
            serviceCollection.AddSingleton<ITccTransactionManager, TccTransactionManager>();//tcc事务管理注入

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
                serviceCollection.AddScoped(_ => dbResourceContentProvider.Invoke());
            else
                serviceCollection.AddScoped(_ => DaoFactory.GetLinq2DBResourceContent());

            if (createTableQueryProvider != null)
                serviceCollection.AddScoped(_ => createTableQueryProvider.Invoke());
            else
                serviceCollection.AddScoped(_ => DaoFactory.GetLinq2DBCreateTableQuery());

            for (int i = 0; i < modelTypes.Length; i++)
            {
                Type modelType = modelTypes[i];
                Type searchQueryType = typeof(ISearchQuery<>).MakeGenericType(modelType);
                Type editQueryType = typeof(IEditQuery<>).MakeGenericType(modelType);
                Type cacheProviderType = typeof(ICacheProvider<>).MakeGenericType(modelType);

                if (searchQueryProvider == null)
                {
                    m_defaultSearchQueryProviderDic.Add(modelType, Expression.Lambda<Func<object>>( //在DaoFactory里搜索 GetSearchLinq2DBQuery 这个公共方法
                                                                                                   Expression.Call(typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetSearchLinq2DBQuery)).
                                                                                                                           MakeGenericMethod(modelType))).Compile());
                                                                                                                        //使用 modelType替换掉泛型方法 Compile好像是编译为委托
                    // 往servers里添加 modelType类型的查询操作
                    serviceCollection.AddScoped(searchQueryType, _ => m_defaultSearchQueryProviderDic[modelType].Invoke());
                }
                else
                {
                    //注入时传入了表达式则执行传入的
                    serviceCollection.AddScoped(searchQueryType, _ => searchQueryProvider.Invoke(modelType));
                }

                if (editQueryProvider == null)
                {
                    m_defaultEditQueryProviderDic.Add(modelType, Expression.Lambda<Func<object>>(
                                                                                                 Expression.Call(typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetEditLinq2DBQuery)).
                                                                                                                                    MakeGenericMethod(modelType))).Compile());
                    //这是添加修改的操作
                    serviceCollection.AddScoped(editQueryType, _ => m_defaultEditQueryProviderDic[modelType].Invoke());
                }
                else
                {
                    serviceCollection.AddScoped(editQueryType, _ => editQueryProvider.Invoke(modelType));
                }

                if (cacheProviderProvider == null)
                {
                    m_defaultCacheProviderDic.Add(modelType, Expression.Lambda<Func<object>>(
                                                                                             Expression.Call(typeof(CacheFactory).GetMethod(nameof(CacheFactory.CreateMemoryCacheProvider)).
                                                                                                                                  MakeGenericMethod(modelType))).Compile());

                    serviceCollection.AddScoped(cacheProviderType, _ => m_defaultCacheProviderDic[modelType].Invoke());
                }
                else
                {
                    serviceCollection.AddScoped(cacheProviderType, _ => cacheProviderProvider.Invoke(modelType));
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