using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取最近一条 Error/Exception 的完整堆栈信息。
    /// </summary>
    public class StackTraceTool : IMcpTool
    {
        public string Name => "debug_getStackTrace";
        public string Category => "debug";
        public string Description => "获取最近一条 Error/Exception 的完整堆栈信息";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

        private static readonly object _lock = new object();
        private static ErrorEntry? _lastError;
        private static bool _subscribed;

        public StackTraceTool()
        {
            EnsureSubscribed();
        }

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            lock (_lock)
            {
                if (_lastError == null)
                    return Task.FromResult(ToolResult.Success("当前无错误日志"));

                var e = _lastError.Value;
                var sb = new StringBuilder();
                sb.Append("{\"message\":");
                sb.Append(MiniJson.SerializeString(e.Message));
                sb.Append(",\"stackTrace\":");
                sb.Append(MiniJson.SerializeString(e.StackTrace));
                sb.Append(",\"timestamp\":");
                sb.Append(MiniJson.SerializeString(e.Timestamp));
                sb.Append('}');
                return Task.FromResult(ToolResult.Success(sb.ToString()));
            }
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            _subscribed = true;
            Application.logMessageReceived += OnLogMessage;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            lock (_lock)
            {
                _lastError = new ErrorEntry
                {
                    Message = message,
                    StackTrace = stackTrace,
                    Timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)
                };
            }
        }

        private struct ErrorEntry
        {
            public string Message;
            public string StackTrace;
            public string Timestamp;
        }
    }
}
