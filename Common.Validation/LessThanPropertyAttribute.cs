using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    /// <summary>
    /// 小于指定属性值验证器特性类
    /// </summary>
    public class LessThanPropertyAttribute : LessThanAttribute
    {
        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="lessThanValuePropertyName">小于的属性名</param>
        public LessThanPropertyAttribute(string lessThanValuePropertyName) : base(lessThanValuePropertyName) { }

        /// <summary>
        /// 获取小于的属性值
        /// </summary>
        /// <param name="validationContext"></param>
        /// <param name="lessThanValuePropertyName"></param>
        /// <returns></returns>
        protected override object GetLessThanValue(ValidationContext validationContext, object lessThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)lessThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        /// <summary>
        /// 获取小于的属性名
        /// </summary>
        /// <param name="validationContext"></param>
        /// <param name="lessThanValuePropertyName"></param>
        /// <returns></returns>
        protected override string GetLessThanText(ValidationContext validationContext, object lessThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)lessThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)lessThanValuePropertyName;
        }
    }
}
