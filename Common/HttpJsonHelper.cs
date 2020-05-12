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
        public static Task<HttpResponseMessage> HttpPostAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            return GetHttpClient(url).AddJsonContentType().AddAuthorizationHeader(bearerToken).PostAsync(action, content);
        }

        public static HttpResponseMessage HttpPost(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPostAsync(url, action, content, bearerToken).Result;
        }

        public static Task<HttpResponseMessage> HttpGetAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url).AddJsonContentType().AddAuthorizationHeader(bearerToken).GetAsync(action);
        }

        public static HttpResponseMessage HttpGet(string url, string action, string bearerToken = "")
        {
            return HttpGetAsync(url, action, bearerToken).Result;
        }

        public static Task<HttpResponseMessage> HttpPutAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            return GetHttpClient(url).AddJsonContentType().AddAuthorizationHeader(bearerToken).PutAsync(action, content);
        }

        public static HttpResponseMessage HttpPut(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPutAsync(url, action, content, bearerToken).Result;
        }

        public static Task<HttpResponseMessage> HttpDeleteAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url).AddJsonContentType().AddAuthorizationHeader(bearerToken).DeleteAsync(action);
        }

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

        public static async Task<T> GetResponseAsync<T>(HttpResponseMessage response)
        {
            if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.PaymentRequired)
                throw new DealException(await response.Content.ReadAsStringAsync());

            else if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception(await response.Content.ReadAsStringAsync());

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public static T GetResponse<T>(HttpResponseMessage response)
        {
            return GetResponseAsync<T>(response).Result;
        }

    }
}
