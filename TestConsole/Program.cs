using System;
using Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Common.ServiceCommon;
using Common.DAL;
using System.Reflection;

namespace TestConsole
{


    internal class Program
    {
        private static void Main(string[] _)
        {
            new HostBuilder().
                ConfigureServices((hostBuilderContext, services) =>
                {
                    hostBuilderContext.ConfigInit();

                    services.Configure<ConsoleLifetimeOptions>(options =>
                    {
                        options.SuppressStatusMessages = true;
                    });

                    services.AddComputeFactory();

                    Type[] modelTypes = TypeReflector.ReflectType((type) =>
                    {
                        if (type.GetInterface(typeof(IEntity).FullName) == null || type.IsInterface || type.IsAbstract)
                            return false;

                        if (type.GetCustomAttribute<IgnoreTableAttribute>() != null)
                            return false;

                        return true;
                    });

                    services.AddQuerys(modelTypes,
                                                (type) => typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetSearchIgniteQuery)).MakeGenericMethod(type).Invoke(null, null),
                                                (type) => typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetEditIgniteQuery)).MakeGenericMethod(type).Invoke(null, null));

                    services.AddScoped(sp => Common.Compute.ComputeFactory.GetIgniteCompute());
                    services.AddScoped(sp => Common.Compute.ComputeFactory.GetIgniteMapReduce());
                    services.AddScoped(sp => Common.Compute.ComputeFactory.GetIgniteAsyncMapReduce());
                    services.AddHostedService<ComputeTestTask>();
                }).
                ConfigureLogging(builder =>
                {
                    builder.AddConsole();
                }).RunConsoleAsync();

            Console.Read();
        }
    }
}