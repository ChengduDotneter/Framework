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
        private static ILogHelper logHelper = LogHelperFactory.GetKafkaLogHelper();
        public static void Main(string[] args)
        {
            IHostBuilder hostBuilder = CreateHostBuilder(args);
            IHost host = hostBuilder.Build();

            //logHelper.Info("123", "123");
            //logHelper.Info("123", "123", "123", "123");
            //logHelper.Error("123", "123", 200, "123", "123", "123");
            //logHelper.SqlError("123", "123", "123");
            //logHelper.TCCNode(123, true, "123");
            //logHelper.TCCServer(123, "123");

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
            //hostBuilderContext.ConfigIgnite();
        }

        private static void LoggingConfig(HostBuilderContext hostBuilderContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            //loggingBuilder.AddConsole();
        }
    }
}