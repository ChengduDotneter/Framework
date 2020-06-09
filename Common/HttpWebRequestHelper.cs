using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Common
{
    /// <summary>
    /// HttpWebRequest请求帮助类
    /// </summary>
    public class HttpWebRequestHelper
    {
        /// <summary>
        /// Json的Post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult JsonPost(string url, byte[] postData, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddPostMethod().AddJsonContentType().AddContent(postData).GetResponseData();
        }

        /// <summary>
        /// json的get请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult JsonGet(string url, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddGetMethod().GetResponseData();
        }

        /// <summary>
        /// json的put请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult JsonPut(string url, byte[] postData, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddPutMethod().AddJsonContentType().AddContent(postData).GetResponseData();
        }

        /// <summary>
        /// json的delete请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult JsonDelete(string url, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddDeleteMethod().GetResponseData();
        }

        private static HttpWebRequest GetHttpRequest(string url, string bearerToken = "")
        {
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

            if (!string.IsNullOrWhiteSpace(bearerToken))
                httpWebRequest.AddAuthorizationHeader(bearerToken);

            return httpWebRequest;
        }

        /// <summary>
        /// form表单的post请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="keyValues"></param>
        /// <param name="bearerToken"></param>
        /// <returns></returns>
        public static HttpWebResponseResult FormPost(string url, IDictionary<string, object> keyValues, string bearerToken = "")
        {
            StringBuilder builder = new StringBuilder();

            if (keyValues != null)
            {
                int i = 0;
                foreach (var item in keyValues)
                {
                    if (i > 0)
                        builder.Append("&");
                    builder.AppendFormat("{0}={1}", item.Key, item.Value);
                    i++;
                }
            }

            byte[] postData = Encoding.UTF8.GetBytes(builder.ToString());

            return GetHttpRequest(url, bearerToken).AddPostMethod().AddFormContentType().AddContent(postData).GetResponseData();
        }
    }


}
