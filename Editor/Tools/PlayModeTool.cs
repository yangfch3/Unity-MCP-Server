using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：进入/退出/查询 Unity Editor PlayMode 状态。
    /// </summary>
    public class PlayModeTool : IMcpTool
    {
        public string Name => "playmode_control";
        public string Category => "editor";
        public string Description => "进入/退出/查询 PlayMode 状态";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"action\":{\"type\":\"string\",\"enum\":[\"enter\",\"exit\",\"status\"],\"description\":\"PlayMode 操作\"}},\"required\":[\"action\"]}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.TryGetValue("action", out var raw) || !(raw is string action) || string.IsNullOrEmpty(action))
            {
                return Task.FromResult(ToolResult.Error("缺少 action 参数"));
            }

            switch (action)
            {
                case "enter":
                    if (EditorApplication.isPlaying)
                        return Task.FromResult(ToolResult.Success("已处于 PlayMode"));
                    EditorApplication.isPlaying = true;
                    return Task.FromResult(ToolResult.Success("已进入 PlayMode"));

                case "exit":
                    if (!EditorApplication.isPlaying)
                        return Task.FromResult(ToolResult.Success("未处于 PlayMode"));
                    EditorApplication.isPlaying = false;
                    return Task.FromResult(ToolResult.Success("已退出 PlayMode"));

                case "status":
                    string status = EditorApplication.isPlaying ? "Playing" : "Stopped";
                    return Task.FromResult(ToolResult.Success("{\"status\":\"" + status + "\"}"));

                default:
                    return Task.FromResult(ToolResult.Error($"无效的 action: {action}"));
            }
        }
    }
}
