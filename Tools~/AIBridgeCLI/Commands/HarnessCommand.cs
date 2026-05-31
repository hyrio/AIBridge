using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using AIBridgeCLI.Core;
using AIBridgeCLI.Workflow;
using Newtonsoft.Json.Linq;

namespace AIBridgeCLI.Commands
{
    public static class HarnessCommand
    {
        private const int DefaultMaxAgeMinutes = 60;
        private const string SnapshotRelativePath = ".aibridge/harness/capabilities.json";

        public static int Execute(string action, Dictionary<string, string> options, OutputMode outputMode)
        {
            var normalizedAction = string.IsNullOrWhiteSpace(action) ? "status" : action.Trim();
            switch (normalizedAction.ToLowerInvariant())
            {
                case "status":
                    return ExecuteStatus(options, outputMode);
                default:
                    return PrintResult(new CommandResult
                    {
                        success = false,
                        error = "Unknown harness action: " + normalizedAction
                    }, outputMode);
            }
        }

        private static int ExecuteStatus(Dictionary<string, string> options, OutputMode outputMode)
        {
            var snapshotPath = ResolveSnapshotPath(options);
            var maxAgeMinutes = GetInt(options, "max-age-minutes", DefaultMaxAgeMinutes);
            var includeSnapshot = GetBool(options, "include-snapshot", false) || IsFullDetail(options);
            if (!File.Exists(snapshotPath))
            {
                return PrintResult(new CommandResult
                {
                    success = true,
                    data = new
                    {
                        state = "missing",
                        snapshotPath = ToDisplayPath(snapshotPath),
                        defaultSnapshotRelativePath = SnapshotRelativePath,
                        exists = false,
                        stale = true,
                        maxAgeMinutes = maxAgeMinutes,
                        fallback = "Use RootRule minimum workflow and run targeted probes only for required capabilities."
                    }
                }, outputMode);
            }

            try
            {
                var snapshot = JObject.Parse(File.ReadAllText(snapshotPath));
                var generatedAtUtc = ReadGeneratedAtUtc(snapshot);
                var stale = !generatedAtUtc.HasValue || DateTime.UtcNow - generatedAtUtc.Value > TimeSpan.FromMinutes(maxAgeMinutes);
                var data = BuildStatusData(snapshotPath, maxAgeMinutes, generatedAtUtc, stale, snapshot, includeSnapshot);
                return PrintResult(new CommandResult
                {
                    success = true,
                    data = data
                }, outputMode);
            }
            catch (Exception ex)
            {
                return PrintResult(new CommandResult
                {
                    success = false,
                    error = "Invalid harness capability snapshot: " + ex.Message,
                    data = new
                    {
                        state = "invalid",
                        snapshotPath = ToDisplayPath(snapshotPath),
                        exists = true
                    }
                }, outputMode);
            }
        }

        private static JObject BuildStatusData(
            string snapshotPath,
            int maxAgeMinutes,
            DateTime? generatedAtUtc,
            bool stale,
            JObject snapshot,
            bool includeSnapshot)
        {
            var data = new JObject
            {
                ["state"] = stale ? "stale" : "fresh",
                ["snapshotPath"] = ToDisplayPath(snapshotPath),
                ["exists"] = true,
                ["stale"] = stale,
                ["generatedAtUtc"] = generatedAtUtc.HasValue ? generatedAtUtc.Value.ToString("o") : null,
                ["maxAgeMinutes"] = maxAgeMinutes,
                ["snapshotSummary"] = BuildSnapshotSummary(snapshot)
            };

            if (includeSnapshot)
            {
                data["snapshot"] = snapshot;
            }

            return data;
        }

