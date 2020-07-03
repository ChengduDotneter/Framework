using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.DAL.Transaction;
using Common.RPC;
using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ResourceManager
{
    internal class Program
    {
        private static void Main()
        {
            ConfigManager.Init("Production");

            //ServiceClient serviceClient = new ServiceClient(TransferAdapterFactory.CreateZeroMQTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), ZeroMQSocketTypeEnum.Server, ConfigManager.Configuration["RPC:Identity"]), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            ServiceClient serviceClient = new ServiceClient(TransferAdapterFactory.CreateUDPCRCTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), UDPCRCSocketTypeEnum.Server), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            serviceClient.Start();

            new HostBuilder().
                ConfigureServices(services =>
                {
                    services.Configure<ConsoleLifetimeOptions>(options =>
                    {
                        options.SuppressStatusMessages = true;
                    });

                    services.AddSingleton<IDeadlockDetection, DeadlockDetection>();
                    services.AddSingleton(serviceClient);
                    services.AddHostedService<ApplyResourceProcessor>();
                    services.AddHostedService<ReleaseResourceProcessor>();
                    //services.AddHostedService<Test>();
                }).
                ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                }).RunConsoleAsync();

            Console.WriteLine("Resource Manager Running...");

            Console.Read();

            serviceClient.Dispose();
        }
    }


    internal class Test : IHostedService
    {
        ApplyResourceProcessor a;
        ReleaseResourceProcessor b;

        public Test(ApplyResourceProcessor a, ReleaseResourceProcessor b)
        {
            this.a = a;
            this.b = b;
        }



        public Task StartAsync(CancellationToken cancellationToken)
        {
            Task.Factory.StartNew(() =>
            {
                int time = Environment.TickCount;
                int count = 0;

                while (true)
                {
                    a.Test(new ApplyRequestData() { Identity = 1, ResourceName = "a", TimeOut = 1000, Weight = 0 });
                    a.Test(new ApplyRequestData() { Identity = 1, ResourceName = "b", TimeOut = 1000, Weight = 0 });

                    //Thread.Sleep(1);

                    Task.Factory.StartNew(() => { b.Test(new ReleaseRequestData() { Identity = 1, ResourceName = "a" }); });
                    Task.Factory.StartNew(() => { b.Test(new ReleaseRequestData() { Identity = 1, ResourceName = "b" }); });

                    count++;

                    if (Environment.TickCount - time > 1000)
                    {
                        Console.WriteLine(count);
                        count = 0;
                        time = Environment.TickCount;
                    }
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}