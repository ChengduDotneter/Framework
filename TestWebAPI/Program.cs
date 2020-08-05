using System;
using System.Threading;
using Common.Log;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestWebAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {

            new Thread(() =>
            {
                ILogHelper logHelper = new KafkaLogHelper();
                logHelper.Info("123", Guid.NewGuid().ToString());
            })
            {
                IsBackground = true
            }.Start();


            new Thread(() =>
            {
                ILogHelper logHelper = new KafkaLogHelper();
                logHelper.Info("123", "path", Guid.NewGuid().ToString(), "testcontroller");
            })
            {
                IsBackground = true
            }.Start();

            new Thread(() =>
            {
                ILogHelper logHelper = new KafkaLogHelper();
                logHelper.Error("123", "path", Guid.NewGuid().ToString(), "testcontroller","message");
            })
            {
                IsBackground = true
            }.Start();












            IHostBuilder hostBuilder = CreateHostBuilder(args);
            IHost host = hostBuilder.Build();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

            hostBuilder.ConfigureServices(ConfigInit);
            hostBuilder.ConfigureLogging(LoggingConfig);
            hostBuilder.ConfigureWebHostDefaults(WebHostConfig);

            return hostBuilder;
        }

        private static void WebHostConfig(IWebHostBuilder webHost)
        {
            webHost.UseStartup<Startup>();
        }

        private static void ConfigInit(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            hostBuilderContext.ConfigInit();
        }

        private static void LoggingConfig(HostBuilderContext hostBuilderContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            //loggingBuilder.AddConsole();
        }
    }
}