using System;
using AIBridge.Editor;
using UnityEditor;

namespace AIBridge.Editor.ScriptExecution.Commands
{
    /// <summary>
    /// 等待 Unity 编译结束，支持跨帧轮询，避免阻塞编辑器主线程。
    /// </summary>
    public class WaitCompileCommand : IScriptCommand
    {
        private const int DefaultTimeoutMs = 60000;

        private readonly int _timeoutMs;
        private double _startTime;
        private bool _started;

        public WaitCompileCommand(int timeoutMs)
        {
            _timeoutMs = timeoutMs > 0 ? timeoutMs : DefaultTimeoutMs;
        }

        public string Type => "wait_compile";

        public ScriptCommandResult Execute(ScriptExecutionContext context)
        {
            if (!_started)
            {
                _started = true;
                _startTime = EditorApplication.timeSinceStartup;
                context.Log($"[WaitCompile] 等待 Unity 编译结束，超时 {_timeoutMs}ms");
            }

            var result = CompilationTracker.CurrentResult;
            var isTrackingCompile = result != null && result.status == CompilationTracker.CompilationStatus.Compiling;
            if (!EditorApplication.isCompiling && !isTrackingCompile)
            {
                if (result != null && result.status == CompilationTracker.CompilationStatus.Failed)
                {
                    return ScriptCommandResult.Fail($"Unity 编译失败，错误数: {result.errorCount}");
                }

                context.Log("[WaitCompile] Unity 当前未在编译");
                return ScriptCommandResult.Ok("Unity compile completed");
            }

            var elapsedMs = (EditorApplication.timeSinceStartup - _startTime) * 1000.0;
            if (elapsedMs >= _timeoutMs)
            {
                return ScriptCommandResult.Fail($"等待 Unity 编译超时: {_timeoutMs}ms");
            }

            return ScriptCommandResult.Wait("Waiting for Unity compile");
        }
    }
}
