using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;

namespace Common.ServiceCommon
{
    /// <summary>
    /// Swagger配置类
    /// </summary>
    public static class SwaggerConfigure
    {
        /// <summary>
        /// 向服务集合中添加Swagger服务
        /// </summary>
        /// <param name="services"></param>
        public static void AddSwagger(this IServiceCollection services)
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.Model.xml")))
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
                    options.IncludeXmlComments(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.Model.xml"));
                });
        }

        /// <summary>
        /// 使用Swagger页面
        /// </summary>
        /// <param name="applicationBuilder"></param>
        public static void UseSwaggerPage(this IApplicationBuilder applicationBuilder)
        {
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{AppDomain.CurrentDomain.FriendlyName}.Model.xml")))
            {
                applicationBuilder.UseSwagger();

                applicationBuilder.UseSwaggerUI(setupAction =>
                {
                    setupAction.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
                });
            }
        }
    }
}