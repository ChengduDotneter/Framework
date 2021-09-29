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
using Common.MessageQueueClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Common.MessageQueueClient.RabbitMQ;
using Newtonsoft.Json.Linq;

// ReSharper disable NotAccessedField.Local

// ReSharper disable UnusedVariable
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable UnusedMember.Local
#pragma warning disable 162

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

    public class TestMQData : IMQData
    {
        public string Data { get; set; }
        public DateTime CreateTime { get; set; }
    }

    public class TestService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            string mqName = "test_mq";

            Task.Factory.StartNew(() =>
            {
                IMQConsumer<TestMQData> mqConsumer = MessageQueueFactory.GetRabbitMQConsumer<TestMQData>(mqName, mqName);
                mqConsumer.Subscribe();

                mqConsumer.Consume((testData) =>
                {
                    Console.WriteLine(testData.Data);
                    return true;
                });
            });

            Task.Factory.StartNew(async () =>
            {
                IMQProducer<TestMQData> mQProducer = MessageQueueFactory.GetRabbitMQProducer<TestMQData>(mqName, mqName);

                while (true)
                {
                    TestMQData mqData = new TestMQData
                    {
                        Data = Guid.NewGuid().ToString(),
                        CreateTime = DateTime.Now
                    };

                    await mQProducer.ProduceAsync(mqData);
                    await Task.Delay(100);
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
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

                    switch (index % 3)
                    {
                        case 0:
                            await Test1(transaction);
                            break;

                        case 1:
                            await Test2(transaction);
                            break;

                        case 2:
                            await Test1(transaction);
                            await Test2(transaction);
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

        [HttpGet("insert")]
        public Task DoInsert()
        {
            StockInfo[] stockInfos = new StockInfo[]
            {
                new StockInfo() { ID = IDGenerator.NextID(), CreateUserID = 0, CreateTime = DateTime.Now, CommodityID = 1, Count = 400 },
                new StockInfo() { ID = IDGenerator.NextID(), CreateUserID = 0, CreateTime = DateTime.Now, CommodityID = 2, Count = 400 }
            };

            m_editQuery.Insert(null, stockInfos);
            return Task.CompletedTask;
        }

        private async Task Test1(ITransaction transaction)
        {
            Random random = new Random();

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
            if (stockInfo_1.Count < order_1.Count)
                throw new DealException("1 库存不够。");

            stockInfo_1.Count -= order_1.Count;
            await m_editQuery.UpdateAsync(stockInfo_1, transaction);
            await m_orderInfoEditQuery.InsertAsync(transaction, order_1);
        }

        private async Task Test2(ITransaction transaction)
        {
            //Random random = new Random();

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
            await m_editQuery.UpdateAsync(stockInfo_2, transaction);
            await m_orderInfoEditQuery.InsertAsync(transaction, order_2);
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
        private readonly ISearchQuery<Right> searchQuery1;
        private readonly IDBResourceContent dBResourceContent;
        private readonly IEditQuery<Right> editQuery1;
        private readonly ISSOUserService m_ssoUserService;

        public CCC(ISearchQuery<Left> searchQuery,
                   IEditQuery<Left> editQuery,
                   ISearchQuery<Right> searchQuery1,
                   IDBResourceContent dBResourceContent,
                   IEditQuery<Right> editQuery1,
                   ISSOUserService ssoUserService)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
            this.searchQuery1 = searchQuery1;
            this.dBResourceContent = dBResourceContent;
            this.editQuery1 = editQuery1;
            m_ssoUserService = ssoUserService;
        }

        [HttpPost]
        public async Task Post()
        {
            using (ITransaction transaction = await m_editQuery.SplitBySystemID("s2b").FilterIsDeleted().BeginTransactionAsync(false))
            {
                Left[] lefts1 = new Left[20];

                for (int i = 0; i < lefts1.Length; i++)
                {
                    lefts1[i] = new Left
                    {
                        ID = IDGenerator.NextID(),
                        CreateTime = DateTime.Now,
                        CreateUserID = 9999,
                        UpdateTime = DateTime.Now,
                        UpdateUserID = 9999,
                        IsDeleted = false,
                        StudentName = $"student_{i}",
                        ClassID = 9999
                    };
                }

                await m_editQuery.SplitBySystemID("s2b").FilterIsDeleted().MergeAsync(transaction, lefts1);

                int pageSize = 20;
                int readCount = 0;
                int totalCount = await m_searchQuery.SplitBySystemID("s2b").FilterIsDeleted().CountAsync(transaction);

                while (readCount <= totalCount)
                {
                    IEnumerable<Left> lefts = await m_searchQuery.SplitBySystemID("s2b").FilterIsDeleted().SearchAsync(transaction, startIndex: readCount, count: pageSize);

                    foreach (var left in lefts)
                    {
                        left.StudentName = "deleted";
                        await m_editQuery.SplitBySystemID("s2b").FilterIsDeleted().UpdateAsync(left, transaction);
                    }

                    readCount += lefts.Count();
                }

                await transaction.SubmitAsync();
            }
        }

        [HttpGet]
        public async Task<Left> Get()
        {
            SSOUserInfo user = m_ssoUserService.GetUser();

            Left data = null;

            var datas = (await m_searchQuery.SplitBySystemID(HttpContext).
                                             FilterIsDeleted().
                                             ConditionCache(HttpContext.RequestServices).
                                             GetAsync(item => item.ID > 0, systemID: HttpContext.Request.Headers["systemID"].FirstOrDefault())).ToArray();

            await editQuery1.SplitBySystemID(HttpContext).MergeAsync(datas: new Right { ClassName = "abc", ID = 123 });

            var a = await m_searchQuery.SplitBySystemID(HttpContext).FilterIsDeleted().GetQueryableAsync(dBResourceContent);
            var b = await searchQuery1.SplitBySystemID(HttpContext).FilterIsDeleted().GetQueryableAsync(dBResourceContent);

            var c = from left in a
                    join right in b on left.ClassID equals right.ID
                    where left.ID > 0
                    select new { id = left.ID, name = left.StudentName, name_class = right.ClassName };

            a.Dispose();
            b.Dispose();

            var sk = await m_searchQuery.SearchAsync(c);


            return null;

            //return await m_searchQuery.SplitBySystemID(HttpContext).FilterIsDeleted().KeyCache(HttpContext.RequestServices).GetAsync(289340350911224001L, systemID: HttpContext.Request.Headers["systemID"].FirstOrDefault());

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

                    m_editQuery.Update(data, transaction);

                    m_editQuery.FilterIsDeleted().Delete(transaction, data.ID);

                    m_editQuery.Update(item => item.ID == data.ID, new Dictionary<string, object>() { [nameof(Left.UpdateTime)] = DateTime.Now }, transaction);

                    m_editQuery.Insert(transaction,
                                       new Left()
                                       {
                                           ID = IDGenerator.NextID(), ClassID = random.Next(0, 100), CreateTime = DateTime.Now, CreateUserID = -9999, StudentName = Guid.NewGuid().ToString()
                                       });

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


    [MessageProcessorRoute("abcds")]
    public class ABCD : MessageProcessor<string>
    {
        private readonly ITest m_test;
        private readonly ISSOUserService m_ssoUserService;

        public override async Task RecieveMessage(string parameter, CancellationToken cancellationToken)
        {
            SSOUserInfo ssoUserInfo = m_ssoUserService.GetUser();
            m_test.Test();

            while (!cancellationToken.IsCancellationRequested)
            {
                SendMessage($"{Environment.TickCount}_{parameter}");
                await Task.Delay(1000);
            }
        }

        public ABCD(string identity, ITest test, ILogHelper logHelper, ISSOUserService ssoUserService) : base(identity, logHelper)
        {
            m_test = test;
            m_ssoUserService = ssoUserService;
        }
    }

    public interface IScopeInstance : IDisposable
    {
        string Status { get; }
    }

    public class ScopeInstance : IScopeInstance
    {
        public ScopeInstance()
        {
            Status = "running";
        }

        public void Dispose()
        {
            Status = "disposed";
        }

        public string Status { get; private set; }
    }

    [Route("scopetest")]
    [ApiController]
    public class ScopeTest : ControllerBase
    {
        private IServiceProvider m_serviceProvider;
        private IScopeInstance m_scopeInstance;

        // ReSharper disable once NotAccessedField.Local
        private readonly IDBResourceContent m_dbResourceContent;

        public ScopeTest(IServiceProvider serviceProvider, IScopeInstance scopeInstance, IDBResourceContent dbResourceContent)
        {
            m_serviceProvider = serviceProvider;
            m_scopeInstance = scopeInstance;
            m_dbResourceContent = dbResourceContent;
        }

        [HttpGet]
        public void Get()
        {
            IServiceScope serviceScope = m_serviceProvider.CreateScope();

            Task.Factory.StartNew(async (state) =>
            {
                IScopeInstance scopeInstance = ((IServiceScope)state).ServiceProvider.GetRequiredService<IScopeInstance>();

                while (true)
                {
                    Console.WriteLine(scopeInstance.Status);
                    Console.WriteLine(m_scopeInstance.Status);
                    await Task.Delay(1000);
                }
            }, serviceScope);
        }
    }

    [Route("mqtest")]
    [ApiController]
    public class MqTest : ControllerBase
    {
        [HttpGet("produce")]
        public async Task Produce()
        {
            using IMQProducer<TestMQData> producer = MessageQueueFactory.GetRabbitMQProducer<TestMQData>("testmq", "testmq");

            for (int i = 0; i < 10; i++)
            {
                await producer.ProduceAsync(new TestMQData { Data = (i + 1).ToString(), CreateTime = DateTime.Now });
            }
        }

        [HttpGet("consume")]
        public async Task Consume()
        {
            using IMQBatchConsumer<TestMQData> consumer = MessageQueueFactory.GetRabbitMQBatchConsumer<TestMQData>("testmq", "testmq");
            consumer.Subscribe();

            consumer.Consume((datas) =>
            {
                foreach (var data in datas)
                {
                    Console.WriteLine(data.Data);
                }

                return datas.Last().Data == "3";
            }, TimeSpan.FromSeconds(1), 3);

            await Task.Delay(int.MaxValue);
        }
    }

    public class CommodityArchives : ViewModelBase
    {
    }

    [Route("connectiontest")]
    [ApiController]
    public class ConnectionTest : ControllerBase
    {
        private readonly IEditQuery<CommodityArchives> m_editQuery;

        public ConnectionTest(IEditQuery<CommodityArchives> editQuery)
        {
            m_editQuery = editQuery;
        }

        [HttpGet]
        public async Task<string> Get()
        {
            try
            {
                using (ITransaction transaction = await m_editQuery.SplitBySystemID("s2b").BeginTransactionAsync())
                {
                    try
                    {
                        await m_editQuery.SplitBySystemID("s2b").InsertAsync(datas: new CommodityArchives { ID = IDGenerator.NextID() });
                        await transaction.SubmitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }

                return "ok";
            }
            catch (Exception exception)
            {
                return JsonConvert.SerializeObject(exception);
            }
        }
    }

    internal class MessageData : IMQData
    {
        public string MessageType { get; set; }
        public JArray data { get; set; }
        public DateTime CreateTime { get; set; }
    }

    [Route("RabbitTest")]
    public class RabbitTestController : ControllerBase
    {
        private readonly ILogHelper m_logHelper;

        public RabbitTestController(ILogHelper logHelper)
        {
            m_logHelper = logHelper;
        }

        [HttpGet]
        public async Task RabbitTest(long wareHouseID)
        {
            try
            {
                IMQBatchConsumer<MessageData> batchConsumer = MessageQueueFactory.GetRabbitMQBatchConsumer<MessageData>(new RabbitMQConfig
                {
                    QueueName = "YC_CHANNEL_GOODS_SYNC_SHOPHGLYC",
                    HostName = "192.168.10.211",
                    Port = 5672,
                    UserName = "admin",
                    Password = "123456",
                    RequestedHeartbeat = 60,
                    RoutingKey = "YC_CHANNEL_GOODS_SYNC_SHOPHGLYC",
                    ExChangeType = ExChangeTypeEnum.Direct
                });

                await batchConsumer.ConsumeAsync(Consume, pullingTimeSpan: TimeSpan.FromSeconds(30), pullingCount: 2);
                batchConsumer.Subscribe();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private Task<bool> Consume(IEnumerable<MessageData> arg)
        {
            return Task.FromResult(false);
        }
    }

    [Route("backgroundworker")]
    public class BackgroundWorkerTest : ControllerBase
    {
        private readonly ISearchQuery<CommodityArchives> m_searchQuery;
        private readonly IEditQuery<CommodityArchives> m_editQuery;
        private readonly ICreateTableQuery m_createTableQuery;
        private readonly IDBResourceContent m_dbResourceContent;
        private readonly IBackgroundWorkerService m_backgroundWorkerService;

        public BackgroundWorkerTest(ISearchQuery<CommodityArchives> searchQuery,
                                    IEditQuery<CommodityArchives> editQuery,
                                    IDBResourceContent dbResourceContent,
                                    IBackgroundWorkerService backgroundWorkerService,
                                    ICreateTableQuery createTableQuery)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
            m_dbResourceContent = dbResourceContent;
            m_backgroundWorkerService = backgroundWorkerService;
            m_createTableQuery = createTableQuery;
        }

        [HttpGet("{id}")]
        public async Task Get(long id)
        {
            DateTime createTime = DateTime.Now;

            await m_backgroundWorkerService.AddWork<long, DateTime, ISearchQuery<CommodityArchives>, IEditQuery<CommodityArchives>, IDBResourceContent>(async (id, createTime, searchQuery, editQuery, dbResourceContent) =>
            {
                await Task.Delay(10000);
                
                using (ITransaction transaction = await editQuery.SplitBySystemID("s2b").BeginTransactionAsync())
                {
                    try
                    {
                        await m_editQuery.SplitBySystemID("s2b").InsertAsync(datas: new CommodityArchives { ID = id, CreateTime = createTime });
                        await transaction.SubmitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }

                    Console.WriteLine(await searchQuery.SplitBySystemID("s2b").CountAsync(transaction: transaction));
                }

                Console.WriteLine(await searchQuery.SplitBySystemID("s2b").CountAsync(dbResourceContent: dbResourceContent));
            }, new { id, createTime });
        }
    }
}