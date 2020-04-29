using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    public class NotEqualThanPropertyAttribute : NotEqualThanAttribute
    {
        public NotEqualThanPropertyAttribute(string notEqualThanValue) : base(notEqualThanValue) { }

        protected override object GetNotEqualThanValue(ValidationContext validationContext, object notEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)notEqualThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        protected override string GetNotEqualThanText(ValidationContext validationContext, object notEqualThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)notEqualThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)notEqualThanValuePropertyName;
        }
    }
}
