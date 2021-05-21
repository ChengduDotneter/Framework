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
using Common.MessageQueueClient.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Common.RPC.BufferSerializer;
using System.Text;

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
            Task.Factory.StartNew(async () =>
            {
                MQContext mQContext = new MQContext(mqName, new RabbitMqContent() { RoutingKey = mqName });
                IMQProducer<TestMQData> mQProducer = MessageQueueFactory.GetRabbitMQProducer<TestMQData>(ExChangeTypeEnum.Direct);
                int i = 0;
                while (i<10)
                {
                    TestMQData mqData = new TestMQData
                    {
                        Data = "屌你"+i+"次",
                        CreateTime = DateTime.Now
                    };

                    await mQProducer.ProduceAsync(mQContext, mqData);
                    await Task.Delay(500);i++;
                }
            });
            Task.Factory.StartNew(() =>
            {
                MQContext mqContext = new MQContext(mqName, new RabbitMqContent() { RoutingKey = mqName });
                IMQConsumer<TestMQData> mqConsumer = MessageQueueFactory.GetRabbitMQConsumer<TestMQData>(ExChangeTypeEnum.Direct);
                mqConsumer.Subscribe(mqContext);

                mqConsumer.Consume(mqContext, (testData) =>
                {
                    Console.WriteLine(testData.Data);
                    return true;
                });
            });

            mqName = "test_mq1";
            Task.Factory.StartNew(() =>
            {
                MQContext mqContext = new MQContext(mqName, new RabbitMqContent() { RoutingKey = mqName });
                IMQConsumer<TestMQData> mqConsumer = MessageQueueFactory.GetRabbitMQConsumer<TestMQData>(ExChangeTypeEnum.Direct);
                mqConsumer.Subscribe(mqContext);

                mqConsumer.Consume(mqContext, (testData) =>
                {
                    Console.WriteLine(testData.Data);
                    return true;
                });
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
            Random random = new Random();

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
        private ISearchQuery<testname> m_searchQuery2;
        private ISearchQuery<score> m_searchQuery3;
        
        private IEditQuery<Left> m_editQuery;

        private IEditQuery<testcreate> m_editQuery4;
        private readonly ISearchQuery<Right> searchQuery1;
        private readonly IDBResourceContent dBResourceContent;
        private readonly IEditQuery<Right> editQuery1;
        private readonly IEditQuery<testname> m_editQuery2;
        private readonly IEditQuery<score> m_editQuery3;
        private readonly ISSOUserService m_ssoUserService;

        public CCC(ISearchQuery<Left> searchQuery,
                   IEditQuery<Left> editQuery, IEditQuery<testcreate> editQuery4,
                   ISearchQuery<Right> searchQuery1,
                   IDBResourceContent dBResourceContent,
                   IEditQuery<Right> editQuery1,
                       IEditQuery<testname> editQuery2,
                   ISSOUserService ssoUserService,
                   ISearchQuery<testname> searchQuery2,
                   ISearchQuery<score> searchQuery3, IEditQuery<score> editQuery3)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery; m_editQuery4 = editQuery4;
            this.searchQuery1 = searchQuery1; m_searchQuery3 = searchQuery3; m_editQuery3 = editQuery3;
            this.dBResourceContent = dBResourceContent;
            this.editQuery1 = editQuery1; m_editQuery2 = editQuery2;
             m_ssoUserService = ssoUserService; m_searchQuery2 = searchQuery2;
        }
        [HttpGet("testget")]
        public async Task<Left> testget()
        {
            TestService service = new TestService();
           var tsc= service.StartAsync(new CancellationToken() { });
            SSOUserInfo user = m_ssoUserService.GetUser();
            ITransaction transaction1 = m_editQuery4.BeginTransaction();
            try
            {
                await m_editQuery4.SplitBySystemID("s2b").InsertAsync(transaction1, new testcreate() { ID = IDGenerator.NextID(), StudentName = "掉你老母" });
                await m_editQuery2.FilterIsDeleted().UpdateAsync(new testname() { ID = 319502795642566849, name = "叼你大得到爷的" });
                Left data = null;
                var datas = (await m_searchQuery.SplitBySystemID(HttpContext).
                                                 FilterIsDeleted().
                                                 ConditionCache(HttpContext.RequestServices).
                                                 GetAsync(item => item.ID > 0, systemID: HttpContext.Request.Headers["systemID"].FirstOrDefault())).ToArray();

                await editQuery1.SplitBySystemID(HttpContext).MergeAsync(datas: new Right { ClassName = "abc", ID = 123 });

                var a = await m_searchQuery.SplitBySystemID(HttpContext).FilterIsDeleted().GetQueryableAsync();
                var b = await searchQuery1.SplitBySystemID(HttpContext).FilterIsDeleted().GetQueryableAsync();
                testc(a,b);
                var c = from left in a
                        join right in b on left.ClassID equals right.ID
                        where left.ID > 0
                        select new { id = left.ID, name = left.StudentName, name_class = right.ClassName };
                var d = await m_searchQuery2.FilterIsDeleted().GetQueryableAsync(dBResourceContent);
                var g = await m_searchQuery3.FilterIsDeleted().GetQueryableAsync(dBResourceContent);
                var f = from testname in d join sco in g on testname.ID equals sco.stuid where testname.ID > 0 select new { id = testname.ID, name = testname.name, score = sco.Stuscore };
                var test = c.Skip(10).Take(5).ToList();
                var sk = await m_searchQuery.SearchAsync(c);
                var list = await m_searchQuery2.SearchAsync(f);
                var list1 = await m_searchQuery2.SearchAsync(item => item.ID > 0);
                d.Dispose();
                a.Dispose();
                b.Dispose();
                transaction1.Submit();
                tsc.Dispose();
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
                                               ID = IDGenerator.NextID(),
                                               ClassID = random.Next(0, 100),
                                               CreateTime = DateTime.Now,
                                               CreateUserID = -9999,
                                               StudentName = Guid.NewGuid().ToString()
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
            catch (Exception ex)
            {
                transaction1.Rollback();
                throw;
            }
         



        }
        public static IQueryable<Left> testc(ISearchQueryable<Left> lefts, ISearchQueryable<Right> rights)
        {
            var c = from left in lefts
                    join right in rights on left.ClassID equals right.ID
                    where left.ID > 0
                    select left;
            return c;
        }

        [HttpPost]
        public async Task Post()
        {
            using (ITransaction transaction = await m_editQuery.SplitBySystemID("s2b").FilterIsDeleted().BeginTransactionAsync(false))
            {
                //var buff = BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8);
                //buff.Serialize(testme);
                Left[] lefts1 = new Left[20];
                testname le = new testname() {ID= 319502795642550465,name="23456456" };
                await m_editQuery2.FilterIsDeleted().UpdateAsync(le, transaction);
                try
                {
                    throw new DealException("测试报错");
                }
                catch (Exception)
                {

                    throw;
                }
                
                //for (int i = 0; i < lefts1.Length; i++)
                //{
                //    lefts1[i] = new Left
                //    {
                //        ID = IDGenerator.NextID(),
                //        CreateTime = DateTime.Now,
                //        CreateUserID = 9999,
                //        UpdateTime = DateTime.Now,
                //        UpdateUserID = 9999,
                //        IsDeleted = false,
                //        StudentName = $"student_{i}",
                //        ClassID = 9999
                //    };
                //}
                //await m_editQuery2.FilterIsDeleted().UpdateAsync(new testname() { ID = 319502795642566849, name = "叼你大得到爷的" },transaction);
                //await m_editQuery.SplitBySystemID("s2b").FilterIsDeleted().MergeAsync(transaction, lefts1);

                //int pageSize = 20;
                //int readCount = 0;
                //int totalCount = await m_searchQuery.SplitBySystemID("s2b").FilterIsDeleted().CountAsync(transaction);

                //while (readCount <= totalCount)
                //{
                //    IEnumerable<Left> lefts = await m_searchQuery.SplitBySystemID("s2b").FilterIsDeleted().SearchAsync(transaction, startIndex: readCount, count: pageSize);

                //    foreach (var left in lefts)
                //    {
                //        left.StudentName = "deleted";
                //        await m_editQuery.SplitBySystemID("s2b").FilterIsDeleted().UpdateAsync(left, transaction);
                //    }

                //    readCount += lefts.Count();
                //}

                await transaction.SubmitAsync();
            }
        }

        [HttpGet("get")]
        public async Task<Left> Get()
        {
            string mqName = "test_mq1";
           await Task.Factory.StartNew(async () =>
            {
                MQContext mQContext = new MQContext(mqName, new RabbitMqContent() { RoutingKey = mqName });
                IMQProducer<TestMQData> mQProducer = MessageQueueFactory.GetRabbitMQProducer<TestMQData>(ExChangeTypeEnum.Direct);
                int i = 0;
                while (i < 10)
                {
                    TestMQData mqData = new TestMQData
                    {
                        Data = "测试屌你" + i + "次",
                        CreateTime = DateTime.Now
                    };

                    await mQProducer.ProduceAsync(mQContext, mqData);
                    await Task.Delay(500); i++;
                }
            });
            return new Left();
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
        [HttpPut]
        public async void put()
        {
            ITransaction transaction = await m_editQuery.SplitBySystemID("s2b").FilterIsDeleted().BeginTransactionAsync(false);
            try
            {
               
                await m_editQuery2.DeleteAsync(null,319502795642566849);
                await m_editQuery2.UpdateAsync(null, item => item.CreateTime >DateTime.Now.AddDays(-5), new Dictionary<string, object>() { ["name"] = "星期五啦",["CreateUserID"]=520}, transaction);
                var list = await m_searchQuery2.FilterIsDeleted().SearchAsync(item=>item.name.Contains("你")&&item.UpdateUserID==9999);
                transaction.Submit();
            }
            catch(Exception ex)
            {
                transaction.Rollback();
            }
            finally
            {

            }

        }
        private void Sleep()
        {
            Thread.Sleep(5000);
        }
    }
    [Route("wo")]
    [ApiController]
    public class wode: GenericPostController<testname>
    {
        private readonly ISearchQuery<testname> m_searchQuery;
        private readonly IEditQuery<testname> m_editQuery;

        public wode(ISearchQuery<testname> searchQuery,
            IEditQuery<testname> editQuery,
            ISSOUserService ssoUserService) :base(editQuery,ssoUserService)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
        }
        protected override async Task DoPost(long id,testname test)
        {
           base.DoPost(id,test);
        }
        [HttpGet]
        public async Task<IActionResult> get()
        {
            return Ok(await m_searchQuery.FilterIsDeleted().SearchAsync(item=>item.ID>0));
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
            MQContext mQContext = new MQContext("testmq", new RabbitMqContent() { RoutingKey = "testmq" });
            using IMQProducer<TestMQData> producer = MessageQueueFactory.GetRabbitMQProducer<TestMQData>(ExChangeTypeEnum.Direct);

            for (int i = 0; i < 10; i++)
            {
                await producer.ProduceAsync(mQContext, new TestMQData { Data = (i).ToString(), CreateTime = DateTime.Now });
            }
        }

        [HttpGet("consume")]
        public async Task Consume()
        {
            MQContext mQContext = new MQContext("testmq", new RabbitMqContent() { RoutingKey = "testmq" });
            using IMQBatchConsumer<TestMQData> consumer = MessageQueueFactory.GetRabbitMQBatchConsumer<TestMQData>(ExChangeTypeEnum.Direct);
            consumer.Subscribe(mQContext);
            
            consumer.Consume(mQContext, (datas) =>
            {
                foreach (var data in datas)
                {
                    Console.WriteLine("1号消费者："+data.Data+":"+data.CreateTime);
                }
                
                return datas.Last().Data == "9";
            }, TimeSpan.FromSeconds(1), 10);
            
           // await Task.Delay(int.MaxValue);
        }
        [HttpGet("consume1")]
        public async Task Consume1()
        {
            MQContext mQContext = new MQContext("testmq", new RabbitMqContent() { RoutingKey = "testm" });
            using IMQBatchConsumer<TestMQData> consumer = MessageQueueFactory.GetRabbitMQBatchConsumer<TestMQData>(ExChangeTypeEnum.Direct);
            consumer.Subscribe(mQContext);

            consumer.Consume(mQContext, (datas) =>
            {
                    foreach (var data in datas)
                    {
                        Console.WriteLine("2号消费者"+data.Data + ":" + data.CreateTime);
                    }
                    return true;

            }, TimeSpan.FromSeconds(1), 10);

            await Task.Delay(int.MaxValue);
        }
    }
}