using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Common.Validation
{
    public class LessThanPropertyAttribute : LessThanAttribute
    {
        public LessThanPropertyAttribute(string lessThanValuePropertyName) : base(lessThanValuePropertyName) { }

        protected override object GetLessThanValue(ValidationContext validationContext, object lessThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)lessThanValuePropertyName).GetValue(validationContext.ObjectInstance);
        }

        protected override string GetLessThanText(ValidationContext validationContext, object lessThanValuePropertyName)
        {
            return validationContext.ObjectType.GetProperty((string)lessThanValuePropertyName).GetCustomAttribute<DisplayAttribute>()?.Name ?? (string)lessThanValuePropertyName;
        }
    }
}
