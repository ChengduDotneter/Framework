using Common.Log;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 日志中间件
    /// </summary>
    public class LogMiddleware
    {
        private const int MAX_JSON_LOG_SIZE = 1024 * 30; //30k
        private readonly static ILogHelper m_logHelper;
        private readonly RequestDelegate m_next;
        private readonly bool m_logSearchAction;

        static LogMiddleware()
        {
            m_logHelper = LogHelperFactory.GetKafkaLogHelper();
        }

        /// <summary>
        /// 日志中间件构造函数
        /// </summary>
        /// <param name="logSearchAction"></param>
        /// <param name="next"></param>
        public LogMiddleware(bool logSearchAction, RequestDelegate next)
        {
            m_next = next;
            m_logSearchAction = logSearchAction;
        }

        /// <summary>
        /// 日志中间件管道操作
        /// </summary>
        /// <param name="httpContext">HttpContext</param>
        /// <returns></returns>
        public async Task Invoke(HttpContext httpContext)
        {
            Endpoint endpoint = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IEndpointFeature>()?.Endpoint;

            if (endpoint == null)
            {
                await m_next(httpContext);
                return;
            }

            HttpMethodMetadata httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();
            ControllerActionDescriptor controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();

            string parameterInfo = await GetCallParameter(httpContext);

            if (httpMethodMetadata != null &&
                controllerActionDescriptor != null &&
               (m_logSearchAction ||
               (httpMethodMetadata.HttpMethods.Count == 1 &&
                httpMethodMetadata.HttpMethods[0] != "Get")))
            {
                if (controllerActionDescriptor.ControllerName != "Health")
                    await m_logHelper.Info(controllerActionDescriptor.ControllerName, httpContext.Request.Method, controllerActionDescriptor.ActionName, parameterInfo);
            }
            try
            {
                await m_next(httpContext);
            }
            catch (DealException exception)
            {
                httpContext.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await HttpResponseWritingExtensions.WriteAsync(httpContext.Response, ExceptionHelper.GetMessage(exception), Encoding.UTF8);

                await m_logHelper.Error(controllerActionDescriptor.ControllerName, httpContext.Request.Method, httpContext.Response.StatusCode, ExceptionHelper.GetMessage(exception), controllerActionDescriptor.ActionName, parameterInfo, exception.StackTrace);
            }
            catch (Exception exception)
            {
                httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await HttpResponseWritingExtensions.WriteAsync(httpContext.Response, "内部异常", Encoding.UTF8);

                await m_logHelper.Error(controllerActionDescriptor.ControllerName, httpContext.Request.Method, httpContext.Response.StatusCode, ExceptionHelper.GetMessage(exception), controllerActionDescriptor.ActionName, parameterInfo, exception.StackTrace);
            }
        }

        private static async Task<string> GetCallParameter(HttpContext httpContext)
        {
            StringBuilder parameter = new StringBuilder();
            string path = httpContext.Request.Path;

            if (httpContext.Request.Method == "GET")
            {
                if (httpContext.Request.RouteValues.ContainsKey("id"))
                {
                    parameter.AppendLine($"id: {httpContext.GetRouteValue("id")}");
                }
                else if (httpContext.Request.Query.Count > 0)
                {
                    IQueryCollection query = httpContext.Request.Query;

                    for (int i = 0; i < query.Count; i++)
                    {
                        KeyValuePair<string, StringValues> queryItem = query.ElementAt(i);
                        parameter.AppendLine($"{queryItem.Key}: {(queryItem.Value.Count > 0 ? queryItem.Value.ToString() : "null")}");
                    }
                }
                else
                    parameter.Append("NULL");
            }
            else if (httpContext.Request.ContentType.Contains("application/json") && httpContext.Request.ContentLength.HasValue && httpContext.Request.ContentLength.Value < MAX_JSON_LOG_SIZE)
                parameter.AppendLine(await LoadJsonFromBody(httpContext));
            else
                parameter.Append("UNKNOWN");

            return $"path: {path}{Environment.NewLine}{Environment.NewLine}parameter: {Environment.NewLine}{parameter}";
        }

        private static async Task<string> LoadJsonFromBody(HttpContext httpContext)
        {
            httpContext.Request.EnableBuffering();

            byte[] jsonBuffer = await SteamHelper.ReadSteamToBufferAsync(httpContext.Request.Body, httpContext.Request.ContentLength ?? 0);

            httpContext.Request.Body.Seek(0, SeekOrigin.Begin);

            return Encoding.UTF8.GetString(jsonBuffer);
        }
    }
}