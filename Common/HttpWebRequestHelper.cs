using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// HttpWebRequest请求帮助类
    /// </summary>
    public class HttpWebRequestHelper
    {
        /// <summary>
        /// Json的同步Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult JsonPost(string url, byte[] postData, string bearerToken = "")
        {
            return JsonPostAsync(url, postData, bearerToken).Result;
        }

        /// <summary>
        /// Json的异步Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpWebResponseResult> JsonPostAsync(string url, byte[] postData, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddPostMethod().AddJsonContentType().AddContent(postData).GetResponseDataAsync();
        }

        /// <summary>
        /// json的get同步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult JsonGet(string url, string bearerToken = "")
        {
            return JsonGetAsync(url, bearerToken).Result;
        }

        /// <summary>
        /// json的get异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpWebResponseResult> JsonGetAsync(string url, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddGetMethod().GetResponseDataAsync();
        }

        /// <summary>
        /// json的put同步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult JsonPut(string url, byte[] postData, string bearerToken = "")
        {
            return JsonPutAsync(url, postData, bearerToken).Result;
        }

        /// <summary>
        /// json的post异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpWebResponseResult> JsonPutAsync(string url, byte[] postData, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddPutMethod().AddJsonContentType().AddContent(postData).GetResponseDataAsync();
        }

        /// <summary>
        /// json的delete同步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult JsonDelete(string url, string bearerToken = "")
        {
            return JsonDeleteAsync(url, bearerToken).Result;
        }

        /// <summary>
        /// json的delete异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpWebResponseResult> JsonDeleteAsync(string url, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddDeleteMethod().GetResponseDataAsync();
        }

        private static HttpWebRequest GetHttpRequest(string url, string bearerToken = "")
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

            if (!string.IsNullOrWhiteSpace(bearerToken))
                httpWebRequest.AddAuthorizationHeader(bearerToken);

            return httpWebRequest;
        }

        /// <summary>
        /// form表单的post同步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="keyValues"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult FormPost(string url, IDictionary<string, object> keyValues, string bearerToken = "")
        {
            return FormPostAsync(url, keyValues, bearerToken).Result;
        }

        /// <summary>
        /// form表单的post异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="keyValues"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpWebResponseResult> FormPostAsync(string url, IDictionary<string, object> keyValues, string bearerToken = "")
        {
            return FormPostAsyncByEncoding(url, keyValues, bearerToken);
        }

        /// <summary>
        /// form表单的post异步请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="keyValues"></param>
        /// <param name="encoding"></param>
        /// <param name="buildValue"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static Task<HttpWebResponseResult> FormPostAsyncByEncoding(string url, IDictionary<string, object> keyValues, string bearerToken = "", Encoding encoding = null, Func<object, string> buildValue = null)
        {
            StringBuilder builder = new StringBuilder();

            if (keyValues != null)
            {
                int i = 0;
                foreach (var item in keyValues)
                {
                    if (i > 0)
                        builder.Append("&");
                    builder.AppendFormat("{0}={1}", item.Key, buildValue == null ? buildValue.Invoke(item.Value) : item.Value);
                    i++;
                }
            }

            byte[] postData = (encoding ?? Encoding.UTF8).GetBytes(builder.ToString());

            return GetHttpRequest(url, bearerToken).AddPostMethod().AddFormContentType().AddContent(postData).GetResponseDataAsync();
        }
    }
}