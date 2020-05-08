using Common.Model;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Text.Json;

namespace Common.ServiceCommon
{

    public interface IJObjectSerializeService
    {
        JObject GetJObject();
    }

    public class JObjectSerializeService : IJObjectSerializeService
    {

        private IHttpContextAccessor m_httpContextAccessor;

        private IJObjectConverter m_jObjectConverter;

        public JObjectSerializeService(IHttpContextAccessor httpContextAccessor, IJObjectConverter jObjectConverter)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_jObjectConverter = jObjectConverter;
        }

        public JObject GetJObject()
        {
            Utf8JsonReader jsonReader = new Utf8JsonReader(StreamToBytes(m_httpContextAccessor.HttpContext.Request.Body));
            return m_jObjectConverter.Read(ref jsonReader, typeof(JObject), null);
        }

        private byte[] StreamToBytes(Stream stream)
        {
            byte[] bytes = new byte[Convert.ToInt32(m_httpContextAccessor.HttpContext.Request.ContentLength)];

            stream.ReadAsync(bytes, 0, bytes.Length).Wait();

            return bytes;
        }

    }
}
