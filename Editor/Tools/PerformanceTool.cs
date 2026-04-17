using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取 FPS、DrawCall、内存占用等关键性能指标。
    /// </summary>
    public class PerformanceTool : IMcpTool
    {
        public string Name => "debug_getPerformanceStats";
        public string Category => "debug";
        public string Description => "获取 FPS、DrawCall、内存占用等关键性能指标";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            float fps = Time.unscaledDeltaTime > 0f
                ? 1.0f / Time.unscaledDeltaTime
                : -1f;

            int drawCalls;
            try
            {
                drawCalls = UnityEditor.UnityStats.drawCalls;
            }
            catch
            {
                drawCalls = -1;
            }

            double memoryMB;
            try
            {
                memoryMB = Profiler.GetTotalAllocatedMemoryLong() / (1024.0 * 1024.0);
            }
            catch
            {
                memoryMB = -1;
            }

            var sb = new StringBuilder();
            sb.Append("{\"fps\":");
            sb.Append(fps.ToString("F1", CultureInfo.InvariantCulture));
            sb.Append(",\"drawCalls\":");
            sb.Append(drawCalls.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"memoryUsedMB\":");
            sb.Append(memoryMB.ToString("F1", CultureInfo.InvariantCulture));
            sb.Append('}');

            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }
    }
}
