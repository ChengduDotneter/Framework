using Common.Lock;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace TCCManager
{
    /// <summary>
    /// Startup
    /// </summary>
    public class Startup
    {
        private IConfiguration m_configuration;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="configuration"></param>
        public Startup(IConfiguration configuration)
        {
            m_configuration = configuration;
        }

        /// <summary>
        /// 配置服务
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            //解决跨域问题
            services.AddCors(options =>
            {
                options.AddPolicy("any", builder =>
                {
                    builder.SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });

            IMvcBuilder mvcBuilder = services.AddControllers(new Type[0], new Type[0]);

            services.AddSingleton((_) => LockFactory.GetRedisLock());
            services.AddHttpContextAccessor();
            services.ConfigureValidation(mvcBuilder, 10);
            services.AddSwagger();
        }

        /// <summary>
        /// 配置管道
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="lifetime"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
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

            //注册服务发现
            if (!env.IsDevelopment())
                app.RegisterConsul(lifetime, m_configuration);
        }
    }
}