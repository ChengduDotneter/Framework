using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Common.DAL;
using Common.MessageQueueClient;
using Common.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Common.ServiceCommon
{
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

        public static HostBuilderContext ConfigInit(this HostBuilderContext hostBuilderContext)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            ConfigManager.Init(hostBuilderContext.HostingEnvironment.EnvironmentName);
            m_isCodeFirst = Convert.ToBoolean(ConfigManager.Configuration["IsCodeFirst"]);

            return hostBuilderContext;
        }

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

        public static void AddTransfers(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped(typeof(IPublisher<MessageBody>), sp => MessageQueueFactory.GetPublisherContext<MessageBody>());
            serviceCollection.AddScoped(typeof(ISubscriber<MessageBody>), sp => MessageQueueFactory.GetSubscriberContext<MessageBody>());
        }

        public static void AddJsonSerialize(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IJObjectSerializeService, JObjectSerializeService>();
            serviceCollection.AddScoped<IJObjectConverter, JObjectConverter>();

            serviceCollection.AddScoped<IJArraySerializeService, JArraySerializeService>();
            serviceCollection.AddScoped<IJArrayConverter, JArrayConverter>();
        }
    }
}