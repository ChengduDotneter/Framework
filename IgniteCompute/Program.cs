using System;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Compute;
using Common.ServiceCommon;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IgniteCompute
{
    class Program
    {
        static async Task Main()
        {
            await Host.CreateDefaultBuilder().
                       ConfigureServices((hostBuilderContext, serviceCollection) =>
                       {
                           ConfigManager.Init("Development");
                           hostBuilderContext.ConfigIgnite();

                           //System.Threading.Tasks.Task.Factory.StartNew(async () =>
                           //{
                           //    while (true)
                           //    {
                           //        var result = await Common.Compute.ComputeFactory.GetIgniteCompute().BordercastAsync(new ComputeFunc(), new ComputeParameter() { RequestData = "ZZZ" });
                           //        Console.WriteLine(string.Join(Environment.NewLine, result.Select(item => item.ResponseData)));

                           //        await Task.Delay(1000);
                           //    }
                           //});
                       }).
                       ConfigureLogging((loggingBuilder) =>
                       {
                           loggingBuilder.ClearProviders();
                       }).
                       Build().
                       RunAsync();

            Console.WriteLine("End");
        }
    }

    public class ComputeParameter
    {
        public string RequestData { get; set; }
    }

    public class ComputeResult
    {
        public string ResponseData { get; set; }
    }

    public class ComputeFunc : IComputeFunc<ComputeParameter, ComputeResult>
    {
        public ComputeResult Excute(ComputeParameter parameter)
        {
            Console.WriteLine(parameter.RequestData);
            return new ComputeResult() { ResponseData = DateTime.Now.ToString("g") };
        }
    }
}
