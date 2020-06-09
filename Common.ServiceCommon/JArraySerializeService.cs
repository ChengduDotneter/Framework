using Common.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.Json;

namespace Common.ServiceCommon
{
    /// <summary>
    /// JArrayS序列化接口
    /// </summary>
    public interface IJArraySerializeService
    {
        /// <summary>
        /// 获取JArray
        /// </summary>
        /// <returns></returns>
        JArray GetJArray();
    }

    /// <summary>
    /// JArrayS序列化
    /// </summary>
    public class JArraySerializeService : IJArraySerializeService
    {

        private IHttpContextAccessor m_httpContextAccessor;

        private IJArrayConverter m_jArrayConverter;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        /// <param name="jArrayConverter">IJArrayConverter</param>
        public JArraySerializeService(IHttpContextAccessor httpContextAccessor, IJArrayConverter jArrayConverter)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_jArrayConverter = jArrayConverter;
        }

        /// <summary>
        /// 获取JArray
        /// </summary>
        /// <returns></returns>
        public JArray GetJArray()
        {
            Utf8JsonReader jsonReader = new Utf8JsonReader(SteamHelper.ReadSteamToBuffer(m_httpContextAccessor.HttpContext.Request.Body, m_httpContextAccessor.HttpContext.Request.ContentLength ?? 0));
            return m_jArrayConverter.Read(ref jsonReader, typeof(JObject), null);
        }
    }
}
