using Common;
using Common.DAL;
using Common.Lock;
using Common.Log;
using Common.Model;
using Common.ServiceCommon;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TestWebAPI.Controllers
{
    public class TCCTestData : ViewModelBase
    {
        public string Data { get; set; }
    }


    public class OrderInfo : ViewModelBase
    {
        public long CommodityID { get; set; }

        public int Count { get; set; }
    }

    public class StockInfo : ViewModelBase
    {
        public int Count { get; set; }

        public long CommodityID { get; set; }

    }

    [ApiController]
    [Route("testtranscation")]
    public class TestTranscation : ControllerBase
    {
        private readonly ISearchQuery<StockInfo> m_searchQuery;
        private readonly IEditQuery<StockInfo> m_editQuery;
        private readonly IEditQuery<OrderInfo> m_orderInfoEditQuery;

        public TestTranscation(ISearchQuery<StockInfo> searchQuery,
                               IEditQuery<StockInfo> editQuery,
                               IEditQuery<OrderInfo> orderInfoEditQuery)
        {
            m_editQuery = editQuery;
            m_searchQuery = searchQuery;
            m_orderInfoEditQuery = orderInfoEditQuery;
        }

        [HttpGet]
        public async Task Do()
        {
            using (ITransaction transaction = await m_editQuery.BeginTransactionAsync())
            {
                try
                {
                    Random random = new Random();

                    int index = random.Next(0, 1000);

                    IList<OrderInfo> orderInfds = new List<OrderInfo>();

                    switch (index % 2)
                    {
                        case 0:
                            StockInfo stockInfo_1 = (await m_searchQuery.FilterIsDeleted().SearchAsync(transaction, item => item.CommodityID == 1, forUpdate: true)).FirstOrDefault();
                            OrderInfo order_1 = new OrderInfo()
                            {
                                ID = IDGenerator.NextID(),
                                CreateUserID = -9999,
                                CreateTime = DateTime.Now,
                                CommodityID = stockInfo_1.CommodityID,
                                Count = 4,
                                //Count = random.Next(1, 9)
                            };
                            if (stockInfo_1.Count < order_1.Count )
                                throw new DealException("1 库存不够。");
                            stockInfo_1.Count -= order_1.Count;
                            orderInfds.Add(order_1);
                            await m_editQuery.UpdateAsync(stockInfo_1, transaction);
                            await m_orderInfoEditQuery.InsertAsync(transaction, orderInfds.ToArray());
                            break;
                        case 1:
                            StockInfo stockInfo_2 = (await m_searchQuery.FilterIsDeleted().SearchAsync(transaction, item => item.CommodityID == 2, forUpdate: true)).FirstOrDefault();
                            OrderInfo order_2 = new OrderInfo()
                            {
                                ID = IDGenerator.NextID(),
                                CreateUserID = -9999,
                                CreateTime = DateTime.Now,
                                CommodityID = stockInfo_2.CommodityID,
                                Count = 4,
                                //Count = random.Next(1, 9)
                            };
                            if (stockInfo_2.Count < order_2.Count)
                                throw new DealException("2 库存不够。");
                            stockInfo_2.Count -= order_2.Count;
                            orderInfds.Add(order_2);
                            await m_editQuery.UpdateAsync(stockInfo_2, transaction);
                            await m_orderInfoEditQuery.InsertAsync(transaction, orderInfds.ToArray());
                            break;

                    }

                    transaction.Submit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    [ApiController]
    [Route("testlock")]
    public class TestLock : ControllerBase
    {
        private readonly ILock @lock;
        public TestLock()
        {
            @lock = LockFactory.GetRedisLock();
        }

        [HttpGet]
        public void Lock()
        {
            Random random = new Random();
            int id = random.Next(0, 999999);

            if (@lock.AcquireWriteLockWithResourceKeys("classinfo", id.ToString(), 0, 5000, "1"))
                LogHelperFactory.GetLog4netLogHelper().Info("testlock", $"{id} 申请成功");
            else
            {
                @lock.Release(id.ToString());
                LogHelperFactory.GetLog4netLogHelper().Info("testlock", $"{id} 申请失败");
            }
        }

    }

    [Route("abc")]
    [ApiController]
    public class CCC : ControllerBase
    {
        private ISearchQuery<Left> m_searchQuery;
        private IEditQuery<Left> m_editQuery;

        public CCC(ISearchQuery<Left> searchQuery, IEditQuery<Left> editQuery)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
        }

        [HttpGet]
        public async Task<Left> Get()
        {
            Left data = null;

            var datas = m_searchQuery.FilterIsDeleted().Search().ToArray();

            Random random = new Random();

            long id = datas[random.Next(0, datas.Length - 1)].ID;


            using (ITransaction transaction = m_editQuery.BeginTransaction())
            {
                try
                {
                    data = m_searchQuery.FilterIsDeleted().Get(id, transaction);

                    if (data == null)
                        throw new Exception();

                    var datas2 = m_searchQuery.FilterIsDeleted().Search(transaction, item => item.ClassID == data.ClassID);

                    data.UpdateUserID = random.Next(1000, 9999);
                    data.UpdateTime = DateTime.Now;

                    m_editQuery.Update(data, transaction);

                    m_editQuery.FilterIsDeleted().Delete(transaction, data.ID);

                    m_editQuery.Update(item => item.ID == data.ID, new Dictionary<string, object>() { [nameof(Left.IsDeleted)] = false }, transaction);

                    transaction.Submit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return data;
        }

        private void Sleep()
        {
            Thread.Sleep(5000);
        }
    }
}