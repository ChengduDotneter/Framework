using Common;
using Common.DAL;
using Common.DAL.Cache;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using TestWebAPI.Controllers;

// ReSharper disable NotAccessedField.Local

namespace TestWebAPI
{
    public interface ITest : IDisposable
    {
        void Test();
    }

    public class Test1 : ITest
    {
        public void Dispose()
        {

        }

        public void Test()
        {

        }
    }

    internal class TestTCCNotify : ITccNotify<TCCTestData>
    {
        public void Notify(long tccID, bool successed, TCCTestData data)
        {
            Console.WriteLine(data.Data);
        }
    }

    public class Startup
    {
        private IConfiguration m_configuration;
        private Type[] m_modelTypes;

        public Startup(IConfiguration configuration)
        {
            m_configuration = configuration;

            m_modelTypes = TypeReflector.ReflectType((type) =>
            {
                if (type.GetInterface(typeof(IEntity).FullName) == null || type.IsInterface || type.IsAbstract)
                    return false;

                if (type.GetCustomAttribute<IgnoreTableAttribute>() != null)
                    return false;

                return true;
            });
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //跨域问题
            services.AddCors(options =>
            {
                options.AddPolicy("any", builder =>
                {
                    builder.SetIsOriginAllowed(_ => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });

            Type[] controllerTypes = TypeReflector.ReflectType((type) =>
            {
                if (type.GetInterface(typeof(IDynamicController).FullName) == null ||
                    type.IsInterface ||
                    type.IsAbstract)
                    return false;

                return true;
            });

            IMvcBuilder mvcBuilder = services.AddControllers(m_modelTypes, controllerTypes);
            services.ConfigureValidation(mvcBuilder, 10);

            Func<Type, object> cacheProviderHandler = (type) =>
            {
                return typeof(CacheFactory).GetMethod(nameof(CacheFactory.CreateMemoryCacheProvider)).MakeGenericMethod(type).Invoke(null, null);
            };

            services.AddQuerys(m_modelTypes, cacheProviderProvider: cacheProviderHandler /*,
            searchQueryProvider: (type) =>
            {
                return typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetSearchMongoDBQuery)).MakeGenericMethod(type).Invoke(null, null);
            },
            editQueryProvider: (type) =>
            {
                return typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetEditMongoDBQuery)).MakeGenericMethod(type).Invoke(null, null);
            }*/);

            services.AddSwagger();
            //services.AddHostedService<CrossService>();

            //services.AddHostedService<TestService>();
            //services.AddScoped<IScopeInstance, ScopeInstance>();

            //services.AddScoped<ITest, Test1>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime, ICreateTableQuery createTableQuery)
        {
            //app.ApplicationServices.GetService<ITccNotifyFactory>().RegisterNotify(new TestTCCNotify());
            createTableQuery.CreateTable("s2b", m_modelTypes).Wait();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseSwaggerPage();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseMiddleware<LogMiddleware>(env.IsDevelopment());

            var options = new WebSocketOptions() { KeepAliveInterval = TimeSpan.FromSeconds(15) };
            options.AllowedOrigins.Add("*");
            app.UseWebSockets(options);

            app.UseMiddleware<WebSocketMiddleware>();

            app.UseCors("any");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //服务发现
            //if (!env.IsDevelopment())
            //app.RegisterConsul(lifetime, m_configuration);
        }
    }
}