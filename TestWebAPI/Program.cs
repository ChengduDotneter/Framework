using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Cache;
using Common;
using Common.DAL;
using Common.Log;
using Common.Model;
using Common.ServiceCommon;
using LinqToDB.Mapping;
using MicroService.StorageService.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

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
            //hostBuilderContext.ConfigIgnite();
            hostBuilderContext.ConfigInit(services);
        }

        private static void LoggingConfig(HostBuilderContext hostBuilderContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            //loggingBuilder.AddConsole();
        }
    }
}