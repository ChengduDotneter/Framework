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
        /// <returns></returns>
        public static string GetMd5Password(string password)
        {
            if (!string.IsNullOrWhiteSpace(password))
                throw new NullReferenceException();

            return BitConverter.ToString(m_md5.ComputeHash(Encoding.Default.GetBytes(password))).Replace("-", "").ToLower();
        }

        static MD5Encryption()
        {
            m_md5 = new MD5CryptoServiceProvider();
        }
    }
}
