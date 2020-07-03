using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Compute;
using Common.DAL;
using Microsoft.Extensions.Hosting;
using NPOI.HPSF;
using ICompute = Common.Compute.ICompute;

namespace TestConsole
{
    public class ComputeTestTask : IHostedService
    {
        private readonly ISearchQuery<StockInfo> m_stockInfoSearchQuery;
        private readonly ISearchQuery<SupplierCommodity> m_supplierCommoditySearchQuery;
        private readonly ISearchQuery<WarehouseInfo> m_warehouseInfoSearchQuery;
        private readonly ICompute m_compute;
        private readonly IMapReduce m_mapReduce;
        private readonly IAsyncMapReduce m_asyncMapReduce;

        public ComputeTestTask(ISearchQuery<StockInfo> stockInfoSearchQuery, ISearchQuery<SupplierCommodity> supplierCommoditySearchQuery, ISearchQuery<WarehouseInfo> warehouseInfoSearchQuery, IMapReduce mapReduce, IAsyncMapReduce asyncMapReduce, ICompute compute)
        {
            m_stockInfoSearchQuery = stockInfoSearchQuery;
            m_supplierCommoditySearchQuery = supplierCommoditySearchQuery;
            m_warehouseInfoSearchQuery = warehouseInfoSearchQuery;
            m_mapReduce = mapReduce;
            m_asyncMapReduce = asyncMapReduce;
            m_compute = compute;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            //Task.Factory.StartNew(async () =>
            //{
            //    while (true)
            //    {
            //        Console.WriteLine((await m_compute.BordercastAsync(new TestFunc(), 5)).Min() + 5);
            //        await Task.Delay(100);
            //    }
            //});

            Test1Func[] test1Func = new Test1Func[1];

            for (int i = 0; i < test1Func.Length; i++)
                test1Func[i] = new Test1Func() { Value = Guid.NewGuid().ToString() };

            m_compute.Call(test1Func);

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    //Console.WriteLine((await m_compute.BordercastAsync(new TestFunc(), 5)).Min() + 5);

                    var result = m_compute.Call(test1Func);

                    foreach (var item in result)
                    {
                        Console.WriteLine(item);
                    }

                    await Task.Delay(100);
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            if (m_mapReduce != null)
                m_mapReduce.Dispose();

            if (m_asyncMapReduce != null && m_asyncMapReduce.Running)
                m_asyncMapReduce.Cancel();

            if (m_asyncMapReduce != null)
                m_asyncMapReduce.Dispose();

            return Task.CompletedTask;
        }
    }

    public class TestFunc : IComputeFunc<int, int>
    {
        public int Excute(int parameter)
        {
            return parameter * 2 - 1;
        }
    }

    public class Test1Func : IComputeFunc<int>
    {
        public string Value { get; set; }

        public int Excute()
        {
            return Value.GetHashCode();
        }
    }
}
