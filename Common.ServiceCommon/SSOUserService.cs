using System.Web;
using Microsoft.AspNetCore.Http;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 用户认证接口
    /// </summary>
    public interface ISSOUserService
    {
        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        SSOUserInfo GetUser();
    }

    /// <summary>
    /// 用户认证
    /// </summary>
    public class SSOUserInfo
    {
        /// <summary>
        /// 默认空值
        /// </summary>
        public readonly static SSOUserInfo Empty = new SSOUserInfo(-9999, "UNKNOWN");

        internal SSOUserInfo(long id, string userName)
        {
            ID = id;
            UserName = userName;
        }

        /// <summary>
        /// 用户ID
        /// </summary>
        public long ID { get; }

        /// <summary>
        /// 用户姓名
        /// </summary>
        public string UserName { get; }
    }

    /// <summary>
    /// 用户认证
    /// </summary>
    public class SSOUserService : ISSOUserService
    {
        private IHttpContextAccessor m_httpContextAccessor;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="httpContextAccessor">IHttpContextAccessor</param>
        public SSOUserService(IHttpContextAccessor httpContextAccessor)
        {
            m_httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 获取用户信息
        /// </summary>
        /// <returns></returns>
        public SSOUserInfo GetUser()
        {
            IHeaderDictionary headers = m_httpContextAccessor.HttpContext?.Request?.Headers;

            //TODO: 可能存在下游服务失去User相关信息或验证
            if (headers["id"].Count == 0 || headers["userName"].Count == 0)
                return SSOUserInfo.Empty;

            return new SSOUserInfo(long.Parse(HttpUtility.UrlDecode(headers["id"].ToString())),
                                   HttpUtility.UrlDecode(headers["userName"].ToString()));
        }
    }
}