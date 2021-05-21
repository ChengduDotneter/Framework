using System;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net.Http;
using System.Web;
using Newtonsoft.Json.Linq;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 用户认证接口
    /// </summary>
    public interface ISSOUserService
    {
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        SSOUserInfo GetUser();
    }

    /// <summary>
    /// 用户认证
    /// </summary>
    public class SSOUserInfo
    {
        /// <summary>
        /// 默认空值
        /// </summary>
        public readonly static SSOUserInfo Empty = new SSOUserInfo(-9999, "UNKNOWN", "UNKNOWN");

        internal SSOUserInfo(long id, string userName, string phone)
        {
            ID = id;
            UserName = userName;
            Phone = phone;
        }

        /// <summary>
        /// 用户ID
        /// </summary>
        public long ID { get; }

        /// <summary>
        /// 用户姓名
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// 用户电话
        /// </summary>
        public string Phone { get; }

        public override bool Equals(object other)
        {
            if (other is SSOUserInfo user)
                return ID == user.ID;

            return false;
        }
        /// <summary>
        /// 获取hashcode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
        /// <summary>
        /// 两个用户的id对比
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(SSOUserInfo a, SSOUserInfo b)
        {
            return a?.ID == b?.ID;
        }
        /// <summary>
        /// 两个用户对比 不相等
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(SSOUserInfo a, SSOUserInfo b)
        {
            return a?.ID != b?.ID;
        }
        /// <summary>
        /// 把用户信息转为string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{ID}, {UserName}, {Phone}";
        }
    }

    /// <summary>
    /// 用户认证
    /// </summary>
    public class SSOUserService : ISSOUserService
    {
        private readonly IHttpContextAccessor m_httpContextAccessor;
        private readonly IHttpClientFactory m_httpClientFactory;
        private SSOUserInfo m_ssoUserInfo;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="httpClientFactory"></param>
        public SSOUserService(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        public SSOUserInfo GetUser()
        {
            if (m_ssoUserInfo == null)
            {
                lock (this)//锁定当前实例对象
                {
                    if (m_ssoUserInfo == null)//用户认证信息为空则创建
                    {
                        m_ssoUserInfo = CreateSSOUserInfo();
                    }
                }
            }

            return m_ssoUserInfo;
        }

        private SSOUserInfo CreateSSOUserInfo()
        {
            if (m_httpContextAccessor == null || m_httpContextAccessor.HttpContext == null)//http对象为空则默认空值
                return SSOUserInfo.Empty;
            
            if (!m_httpContextAccessor.HttpContext.WebSockets.IsWebSocketRequest)//不是WebSocket请求时
            {
                IHeaderDictionary headers = m_httpContextAccessor.HttpContext?.Request?.Headers;//获取http请求头

                if (headers == null || headers["id"].Count == 0 || headers["userName"].Count == 0)//当请求头id userName等为空时 用户认证也默认空
                    return SSOUserInfo.Empty;

                string phone = HttpUtility.UrlDecode(headers["phone"].FirstOrDefault() ?? "UNKNOWN");//请求头没有携带电话时 也为空

                return new SSOUserInfo(long.Parse(HttpUtility.UrlDecode(headers["id"].ToString())),//创建用户信息
                                       HttpUtility.UrlDecode(headers["userName"].ToString()),
                                       phone);
            }
            else//是WebSocket请求时
            {
                string identity = m_httpContextAccessor.HttpContext?.Request.Query["identity"];//查看是否携带身份

                if (string.IsNullOrWhiteSpace(identity))//身份为空则创建空用户认证
                    return SSOUserInfo.Empty;

                HttpClient httpClient = m_httpClientFactory.CreateClient("userinfo");//创建httpClient实例
                httpClient.BaseAddress = new Uri($"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}");//发送请求的地址
                HttpResponseMessage httpResponseMessage = httpClient.AddAuthorizationHeader($"Bearer {identity}").GetAsync("connect/userinfo").Result;//添加权限认证

                if (!httpResponseMessage.IsSuccessStatusCode)//是否成功返回
                    return SSOUserInfo.Empty;

                JObject jObject = JObject.Parse(httpResponseMessage.Content.ReadAsStringAsync().Result);//获取返回的数据 转为JObject对象

                return new SSOUserInfo(long.Parse(HttpUtility.UrlDecode(jObject["sub"].ToString())),//创建用户认证
                                       jObject["userName"].ToString(),
                                       jObject["phone"].ToString());
            }
        }
    }
}