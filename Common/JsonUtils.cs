namespace Common
{
    public static class JsonUtils
    {
        public static string PropertyNameToJavaScriptStyle(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return string.Empty;

            return $"{propertyName.Substring(0, 1).ToLower()}{propertyName.Substring(1)}";
        }

        public static string PropertyNameToCSharpStyle(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return string.Empty;

            return $"{propertyName.Substring(0, 1).ToUpper()}{propertyName.Substring(1)}";
        }
    }
}
