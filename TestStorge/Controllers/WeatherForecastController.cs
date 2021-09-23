using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedVariable

namespace TestStorge.Controllers
{
    //[Route("tccstorge")]
    //public class TCCStorgeController : TransactionTCCController<StockInfoCousme, StockInfoCousme>
    //{
    //    private readonly ISearchQuery<StockInfo> m_stockInfoSearchQuery;
    //    private readonly IEditQuery<StockInfo> m_stockInfoEditQuery;

    //    public TCCStorgeController(
    //        IEditQuery<StockInfoCousme> editQuery,
    //        IHttpClientFactory httpClientFactory,
    //        IHttpContextAccessor httpContextAccessor,
    //        ITccTransactionManager tccTransactionManager,
    //        ISearchQuery<StockInfo> stockInfoSearchQuery,
    //        IEditQuery<StockInfo> stockInfoEditQuery) : base(editQuery, httpClientFactory, httpContextAccessor, tccTransactionManager)
    //    {
    //        m_stockInfoSearchQuery = stockInfoSearchQuery;
    //        m_stockInfoEditQuery = stockInfoEditQuery;
    //    }

    //    protected override async Task<object> DoTry(long tccID, ITransaction transaction, StockInfoCousme data)
    //    {
    //        IEnumerable<string> commodityNames = data.StockInfos.Select(item => item.CommodityName);
    //        IEnumerable<StockInfo> currentStockInfos = await m_stockInfoSearchQuery.FilterIsDeleted().SearchAsync(item => commodityNames.Contains(item.CommodityName), transaction: transaction);

    //        foreach (StockInfo stockInfo in data.StockInfos)
    //        {
    //            StockInfo currentStock = currentStockInfos.FirstOrDefault(item => item.CommodityName == stockInfo.CommodityName);

    //            if (currentStock == null)
    //                throw new DealException("库存不存在");

    //            if (currentStock.Number < stockInfo.Number)
    //                throw new DealException("库存不足");

    //            currentStock.Number -= stockInfo.Number;
    //        }

    //        await m_stockInfoEditQuery.FilterIsDeleted().MergeAsync(transaction, currentStockInfos.ToArray());
    //        return data;
    //    }
    //}

    [Route("testssss")]
    public class TestssssController : ControllerBase
    {
        private readonly ISearchQuery<StockInfo> m_stockInfoSearchQuery;
        private readonly IEditQuery<StockInfo> m_stockInfoEditQuery;
        private readonly IDBResourceContent m_dbResourceContent;

        public TestssssController(ISearchQuery<StockInfo> stockInfoSearchQuery,
            IEditQuery<StockInfo> stockInfoEditQuery,
            IDBResourceContent dbResourceContent)
        {
            m_stockInfoSearchQuery = stockInfoSearchQuery;
            m_stockInfoEditQuery = stockInfoEditQuery;
            m_dbResourceContent = dbResourceContent;
        }

        [HttpGet]
        public IActionResult Get()
        {
            IEnumerable<StockInfo> search;

            using (ISearchQueryable<StockInfo> stockInfoQuery = m_stockInfoSearchQuery.FilterIsDeleted().GetQueryable(dbResourceContent: m_dbResourceContent))
            using (ISearchQueryable<StockInfo> stockInfoQuery1 = m_stockInfoSearchQuery.FilterIsDeleted().GetQueryable(dbResourceContent: m_dbResourceContent))
            {
                search = m_stockInfoSearchQuery.FilterIsDeleted().Search(dbResourceContent: m_dbResourceContent);

                //Thread.Sleep(500);
            }

            return Ok(search);
        }
    }

    [Route("testaaaa")]
    public class TestaaaaController : ControllerBase
    {
        private readonly ISearchQuery<StockInfo> m_stockInfoSearchQuery;
        private readonly IEditQuery<StockInfo> m_stockInfoEditQuery;
        private readonly IDBResourceContent m_dbResourceContent;

        public TestaaaaController(ISearchQuery<StockInfo> stockInfoSearchQuery,
            IEditQuery<StockInfo> stockInfoEditQuery,
            IDBResourceContent dbResourceContent)
        {
            m_stockInfoSearchQuery = stockInfoSearchQuery;
            m_stockInfoEditQuery = stockInfoEditQuery;
            m_dbResourceContent = dbResourceContent;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            IEnumerable<StockInfo> search;

            using (ISearchQueryable<StockInfo> stockInfoQuery = await m_stockInfoSearchQuery.FilterIsDeleted().GetQueryableAsync(dbResourceContent: m_dbResourceContent))
            using (ISearchQueryable<StockInfo> stockInfoQuery1 = await m_stockInfoSearchQuery.FilterIsDeleted().GetQueryableAsync(dbResourceContent: m_dbResourceContent))
            {
                search = await m_stockInfoSearchQuery.FilterIsDeleted().SearchAsync(dbResourceContent: m_dbResourceContent);

                Thread.Sleep(500);
            }

            return Ok(search);
        }
    }

    [Route("test2")]
    public class Test2Controller : ControllerBase
    {
        private readonly ISearchQuery<StockInfo> m_stockInfoSearchQuery;

        public Test2Controller(ISearchQuery<StockInfo> stockInfoSearchQuery)
        {
            m_stockInfoSearchQuery = stockInfoSearchQuery;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(m_stockInfoSearchQuery.KeyCache(HttpContext.RequestServices).Get(456));
        }
    }

    //public class StockInfoCousme : ViewModelBase
    //{
    //    public IEnumerable<StockInfo> StockInfos { get; set; }
    //}

    public class StockInfo : ViewModelBase
    {
        [Column(CanBeNull = true)]
        public decimal? Number { get; set; }
    }
}