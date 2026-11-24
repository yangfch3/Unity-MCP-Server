using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// GameScreenshotTool 和 SceneScreenshotTool 测试。
    /// </summary>
    public class ScreenshotToolTests
    {
        private bool _hadGameScreenshotPref;
        private bool _previousGameScreenshotDisabled;
        private bool _hadSceneScreenshotPref;
        private bool _previousSceneScreenshotEnabled;

        [TestCase(0)]
        [TestCase(32)]
        public void EncodeResult_RepeatedCaptures_DoNotRetainTextures(int maxHeight)
        {
            // 预热编码器与渲染资源，排除首次初始化引入的对象。
            ScreenshotTool.EncodeResult(new Texture2D(64, 64), 0, maxHeight, "jpg", 75);
            var before = new HashSet<int>(Resources.FindObjectsOfTypeAll<Texture2D>().Select(t => t.GetInstanceID()));
            try
            {
                for (int i = 0; i < 10; i++)
                {
                    var shot = new Texture2D(64, 64);
                    var result = ScreenshotTool.EncodeResult(shot, 0, maxHeight, "jpg", 75);
                    Assert.IsTrue(shot == null, "Source texture must be destroyed.");
                    var decoded = LoadTexture(GetImage(result));
                    try { Assert.AreEqual(maxHeight == 0 ? 64 : maxHeight, decoded.height); }
                    finally { UnityEngine.Object.DestroyImmediate(decoded); }
                }
                var retained = Resources.FindObjectsOfTypeAll<Texture2D>()
                    .Where(t => !before.Contains(t.GetInstanceID()) && (t.width == 32 || t.width == 64)).ToArray();
                Assert.IsEmpty(retained, "Screenshot textures must be destroyed after encoding.");
            }
            finally
            {
                foreach (var texture in Resources.FindObjectsOfTypeAll<Texture2D>()
                    .Where(t => !before.Contains(t.GetInstanceID()) && (t.width == 32 || t.width == 64)))
                    UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [SetUp]
        public void SetUp()
        {
            _hadGameScreenshotPref = EditorPrefs.HasKey(GameScreenshotTool.DisablePrefKey);
            _previousGameScreenshotDisabled = EditorPrefs.GetBool(GameScreenshotTool.DisablePrefKey, false);
            _hadSceneScreenshotPref = EditorPrefs.HasKey(SceneScreenshotTool.PrefKey);
            _previousSceneScreenshotEnabled = EditorPrefs.GetBool(SceneScreenshotTool.PrefKey, false);
            EditorPrefs.SetBool(GameScreenshotTool.DisablePrefKey, false);
            EditorPrefs.SetBool(SceneScreenshotTool.PrefKey, true);
        }

        [TearDown]
        public void TearDown()
        {
            RestorePref(GameScreenshotTool.DisablePrefKey, _hadGameScreenshotPref, _previousGameScreenshotDisabled);
            RestorePref(SceneScreenshotTool.PrefKey, _hadSceneScreenshotPref, _previousSceneScreenshotEnabled);
        }

        [Test]
        public void Tools_NameAndCategory_AreCorrect()
        {
            Assert.AreEqual("debug_screenshotGame", new GameScreenshotTool().Name);
            Assert.AreEqual("debug_screenshotScene", new SceneScreenshotTool().Name);
            Assert.AreEqual("debug", new GameScreenshotTool().Category);
            Assert.AreEqual("debug", new SceneScreenshotTool().Category);
        }

        [Test]
        public void InputSchema_UsesOptimizedDefaultsWithoutViewParameter()
        {
            var gameSchema = new GameScreenshotTool().InputSchema;
            var sceneSchema = new SceneScreenshotTool().InputSchema;

            Assert.That(gameSchema, Does.Contain("\"default\":1024"));
            Assert.That(gameSchema, Does.Contain("\"default\":\"jpg\""));
            Assert.That(gameSchema, Does.Contain("\"default\":75"));
            Assert.That(gameSchema, Does.Not.Contain("\"view\""));
            Assert.That(sceneSchema, Does.Not.Contain("\"view\""));
            Assert.That(sceneSchema, Does.Contain("\"includeUI\""));
            Assert.That(sceneSchema, Does.Contain("\"default\":false"));
        }

        [Test]
        public void SceneExecute_DefaultParameters_UsesOptimizedFormatAndHeight()
        {
            var result = new SceneScreenshotTool().Execute(new Dictionary<string, object>()).Result;
            var image = GetImage(result);
            var texture = LoadTexture(image);
            try
            {
                Assert.AreEqual("image/jpeg", image.MimeType);
                Assert.LessOrEqual(texture.height, ScreenshotTool.DefaultMaxHeight);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void SceneExecute_ExplicitParameters_OverrideDefaults()
        {
            var result = new SceneScreenshotTool().Execute(new Dictionary<string, object>
            {
                { "maxHeight", 256 },
                { "format", "png" },
                { "quality", 10 }
            }).Result;
            var image = GetImage(result);
            var texture = LoadTexture(image);
            try
            {
                Assert.AreEqual("image/png", image.MimeType);
                Assert.LessOrEqual(texture.height, 256);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void SceneExecute_IncludeUI_ReturnsImageWithoutChangingSceneViewCamera()
        {
            var sceneView = EditorWindow.GetWindow<SceneView>(true, null, true);
            Assert.IsNotNull(sceneView);
            Assert.IsNotNull(sceneView.camera);

            var camera = sceneView.camera;
            var previousTarget = camera.targetTexture;
            var previousAspect = camera.aspect;
            var previousPosition = camera.transform.position;
            var previousRotation = camera.transform.rotation;

            var result = new SceneScreenshotTool().Execute(new Dictionary<string, object>
            {
                { "includeUI", true }
            }).Result;
            var image = GetImage(result);
            var texture = LoadTexture(image);
            try
            {
                Assert.AreEqual("image/jpeg", image.MimeType);
                Assert.AreSame(previousTarget, camera.targetTexture);
                Assert.AreEqual(previousAspect, camera.aspect);
                Assert.AreEqual(previousPosition, camera.transform.position);
                Assert.AreEqual(previousRotation, camera.transform.rotation);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void GameExecute_OutsidePlayMode_ReturnsExpectedError()
        {
            var result = new GameScreenshotTool().Execute(new Dictionary<string, object>()).Result;

            Assert.IsTrue(result.IsError);
            StringAssert.Contains("Play 模式", result.Content[0].Text);
        }

        [Test]
        public void AutoDiscovery_FindsBothToolsWhenEnabled()
        {
            var registry = new ToolRegistry();
            registry.AutoDiscover();

            Assert.IsNotNull(registry.Resolve(GameScreenshotTool.ToolName));
            Assert.IsNotNull(registry.Resolve(SceneScreenshotTool.ToolName));
        }

        private static ContentItem GetImage(ToolResult result)
        {
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Content);
            Assert.AreEqual(1, result.Content.Count);
            Assert.AreEqual("image", result.Content[0].Type);
            return result.Content[0];
        }

        private static Texture2D LoadTexture(ContentItem image)
        {
            var data = Convert.FromBase64String(image.Data);
            var texture = new Texture2D(2, 2);
            Assert.IsTrue(texture.LoadImage(data, false));
            return texture;
        }

        private static void RestorePref(string key, bool hadKey, bool previousValue)
        {
            if (hadKey)
                EditorPrefs.SetBool(key, previousValue);
            else
                EditorPrefs.DeleteKey(key);
        }
    }
}
