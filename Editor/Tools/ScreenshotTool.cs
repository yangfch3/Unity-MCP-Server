using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// 截图工具共享实现。具体的 Game/Scene MCP 工具分别由独立类暴露。
    /// </summary>
    internal static class ScreenshotTool
    {
        internal const int DefaultMaxHeight = 1024;
        internal const string DefaultFormat = "jpg";
        internal const int DefaultQuality = 75;

        private const BindingFlags AllFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private const int WaitForTicks = 4;
        private const int MaxTicks = 120;
        private const int MaxBlackRetries = 2;
        private const float BlackThreshold = 0.02f;

        /// <summary>捕获 Game 视图。</summary>
        internal static Task<ToolResult> CaptureGameView(int maxWidth, int maxHeight, string format, int quality)
        {
            if (!EditorApplication.isPlaying)
                return Task.FromResult(ToolResult.Error("Game 截图仅支持 Play 模式（当前为编辑模式）。请先进入 Play 模式"));

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

            var prevTarget = (RenderTexture)mttField.GetValue(window);
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var tcs = new TaskCompletionSource<ToolResult>();
            int tick = 0;
            int blackRetries = 0;
            bool finished = false;

            EditorApplication.CallbackFunction callback = null;
            AssemblyReloadEvents.AssemblyReloadCallback onReload = null;
            Action<PlayModeStateChange> onPlayModeChanged = null;
            Action<ToolResult> finish = result =>
            {
                if (finished) return;
                finished = true;
                EditorApplication.update -= callback;
                AssemblyReloadEvents.beforeAssemblyReload -= onReload;
                EditorApplication.playModeStateChanged -= onPlayModeChanged;
                RestoreGameView(window, mttField, prevTarget, rt);
                tcs.TrySetResult(result);
            };
            onReload = () => finish(ToolResult.Error("截图失败: 捕获期间发生程序集重载，请重新调用"));
            onPlayModeChanged = state =>
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                    finish(ToolResult.Error("截图失败: 捕获期间退出了 Play 模式，请重新调用"));
            };
            callback = () =>
            {
                if (finished) return;
                Texture2D shot = null;
                try
                {
                    if (!EditorApplication.isPlaying)
                    {
                        finish(ToolResult.Error("截图失败: 捕获期间退出了 Play 模式，请重新调用"));
                        return;
                    }
                    if (++tick > MaxTicks)
                    {
                        finish(ToolResult.Error("截图失败: 等待 Game 视图渲染超时"));
                        return;
                    }
                    if (tick == 1)
                    {
                        mttField.SetValue(window, rt);
                        window.Repaint();
                        return;
                    }
                    if (tick < WaitForTicks) return;

                    shot = ReadRenderTexture(rt, width, height, true);
                    bool isBlack = IsUniformBlack(shot);
                    if (isBlack && blackRetries < MaxBlackRetries)
                    {
                        blackRetries++;
                        window.Repaint();
                        return;
                    }

                    if (isBlack)
                    {
                        var warning = new ContentItem("text",
                            "警告: Game 视图画面接近纯黑。可能是渲染未完成，也可能是游戏本身处于黑屏状态");
                        var image = new ContentItem("image", Convert.ToBase64String(shot.EncodeToPNG()), "image/png");
                        finish(ToolResult.Success(new List<ContentItem> { warning, image }));
                        return;
                    }

                    var ownedShot = shot;
                    shot = null;
                    finish(EncodeResult(ownedShot, maxWidth, maxHeight, format, quality));
                }
                catch (Exception ex)
                {
                    finish(ToolResult.Error($"截图失败: {ex.Message}"));
                }
                finally
                {
                    if (shot != null) UnityEngine.Object.DestroyImmediate(shot);
                }
            };
            AssemblyReloadEvents.beforeAssemblyReload += onReload;
            EditorApplication.playModeStateChanged += onPlayModeChanged;
            EditorApplication.update += callback;
            return tcs.Task;
        }

        /// <summary>捕获 Scene 视图。默认只渲染场景；includeUI 为 true 时使用临时 Canvas 合成 UI。</summary>
        internal static Task<ToolResult> CaptureSceneView(int maxWidth, int maxHeight, string format, int quality, bool includeUI)
        {
            var window = EditorWindow.GetWindow<SceneView>(true, null, true);
            if (window == null)
                return Task.FromResult(ToolResult.Error("视图未打开: scene"));

            Camera sourceCamera = window.camera;
            if (sourceCamera == null)
                return Task.FromResult(ToolResult.Error("SceneView 相机不可用"));

            int width = Mathf.Max(1, (int)window.position.width);
            int height = Mathf.Max(1, (int)window.position.height);
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            GameObject cameraObject = null;
            try
            {
                cameraObject = new GameObject("McpSceneScreenshotCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                var captureCamera = cameraObject.AddComponent<Camera>();
                captureCamera.CopyFrom(sourceCamera);
                cameraObject.transform.SetPositionAndRotation(
                    sourceCamera.transform.position,
                    sourceCamera.transform.rotation);
                captureCamera.enabled = false;
                captureCamera.targetTexture = rt;
                captureCamera.aspect = (float)width / height;
                captureCamera.Render();

                if (includeUI)
                    RenderUiCanvases(captureCamera);

                Texture2D shot = ReadRenderTexture(rt, width, height, false);
                return Task.FromResult(EncodeResult(shot, maxWidth, maxHeight, format, quality));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Error($"截图失败: {ex.Message}"));
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
                if (cameraObject != null)
                    UnityEngine.Object.DestroyImmediate(cameraObject);
            }
        }

        /// <summary>在临时 Canvas 副本上合成 UI，不修改场景中的真实 Canvas。</summary>
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
            var clones = new List<GameObject>();
            var prevClear = camera.clearFlags;
            try
            {
                foreach (var sourceCanvas in canvases)
                {
                    var clone = UnityEngine.Object.Instantiate(sourceCanvas.gameObject);
                    clone.name = sourceCanvas.gameObject.name + " (MCP Screenshot)";
                    clone.hideFlags = HideFlags.HideAndDontSave;
                    clones.Add(clone);

                    var canvas = clone.GetComponent<Canvas>();
                    if (canvas == null)
                        continue;

                    canvas.worldCamera = camera;
                    canvas.planeDistance = planeDistance;
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    Canvas.ForceUpdateCanvases();

                    camera.clearFlags = CameraClearFlags.Depth;
                    camera.Render();
                }
            }
            finally
            {
                camera.clearFlags = prevClear;
                for (int i = clones.Count - 1; i >= 0; i--)
                {
                    if (clones[i] != null)
                        UnityEngine.Object.DestroyImmediate(clones[i]);
                }
            }
        }

        private static Texture2D ReadRenderTexture(RenderTexture rt, int width, int height, bool flipY)
        {
            RenderTexture source = rt;
            RenderTexture temp = null;
            Texture2D tex = null;
            var prevActive = RenderTexture.active;
            try
            {
                if (flipY)
                {
                    temp = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
                    Graphics.Blit(rt, temp, new Vector2(1f, -1f), new Vector2(0f, 1f));
                    source = temp;
                }
                RenderTexture.active = source;
                tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                return tex;
            }
            catch
            {
                if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
                throw;
            }
            finally
            {
                RenderTexture.active = prevActive;
                if (temp != null)
                    RenderTexture.ReleaseTemporary(temp);
            }
        }

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

        internal static ToolResult EncodeResult(Texture2D shot, int maxWidth, int maxHeight, string format, int quality)
        {
            Texture2D output = null;
            try
            {
                output = DownscaleIfNeeded(shot, maxWidth, maxHeight);
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
                if (output != null && output != shot)
                    UnityEngine.Object.DestroyImmediate(output);
                UnityEngine.Object.DestroyImmediate(shot);
            }
        }

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
            try
            {
                Graphics.Blit(source, rt);
                return ReadRenderTexture(rt, newWidth, newHeight, false);
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

        internal static string GetString(Dictionary<string, object> parameters, string key, string defaultValue)
        {
            if (parameters != null && parameters.TryGetValue(key, out var value) && value is string s)
                return s;
            return defaultValue;
        }

        internal static int GetInt(Dictionary<string, object> parameters, string key, int defaultValue)
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

        internal static bool GetBool(Dictionary<string, object> parameters, string key, bool defaultValue)
        {
            if (parameters == null || !parameters.TryGetValue(key, out var value) || value == null)
                return defaultValue;
            if (value is bool b)
                return b;
            try
            {
                return Convert.ToBoolean(value);
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
