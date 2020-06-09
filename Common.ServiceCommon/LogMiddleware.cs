using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 日志中间件
    /// </summary>
    public class LogMiddleware
    {
        private class ControllerLogKey
        {
            public string ControllerName { get; set; }
            public string ActionName { get; set; }

            public ControllerLogKey(string controllerName, string actionName)
            {
                ControllerName = controllerName;
                ActionName = actionName;
            }

            public override bool Equals(object obj)
            {
                if (obj is ControllerLogKey controllerLogKey)
                    return controllerLogKey == this;

                return false;
            }

            public override int GetHashCode()
            {
                return ControllerName.GetHashCode() ^ ActionName.GetHashCode();
            }

            public static bool operator ==(ControllerLogKey a, ControllerLogKey b)
            {
                return a.ControllerName == b.ControllerName &&
                       a.ActionName == b.ActionName;
            }

            public static bool operator !=(ControllerLogKey a, ControllerLogKey b)
            {
                return a.ControllerName != b.ControllerName ||
                       a.ActionName != b.ActionName;
            }
        }

        private const int MAX_JSON_LOG_SIZE = 1024 * 6; //6k
        private static IDictionary<ControllerLogKey, ILog> m_controllerLogs;
        private RequestDelegate m_next;
        private bool m_logSearchAction;

        static LogMiddleware()
        {
            m_controllerLogs = new Dictionary<ControllerLogKey, ILog>();
        }

        /// <summary>
        /// 日志中间件
        /// </summary>
        /// <param name="logSearchAction"></param>
        /// <param name="next"></param>
        public LogMiddleware(bool logSearchAction, RequestDelegate next)
        {
            m_next = next;
            m_logSearchAction = logSearchAction;
        }

        /// <summary>
        /// 
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
                GetControllerLog(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName).Info(parameterInfo);
            }

            try
            {
                await m_next(httpContext);
            }
            catch (DealException exception)
            {
                string error = $"parameter_info: {parameterInfo}{Environment.NewLine}{Environment.NewLine}exception_message: {Environment.NewLine}{exception.Message}{Environment.NewLine}{Environment.NewLine}stack_trace: {Environment.NewLine}{exception.StackTrace}";
                GetControllerLog(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName).Error(error);

                httpContext.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                await HttpResponseWritingExtensions.WriteAsync(httpContext.Response, exception.Message, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                string error = $"parameter_info: {parameterInfo}{Environment.NewLine}{Environment.NewLine}exception_message: {Environment.NewLine}{exception.Message}{Environment.NewLine}{Environment.NewLine}stack_trace: {Environment.NewLine}{exception.StackTrace}";
                GetControllerLog(controllerActionDescriptor.ControllerName, controllerActionDescriptor.ActionName).Error(error);

                throw new Exception("内部异常");
            }
        }

        private static ILog GetControllerLog(string controllerName, string actionName)
        {
            ControllerLogKey controllerLogKey = new ControllerLogKey(controllerName, actionName);

            if (!m_controllerLogs.ContainsKey(controllerLogKey))
            {
                lock (m_controllerLogs)
                {
                    if (!m_controllerLogs.ContainsKey(controllerLogKey))
                        m_controllerLogs.Add(controllerLogKey, LogHelper.CreateLog("controller", controllerName, actionName));
                }
            }

            return m_controllerLogs[controllerLogKey];
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
                {
                    parameter.Append("NULL");
                }
            }
            else if (httpContext.Request.ContentType == "application/json" && httpContext.Request.ContentLength.HasValue && httpContext.Request.ContentLength.Value < MAX_JSON_LOG_SIZE)
            {
                parameter.AppendLine(await LoadJsonFromBody(httpContext));
            }
            else
            {
                parameter.Append("UNKNOWN");
            }

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
