using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// AssetDeleteFolderTool 单元测试。
    /// </summary>
    public class AssetDeleteFolderToolTests
    {
        private AssetDeleteFolderTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new AssetDeleteFolderTool();
        }

        [Test]
        public void NameAndCategory_AreCorrect()
        {
            Assert.AreEqual("asset_deleteFolder", _tool.Name);
            Assert.AreEqual("editor", _tool.Category);
        }

        [Test]
        public void EmptyPath_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object> { { "path", "" } }).Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.Contains("path parameter is required"));
        }

        [Test]
        public void NullPath_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.Contains("path parameter is required"));
        }

        [Test]
        public void NonExistentDirectory_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", "Assets/NonExistent_TestDir_12345" }
            }).Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.Contains("directory not found"));
        }

        [Test]
        public void PathTraversal_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", "../../etc" }
            }).Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.Contains("outside the Assets folder"));
        }

        [Test]
        public void AutoDiscovery_FindsTool()
        {
            var registry = new ToolRegistry();
            registry.AutoDiscover();
            var tool = registry.Resolve("asset_deleteFolder");
            Assert.IsNotNull(tool);
            Assert.AreEqual("asset_deleteFolder", tool.Name);
        }
    }
}
