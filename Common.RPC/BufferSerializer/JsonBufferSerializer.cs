using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.RPC.BufferSerializer
{
    internal class JsonBufferSerializer : IBufferSerializer
    {
        private readonly static IDictionary<int, Type> m_typeDic;
        private readonly Encoding m_encoding;

        private class JsonData
        {
            public int MessageID { get; }

            public string Content { get; }

            public JsonData(int messageID, string content)
            {
                MessageID = messageID;
                Content = content;
            }
        }

        static JsonBufferSerializer()
        {
            m_typeDic = new Dictionary<int, Type>();

            Type[] dataTypes = TypeReflector.ReflectType(type =>
            {
                if (type.GetInterface(nameof(IRPCData)) == null || type.IsInterface)
                    return false;

                return true;
            });

            foreach (Type type in dataTypes)
            {
                if (type.IsClass)
                    throw new Exception("序列化对象必须为结构体。");

                IRPCData template = (IRPCData)Activator.CreateInstance(type);
                byte messageID = template.MessageID;

                if (m_typeDic.ContainsKey(messageID))
                    throw new Exception(string.Format("序列化对象ID重复，重复ID：{0}，对象类型：{1}和{2}。", messageID, m_typeDic[messageID].FullName, type.FullName));

                m_typeDic.Add(messageID, type);
            }
        }

        public JsonBufferSerializer(Encoding encoding)
        {
            m_encoding = encoding;
        }
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public IRPCData Deserialize(byte[] buffer)
        {
            JObject jObject = JObject.Parse(m_encoding.GetString(buffer));
            JsonData jsonData = jObject.ToObject<JsonData>();
            jObject = JObject.Parse(jsonData.Content);

            return (IRPCData)jObject.ToObject(m_typeDic[jsonData.MessageID]);
        }
        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="data"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public unsafe int Serialize(IRPCData data, byte[] buffer)
        {
            JObject jObject = JObject.FromObject(data);
            jObject.Remove("MessageID");
            jObject = JObject.FromObject(new JsonData(data.MessageID, jObject.ToString()));

            byte[] encodingBuffer = m_encoding.GetBytes(jObject.ToString());

            fixed (byte* bufferPtr = buffer)
            fixed (byte* encodingBufferPtr = encodingBuffer)
                Buffer.MemoryCopy(encodingBufferPtr, bufferPtr, buffer.Length, encodingBuffer.Length);

            return encodingBuffer.Length;
        }
    }
}