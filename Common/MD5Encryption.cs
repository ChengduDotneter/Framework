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
        /// <param name="encryptedString"></param>
        /// <param name="charset"></param>
        /// <returns></returns>
        public static string GetMD5(string encryptedString, string charset)
        {
            byte[] fromData = Encoding.GetEncoding(charset).GetBytes(encryptedString);
            byte[] targetData = m_md5.ComputeHash(fromData);
            string byte2String = null;

            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String += targetData[i].ToString("x2");
            }

            return byte2String;
        }

        static MD5Encryption()
        {
            m_md5 = new MD5CryptoServiceProvider();
        }
    }
}