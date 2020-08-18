using System;
using System.IO;
using System.Net;
using System.Text;
using HtmlAgilityPack;
using RestSharp;

namespace Reptiles
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new RestClient("https://s.taobao.com/search?q=%E7%88%B1%E4%BB%96%E7%BE%8E");
            client.Timeout = -1;
            var request = new RestRequest(Method.GET);
            client.UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:79.0) Gecko/20100101 Firefox/79.0";
            request.AddHeader("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            request.AddHeader("Accept-Language", "zh-CN,zh;q=0.8,zh-TW;q=0.7,zh-HK;q=0.5,en-US;q=0.3,en;q=0.2");
            request.AddHeader("Cookie", "thw=cn;");
            IRestResponse response = client.Execute(request);

            string html = response.Content;

            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            foreach (var item in htmlDocument.DocumentNode.SelectNodes("//head/script"))
            {
                string a = item.InnerHtml;
            }

            Console.WriteLine(html);

            Console.Read();
        }
    }
}
