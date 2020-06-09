using System;
using System.Security.Cryptography;
using System.Text;

namespace Common
{
    /// <summary>
    /// MD5编码扩展类
    /// </summary>
    public static class MD5Encryption
    {
        private static MD5 m_md5;

        /// <summary>
        /// 获取MD5加密字符串
        /// </summary>
        /// <param name="password"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string GetMd5Password(string password, string charset)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new NullReferenceException();

            return Convert.ToBase64String(m_md5.ComputeHash(Encoding.GetEncoding(charset).GetBytes(password)));
        }

        static MD5Encryption()
        {
            m_md5 = MD5.Create();
        }
    }
}
