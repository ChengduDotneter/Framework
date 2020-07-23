using Common.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Common.ServiceCommon
{
    /// <summary>
    /// JObject序列化接口
    /// </summary>
    public interface IJObjectSerializeService
    {
        /// <summary>
        /// 获取JObject
        /// </summary>
        /// <returns></returns>
        JObject GetJObject();
    }

    /// <summary>
    /// JObject序列化
    /// </summary>
    public class JObjectSerializeService : IJObjectSerializeService
    {
        private IHttpContextAccessor m_httpContextAccessor;
        private IJObjectConverter m_jObjectConverter;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="jObjectConverter">IJObjectConverter</param>
        public JObjectSerializeService(IHttpContextAccessor httpContextAccessor, IJObjectConverter jObjectConverter)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_jObjectConverter = jObjectConverter;
        }

        /// <summary>
        /// 获取JObject
        /// </summary>
        /// <returns></returns>
        public JObject GetJObject()
        {
            Utf8JsonReader jsonReader = new Utf8JsonReader(SteamHelper.ReadSteamToBuffer(m_httpContextAccessor.HttpContext.Request.Body, m_httpContextAccessor.HttpContext.Request.ContentLength ?? 0));
            return m_jObjectConverter.Read(ref jsonReader, typeof(JObject), null);
        }
    }
}