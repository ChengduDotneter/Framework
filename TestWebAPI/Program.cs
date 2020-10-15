using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Apache.Ignite.Core.Cache;
using Common;
using Common.DAL;
using Common.Log;
using Common.Model;
using Common.ServiceCommon;
using LinqToDB.Mapping;
using MicroService.StorageService.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace TestWebAPI
{
    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Left : ViewModelBase
    {
        [Column]
        public string StudentName { get; set; }

        [Column]
        public long ClassID { get; set; }
    }

    [MongoDB.Bson.Serialization.Attributes.BsonIgnoreExtraElements]
    public class Right : ViewModelBase
    {
        [Column]
        public string ClassName { get; set; }
    }

    public class TestData : ViewModelBase
    {
        public string Data { get; set; }
        public DateTime AC { get; set; }
    }

    public class Program
    {
        private static ILogHelper logHelper = LogHelperFactory.GetKafkaLogHelper();

        public static void Main(string[] args)
        {
            ConfigManager.Init("Development");

            var query = DaoFactory.GetEditLinq2DBQuery<Left>(false);
            var query11 = DaoFactory.GetSearchLinq2DBQuery<Left>(false);

            using (ITransaction transaction = query.BeginTransaction())
            {
                //var datas = query11.Search(transaction: transaction).ToList();
                //datas.FirstOrDefault().StudentName = "111";

                //data.StudentName = "111";
                //query.Insert(transaction, new Left() { ID = IDGenerator.NextID(), StudentName = "111" });

                //query.Merge(item => !item.IsDeleted, new Dictionary<string, object> { [nameof(Left.StudentName)] = "123456" }, transaction).Wait();
                //query.MergeAsync(transaction, datas.ToArray()).Wait();

                query.DeleteAsync(transaction, 236813124689205441).Wait();

                //var datass = query11.SearchAsync(item => item.StudentName == "abc", transaction: transaction).Result;

                var datass = query11.Search(transaction: transaction).ToList();

                Console.WriteLine(datass.Count());
                transaction.Rollback();
            }

            Console.ReadLine();


            var query1 = DaoFactory.GetSearchMongoDBQuery<Left>();
            var query2 = DaoFactory.GetSearchMongoDBQuery<Right>();

            var queryable1 = query1.FilterIsDeleted().GetQueryable();
            var queryable2 = query2.FilterIsDeleted().GetQueryable();


            var m = queryable1.OrderByDescending(item => item.ID).ThenBy(item => item.StudentName).ToArray();


            var d = from left in queryable1.OrderByDescending(item => item.ID).ThenBy(item => item.StudentName)
                    join right in queryable2.OrderByDescending(item => item.ID).ThenBy(item => item.ClassName) on left.ClassID equals right.ID into inqs
                    where left.StudentName != null
                    orderby left.ID ascending
                    select left.StudentName;



            var ks = query1.SearchAsync(d).Result;

            //var query1 = DaoFactory.GetSearchMongoDBQuery<TestData>();
            //IEditQuery<TestData> editQuery = DaoFactory.GetEditMongoDBQuery<TestData>();
            //var c1 = query1.Count();

            //int index = 0;
            //int time = Environment.TickCount;
            //int startTime = Environment.TickCount;

            //while (true)
            //{
            //    TestData[] testDatas = new TestData[1000];

            //    for (int i = 0; i < testDatas.Length; i++)
            //    {
            //        testDatas[i] = new TestData() { AC = DateTime.Now, CreateTime = DateTime.Now, CreateUserID = -9999, Data = $"data_{++index}", ID = IDGenerator.NextID(), IsDeleted = false, UpdateTime = null, UpdateUserID = null };
            //    }

            //    editQuery.Insert(datas: testDatas);

            //    if (Environment.TickCount - time > 1000)
            //    {
            //        Console.WriteLine(index * 1000d / (Environment.TickCount - startTime));
            //        time = Environment.TickCount;
            //    }
            //}

            //while (true)
            //{
            //    int time1 = Environment.TickCount;
            //    Console.WriteLine($"{query1.Count(item => item.ID > 0)} time: {Environment.TickCount - time1}");
            //    time1 = Environment.TickCount;

            //    Thread.Sleep(1000);
            //}

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



            //MySql.Data.MySqlClient.MySqlConnection conn = new MySql.Data.MySqlClient.MySqlConnection("Server=192.168.10.211;Database=commodity_fix;User=sa;Password=hgl@2020;");
            //conn.Open();

            //MySql.Data.MySqlClient.MySqlCommand mySqlCommand = new MySql.Data.MySqlClient.MySqlCommand();
            //mySqlCommand.CommandText = "select * from orign_commodity_hn where BarCode like '%【%'";
            //mySqlCommand.Connection = conn;

            //MySql.Data.MySqlClient.MySqlConnection conn1 = new MySql.Data.MySqlClient.MySqlConnection("Server=192.168.10.211;Database=commodity_fix;User=sa;Password=hgl@2020;");
            //conn1.Open();


            //var reader = mySqlCommand.ExecuteReader();

            //while (reader.Read())
            //{
            //    string name = reader["barcode"].ToString();
            //    string value = name;

            //    while (true)
            //    {
            //        int start = value.IndexOf("【");

            //        if (start > -1)
            //        {
            //            int end = value.IndexOf("】");

            //            if (end > -1)
            //            {
            //                string yd = value.Substring(start, end - start + 1);
            //                value = value.Replace(yd, string.Empty);
            //            }
            //            else
            //                break;
            //        }
            //        else
            //            break;
            //    }

            //    MySql.Data.MySqlClient.MySqlCommand ab = new MySql.Data.MySqlClient.MySqlCommand();
            //    ab.Connection = conn1;
            //    ab.CommandText = $"UPDATE orign_commodity_hn set barcode = '{value}' where id = {reader["ID"]} and name = '{reader["name"]}' and barcode = '{name}'";
            //    ab.ExecuteNonQuery();
            //}



            //var etlTask = Common.DAL.ETL.ETLHelper.Transform(new Type[] { typeof(StockInfo), typeof(WarehouseInfo), typeof(SupplierCommodity) },
            //         type => typeof(DaoFactory).GetMethod("GetSearchMongoDBQuery").MakeGenericMethod(type).Invoke(null, null),
            //         type => typeof(DaoFactory).GetMethod("GetEditLinq2DBQuery").MakeGenericMethod(type).Invoke(null, new object[] { false }));

            //while (!etlTask.Task.IsCompleted)
            //{
            //    if (etlTask.RunningTable != null)
            //        Console.WriteLine($"{etlTask.RunningTable}: {etlTask.RunningTable.ComplatedCount}/{etlTask.RunningTable.DataCount}");

            //    System.Threading.Thread.Sleep(1000);
            //}

            //Console.WriteLine("ok");

            //string foreignColumn = "ID";
            //ParameterExpression parameter = Expression.Parameter(typeof(StockInfo), "item");
            //Expression equal = Expression.Equal(Expression.Property(parameter, foreignColumn), Expression.Constant(191574238853980162L));
            //Expression equal1 = Expression.Equal(Expression.Property(parameter, "IsDeleted"), Expression.Constant(false));

            //equal = Expression.And(equal, equal1);

            //Expression expression = Expression.Lambda(equal, parameter);




            //ISearchQuery<StockInfo> asearch = DaoFactory.GetSearchMongoDBQuery<StockInfo>();
            //ISearchQuery<WarehouseInfo> bsearch = DaoFactory.GetSearchMongoDBQuery<WarehouseInfo>();
            //ISearchQuery<SupplierCommodity> csearch = DaoFactory.GetSearchMongoDBQuery<SupplierCommodity>();

            //var type1 = typeof(Func<,>).MakeGenericType(typeof(StockInfo), typeof(bool));
            //var type = typeof(Expression<>).MakeGenericType(type1);

            //var q = typeof(DaoFactory).GetMethod("GetSearchMongoDBQuery").MakeGenericMethod(typeof(StockInfo)).Invoke(null, null);
            //var method = typeof(ISearchQuery<>).MakeGenericType(typeof(StockInfo)).GetMethod("Count", new Type[] { type, typeof(ITransaction) });

            //var ksd = (int)method.Invoke(q, new object[] { expression, null });

            //var a = asearch.GetQueryable();
            //var b = bsearch.GetQueryable();
            //var c = csearch.GetQueryable();

            //var query = from adata in a
            //            join bdata in b on adata.WarehouseID.Value equals bdata.ID
            //            //join cdata in c on adata.SupplierCommodityID.Value equals cdata.ID into am
            //            //from cdata in am.DefaultIfEmpty()
            //            //select new { a = adata, b = bdata, c = cdata } into res
            //            select adata.ID;
            ////where !res.a.IsDeleted && res.b.IsDeleted && res.c == null ? true : !res.c.IsDeleted
            ////select new { aid = res.a.ID, bid = res.b.ID, cid = res.c.ID };
            //var m = asearch.Search(item => item.IsDeleted, new QueryOrderBy<StockInfo>[] { new QueryOrderBy<StockInfo>(item => item.CreateTime, OrderByType.Desc) }, 0, 15);

            //var s = asearch.Count(item => !item.IsDeleted);





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