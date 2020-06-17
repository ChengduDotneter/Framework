using Common;
using Common.DAL.Transaction;
using Common.RPC;
using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Text;

namespace ResourceManager
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ConfigManager.Init("Development");

            ServiceClient serviceClient = new ServiceClient(TransferAdapterFactory.CreateZeroMQTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), ZeroMQSocketTypeEnum.Server, ConfigManager.Configuration["RPC:Identity"]), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            //ServiceClient serviceClient = new ServiceClient(TransferAdapterFactory.CreateUDPCRCTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), UDPCRCSocketTypeEnum.Server), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            serviceClient.Start();

            new HostBuilder()
                .ConfigureServices(services =>
                {
                    services.Configure<ConsoleLifetimeOptions>(options =>
                    {
                        options.SuppressStatusMessages = true;
                    });

                    services.AddSingleton<IResourceManager, Common.DAL.Transaction.ResourceManager>();
                    services.AddSingleton(serviceClient);
                    services.AddHostedService<DeadLockDetection>();
                    services.AddHostedService<ApplyResourceProcessor>();
                    services.AddHostedService<ReleaseResourceProcessor>();
                })
                .ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                }).RunConsoleAsync();

            Console.WriteLine("Resource Manager Running...");

            Console.Read();

            serviceClient.Dispose();
        }
    }
}