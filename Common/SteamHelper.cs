using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class SteamHelper
    {
        public static async Task<byte[]> ReadSteamToBufferAsync(Stream stream, long contentLength)
        {
            byte[] bytes = new byte[contentLength];
            int readTotal = 0;

            while (readTotal < bytes.Length)
                readTotal += await stream.ReadAsync(bytes, readTotal, bytes.Length - readTotal);

            return bytes;
        }

        public static byte[] ReadSteamToBuffer(Stream stream, long contentLength)
        {
            return ReadSteamToBufferAsync(stream, contentLength).Result;
        }
    }
}
