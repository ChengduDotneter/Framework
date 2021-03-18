using Microsoft.AspNetCore.Http;

namespace Common.ServiceCommon
{
    public static class HttpContextExtentions
    {
        public static string GetSecWebSocketProtocol(this HttpContext httpContext)
        {
            return httpContext.Request.Headers["Sec-WebSocket-Protocol"].ToString();
        }
    }
}
