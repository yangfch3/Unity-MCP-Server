using System.Linq;
using NUnit.Framework;
using UnityMcp.Editor;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ToolRegistry 注册完整性与分组正确性测试。
    /// </summary>
    public class ToolRegistryTests
    {
        private ToolRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new ToolRegistry();
            _registry.AutoDiscover();
        }

        [Test]
        public void AutoDiscover_FindsAllExpectedTools()
        {
            var all = _registry.ListAll();
            var names = all.Select(t => t.Name).ToList();

            // 原有 3 个工具
            Assert.Contains("console_getLogs", names);
            Assert.Contains("menu_execute", names);
            Assert.Contains("playmode_control", names);

            // 新增 10 个工具
            Assert.Contains("debug_getStackTrace", names);
            Assert.Contains("debug_getPerformanceStats", names);
            Assert.Contains("debug_screenshot", names);
            Assert.Contains("editor_getSelection", names);
            Assert.Contains("editor_getHierarchy", names);
            Assert.Contains("editor_getProjectStructure", names);
            Assert.Contains("editor_getInspector", names);
            Assert.Contains("build_compile", names);
            Assert.Contains("build_getCompileErrors", names);
            Assert.Contains("build_runTests", names);
            Assert.Contains("asset_deleteFolder", names);
            Assert.Contains("console_clearLogs", names);
            Assert.Contains("editor_selectGameObject", names);

            Assert.GreaterOrEqual(all.Count, 16);
        }

        [Test]
        public void AllTools_FollowNamingConvention()
        {
            var all = _registry.ListAll();
            foreach (var tool in all)
            {
                // 名称应包含下划线: {category}_{action}
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
            Assert.Contains("debug_screenshot", names);
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
            Assert.Contains("editor_getInspector", names);
            Assert.Contains("asset_deleteFolder", names);
            Assert.Contains("editor_selectGameObject", names);

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
            var tool = _registry.Resolve("debug_getStackTrace");
            Assert.IsNotNull(tool);
            Assert.AreEqual("debug_getStackTrace", tool.Name);
        }

        [Test]
        public void Resolve_NonExistingTool_ReturnsNull()
        {
            var tool = _registry.Resolve("nonexistent_tool");
            Assert.IsNull(tool);
        }
    }
}
