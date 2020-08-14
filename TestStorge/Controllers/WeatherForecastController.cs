using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestStorge.Controllers
{
    [Route("tccstorge")]
    public class TCCStorgeController : TransactionTCCController<StockInfoCousme, StockInfoCousme>
    {
        private readonly ISearchQuery<StockInfo> m_stockInfoSearchQuery;
        private readonly IEditQuery<StockInfo> m_stockInfoEditQuery;

        public TCCStorgeController(
            IEditQuery<StockInfoCousme> editQuery,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ITccTransactionManager tccTransactionManager,
            ISearchQuery<StockInfo> stockInfoSearchQuery,
            IEditQuery<StockInfo> stockInfoEditQuery) : base(editQuery, httpClientFactory, httpContextAccessor, tccTransactionManager)
        {
            m_stockInfoSearchQuery = stockInfoSearchQuery;
            m_stockInfoEditQuery = stockInfoEditQuery;
        }

        protected override async Task<object> DoTry(long tccID, ITransaction transaction, StockInfoCousme data)
        {
            IEnumerable<string> commodityNames = data.StockInfos.Select(item => item.CommodityName);
            IEnumerable<StockInfo> currentStockInfos = await m_stockInfoSearchQuery.FilterIsDeleted().SearchAsync(item => commodityNames.Contains(item.CommodityName), transaction: transaction);

            foreach (StockInfo stockInfo in data.StockInfos)
            {
                StockInfo currentStock = currentStockInfos.FirstOrDefault(item => item.CommodityName == stockInfo.CommodityName);

                if (currentStock == null)
                    throw new DealException("库存不存在");

                if (currentStock.Number < stockInfo.Number)
                    throw new DealException("库存不足");

                currentStock.Number -= stockInfo.Number;
            }

            await m_stockInfoEditQuery.FilterIsDeleted().MergeAsync(transaction, currentStockInfos.ToArray());
            return data;
        }
    }

    public class StockInfoCousme : ViewModelBase
    {
        [SugarColumn(IsIgnore = true)]
        public IEnumerable<StockInfo> StockInfos { get; set; }
    }

    public class StockInfo : ViewModelBase
    {
        [SugarColumn(Length = 150, IsNullable = false, ColumnDescription = "商品名称")]
        [QuerySqlField]
        public string CommodityName { get; set; }

        [SugarColumn(DecimalDigits = 5, Length = 18, IsNullable = false, ColumnDescription = "数量")]
        [QuerySqlField]
        public decimal? Number { get; set; }
    }
}