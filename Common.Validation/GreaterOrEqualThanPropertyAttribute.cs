using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    /// <summary>
    /// 大于等于指定属性的验证器特性类
    /// </summary>
    public class GreaterOrEqualThanPropertyAttribute : GreaterOrEqualThanAttribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="greaterOrEqualThanValuePropertyName">大于等于的指定属性名</param>
        public GreaterOrEqualThanPropertyAttribute(string greaterOrEqualThanValuePropertyName) : base(greaterOrEqualThanValuePropertyName) { }

        /// <summary>
        /// 获取大于等于的指定属性的值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="greaterOrEqualThanValuePropertyName">大于等于的指定属性名</param>
        /// <returns></returns>
        protected override object GetGreaterOrEqualThanValue(ValidationContext validationContext, object greaterOrEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)greaterOrEqualThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        /// <summary>
        /// 获取大于等于的指定属性的展示名
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="greaterOrEqualThanValuePropertyName">大于等于的指定属性名</param>
        /// <returns>如指定的属性带有DisplayAttribute特性，则返回DisplayAttribute的Name，否则DisplayAttribute特性则返回属性名</returns>
        protected override string GetGreaterOrEqualThanText(ValidationContext validationContext, object greaterOrEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)greaterOrEqualThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)greaterOrEqualThanValuePropertyName;
        }
    }
}