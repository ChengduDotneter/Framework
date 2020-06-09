using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;

namespace Common
{
    /// <summary>
    /// 请求错误类型枚举
    /// </summary>
    public enum NetErrorTypeEnum
    {
        /// <summary>
        /// 获取数据失败
        /// </summary>
        [Display(Name = "获取数据失败")]
        DataError,
        /// <summary>
        /// 请检查网络连接
        /// </summary>
        [Display(Name = "请检查网络连接")]
        NetworkError,
        /// <summary>
        /// 接口错误
        /// </summary>
        [Display(Name = "接口错误")]
        ErrorInterface,
        /// <summary>
        /// 操作成功
        /// </summary>
        [Display(Name = "操作成功")]
        Success = 200,
    }

    /// <summary>
    /// HTTP请求结果
    /// </summary>
    public class HttpResult
    {
        /// <summary>
        /// 请求结果字符串
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// 请求状态
        /// </summary>
        public NetErrorTypeEnum Status { get; set; }

        /// <summary>
        /// 异常
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="status"></param>
        /// <param name="data"></param>
        /// <param name="exception"></param>
        public HttpResult(NetErrorTypeEnum status, string data = null, Exception exception = null)
        {
            Status = status;
            Data = data;
            Exception = exception;
        }
    }

    /// <summary>
    /// 表单请求帮助类
    /// </summary>
    public class HttpFormPostHelper
    {
        /// <summary>
        /// form表单post请求
        /// </summary>
        /// <param name="url">请求url</param>
        /// <param name="keyValues">请求参数</param>
        /// <param name="timeOut">超时时间（单位秒）</param>
        /// <param name="tokenName">tokenName</param>
        /// <param name="tokenValue">token</param>
        /// <returns></returns>
        public static HttpResult Post(string url, Dictionary<string, object> keyValues,int timeOut = 10, string tokenName = null,string tokenValue = null)
        {
            if (url == null || string.IsNullOrWhiteSpace(url))
                new HttpResult(NetErrorTypeEnum.ErrorInterface);

            //添加Post 参数
            StringBuilder builder = new StringBuilder();

            if (keyValues != null)
            {
                int i = 0;
                foreach (var item in keyValues)
                {
                    if (i > 0)
                        builder.Append("&");
                    builder.AppendFormat("{0}={1}", item.Key, item.Value == null ? null : item.Value.ToString());
                    i++;
                }
            }

            byte[] postData = Encoding.UTF8.GetBytes(builder.ToString());

            HttpWebRequest _webRequest = (HttpWebRequest)WebRequest.Create(url);
            _webRequest.Method = "POST";
            //内容类型  
            _webRequest.ContentType = "application/x-www-form-urlencoded";
            _webRequest.Timeout = timeOut * 1000;
            _webRequest.ContentLength = postData.Length;

            if (!string.IsNullOrWhiteSpace(tokenName) && !string.IsNullOrWhiteSpace(tokenValue))
                _webRequest.Headers.Add(tokenName, tokenValue);

            try
            {
                using (System.IO.Stream reqStream = _webRequest.GetRequestStream())
                {
                    reqStream.Write(postData, 0, postData.Length);
                    reqStream.Close();
                }

                HttpWebResponse resp = (HttpWebResponse)_webRequest.GetResponse();
                System.IO.Stream stream = resp.GetResponseStream();
                string returnJobject = null;

                //获取响应内容
                using (System.IO.StreamReader reader = new System.IO.StreamReader(stream, Encoding.UTF8))
                    returnJobject = reader.ReadToEnd();

                return new HttpResult(NetErrorTypeEnum.Success, returnJobject);
            }
            catch (Exception ex)
            {
                return new HttpResult(NetErrorTypeEnum.DataError, exception: ex);
            }
        }
    }
}
