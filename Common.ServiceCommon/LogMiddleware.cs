using Common.Const;
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
            m_logHelper = LogHelperFactory.GetDefaultLogHelper();
        }

        /// <summary>
        /// 日志中间件构造函数
        /// </summary>
        /// <param name="logSearchAction"></param>
        /// <param name="next">管道下一个执行的委托</param>
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
        public async Task Invoke(HttpContext httpContext)//http请求
        {
            Endpoint endpoint = httpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IEndpointFeature>()?.Endpoint;//获取请求的功能

            if (endpoint == null)//为空则执行日志管道的下一步
            {
                await m_next(httpContext);
                return;
            }

            HttpMethodMetadata httpMethodMetadata = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();//表示路由期间使用的HTTP方法元数据
            ControllerActionDescriptor controllerActionDescriptor = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();//http方法请求的控制器

            string parameterInfo = await GetCallParameter(httpContext);

            if (httpMethodMetadata != null &&
                controllerActionDescriptor != null &&
               (m_logSearchAction ||
               (httpMethodMetadata.HttpMethods.Count == 1 &&
                httpMethodMetadata.HttpMethods[0].ToUpper() != HttpMethodConst.GET_UPPER)))//判断不是get请求
            {
                if (controllerActionDescriptor.ControllerName != "Health")//不是心跳就记录一下
                    await m_logHelper.Info(controllerActionDescriptor.ControllerName, httpContext.Request.Method, controllerActionDescriptor.ActionName, parameterInfo);
            }
            try
            {
                await m_next(httpContext);
            }
            catch (DealException exception)
            {
                await ExceptionHandling(httpContext, controllerActionDescriptor, exception, parameterInfo, StatusCodes.Status402PaymentRequired);
            }
            catch (ResourceException exception)
            {
                await ExceptionHandling(httpContext, controllerActionDescriptor, exception, parameterInfo, StatusCodes.Status402PaymentRequired, "系统繁忙，请稍后再试。");
            }
            catch (Exception exception)
            {
                await ExceptionHandling(httpContext, controllerActionDescriptor, exception, parameterInfo, StatusCodes.Status500InternalServerError, "内部异常");
            }
        }

        private static Task ExceptionHandling(HttpContext httpContext, ControllerActionDescriptor controllerActionDescriptor, Exception exception, string parameterInfo, int statusCode, string returnMessage = "")
        {
            string errorMessage = ExceptionHelper.GetMessage(exception);

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.SetHTMLContentType();

            m_logHelper.Error(controllerActionDescriptor.ControllerName, httpContext.Request.Method, httpContext.Response.StatusCode, errorMessage, controllerActionDescriptor.ActionName, parameterInfo, exception.StackTrace);

            return HttpResponseWritingExtensions.WriteAsync(httpContext.Response, string.IsNullOrWhiteSpace(returnMessage) ? errorMessage : returnMessage, Encoding.UTF8);
        }
        /// <summary>
        /// 获取http请求的参数
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        private static async Task<string> GetCallParameter(HttpContext httpContext)
        {
            StringBuilder parameter = new StringBuilder();
            string path = httpContext.Request.Path;

            if (httpContext.Request.Method == HttpMethodConst.GET_UPPER || httpContext.Request.Method == HttpMethodConst.DELETE_UPPER)//get与delete
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
            }//请求头不为空 请求头是application/json 内容长度不为空且小于1024*30
            else if (httpContext.Request.ContentType != null && httpContext.Request.ContentType.Contains(ContentTypeConst.APPLICATION_JSON) && httpContext.Request.ContentLength.HasValue && httpContext.Request.ContentLength.Value < MAX_JSON_LOG_SIZE)
                parameter.AppendLine(await LoadJsonFromBody(httpContext));//满足则添加进字符串里
            else
                parameter.Append("UNKNOWN");//不满足则无法识别

            return $"path: {path}{Environment.NewLine}{Environment.NewLine}parameter: {Environment.NewLine}{parameter}";//拼接后返回
        }
        /// <summary>
        /// 读取http请求携带的数据
        /// </summary>
        /// <param name="httpContext"></param>
        /// <returns></returns>
        private static async Task<string> LoadJsonFromBody(HttpContext httpContext)
        {
            httpContext.Request.EnableBuffering();

            byte[] jsonBuffer = await SteamHelper.ReadSteamToBufferAsync(httpContext.Request.Body, httpContext.Request.ContentLength ?? 0);

            httpContext.Request.Body.Seek(0, SeekOrigin.Begin);//指定流的开头

            return Encoding.UTF8.GetString(jsonBuffer);
        }
    }
}