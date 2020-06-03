using Common.DAL;
using Common.MessageQueueClient;
using Common.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text;

namespace Common.ServiceCommon
{
    public static class MvcExtentions
    {
        private static bool m_isCodeFirst;

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
            if (searchQueryProvider == null)
            {
                //TODO: 反射性能问题
                searchQueryProvider = (modelType) =>
                {
                    return typeof(DaoFactory).
                           GetMethod(nameof(DaoFactory.GetSearchSqlSugarQuery)).
                           MakeGenericMethod(modelType).
                           Invoke(null, new object[] { m_isCodeFirst });
                };
            }

            if (editQueryProvider == null)
            {
                //TODO: 反射性能问题
                editQueryProvider = (modelType) =>
                {
                    return typeof(DaoFactory).
                           GetMethod(nameof(DaoFactory.GetEditSqlSugarQuery)).
                           MakeGenericMethod(modelType).
                           Invoke(null, new object[] { m_isCodeFirst });
                };
            }

            for (int i = 0; i < modelTypes.Length; i++)
            {
                Type modelType = modelTypes[i];
                Type searchQueryType = typeof(ISearchQuery<>).MakeGenericType(modelType);
                Type editQueryType = typeof(IEditQuery<>).MakeGenericType(modelType);

                serviceCollection.AddScoped(searchQueryType, sp => searchQueryProvider.Invoke(modelType));
                serviceCollection.AddScoped(editQueryType, sp => editQueryProvider.Invoke(modelType));
            }
        }

        public static void AddTransfers(this IServiceCollection serviceCollection)
        {
            //TODO: 反射性能问题
            serviceCollection.AddScoped(typeof(IPublisher<>).MakeGenericType(typeof(MessageBody)),
                sp => typeof(MessageQueueFactory).GetMethod("GetPublisherContext").MakeGenericMethod(typeof(MessageBody)).Invoke(null, new object[] { }));

            //TODO: 反射性能问题
            serviceCollection.AddScoped(typeof(ISubscriber<>).MakeGenericType(typeof(MessageBody)),
                sp => typeof(MessageQueueFactory).GetMethod("GetSubscriberContext").MakeGenericMethod(typeof(MessageBody)).Invoke(null, new object[] { }));
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