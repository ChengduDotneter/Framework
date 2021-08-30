using Common.Const;
using Microsoft.AspNetCore.Http;

namespace Common
{
    /// <summary>
    /// 设置http响应头
    /// </summary>
    public static class HttpResponseExtension
    {
        /// <summary>
        /// 设置application/json内容类型响应标头
        /// </summary>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        public static HttpResponse SetJsonContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType(ContentTypeConst.APPLICATION_JSON);
        }
        /// <summary>
        /// 设置xml内容类型响应标头
        /// </summary>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        public static HttpResponse SetXMLContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType(ContentTypeConst.APPLICATION_XML);
        }
        /// <summary>
        /// 设置text/html; charset=utf-8
        /// </summary>
        /// <param name="httpResponse"></param>
        /// <returns></returns>
        public static HttpResponse SetHTMLContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType(ContentTypeConst.TEXT_HTML_UTF8);
        }
        /// <summary>
        /// 设置内容类型响应标头
        /// </summary>
        /// <param name="httpResponse">http请求</param>
        /// <param name="contentType">内容类型</param>
        /// <returns></returns>
        internal static HttpResponse SetContentType(this HttpResponse httpResponse, string contentType)
        {
            httpResponse.ContentType = contentType;
            return httpResponse;
        }
    }
}
