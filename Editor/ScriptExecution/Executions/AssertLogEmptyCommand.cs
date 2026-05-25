using AIBridge.Editor;

namespace AIBridge.Editor.ScriptExecution.Commands
{
    public class AssertLogEmptyCommand : IScriptCommand
    {
        private readonly string _logType;
        private readonly string _regexPattern;
        private readonly int _count;

        public AssertLogEmptyCommand(string logType, string regexPattern, int count)
        {
            _logType = string.IsNullOrEmpty(logType) ? "Error" : AIBridgeProjectSettings.NormalizeLogRetrievalType(logType);
            _regexPattern = regexPattern;
            _count = count > 0 ? count : 500;
        }

        public string Type => "assert_log_empty";

        public ScriptCommandResult Execute(ScriptExecutionContext context)
        {
            try
            {
                var logs = GetLogsCommand.GetConsoleLogsByMinimumLevel(_count, _logType, _regexPattern);
                if (logs.Count > 0)
                {
                    var first = logs[0];
                    return ScriptCommandResult.Fail($"发现 {_logType} 日志 {logs.Count} 条，首条: [{first.type}] {first.message}");
                }

                context.Log($"[AssertLogEmpty] 未发现 {_logType} 日志");
                return ScriptCommandResult.Ok("No matching logs");
            }
            catch (System.ArgumentException ex)
            {
                return ScriptCommandResult.Fail("日志断言参数无效: " + ex.Message, ex);
            }
        }
    }
}
