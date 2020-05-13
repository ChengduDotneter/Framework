using System;
using System.Collections.Generic;
using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using System.Linq;

namespace Test
{
    public class TestData : ViewModelBase
    {
        [QuerySqlField(IsIndexed = true, NotNull = true)]
        public string Name { get; set; }
        [QuerySqlField(IsIndexed = false, NotNull = false)]
        public int? Number { get; set; }
    }

    class Program
    {

        static void Main(string[] args)
        {
            ConfigManager.Init("Development");

            //Random random = new Random();

            //TestData testData = new TestData()
            //{
            //    ID = IDGenerator.NextID(),
            //    Name = $"Zxy{DateTime.Now:yyyy-MM-dd hh:mm:ss}",
            //    Number = random.Next(1, 10000)
            //};

            //DaoFactory.GetEditIgniteQuery<TestData>().Insert(testData);
            //Console.WriteLine(DaoFactory.GetSearchIgniteQuery<TestData>().Count());

            //string command = Console.ReadLine();

            //while (command != "exit")
            //{
            //    //foreach (IDictionary<string, object> item in DaoFactory.GetSearchIgniteQuery<TestData>().Query(command))
            //    //    foreach (var data in item)
            //    //        Console.WriteLine($"{data.Key}:{data.Value}");

            //    Console.WriteLine(DaoFactory.GetSearchIgniteQuery<TestData>().Count());

            //    command = Console.ReadLine();
            //}

           ISearchQuery<TestData> searchQuery = DaoFactory.GetSearchSqlSugarQuery<TestData>(false);

            var data = searchQuery.Query("SELECT * FROM TestData");

            var data_2 = MapperModelHelper<TestData>.ReadModel(data);
        }
    }
}
