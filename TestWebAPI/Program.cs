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
            //Common.ConfigManager.Init("Production");
            //string key = System.Guid.NewGuid().ToString("D");
            //Common.Lock.ILock @lock = Common.Lock.LockFactory.GetRedisLock();
            //int count = 0;

            //for (int i = 0; i < 100; i++)
            //{
            //    int index = i;

            //    System.Threading.Thread thread = new System.Threading.Thread(() =>
            //    {
            //        while (true)
            //        {
            //            string identity = System.Guid.NewGuid().ToString("D");

            //            if (@lock.Acquire(key, identity, 0, 10000))
            //            {
            //                //Thread.Sleep(2);

            //                Thread.Sleep(4000);

            //                count++;

            //                @lock.Release(identity);
            //            }
            //            else
            //            {
            //                Console.WriteLine("lock time out" + Environment.TickCount);
            //            }
            //        }
            //    });

            //    thread.IsBackground = true;

            //    thread.Start();
            //}

            //System.Threading.Thread thread1 = new System.Threading.Thread(() =>
            //{
            //    int time = Environment.TickCount;

            //    while (true)
            //    {
            //        System.Threading.Thread.Sleep(1000);

            //        //System.Console.WriteLine(count * 1000f / (Environment.TickCount - time));
            //        Console.WriteLine(count);
            //        //count = 0;
            //        //time = Environment.TickCount;
            //    }
            //});

            //thread1.IsBackground = true;
            //thread1.Start();


            //System.Console.Read();
















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