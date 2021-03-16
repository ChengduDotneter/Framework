using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    /// <summary>
    /// 小于等于指定属性验证器特性类
    /// </summary>
    public class LessOrEqualThanPropertyAttribute : LessOrEqualThanAttribute
    {
        /// <summary>
        /// 验证器构造函数
        /// </summary>
        /// <param name="lessOrEqualThanValuePropertyName">小于等于的指定属性名</param>
        /// <param name="ignorePredeciteFunction"></param> 
        public LessOrEqualThanPropertyAttribute(string lessOrEqualThanValuePropertyName, string ignorePredeciteFunction = null) : base(lessOrEqualThanValuePropertyName, ignorePredeciteFunction) { }

        /// <summary>
        /// 获取小于等于的值
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="lessOrEqualThanValuePropertyName">小于等于的指定属性名</param>
        /// <returns></returns>
        protected override object GetLessOrEqualThanValue(ValidationContext validationContext, object lessOrEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)lessOrEqualThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        /// <summary>
        /// 获取小于等于的指定属性的展示名
        /// </summary>
        /// <param name="validationContext">验证器上下文</param>
        /// <param name="lessOrEqualThanValuePropertyName">小于等于的指定属性名</param>
        /// <returns>如指定的属性带有DisplayAttribute特性，则返回DisplayAttribute的Name，否则DisplayAttribute特性则返回属性名</returns>
        protected override string GetLessOrEqualThanText(ValidationContext validationContext, object lessOrEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)lessOrEqualThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)lessOrEqualThanValuePropertyName;
        }
    }
}