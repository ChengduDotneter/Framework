using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    /// <summary>
    /// 不等于验证器特性类
    /// </summary>
    public class NotEqualThanPropertyAttribute : NotEqualThanAttribute
    {
        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="notEqualThanValue">不等于的值</param>
        public NotEqualThanPropertyAttribute(string notEqualThanValue) : base(notEqualThanValue) { }

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext"></param>
        /// <param name="notEqualThanValuePropertyName"></param>
        /// <returns></returns>
        protected override object GetNotEqualThanValue(ValidationContext validationContext, object notEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)notEqualThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="validationContext"></param>
        /// <param name="notEqualThanValuePropertyName"></param>
        /// <returns></returns>
        protected override string GetNotEqualThanText(ValidationContext validationContext, object notEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)notEqualThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)notEqualThanValuePropertyName;
        }
    }
}
