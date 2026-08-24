using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityMcp.Editor
{
    /// <summary>
    /// 工具统一接口。所有 MCP 工具实现此接口，通过反射自动发现并注册。
    /// </summary>
    public interface IMcpTool
    {
        /// <summary>工具名称，如 "console_getLogs"</summary>
        string Name { get; }

        /// <summary>所属分类，如 "debug"</summary>
        string Category { get; }

        /// <summary>工具描述</summary>
        string Description { get; }

        /// <summary>JSON Schema 描述参数（序列化为 JSON 字符串）</summary>
        string InputSchema { get; }

        /// <summary>执行工具逻辑，返回结果或错误</summary>
        Task<ToolResult> Execute(Dictionary<string, object> parameters);
    }

    /// <summary>
    /// 条件注册接口。实现此接口的工具在 AutoDiscover 时按 IsEnabled 决定是否注册，
    /// 未实现此接口的工具始终注册。
    /// </summary>
    public interface IConditionalTool
    {
        /// <summary>当前是否启用（通常读取 EditorPrefs），false 时工具不会被注册。</summary>
        bool IsEnabled { get; }
    }
}
