using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// HierarchyTool 属性测试（标记 Slow，可通过 --where "cat != Slow" 跳过）。
    /// </summary>
    public class HierarchyToolPropertyTests
    {
        // Feature: hierarchy-tool-root-param, Property 2: 无效 root 值一律返回错误
        // Validates: Requirements 3.1
        [Test]
        [Category("Slow")]
        public void Property2_InvalidRootValues_AlwaysReturnError()
        {
            var tool = new HierarchyTool();
            var rng = new System.Random(42);
            var validValues = new HashSet<string> { "", "selection" };

            for (int iter = 0; iter < 100; iter++)
            {
                // Generate a random string that is NOT "" or "selection"
                string randomRoot;
                do
                {
                    int len = rng.Next(1, 30);
                    var chars = new char[len];
                    for (int i = 0; i < len; i++)
                        chars[i] = (char)rng.Next(32, 127);
                    randomRoot = new string(chars);
                } while (validValues.Contains(randomRoot));

                var args = new Dictionary<string, object> { { "root", randomRoot } };
                var result = tool.Execute(args).Result;

                Assert.IsTrue(result.IsError,
                    $"Iteration {iter}: root=\"{randomRoot}\" should return error but didn't");
            }
        }
    }
}
