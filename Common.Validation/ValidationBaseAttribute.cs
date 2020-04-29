using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class ValidationBaseAttribute : ValidationAttribute
    {
        protected abstract string GetErrorMessage(ValidationContext validationContext, string propertyName);

        protected abstract bool ValidateValue(object value, ValidationContext validationContext);

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (!ValidateValue(value, validationContext))
            {
                return new ValidationResult(GetErrorMessage(validationContext, validationContext.DisplayName));
            }

            return ValidationResult.Success;
        }
    }
}
