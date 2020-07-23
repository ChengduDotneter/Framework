using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// HTTPJsonHelper���������
    /// </summary>
    public static class HttpJsonHelper
    {
        /// <summary>
        /// post�첽����
        /// </summary>
        /// <param name="url">�������ַ</param>
        /// <param name="action">���󷽷�</param>
        /// <param name="content">����Body</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPostAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).PostAsync(action, content.AddJsonContentType());
        }

        /// <summary>
        /// Postͬ������
        /// </summary>
        /// <param name="url">�������ַ</param>
        /// <param name="action">���󷽷�</param>
        /// <param name="content">����Body</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPost(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPostAsync(url, action, content, bearerToken).Result;
        }

        /// <summary>
        /// get�첽����
        /// </summary>
        /// <param name="url">�������ַ</param>
        /// <param name="action">���󷽷�</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpGetAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).GetAsync(action);
        }

        /// <summary>
        /// getͬ������
        /// </summary>
        /// <param name="url">�������ַ</param>
        /// <param name="action">���󷽷�</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpGet(string url, string action, string bearerToken = "")
        {
            return HttpGetAsync(url, action, bearerToken).Result;
        }

        /// <summary>
        /// put�첽����
        /// </summary>
        /// <param name="url">�������ַ</param>
        /// <param name="action">���󷽷�</param>
        /// <param name="content">����Body</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPutAsync(string url, string action, HttpContent content, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).PutAsync(action, content.AddJsonContentType());
        }

        /// <summary>
        /// putͬ������
        /// </summary>
        /// <param name="url">�������ַ</param>
        /// <param name="action">���󷽷�</param>
        /// <param name="content">����Body</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPut(string url, string action, HttpContent content, string bearerToken = "")
        {
            return HttpPutAsync(url, action, content, bearerToken).Result;
        }

        /// <summary>
        /// delete�첽����
        /// </summary>
        /// <param name="url">�������ַ</param>
        /// <param name="action">���󷽷�</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpDeleteAsync(string url, string action, string bearerToken = "")
        {
            return GetHttpClient(url).AddAuthorizationHeader(bearerToken).DeleteAsync(action);
        }

        /// <summary>
        /// deleteͬ������
        /// </summary>
        /// <param name="url">�������ַ</param>
        /// <param name="action">���󷽷�</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpDelete(string url, string action, string bearerToken = "")
        {
            return HttpDeleteAsync(url, action, bearerToken).Result;
        }

        /// <summary>
        /// post����·���첽����
        /// </summary>
        /// <param name="absoluteUri">�������·��</param>
        /// <param name="requestObject">����Ķ���</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPostByAbsoluteUriAsync(string absoluteUri, object requestObject, string bearerToken = "")
        {
            return GetHttpClient().AddAuthorizationHeader(bearerToken).PostAsync(absoluteUri, ObjectToByteArrayContent(requestObject).AddJsonContentType());
        }

        /// <summary>
        /// Post����·��ͬ������
        /// </summary>
        /// <param name="absoluteUri">�������·��</param>
        /// <param name="requestObject">����Ķ���</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPostByAbsoluteUri(string absoluteUri, object requestObject, string bearerToken = "")
        {
            return HttpPostByAbsoluteUriAsync(absoluteUri, requestObject, bearerToken).Result;
        }

        /// <summary>
        /// get����·���첽����
        /// </summary>
        /// <param name="absoluteUri">�������·��</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpGetByAbsoluteUriAsync(string absoluteUri, string bearerToken = "")
        {
            return GetHttpClient().AddAuthorizationHeader(bearerToken).GetAsync(absoluteUri);
        }

        /// <summary>
        /// get����·��ͬ������
        /// </summary>
        /// <param name="absoluteUri">�������·��</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpGetByAbsoluteUri(string absoluteUri, string bearerToken = "")
        {
            return HttpGetByAbsoluteUriAsync(absoluteUri, bearerToken).Result;
        }

        /// <summary>
        /// put����·���첽����
        /// </summary>
        /// <param name="absoluteUri">�������·��</param>
        /// <param name="requestObject">����Ķ���</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpPutByAbsoluteUriAsync(string absoluteUri, object requestObject, string bearerToken = "")
        {
            return GetHttpClient().AddAuthorizationHeader(bearerToken).PutAsync(absoluteUri, ObjectToByteArrayContent(requestObject).AddJsonContentType());
        }

        /// <summary>
        /// put����·��ͬ������
        /// </summary>
        /// <param name="absoluteUri">�������·��</param>
        /// <param name="requestObject">����Ķ���</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static HttpResponseMessage HttpPutByAbsoluteUri(string absoluteUri, object requestObject, string bearerToken = "")
        {
            return HttpPutByAbsoluteUriAsync(absoluteUri, requestObject, bearerToken).Result;
        }

        /// <summary>
        /// delete����·���첽����
        /// </summary>
        /// <param name="absoluteUri">�������·��</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
        /// <returns></returns>
        public static Task<HttpResponseMessage> HttpDeleteByAbsoluteUriAsync(string absoluteUri, string bearerToken = "")
        {
            return GetHttpClient().AddAuthorizationHeader(bearerToken).DeleteAsync(absoluteUri);
        }

        /// <summary>
        /// delete����·��ͬ������
        /// </summary>
        /// <param name="absoluteUri">�������·��</param>
        /// <param name="bearerToken">Bearer��֤���粻��Bearer��֤�򲻴�</param>
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
        /// �첽��ȡ������
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
        /// ͬ����ȡ������
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public static T GetResponse<T>(HttpResponseMessage response)
        {
            return GetResponseAsync<T>(response).Result;
        }

        /// <summary>
        /// ����Object��ȡhttpcontext
        /// </summary>
        /// <param name="requestObject"></param>
        /// <returns></returns>
        public static HttpContent ObjectToByteArrayContent(object requestObject)
        {
            return new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestObject)));
        }
    }
}