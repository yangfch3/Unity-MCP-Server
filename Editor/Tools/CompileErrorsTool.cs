using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.Compilation;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取当前编译错误列表。
    /// 通过 CompilationPipeline.assemblyCompilationFinished 缓存最近编译错误。
    /// </summary>
    public class CompileErrorsTool : IMcpTool
    {
        public string Name => "build_getCompileErrors";
        public string Category => "build";
        public string Description => "获取当前编译错误列表";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

        private static readonly object _lock = new object();
        private static readonly List<CompilerMessage> _cachedErrors = new List<CompilerMessage>();
        private static bool _subscribed;

        public CompileErrorsTool()
        {
            EnsureSubscribed();
        }

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            lock (_lock)
            {
                if (_cachedErrors.Count == 0)
                    return Task.FromResult(ToolResult.Success("{\"errors\":[],\"message\":\"当前无编译错误\"}"));

                var sb = new StringBuilder();
                sb.Append("{\"errors\":[");
                for (int i = 0; i < _cachedErrors.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    var err = _cachedErrors[i];
                    sb.Append("{\"file\":");
                    sb.Append(MiniJson.SerializeString(err.file ?? ""));
                    sb.Append(",\"line\":");
                    sb.Append(err.line);
                    sb.Append(",\"column\":");
                    sb.Append(err.column);
                    sb.Append(",\"code\":");
                    // CompilerMessage 没有 code 字段，从 message 中提取 CSxxxx
                    sb.Append(MiniJson.SerializeString(ExtractErrorCode(err.message)));
                    sb.Append(",\"message\":");
                    sb.Append(MiniJson.SerializeString(err.message ?? ""));
                    sb.Append('}');
                }
                sb.Append("]}");
                return Task.FromResult(ToolResult.Success(sb.ToString()));
            }
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            _subscribed = true;
            CompilationPipeline.assemblyCompilationFinished += OnAssemblyCompilationFinished;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
        }

        private static void OnCompilationStarted(object obj)
        {
            lock (_lock)
            {
                _cachedErrors.Clear();
            }
        }

        private static void OnAssemblyCompilationFinished(string assemblyName, CompilerMessage[] messages)
        {
            lock (_lock)
            {
                foreach (var msg in messages)
                {
                    if (msg.type == CompilerMessageType.Error)
                        _cachedErrors.Add(msg);
                }
            }
        }

        private static string ExtractErrorCode(string message)
        {
            if (string.IsNullOrEmpty(message)) return "";
            // 尝试匹配 "CSxxxx:" 模式
            int idx = message.IndexOf("CS");
            if (idx < 0) return "";
            int end = idx + 2;
            while (end < message.Length && char.IsDigit(message[end])) end++;
            return end > idx + 2 ? message.Substring(idx, end - idx) : "";
        }
    }
}
