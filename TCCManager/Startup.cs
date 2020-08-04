using System;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace TCCManager
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
            //解决跨域问题
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

            IMvcBuilder mvcBuilder = services.AddControllers(new Type[0], new Type[0]);

            services.AddHttpContextAccessor();
            services.ConfigureValidation(mvcBuilder, 10);
            services.AddJsonSerialize();
            services.AddSwagger();
            services.AddHttpClient();
        }

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
