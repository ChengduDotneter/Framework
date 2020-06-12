using Common.ServiceCommon;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestResourceManager
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).UseOrleans().ConfigureLogging(LoggingConfig).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static void LoggingConfig(HostBuilderContext hostBuilderContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
        }
    }
}
