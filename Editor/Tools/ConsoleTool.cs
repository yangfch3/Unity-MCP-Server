using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取 Unity Console 最近 N 条日志。
    /// 通过 Application.logMessageReceived 捕获日志到内存环形缓冲区。
    /// </summary>
    public class ConsoleTool : IMcpTool
    {
        public string Name => "console_getLogs";
        public string Category => "debug";
        public string Description => "获取 Unity Console 最近 N 条日志";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"count\":{\"type\":\"integer\",\"description\":\"日志条数\",\"default\":20}}}";

        private const int MaxBufferSize = 1000;

        // 日志缓冲区及锁（logMessageReceived 可能在非主线程触发）
        private static readonly object _lock = new object();
        private static readonly List<LogEntry> _buffer = new List<LogEntry>();
        private static bool _subscribed;

        public ConsoleTool()
        {
            EnsureSubscribed();
        }

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            int count = 20;
            if (parameters != null && parameters.TryGetValue("count", out var raw))
            {
                if (raw is long l) count = (int)l;
                else if (raw is double d) count = (int)d;
                else if (raw is int i) count = i;
            }
            if (count < 0) count = 0;

            string json;
            lock (_lock)
            {
                int total = _buffer.Count;
                int take = Math.Min(count, total);
                int start = total - take;

                var sb = new StringBuilder();
                sb.Append('[');
                for (int idx = start; idx < total; idx++)
                {
                    if (idx > start) sb.Append(',');
                    var entry = _buffer[idx];
                    sb.Append("{\"level\":");
                    sb.Append(MiniJson.SerializeString(entry.Level));
                    sb.Append(",\"timestamp\":");
                    sb.Append(MiniJson.SerializeString(entry.Timestamp));
                    sb.Append(",\"message\":");
                    sb.Append(MiniJson.SerializeString(entry.Message));
                    sb.Append('}');
                }
                sb.Append(']');
                json = sb.ToString();
            }

            return Task.FromResult(ToolResult.Success(json));
        }

        /// <summary>确保只订阅一次 logMessageReceived。</summary>
        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            _subscribed = true;
            Application.logMessageReceived += OnLogMessage;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            var entry = new LogEntry
            {
                Level = ToLevel(type),
                Timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Message = message
            };

            lock (_lock)
            {
                if (_buffer.Count >= MaxBufferSize)
                    _buffer.RemoveAt(0);
                _buffer.Add(entry);
            }
        }

        private static string ToLevel(LogType type)
        {
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return "Error";
                case LogType.Warning:
                    return "Warning";
                default:
                    return "Log";
            }
        }

        /// <summary>内部日志条目。</summary>
        private struct LogEntry
        {
            public string Level;
            public string Timestamp;
            public string Message;
        }

        // --- 测试辅助方法 ---

        /// <summary>清空日志缓冲区（仅供测试使用）。</summary>
        internal static void ClearBuffer()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }

        /// <summary>向缓冲区注入一条日志（仅供测试使用，绕过 Application 事件）。</summary>
        internal static void InjectLog(string level, string timestamp, string message)
        {
            lock (_lock)
            {
                if (_buffer.Count >= MaxBufferSize)
                    _buffer.RemoveAt(0);
                _buffer.Add(new LogEntry
                {
                    Level = level,
                    Timestamp = timestamp,
                    Message = message
                });
            }
        }

        /// <summary>当前缓冲区日志数量（仅供测试使用）。</summary>
        internal static int BufferCount
        {
            get { lock (_lock) { return _buffer.Count; } }
        }
    }
}
