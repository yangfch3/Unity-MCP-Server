using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ConsoleTool 属性测试（标记 Slow，可通过 --where "cat != Slow" 跳过）。
    /// </summary>
    public class ConsoleToolPropertyTests
    {
        private static readonly string[] ValidLevels = { "Error", "Warning", "Log" };

        // Feature: mcp-console-asset-tools, Property 2: Combined log filtering invariant
        // Validates: Requirements 3.1, 3.4, 4.1, 4.3
        [Test]
        [Category("Slow")]
        public void Property2_CombinedLogFilteringInvariant()
        {
            var tool = new ConsoleTool();
            var rng = new System.Random(123);
            string knownSubstring = "XMARKER";

            for (int iter = 0; iter < 100; iter++)
            {
                ConsoleTool.ClearBuffer();

                // 1. Inject random entries (10-100) with random levels and messages
                int entryCount = rng.Next(10, 101);
                for (int i = 0; i < entryCount; i++)
                {
                    string level = ValidLevels[rng.Next(ValidLevels.Length)];
                    // Some messages contain the known substring, some don't
                    bool includeMarker = rng.Next(2) == 0;
                    string message = includeMarker
                        ? $"prefix_{i}_{knownSubstring}_suffix_{rng.Next(1000)}"
                        : $"nomatch_{i}_random_{rng.Next(1000)}";
                    ConsoleTool.InjectLog(level, "2025-01-01T00:00:00Z", message);
                }

                // 2. Pick random parameters
                string filterLevel = rng.Next(2) == 0 ? ValidLevels[rng.Next(ValidLevels.Length)] : null;
                string filterKeyword = rng.Next(2) == 0 ? knownSubstring : null;
                int count = rng.Next(1, 51);

                // 3. Call Execute
                var parameters = new Dictionary<string, object> { { "count", (long)count } };
                if (filterLevel != null) parameters["level"] = filterLevel;
                if (filterKeyword != null) parameters["keyword"] = filterKeyword;

                var result = tool.Execute(parameters).Result;
                Assert.IsFalse(result.IsError, $"Iteration {iter}: unexpected error: {result.Content[0].Text}");

                string json = result.Content[0].Text;
                var entries = ConsoleToolTestHelper.ParseEntries(json);

                // 4. Verify invariants
                // result length ≤ count
                Assert.LessOrEqual(entries.Count, count,
                    $"Iteration {iter}: result count {entries.Count} exceeds requested count {count}");

                for (int e = 0; e < entries.Count; e++)
                {
                    var (entryLevel, entryMessage) = entries[e];

                    // if level was specified, every returned entry's level equals the specified level
                    if (filterLevel != null)
                    {
                        Assert.AreEqual(filterLevel, entryLevel,
                            $"Iteration {iter}, entry {e}: expected level '{filterLevel}' but got '{entryLevel}'");
                    }

                    // if keyword was specified, every returned entry's message contains the keyword (case-insensitive)
                    if (filterKeyword != null)
                    {
                        Assert.IsTrue(
                            entryMessage.IndexOf(filterKeyword, StringComparison.OrdinalIgnoreCase) >= 0,
                            $"Iteration {iter}, entry {e}: message '{entryMessage}' does not contain keyword '{filterKeyword}'");
                    }
                }
            }

            // Clean up
            ConsoleTool.ClearBuffer();
        }

        // Feature: mcp-console-asset-tools, Property 3: Context mode returns correct contiguous slice
        // Validates: Requirements 5.1, 5.2, 5.5, 5.6
        [Test]
        [Category("Slow")]
        public void Property3_ContextModeReturnsCorrectContiguousSlice()
        {
            var tool = new ConsoleTool();
            var rng = new System.Random(456);

            for (int iter = 0; iter < 100; iter++)
            {
                ConsoleTool.ClearBuffer();

                // 1. Inject random number of entries (10-200) with unique messages
                int entryCount = rng.Next(10, 201);
                for (int i = 0; i < entryCount; i++)
                {
                    ConsoleTool.InjectLog("Log", "2025-01-01T00:00:00Z", $"ctx_{i}");
                }

                // 2. Pick random valid beforeIndex and count
                int beforeIndex = rng.Next(0, entryCount); // 0 to buffer.Count-1
                int count = rng.Next(1, 51);               // 1 to 50

                // 3. Call Execute with beforeIndex and count
                var parameters = new Dictionary<string, object>
                {
                    { "beforeIndex", (long)beforeIndex },
                    { "count", (long)count }
                };

                var result = tool.Execute(parameters).Result;
                Assert.IsFalse(result.IsError,
                    $"Iteration {iter}: unexpected error: {result.Content[0].Text}");

                string json = result.Content[0].Text;
                var entries = ConsoleToolTestHelper.ParseEntries(json);

                // 4. Compute expected slice
                int expectedStart = Math.Max(0, beforeIndex - count + 1);
                int expectedCount = beforeIndex - expectedStart + 1; // == min(count, beforeIndex + 1)

                // 5. Verify number of returned entries
                Assert.AreEqual(expectedCount, entries.Count,
                    $"Iteration {iter}: expected {expectedCount} entries but got {entries.Count} " +
                    $"(beforeIndex={beforeIndex}, count={count})");

                // 6. Verify each entry's message and chronological order
                for (int e = 0; e < entries.Count; e++)
                {
                    int expectedIndex = expectedStart + e;
                    string expectedMessage = $"ctx_{expectedIndex}";

                    Assert.AreEqual(expectedMessage, entries[e].message,
                        $"Iteration {iter}, entry {e}: expected message '{expectedMessage}' " +
                        $"but got '{entries[e].message}'");

                    // Parse the index field from the raw JSON to verify ordering
                    // Re-extract from the JSON to get the index field
                }

                // 7. Verify index fields are ascending (chronological order)
                // Re-parse to get index values
                int pos = 0;
                var indices = new List<long>();
                while (pos < json.Length)
                {
                    int objStart = json.IndexOf('{', pos);
                    if (objStart < 0) break;
                    int objEnd = json.IndexOf('}', objStart);
                    if (objEnd < 0) break;
                    string obj = json.Substring(objStart, objEnd - objStart + 1);
                    indices.Add(ConsoleToolTestHelper.ExtractLongField(obj, "index"));
                    pos = objEnd + 1;
                }

                Assert.AreEqual(entries.Count, indices.Count,
                    $"Iteration {iter}: index count mismatch");

                for (int e = 0; e < indices.Count; e++)
                {
                    long expectedIndex = expectedStart + e;
                    Assert.AreEqual(expectedIndex, indices[e],
                        $"Iteration {iter}, entry {e}: expected index {expectedIndex} but got {indices[e]}");
                }

                // Verify ascending order
                for (int e = 1; e < indices.Count; e++)
                {
                    Assert.Greater(indices[e], indices[e - 1],
                        $"Iteration {iter}: entries not in chronological order at position {e}");
                }
            }

            // Clean up
            ConsoleTool.ClearBuffer();
        }

        // Feature: mcp-console-asset-tools, Property 4: Buffer capacity invariant with FIFO eviction
        // Validates: Requirements 6.1, 6.2
        [Test]
        [Category("Slow")]
        public void Property4_BufferCapacityInvariant_FIFOEviction()
        {
            var tool = new ConsoleTool();
            var rng = new System.Random(42);

            for (int iter = 0; iter < 100; iter++)
            {
                ConsoleTool.ClearBuffer();

                int n = rng.Next(2501, 5001); // random N in [2501, 5000]

                for (int i = 0; i < n; i++)
                {
                    ConsoleTool.InjectLog("Log", "2025-01-01T00:00:00Z", $"msg_{i}");
                }

                // Buffer size must be exactly 2500
                Assert.AreEqual(2500, ConsoleTool.BufferCount,
                    $"Iteration {iter}: expected buffer size 2500 after injecting {n} entries");

                // Retrieve all 2500 entries and verify the last one is the most recently injected
                var result = tool.Execute(new Dictionary<string, object> { { "count", (long)2500 } }).Result;
                Assert.IsFalse(result.IsError);

                string json = result.Content[0].Text;
                string expectedLastMsg = $"msg_{n - 1}";

                // The last entry in the returned JSON should have the last injected message
                Assert.IsTrue(json.Contains($"\"message\":\"{expectedLastMsg}\""),
                    $"Iteration {iter}: buffer should contain the last injected message '{expectedLastMsg}'");

                // The first entry should be msg_{n - 2500} (oldest surviving entry)
                string expectedFirstMsg = $"msg_{n - 2500}";
                Assert.IsTrue(json.Contains($"\"message\":\"{expectedFirstMsg}\""),
                    $"Iteration {iter}: buffer should contain the oldest surviving message '{expectedFirstMsg}'");

                // Verify that an evicted entry is NOT present
                string evictedMsg = $"msg_{n - 2501}";
                Assert.IsFalse(json.Contains($"\"message\":\"{evictedMsg}\""),
                    $"Iteration {iter}: buffer should NOT contain evicted message '{evictedMsg}'");
            }

            // Clean up
            ConsoleTool.ClearBuffer();
        }
    }
}