        private static JObject BuildSnapshotSummary(JObject snapshot)
        {
            var summary = new JObject
            {
                ["package"] = new JObject
                {
                    ["name"] = ReadString(snapshot, "package.name"),
                    ["version"] = ReadString(snapshot, "package.version")
                },
                ["unity"] = new JObject
                {
                    ["version"] = ReadString(snapshot, "unity.version"),
                    ["editorAvailable"] = ReadToken(snapshot, "unity.editorAvailable"),
                    ["isCompiling"] = ReadToken(snapshot, "unity.isCompiling"),
                    ["isUpdating"] = ReadToken(snapshot, "unity.isUpdating")
                },
                ["cli"] = new JObject
                {
                    ["status"] = ReadString(snapshot, "cli.status"),
                    ["path"] = ReadString(snapshot, "cli.path")
                },
                ["skills"] = new JObject
                {
                    ["status"] = ReadString(snapshot, "skills.status"),
                    ["roots"] = ReadToken(snapshot, "skills.roots"),
                    ["installed"] = ReadToken(snapshot, "skills.installed")
                },
                ["codeIndex"] = new JObject
                {
                    ["status"] = ReadString(snapshot, "codeIndex.status"),
                    ["enabled"] = ReadToken(snapshot, "codeIndex.enabled")
                },
                ["workflow"] = new JObject
                {
                    ["status"] = ReadString(snapshot, "workflow.status"),
                    ["builtInRecipeCount"] = ReadToken(snapshot, "workflow.builtInRecipeCount"),
                    ["projectRecipeCount"] = ReadToken(snapshot, "workflow.projectRecipeCount"),
                    ["agentManualSteps"] = ReadString(snapshot, "workflow.agentManualSteps")
                },
                ["runtime"] = new JObject
                {
                    ["status"] = ReadString(snapshot, "runtime.status"),
                    ["targetStatus"] = ReadString(snapshot, "runtime.targetStatus")
                },
                ["harness"] = new JObject
                {
                    ["externalExecutor"] = ReadString(snapshot, "harness.externalExecutor"),
                    ["subAgents"] = ReadString(snapshot, "harness.subAgents"),
                    ["shellPermissions"] = ReadString(snapshot, "harness.shellPermissions")
                }
            };

            return summary;
        }

        private static JToken ReadToken(JObject root, string path)
        {
            var token = root == null ? null : root.SelectToken(path);
            return token == null ? null : token.DeepClone();
        }

        private static string ReadString(JObject root, string path)
        {
            var token = root == null ? null : root.SelectToken(path);
            return token == null || token.Type == JTokenType.Null ? null : token.ToString();
        }

        private static string ResolveSnapshotPath(Dictionary<string, string> options)
        {
            string file;
            if (options != null && options.TryGetValue("file", out file) && !string.IsNullOrWhiteSpace(file))
            {
                return Path.IsPathRooted(file)
                    ? Path.GetFullPath(file)
                    : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), file));
            }

            return Path.Combine(PathHelper.GetExchangeDirectory(), "harness", "capabilities.json");
        }

        private static DateTime? ReadGeneratedAtUtc(JObject snapshot)
        {
            if (snapshot == null)
            {
                return null;
            }

            var token = snapshot["generatedAtUtc"];
            if (token == null)
            {
                return null;
            }

            if (token.Type == JTokenType.Date)
            {
                return token.Value<DateTime>().ToUniversalTime();
            }

            if (token.Type != JTokenType.String)
            {
                return null;
            }

            DateTime parsed;
            if (!DateTime.TryParse(
                (string)token,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out parsed))
            {
                return null;
            }

            return parsed;
        }

        private static int GetInt(Dictionary<string, string> options, string key, int defaultValue)
        {
            if (options == null)
            {
                return defaultValue;
            }

            string value;
            int parsed;
            return options.TryGetValue(key, out value) && int.TryParse(value, out parsed) ? parsed : defaultValue;
        }

        private static bool GetBool(Dictionary<string, string> options, string key, bool defaultValue)
        {
            if (options == null)
            {
                return defaultValue;
            }

            string value;
            if (!options.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1";
        }

        private static bool IsFullDetail(Dictionary<string, string> options)
        {
            string detail;
            return options != null
                && options.TryGetValue("detail", out detail)
                && detail.Equals("full", StringComparison.OrdinalIgnoreCase);
        }

        private static string ToDisplayPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            try
            {
                return WorkflowPathHelper.ToDisplayPath(path);
            }
            catch
            {
                return path.Replace('\\', '/');
            }
        }

        private static int PrintResult(CommandResult result, OutputMode outputMode)
        {
            OutputFormatter.PrintResult(result, outputMode, includeIdInRaw: false);
            return result.success ? 0 : 1;
        }
    }
}
