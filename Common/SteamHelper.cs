using System.IO;
using System.Threading.Tasks;

namespace Common
{
    /// <summary>
    /// 流数据处理帮助类
    /// </summary>
    public static class SteamHelper
    {
        /// <summary>
        /// 异步读取流数据
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="contentLength">数据流长度</param>
        /// <returns></returns>
        public static async Task<byte[]> ReadSteamToBufferAsync(Stream stream, long contentLength)
        {
            byte[] bytes = new byte[contentLength];
            int readTotal = 0;

            while (readTotal < bytes.Length)
                readTotal += await stream.ReadAsync(bytes, readTotal, bytes.Length - readTotal);

            return bytes;
        }

        /// <summary>
        /// 同步读取流数据
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="contentLength">数据流长度</param>
        /// <returns></returns>
        public static byte[] ReadSteamToBuffer(Stream stream, long contentLength)
        {
            return ReadSteamToBufferAsync(stream, contentLength).Result;
        }
    }
}
