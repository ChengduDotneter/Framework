using System;
using System.Threading;
using System.Threading.Tasks;
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
        private static IServiceProvider m_serviceProvider;

        public static void Main(string[] args)
        {
            IHostBuilder hostBuilder = CreateHostBuilder(args);
            IHost host = hostBuilder.Build();

            m_serviceProvider = host.Services;
            m_serviceProvider.CreateScope();

            long count = 0;
            int time = Environment.TickCount;

            new Thread(() =>
            {
                while (true)
                {
                    foreach (var item in m_serviceProvider.GetServices<ILogHelper>())
                    {
                        item.Info("123", Guid.NewGuid().ToString());
                    }

                    count++;
                }
            })
            {
                IsBackground = true
            }.Start();


            new Thread(() =>
            {
                while (true)
                {
                    foreach (var item in m_serviceProvider.GetServices<ILogHelper>())
                    {
                        item.Info("123", "path", Guid.NewGuid().ToString(), "testcontroller");
                    }
                }
            })
            {
                IsBackground = true
            }.Start();

            new Thread(() =>
            {
                while (true)
                {
                    foreach (var item in m_serviceProvider.GetServices<ILogHelper>())
                    {
                        item.Error("123", "path", Guid.NewGuid().ToString(), "testcontroller", "message", 500);
                    }
                }
            })
            {
                IsBackground = true
            }.Start();

            new Thread(() =>
            {
                while (true)
                {
                    foreach (var item in m_serviceProvider.GetServices<ILogHelper>())
                    {
                        item.SqlError("123", Guid.NewGuid().ToString(), "message");
                    }
                }
            })
            {
                IsBackground = true
            }.Start();


            new Thread(() =>
            {
                while (true)
                {
                    foreach (var item in m_serviceProvider.GetServices<ILogHelper>())
                    {
                        item.TCCNode(123, false, "message");
                    }
                }
            })
            {
                IsBackground = true
            }.Start();

            new Thread(() =>
            {
                while (true)
                {
                    foreach (var item in m_serviceProvider.GetServices<ILogHelper>())
                    {
                        item.TCCServer(123, "message");
                    }
                }
            })
            {
                IsBackground = true
            }.Start();

            new Thread(() =>
            {
                while (true)
                {
                    if (count == 0)
                        time = Environment.TickCount;
                    if (Environment.TickCount - time > 0)
                    {
                        Console.WriteLine($"Count: {count / (Environment.TickCount - time)}");
                    }

                    Thread.Sleep(1000);
                }
            })
            {
                IsBackground = true
            }.Start();

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