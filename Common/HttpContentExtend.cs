using System.Net.Http;

namespace Common
{
    public static class HttpClientExtend
    {

        //Authorization 请求头名称
        private const string AUTHORIZATION_HEADER_NAME = "Authorization";



        public static HttpClient AddAuthorizationHeader(this HttpClient httpClient, string authorization)
        {
            if (httpClient.DefaultRequestHeaders.Contains(AUTHORIZATION_HEADER_NAME))
                httpClient.DefaultRequestHeaders.Remove(AUTHORIZATION_HEADER_NAME);

            if (!string.IsNullOrWhiteSpace(authorization))
                httpClient.DefaultRequestHeaders.Add(AUTHORIZATION_HEADER_NAME, authorization);

            return httpClient;
        }


    }

    public static class HttpContentExtend
    {
        //Content-Type 请求头名称
        private const string CONTENT_TYPE_HEADER_NAME = "Content-Type";
        //Content-Type 请求头值（Json）
        private const string CONTENT_TYPE_HEADER_VALUE_JSON = "application/json";

        public static HttpContent AddJsonContentType(this HttpContent httpContent)
        {
            if (httpContent.Headers.Contains(CONTENT_TYPE_HEADER_NAME))
                httpContent.Headers.Remove(CONTENT_TYPE_HEADER_NAME);

            httpContent.Headers.Add(CONTENT_TYPE_HEADER_NAME, CONTENT_TYPE_HEADER_VALUE_JSON);

            return httpContent;
        }
    }
}
