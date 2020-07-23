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
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="content">请求Body</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPostAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).PostAsync(action, content.AddJsonContentType());
        }

        /// <summary>
        /// Post同步请求
        /// </summary>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="content">请求Body</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPost(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPostAsync(url, action, content, bearerToken).Result;
        }

        /// <summary>
        /// get异步请求
        /// </summary>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpGetAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).GetAsync(action);
        }

        /// <summary>
        /// get同步请求
        /// </summary>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpGet(string url, string action, string bearerToken = "")
        {
            return HttpGetAsync(url, action, bearerToken).Result;
        }

        /// <summary>
        /// put异步请求
        /// </summary>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="content">请求Body</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPutAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).PutAsync(action, content.AddJsonContentType());
        }

        /// <summary>
        /// put同步请求
        /// </summary>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="content">请求Body</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPut(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPutAsync(url, action, content, bearerToken).Result;
        }

        /// <summary>
        /// delete异步请求
        /// </summary>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpDeleteAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).DeleteAsync(action);
        }

        /// <summary>
        /// delete同步请求
        /// </summary>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpDelete(string url, string action, string bearerToken = "")
        {
            return HttpDeleteAsync(url, action, bearerToken).Result;
        }

        /// <summary>
        /// post绝对路径异步请求
        /// </summary>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="requestObject">请求的对象</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPostByAbsoluteUriAsync(string absoluteUri, object requestObject, string bearerToken = "")
        {
            return GetHttpClient().AddAuthorizationHeader(bearerToken).PostAsync(absoluteUri, ObjectToByteArrayContent(requestObject).AddJsonContentType());
        }

        /// <summary>
        /// Post绝对路径同步请求
        /// </summary>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="requestObject">请求的对象</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPostByAbsoluteUri(string absoluteUri, object requestObject, string bearerToken = "")
        {
            return HttpPostByAbsoluteUriAsync(absoluteUri, requestObject, bearerToken).Result;
        }

        /// <summary>
        /// get绝对路径异步请求
        /// </summary>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpGetByAbsoluteUriAsync(string absoluteUri, string bearerToken = "")
        {
            return GetHttpClient().AddAuthorizationHeader(bearerToken).GetAsync(absoluteUri);
        }

        /// <summary>
        /// get绝对路径同步请求
        /// </summary>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpGetByAbsoluteUri(string absoluteUri, string bearerToken = "")
        {
            return HttpGetByAbsoluteUriAsync(absoluteUri, bearerToken).Result;
        }

        /// <summary>
        /// put绝对路径异步请求
        /// </summary>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="requestObject">请求的对象</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPutByAbsoluteUriAsync(string absoluteUri, object requestObject, string bearerToken = "")
        {
            return GetHttpClient().AddAuthorizationHeader(bearerToken).PutAsync(absoluteUri, ObjectToByteArrayContent(requestObject).AddJsonContentType());
        }

        /// <summary>
        /// put绝对路径同步请求
        /// </summary>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="requestObject">请求的对象</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPutByAbsoluteUri(string absoluteUri, object requestObject, string bearerToken = "")
        {
            return HttpPutByAbsoluteUriAsync(absoluteUri, requestObject, bearerToken).Result;
        }

        /// <summary>
        /// delete绝对路径异步请求
        /// </summary>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpDeleteByAbsoluteUriAsync(string absoluteUri, string bearerToken = "")
        {
            return GetHttpClient().AddAuthorizationHeader(bearerToken).DeleteAsync(absoluteUri);
        }

        /// <summary>
        /// delete绝对路径同步请求
        /// </summary>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpDeleteByAbsoluteUri(string absoluteUri, string bearerToken = "")
        {
            return HttpDeleteByAbsoluteUriAsync(absoluteUri, bearerToken).Result;
        }

        private static HttpClient GetHttpClient(string url)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);
            return httpClient;
        }

        private static HttpClient GetHttpClient()
        {
            return new HttpClient();
        }

        /// <summary>
        /// 异步获取请求结果
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public static async Task<T> GetResponseAsync<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
            else if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.PaymentRequired)
                throw new DealException(await response.Content.ReadAsStringAsync());
            else
                throw new Exception(await response.Content.ReadAsStringAsync());
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

        /// <summary>
        /// 根据Object获取httpcontext
        /// </summary>
        /// <param name="requestObject"></param>
        /// <returns></returns>
        public static HttpContent ObjectToByteArrayContent(object requestObject)
        {
            return new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestObject)));
        }
    }
}