namespace AIBridge.Editor.ScriptExecution.Commands
{
    public class SetVarCommand : IScriptCommand
    {
        private readonly string _name;
        private readonly string _value;

        public SetVarCommand(string name, string value)
        {
            _name = name;
            _value = value;
        }

        public string Type => "set_var";

        public ScriptCommandResult Execute(ScriptExecutionContext context)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return ScriptCommandResult.Fail("set_var 需要变量名");
            }

            context.SetVariable(_name, _value);
            context.Log($"[SetVar] {_name}={_value}");
            return ScriptCommandResult.Ok("Variable set");
        }
    }
}
