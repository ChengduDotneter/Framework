using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class NotNullAttribute : ValidationBaseAttribute
    {
        public NotNullAttribute() { }

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}不能为空。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value != null && value is string stringValue)
                return !string.IsNullOrWhiteSpace(stringValue);

            return value != null;
        }
    }
}
