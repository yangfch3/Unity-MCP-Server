using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：按路径执行 Unity Editor 菜单项。
    /// </summary>
    public class MenuTool : IMcpTool
    {
        public string Name => "menu_execute";
        public string Category => "editor";
        public string Description => "按路径执行 Unity Editor 菜单项";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\",\"description\":\"Unity 菜单路径\"}},\"required\":[\"path\"]}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            if (parameters == null || !parameters.TryGetValue("path", out var raw) || !(raw is string path) || string.IsNullOrEmpty(path))
            {
                return Task.FromResult(ToolResult.Error("缺少 path 参数"));
            }

            bool success = EditorApplication.ExecuteMenuItem(path);
            if (!success)
            {
                return Task.FromResult(ToolResult.Error($"菜单路径不存在: {path}"));
            }

            return Task.FromResult(ToolResult.Success($"菜单项执行成功: {path}"));
        }
    }
}
