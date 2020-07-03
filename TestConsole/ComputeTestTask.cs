using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Compute;
using Common.DAL;
using Microsoft.Extensions.Hosting;
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
            Console.WriteLine(m_compute.Bordercast(new TestFunc(), 5).Sum());

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
            return parameter * 2;
        }
    }
}
