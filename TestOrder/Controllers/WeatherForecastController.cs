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

namespace TestOrder.Controllers
{
    [Route("order")]
    public class TCCOrderController : TransactionTCCController<OrderInfo>
    {
        public readonly ISearchQuery<OrderInfo> m_orderSearchQuery;
        public readonly IEditQuery<OrderInfo> m_orderEditQuery;
        public readonly IEditQuery<OrderCommodity> m_orderCommodityEditQuery;

        public TCCOrderController(
            IEditQuery<OrderInfo> orderEditQuery,
            IHttpContextAccessor httpContextAccessor,
            ITccTransactionManager tccTransactionManager,
            ISearchQuery<OrderInfo> orderSearchQuery, IEditQuery<OrderCommodity> orderCommodityEditQuery) : base(orderEditQuery, httpContextAccessor, tccTransactionManager)
        {
            m_orderSearchQuery = orderSearchQuery;
            m_orderEditQuery = orderEditQuery;
            m_orderCommodityEditQuery = orderCommodityEditQuery;
        }

        protected override void DoTry(long tccID, ITransaction transaction, OrderInfo order)
        {
            order.ID = IDGenerator.NextID();
            order.OrderNo = IDGenerator.NextID().ToString();
            order.IsDeleted = false;
            order.CreateTime = DateTime.Now;
            order.CreateUserID = -9999;

            if (m_orderSearchQuery.FilterIsDeleted().Count(item => item.OrderNo == order.OrderNo && item.ID != order.ID, transaction: transaction) > 0)
                throw new DealException("订单已存在");

            m_orderEditQuery.FilterIsDeleted().Insert(transaction, order);

            foreach (OrderCommodity orderCommodity in order.OrderCommodities)
            {
                orderCommodity.ID = IDGenerator.NextID();
                orderCommodity.IsDeleted = false;
                orderCommodity.CreateTime = DateTime.Now;
                orderCommodity.CreateUserID = -9999;
                orderCommodity.OrderID = order.ID;
            }

            m_orderCommodityEditQuery.FilterIsDeleted().Insert(transaction, order.OrderCommodities.ToArray());
        }
    }

    public class OrderInfo : ViewModelBase
    {
        [SugarColumn(Length = 150, IsNullable = false, ColumnDescription = "订单编号")]
        [QuerySqlField]
        public string OrderNo { get; set; }

        [SugarColumn(IsIgnore = true)]
        public IEnumerable<OrderCommodity> OrderCommodities { get; set; }
    }

    public class OrderCommodity : ViewModelBase
    {
        [SugarColumn(Length = 150, IsNullable = false, ColumnDescription = "商品名称")]
        [QuerySqlField]
        public string CommodityName { get; set; }

        [SugarColumn(DecimalDigits = 5, Length = 18, IsNullable = false, ColumnDescription = "数量")]
        [QuerySqlField]
        public decimal? Number { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "订单ID")]
        [QuerySqlField]
        public long OrderID { get; set; }
    }
}
