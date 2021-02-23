using Microsoft.AspNetCore.Http;

namespace Common
{
    public static class HttpResponseExtension
    {
        public static HttpResponse SetJsonContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType("application/json");
        }

        public static HttpResponse SetXMLContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType("application/xml");
        }

        public static HttpResponse SetHTMLContentType(this HttpResponse httpResponse)
        {
            return httpResponse.SetContentType("text/html; charset=utf-8");
        }

        internal static HttpResponse SetContentType(this HttpResponse httpResponse, string contentType)
        {
            httpResponse.ContentType = contentType;
            return httpResponse;
        }
    }
}
