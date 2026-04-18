using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ConsoleClearTool 单元测试。
    /// </summary>
    public class ConsoleClearToolTests
    {
        private ConsoleClearTool _tool;

        [SetUp]
        public void SetUp()
        {
            ConsoleTool.ClearBuffer();
            _tool = new ConsoleClearTool();
        }

        [TearDown]
        public void TearDown()
        {
            ConsoleTool.ClearBuffer();
        }

        // Requirements: 2.4 — Name and Category properties
        [Test]
        public void NameAndCategory_AreCorrect()
        {
            Assert.AreEqual("console_clearLogs", _tool.Name);
            Assert.AreEqual("debug", _tool.Category);
        }

        // Requirements: 2.1 — clearing non-empty buffer returns success and empties buffer
        [Test]
        public void ClearNonEmptyBuffer_ReturnsSuccessAndEmptiesBuffer()
        {
            ConsoleTool.InjectLog("Error", "2025-01-01T00:00:00Z", "err msg");
            ConsoleTool.InjectLog("Warning", "2025-01-01T00:00:01Z", "warn msg");
            ConsoleTool.InjectLog("Log", "2025-01-01T00:00:02Z", "log msg");
            Assert.AreEqual(3, ConsoleTool.BufferCount);

            var result = _tool.Execute(new Dictionary<string, object>()).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsTrue(result.Content[0].Text.Contains("cleared"),
                $"Success message should contain 'cleared', got: {result.Content[0].Text}");
            Assert.AreEqual(0, ConsoleTool.BufferCount);
        }

        // Requirements: 2.2 — clearing already-empty buffer returns success without error
        [Test]
        public void ClearEmptyBuffer_ReturnsSuccess()
        {
            Assert.AreEqual(0, ConsoleTool.BufferCount);

            var result = _tool.Execute(new Dictionary<string, object>()).Result;

            Assert.IsFalse(result.IsError);
        }

        // Requirements: 2.3 — auto-discovery via ToolRegistry
        [Test]
        public void AutoDiscovery_FindsTool()
        {
            var registry = new ToolRegistry();
            registry.AutoDiscover();
            var tool = registry.Resolve("console_clearLogs");
            Assert.IsNotNull(tool);
            Assert.AreEqual("console_clearLogs", tool.Name);
        }
    }
}
