using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Common
{
    /// <summary>
    /// 枚举描述
    /// </summary>
    public class DisplayHelper
    {
        /// <summary>
        /// 获取枚举描述
        /// </summary>
        /// <param name="value">枚举值</param>
        public static string GetDisplayName(object value)
        {
            object[] attrs = value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DisplayAttribute), false);
            if (attrs.Count() != 1)
                throw new DealException("获取描述失败");

            return ((DisplayAttribute)attrs[0]).Name;
        }
    }
}