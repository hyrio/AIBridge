namespace AIBridge.Editor.ScriptExecution.Commands
{
    public class PrintVarCommand : IScriptCommand
    {
        private readonly string _name;

        public PrintVarCommand(string name)
        {
            _name = name;
        }

        public string Type => "print_var";

        public ScriptCommandResult Execute(ScriptExecutionContext context)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return ScriptCommandResult.Fail("print_var 需要变量名");
            }

            var value = context.GetVariable(_name);
            if (value == null)
            {
                return ScriptCommandResult.Fail($"变量不存在: {_name}");
            }

            context.Log($"[PrintVar] {_name}={value}");
            return ScriptCommandResult.Ok(value);
        }
    }
}
