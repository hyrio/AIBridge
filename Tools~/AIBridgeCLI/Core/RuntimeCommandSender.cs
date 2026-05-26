using System;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AIBridgeCLI.Core
{
    public class RuntimeCommandSender
    {
        private readonly string _runtimeDirectory;
        private readonly string _target;
        private readonly int _timeout;
        private readonly int _pollInterval;

        public RuntimeCommandSender(string runtimeDirectoryOverride, string target, int timeout = 5000, int pollInterval = 50)
        {
            _runtimeDirectory = RuntimePathHelper.ResolveRuntimeDirectory(runtimeDirectoryOverride);
            _target = string.IsNullOrWhiteSpace(target) ? "latest" : target;
            _timeout = timeout;
            _pollInterval = pollInterval;
        }

        public CommandResult SendCommand(CommandRequest request)
        {
            var runtimeAction = RuntimePathHelper.GetRuntimeAction(request);
            if (string.Equals(runtimeAction, "runtime.list_targets", StringComparison.OrdinalIgnoreCase))
            {
                return ListTargets(request?.id);
            }

            var targetInfo = RuntimePathHelper.ResolveTarget(_runtimeDirectory, _target);
            if (targetInfo == null)
            {
                return CreateTargetNotFoundResult(request?.id);
            }

            EnsureRequestId(request);
            WriteCommandFile(targetInfo, request);

            var resultFile = Path.Combine(targetInfo.resultsPath, $"{request.id}.json");
            var startTime = DateTime.UtcNow;
            while ((DateTime.UtcNow - startTime).TotalMilliseconds < _timeout)
            {
                if (File.Exists(resultFile))
                {
                    Thread.Sleep(10);
                    try
                    {
                        var resultJson = File.ReadAllText(resultFile, Encoding.UTF8);
                        var result = ParseRuntimeResult(request.id, resultJson);
                        try { File.Delete(resultFile); } catch { }
                        return result;
                    }
                    catch (IOException)
                    {
                        Thread.Sleep(_pollInterval);
                        continue;
                    }
                }

                Thread.Sleep(_pollInterval);
            }

            var commandFile = Path.Combine(targetInfo.commandsPath, $"{request.id}.json");
            try { if (File.Exists(commandFile)) File.Delete(commandFile); } catch { }

            return new CommandResult
            {
                id = request.id,
                success = false,
                error = $"Timeout waiting for runtime result after {_timeout}ms.",
                data = new
                {
                    runtimeDirectory = _runtimeDirectory,
                    target = targetInfo.targetId,
                    action = runtimeAction
                }
            };
        }

        public CommandResult TrySendCommandNoWait(CommandRequest request)
        {
            var runtimeAction = RuntimePathHelper.GetRuntimeAction(request);
            if (string.Equals(runtimeAction, "runtime.list_targets", StringComparison.OrdinalIgnoreCase))
            {
                return ListTargets(request?.id);
            }

            var targetInfo = RuntimePathHelper.ResolveTarget(_runtimeDirectory, _target);
            if (targetInfo == null)
            {
                return CreateTargetNotFoundResult(request?.id);
            }

            EnsureRequestId(request);
            WriteCommandFile(targetInfo, request);
            return new CommandResult
            {
                id = request.id,
                success = true,
                data = new
                {
                    id = request.id,
                    status = "sent",
                    target = targetInfo.targetId,
                    action = runtimeAction
                }
            };
        }

        public CommandResult ListTargets(string requestId = null)
        {
            var targets = RuntimePathHelper.ListTargets(_runtimeDirectory);
            return new CommandResult
            {
                id = requestId,
                success = true,
                data = new
                {
                    runtimeDirectory = _runtimeDirectory,
                    count = targets.Count,
                    targets = targets
                }
            };
        }

        private CommandResult CreateTargetNotFoundResult(string requestId)
        {
            return new CommandResult
            {
                id = requestId,
                success = false,
                error = "Runtime target was not found. Start a Player with AIBridgeRuntime or pass --runtime-dir/--target.",
                data = new
                {
                    runtimeDirectory = _runtimeDirectory,
                    target = _target,
                    targets = RuntimePathHelper.ListTargets(_runtimeDirectory)
                }
            };
        }

        private static void EnsureRequestId(CommandRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrEmpty(request.id))
            {
                request.id = PathHelper.GenerateCommandId();
            }
        }

        private static void WriteCommandFile(RuntimeTargetInfo targetInfo, CommandRequest request)
        {
            Directory.CreateDirectory(targetInfo.commandsPath);
            Directory.CreateDirectory(targetInfo.resultsPath);

            var commandFile = Path.Combine(targetInfo.commandsPath, $"{request.id}.json");
            var tempFile = commandFile + ".tmp";
            var json = JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            // 先写临时文件再改名，避免 Player 读到半截命令。
            File.WriteAllText(tempFile, json, new UTF8Encoding(false));
            if (File.Exists(commandFile))
            {
                File.Delete(commandFile);
            }

            File.Move(tempFile, commandFile);
        }

        private static CommandResult ParseRuntimeResult(string requestId, string resultJson)
        {
            try
            {
                var token = JObject.Parse(resultJson);
                return new CommandResult
                {
                    id = ReadString(token, "id") ?? ReadString(token, "CommandId") ?? requestId,
                    success = ReadBool(token, "success") ?? ReadBool(token, "Success") ?? false,
                    error = ReadString(token, "error") ?? ReadString(token, "Error"),
                    data = token["data"] ?? token["Data"],
                    executionTime = ReadLong(token, "executionTime") ?? ReadLong(token, "ExecutionTime")
                };
            }
            catch (Exception ex)
            {
                return new CommandResult
                {
                    id = requestId,
                    success = false,
                    error = $"Failed to parse runtime result: {ex.Message}",
                    data = resultJson
                };
            }
        }

        private static string ReadString(JObject token, string name)
        {
            return token.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var value) ? value.Value<string>() : null;
        }

        private static bool? ReadBool(JObject token, string name)
        {
            if (!token.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var value))
            {
                return null;
            }

            return value.Type == JTokenType.Boolean ? value.Value<bool>() : (bool?)null;
        }

        private static long? ReadLong(JObject token, string name)
        {
            if (!token.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var value))
            {
                return null;
            }

            return value.Type == JTokenType.Integer ? value.Value<long>() : (long?)null;
        }
    }
}
