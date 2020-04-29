using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// HTTP请求帮助类
    /// </summary>
    public static class HttpHelper
    {
        public static Task<HttpResponseMessage> HttpPostAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            content.Headers.Add("Content-Type", "application/json");
            return GetHttpClient(url, bearerToken).PostAsync(action, content);
        }

        public static HttpResponseMessage HttpPost(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPostAsync(url, action, content, bearerToken).Result;
        }

        public static Task<HttpResponseMessage> HttpGetAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url, bearerToken).GetAsync(action);
        }

        public static HttpResponseMessage HttpGet(string url, string action, string bearerToken = "")
        {
            return HttpGetAsync(url, action, bearerToken).Result;
        }

        public static Task<HttpResponseMessage> HttpPutAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            content.Headers.Add("Content-Type", "application/json");
            return GetHttpClient(url, bearerToken).PutAsync(action, content);
        }

        public static HttpResponseMessage HttpPut(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPutAsync(url, action, content, bearerToken).Result;
        }

        public static Task<HttpResponseMessage> HttpDeleteAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url, bearerToken).DeleteAsync(action);
        }

        public static HttpResponseMessage HttpDelete(string url, string action, string bearerToken = "")
        {
            return HttpDeleteAsync(url, action, bearerToken).Result;
        }

        private static HttpClient GetHttpClient(string url, string bearerToken = "")
        {
            HttpClient httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(url);

            if (!string.IsNullOrEmpty(bearerToken))
                httpClient.DefaultRequestHeaders.Add("Authorization", bearerToken);

            return httpClient;
        }

        public static async Task<T> GetResponseAsync<T>(HttpResponseMessage response)
        {
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception();

            return JsonConvert.DeserializeObject<T>(await response.Content.ReadAsStringAsync());
        }

        public static T GetResponse<T>(HttpResponseMessage response)
        {
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception();

            return JsonConvert.DeserializeObject<T>(response.Content.ReadAsStringAsync().Result);
        }

    }
}
