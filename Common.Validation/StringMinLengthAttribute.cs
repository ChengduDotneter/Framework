using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class StringMinLengthAttribute : ValidationBaseAttribute
    {
        private int m_minLength;

        public StringMinLengthAttribute(int minLength) => m_minLength = minLength;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}长度必须长于{m_minLength}。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value is string stringValue)
            {
                if (!string.IsNullOrEmpty(stringValue))
                    return stringValue.Length >= m_minLength;

                return false;
            }

            return true;
        }
    }
}
