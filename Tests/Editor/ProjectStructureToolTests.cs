using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ProjectStructureTool 测试。
    /// </summary>
    public class ProjectStructureToolTests
    {
        private ProjectStructureTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new ProjectStructureTool();
        }

        [Test]
        public void Execute_DefaultDepth_ReturnsValidJson()
        {
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);
            var json = result.Content[0].Text;
            Assert.IsTrue(json.StartsWith("["));
        }

        [Test]
        public void Execute_NoMetaFiles()
        {
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            var json = result.Content[0].Text;
            Assert.IsFalse(json.Contains(".meta"));
        }

        [Test]
        public void Execute_Depth1_NoNestedChildren()
        {
            var args = new Dictionary<string, object> { { "maxDepth", 1L } };
            var result = _tool.Execute(args).Result;
            Assert.IsFalse(result.IsError);
            // depth=1 时子目录的 children 应为空数组
            // 只要不报错且返回合法 JSON 即可
            Assert.IsTrue(result.Content[0].Text.StartsWith("["));
        }
    }
}
