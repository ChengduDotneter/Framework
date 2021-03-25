using Common.Log;
using Common.Model;
using Common.ServiceCommon;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Common;

// ReSharper disable UnusedMember.Local

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
            // IEnumerable<DropDownListDataCollection> dropDownListDatas = new[]
            // {
            //     new DropDownListDataCollection(new[]
            //     {
            //         new DropDownListData
            //         {
            //             Data = "itemA",
            //             Children = new DropDownListDataCollection(new[]
            //             {
            //                 new DropDownListData
            //                 {
            //                     Data = "itemAA"
            //                 },
            //                 new DropDownListData
            //                 {
            //                     Data = "itemAB"
            //                 },
            //                 new DropDownListData
            //                 {
            //                     Data = "itemAC",
            //                     Children = new DropDownListDataCollection(new[]
            //                     {
            //                         new DropDownListData
            //                         {
            //                             Data = "ItemACA"
            //                         },
            //                         new DropDownListData
            //                         {
            //                             Data = "ItemACB"
            //                         },
            //                         new DropDownListData
            //                         {
            //                             Data = "ItemACC"
            //                         }
            //                     })
            //                     {
            //                         DataColumn = 3
            //                     }
            //                 }
            //             })
            //             {
            //                 DataColumn = 2
            //             }
            //         },
            //         new DropDownListData
            //         {
            //             Data = "itemB",
            //             Children = new DropDownListDataCollection(new[]
            //             {
            //                 new DropDownListData
            //                 {
            //                     Data = "itemBA"
            //                 },
            //                 new DropDownListData
            //                 {
            //                     Data = "itemBB"
            //                 },
            //                 new DropDownListData
            //                 {
            //                     Data = "itemBC"
            //                 }
            //             })
            //             {
            //                 DataColumn = 2
            //             }
            //         }
            //     })
            //     {
            //         DataColumn = 1
            //     },
            //     new DropDownListDataCollection(new[]
            //     {
            //         new DropDownListData
            //         {
            //             Data = "HAHA1"
            //         },
            //         new DropDownListData
            //         {
            //             Data = "HAHA2"
            //         },
            //         new DropDownListData
            //         {
            //             Data = "HAHA3"
            //         },
            //         new DropDownListData
            //         {
            //             Data = "HAHA4"
            //         }
            //     })
            //     {
            //         DataColumn = 4
            //     },
            // };
            //
            // DataTable dataTable = new DataTable();
            // dataTable.Columns.Add("Name");
            // dataTable.Columns.Add("Item1");
            // dataTable.Columns.Add("Item2");
            // dataTable.Columns.Add("Item3");
            // dataTable.Columns.Add("HAHA");
            //
            // for (int i = 0; i < 2000; i++)
            // {
            //     DataRow dataRow = dataTable.NewRow();
            //     dataRow["Name"] = "item" + (i + 1);
            //     dataTable.Rows.Add(dataRow);
            // }
            //
            // byte[] buffer = ExcelHelper.DataTableToExcelByte(dataTable, "数据导出", dropDownListDatas);
            //
            // using (FileStream fileStream = new FileStream("/Users/zhangxiaoya/Desktop/test.xlsx", FileMode.OpenOrCreate))
            // {
            //     fileStream.Write(buffer, 0, buffer.Length);
            // }

            IHostBuilder hostBuilder = CreateHostBuilder(args);
            IHost host = hostBuilder.Build();

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
            services.DefaultLogHelperConfig(Common.Log.LogModel.LogHelperTypeEnum.Log4netLog);
            //hostBuilderContext.ConfigIgnite();
            hostBuilderContext.ConfigInit(services);
        }

        private static void LoggingConfig(HostBuilderContext hostBuilderContext, ILoggingBuilder loggingBuilder)
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConsole();
        }
    }
}