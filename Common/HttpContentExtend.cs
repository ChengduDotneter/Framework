using System.Net.Http;

namespace Common
{
    /// <summary>
    ///Httpclient的扩展类
    /// </summary>
    public static class HttpClientExtend
    {
        //Authorization 请求头名称
        private const string AUTHORIZATION_HEADER_NAME = "Authorization";

        /// <summary>
        /// 添加AuthorizationHeader
        /// </summary>
        /// <param name="httpClient"></param>
        /// <param name="authorization"></param>
        /// <returns></returns>
        public static HttpClient AddAuthorizationHeader(this HttpClient httpClient, string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return httpClient;

            if (httpClient.DefaultRequestHeaders.Contains(AUTHORIZATION_HEADER_NAME))
                httpClient.DefaultRequestHeaders.Remove(AUTHORIZATION_HEADER_NAME);

            if (!string.IsNullOrWhiteSpace(authorization))
                httpClient.DefaultRequestHeaders.Add(AUTHORIZATION_HEADER_NAME, authorization);

            return httpClient;
        }
    }

    /// <summary>
    /// HttpContent扩展类
    /// </summary>
    public static class HttpContentExtend
    {
        //Content-Type 请求头名称
        private const string CONTENT_TYPE_HEADER_NAME = "Content-Type";

        //Content-Type 请求头值（Json）
        private const string CONTENT_TYPE_HEADER_VALUE_JSON = "application/json";

        /// <summary>
        /// 添加JsonContenttype的请求头
        /// </summary>
        /// <param name="httpContent"></param>
        /// <returns></returns>
        public static HttpContent AddJsonContentType(this HttpContent httpContent)
        {
            if (httpContent.Headers.Contains(CONTENT_TYPE_HEADER_NAME))
                httpContent.Headers.Remove(CONTENT_TYPE_HEADER_NAME);

            httpContent.Headers.Add(CONTENT_TYPE_HEADER_NAME, CONTENT_TYPE_HEADER_VALUE_JSON);

            return httpContent;
        }
    }
}