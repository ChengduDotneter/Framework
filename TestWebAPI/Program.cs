using Common.Log;
using Common.Model;
using Common.ServiceCommon;
using Common.Validation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations.Schema;
// ReSharper disable UnusedMember.Local

namespace TestWebAPI
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Left : ViewModelBase
    {
        [Column]
        public string StudentName { get; set; }

        [Column]
        public long ClassID { get; set; }
    }
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class testcreate : ViewModelBase
    {
        [Column]
        public string StudentName { get; set; }

        [Column]
        public long ClassID { get; set; }
    }
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class testname : ViewModelBase
    {
        [NotNull]
        [Column]
        public string name { get; set; }

    }

    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class score : ViewModelBase
    {
        [Column]
        public long Stuscore { get; set; }

        [Column]
        public long stuid { get; set; }

    }

    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Right : ViewModelBase
    {
        [Column]
        public string ClassName { get; set; }
    }

    public class TestData : ViewModelBase
    {
        public string Data { get; set; }
        public DateTime AC { get; set; }
    }

    public class Program
    {
        private static ILogHelper logHelper = LogHelperFactory.GetKafkaLogHelper();

        public static void Main(string[] args)
        {
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
            services.DefaultLogHelperConfig(Common.Log.LogModel.LogHelperTypeEnum.Log4netLog);
            //hostBuilderContext.ConfigIgnite();
            hostBuilderContext.ConfigInit(services);
        }

        private static void LoggingConfig(HostBuilderContext hostBuilderContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConsole();
        }
    }
}