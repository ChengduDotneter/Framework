using Common.Const;
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
        /// <summary>
        /// 返回请求数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="microServiceName"></param>
        /// <param name="httpResponseMessage"></param>
        /// <returns></returns>
        private static T ReturnEntity<T>(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)//http请求状态很ok
                return HttpJsonHelper.GetResponse<T>(httpResponseMessage);//返回http请求携带的结果

            CheckReturn(microServiceName, httpResponseMessage);//检查接口与调用不成功原因

            throw new DealException($"{microServiceName}接口调用失败");
        }
        /// <summary>
        /// 返回成功与否
        /// </summary>
        /// <param name="microServiceName"></param>
        /// <param name="httpResponseMessage"></param>
        /// <returns></returns>
        private static bool ReturnEntity(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)//返回true
                return true;

            CheckReturn(microServiceName, httpResponseMessage);

            throw new DealException($"{microServiceName}接口调用失败");
        }
        /// <summary>
        /// 检查接口未成功原因
        /// </summary>
        /// <param name="microServiceName"></param>
        /// <param name="httpResponseMessage"></param>
        private static void CheckReturn(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.PaymentRequired)
                throw new DealException($"{microServiceName}接口调用失败,原因{httpResponseMessage.Content?.ReadAsStringAsync().Result}");
            else if (httpResponseMessage.StatusCode == HttpStatusCode.Unauthorized)
                throw new DealException($"{microServiceName}接口调用失败,原因未认证系统");
            else if (httpResponseMessage.StatusCode == HttpStatusCode.ServiceUnavailable)
                throw new DealException($"{microServiceName}服务调用超时");
            else if (httpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                throw new DealException($"未发现{microServiceName}服务");
            else if (httpResponseMessage.StatusCode == HttpStatusCode.BadRequest)
                throw new DealException($"{microServiceName}参数验证不通过");
            else if (httpResponseMessage.StatusCode == HttpStatusCode.InternalServerError)
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
            HttpContent httpContent = HttpJsonHelper.ObjectToByteArrayContent(sendText);//把发送的数据体包装秤http

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(//http post同步请求接口
                                httpClientFactory,//工厂
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",//服务地址
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}",//服务名称和接口名称
                                httpContent,//发送的数据体
                                httpContextAccessor?.HttpContext?.Request.Headers[HttpHeaderConst.AUTHORIZATION]//添加验证头
                                );

            return ReturnEntity<T>(microServiceName, httpResponseMessage);//返回请求到的数据
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
            HttpContent httpContent = HttpJsonHelper.ObjectToByteArrayContent(sendText);//请求携带的数据

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers[HttpHeaderConst.AUTHORIZATION]
                                );

            return ReturnEntity(microServiceName, httpResponseMessage);//返回请求成功与否
        }

        /// <summary>
        /// 微服务Put 返回bool值
        /// </summary>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="sendText">参数</param>
        /// <returns></returns>
        public static bool SendMicroServicePut(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, object sendText)
        {
            HttpContent httpContent = HttpJsonHelper.ObjectToByteArrayContent(sendText);

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPut(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers[HttpHeaderConst.AUTHORIZATION]
                                );

            return ReturnEntity(microServiceName, httpResponseMessage);
        }

        /// <summary>
        /// 微服务Put 返回数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpClientFactory">HttpClient构造工厂</param>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="microServiceName">微服务名称</param>
        /// <param name="functionName">接口名</param>
        /// <param name="sendText">参数</param>
        /// <returns></returns>
        public static T SendMicroServicePut<T>(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, string functionName, object sendText)
        {
            HttpContent httpContent = HttpJsonHelper.ObjectToByteArrayContent(sendText);

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPut(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers[HttpHeaderConst.AUTHORIZATION]
                                );

            return ReturnEntity<T>(microServiceName, httpResponseMessage);
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
                                $"{ConfigManager.Configuration[microServiceName]}/{functionName}/{parameter}",   //因为是get 所以id拼接在url后面就行了
                                httpContextAccessor?.HttpContext?.Request.Headers[HttpHeaderConst.AUTHORIZATION]);

            return ReturnEntity<JObject>(microServiceName, httpResponseMessage);//获取接口返回的数据
        }

        /// <summary>
        /// 微服务Get，通过条件 这应该是就是search了
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
                                httpContextAccessor?.HttpContext?.Request.Headers[HttpHeaderConst.AUTHORIZATION]);

            return ReturnEntity<JObject>(microServiceName, httpResponseMessage);
        }
    }

    /// <summary>
    /// 对接php接口调用方式
    /// </summary>
    public class PHPMicroServiceHelper
    {
        /// <summary>
        /// 结果验证
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="microServiceName"></param>
        /// <param name="httpResponseMessage"></param>
        /// <returns></returns>
        private static T ReturnEntity<T>(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                return CheckReturn<T>(microServiceName, httpResponseMessage);

            throw new DealException($"{microServiceName}接口调用失败");
        }

        /// <summary>
        /// 检测返回结果是否有效
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="microServiceName"></param>
        /// <param name="httpResponseMessage"></param>
        /// <returns></returns>
        private static T CheckReturn<T>(string microServiceName, HttpResponseMessage httpResponseMessage)
        {
            JObject jObject = JsonConvert.DeserializeObject<JObject>(httpResponseMessage.Content.ReadAsStringAsync().Result);

            if (JTokenHelper.GetIntValue(jObject["code"]) != 200)
                throw new DealException($"{microServiceName}接口调用失败,原因:{JTokenHelper.GetStringValue(jObject["msg"])}");
            else if (!jObject["data"].HasValues)
                throw new DealException($"{microServiceName}接口调用失败,无返回数据");
            else return JsonConvert.DeserializeObject<T>(jObject["data"].ToString());
        }

        /// <summary>
        /// get方式调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpClientFactory">http工厂</param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="microServiceName">服务名称</param>
        /// <param name="parameter">参数</param>
        /// <returns></returns>
        public static T MicroServiceGetByCondition<T>(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, string parameter)
        {
            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpGet(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}?{parameter}",
                                httpContextAccessor?.HttpContext?.Request.Headers[HttpHeaderConst.AUTHORIZATION]);

            return ReturnEntity<T>(microServiceName, httpResponseMessage);
        }

        /// <summary>
        /// post方式调用
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpClientFactory"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="microServiceName"></param>
        /// <param name="sendText"></param>
        /// <returns></returns>
        public static T SendMicroServicePost<T>(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, string microServiceName, object sendText)
        {
            HttpContent httpContent = HttpJsonHelper.ObjectToByteArrayContent(sendText);

            HttpResponseMessage httpResponseMessage = HttpJsonHelper.HttpPost(
                                httpClientFactory,
                                $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}",
                                $"{ConfigManager.Configuration[microServiceName]}",
                                httpContent,
                                httpContextAccessor?.HttpContext?.Request.Headers[HttpHeaderConst.AUTHORIZATION]
                                );

            return ReturnEntity<T>(microServiceName, httpResponseMessage);
        }
    }
}