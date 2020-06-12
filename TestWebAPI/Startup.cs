using Common;
using Common.DAL;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;

namespace TestWebAPI
{
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

            //services.AddQuerys(modelTypes,
            //    (type) =>
            //    {
            //        return typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetSearchIgniteQuery)).MakeGenericMethod(type).Invoke(null, null);
            //    },
            //    (type) =>
            //    {
            //        return typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetEditIgniteQuery)).MakeGenericMethod(type).Invoke(null, null);
            //    });

            services.AddJsonSerialize();

            services.AddTransfers();

            services.AddSwagger();

            //System.Threading.Tasks.Task.Factory.StartNew(() =>
            //{
            //    var task = ETLHelper.Transform(
            //       modelTypes,
            //       (type) =>
            //       {
            //           return typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetSearchSqlSugarQuery)).MakeGenericMethod(type).Invoke(null, new object[] { false });
            //       },
            //       (type) =>
            //       {
            //           return typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetEditIgniteQuery)).MakeGenericMethod(type).Invoke(null, null);
            //       },
            //       2048);

            //    int time = Environment.TickCount;

            //    while (!task.Task.IsCompleted)
            //    {
            //        if (task.RunningTable != null)
            //            Console.WriteLine($"Current Table: {task.RunningTable.TableType.Name}, Data Count: {task.RunningTable.DataCount}, Complated Count: {task.RunningTable.ComplatedCount}");

            //        Thread.Sleep(5000);
            //    }

            //    Console.WriteLine("Transform Done.");
            //});
        }

#pragma warning disable CS0618 // 类型或成员已过时

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, Microsoft.AspNetCore.Hosting.IApplicationLifetime lifetime)
#pragma warning restore CS0618 // 类型或成员已过时
        {
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

            ////服务发现
            //if (!env.IsDevelopment())
            //    app.RegisterConsul(lifetime, m_configuration);
        }
    }
}