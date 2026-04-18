using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ConsoleTool 单元测试：级别过滤、关键字搜索、上下文模式等增强功能。
    /// </summary>
    public class ConsoleToolTests
    {
        private ConsoleTool _tool;

        [SetUp]
        public void SetUp()
        {
            ConsoleTool.ClearBuffer();
            _tool = new ConsoleTool();
        }

        [TearDown]
        public void TearDown()
        {
            ConsoleTool.ClearBuffer();
        }

        // Requirements: 3.2 — no level param returns all levels
        [Test]
        public void NoLevelParam_ReturnsAllLevels()
        {
            ConsoleTool.InjectLog("Error", "2025-01-01T00:00:00Z", "err msg");
            ConsoleTool.InjectLog("Warning", "2025-01-01T00:00:01Z", "warn msg");
            ConsoleTool.InjectLog("Log", "2025-01-01T00:00:02Z", "log msg");

            var result = _tool.Execute(new Dictionary<string, object> { { "count", (long)10 } }).Result;

            Assert.IsFalse(result.IsError);
            var entries = ConsoleToolTestHelper.ParseEntries(result.Content[0].Text);
            Assert.AreEqual(3, entries.Count);

            var levels = new HashSet<string>();
            foreach (var e in entries) levels.Add(e.level);
            Assert.IsTrue(levels.Contains("Error"), "Should contain Error level");
            Assert.IsTrue(levels.Contains("Warning"), "Should contain Warning level");
            Assert.IsTrue(levels.Contains("Log"), "Should contain Log level");
        }

        // Requirements: 3.3 — invalid level returns error with valid values listed
        [Test]
        public void InvalidLevel_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object> { { "level", "Debug" } }).Result;

            Assert.IsTrue(result.IsError);
            string msg = result.Content[0].Text;
            Assert.IsTrue(msg.IndexOf("invalid level", StringComparison.OrdinalIgnoreCase) >= 0,
                $"Error message should contain 'invalid level', got: {msg}");
            Assert.IsTrue(msg.Contains("Error"), $"Error message should list 'Error' as valid value, got: {msg}");
            Assert.IsTrue(msg.Contains("Warning"), $"Error message should list 'Warning' as valid value, got: {msg}");
            Assert.IsTrue(msg.Contains("Log"), $"Error message should list 'Log' as valid value, got: {msg}");
        }

        // Requirements: 5.4 — beforeIndex negative returns error
        [Test]
        public void BeforeIndex_Negative_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object> { { "beforeIndex", (long)(-1) } }).Result;

            Assert.IsTrue(result.IsError);
            string msg = result.Content[0].Text;
            Assert.IsTrue(msg.IndexOf("non-negative", StringComparison.OrdinalIgnoreCase) >= 0,
                $"Error message should contain 'non-negative', got: {msg}");
        }

        // Requirements: 5.3 — beforeIndex out of range returns error
        [Test]
        public void BeforeIndex_OutOfRange_ReturnsError()
        {
            for (int i = 0; i < 5; i++)
                ConsoleTool.InjectLog("Log", "2025-01-01T00:00:00Z", $"msg_{i}");

            var result = _tool.Execute(new Dictionary<string, object> { { "beforeIndex", (long)10 } }).Result;

            Assert.IsTrue(result.IsError);
            string msg = result.Content[0].Text;
            Assert.IsTrue(msg.IndexOf("out of range", StringComparison.OrdinalIgnoreCase) >= 0,
                $"Error message should contain 'out of range', got: {msg}");
        }

        // Requirements: 5.6 — context mode ignores level and keyword params
        [Test]
        public void ContextMode_IgnoresLevelAndKeyword()
        {
            ConsoleTool.InjectLog("Error", "2025-01-01T00:00:00Z", "alpha");
            ConsoleTool.InjectLog("Warning", "2025-01-01T00:00:01Z", "beta");
            ConsoleTool.InjectLog("Log", "2025-01-01T00:00:02Z", "gamma");

            // Request context mode with level and keyword that would filter out entries in filter mode
            var parameters = new Dictionary<string, object>
            {
                { "beforeIndex", (long)2 },
                { "count", (long)10 },
                { "level", "Error" },
                { "keyword", "nonexistent" }
            };

            var result = _tool.Execute(parameters).Result;

            Assert.IsFalse(result.IsError);
            var entries = ConsoleToolTestHelper.ParseEntries(result.Content[0].Text);
            // Context mode should return all 3 entries (indices 0,1,2), ignoring level and keyword
            Assert.AreEqual(3, entries.Count, "Context mode should ignore level and keyword filters");
            Assert.AreEqual("alpha", entries[0].message);
            Assert.AreEqual("beta", entries[1].message);
            Assert.AreEqual("gamma", entries[2].message);
        }

        // Requirements: 4.1, 4.2 — keyword filter is case-insensitive
        [Test]
        public void KeywordFilter_CaseInsensitive()
        {
            ConsoleTool.InjectLog("Log", "2025-01-01T00:00:00Z", "Hello world");
            ConsoleTool.InjectLog("Log", "2025-01-01T00:00:01Z", "HELLO WORLD");
            ConsoleTool.InjectLog("Log", "2025-01-01T00:00:02Z", "hello world");
            ConsoleTool.InjectLog("Log", "2025-01-01T00:00:03Z", "no match here");

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "keyword", "hello" },
                { "count", (long)10 }
            }).Result;

            Assert.IsFalse(result.IsError);
            var entries = ConsoleToolTestHelper.ParseEntries(result.Content[0].Text);
            Assert.AreEqual(3, entries.Count, "All case variants of 'hello' should match");
        }

        // Requirements: 3.1, 3.4, 4.1, 4.3 — combined level + keyword + count filtering
        [Test]
        public void CombinedLevelKeywordCount()
        {
            ConsoleTool.InjectLog("Error", "2025-01-01T00:00:00Z", "target found");
            ConsoleTool.InjectLog("Warning", "2025-01-01T00:00:01Z", "target missed");
            ConsoleTool.InjectLog("Error", "2025-01-01T00:00:02Z", "no match");
            ConsoleTool.InjectLog("Error", "2025-01-01T00:00:03Z", "target again");
            ConsoleTool.InjectLog("Log", "2025-01-01T00:00:04Z", "target log");
            ConsoleTool.InjectLog("Error", "2025-01-01T00:00:05Z", "target third");

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "level", "Error" },
                { "keyword", "target" },
                { "count", (long)2 }
            }).Result;

            Assert.IsFalse(result.IsError);
            var entries = ConsoleToolTestHelper.ParseEntries(result.Content[0].Text);

            // Should return at most 2 entries
            Assert.AreEqual(2, entries.Count, "Count should limit results to 2");

            // All entries must be Error level and contain "target"
            foreach (var e in entries)
            {
                Assert.AreEqual("Error", e.level, $"Entry level should be Error, got: {e.level}");
                Assert.IsTrue(e.message.IndexOf("target", StringComparison.OrdinalIgnoreCase) >= 0,
                    $"Entry message should contain 'target', got: {e.message}");
            }

            // Since scan is from tail, the 2 most recent matching entries are "target third" and "target again"
            Assert.AreEqual("target again", entries[0].message);
            Assert.AreEqual("target third", entries[1].message);
        }
    }
}
