using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class StringMaxLengthAttribute : ValidationBaseAttribute
    {
        private int m_maxLength;

        public StringMaxLengthAttribute(int maxLength) => m_maxLength = maxLength;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}长度必须短于{m_maxLength}。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value is string stringValue && !string.IsNullOrEmpty(stringValue))
                return stringValue.Length <= m_maxLength;

            return true;
        }
    }
}
