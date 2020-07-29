using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 微服务接口调用辅助类
    /// </summary>
    public class MicroServiceHelper
    {
        private static T ReturnEntity<T>(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                return HttpJsonHelper.GetResponse<T>(httpResponseMessage);

             CheckReturn(microServiceName, httpResponseMessage);

            throw new DealException($"{microServiceName}接口调用失败");
        }

        private static bool ReturnEntity(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                return true;

            CheckReturn(microServiceName, httpResponseMessage);

            throw new DealException($"{microServiceName}接口调用失败");
        }

        private static void CheckReturn(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.PaymentRequired)
                throw new DealException($"{microServiceName}接口调用失败,原因{httpResponseMessage.Content.ReadAsStringAsync().Result}");

            if (httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized)
                throw new DealException($"{microServiceName}接口调用失败,原因未认证系统");

            if (httpResponseMessage.StatusCode == HttpStatusCode.ServiceUnavailable)
                throw new DealException($"{microServiceName}服务调用超时");

            if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                throw new DealException($"未发现{microServiceName}服务数据");

            if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError)
                throw new DealException($"{microServiceName}服务内部错误");
        }

        /// <summary>
        /// 微服务Post
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="sendText">参数</param>
        /// <returns></returns>
        public static T SendMicroServicePost<T>(IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, object sendText)
        {
            ByteArrayContent httpContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sendText)));

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]
                                );

            return ReturnEntity<T>(microServiceName, httpResponseMessage);
        }

        /// <summary>
        /// 微服务Post
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="sendText">参数</param>
        /// <returns></returns>
        public static bool SendMicroServicePost(IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, object sendText)
        {
            ByteArrayContent httpContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sendText)));

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]
                                );

            return ReturnEntity(microServiceName, httpResponseMessage);
        }

        /// <summary>
        /// 微服务Put
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="sendText">参数</param>
        /// <returns></returns>
        public static bool SendMicroServicePut(IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, object sendText)
        {
            ByteArrayContent httpContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sendText)));

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPut(
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]
                                );

            return ReturnEntity(microServiceName, httpResponseMessage);
        }

        /// <summary>
        ///  通过url Post
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpContextAccessor"></param>
        /// <param name="url"></param>
        /// <param name="functionName"></param>
        /// <param name="displayName"></param>
        /// <param name="sendText"></param>
        /// <returns></returns>
        public static T SendByUrlPost<T>(string url, string functionName, string displayName, object sendText)
        {
            ByteArrayContent httpContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(sendText)));

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(
                                $"{url}",
                                $"{functionName}",
                                httpContent);

            return ReturnEntity<T>(displayName, httpResponseMessage);
        }

        /// <summary>
        /// 微服务Get，通过ID
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="parameter">ID值</param>
        /// <returns></returns>
        public static JObject MicroServiceGetByID(IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, string parameter)
        {
            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpGet(
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}/{parameter}",
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

            return ReturnEntity<JObject>(microServiceName, httpResponseMessage);
        }

        /// <summary>
        /// 微服务Get，通过条件
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="parameter">参数</param>
        /// <returns></returns>
        public static JObject MicroServiceGetByCondition(IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, string parameter)
        {
            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpGet(
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}?{parameter}",
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

            return ReturnEntity<JObject>(microServiceName, httpResponseMessage);
        }
    }
}