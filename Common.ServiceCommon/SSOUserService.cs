using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System;
using System.Web;

namespace Common.ServiceCommon
{
    public interface ISSOUserService
    {
        SSOUserInfo GetUser();
    }

    public class SSOUserInfo
    {
        public readonly static SSOUserInfo Empty = new SSOUserInfo(-9999, "UNKNOWN");

        internal SSOUserInfo(long id, string userName)
        {
            ID = id;
            UserName = userName;
        }

        public long ID { get; }

        public string UserName { get; }
    }

    public class SSOUserService : ISSOUserService
    {
        private IHttpContextAccessor m_httpContextAccessor;
        private IWebHostEnvironment m_webHostEnvironment;

        public SSOUserService(IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment)
        {
            m_httpContextAccessor = httpContextAccessor;
            m_webHostEnvironment = webHostEnvironment;
        }

        public SSOUserInfo GetUser()
        {
#if DEBUG
            return new SSOUserInfo(0, "admin");
#endif

            IHeaderDictionary headers = m_httpContextAccessor.HttpContext.Request.Headers;

            if (headers["id"].Count == 0 || headers["userName"].Count == 0)
            {
                if (m_webHostEnvironment.IsDevelopment())
                    return SSOUserInfo.Empty;
                else
                    throw new NullReferenceException();
            }

            return new SSOUserInfo(long.Parse(HttpUtility.UrlDecode(headers["id"].ToString())),
                                   HttpUtility.UrlDecode(headers["userName"].ToString()));
        }
    }
}
