using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// CompileErrorsTool 测试。
    /// </summary>
    public class CompileErrorsToolTests
    {
        private CompileErrorsTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new CompileErrorsTool();
        }

        [Test]
        public void Execute_NoErrors_ReturnsEmptyList()
        {
            // 编译通过的项目应返回空错误列表
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);
            var json = result.Content[0].Text;
            Assert.IsTrue(json.Contains("\"errors\":[]"));
        }
    }
}
