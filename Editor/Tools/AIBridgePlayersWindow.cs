using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AIBridge.Editor
{
    public sealed class AIBridgePlayersWindow : EditorWindow
    {
        private readonly List<AIBridgeRuntimePlayerInfo> _players = new List<AIBridgeRuntimePlayerInfo>();
        private Vector2 _scrollPosition;
        private string _runtimeDirectory;
        private double _lastRefreshTime;

        [MenuItem("AIBridge/Players")]
        public static void OpenWindow()
        {
            var window = GetWindow<AIBridgePlayersWindow>();
            window.titleContent = new GUIContent(AIBridgeEditorText.T("AIBridge Players", "AIBridge Players"));
            window.minSize = new Vector2(560, 360);
            window.Show();
        }

        private void OnEnable()
        {
            RefreshPlayers();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(6);

            EditorGUILayout.LabelField(
                AIBridgeEditorText.T("Runtime Directory", "Runtime 目录"),
                _runtimeDirectory ?? string.Empty,
                EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(6);
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_players.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    AIBridgeEditorText.T(
                        "No Runtime Player targets found. Start Play Mode or a built Player with AIBridgeRuntime enabled.",
                        "未找到 Runtime Player 目标。请启动挂有 AIBridgeRuntime 的 Play Mode 或已编译 Player。"),
                    MessageType.Info);
            }
            else
            {
                for (var i = 0; i < _players.Count; i++)
                {
                    DrawPlayer(_players[i]);
                    EditorGUILayout.Space(5);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button(AIBridgeEditorText.T("Refresh", "刷新"), EditorStyles.toolbarButton, GUILayout.Width(72)))
            {
                RefreshPlayers();
            }

            if (GUILayout.Button(AIBridgeEditorText.T("Open Directory", "打开目录"), EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                OpenRuntimeDirectory();
            }

            if (GUILayout.Button(AIBridgeEditorText.T("Copy List CLI", "复制列表命令"), EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                CopyCommand("runtime list_targets");
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(
                AIBridgeEditorText.T($"Targets: {_players.Count}", $"目标数：{_players.Count}"),
                EditorStyles.miniLabel,
                GUILayout.Width(90));
            EditorGUILayout.LabelField(
                AIBridgeEditorText.T($"Refreshed: {FormatRefreshAge()}", $"刷新：{FormatRefreshAge()}"),
                EditorStyles.miniLabel,
                GUILayout.Width(130));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlayer(AIBridgeRuntimePlayerInfo player)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(player.TargetId, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            var statusText = player.Stale
                ? AIBridgeEditorText.T("STALE", "已过期")
                : AIBridgeEditorText.T("ONLINE", "在线");
            var previousColor = GUI.color;
            GUI.color = player.Stale ? new Color(1f, 0.72f, 0.25f) : new Color(0.55f, 1f, 0.55f);
            GUILayout.Label(statusText, EditorStyles.boldLabel, GUILayout.Width(72));
            GUI.color = previousColor;
            EditorGUILayout.EndHorizontal();

            DrawInfoLine(AIBridgeEditorText.T("Product", "产品"), JoinNonEmpty(player.ProductName, player.ApplicationVersion));
            DrawInfoLine(AIBridgeEditorText.T("Scene", "场景"), player.ActiveScene);
            DrawInfoLine(AIBridgeEditorText.T("Platform", "平台"), player.Platform);
            DrawInfoLine(AIBridgeEditorText.T("Runtime", "Runtime"), player.RuntimeVersion);
            DrawInfoLine(AIBridgeEditorText.T("Process", "进程"), player.ProcessId > 0 ? player.ProcessId.ToString() : "-");
            DrawInfoLine(AIBridgeEditorText.T("Heartbeat", "Heartbeat"), FormatHeartbeat(player));
            DrawInfoLine(AIBridgeEditorText.T("Path", "路径"), player.TargetPath);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(AIBridgeEditorText.T("Copy Status CLI", "复制状态命令")))
            {
                CopyCommand("runtime status --target " + QuoteTarget(player.TargetId));
            }

            if (GUILayout.Button(AIBridgeEditorText.T("Copy Logs CLI", "复制日志命令")))
            {
                CopyCommand("runtime logs --target " + QuoteTarget(player.TargetId) + " --logType Error --count 100");
            }

            if (GUILayout.Button(AIBridgeEditorText.T("Copy Screenshot CLI", "复制截图命令")))
            {
                CopyCommand("runtime screenshot --target " + QuoteTarget(player.TargetId));
            }

            if (GUILayout.Button(AIBridgeEditorText.T("Open", "打开"), GUILayout.Width(58)))
            {
                if (Directory.Exists(player.TargetPath))
                {
                    EditorUtility.RevealInFinder(player.TargetPath);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void DrawInfoLine(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, GUILayout.Width(88));
            EditorGUILayout.SelectableLabel(string.IsNullOrEmpty(value) ? "-" : value, EditorStyles.miniLabel, GUILayout.Height(16));
            EditorGUILayout.EndHorizontal();
        }

        private void RefreshPlayers()
        {
            _runtimeDirectory = AIBridgeRuntimeBridgeEditorUtility.GetRuntimeDirectory();
            _players.Clear();
            _players.AddRange(AIBridgeRuntimeBridgeEditorUtility.ListPlayers());
            _lastRefreshTime = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private static void OpenRuntimeDirectory()
        {
            var path = AIBridgeRuntimeBridgeEditorUtility.GetRuntimeDirectory();
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }

        private static void CopyCommand(string commandBody)
        {
            EditorGUIUtility.systemCopyBuffer = AIBridgeRuntimeBridgeEditorUtility.BuildCliCommand(commandBody);
            Debug.Log(AIBridgeEditorText.T("[AIBridge] Runtime CLI command copied.", "[AIBridge] Runtime CLI 命令已复制。"));
        }

        private string FormatRefreshAge()
        {
            var age = Math.Max(0, EditorApplication.timeSinceStartup - _lastRefreshTime);
            return age < 1 ? AIBridgeEditorText.T("now", "刚刚") : age.ToString("F0") + "s";
        }

        private static string FormatHeartbeat(AIBridgeRuntimePlayerInfo player)
        {
            if (!player.AgeSeconds.HasValue)
            {
                return "-";
            }

            return player.AgeSeconds.Value.ToString("F1") + "s ago / " + player.LastHeartbeatUtc;
        }

        private static string JoinNonEmpty(string left, string right)
        {
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }

            return string.IsNullOrEmpty(right) ? left : left + " " + right;
        }

        private static string QuoteTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
            {
                return "latest";
            }

            return targetId.IndexOf(' ') >= 0 ? "\"" + targetId.Replace("\"", "\\\"") + "\"" : targetId;
        }
    }
}
