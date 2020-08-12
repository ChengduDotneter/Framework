using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;

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
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="sendText">参数</param>
        /// <returns></returns>
        public static T SendMicroServicePost<T>(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, object sendText)
        {
            HttpContent httpContent = HttpJsonHelper.ObjectToByteArrayContent(sendText);

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(
                                httpClientFactory,
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
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="sendText">参数</param>
        /// <returns></returns>
        public static bool SendMicroServicePost(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, object sendText)
        {
            HttpContent httpContent = HttpJsonHelper.ObjectToByteArrayContent(sendText);

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]
                                );

            return ReturnEntity(microServiceName, httpResponseMessage);
        }

        /// <summary>
        /// 微服务Get，通过ID
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="parameter">ID值</param>
        /// <returns></returns>
        public static JObject MicroServiceGetByID(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, string parameter)
        {
            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpGet(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}/{parameter}",
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

            return ReturnEntity<JObject>(microServiceName, httpResponseMessage);
        }

        /// <summary>
        /// 微服务Get，通过条件
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="parameter">参数</param>
        /// <returns></returns>
        public static JObject MicroServiceGetByCondition(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, string parameter)
        {
            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpGet(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}?{parameter}",
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

            return ReturnEntity<JObject>(microServiceName, httpResponseMessage);
        }
    }

    public class PHPMicroServiceHelper
    {
        private static T ReturnEntity<T>(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                return CheckReturn<T>(microServiceName, httpResponseMessage);

            throw new DealException($"{microServiceName}接口调用失败");
        }

        private static T CheckReturn<T>(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            JObject jObject = JsonConvert.DeserializeObject<JObject>(httpResponseMessage.Content.ReadAsStringAsync().Result);

            if (JTokenHelper.GetIntValue(jObject["code"]) != 200)
                throw new DealException($"{microServiceName}接口调用失败,原因:{JTokenHelper.GetStringValue(jObject["msg"])}");

            else if (!jObject["data"].HasValues)
                throw new DealException($"{microServiceName}接口调用失败,无返回数据");

            else return JsonConvert.DeserializeObject<T>(jObject["data"].ToString());
        }

        public static T MicroServiceGetByCondition<T>(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, string parameter)
        {
            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpGet(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}?{parameter}",
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

            return ReturnEntity<T>(microServiceName, httpResponseMessage);
        }

        public static T SendMicroServicePost<T>(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, object sendText)
        {
            HttpContent httpContent = HttpJsonHelper.ObjectToByteArrayContent(sendText);

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]
                                );

            return ReturnEntity<T>(microServiceName, httpResponseMessage);
        }
    }
}