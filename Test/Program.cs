using System;
using System.Collections.Generic;
using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.IO;
using System.Text;
using SqlSugar;
using Common.Validation;

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

            var count = DaoFactory.GetSearchIgniteQuery<TestData>().Count("Name = @Name", new Dictionary<string, object>
            {
                ["@Name"] = "zxy"
            });

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

            //ISearchQuery<TestData> searchQuery = DaoFactory.GetSearchSqlSugarQuery<TestData>(false);

            // var data = searchQuery.Query("SELECT * FROM TestData");

            // var data_2 = MapperModelHelper<TestData>.ReadModel(data);

            Console.WriteLine($"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa      {count}");


            JObject a = new JObject();
            a["a"] = "123";
            JArray jArray = new JArray();
            jArray.Add(1);
            jArray.Add(2);
            jArray.Add(3);
            jArray.Add(4);
            a["b"] = jArray;


            JArray jArray1 = new JArray();
            JArray jArray2 = new JArray();
            jArray2.Add(1);
            jArray2.Add(2);
            jArray2.Add(3);
            jArray2.Add(4);
            JArray jArray3 = new JArray();
            jArray3.Add(1);
            jArray3.Add(2);
            jArray3.Add(3);
            jArray3.Add(4);
            JArray jArray4 = new JArray();
            jArray4.Add(1);
            jArray4.Add(2);
            jArray4.Add(3);
            jArray4.Add(4);

            jArray1.Add(jArray2);
            jArray1.Add(jArray3);
            jArray1.Add(jArray4);

            a["c"] = jArray1;


            var options = new JsonWriterOptions
            {
                Indented = true
            };

            //using (var stream = new MemoryStream())
            //{
            //    using (var writer = new Utf8JsonWriter(stream, options))
            //    {
            //        writer.WriteStartObject();
            //        writer.WriteString("date", DateTimeOffset.UtcNow);
            //        writer.WriteNumber("temp", 42);
            //        writer.WriteEndObject();
            //    }
            //    string json = Encoding.UTF8.GetString(stream.ToArray());
            //    Console.WriteLine(json);
            //}

            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream, options);
            JObjectConverter jObjectConverter = new JObjectConverter();
            JArrayConverter jArrayConverter = new JArrayConverter();
            jObjectConverter.Write(writer, a, null);
            //jArrayConverter.Write(writer, jArray1, null);

            writer.Flush();

            string json = Encoding.UTF8.GetString(stream.ToArray());
            Console.WriteLine(json);

            var options1 = new JsonReaderOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip
            };

            Utf8JsonReader reader = new Utf8JsonReader(stream.ToArray(), options1);

            var data = jObjectConverter.Read(ref reader, typeof(int), null);

            //var data = jArrayConverter.Read(ref reader, typeof(int), null);

            Console.ReadLine();
        }
    }
}
