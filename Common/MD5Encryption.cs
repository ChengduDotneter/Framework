using System;
using System.Security.Cryptography;
using System.Text;

namespace Common
{

    public static class MD5Encryption
    {
        private static MD5 m_md5;

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
