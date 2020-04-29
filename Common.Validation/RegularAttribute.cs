using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Common.Validation
{
    public class RegexAttribute : ValidationBaseAttribute
    {
        private string m_regexValue;

        public RegexAttribute(string regexValue) => m_regexValue = regexValue;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}必须满足正则表达式（{ m_regexValue}）。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return false;

            return Regex.IsMatch(value.ToString(), m_regexValue);
        }

    }
}
