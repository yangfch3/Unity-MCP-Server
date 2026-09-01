using System;
using System.Collections.Generic;
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
        private bool _previousGameScreenshotEnabled;
        private bool _hadSceneScreenshotPref;
        private bool _previousSceneScreenshotEnabled;

        [SetUp]
        public void SetUp()
        {
            _hadGameScreenshotPref = EditorPrefs.HasKey(GameScreenshotTool.PrefKey);
            _previousGameScreenshotEnabled = EditorPrefs.GetBool(GameScreenshotTool.PrefKey, false);
            _hadSceneScreenshotPref = EditorPrefs.HasKey(SceneScreenshotTool.PrefKey);
            _previousSceneScreenshotEnabled = EditorPrefs.GetBool(SceneScreenshotTool.PrefKey, false);
            EditorPrefs.SetBool(GameScreenshotTool.PrefKey, true);
            EditorPrefs.SetBool(SceneScreenshotTool.PrefKey, true);
        }

        [TearDown]
        public void TearDown()
        {
            RestorePref(GameScreenshotTool.PrefKey, _hadGameScreenshotPref, _previousGameScreenshotEnabled);
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
