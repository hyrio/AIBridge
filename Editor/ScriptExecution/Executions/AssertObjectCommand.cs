using UnityEngine;

namespace AIBridge.Editor.ScriptExecution.Commands
{
    public class AssertObjectCommand : IScriptCommand
    {
        private readonly string _path;

        public AssertObjectCommand(string path)
        {
            _path = path;
        }

        public string Type => "assert_object";

        public ScriptCommandResult Execute(ScriptExecutionContext context)
        {
            if (string.IsNullOrEmpty(_path))
            {
                return ScriptCommandResult.Fail("assert_object 需要对象路径");
            }

            var go = GameObject.Find(_path);
            if (go == null)
            {
                return ScriptCommandResult.Fail($"对象不存在: {_path}");
            }

            context.Log($"[AssertObject] 对象存在: {_path}");
            return ScriptCommandResult.Ok("Object exists");
        }
    }
}
