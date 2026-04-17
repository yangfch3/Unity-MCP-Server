using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：截取 Game/Scene 视图截图返回 base64 PNG。
    /// 优先使用 InternalEditorUtility.ReadScreenPixel（非公开 API），
    /// 捕获 MissingMethodException 后回退到 Texture2D.ReadPixels 方案。
    /// </summary>
    public class ScreenshotTool : IMcpTool
    {
        public string Name => "debug_screenshot";
        public string Category => "debug";
        public string Description => "截取 Game/Scene 视图截图返回 base64";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"view\":{\"type\":\"string\",\"enum\":[\"game\",\"scene\"],\"description\":\"视图类型\",\"default\":\"game\"}}}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            string view = "game";
            if (parameters != null && parameters.TryGetValue("view", out var raw) && raw is string v)
                view = v.ToLowerInvariant();

            EditorWindow window;
            if (view == "scene")
            {
                window = EditorWindow.GetWindow<SceneView>(false, null, false);
            }
            else
            {
                // GameView 是内部类型，通过反射获取
                var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameViewType == null)
                    return Task.FromResult(ToolResult.Error("无法获取 GameView 类型"));
                window = EditorWindow.GetWindow(gameViewType, false, null, false);
            }

            if (window == null)
                return Task.FromResult(ToolResult.Error($"视图未打开: {view}"));

            window.Repaint();

            Texture2D screenshot = null;
            try
            {
                screenshot = CaptureWindow(window);
                byte[] pngBytes = screenshot.EncodeToPNG();
                string base64 = Convert.ToBase64String(pngBytes);
                return Task.FromResult(ToolResult.SuccessImage(base64, "image/png"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Error($"截图失败: {ex.Message}"));
            }
            finally
            {
                if (screenshot != null)
                    UnityEngine.Object.DestroyImmediate(screenshot);
            }
        }

        private static Texture2D CaptureWindow(EditorWindow window)
        {
            var pos = window.position;
            int width = (int)pos.width;
            int height = (int)pos.height;

            // 优先尝试 InternalEditorUtility.ReadScreenPixel（非公开 API）
            try
            {
                Color[] pixels = InternalEditorUtility.ReadScreenPixel(
                    new Vector2(pos.x, pos.y), width, height);
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.SetPixels(pixels);
                tex.Apply();
                return tex;
            }
            catch (MissingMethodException)
            {
                // 回退方案：使用 RenderTexture + ReadPixels
            }

            // 回退方案
            var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            try
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                return tex;
            }
            finally
            {
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
