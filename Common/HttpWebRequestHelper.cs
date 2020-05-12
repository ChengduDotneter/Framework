using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Common
{
    public class HttpWebRequestHelper
    {
        public static HttpWebResponseResult JsonPost(string url, byte[] postData, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddPostMethod().AddJsonContentType().AddContent(postData).GetResponseData();
        }

        public static HttpWebResponseResult JsonGet(string url, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddGetMethod().GetResponseData();
        }

        public static HttpWebResponseResult JsonPut(string url, byte[] postData, string bearerToken = "")
        {
            return GetHttpRequest(url, bearerToken).AddPutMethod().AddJsonContentType().AddContent(postData).GetResponseData();
        }

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
