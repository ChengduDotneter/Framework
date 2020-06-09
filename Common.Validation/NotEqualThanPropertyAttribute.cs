using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    /// <summary>
    /// 不等于指定属性的值验证器特性类
    /// </summary>
    public class NotEqualThanPropertyAttribute : NotEqualThanAttribute
    {
        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="notEqualThanValue">不等于属性名</param>
        public NotEqualThanPropertyAttribute(string notEqualThanValue) : base(notEqualThanValue) { }

        /// <summary>
        /// 获取验证失败的错误信息
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="notEqualThanValuePropertyName">不等于的属性的属性名</param>
        /// <returns></returns>
        protected override object GetNotEqualThanValue(ValidationContext validationContext, object notEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)notEqualThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        /// <summary>
        /// 验证属性值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="notEqualThanValuePropertyName">不等于的属性的属性名</param>
        /// <returns></returns>
        protected override string GetNotEqualThanText(ValidationContext validationContext, object notEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)notEqualThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)notEqualThanValuePropertyName;
        }
    }
}
