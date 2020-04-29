using System;
using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class EnumValueExistAttribute : ValidationBaseAttribute
    {
        public EnumValueExistAttribute() { }

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}对应枚举值不存在。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            try
            {
                if (value == null)
                    return true;

                if (Enum.IsDefined(value.GetType(), (int)value))
                    return true;

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
