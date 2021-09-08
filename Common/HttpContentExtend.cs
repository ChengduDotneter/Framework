using Common.Const;
using System.Net.Http;

namespace Common
{
    /// <summary>
    ///Httpclient的扩展类
    /// </summary>
    public static class HttpClientExtend
    {
        /// <summary>
        /// 添加AuthorizationHeader 添加权限认证
        /// </summary>
        /// <param name="httpClient">http请求基类</param>
        /// <param name="authorization">权限认证</param>
        /// <returns></returns>
        public static HttpClient AddAuthorizationHeader(this HttpClient httpClient, string authorization)
        {
            if (string.IsNullOrEmpty(authorization))
                return httpClient;

            if (httpClient.DefaultRequestHeaders.Contains(HttpHeaderConst.AUTHORIZATION))
                httpClient.DefaultRequestHeaders.Remove(HttpHeaderConst.AUTHORIZATION);

            if (!string.IsNullOrWhiteSpace(authorization))
                httpClient.DefaultRequestHeaders.Add(HttpHeaderConst.AUTHORIZATION, authorization);

            return httpClient;
        }
    }

    /// <summary>
    /// HttpContent扩展类
    /// </summary>
    public static class HttpContentExtend
    {
        /// <summary>
        /// 添加JsonContenttype的请求头
        /// </summary>
        /// <param name="httpContent"></param>
        /// <returns></returns>
        public static HttpContent AddJsonContentType(this HttpContent httpContent)
        {
            if (httpContent.Headers.Contains(HttpHeaderConst.CONTENT_TYPE))
                httpContent.Headers.Remove(HttpHeaderConst.CONTENT_TYPE);

            httpContent.Headers.Add(HttpHeaderConst.CONTENT_TYPE, ContentTypeConst.APPLICATION_JSON);

            return httpContent;
        }
    }
}