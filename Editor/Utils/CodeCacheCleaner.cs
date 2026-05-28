using System;
using System.IO;

namespace AIBridge.Editor
{
    internal static class CodeCacheCleaner
    {
        private const string CodeDirectoryName = "code";
        private const string CompiledDirectoryName = ".compiled";
        private static readonly TimeSpan DefaultRetentionPeriod = TimeSpan.FromDays(3);

        internal static int CleanupIfNeeded(string bridgeDirectory)
        {
            return CleanupIfNeeded(bridgeDirectory, DateTime.UtcNow, DefaultRetentionPeriod);
        }

        internal static int CleanupIfNeeded(string bridgeDirectory, DateTime nowUtc, TimeSpan retentionPeriod)
        {
            if (string.IsNullOrEmpty(bridgeDirectory) || retentionPeriod <= TimeSpan.Zero)
            {
                return 0;
            }

            var fullBridgeDirectory = NormalizeFullPath(bridgeDirectory);
            var codeDirectory = NormalizeFullPath(Path.Combine(fullBridgeDirectory, CodeDirectoryName));
            if (!IsDirectChildOf(codeDirectory, fullBridgeDirectory))
            {
                AIBridgeLogger.LogWarning($"Skipped unsafe code cache cleanup path: {codeDirectory}");
                return 0;
            }

            if (!Directory.Exists(codeDirectory))
            {
                return 0;
            }

            // 只清理明确由 code execute 使用的脚本和编译产物，避免误删用户在 code 目录下临时保存的其它资料。
            var cleanedCount = CleanupFiles(codeDirectory, nowUtc, retentionPeriod, "*.cs", "*.csx");
            var compiledDirectory = NormalizeFullPath(Path.Combine(codeDirectory, CompiledDirectoryName));
            if (Directory.Exists(compiledDirectory))
            {
                if (IsDirectChildOf(compiledDirectory, codeDirectory))
                {
                    cleanedCount += CleanupFiles(compiledDirectory, nowUtc, retentionPeriod, "*.generated.cs", "*.dll", "*.rsp");
                }
                else
                {
                    AIBridgeLogger.LogWarning($"Skipped unsafe compiled code cache cleanup path: {compiledDirectory}");
                }
            }

            if (cleanedCount > 0)
            {
                AIBridgeLogger.LogInfo($"Code cache cleanup: removed {cleanedCount} old file(s)");
            }

            return cleanedCount;
        }

        private static int CleanupFiles(string directory, DateTime nowUtc, TimeSpan retentionPeriod, params string[] patterns)
        {
            var fullDirectory = NormalizeFullPath(directory);
            var cleanedCount = 0;

            foreach (var pattern in patterns)
            {
                string[] files;
                try
                {
                    files = Directory.GetFiles(fullDirectory, pattern, SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    AIBridgeLogger.LogWarning($"Failed to scan code cache '{fullDirectory}' with pattern '{pattern}': {ex.Message}");
                    continue;
                }

                foreach (var file in files)
                {
                    var fullFile = NormalizeFullPath(file);
                    if (!IsDirectChildOf(fullFile, fullDirectory))
                    {
                        AIBridgeLogger.LogWarning($"Skipped unsafe code cache file path: {fullFile}");
                        continue;
                    }

                    try
                    {
                        var fileInfo = new FileInfo(fullFile);
                        var fileAge = nowUtc - fileInfo.LastWriteTimeUtc;
                        if (fileAge <= retentionPeriod)
                        {
                            continue;
                        }

                        File.Delete(fullFile);
                        cleanedCount++;
                        AIBridgeLogger.LogDebug($"Cleaned up old code cache file: {Path.GetFileName(fullFile)} (age: {fileAge.TotalDays:F1} days)");
                    }
                    catch (Exception ex)
                    {
                        AIBridgeLogger.LogWarning($"Failed to delete code cache file '{fullFile}': {ex.Message}");
                    }
                }
            }

            return cleanedCount;
        }

        private static string NormalizeFullPath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsDirectChildOf(string childPath, string parentPath)
        {
            var parent = Directory.GetParent(childPath);
            return parent != null
                && string.Equals(
                    NormalizeFullPath(parent.FullName),
                    NormalizeFullPath(parentPath),
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
