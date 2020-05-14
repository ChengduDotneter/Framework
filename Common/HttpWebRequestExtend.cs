using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Common
{
    public static class HttpWebRequestExtend
    {
        //Authorization 请求头名称
        private const string AUTHORIZATION_HEADER_NAME = "Authorization";

        //Content-Type值（Json）
        private const string CONTENT_TYPE_JSON = "application/json";
        //Content-Type值（Form表单）
        private const string CONTENT_TYPE_FORM = "application/x-www-form-urlencoded";
        //http请求方式GET
        private const string HTTP_METHOD_GET = "GET";
        //http请求方式POST
        private const string HTTP_METHOD_POST = "POST";
        //http请求方式DELETE
        private const string HTTP_METHOD_DELETE = "DELETE";
        //http请求方式PUT
        private const string HTTP_METHOD_PUT = "PUT";


        public static HttpWebRequest AddJsonContentType(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.ContentType = CONTENT_TYPE_JSON;
            return httpWebRequest;
        }

        public static HttpWebRequest AddFormContentType(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.ContentType = CONTENT_TYPE_FORM;
            return httpWebRequest;
        }

        public static HttpWebRequest AddPostMethod(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Method = HTTP_METHOD_POST;
            return httpWebRequest;
        }

        public static HttpWebRequest AddGetMethod(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Method = HTTP_METHOD_GET;
            return httpWebRequest;
        }

        public static HttpWebRequest AddDeleteMethod(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Method = HTTP_METHOD_DELETE;
            return httpWebRequest;
        }

        public static HttpWebRequest AddPutMethod(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Method = HTTP_METHOD_PUT;
            return httpWebRequest;
        }

        public static HttpWebRequest AddAuthorizationHeader(this HttpWebRequest httpWebRequest, string authorization)
        {
            if (httpWebRequest.Headers.AllKeys.Contains(AUTHORIZATION_HEADER_NAME))
                httpWebRequest.Headers.Remove(AUTHORIZATION_HEADER_NAME);

            if (!string.IsNullOrWhiteSpace(authorization))
                httpWebRequest.Headers.Add(AUTHORIZATION_HEADER_NAME, authorization);

            return httpWebRequest;
        }

        public static HttpWebRequest AddContent(this HttpWebRequest httpWebRequest, byte[] postData)
        {
            httpWebRequest.ContentLength = postData.Length;

            using (Stream reqStream = httpWebRequest.GetRequestStream())
            {
                reqStream.Write(postData, 0, postData.Length);
                reqStream.Close();
            }

            return httpWebRequest;
        }

        public static HttpWebResponseResult GetResponseData(this HttpWebRequest httpWebRequest)
        {
            HttpWebResponse httpWebResponse;

            try
            {
                httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            }
            catch (WebException ex)
            {
                httpWebResponse = (HttpWebResponse)ex.Response;
            }

            Stream stream = httpWebResponse.GetResponseStream();

            string returnString = null;

            //获取响应内容
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                returnString = reader.ReadToEnd();
            }

            return new HttpWebResponseResult(httpWebResponse.StatusCode, returnString);
        }
    }

    public class HttpWebResponseResult
    {
        public HttpStatusCode HttpStatus { get; set; }
        public string DataString { get; set; }

        public HttpWebResponseResult(HttpStatusCode httpStatus, string dataString = null)
        {
            HttpStatus = httpStatus;
            DataString = dataString;
        }
    }
}
