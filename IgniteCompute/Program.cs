using Common;
using Common.Compute;
using CsvHelper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IgniteCompute
{
    internal class Order
    {
        public string PddOrderNumber { get; set; }
        public string S2BOrderNumber { get; set; }
        public string GWOrderNumber { get; set; }
        public bool Ready { get; set; }
        public bool InS2B { get; set; }
        public bool InGW { get; set; }
    }

    internal class Program
    {
        private static async Task Main()
        {
            IList<Order> orders = new List<Order>();

            using (TextReader reader = new StreamReader("/Users/zhangxiaoya/Downloads/132078497orders_export2020-09-11-14-06-22.csv"))
            {
                var csvReader = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture));
                var dataReader = new CsvDataReader(csvReader);

                while (dataReader.Read())
                {
                    orders.Add(new Order() { PddOrderNumber = dataReader["订单号"].ToString() });
                }
            }

            using (MySqlConnection conn = new MySqlConnection("Server=120.27.149.76;Database=pss;User=helong;Password=hl@0728;"))
            {
                conn.Open();

                MySqlCommand mySqlCommand = new MySqlCommand($"SELECT order_sn, supplier_csn, express_sn FROM order_supplier WHERE order_sn IN ({string.Join(",", orders.Select(item => $"'{item.PddOrderNumber}'"))})", conn);

                using (var reader = mySqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Order order = orders.SingleOrDefault(item => item.PddOrderNumber == reader["order_sn"].ToString());

                        if (order == null)
                            continue;

                        order.S2BOrderNumber = reader["supplier_csn"].ToString();
                        order.Ready = reader["express_sn"] != null;
                    }
                }
            }

            orders.ForEach(item => item.InS2B = item.S2BOrderNumber != null);

            using (MySqlConnection conn = new MySqlConnection("Server=125.64.9.61;Database=tianfumicroserver;User=root;Password=mb123456;"))
            {
                conn.Open();

                MySqlCommand mySqlCommand = new MySqlCommand($"SELECT oldordercode, ordercode FROM orderinfo WHERE oldordercode IN ({string.Join(",", orders.Where(item => item.InS2B).Select(item => $"'{item.S2BOrderNumber}'"))})", conn);

                using (var reader = mySqlCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Order order = orders.SingleOrDefault(item => item.S2BOrderNumber == reader["oldordercode"].ToString());

                        if (order == null)
                            continue;

                        order.GWOrderNumber = reader["ordercode"].ToString();
                    }
                }
            }

            orders.ForEach(item => item.InGW = item.GWOrderNumber != null);

            foreach (var item in orders.Where(item => !item.InGW || !item.InS2B || !item.Ready))
            {
                Console.WriteLine($"PDD订单号: {item.PddOrderNumber}, S2B订单号: {(item.InS2B ? item.S2BOrderNumber : "漏单")}, 关务订单号: {(item.InGW ? item.GWOrderNumber : "漏单")}, 发货状态: {(item.Ready ? "已发货" : "未发货")}");
            }

            await Host.CreateDefaultBuilder().
                   ConfigureServices((hostBuilderContext, serviceCollection) =>
                   {
                       ConfigManager.Init("Development");
                       //serviceCollection.AddComputeFactory();
                       //serviceCollection.AddHttpCompute();

                       //hostBuilderContext.ConfigIgnite();

                       //System.Threading.Tasks.Task.Factory.StartNew(async () =>
                       //{
                       //    while (true)
                       //    {
                       //        var result = await Common.Compute.ComputeFactory.GetIgniteCompute().BordercastAsync(new ComputeFunc(), new ComputeParameter() { RequestData = "ZZZ" });
                       //        Console.WriteLine(string.Join(Environment.NewLine, result.Select(item => item.ResponseData)));

                       //        await Task.Delay(1000);
                       //    }
                       //});
                   }).
                   ConfigureLogging((loggingBuilder) =>
                   {
                       loggingBuilder.ClearProviders();
                   }).
                   Build().
                   RunAsync();

            Console.WriteLine("End");
        }
    }

    public class ComputeParameter
    {
        public string RequestData { get; set; }
    }

    public class ComputeResult
    {
        public string ResponseData { get; set; }
    }

    public class ComputeFunc : IComputeFunc<ComputeParameter, ComputeResult>
    {
        public ComputeResult Excute(ComputeParameter parameter)
        {
            Console.WriteLine(parameter.RequestData);
            return new ComputeResult() { ResponseData = DateTime.Now.ToString("g") };
        }
    }
}