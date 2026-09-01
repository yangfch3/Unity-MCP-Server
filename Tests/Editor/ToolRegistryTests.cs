using System.Linq;
using NUnit.Framework;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;
using UnityEditor;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ToolRegistry 注册完整性与分组正确性测试。
    /// </summary>
    public class ToolRegistryTests
    {
        private ToolRegistry _registry;
        private bool _prevGameScreenshotEnabled;
        private bool _hadGameScreenshotPref;
        private bool _prevSceneScreenshotEnabled;
        private bool _hadSceneScreenshotPref;

        [SetUp]
        public void SetUp()
        {
            _hadGameScreenshotPref = EditorPrefs.HasKey(GameScreenshotTool.PrefKey);
            _prevGameScreenshotEnabled = EditorPrefs.GetBool(GameScreenshotTool.PrefKey, false);
            _hadSceneScreenshotPref = EditorPrefs.HasKey(SceneScreenshotTool.PrefKey);
            _prevSceneScreenshotEnabled = EditorPrefs.GetBool(SceneScreenshotTool.PrefKey, false);
            EditorPrefs.SetBool(GameScreenshotTool.PrefKey, true);
            EditorPrefs.SetBool(SceneScreenshotTool.PrefKey, true);

            _registry = new ToolRegistry();
            _registry.AutoDiscover();
        }

        [TearDown]
        public void TearDown()
        {
            RestorePref(GameScreenshotTool.PrefKey, _hadGameScreenshotPref, _prevGameScreenshotEnabled);
            RestorePref(SceneScreenshotTool.PrefKey, _hadSceneScreenshotPref, _prevSceneScreenshotEnabled);
        }

        [Test]
        public void AutoDiscover_FindsAllExpectedTools()
        {
            var all = _registry.ListAll();
            var names = all.Select(t => t.Name).ToList();

            Assert.Contains("console_getLogs", names);
            Assert.Contains("menu_execute", names);
            Assert.Contains("playmode_control", names);
            Assert.Contains("debug_getStackTrace", names);
            Assert.Contains("debug_getPerformanceStats", names);
            Assert.Contains("debug_screenshotGame", names);
            Assert.Contains("debug_screenshotScene", names);
            Assert.Contains("editor_getSelection", names);
            Assert.Contains("editor_getHierarchy", names);
            Assert.Contains("editor_getProjectStructure", names);
            Assert.Contains("editor_getProjectPath", names);
            Assert.Contains("editor_getInspector", names);
            Assert.Contains("build_compile", names);
            Assert.Contains("build_getCompileErrors", names);
            Assert.Contains("build_runTests", names);
            Assert.Contains("asset_deleteFolder", names);
            Assert.Contains("console_clearLogs", names);
            Assert.Contains("editor_selectGameObject", names);
            Assert.Contains("editor_findGameObjects", names);
            Assert.Contains("editor_addGameObject", names);
            Assert.Contains("editor_addComponent", names);
            Assert.Contains("editor_deleteGameObject", names);
            Assert.Contains("editor_removeComponent", names);
            Assert.Contains("editor_reparentGameObject", names);
            Assert.Contains("editor_setActive", names);
            Assert.Contains("editor_setComponentEnabled", names);
            Assert.Contains("editor_setTransform", names);
            Assert.Contains("editor_setField", names);

#if !UNITY_6000_OR_NEWER
            Assert.IsNotNull(_registry.Resolve("code_executeImmediate"), "code_executeImmediate");
            Assert.GreaterOrEqual(all.Count, 28);
#else
            Assert.GreaterOrEqual(all.Count, 27);
#endif
        }

        [Test]
        public void AllTools_FollowNamingConvention()
        {
            var all = _registry.ListAll();
            foreach (var tool in all)
            {
                Assert.IsTrue(tool.Name.Contains("_"),
                    $"Tool '{tool.Name}' does not follow category_action naming convention");
            }
        }

        [Test]
        public void AllTools_HaveNonEmptyInputSchema()
        {
            var all = _registry.ListAll();
            foreach (var tool in all)
            {
                Assert.IsNotNull(tool.InputSchema,
                    $"Tool '{tool.Name}' has null InputSchema");
                Assert.IsNotEmpty(tool.InputSchema,
                    $"Tool '{tool.Name}' has empty InputSchema");
            }
        }

        [Test]
        public void ListByCategory_Debug_ReturnsCorrectTools()
        {
            var debugTools = _registry.ListByCategory("debug");
            var names = debugTools.Select(t => t.Name).ToList();

            Assert.Contains("console_getLogs", names);
            Assert.Contains("debug_getStackTrace", names);
            Assert.Contains("debug_getPerformanceStats", names);
            Assert.Contains("debug_screenshotGame", names);
            Assert.Contains("debug_screenshotScene", names);
            Assert.Contains("console_clearLogs", names);

            foreach (var tool in debugTools)
                Assert.AreEqual("debug", tool.Category);
        }

        [Test]
        public void ListByCategory_Editor_ReturnsCorrectTools()
        {
            var editorTools = _registry.ListByCategory("editor");
            var names = editorTools.Select(t => t.Name).ToList();

            Assert.Contains("menu_execute", names);
            Assert.Contains("playmode_control", names);
            Assert.Contains("editor_getSelection", names);
            Assert.Contains("editor_getHierarchy", names);
            Assert.Contains("editor_getProjectStructure", names);
            Assert.Contains("editor_getProjectPath", names);
            Assert.Contains("editor_getInspector", names);
            Assert.Contains("asset_deleteFolder", names);
            Assert.Contains("editor_selectGameObject", names);
            Assert.Contains("editor_findGameObjects", names);
            Assert.Contains("editor_addGameObject", names);
            Assert.Contains("editor_addComponent", names);
            Assert.Contains("editor_deleteGameObject", names);
            Assert.Contains("editor_removeComponent", names);
            Assert.Contains("editor_reparentGameObject", names);
            Assert.Contains("editor_setActive", names);
            Assert.Contains("editor_setComponentEnabled", names);
            Assert.Contains("editor_setTransform", names);
            Assert.Contains("editor_setField", names);

            foreach (var tool in editorTools)
                Assert.AreEqual("editor", tool.Category);
        }

        [Test]
        public void ListByCategory_Build_ReturnsCorrectTools()
        {
            var buildTools = _registry.ListByCategory("build");
            var names = buildTools.Select(t => t.Name).ToList();

            Assert.Contains("build_compile", names);
            Assert.Contains("build_getCompileErrors", names);
            Assert.Contains("build_runTests", names);

            foreach (var tool in buildTools)
                Assert.AreEqual("build", tool.Category);
        }

        [Test]
        public void Resolve_ExistingTool_ReturnsTool()
        {
            var tool = _registry.Resolve(GameScreenshotTool.ToolName);
            Assert.IsNotNull(tool);
            Assert.AreEqual(GameScreenshotTool.ToolName, tool.Name);
        }

        [Test]
        public void Resolve_NonExistingTool_ReturnsNull()
        {
            var tool = _registry.Resolve("nonexistent_tool");
            Assert.IsNull(tool);
        }

        [Test]
        public void AutoDiscover_ScreenshotDisabled_SkipsBothRegistrations()
        {
            EditorPrefs.SetBool(GameScreenshotTool.PrefKey, false);
            EditorPrefs.SetBool(SceneScreenshotTool.PrefKey, false);
            try
            {
                var registry = new ToolRegistry();
                registry.AutoDiscover();

                Assert.IsNull(registry.Resolve(GameScreenshotTool.ToolName));
                Assert.IsNull(registry.Resolve(SceneScreenshotTool.ToolName));
            }
            finally
            {
                EditorPrefs.SetBool(GameScreenshotTool.PrefKey, true);
                EditorPrefs.SetBool(SceneScreenshotTool.PrefKey, true);
            }
        }

        [Test]
        public void Unregister_ExistingTools_RemovesBoth()
        {
            Assert.IsNotNull(_registry.Resolve(GameScreenshotTool.ToolName));
            Assert.IsNotNull(_registry.Resolve(SceneScreenshotTool.ToolName));

            Assert.IsTrue(_registry.Unregister(GameScreenshotTool.ToolName));
            Assert.IsTrue(_registry.Unregister(SceneScreenshotTool.ToolName));
            Assert.IsNull(_registry.Resolve(GameScreenshotTool.ToolName));
            Assert.IsNull(_registry.Resolve(SceneScreenshotTool.ToolName));
            Assert.IsFalse(_registry.Unregister(GameScreenshotTool.ToolName));
            Assert.IsFalse(_registry.Unregister(SceneScreenshotTool.ToolName));
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
