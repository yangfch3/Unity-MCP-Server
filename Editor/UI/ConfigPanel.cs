using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor
{
    /// <summary>
    /// MCP Server 配置面板。纯 UI 视图，服务生命周期由 McpServerManager 管理。
    /// 关闭窗口不会停止服务。
    /// </summary>
    public class ConfigPanel : EditorWindow
    {
        private const string PortPrefKey = "McpServer_Port";
#if !UNITY_6000_OR_NEWER
        private const string DisableCodeExecuteImmediatePrefKey = UnityMcp.Editor.Tools.ExecuteImmediateTool.DisablePrefKey;
#endif
        private const string DisableGameScreenshotPrefKey = UnityMcp.Editor.Tools.GameScreenshotTool.DisablePrefKey;
        private const string SceneScreenshotPrefKey = UnityMcp.Editor.Tools.SceneScreenshotTool.PrefKey;
        private const int DefaultPort = 8090;

        private int _port;
        private bool _enableGameScreenshot;
        private bool _enableSceneScreenshot;
        private Vector2 _scrollPosition;
#if !UNITY_6000_OR_NEWER
        private bool _codeExecuteImmediate;
#endif

        [MenuItem("Window/MCP Server")]
        public static void ShowWindow()
        {
            GetWindow<ConfigPanel>("MCP Server");
        }

        private void OnEnable()
        {
            _port = EditorPrefs.GetInt(PortPrefKey, DefaultPort);
            _enableGameScreenshot = !EditorPrefs.GetBool(DisableGameScreenshotPrefKey, false);
            _enableSceneScreenshot = EditorPrefs.GetBool(SceneScreenshotPrefKey, false);
#if !UNITY_6000_OR_NEWER
            _codeExecuteImmediate = !EditorPrefs.GetBool(DisableCodeExecuteImmediatePrefKey, false);
#endif
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Label("MCP Server", EditorStyles.boldLabel);
            GUILayout.Space(4);

            bool running = McpServerManager.IsRunning;

            EditorGUI.BeginDisabledGroup(running);
            var newPort = EditorGUILayout.IntField("Port", _port);
            if (newPort != _port)
            {
                _port = newPort;
                EditorPrefs.SetInt(PortPrefKey, _port);
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(4);

            if (!running)
            {
                if (GUILayout.Button("Start"))
                    McpServerManager.StartServer(_port);
            }
            else
            {
                if (GUILayout.Button("Stop"))
                    McpServerManager.StopServer();
            }

            GUILayout.Space(8);

            var statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = running ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
            EditorGUILayout.LabelField("Status", running ? "Running" : "Stopped", statusStyle);

            var server = McpServerManager.Server;
            string error = server != null ? server.LastError : null;
            if (!string.IsNullOrEmpty(error))
                EditorGUILayout.HelpBox(error, MessageType.Error);

            GUILayout.Space(12);
            GUILayout.Label("Agent Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Copy the following JSON to the Agent's MCP configuration (e.g., mcp.json)", MessageType.Info);

            string configJson =
                "{\n" +
                "  \"mcpServers\": {\n" +
                "    \"unity-mcp\": {\n" +
                $"      \"url\": \"http://localhost:{_port}/\"\n" +
                "    }\n" +
                "  }\n" +
                "}";

            EditorGUILayout.TextArea(configJson, EditorStyles.textArea, GUILayout.Height(100));

            if (GUILayout.Button("Copy to Clipboard"))
                EditorGUIUtility.systemCopyBuffer = configJson;

            GUILayout.Space(12);
            GUILayout.Label("PlayMode", EditorStyles.boldLabel);
            StartupTiming.RefreshSettings();
            EditorGUILayout.BeginHorizontal();
            var waitText = EditorGUILayout.DelayedTextField(
                new GUIContent("Startup wait (seconds)", "本机、本项目设置；留空或无效值使用 5 秒。仅建议等待，不保证项目就绪。"),
                StartupTiming.WaitText);
            if (waitText != StartupTiming.WaitText)
                StartupTiming.WaitText = waitText;
            var recommendation = StartupTiming.Recommendation;
            if (recommendation.HasValue)
                GUILayout.Label("Recommended: " + recommendation.Value + "s", GUILayout.ExpandWidth(false));
            EditorGUILayout.EndHorizontal();
            if (string.IsNullOrWhiteSpace(waitText))
                EditorGUILayout.LabelField("Effective wait", "5s (default)");
            else if (!float.TryParse(waitText, System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture, out var wait)
                     || wait <= 0 || float.IsInfinity(wait) || float.IsNaN(wait))
                EditorGUILayout.HelpBox("请输入正数秒数；当前使用默认值 5s。", MessageType.Warning);

            GUILayout.Space(12);
            GUILayout.Label("Experimental", EditorStyles.boldLabel);

            var timingSettings = StartupTiming.Settings;
            var timingEnabled = EditorGUILayout.ToggleLeft(
                new GUIContent("Measure startup time (shared project setting)",
                    "默认关闭。开关和关键字保存到 ProjectSettings/UnityMcpSettings.json，可纳入 SVN。"),
                timingSettings.enabled);
            var timingKeyword = EditorGUILayout.DelayedTextField(
                new GUIContent("Ready log keyword", "单个、区分大小写的包含字符串。每次启动只取首次命中；修改后清空本机旧样本。"),
                timingSettings.keyword);
            if (timingEnabled != timingSettings.enabled || timingKeyword != timingSettings.keyword)
                StartupTiming.SaveSettings(timingEnabled, timingKeyword);
            if (timingEnabled && string.IsNullOrWhiteSpace(timingKeyword))
                EditorGUILayout.HelpBox("填写就绪日志关键字后才会采样。", MessageType.Info);
            EditorGUILayout.LabelField("Local samples", StartupTiming.SampleCount + "/50 (recommendation after 10)");
            if (GUILayout.Button("Clear local startup statistics"))
                StartupTiming.ClearHistory();

            var newGameScreenshot = EditorGUILayout.ToggleLeft(
                new GUIContent("Enable Game Screen Shot",
                    "开启后 debug_screenshotGame 工具才会注册到 MCP 服务（默认开启）。服务运行中切换立即生效。"),
                _enableGameScreenshot);
            if (newGameScreenshot != _enableGameScreenshot)
            {
                _enableGameScreenshot = newGameScreenshot;
                EditorPrefs.SetBool(DisableGameScreenshotPrefKey, !_enableGameScreenshot);
                ApplyScreenshotRegistration();
            }

            var newSceneScreenshot = EditorGUILayout.ToggleLeft(
                new GUIContent("Enable Scene Screen Shot",
                    "开启后 debug_screenshotScene 工具才会注册到 MCP 服务（默认关闭）。服务运行中切换立即生效。"),
                _enableSceneScreenshot);
            if (newSceneScreenshot != _enableSceneScreenshot)
            {
                _enableSceneScreenshot = newSceneScreenshot;
                EditorPrefs.SetBool(SceneScreenshotPrefKey, _enableSceneScreenshot);
                ApplyScreenshotRegistration();
            }

#if !UNITY_6000_OR_NEWER
            var newCodeExec = EditorGUILayout.ToggleLeft(
                new GUIContent("Code Execute Immediate",
                    "实验性功能：允许 AI Agent 动态编译并执行 C# 代码（默认开启）。仅限 Mono 环境，存在安全风险，请仅在受信任的环境中使用。"),
                _codeExecuteImmediate);
            if (newCodeExec != _codeExecuteImmediate)
            {
                _codeExecuteImmediate = newCodeExec;
                EditorPrefs.SetBool(DisableCodeExecuteImmediatePrefKey, !_codeExecuteImmediate);
            }
#endif

            EditorGUILayout.EndScrollView();
            if (running || EditorApplication.isPlaying)
                Repaint();
        }

        /// <summary>服务运行中时按两个开关状态热切换截图工具注册。</summary>
        private static void ApplyScreenshotRegistration()
        {
            var registry = McpServerManager.Registry;
            if (registry == null)
                return;

            bool gameEnabled = !EditorPrefs.GetBool(DisableGameScreenshotPrefKey, false);
            if (gameEnabled)
                registry.Register(new UnityMcp.Editor.Tools.GameScreenshotTool());
            else
                registry.Unregister(UnityMcp.Editor.Tools.GameScreenshotTool.ToolName);

            bool sceneEnabled = EditorPrefs.GetBool(SceneScreenshotPrefKey, false);
            if (sceneEnabled)
                registry.Register(new UnityMcp.Editor.Tools.SceneScreenshotTool());
            else
                registry.Unregister(UnityMcp.Editor.Tools.SceneScreenshotTool.ToolName);
        }
    }
}
