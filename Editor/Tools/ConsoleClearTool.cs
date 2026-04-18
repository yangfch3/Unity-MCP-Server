using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：清空日志缓冲区。
    /// 调用 <see cref="ConsoleTool.ClearBuffer"/> 清除所有已缓存的日志条目。
    /// </summary>
    public class ConsoleClearTool : IMcpTool
    {
        /// <summary>工具名称。</summary>
        public string Name => "console_clearLogs";

        /// <summary>所属分类。</summary>
        public string Category => "debug";

        /// <summary>工具描述。</summary>
        public string Description => "清空日志缓冲区";

        /// <summary>JSON Schema 描述参数（无参数）。</summary>
        public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

        /// <summary>执行清空日志缓冲区操作。</summary>
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            ConsoleTool.ClearBuffer();
            return Task.FromResult(ToolResult.Success("Log buffer cleared."));
        }
    }
}
