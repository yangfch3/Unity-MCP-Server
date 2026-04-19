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
        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{"
            + "\"count\":{\"type\":\"integer\",\"description\":\"日志条数\",\"default\":20},"
            + "\"level\":{\"type\":\"string\",\"description\":\"日志级别过滤\",\"enum\":[\"Error\",\"Warning\",\"Log\"]},"
            + "\"keyword\":{\"type\":\"string\",\"description\":\"关键字过滤（大小写不敏感）\"},"
            + "\"beforeIndex\":{\"type\":\"integer\",\"description\":\"上下文模式：锚点索引（稳定全局 ID）\"}"
            + "}}";

        private const int MaxBufferSize = 2500;

        // 日志缓冲区及锁（logMessageReceived 可能在非主线程触发）
        private static readonly object _lock = new object();
        private static readonly List<LogEntry> _buffer = new List<LogEntry>();
        private static bool _subscribed;
        private static long _nextIndex; // 全局递增计数器，每条日志写入时分配稳定 ID

        public ConsoleTool()
        {
            EnsureSubscribed();
        }

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            // Parse count
            int count = 20;
            if (parameters != null && parameters.TryGetValue("count", out var rawCount))
            {
                if (rawCount is long l) count = (int)l;
                else if (rawCount is double d) count = (int)d;
                else if (rawCount is int i) count = i;
            }
            if (count < 0) count = 0;

            // Parse level
            string level = null;
            if (parameters != null && parameters.TryGetValue("level", out var rawLevel) && rawLevel != null)
                level = rawLevel.ToString();

            // Parse keyword
            string keyword = null;
            if (parameters != null && parameters.TryGetValue("keyword", out var rawKeyword) && rawKeyword != null)
                keyword = rawKeyword.ToString();

            // Parse beforeIndex
            int beforeIndex = -1;
            bool hasBeforeIndex = false;
            if (parameters != null && parameters.TryGetValue("beforeIndex", out var rawBefore) && rawBefore != null)
            {
                hasBeforeIndex = true;
                if (rawBefore is long bl) beforeIndex = (int)bl;
                else if (rawBefore is double bd) beforeIndex = (int)bd;
                else if (rawBefore is int bi) beforeIndex = bi;
            }

            if (hasBeforeIndex)
            {
                // Context mode — ignore level and keyword
                if (beforeIndex < 0)
                    return Task.FromResult(ToolResult.Error("beforeIndex must be non-negative"));

                string contextJson;
                // lock 内 return 是安全的：C# lock 语句在 return 前会正确释放锁
                lock (_lock)
                {
                    // 通过稳定 index 做 O(1) 偏移计算定位 buffer 位置
                    int bufferPos = FindBufferPosition(beforeIndex);
                    if (bufferPos < 0)
                        return Task.FromResult(ToolResult.Error(
                            $"beforeIndex out of range: {beforeIndex}, valid range: " +
                            (_buffer.Count > 0
                                ? $"{_buffer[0].Index}–{_buffer[_buffer.Count - 1].Index}"
                                : "empty buffer")));

                    int start = Math.Max(0, bufferPos - count + 1);
                    int end = bufferPos;

                    var sb = new StringBuilder();
                    sb.Append('[');
                    for (int pos = start; pos <= end; pos++)
                    {
                        if (pos > start) sb.Append(',');
                        SerializeEntry(sb, _buffer[pos]);
                    }
                    sb.Append(']');
                    contextJson = sb.ToString();
                }

                return Task.FromResult(ToolResult.Success(contextJson));
            }

            // Filter mode
            if (level != null && level != "Error" && level != "Warning" && level != "Log")
                return Task.FromResult(ToolResult.Error($"invalid level: {level}. Valid values: Error, Warning, Log"));

            string json;
            lock (_lock)
            {
                var matched = new List<int>();
                for (int pos = _buffer.Count - 1; pos >= 0 && matched.Count < count; pos--)
                {
                    var entry = _buffer[pos];
                    if (level != null && entry.Level != level)
                        continue;
                    if (keyword != null && entry.Message.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    matched.Add(pos);
                }
                matched.Reverse();

                var sb = new StringBuilder();
                sb.Append('[');
                for (int m = 0; m < matched.Count; m++)
                {
                    if (m > 0) sb.Append(',');
                    SerializeEntry(sb, _buffer[matched[m]]);
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
            bool isError = type == LogType.Error || type == LogType.Exception || type == LogType.Assert;
            var entry = new LogEntry
            {
                Level = ToLevel(type),
                Timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                Message = message,
                StackTrace = isError && !string.IsNullOrEmpty(stackTrace) ? stackTrace : null
            };

            lock (_lock)
            {
                if (_buffer.Count >= MaxBufferSize)
                    _buffer.RemoveAt(0);
                entry.Index = _nextIndex++;
                _buffer.Add(entry);
            }
        }

        /// <summary>
        /// 通过稳定 index 定位 buffer 中的数组位置。
        /// 由于 index 是递增的且 buffer 是连续的，可以 O(1) 计算偏移。
        /// 必须在 lock(_lock) 内调用。
        /// </summary>
        /// <returns>buffer 数组位置，若 index 不在当前 buffer 范围内则返回 -1。</returns>
        private static int FindBufferPosition(long globalIndex)
        {
            if (_buffer.Count == 0) return -1;
            long firstIndex = _buffer[0].Index;
            long lastIndex = _buffer[_buffer.Count - 1].Index;
            if (globalIndex < firstIndex || globalIndex > lastIndex) return -1;
            return (int)(globalIndex - firstIndex);
        }

        /// <summary>将单条 LogEntry 序列化为 JSON 对象并追加到 StringBuilder。</summary>
        private static void SerializeEntry(StringBuilder sb, LogEntry entry)
        {
            sb.Append("{\"level\":");
            sb.Append(MiniJson.SerializeString(entry.Level));
            sb.Append(",\"timestamp\":");
            sb.Append(MiniJson.SerializeString(entry.Timestamp));
            sb.Append(",\"message\":");
            sb.Append(MiniJson.SerializeString(entry.Message));
            if (entry.StackTrace != null)
            {
                sb.Append(",\"stackTrace\":");
                sb.Append(MiniJson.SerializeString(entry.StackTrace));
            }
            sb.Append(",\"index\":");
            sb.Append(entry.Index);
            sb.Append('}');
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
            public long Index;
            public string Level;
            public string Timestamp;
            public string Message;
            public string StackTrace;
        }

        // --- 测试辅助方法 ---

        /// <summary>清空日志缓冲区（仅供测试使用）。</summary>
        internal static void ClearBuffer()
        {
            lock (_lock)
            {
                _buffer.Clear();
                _nextIndex = 0;
            }
        }

        /// <summary>向缓冲区注入一条日志（仅供测试使用，绕过 Application 事件）。</summary>
        internal static void InjectLog(string level, string timestamp, string message, string stackTrace = null)
        {
            lock (_lock)
            {
                if (_buffer.Count >= MaxBufferSize)
                    _buffer.RemoveAt(0);
                _buffer.Add(new LogEntry
                {
                    Index = _nextIndex++,
                    Level = level,
                    Timestamp = timestamp,
                    Message = message,
                    StackTrace = stackTrace
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
