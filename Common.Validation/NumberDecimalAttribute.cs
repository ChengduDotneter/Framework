using System.ComponentModel.DataAnnotations;

namespace Common.Validation
{
    public class NumberDecimalAttribute : ValidationBaseAttribute
    {
        private int m_decimal;

        public NumberDecimalAttribute(int @decimal) => m_decimal = @decimal;

        protected override string GetErrorMessage(ValidationContext validationContext, string propertyName) => $"{propertyName}超出数值精度，数值精度为{m_decimal}位小数。";

        protected override bool ValidateValue(object value, ValidationContext validationContext)
        {
            if (value == null)
                return true;

            string stringValue = value.ToString();
            var decimalsLength = stringValue.IndexOf(".") > -1 ? stringValue.Length - stringValue.IndexOf(".") - 1 : 0;
            return decimalsLength <= m_decimal;
        }
    }
}
