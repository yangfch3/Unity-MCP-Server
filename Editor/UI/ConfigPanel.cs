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
        private const int DefaultPort = 8090;

        private int _port;

        [MenuItem("Window/MCP Server")]
        public static void ShowWindow()
        {
            GetWindow<ConfigPanel>("MCP Server");
        }

        private void OnEnable()
        {
            _port = EditorPrefs.GetInt(PortPrefKey, DefaultPort);
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
            EditorGUILayout.LabelField("Status", running ? "Running" : "Stopped");

            var server = McpServerManager.Server;
            if (running && server != null)
            {
                EditorGUILayout.LabelField("Connected Agents", server.ConnectedAgents.ToString());
            }

            // Error
            string error = server != null ? server.LastError : null;
            if (!string.IsNullOrEmpty(error))
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            // MCP Config JSON
            GUILayout.Space(12);
            GUILayout.Label("Agent Configuration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("将以下 JSON 复制到 Agent 的 MCP 配置中（如 mcp.json）", MessageType.Info);

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
            {
                EditorGUIUtility.systemCopyBuffer = configJson;
            }

            // Repaint while running to keep status fresh
            if (running)
                Repaint();
        }
    }
}
