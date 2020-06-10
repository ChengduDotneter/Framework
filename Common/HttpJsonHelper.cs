using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// HTTPJsonHelper请求帮助类
    /// </summary>
    public static class HttpJsonHelper
    {
        /// <summary>
        /// post异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <param name="content"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPostAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).PostAsync(action, content.AddJsonContentType());
        }

        /// <summary>
        /// Post同步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <param name="content"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPost(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPostAsync(url, action, content, bearerToken).Result;
        }

        /// <summary>
        /// get异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpGetAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).GetAsync(action);
        }

        /// <summary>
        /// get同步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpResponseMessage HttpGet(string url, string action, string bearerToken = "")
        {
            return HttpGetAsync(url, action, bearerToken).Result;
        }

        /// <summary>
        /// put异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <param name="content"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPutAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).PutAsync(action, content.AddJsonContentType());
        }

        /// <summary>
        /// put同步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <param name="content"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPut(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPutAsync(url, action, content, bearerToken).Result;
        }

        /// <summary>
        /// delete异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpDeleteAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).DeleteAsync(action);
        }

        /// <summary>
        /// delete同步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="action"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpResponseMessage HttpDelete(string url, string action, string bearerToken = "")
        {
            return HttpDeleteAsync(url, action, bearerToken).Result;
        }

        private static HttpClient GetHttpClient(string url)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            return httpClient;
        }

        /// <summary>
        /// 异步获取请求结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public static async Task<T> GetResponseAsync<T>(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.PaymentRequired)
                throw new DealException(await response.Content.ReadAsStringAsync());
            else if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception(await response.Content.ReadAsStringAsync());

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// 同步获取请求结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public static T GetResponse<T>(HttpResponseMessage response)
        {
            return GetResponseAsync<T>(response).Result;
        }
    }
}