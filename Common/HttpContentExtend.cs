using System.Net.Http;

namespace Common
{
    public static class HttpClientExtend
    {
        //Content-Type 请求头名称
        private const string CONTENT_TYPE_HEADER_NAME = "Content-Type";
        //Authorization 请求头名称
        private const string AUTHORIZATION_HEADER_NAME = "Authorization";

        //Content-Type 请求头值（Json）
        private const string CONTENT_TYPE_HEADER_VALUE_JSON = "application/json";

        public static HttpClient AddJsonContentType(this HttpClient httpClient)
        {
            if (httpClient.DefaultRequestHeaders.Contains(CONTENT_TYPE_HEADER_NAME))
                httpClient.DefaultRequestHeaders.Remove(CONTENT_TYPE_HEADER_NAME);

            httpClient.DefaultRequestHeaders.Add(CONTENT_TYPE_HEADER_NAME, CONTENT_TYPE_HEADER_VALUE_JSON);

            return httpClient;
        }

        public static HttpClient AddAuthorizationHeader(this HttpClient httpClient, string authorization)
        {
            if (httpClient.DefaultRequestHeaders.Contains(AUTHORIZATION_HEADER_NAME))
                httpClient.DefaultRequestHeaders.Remove(AUTHORIZATION_HEADER_NAME);

            if (!string.IsNullOrWhiteSpace(authorization))
                httpClient.DefaultRequestHeaders.Add(AUTHORIZATION_HEADER_NAME, authorization);

            return httpClient;
        }


    }
}
