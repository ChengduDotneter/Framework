using Common;
using Common.Compute;
using Common.DAL;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net.Http;
using System.Reflection;
using TestWebAPI.Controllers;

namespace TestWebAPI
{
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

        public Startup(IConfiguration configuration)
        {
            m_configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //跨域问题
            services.AddCors(options =>
            {
                options.AddPolicy("any", builder =>
                {
                    builder.SetIsOriginAllowed(origin => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            Type[] modelTypes = TypeReflector.ReflectType((type) =>
            {
                if (type.GetInterface(typeof(IEntity).FullName) == null || type.IsInterface || type.IsAbstract)
                    return false;

                if (type.GetCustomAttribute<IgnoreTableAttribute>() != null)
                    return false;

                return true;
            });

            Type[] controllerTypes = TypeReflector.ReflectType((type) =>
            {
                if (type.GetInterface(typeof(IDynamicController).FullName) == null ||
                    type.IsInterface ||
                    type.IsAbstract)
                    return false;

                return true;
            });

            IMvcBuilder mvcBuilder = services.AddControllers(modelTypes, controllerTypes);
            services.ConfigureValidation(mvcBuilder, 10);

            services.AddQuerys(modelTypes);

            services.AddJsonSerialize();

            services.AddSwagger();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            //app.ApplicationServices.GetService<ITccNotifyFactory>().RegisterNotify(new TestTCCNotify());

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseSwaggerPage();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseMiddleware<LogMiddleware>(env.IsDevelopment());

            app.UseCors("any");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            //服务发现
            if (!env.IsDevelopment())
                app.RegisterConsul(lifetime, m_configuration);
        }
    }
}