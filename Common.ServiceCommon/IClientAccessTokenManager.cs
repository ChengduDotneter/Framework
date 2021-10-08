using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Common.ServiceCommon
{
    public interface IClientAccessTokenManager
    {
        Tuple<bool, string> GetToken();
    }

    internal class ClientAccessTokenManager : IClientAccessTokenManager
    {
        private static string m_accessToken;
        private static bool m_getTokenSuccess;
        private static object m_lockThis;

        public ClientAccessTokenManager()
        {
            m_getTokenSuccess = true;
            m_lockThis = new object();
        }

        /// <summary>
        /// 获取token
        /// </summary>
        /// <returns></returns>
        public Tuple<bool, string> GetToken()
        {
            if (string.IsNullOrEmpty(m_accessToken) && m_getTokenSuccess)
            {
                if (string.IsNullOrEmpty(m_accessToken) && m_getTokenSuccess)
                {
                    lock (m_lockThis)
                    {
                        if (string.IsNullOrEmpty(m_accessToken) && m_getTokenSuccess)
                            InitToken();
                    }
                }
            }

            return Tuple.Create(m_getTokenSuccess, m_accessToken);
        }

        private static void InitToken() //token初始化
        {
            string tokenRequestUrl = $"{ConfigManager.Configuration["CommunicationScheme"]}{ConfigManager.Configuration["GatewayIP"]}/connect/token"; //token请求地址
            string clientID = ConfigManager.Configuration["ClientID"]; //客户端id
            string clientSecret = ConfigManager.Configuration["ClientSecret"];

            if (string.IsNullOrEmpty(tokenRequestUrl) ||
                string.IsNullOrEmpty(clientID) ||
                string.IsNullOrEmpty(clientSecret))
            {
                m_getTokenSuccess = false;
                return;
            }

            HttpWebResponseResult httpWebResponseResult = HttpWebRequestHelper.FormPost(tokenRequestUrl, new Dictionary<string, object>
            {
                ["client_id"] = clientID,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "client_credentials"
            }); //请求token

            if (httpWebResponseResult.HttpStatus != HttpStatusCode.OK)
            {
                m_getTokenSuccess = false;
                return;
            }

            JObject @object = JObject.Parse(httpWebResponseResult.DataString); //获取token
            m_accessToken = $"{@object["token_type"]} {@object["access_token"]}";
        }
    }
}