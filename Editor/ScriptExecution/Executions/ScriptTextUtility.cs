namespace AIBridge.Editor.ScriptExecution.Commands
{
    internal static class ScriptTextUtility
    {
        public static string StripOptionalQuotes(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var trimmed = value.Trim();
            if (trimmed.Length >= 2
                && ((trimmed[0] == '"' && trimmed[trimmed.Length - 1] == '"')
                    || (trimmed[0] == '\'' && trimmed[trimmed.Length - 1] == '\'')))
            {
                return trimmed.Substring(1, trimmed.Length - 2);
            }

            return trimmed;
        }
    }
}
