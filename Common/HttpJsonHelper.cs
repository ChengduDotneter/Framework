using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
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
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="content">请求Body</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPostAsync(IHttpClientFactory httpClientFactory, string url, string action, HttpContent content, string bearerToken = "")
        {
            HttpClient httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(url);

            return httpClient.AddAuthorizationHeader(bearerToken).PostAsync(action, content.AddJsonContentType());
        }

        /// <summary>
        /// Post同步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="content">请求Body</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPost(IHttpClientFactory httpClientFactory, string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPostAsync(httpClientFactory, url, action, content, bearerToken).Result;
        }

        /// <summary>
        /// get异步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpGetAsync(IHttpClientFactory httpClientFactory, string url, string action, string bearerToken = "")
        {
            HttpClient httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(url);

            return httpClient.AddAuthorizationHeader(bearerToken).GetAsync(action);
        }

        /// <summary>
        /// get同步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpGet(IHttpClientFactory httpClientFactory, string url, string action, string bearerToken = "")
        {
            return HttpGetAsync(httpClientFactory, url, action, bearerToken).Result;
        }

        /// <summary>
        /// put异步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="content">请求Body</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPutAsync(IHttpClientFactory httpClientFactory, string url, string action, HttpContent content, string bearerToken = "")
        {
            HttpClient httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(url);

            return httpClient.AddAuthorizationHeader(bearerToken).PutAsync(action, content.AddJsonContentType());
        }

        /// <summary>
        /// put同步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="content">请求Body</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPut(IHttpClientFactory httpClientFactory, string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPutAsync(httpClientFactory, url, action, content, bearerToken).Result;
        }

        /// <summary>
        /// delete异步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpDeleteAsync(IHttpClientFactory httpClientFactory, string url, string action, string bearerToken = "")
        {
            HttpClient httpClient = httpClientFactory.CreateClient();
            httpClient.BaseAddress = new Uri(url);

            return httpClient.AddAuthorizationHeader(bearerToken).DeleteAsync(action);
        }

        /// <summary>
        /// delete同步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="url">请求基地址</param>
        /// <param name="action">请求方法</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpDelete(IHttpClientFactory httpClientFactory, string url, string action, string bearerToken = "")
        {
            return HttpDeleteAsync(httpClientFactory, url, action, bearerToken).Result;
        }

        /// <summary>
        /// post绝对路径异步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="requestObject">请求的对象</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPostByAbsoluteUriAsync(IHttpClientFactory httpClientFactory, string absoluteUri, object requestObject = null, string bearerToken = "")
        {
            return httpClientFactory.CreateClient().AddAuthorizationHeader(bearerToken).PostAsync(absoluteUri, ObjectToByteArrayContent(requestObject).AddJsonContentType());
        }

        /// <summary>
        /// Post绝对路径同步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="requestObject">请求的对象</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPostByAbsoluteUri(IHttpClientFactory httpClientFactory, string absoluteUri, object requestObject = null, string bearerToken = "")
        {
            return HttpPostByAbsoluteUriAsync(httpClientFactory, absoluteUri, requestObject, bearerToken).Result;
        }

        /// <summary>
        /// get绝对路径异步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpGetByAbsoluteUriAsync(IHttpClientFactory httpClientFactory, string absoluteUri, string bearerToken = "")
        {
            return httpClientFactory.CreateClient().AddAuthorizationHeader(bearerToken).GetAsync(absoluteUri);
        }

        /// <summary>
        /// get绝对路径同步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpGetByAbsoluteUri(IHttpClientFactory httpClientFactory, string absoluteUri, string bearerToken = "")
        {
            return HttpGetByAbsoluteUriAsync(httpClientFactory, absoluteUri, bearerToken).Result;
        }

        /// <summary>
        /// put绝对路径异步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="requestObject">请求的对象</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPutByAbsoluteUriAsync(IHttpClientFactory httpClientFactory, string absoluteUri, object requestObject = null, string bearerToken = "")
        {
            return httpClientFactory.CreateClient().AddAuthorizationHeader(bearerToken).PutAsync(absoluteUri, ObjectToByteArrayContent(requestObject).AddJsonContentType());
        }

        /// <summary>
        /// put绝对路径同步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="requestObject">请求的对象</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPutByAbsoluteUri(IHttpClientFactory httpClientFactory, string absoluteUri, object requestObject = null, string bearerToken = "")
        {
            return HttpPutByAbsoluteUriAsync(httpClientFactory, absoluteUri, requestObject, bearerToken).Result;
        }

        /// <summary>
        /// delete绝对路径异步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpDeleteByAbsoluteUriAsync(IHttpClientFactory httpClientFactory, string absoluteUri, string bearerToken = "")
        {
            return httpClientFactory.CreateClient().AddAuthorizationHeader(bearerToken).DeleteAsync(absoluteUri);
        }

        /// <summary>
        /// delete绝对路径同步请求
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="absoluteUri">请求绝对路径</param>
        /// <param name="bearerToken">Bearer验证，如不用Bearer认证则不传</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpDeleteByAbsoluteUri(IHttpClientFactory httpClientFactory, string absoluteUri, string bearerToken = "")
        {
            return HttpDeleteByAbsoluteUriAsync(httpClientFactory, absoluteUri, bearerToken).Result;
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
                return JsonSerializer.Deserialize<T>(await response.Content.ReadAsStringAsync());
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
            return new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(requestObject)));
        }
    }
}