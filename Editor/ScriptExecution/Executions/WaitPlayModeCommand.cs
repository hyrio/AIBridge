using UnityEditor;

namespace AIBridge.Editor.ScriptExecution.Commands
{
    /// <summary>
    /// 等待进入或退出 PlayMode。PlayMode 切换跨帧完成，必须用 Pending 重试同一脚本行。
    /// </summary>
    public class WaitPlayModeCommand : IScriptCommand
    {
        private const int DefaultTimeoutMs = 30000;

        private readonly bool _targetPlaying;
        private readonly int _timeoutMs;
        private double _startTime;
        private bool _started;

        public WaitPlayModeCommand(bool targetPlaying, int timeoutMs)
        {
            _targetPlaying = targetPlaying;
            _timeoutMs = timeoutMs > 0 ? timeoutMs : DefaultTimeoutMs;
        }

        public string Type => "wait_playmode";

        public ScriptCommandResult Execute(ScriptExecutionContext context)
        {
            if (!_started)
            {
                _started = true;
                _startTime = EditorApplication.timeSinceStartup;
                context.Log($"[WaitPlayMode] 等待 PlayMode 状态: {(_targetPlaying ? "playing" : "stopped")}，超时 {_timeoutMs}ms");
            }

            if (!EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying == _targetPlaying)
            {
                context.Log("[WaitPlayMode] PlayMode 状态已匹配");
                return ScriptCommandResult.Ok("PlayMode state matched");
            }

            var elapsedMs = (EditorApplication.timeSinceStartup - _startTime) * 1000.0;
            if (elapsedMs >= _timeoutMs)
            {
                return ScriptCommandResult.Fail($"等待 PlayMode 状态超时: {_timeoutMs}ms");
            }

            return ScriptCommandResult.Wait("Waiting for PlayMode state");
        }
    }
}
