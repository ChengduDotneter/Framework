using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Common.DAL;
using HtmlAgilityPack;
using RestSharp;

namespace Reptiles
{
    class Goods : IEntity
    {
        public long ID { get; set; }
        public string GoodsName { get; set; }
        public string StorageName { get; set; }
        public string ProductPlace { get; set; }
        public string BarCode { get; set; }
        public decimal Price { get; set; }
        public decimal BlackCardPrice { get; set; }
    }

    class SkuInfo : IEntity
    {
        public long ID { get; set; }
        public string BarCode { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Common.ConfigManager.Init("Development");

            var str = "kaola_user_key=00db1ea3-ef83-436c-b2d4-e085ab20c523; JSESSIONID-WKL-8IO=qS2s4Bw%5ChnOc7wVtfrXkcBkhD59fKcd%2Fkijj25%2FyI3naDYNvwBJS7llExvmdY%5CQYGp4viana0Q7dEQaCwppWAxNAfLiaY2RHb74iAgHZOgj9dQ4am9RqxXApbQBsRdqfzZy3K2n6X0PHvDCODt9CI%2Bgm4MfjRiS5A%2FOYiGtluhUXvz%2Fx%3A1597891273149; _klhtxd_=31; hb_MA-AE38-1FCC6CD7201B_source=search.kaola.com; isg=BN_f4nHW_KrvR_iwV85lGxi-bTVpRDPm8wmUpnEseQ7VAP-CeBArNzzCwFDbgwte; __da_ntes_utma=2525167.319784201.1597650579.1597650579.1597650579.1; davisit=1; __da_ntes_utmz=2525167.1597650579.1.1.utmcsr%3D(direct)%7Cutmccn%3D(direct)%7Cutmcmd%3D(none); __da_ntes_utmfc=1; KAOLA_NEW_USER_COOKIE=yes; WM_TID=hBiQK7ZBCQFFBBAUBVJ%2FDqqKPXZKWhv3; t=951e532783fc0a101a2e6d0fb3205fa5; KAOLA_MAIN_ACCOUNT=159780823486692472@pvkaola.163.com; _uab_collina=159780826122283247862116; _samesite_flag_=true; cookie2=1611fc621a5e1178d014b5443fe922f8; _tb_token_=576163b787e56; NTES_KAOLA_RV=2843611_1597813239022_0|5206216_1597808251842_0|5971488_1597728326124_0; csg=093d2a53; NTES_OSESS=7c96098aed574066990d18fcbc399321; kaola_csg=460cb3ed; kaola-user-beta-traffic=11317970477; KAOLA_USER_ID=109999078921347715; cna=zbYxF/bMDgsCAd7TioK/ZLxx; x5sec=7b227761676272696467652d616c69626162612d67726f75703b32223a22313734623933656165373566396261323035666564653535623961373831656343494b6c382f6b46454e2b656e666a72684d503157513d3d227d";

            Dictionary<string, string> dic = new Dictionary<string, string>();

            foreach (var item in str.Split(";"))
            {
                var arr = item.Trim().Split("=");
                dic.Add(arr[0].Trim(), arr[1].Trim());
            }

            var searchQuery = Common.DAL.DaoFactory.GetSearchLinq2DBQuery<SkuInfo>(false);
            var editQuery = Common.DAL.DaoFactory.GetEditMongoDBQuery<Goods>();
            var query = from sku in searchQuery.GetQueryable<SkuInfo>(null)
                        where sku.BarCode.Length >= 8
                        group sku.BarCode by sku.BarCode into barCodes
                        select barCodes.Key;

            var results = searchQuery.Search(query).ToArray();
            int taskCount = 10;
            var perCount = (int)Math.Ceiling(results.Length * 1d / taskCount);
            var taskDatas = new string[10][];
            int index = 0;

            for (int i = 0; i < taskDatas.Length; i++)
            {
                taskDatas[i] = new string[perCount];

                for (int k = 0; k < perCount; k++)
                {
                    if (index + k < results.Length)
                    {
                        taskDatas[i][k] = results[index + k];
                    }
                    else
                    {
                        break;
                    }
                }

                index += perCount;
            }

            for (int i = 0; i < taskCount; i++)
            {
                int taskIndex = i;

                Task.Factory.StartNew((state) =>
                {
                    string[] datas = ((string[])state).Where(item => !string.IsNullOrWhiteSpace(item)).ToArray();

                    for (int k = 0; k < datas.Length; k++)
                    {
                        string searchKey = System.Net.WebUtility.UrlEncode(datas[k]);
                        var client = new RestClient($"https://search.kaola.com/search.html?key={searchKey}&searchRefer=searchbutton&zn=top");
                        client.Timeout = -1;
                        var request = new RestRequest(Method.GET);

                        request.AddHeader("Cookie", string.Join(";", dic.Select(item => item.Key + "=" + item.Value)));
                        IRestResponse response = client.Execute(request);
                        HtmlDocument htmlDocument = new HtmlDocument();
                        htmlDocument.LoadHtml(response.Content);

                        IList<Goods> goods = new List<Goods>();

                        if (htmlDocument.DocumentNode.SelectSingleNode("//div[@class='correction']") != null)
                            return;

                        foreach (var item in htmlDocument.DocumentNode.SelectNodes("//li[contains(@class, 'goods')]"))
                        {
                            goods.Add(new Goods()
                            {
                                ID = Common.IDGenerator.NextID(),
                                GoodsName = item.SelectSingleNode("//a[@class='title']").Attributes["title"].Value,
                                ProductPlace = item.SelectSingleNode("//span[contains(@class, 'proPlace')]").InnerHtml,
                                StorageName = item.SelectSingleNode("//p[@class='selfflag']").ChildNodes[1].InnerText,
                                BarCode = datas[k],
                                Price = Convert.ToDecimal(item.SelectSingleNode("//span[@class='bigPrice']").InnerHtml),
                                BlackCardPrice = Convert.ToDecimal(item.SelectSingleNode("//span[@class='blackCardPrice']").ChildNodes[1].InnerHtml)
                            });
                        }

                        editQuery.Insert(datas: goods.ToArray());

                        if (k % 10 == 0)
                        {
                            Console.WriteLine($"{taskIndex}: {k}");
                        }

                        Task.Delay(1000).Wait();
                    }

                    Console.WriteLine($"{taskIndex}: done");

                }, taskDatas[i]);
            }

            Console.Read();
        }
    }
}
