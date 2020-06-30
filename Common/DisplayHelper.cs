using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Common
{
    public class DisplayHelper
    {
        public static string GetDisplayName(object value)
        {
            object[] attrs = value.GetType().GetField(value.ToString()).GetCustomAttributes(typeof(DisplayAttribute), false);
            if (attrs.Count() != 1)
                throw new DealException("获取描述失败");

            return ((DisplayAttribute)attrs[0]).Name;
        }
    }
}