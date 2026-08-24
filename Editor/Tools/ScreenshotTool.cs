using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：截取 Game/Scene 视图截图返回 base64 图片。
    /// 离屏渲染实现，不受其他应用窗口或 Unity 标签页遮挡影响。
    /// - game: 仅 Play 模式可用（编辑模式下 Game 视图无持续渲染，截出黑帧）。
    ///   通过 GameView.m_TargetTexture 将游戏画面（含 Overlay UI）渲染到 RenderTexture，
    ///   分辨率取 Game 视图目标分辨率（targetSize），带黑帧检测与重试。
    /// - scene: 将 SceneView 相机渲染到 RenderTexture，并合成 Overlay / ScreenSpaceCamera
    ///   Canvas（复现 SceneView 的 UI 可视化），为纯相机画面（不含 gizmo/网格线）。
    /// 默认不注册，需在 ConfigPanel 的 Experimental 区开启（IConditionalTool）。
    /// </summary>
    public class ScreenshotTool : IMcpTool, IConditionalTool
    {
        public const string PrefKey = "McpServer_EnableScreenshot";
        public const string ToolName = "debug_screenshot";

        public string Name => ToolName;
        public string Category => "debug";
        public string Description => "截取 Game/Scene 视图截图返回 base64 图片（离屏渲染，防遮挡）。game 仅支持 Play 模式；scene 为相机画面（无 gizmo/网格线，含 UI Canvas 合成）";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"view\":{\"type\":\"string\",\"enum\":[\"game\",\"scene\"],\"description\":\"视图类型\",\"default\":\"game\"},\"maxWidth\":{\"type\":\"integer\",\"description\":\"最大宽度，超出则等比缩小，0=不限制\",\"default\":0},\"maxHeight\":{\"type\":\"integer\",\"description\":\"最大高度，超出则等比缩小，0=不限制\",\"default\":0},\"format\":{\"type\":\"string\",\"enum\":[\"png\",\"jpg\"],\"description\":\"图片格式\",\"default\":\"png\"},\"quality\":{\"type\":\"integer\",\"description\":\"jpg 质量 1-100\",\"default\":75}}}";

        /// <summary>是否启用截图工具（EditorPrefs 开关，默认关闭，ConfigPanel Experimental 区控制）。</summary>
        public bool IsEnabled => EditorPrefs.GetBool(PrefKey, false);

        private const BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Game 视图渲染进 RenderTexture 所需的等待帧数</summary>
        private const int WaitForTicks = 4;

        /// <summary>等待重绘的最大帧数，超过则报错（防止编辑器挂起时悬挂）</summary>
        private const int MaxTicks = 120;

        /// <summary>黑帧时的额外重试次数</summary>
        private const int MaxBlackRetries = 2;

        /// <summary>判定黑帧的单通道亮度阈值</summary>
        private const float BlackThreshold = 0.02f;

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            string view = GetString(parameters, "view", "game").ToLowerInvariant();
            int maxWidth = GetInt(parameters, "maxWidth", 0);
            int maxHeight = GetInt(parameters, "maxHeight", 0);
            string format = GetString(parameters, "format", "png").ToLowerInvariant();
            int quality = Mathf.Clamp(GetInt(parameters, "quality", 75), 1, 100);

            if (view == "scene")
                return CaptureSceneView(maxWidth, maxHeight, format, quality);
            return CaptureGameView(maxWidth, maxHeight, format, quality);
        }

        /// <summary>
        /// Game 视图：仅 Play 模式可用。将 GameView.m_TargetTexture 指向临时 RenderTexture，
        /// 等待数帧渲染完成后读取（带黑帧检测重试），再恢复原值。
        /// </summary>
        private static Task<ToolResult> CaptureGameView(int maxWidth, int maxHeight, string format, int quality)
        {
            if (!EditorApplication.isPlaying)
                return Task.FromResult(ToolResult.Error("Game 截图仅支持 Play 模式（当前为编辑模式，Game 视图无持续渲染）。请先进入 Play 模式，或改用 view=scene"));

            var gameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null)
                return Task.FromResult(ToolResult.Error("无法获取 GameView 类型"));

            var window = EditorWindow.GetWindow(gameViewType, true, null, true);
            if (window == null)
                return Task.FromResult(ToolResult.Error("视图未打开: game"));

            FieldInfo mttField = FindField(gameViewType, "m_TargetTexture");
            if (mttField == null)
                return Task.FromResult(ToolResult.Error("当前 Unity 版本不支持 GameView 离屏捕获"));

            Vector2 size = GetGameViewSize(gameViewType, window);
            int width = Mathf.Max(1, (int)size.x);
            int height = Mathf.Max(1, (int)size.y);

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = (RenderTexture)mttField.GetValue(window);
            var tcs = new TaskCompletionSource<ToolResult>();
            int tick = 0;
            int blackRetries = 0;
            bool finished = false;

            EditorApplication.CallbackFunction callback = null;
            callback = () =>
            {
                if (finished)
                {
                    EditorApplication.update -= callback;
                    return;
                }

                // 捕获途中退出 Play 模式时立即中止，避免与场景拆除竞态
                if (!EditorApplication.isPlaying)
                {
                    finished = true;
                    EditorApplication.update -= callback;
                    RestoreGameView(window, mttField, prevTarget, rt);
                    tcs.TrySetResult(ToolResult.Error("截图失败: 捕获期间退出了 Play 模式，请重新调用"));
                    return;
                }

                tick++;
                if (tick > MaxTicks)
                {
                    finished = true;
                    EditorApplication.update -= callback;
                    RestoreGameView(window, mttField, prevTarget, rt);
                    tcs.TrySetResult(ToolResult.Error("截图失败: 等待 Game 视图渲染超时"));
                    return;
                }

                if (tick == 1)
                {
                    mttField.SetValue(window, rt);
                    window.Repaint();
                    return;
                }
                if (tick < WaitForTicks)
                    return;

                try
                {
                    // Game 视图渲染到 RenderTexture 时为屏幕方向（顶部在前），读取后需垂直翻转
                    Texture2D shot = ReadRenderTexture(rt, width, height, flipY: true);
                    if (IsUniformBlack(shot) && blackRetries < MaxBlackRetries)
                    {
                        blackRetries++;
                        UnityEngine.Object.DestroyImmediate(shot);
                        window.Repaint();
                        return;
                    }

                    finished = true;
                    EditorApplication.update -= callback;
                    RestoreGameView(window, mttField, prevTarget, rt);

                    if (IsUniformBlack(shot))
                    {
                        // 合法的纯黑画面（暗场景/加载中）与渲染失败无法区分，
                        // 返回图片并附加警告文本，而非直接报错
                        var warning = new ContentItem("text",
                            "警告: Game 视图画面接近纯黑。可能是渲染未完成（Unity 窗口挂起），也可能是游戏本身处于黑屏状态（加载/暗场景），请结合上下文判断");
                        var image = new ContentItem("image", Convert.ToBase64String(shot.EncodeToPNG()), "image/png");
                        UnityEngine.Object.DestroyImmediate(shot);
                        tcs.TrySetResult(ToolResult.Success(new List<ContentItem> { warning, image }));
                        return;
                    }

                    tcs.TrySetResult(EncodeResult(shot, maxWidth, maxHeight, format, quality));
                }
                catch (Exception ex)
                {
                    finished = true;
                    EditorApplication.update -= callback;
                    RestoreGameView(window, mttField, prevTarget, rt);
                    tcs.TrySetResult(ToolResult.Error($"截图失败: {ex.Message}"));
                }
            };
            EditorApplication.update += callback;
            return tcs.Task;
        }

        /// <summary>
        /// Scene 视图：将 SceneView 相机渲染到临时 RenderTexture（同步完成），
        /// 并把 Overlay / ScreenSpaceCamera 根 Canvas 临时切换为挂到场景相机合成进同一张图。
        /// 纯相机画面，不含 gizmo/网格线。
        /// </summary>
        private static Task<ToolResult> CaptureSceneView(int maxWidth, int maxHeight, string format, int quality)
        {
            var window = EditorWindow.GetWindow<SceneView>(true, null, true);
            if (window == null)
                return Task.FromResult(ToolResult.Error("视图未打开: scene"));

            Camera camera = window.camera;
            if (camera == null)
                return Task.FromResult(ToolResult.Error("SceneView 相机不可用"));

            int width = Mathf.Max(1, (int)window.position.width);
            int height = Mathf.Max(1, (int)window.position.height);

            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = camera.targetTexture;
            var prevAspect = camera.aspect;
            try
            {
                camera.targetTexture = rt;
                camera.aspect = (float)width / height;
                camera.Render();

                RenderUiCanvases(camera);

                // 相机渲染遵循 GL 底部原点约定，ReadPixels 结果方向正确，无需翻转
                Texture2D shot = ReadRenderTexture(rt, width, height, flipY: false);
                return Task.FromResult(EncodeResult(shot, maxWidth, maxHeight, format, quality));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Error($"截图失败: {ex.Message}"));
            }
            finally
            {
                camera.targetTexture = prevTarget;
                camera.aspect = prevAspect;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// 将场景中激活的 Screen Space Overlay / Screen Space Camera 根 Canvas
        /// 临时切换为挂到指定相机的 ScreenSpaceCamera 模式并渲染（仅清深度，叠加在已有画面上），
        /// 复现 SceneView 对 UI Canvas 的编辑器可视化，完成后按原值精确还原。
        /// </summary>
        private static void RenderUiCanvases(Camera camera)
        {
            var canvases = new List<Canvas>(Resources.FindObjectsOfTypeAll<Canvas>());
            canvases.RemoveAll(c => c == null || !c.enabled || !c.isRootCanvas
                || !c.gameObject.activeInHierarchy
                || !c.gameObject.scene.IsValid()
                || c.renderMode == RenderMode.WorldSpace);
            canvases.Sort((a, b) => a.sortingOrder.CompareTo(b.sortingOrder));
            if (canvases.Count == 0)
                return;

            float planeDistance = Mathf.Clamp(
                10f, camera.nearClipPlane + 0.1f, Mathf.Max(camera.nearClipPlane + 0.2f, camera.farClipPlane * 0.5f));
            var switched = new List<CanvasState>();
            var prevClear = camera.clearFlags;
            try
            {
                foreach (var canvas in canvases)
                {
                    switched.Add(new CanvasState(canvas));
                    canvas.worldCamera = camera;
                    canvas.planeDistance = planeDistance;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;

                    camera.clearFlags = CameraClearFlags.Depth;
                    camera.Render();
                }
            }
            finally
            {
                for (int i = switched.Count - 1; i >= 0; i--)
                    switched[i].Restore();
                camera.clearFlags = prevClear;
            }
        }

        private readonly struct CanvasState
        {
            private readonly Canvas _canvas;
            private readonly RenderMode _renderMode;
            private readonly Camera _worldCamera;
            private readonly float _planeDistance;

            public CanvasState(Canvas canvas)
            {
                _canvas = canvas;
                _renderMode = canvas.renderMode;
                _worldCamera = canvas.worldCamera;
                _planeDistance = canvas.planeDistance;
            }

            public void Restore()
            {
                _canvas.worldCamera = _worldCamera;
                _canvas.planeDistance = _planeDistance;
                _canvas.renderMode = _renderMode;
            }
        }

        /// <summary>读取 RenderTexture 为 Texture2D。flipY 用于屏幕方向的渲染结果。</summary>
        private static Texture2D ReadRenderTexture(RenderTexture rt, int width, int height, bool flipY)
        {
            RenderTexture source = rt;
            RenderTexture temp = null;
            if (flipY)
            {
                temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(rt, temp, new Vector2(1f, -1f), new Vector2(0f, 1f));
                source = temp;
            }

            var prevActive = RenderTexture.active;
            RenderTexture.active = source;
            try
            {
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                return tex;
            }
            finally
            {
                RenderTexture.active = prevActive;
                if (temp != null)
                    RenderTexture.ReleaseTemporary(temp);
            }
        }

        /// <summary>采样判断画面是否接近纯黑（渲染未发生时 RenderTexture 内容为空）。</summary>
        private static bool IsUniformBlack(Texture2D tex)
        {
            var pixels = tex.GetPixels();
            int stride = Mathf.Max(1, pixels.Length / 4096);
            for (int i = 0; i < pixels.Length; i += stride)
            {
                if (pixels[i].r > BlackThreshold || pixels[i].g > BlackThreshold || pixels[i].b > BlackThreshold)
                    return false;
            }
            return true;
        }

        /// <summary>按需等比缩小（GPU Blit），然后编码为 base64 图片并释放纹理。</summary>
        private static ToolResult EncodeResult(Texture2D shot, int maxWidth, int maxHeight, string format, int quality)
        {
            try
            {
                Texture2D output = DownscaleIfNeeded(shot, maxWidth, maxHeight);
                byte[] data;
                string mimeType;
                if (format == "jpg")
                {
                    data = output.EncodeToJPG(quality);
                    mimeType = "image/jpeg";
                }
                else
                {
                    data = output.EncodeToPNG();
                    mimeType = "image/png";
                }
                return ToolResult.SuccessImage(Convert.ToBase64String(data), mimeType);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(shot);
            }
        }

        /// <summary>超出 maxWidth/maxHeight 时通过 GPU Blit 等比缩小，返回新纹理；无需缩小时返回原图。</summary>
        private static Texture2D DownscaleIfNeeded(Texture2D source, int maxWidth, int maxHeight)
        {
            float scaleX = maxWidth > 0 && source.width > maxWidth ? (float)maxWidth / source.width : 1f;
            float scaleY = maxHeight > 0 && source.height > maxHeight ? (float)maxHeight / source.height : 1f;
            float scale = Mathf.Min(scaleX, scaleY);
            if (scale >= 1f)
                return source;

            int newWidth = Mathf.Max(1, Mathf.RoundToInt(source.width * scale));
            int newHeight = Mathf.Max(1, Mathf.RoundToInt(source.height * scale));

            var rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
            var prevActive = RenderTexture.active;
            RenderTexture.active = rt;
            try
            {
                Graphics.Blit(source, rt);
                var result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                result.Apply();
                return result;
            }
            finally
            {
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        private static void RestoreGameView(EditorWindow window, FieldInfo mttField, RenderTexture prevTarget, RenderTexture rt)
        {
            try
            {
                mttField.SetValue(window, prevTarget);
                window.Repaint();
            }
            catch
            {
                // 恢复失败不影响返回结果
            }
            RenderTexture.ReleaseTemporary(rt);
        }

        /// <summary>解析 Game 视图目标分辨率：优先 targetSize（宽高比预设的虚拟分辨率），回退窗口像素尺寸。</summary>
        private static Vector2 GetGameViewSize(Type gameViewType, EditorWindow window)
        {
            try
            {
                var prop = gameViewType.GetProperty("targetSize", AllFlags);
                if (prop != null)
                {
                    var size = (Vector2)prop.GetValue(window);
                    if (size.x >= 1f && size.y >= 1f)
                        return size;
                }
            }
            catch
            {
                // 回退到下一方案
            }

            try
            {
                var field = FindField(gameViewType, "m_LastWindowPixelSize");
                if (field != null)
                {
                    var size = (Vector2)field.GetValue(window);
                    if (size.x >= 1f && size.y >= 1f)
                        return size;
                }
            }
            catch
            {
                // 回退到窗口尺寸估算
            }

            return new Vector2(window.position.width, Mathf.Max(1f, window.position.height - 21f));
        }

        /// <summary>沿类型继承链查找字段（含基类私有字段）。</summary>
        private static FieldInfo FindField(Type type, string name)
        {
            while (type != null)
            {
                var field = type.GetField(name, AllFlags);
                if (field != null)
                    return field;
                type = type.BaseType;
            }
            return null;
        }

        private static string GetString(Dictionary<string, object> parameters, string key, string defaultValue)
        {
            if (parameters != null && parameters.TryGetValue(key, out var value) && value is string s)
                return s;
            return defaultValue;
        }

        private static int GetInt(Dictionary<string, object> parameters, string key, int defaultValue)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
                return defaultValue;
            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
