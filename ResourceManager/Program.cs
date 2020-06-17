using Common;
using Common.DAL.Transaction;
using Common.RPC;
using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Net;
using System.Text;

namespace ResourceManager
{
    class Program
    {
        static void Main(string[] args)
        {
            ConfigManager.Init("Development");

            ServiceClient serviceClient = new ServiceClient(TransferAdapterFactory.CreateZeroMQTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), ZeroMQSocketTypeEnum.Server, ConfigManager.Configuration["RPC:Identity"]), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            //ServiceClient serviceClient = new ServiceClient(TransferAdapterFactory.CreateUDPCRCTransferAdapter(new IPEndPoint(IPAddress.Parse(ConfigManager.Configuration["RPC:IPAddress"]), Convert.ToInt32(ConfigManager.Configuration["RPC:Port"])), UDPCRCSocketTypeEnum.Server), BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));

            serviceClient.Start();

            new HostBuilder()
                .UseOrleans((Microsoft.Extensions.Hosting.HostBuilderContext context, ISiloBuilder builder) =>
                {
                    builder
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "ResourceManager";
                            options.ServiceId = "ResourceManager";
                        })
                        .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(Resource).Assembly).WithReferences());
                })
                .ConfigureServices(services =>
                {
                    services.Configure<ConsoleLifetimeOptions>(options =>
                    {
                        options.SuppressStatusMessages = true;
                    });

                    services.AddSingleton(serviceClient);
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
