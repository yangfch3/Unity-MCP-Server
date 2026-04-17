using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// StackTraceTool 测试。
    /// </summary>
    public class StackTraceToolTests
    {
        private StackTraceTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new StackTraceTool();
        }

        [Test]
        public void Execute_ReturnsNonErrorResult()
        {
            // 无论有无错误日志，都不应返回 isError=true
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(result.Content);
            Assert.IsTrue(result.Content.Count > 0);
        }
    }
}
