using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    /// <summary>
    /// 参数之间比较：GreaterThanProperty(nameof(parameter))
    /// </summary>
    public class GreaterThanPropertyAttribute : GreaterThanAttribute
    {
        public GreaterThanPropertyAttribute(string greaterThanValuePropertyName) : base(greaterThanValuePropertyName) { }

        protected override object GetMoreThanValue(ValidationContext validationContext, object moreThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)moreThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        protected override string GetGreaterThanText(ValidationContext validationContext, object greaterThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)greaterThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)greaterThanValuePropertyName;
        }
    }
}
