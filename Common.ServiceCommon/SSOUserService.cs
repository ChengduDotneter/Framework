using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Web;

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
        public readonly static SSOUserInfo Empty = new SSOUserInfo(-9999, "UNKNOWN", "UNKNOWN");

        internal SSOUserInfo(long id, string userName, string phone)
        {
            ID = id;
            UserName = userName;
            Phone = phone;
        }

        /// <summary>
        /// 用户ID
        /// </summary>
        public long ID { get; }

        /// <summary>
        /// 用户姓名
        /// </summary>
        public string UserName { get; }

        /// <summary>
        /// 用户电话
        /// </summary>
        public string Phone { get; }

        public override bool Equals(object other)
        {
            if (other is SSOUserInfo user)
                return ID == user.ID;

            return false;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public static bool operator ==(SSOUserInfo a, SSOUserInfo b)
        {
            return a.ID == b.ID;
        }

        public static bool operator !=(SSOUserInfo a, SSOUserInfo b)
        {
            return a.ID != b.ID;
        }

        public override string ToString()
        {
            return $"{ID}, {UserName}, {Phone}";
        }
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

            if (headers == null || headers["id"].Count == 0 || headers["userName"].Count == 0)
                return SSOUserInfo.Empty;

            string phone = HttpUtility.UrlDecode(headers["phone"].FirstOrDefault() ?? "UNKNOWN");

            return new SSOUserInfo(long.Parse(HttpUtility.UrlDecode(headers["id"].ToString())),
                                   HttpUtility.UrlDecode(headers["userName"].ToString()),
                                   phone);
        }
    }
}