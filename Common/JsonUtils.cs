namespace Common
{
    /// <summary>
    /// Json格式转换类
    /// </summary>
    public static class JsonUtils
    {
        /// <summary>
        /// 属性名转换为JavaScript风格
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>属性名首字母小写</returns>
        public static string PropertyNameToJavaScriptStyle(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return string.Empty;

            return $"{propertyName.Substring(0, 1).ToLower()}{propertyName.Substring(1)}";
        }

        /// <summary>
        /// 属性名转换为CSharp风格
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns>属性名大写</returns>
        public static string PropertyNameToCSharpStyle(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return string.Empty;

            return $"{propertyName.Substring(0, 1).ToUpper()}{propertyName.Substring(1)}";
        }
    }
}