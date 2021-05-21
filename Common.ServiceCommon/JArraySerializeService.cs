using Common.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Common.ServiceCommon
{
    /// <summary>
    /// JArray序列化接口
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
    /// JArray序列化
    /// </summary>
    public class JArraySerializeService : IJArraySerializeService
    {
        private IHttpContextAccessor m_httpContextAccessor;//httpcontext 请求内容

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
            //从http请求里读取数据并转化为utf8编码格式的json文本
            Utf8JsonReader jsonReader = new Utf8JsonReader(SteamHelper.ReadSteamToBuffer(m_httpContextAccessor.HttpContext.Request.Body, m_httpContextAccessor.HttpContext.Request.ContentLength ?? 0));
            return m_jArrayConverter.Read(ref jsonReader, typeof(JObject), null);//转换为json数组
        }
    }
}