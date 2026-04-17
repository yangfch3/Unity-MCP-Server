using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor
{
    /// <summary>
    /// MCP Server 生命周期管理器（静态单例）。
    /// 服务独立于 ConfigPanel 窗口，关闭窗口不会停止服务。
    /// Domain Reload 后通过 EditorPrefs 检测之前的运行状态并自动重启。
    /// </summary>
    [InitializeOnLoad]
    public static class McpServerManager
    {
        private const string ActivePrefKey = "McpServer_Active";
        private const string PortPrefKey = "McpServer_Port";
        private const int DefaultPort = 8090;

        private static ToolRegistry _toolRegistry;
        private static MainThreadQueue _mainThreadQueue;
        private static McpServer _server;

        public static McpServer Server => _server;
        public static bool IsRunning => _server != null && _server.IsRunning;

        static McpServerManager()
        {
            // Domain Reload 后检查是否需要自动重启
            if (EditorPrefs.GetBool(ActivePrefKey, false))
            {
                int port = EditorPrefs.GetInt(PortPrefKey, DefaultPort);
                // 延迟一帧启动，确保 Editor 初始化完成
                EditorApplication.delayCall += () => StartServer(port);
            }
        }

        public static void StartServer(int port)
        {
            if (IsRunning) return;

            _toolRegistry = new ToolRegistry();
            _toolRegistry.AutoDiscover();
            _mainThreadQueue = new MainThreadQueue();
            _mainThreadQueue.Start();
            _server = new McpServer(_toolRegistry, _mainThreadQueue);
            _server.Start(port);

            if (_server.IsRunning)
            {
                EditorPrefs.SetBool(ActivePrefKey, true);
                EditorPrefs.SetInt(PortPrefKey, port);
            }
        }

        public static void StopServer()
        {
            _server?.Stop();
            _server = null;
            _mainThreadQueue?.Stop();
            _mainThreadQueue = null;
            _toolRegistry = null;

            EditorPrefs.SetBool(ActivePrefKey, false);
        }
    }
}
