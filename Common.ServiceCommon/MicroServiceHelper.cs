using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Common.ServiceCommon
{
   public class MicroServiceHelper
    {
        private static T ReturnEntity<T>(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                return HttpJsonHelper.GetResponse<T>(httpResponseMessage);

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

            throw new DealException($"{microServiceName}接口调用失败");
        }

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

        public static JObject MicroServiceGetByID(IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, string parameter)
        {
            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpGet(
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}/{parameter}",
                                httpContextAccessor?.HttpContext?.Request.Headers["Authorization"]);

            return ReturnEntity<JObject>(microServiceName, httpResponseMessage);
        }

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
