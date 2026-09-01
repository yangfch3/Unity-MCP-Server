using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：截取 Game 视图截图返回图片。
    /// </summary>
    public class GameScreenshotTool : IMcpTool, IConditionalTool
    {
        public const string PrefKey = "McpServer_EnableGameScreenshot";
        public const string ToolName = "debug_screenshotGame";

        public string Name => ToolName;
        public string Category => "debug";
        public string Description => "截图 PlayMode 下 Game 视图";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"maxWidth\":{\"type\":\"integer\",\"description\":\"最大宽度，超出则等比缩小，0=不限制\",\"default\":0},\"maxHeight\":{\"type\":\"integer\",\"description\":\"最大高度，超出则等比缩小，0=不限制\",\"default\":1024},\"format\":{\"type\":\"string\",\"enum\":[\"png\",\"jpg\"],\"description\":\"图片格式\",\"default\":\"jpg\"},\"quality\":{\"type\":\"integer\",\"description\":\"jpg 质量 1-100\",\"default\":75}}}";

        /// <summary>是否启用 Game 截图工具。</summary>
        public bool IsEnabled => EditorPrefs.GetBool(PrefKey, false);

        /// <summary>执行 Game 截图。</summary>
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            int maxWidth = ScreenshotTool.GetInt(parameters, "maxWidth", 0);
            int maxHeight = ScreenshotTool.GetInt(parameters, "maxHeight", ScreenshotTool.DefaultMaxHeight);
            string format = ScreenshotTool.GetString(parameters, "format", ScreenshotTool.DefaultFormat).ToLowerInvariant();
            int quality = Mathf.Clamp(ScreenshotTool.GetInt(parameters, "quality", ScreenshotTool.DefaultQuality), 1, 100);
            return ScreenshotTool.CaptureGameView(maxWidth, maxHeight, format, quality);
        }
    }
}
