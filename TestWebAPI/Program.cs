using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Apache.Ignite.Core.Cache;
using Apache.Ignite.Linq;
using Common;
using Common.DAL;
using Common.Log;
using Common.ServiceCommon;
using LinqToDB.Mapping;
using MicroService.StorageService.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestWebAPI
{
    //[MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    //public class Left : IEntity
    //{
    //    [SqlSugar.SugarColumn(IsPrimaryKey = true)]
    //    [Apache.Ignite.Core.Cache.Configuration.QuerySqlField(IsIndexed = true)]
    //    [PrimaryKey]
    //    public long ID { get; set; }

    //    [SqlSugar.SugarColumn]
    //    [Apache.Ignite.Core.Cache.Configuration.QuerySqlField]
    //    [Column]
    //    public string StudentName { get; set; }

    //    [SqlSugar.SugarColumn]
    //    [Apache.Ignite.Core.Cache.Configuration.QuerySqlField]
    //    [Column]
    //    public long ClassID { get; set; }
    //}

    //[MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    //public class Right : IEntity
    //{
    //    [SqlSugar.SugarColumn(IsPrimaryKey = true)]
    //    [Apache.Ignite.Core.Cache.Configuration.QuerySqlField(IsIndexed = true)]
    //    [PrimaryKey]
    //    public long ID { get; set; }

    //    [SqlSugar.SugarColumn]
    //    [Apache.Ignite.Core.Cache.Configuration.QuerySqlField]
    //    [Column]
    //    public string ClassName { get; set; }
    //}

    public class Program
    {
        private static ILogHelper logHelper = LogHelperFactory.GetKafkaLogHelper();

        public static void Main(string[] args)
        {
            ConfigManager.Init("Development");

            ////IEditQuery<Left> leftEditQuery = DaoFactory.GetEditMongoDBQuery<Left>();
            //IEditQuery<Left> leftEditQuery = DaoFactory.GetEditLinq2DBQuery<Left>(false);

            ////IEditQuery<Right> rightEditQuery = DaoFactory.GetEditMongoDBQuery<Right>();
            //IEditQuery<Right> rightEditQuery = DaoFactory.GetEditLinq2DBQuery<Right>(false);

            ////ISearchQuery<Left> leftSearchQuery = DaoFactory.GetSearchMongoDBQuery<Left>();
            //ISearchQuery<Left> leftSearchQuery = DaoFactory.GetSearchLinq2DBQuery<Left>(false);

            ////ISearchQuery<Right> rightSearchQuery = DaoFactory.GetSearchMongoDBQuery<Right>();
            //ISearchQuery<Right> rightSearchQuery = DaoFactory.GetSearchLinq2DBQuery<Right>(false);

            //var right = new Right() { ID = IDGenerator.NextID(), ClassName = "class_one" };
            //rightEditQuery.Insert(datas: right);
            //leftEditQuery.Insert(datas: new Left() { ID = IDGenerator.NextID(), ClassID = right.ID, StudentName = $"student_{IDGenerator.NextID()}" });

            //int count = 0;

            ////foreach (var item in leftSearchQuery.Search(queryOrderBies: new QueryOrderBy<Left>[] { new QueryOrderBy<Left>(item => item.ID),
            ////                                                                                       new QueryOrderBy<Left>(item => item.StudentName, OrderByType.Desc)}))
            ////{
            ////    Console.WriteLine(item.ID + " " + item.StudentName);
            ////}

            ////for (int i = 0; i < 10; i++)
            ////{
            ////    int index = i;

            ////    Thread thread = new Thread(() =>
            ////    {
            ////        while (true)
            ////        {
            ////            Left[] datas = new Left[1000];

            ////            for (int i = 0; i < datas.Length; i++)
            ////            {
            ////                datas[i] = new Left()
            ////                {
            ////                    ID = IDGenerator.NextID(),
            ////                    ClassID = 1,
            ////                    StudentName = $"student_{index}"
            ////                };
            ////            }

            ////            leftEditQuery.Insert(datas: datas);
            ////            count += datas.Length;
            ////        }
            ////    });

            ////    thread.IsBackground = true;
            ////    thread.Start();
            ////}

            ////Thread thread1 = new Thread(() =>
            ////{
            ////    int time = System.Environment.TickCount;

            ////    while (true)
            ////    {
            ////        System.Console.WriteLine(count * 1000f / (System.Environment.TickCount - time));
            ////        count = 0;
            ////        time = System.Environment.TickCount;
            ////        Thread.Sleep(1000);
            ////    }
            ////});

            ////thread1.IsBackground = true;
            ////thread1.Start();

            ////Console.Read();

            //foreach (var item in leftSearchQuery.Search(item => item.StudentName.Contains("student_"), new QueryOrderBy<Left>[] {
            //    new QueryOrderBy<Left>(item => item.StudentName, OrderByType.Desc),
            //    new QueryOrderBy<Left>(item => item.ID)
            //}))
            //{
            //    System.Console.WriteLine(item.StudentName);
            //}

            //System.Console.WriteLine(rightSearchQuery.Count(item => item.ID >= 214179025868816385));

            //right.ID = 214179025868816385;
            //right.ClassName = "hw";
            //rightEditQuery.Merge(datas: right);

            //rightEditQuery.Delete(ids: new long[] { 213322624808255489, 213322771545980929, 213322849337737217 });

            //rightEditQuery.Update(item => item.ClassName == "haha", new Dictionary<string, object>() { ["ClassName"] = "hello world!" });


            ////while (true)
            ////{
            ////    foreach (var item in leftSearchQuery.Search(item => item.StudentName.Contains("student_"), new QueryOrderBy<Left>[] {
            ////        new QueryOrderBy<Left>(item => item.StudentName, OrderByType.Desc),
            ////        new QueryOrderBy<Left>(item => item.ID)
            ////    }))
            ////    {
            ////        System.Console.WriteLine(item.StudentName);
            ////    }
            ////}

            //while (true)
            //{
            //    var query = from a in leftSearchQuery.GetQueryable<Left>(null)
            //                join b in leftSearchQuery.GetQueryable<Right>(null) on a.ClassID equals b.ID
            //                where b.ID > 0
            //                select new { a, b };

            //    var datas = leftSearchQuery.Search(query).ToArray();

            //    foreach (var item in datas)
            //    {
            //        Console.WriteLine(item.a.StudentName + " " + item.b.ClassName);
            //    }
            //}

            ////try
            ////{
            ////    using (var transaction = leftEditQuery.BeginTransaction())
            ////    {
            ////        Left left = new Left() { ID = 1, ClassID = 1, StudentName = "transaction_test" };
            ////        leftEditQuery.Insert(transaction, left);

            ////        System.Console.WriteLine("transaction " + leftSearchQuery.Count(transaction: transaction));

            ////        int a = 0;
            ////        int b = 5;
            ////        int wd = b / a;



            ////        transaction.Submit();
            ////    }
            ////}
            ////catch
            ////{
            ////    System.Console.WriteLine("error");
            ////}


            //System.Console.WriteLine("insert ok");
            //System.Console.Read();






            ISearchQuery<StockInfo> asearch = DaoFactory.GetSearchMongoDBQuery<StockInfo>();
            ISearchQuery<WarehouseInfo> bsearch = DaoFactory.GetSearchMongoDBQuery<WarehouseInfo>();
            ISearchQuery<SupplierCommodity> csearch = DaoFactory.GetSearchMongoDBQuery<SupplierCommodity>();

            var a = asearch.GetQueryable<StockInfo>();
            var b = bsearch.GetQueryable<WarehouseInfo>();
            var c = csearch.GetQueryable<SupplierCommodity>();

            var query = from adata in a
                        join bdata in b on adata.WarehouseID.Value equals bdata.ID
                        join cdata in c on adata.SupplierCommodityID.Value equals cdata.ID into am
                        from cdata in am.DefaultIfEmpty()
                            //select new { a = adata, b = bdata, c = cdata } into res
                        select adata.ID;
                        //where !res.a.IsDeleted && res.b.IsDeleted && res.c == null ? true : !res.c.IsDeleted
                        //select new { aid = res.a.ID, bid = res.b.ID, cid = res.c.ID };
            var m = query.ToList();









            ////Common.ConfigManager.Init("Production");
            ////string key = System.Guid.NewGuid().ToString("D");
            ////Common.Lock.ILock @lock = Common.Lock.LockFactory.GetRedisLock();
            ////int count = 0;

            ////for (int i = 0; i < 100; i++)
            ////{
            ////    int index = i;

            ////    System.Threading.Thread thread = new System.Threading.Thread(() =>
            ////    {
            ////        while (true)
            ////        {
            ////            string identity = System.Guid.NewGuid().ToString("D");

            ////            if (@lock.Acquire(key, identity, 0, 10000))
            ////            {
            ////                //Thread.Sleep(2);

            ////                Thread.Sleep(4000);

            ////                count++;

            ////                @lock.Release(identity);
            ////            }
            ////            else
            ////            {
            ////                Console.WriteLine("lock time out" + Environment.TickCount);
            ////            }
            ////        }
            ////    });

            ////    thread.IsBackground = true;

            ////    thread.Start();
            ////}

            ////System.Threading.Thread thread1 = new System.Threading.Thread(() =>
            ////{
            ////    int time = Environment.TickCount;

            ////    while (true)
            ////    {
            ////        System.Threading.Thread.Sleep(1000);

            ////        //System.Console.WriteLine(count * 1000f / (Environment.TickCount - time));
            ////        Console.WriteLine(count);
            ////        //count = 0;
            ////        //time = Environment.TickCount;
            ////    }
            ////});

            ////thread1.IsBackground = true;
            ////thread1.Start();


            ////System.Console.Read();
















            IHostBuilder hostBuilder = CreateHostBuilder(args);
            IHost host = hostBuilder.Build();

            //logHelper.Info("123", "123");
            //logHelper.Info("123", "123", "123", "123");
            //logHelper.Error("123", "123", 200, "123", "123", "123");
            //logHelper.SqlError("123", "123", "123");
            //logHelper.TCCNode(123, true, "123");
            //logHelper.TCCServer(123, "123");

            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args);

            hostBuilder.ConfigureServices(ConfigInit);
            hostBuilder.ConfigureLogging(LoggingConfig);
            hostBuilder.ConfigureWebHostDefaults(WebHostConfig);

            return hostBuilder;
        }

        private static void WebHostConfig(IWebHostBuilder webHost)
        {
            webHost.UseStartup<Startup>();
        }

        private static void ConfigInit(HostBuilderContext hostBuilderContext, IServiceCollection services)
        {
            //hostBuilderContext.ConfigIgnite();
            hostBuilderContext.ConfigInit(services);
        }

        private static void LoggingConfig(HostBuilderContext hostBuilderContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            //loggingBuilder.AddConsole();
        }
    }
}