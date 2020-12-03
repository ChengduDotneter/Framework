using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// httpwebrequest的扩展类
    /// </summary>
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

        /// <summary>
        /// 添加JsonContenttype
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <returns></returns>

        public static HttpWebRequest AddJsonContentType(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.ContentType = CONTENT_TYPE_JSON;
            return httpWebRequest;
        }

        /// <summary>
        /// 添加FormContenttype
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <returns></returns>
        public static HttpWebRequest AddFormContentType(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.ContentType = CONTENT_TYPE_FORM;
            return httpWebRequest;
        }

        /// <summary>
        /// 设置post的请求方法
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <returns></returns>
        public static HttpWebRequest AddPostMethod(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Method = HTTP_METHOD_POST;
            return httpWebRequest;
        }

        /// <summary>
        /// 设置get的请求方法
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <returns></returns>
        public static HttpWebRequest AddGetMethod(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Method = HTTP_METHOD_GET;
            return httpWebRequest;
        }

        /// <summary>
        /// 设置delete的请求方法
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <returns></returns>
        public static HttpWebRequest AddDeleteMethod(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Method = HTTP_METHOD_DELETE;
            return httpWebRequest;
        }

        /// <summary>
        /// 设置put的请求方法
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <returns></returns>
        public static HttpWebRequest AddPutMethod(this HttpWebRequest httpWebRequest)
        {
            httpWebRequest.Method = HTTP_METHOD_PUT;
            return httpWebRequest;
        }

        /// <summary>
        /// 添加Authorization的请求头
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        public static HttpWebRequest AddAuthorizationHeader(this HttpWebRequest httpWebRequest, string authorization)
        {
            if (httpWebRequest.Headers.AllKeys.Contains(AUTHORIZATION_HEADER_NAME))
                httpWebRequest.Headers.Remove(AUTHORIZATION_HEADER_NAME);

            if (!string.IsNullOrWhiteSpace(authorization))
                httpWebRequest.Headers.Add(AUTHORIZATION_HEADER_NAME, authorization);

            return httpWebRequest;
        }

        /// <summary>
        /// 添加请求body
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <param name="postData"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 同步获取请求结果数据
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <returns></returns>
        public static HttpWebResponseResult GetResponseData(this HttpWebRequest httpWebRequest)
        {
            return httpWebRequest.GetResponseDataAsync().Result;
        }

        /// <summary>
        /// 异步获取请求结果数据
        /// </summary>
        /// <param name="httpWebRequest"></param>
        /// <returns></returns>
        public static async Task<HttpWebResponseResult> GetResponseDataAsync(this HttpWebRequest httpWebRequest)
        {
            HttpWebResponse httpWebResponse;

            try
            {
                httpWebResponse = (HttpWebResponse)await httpWebRequest.GetResponseAsync();
            }
            catch (WebException ex)
            {
                httpWebResponse = (HttpWebResponse)ex.Response;
            }

            if (httpWebResponse == null)
                throw new Exception($"{httpWebRequest.Address.AbsoluteUri}请求调用失败");

            Stream stream = httpWebResponse.GetResponseStream();

            string returnString = null;

            //获取响应内容
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                returnString = await reader.ReadToEndAsync();
            }

            return new HttpWebResponseResult(httpWebResponse.StatusCode, returnString);
        }
    }

    /// <summary>
    /// HttpWebResponse结果类
    /// </summary>
    public class HttpWebResponseResult
    {
        /// <summary>
        /// 请求状态码
        /// </summary>
        public HttpStatusCode HttpStatus { get; set; }

        /// <summary>
        /// 请求结果数据字符串
        /// </summary>
        public string DataString { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="httpStatus"></param>
        /// <param name="dataString"></param>
        public HttpWebResponseResult(HttpStatusCode httpStatus, string dataString = null)
        {
            HttpStatus = httpStatus;
            DataString = dataString;
        }
    }
}