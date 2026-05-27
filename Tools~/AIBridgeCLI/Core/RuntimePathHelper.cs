using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AIBridgeCLI.Core
{
    public class RuntimeTargetInfo
    {
        public string targetId { get; set; }
        public string path { get; set; }
        public string heartbeatPath { get; set; }
        public string commandsPath { get; set; }
        public string resultsPath { get; set; }
        public string screenshotsPath { get; set; }
        public string source { get; set; }
        public string platform { get; set; }
        public string projectName { get; set; }
        public string deviceName { get; set; }
        public string targetKind { get; set; }
        public bool? reachable { get; set; }
        public string connectionUrl { get; set; }
        public bool preferred { get; set; }
        public bool stale { get; set; }
        public double? ageSeconds { get; set; }
        public string lastHeartbeatUtc { get; set; }
        public JObject heartbeat { get; set; }
    }

    public static class RuntimePathHelper
    {
        public const string RuntimeDirectoryName = "runtime";
        public const string TargetsDirectoryName = "targets";
        public const string CommandsDirectoryName = "commands";
        public const string ResultsDirectoryName = "results";
        public const string ScreenshotsDirectoryName = "screenshots";
        public const string HeartbeatFileName = "heartbeat.json";
        public const string RuntimeDirEnvironment = "AIBRIDGE_RUNTIME_DIR";
        private static readonly TimeSpan StaleHeartbeatTimeout = TimeSpan.FromSeconds(15);

        public static string ResolveRuntimeDirectory(string overridePath)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return Path.GetFullPath(overridePath);
            }

            var envPath = Environment.GetEnvironmentVariable(RuntimeDirEnvironment);
            if (!string.IsNullOrWhiteSpace(envPath))
            {
                return Path.GetFullPath(envPath);
            }

            return Path.Combine(PathHelper.GetExchangeDirectory(), RuntimeDirectoryName);
        }

        public static List<RuntimeTargetInfo> ListTargets(string runtimeDirectory)
        {
            var results = new List<RuntimeTargetInfo>();
            var targetsDirectory = Path.Combine(runtimeDirectory, TargetsDirectoryName);
            if (!Directory.Exists(targetsDirectory))
            {
                return results;
            }

            foreach (var targetPath in Directory.GetDirectories(targetsDirectory))
            {
                var heartbeatPath = Path.Combine(targetPath, HeartbeatFileName);
                var heartbeat = ReadHeartbeat(heartbeatPath);
                var targetId = heartbeat?["targetId"]?.Value<string>();
                if (string.IsNullOrWhiteSpace(targetId))
                {
                    targetId = Path.GetFileName(targetPath);
                }

                var lastHeartbeat = ParseHeartbeatTime(heartbeat);
                var ageSeconds = lastHeartbeat.HasValue
                    ? (double?)(DateTime.UtcNow - lastHeartbeat.Value).TotalSeconds
                    : null;

                results.Add(new RuntimeTargetInfo
                {
                    targetId = targetId,
                    path = targetPath,
                    heartbeatPath = heartbeatPath,
                    commandsPath = GetHeartbeatPathOrDefault(heartbeat, "commandsPath", targetPath, CommandsDirectoryName),
                    resultsPath = GetHeartbeatPathOrDefault(heartbeat, "resultsPath", targetPath, ResultsDirectoryName),
                    screenshotsPath = GetHeartbeatPathOrDefault(heartbeat, "screenshotsPath", targetPath, ScreenshotsDirectoryName),
                    source = "file-heartbeat",
                    platform = heartbeat?["platform"]?.Value<string>(),
                    projectName = heartbeat?["productName"]?.Value<string>() ?? heartbeat?["projectName"]?.Value<string>(),
                    deviceName = heartbeat?["deviceName"]?.Value<string>(),
                    targetKind = heartbeat?["targetKind"]?.Value<string>(),
                    reachable = !lastHeartbeat.HasValue ? (bool?)null : DateTime.UtcNow - lastHeartbeat.Value <= StaleHeartbeatTimeout,
                    connectionUrl = heartbeat?["reachableUrl"]?.Value<string>() ?? heartbeat?["httpUrl"]?.Value<string>(),
                    stale = !lastHeartbeat.HasValue || DateTime.UtcNow - lastHeartbeat.Value > StaleHeartbeatTimeout,
                    ageSeconds = ageSeconds,
                    lastHeartbeatUtc = lastHeartbeat.HasValue ? lastHeartbeat.Value.ToString("o") : null,
                    heartbeat = heartbeat
                });
            }

            return results
                .OrderBy(t => t.stale)
                .ThenByDescending(t => t.lastHeartbeatUtc)
                .ThenBy(t => t.targetId, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static RuntimeTargetInfo ResolveTarget(string runtimeDirectory, string target)
        {
            var targets = ListTargets(runtimeDirectory);
            if (targets.Count == 0)
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(target) || string.Equals(target, "latest", StringComparison.OrdinalIgnoreCase))
            {
                return targets[0];
            }

            return targets.FirstOrDefault(t => string.Equals(t.targetId, target, StringComparison.OrdinalIgnoreCase));
        }

        public static bool TryResolveFreshHttpUrl(string runtimeDirectory, string target, out string url)
        {
            return TryResolveFreshHttpUrl(runtimeDirectory, target, null, null, out url);
        }

        public static bool TryResolveFreshHttpUrl(
            string runtimeDirectory,
            string target,
            string platform,
            string projectHint,
            out string url)
        {
            url = null;
            var targetInfo = ResolveTarget(runtimeDirectory, target);
            if (targetInfo == null || targetInfo.stale)
            {
                return false;
            }

            if (!MatchesHeartbeatFilters(targetInfo.heartbeat, platform, projectHint))
            {
                return false;
            }

            var heartbeatUrl = targetInfo.heartbeat?["reachableUrl"]?.Value<string>()
                ?? targetInfo.heartbeat?["httpUrl"]?.Value<string>()
                ?? targetInfo.heartbeat?["bindUrl"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(heartbeatUrl))
            {
                return false;
            }

            url = heartbeatUrl.Trim().TrimEnd('/');
            return true;
        }

        private static bool MatchesHeartbeatFilters(JObject heartbeat, string platform, string projectHint)
        {
            if (heartbeat == null)
            {
                return string.IsNullOrWhiteSpace(platform) && string.IsNullOrWhiteSpace(projectHint);
            }

            var heartbeatPlatform = heartbeat["platform"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(platform)
                && (string.IsNullOrWhiteSpace(heartbeatPlatform)
                    || heartbeatPlatform.IndexOf(platform, StringComparison.OrdinalIgnoreCase) < 0))
            {
                return false;
            }

            var productName = heartbeat["productName"]?.Value<string>() ?? heartbeat["projectName"]?.Value<string>();
            if (!string.IsNullOrWhiteSpace(projectHint)
                && !string.Equals(productName, projectHint, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        public static string GetRuntimeAction(CommandRequest request)
        {
            if (request == null || request.@params == null)
            {
                return null;
            }

            return request.@params.TryGetValue("action", out var actionValue) ? actionValue?.ToString() : null;
        }

        public static string GetCommandPath(RuntimeTargetInfo targetInfo, string commandId)
        {
            return Path.Combine(targetInfo.commandsPath, commandId + ".json");
        }

        public static string GetResultPath(RuntimeTargetInfo targetInfo, string commandId)
        {
            return Path.Combine(targetInfo.resultsPath, commandId + ".json");
        }

        private static string GetHeartbeatPathOrDefault(JObject heartbeat, string heartbeatKey, string targetPath, string directoryName)
        {
            var value = heartbeat?[heartbeatKey]?.Value<string>();
            return string.IsNullOrWhiteSpace(value) ? Path.Combine(targetPath, directoryName) : value;
        }

        private static JObject ReadHeartbeat(string heartbeatPath)
        {
            if (!File.Exists(heartbeatPath))
            {
                return null;
            }

            try
            {
                using (var reader = new JsonTextReader(new StringReader(File.ReadAllText(heartbeatPath))))
                {
                    reader.DateParseHandling = DateParseHandling.None;
                    return JObject.Load(reader);
                }
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ParseHeartbeatTime(JObject heartbeat)
        {
            var value = heartbeat?["lastHeartbeatUtc"]?.Value<string>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(value, out var parsedOffset))
            {
                return parsedOffset.UtcDateTime;
            }

            return null;
        }
    }
}
