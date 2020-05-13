using Common.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text.Json;

namespace Common.ServiceCommon
{

    public interface IJArraySerializeService
    {
        JArray GetJArray();
    }

    public class JArraySerializeService : IJArraySerializeService
    {

        private IHttpContextAccessor m_httpContextAccessor;

        private IJArrayConverter m_jArrayConverter;

        public JArraySerializeService(IHttpContextAccessor httpContextAccessor, IJArrayConverter jArrayConverter)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_jArrayConverter = jArrayConverter;
        }

        public JArray GetJArray()
        {
            Utf8JsonReader jsonReader = new Utf8JsonReader(SteamHelper.ReadSteamToBuffer(m_httpContextAccessor.HttpContext.Request.Body, m_httpContextAccessor.HttpContext.Request.ContentLength ?? 0));
            return m_jArrayConverter.Read(ref jsonReader, typeof(JObject), null);
        }
    }
}
