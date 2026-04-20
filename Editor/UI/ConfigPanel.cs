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
        private const int DefaultPort = 8090;

        private int _port;
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
#if !UNITY_6000_OR_NEWER
            _codeExecuteImmediate = EditorPrefs.GetBool(CodeExecuteImmediatePrefKey, false);
#endif
        }

        private void OnGUI()
        {
            GUILayout.Label("MCP Server", EditorStyles.boldLabel);
            GUILayout.Space(4);

            bool running = McpServerManager.IsRunning;

            // Port
            EditorGUI.BeginDisabledGroup(running);
            var newPort = EditorGUILayout.IntField("Port", _port);
            if (newPort != _port)
            {
                _port = newPort;
                EditorPrefs.SetInt(PortPrefKey, _port);
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(4);

            // Start / Stop
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

            // Status
            var statusStyle = new GUIStyle(EditorStyles.label);
            statusStyle.normal.textColor = running ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.2f, 0.2f);
            EditorGUILayout.LabelField("Status", running ? "Running" : "Stopped", statusStyle);

            // Error
            var server = McpServerManager.Server;
            string error = server != null ? server.LastError : null;
            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            // MCP Config JSON
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

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(configJson, EditorStyles.textArea, GUILayout.Height(100));
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Copy to Clipboard"))
            {
                EditorGUIUtility.systemCopyBuffer = configJson;
            }

#if !UNITY_6000_OR_NEWER
            // Experimental features section
            GUILayout.Space(12);
            GUILayout.Label("Experimental", EditorStyles.boldLabel);
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

            // Repaint while running to keep status fresh
            if (running)
                Repaint();
        }
    }
}
