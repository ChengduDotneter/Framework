using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.MessageQueueClient;
using Common.Model;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TestOrder.Controllers
{
    [Route("test")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public bool Test()
        {
            MessageQueueFactory.GetRabbitMQProducer<MqSendMessage>(new List<string>() { "TestMQ" }, "TestMQ", ExChangeTypeEnum.fanout).Produce(new MQContext("TestMQ", ""), new MqSendMessage() { });
            return true;
        }
    }

    public class MqSendMessage : IMQData
    {
        public DateTime CreateTime => DateTime.Now;
    }

    [Route("order")]
    public class TCCOrderController : TransactionTCCController<OrderInfo, OrderInfo>
    {
        public readonly ISearchQuery<OrderInfo> m_orderSearchQuery;
        public readonly IEditQuery<OrderInfo> m_orderEditQuery;
        public readonly IEditQuery<OrderCommodity> m_orderCommodityEditQuery;

        public TCCOrderController(
            IEditQuery<OrderInfo> orderEditQuery,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            ITccTransactionManager tccTransactionManager,
            ISearchQuery<OrderInfo> orderSearchQuery, IEditQuery<OrderCommodity> orderCommodityEditQuery) : base(orderEditQuery, httpClientFactory, httpContextAccessor, tccTransactionManager)
        {
            m_orderSearchQuery = orderSearchQuery;
            m_orderEditQuery = orderEditQuery;
            m_orderCommodityEditQuery = orderCommodityEditQuery;
        }

        protected override async Task<object> DoTry(long tccID, ITransaction transaction, OrderInfo order)
        {
            order.ID = IDGenerator.NextID();
            order.OrderNo = IDGenerator.NextID().ToString();
            order.IsDeleted = false;
            order.CreateTime = DateTime.Now;
            order.CreateUserID = -9999;

            if (await m_orderSearchQuery.FilterIsDeleted().CountAsync(transaction: transaction, item => item.OrderNo == order.OrderNo && item.ID != order.ID) > 0)
                throw new DealException("订单已存在");

            await m_orderEditQuery.FilterIsDeleted().InsertAsync(transaction, order);

            foreach (OrderCommodity orderCommodity in order.OrderCommodities)
            {
                orderCommodity.ID = IDGenerator.NextID();
                orderCommodity.IsDeleted = false;
                orderCommodity.CreateTime = DateTime.Now;
                orderCommodity.CreateUserID = -9999;
                orderCommodity.OrderID = order.ID;
            }

            await m_orderCommodityEditQuery.FilterIsDeleted().InsertAsync(transaction, order.OrderCommodities.ToArray());
            return order;
        }
    }

    public class OrderInfo : ViewModelBase
    {
        [QuerySqlField]
        public string OrderNo { get; set; }

        public IEnumerable<OrderCommodity> OrderCommodities { get; set; }
    }

    public class OrderCommodity : ViewModelBase
    {
        [QuerySqlField]
        public string CommodityName { get; set; }

        [QuerySqlField]
        public decimal? Number { get; set; }

        [QuerySqlField]
        public long OrderID { get; set; }
    }
}