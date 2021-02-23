using Common.Const;
using Microsoft.AspNetCore.Http;

namespace Common
{
    public static class HttpResponseExtension
    {
        public static HttpResponse SetJsonContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType(ContentTypeConst.APPLICATION_JSON);
        }

        public static HttpResponse SetXMLContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType(ContentTypeConst.APPLICATION_XML);
        }

        public static HttpResponse SetHTMLContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType(ContentTypeConst.TEXT_HTML_UTF8);
        }

        internal static HttpResponse SetContentType(this HttpResponse httpResponse, string contentType)
        {
            httpResponse.ContentType = contentType;
            return httpResponse;
        }
    }
}
