using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    /// <summary>
    /// 大于指定属性的验证器特性类
    /// </summary>
    public class GreaterThanPropertyAttribute : GreaterThanAttribute
    {
        /// <summary>
        /// 验证器特性构造函数
        /// </summary>
        /// <param name="greaterThanValuePropertyName">大于的指定属性名</param>
        public GreaterThanPropertyAttribute(string greaterThanValuePropertyName) : base(greaterThanValuePropertyName) { }

        /// <summary>
        /// 获取大于的指定属性值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="greaterThanValuePropertyName">大于的指定属性名</param>
        /// <returns></returns>
        protected override object GetGreaterThanValue(ValidationContext validationContext, object greaterThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)greaterThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        /// <summary>
        /// 获取大于的指定属性的展示名
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="greaterThanValuePropertyName">大于的指定属性名</param>
        /// <returns>如指定的属性带有DisplayAttribute特性，则返回DisplayAttribute的Name，否则DisplayAttribute特性则返回属性名</returns>
        protected override string GetGreaterThanText(ValidationContext validationContext, object greaterThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)greaterThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)greaterThanValuePropertyName;
        }
    }
}