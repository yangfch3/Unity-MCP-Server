using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：截取 Scene 视图截图返回图片。
    /// 默认只返回场景相机画面，不合成 UI Canvas。
    /// </summary>
    public class SceneScreenshotTool : IMcpTool, IConditionalTool
    {
        public const string PrefKey = "McpServer_EnableSceneScreenshot";
        public const string ToolName = "debug_screenshotScene";

        public string Name => ToolName;
        public string Category => "debug";
        public string Description => "截取 Scene 视图截图返回图片，2D 项目需 UI 时指定 includeUI=true";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"maxWidth\":{\"type\":\"integer\",\"description\":\"最大宽度，超出则等比缩小，0=不限制\",\"default\":0},\"maxHeight\":{\"type\":\"integer\",\"description\":\"最大高度，超出则等比缩小，0=不限制\",\"default\":1024},\"format\":{\"type\":\"string\",\"enum\":[\"png\",\"jpg\"],\"description\":\"图片格式\",\"default\":\"jpg\"},\"quality\":{\"type\":\"integer\",\"description\":\"jpg 质量 1-100\",\"default\":75},\"includeUI\":{\"type\":\"boolean\",\"description\":\"是否合成 UI Canvas，默认 false\",\"default\":false}}}";

        /// <summary>是否启用 Scene 截图工具。</summary>
        public bool IsEnabled => EditorPrefs.GetBool(PrefKey, false);

        /// <summary>执行 Scene 截图。</summary>
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            int maxWidth = ScreenshotTool.GetInt(parameters, "maxWidth", 0);
            int maxHeight = ScreenshotTool.GetInt(parameters, "maxHeight", ScreenshotTool.DefaultMaxHeight);
            string format = ScreenshotTool.GetString(parameters, "format", ScreenshotTool.DefaultFormat).ToLowerInvariant();
            int quality = Mathf.Clamp(ScreenshotTool.GetInt(parameters, "quality", ScreenshotTool.DefaultQuality), 1, 100);
            bool includeUI = ScreenshotTool.GetBool(parameters, "includeUI", false);
            return ScreenshotTool.CaptureSceneView(maxWidth, maxHeight, format, quality, includeUI);
        }
    }
}
