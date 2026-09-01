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
        private const string CodeExecuteImmediatePrefKey = "McpServer_CodeExecuteImmediate";
        private const string GameScreenshotPrefKey = UnityMcp.Editor.Tools.GameScreenshotTool.PrefKey;
        private const string SceneScreenshotPrefKey = UnityMcp.Editor.Tools.SceneScreenshotTool.PrefKey;
        private const int DefaultPort = 8090;

        private int _port;
        private bool _enableGameScreenshot;
        private bool _enableSceneScreenshot;
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
            _enableGameScreenshot = EditorPrefs.GetBool(GameScreenshotPrefKey, false);
            _enableSceneScreenshot = EditorPrefs.GetBool(SceneScreenshotPrefKey, false);
#if !UNITY_6000_OR_NEWER
            _codeExecuteImmediate = EditorPrefs.GetBool(CodeExecuteImmediatePrefKey, false);
#endif
        }

        private void OnGUI()
        {
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
            GUILayout.Label("Experimental", EditorStyles.boldLabel);

            var newGameScreenshot = EditorGUILayout.ToggleLeft(
                new GUIContent("Enable Game Screen Shot",
                    "开启后 debug_screenshotGame 工具才会注册到 MCP 服务（默认关闭）。服务运行中切换立即生效。"),
                _enableGameScreenshot);
            if (newGameScreenshot != _enableGameScreenshot)
            {
                _enableGameScreenshot = newGameScreenshot;
                EditorPrefs.SetBool(GameScreenshotPrefKey, _enableGameScreenshot);
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
                    "实验性功能：允许 AI Agent 动态编译并执行 C# 代码。仅限 Mono 环境，存在安全风险，请仅在受信任的环境中开启。"),
                _codeExecuteImmediate);
            if (newCodeExec != _codeExecuteImmediate)
            {
                _codeExecuteImmediate = newCodeExec;
                EditorPrefs.SetBool(CodeExecuteImmediatePrefKey, _codeExecuteImmediate);
            }
#endif

            if (running)
                Repaint();
        }

        /// <summary>服务运行中时按两个开关状态热切换截图工具注册。</summary>
        private static void ApplyScreenshotRegistration()
        {
            var registry = McpServerManager.Registry;
            if (registry == null)
                return;

            bool gameEnabled = EditorPrefs.GetBool(GameScreenshotPrefKey, false);
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
