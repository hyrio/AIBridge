using System;
using System.Collections.Generic;
using System.IO;
using AIBridge.Internal.Json;

namespace AIBridge.Editor
{
    internal sealed class AIBridgeRuntimePlayerInfo
    {
        public string TargetId;
        public string ProductName;
        public string ApplicationVersion;
        public string RuntimeVersion;
        public string Platform;
        public string ActiveScene;
        public string TargetPath;
        public string CommandsPath;
        public string ResultsPath;
        public string LastHeartbeatUtc;
        public int ProcessId;
        public bool Stale;
        public double? AgeSeconds;
    }

    internal static class AIBridgeRuntimeBridgeEditorUtility
    {
        public const string RuntimeDirectoryName = "runtime";
        public const string TargetsDirectoryName = "targets";
        public const string HeartbeatFileName = "heartbeat.json";
        public const string CommandsDirectoryName = "commands";
        public const string ResultsDirectoryName = "results";
        private static readonly TimeSpan StaleHeartbeatTimeout = TimeSpan.FromSeconds(15);

        public static string GetRuntimeDirectory()
        {
            var configured = AIBridgeProjectSettings.Instance.RuntimeBridge.ExchangeDirectory;
            if (string.IsNullOrWhiteSpace(configured))
            {
                return Path.Combine(AIBridge.BridgeDirectory, RuntimeDirectoryName);
            }

            return Path.IsPathRooted(configured)
                ? Path.GetFullPath(configured)
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), configured));
        }

        public static List<AIBridgeRuntimePlayerInfo> ListPlayers()
        {
            var players = new List<AIBridgeRuntimePlayerInfo>();
            var targetsRoot = Path.Combine(GetRuntimeDirectory(), TargetsDirectoryName);
            if (!Directory.Exists(targetsRoot))
            {
                return players;
            }

            var targetDirectories = Directory.GetDirectories(targetsRoot);
            for (var i = 0; i < targetDirectories.Length; i++)
            {
                var targetPath = targetDirectories[i];
                var heartbeatPath = Path.Combine(targetPath, HeartbeatFileName);
                var heartbeat = ReadHeartbeat(heartbeatPath);
                var targetId = GetString(heartbeat, "targetId");
                if (string.IsNullOrEmpty(targetId))
                {
                    targetId = Path.GetFileName(targetPath);
                }

                var lastHeartbeat = ParseHeartbeatTime(GetString(heartbeat, "lastHeartbeatUtc"));
                var ageSeconds = lastHeartbeat.HasValue
                    ? (double?)(DateTime.UtcNow - lastHeartbeat.Value).TotalSeconds
                    : null;

                players.Add(new AIBridgeRuntimePlayerInfo
                {
                    TargetId = targetId,
                    ProductName = GetString(heartbeat, "productName"),
                    ApplicationVersion = GetString(heartbeat, "applicationVersion"),
                    RuntimeVersion = GetString(heartbeat, "runtimeVersion"),
                    Platform = GetString(heartbeat, "platform"),
                    ActiveScene = GetString(heartbeat, "activeScene"),
                    TargetPath = targetPath,
                    CommandsPath = GetString(heartbeat, "commandsPath") ?? Path.Combine(targetPath, CommandsDirectoryName),
                    ResultsPath = GetString(heartbeat, "resultsPath") ?? Path.Combine(targetPath, ResultsDirectoryName),
                    LastHeartbeatUtc = lastHeartbeat.HasValue ? lastHeartbeat.Value.ToString("o") : null,
                    ProcessId = GetInt(heartbeat, "processId"),
                    Stale = !lastHeartbeat.HasValue || DateTime.UtcNow - lastHeartbeat.Value > StaleHeartbeatTimeout,
                    AgeSeconds = ageSeconds
                });
            }

            players.Sort(ComparePlayers);
            return players;
        }

        public static string BuildCliCommand(string commandBody)
        {
            var runtimeDirectory = GetRuntimeDirectory();
            return "$CLI " + commandBody + " --runtime-dir " + Quote(runtimeDirectory);
        }

        public static string Quote(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static Dictionary<string, object> ReadHeartbeat(string heartbeatPath)
        {
            if (!File.Exists(heartbeatPath))
            {
                return null;
            }

            try
            {
                return AIBridgeJson.DeserializeObject(File.ReadAllText(heartbeatPath));
            }
            catch
            {
                return null;
            }
        }

        private static DateTime? ParseHeartbeatTime(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(value, out var parsed))
            {
                return parsed.UtcDateTime;
            }

            return null;
        }

        private static string GetString(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out var value) || value == null)
            {
                return null;
            }

            return value.ToString();
        }

        private static int GetInt(Dictionary<string, object> data, string key)
        {
            if (data == null || !data.TryGetValue(key, out var value) || value == null)
            {
                return 0;
            }

            if (value is long longValue)
            {
                return (int)longValue;
            }

            if (value is double doubleValue)
            {
                return (int)doubleValue;
            }

            return int.TryParse(value.ToString(), out var parsed) ? parsed : 0;
        }

        private static int ComparePlayers(AIBridgeRuntimePlayerInfo left, AIBridgeRuntimePlayerInfo right)
        {
            if (left.Stale != right.Stale)
            {
                return left.Stale ? 1 : -1;
            }

            var leftAge = left.AgeSeconds ?? double.MaxValue;
            var rightAge = right.AgeSeconds ?? double.MaxValue;
            var ageCompare = leftAge.CompareTo(rightAge);
            return ageCompare != 0
                ? ageCompare
                : string.Compare(left.TargetId, right.TargetId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
