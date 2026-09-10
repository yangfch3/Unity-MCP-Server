using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：等待指定毫秒后返回。用于给 Editor 侧异步过程（协程、资源加载、
    /// 场景启动）留出推进时间，期间主线程照常走帧。
    /// </summary>
    public class DelayTool : IMcpTool
    {
        public string Name => "util_delay";
        public string Category => "util";
        public string Description => "等待指定毫秒后返回（期间 Editor 照常走帧）";
        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{\"ms\":{\"type\":\"integer\",\"description\":\"等待毫秒数，范围 100-120000，默认 1000\",\"default\":1000}}}";

        private const int DefaultMs = 1000;
        private const int MinMs = 100;
        private const int MaxMs = 120000;

        public async Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            int ms = DefaultMs;
            if (parameters != null && parameters.TryGetValue("ms", out var raw))
            {
                if (raw is long l) ms = (int)l;
                else if (raw is double d) ms = (int)d;
                else if (raw is int i) ms = i;
            }
            if (ms < MinMs) ms = MinMs;
            if (ms > MaxMs) ms = MaxMs;

            var watch = System.Diagnostics.Stopwatch.StartNew();
            await Task.Delay(ms);

            var sb = new StringBuilder();
            sb.Append("{\"waitedMs\":");
            sb.Append((int)watch.ElapsedMilliseconds);
            sb.Append(",\"playMode\":");
            sb.Append(MiniJson.SerializeString(PlayModeStatus()));
            sb.Append('}');
            return ToolResult.Success(sb.ToString());
        }

        /// <summary>返回等待结束时的 PlayMode 状态，便于判断期间是否发生状态跃迁。</summary>
        private static string PlayModeStatus()
        {
            if (!EditorApplication.isPlaying) return "Stopped";
            return EditorApplication.isPaused ? "Paused" : "Playing";
        }
    }
}
