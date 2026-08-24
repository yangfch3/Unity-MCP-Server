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

        /// <summary>当前工具注册表，服务未运行时为 null。供运行中热切换工具注册（如 ConfigPanel 开关）。</summary>
        public static ToolRegistry Registry => _toolRegistry;

        static McpServerManager()
        {
            if (!EditorPrefs.GetBool(ActivePrefKey, false))
                return;

            int port = EditorPrefs.GetInt(PortPrefKey, DefaultPort);

            // 直接在静态构造函数中尝试恢复，不依赖 delayCall/update，
            // 这样即使 Unity 处于后台（主循环暂停）也能立即重启服务。
            try
            {
                StartServer(port);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[McpServer] Immediate recovery failed ({ex.Message}), will retry via delayCall.");
                EditorApplication.delayCall += () => StartServer(port);
            }

            // 兜底：如果上面两条路径都没成功（极端时序），焦点恢复时再补一次。
            EditorApplication.focusChanged += OnFocusChanged;
        }

        private static void OnFocusChanged(bool focused)
        {
            if (!focused) return;
            if (IsRunning) return;
            if (!EditorPrefs.GetBool(ActivePrefKey, false)) return;

            int port = EditorPrefs.GetInt(PortPrefKey, DefaultPort);
            Debug.Log("[McpServer] Recovering after focus regained.");
            StartServer(port);
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
