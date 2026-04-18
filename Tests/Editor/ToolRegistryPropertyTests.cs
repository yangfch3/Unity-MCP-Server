using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ToolRegistry 属性测试。
    /// 使用手写随机生成器验证注册完整性与分组正确性。
    /// </summary>
    [Category("Slow")]
    public class ToolRegistryPropertyTests
    {
        private static readonly Random Rng = new Random(42);
        private static readonly string[] Categories = { "debug", "editor", "build", "test", "custom" };

        /// <summary>
        /// Property: ListAll 返回数量 == 注册数量（去重后），且所有字段一致。
        /// </summary>
        [Test]
        public void RegisteredTools_ListAll_CountAndFieldsMatch()
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var registry = new ToolRegistry();
                var tools = GenerateRandomTools(Rng.Next(1, 30));

                foreach (var tool in tools)
                    registry.Register(tool);

                // 去重：后注册的覆盖先注册的
                var expected = new Dictionary<string, FakeTool>();
                foreach (var tool in tools)
                    expected[tool.Name] = tool;

                var listed = registry.ListAll();

                Assert.AreEqual(expected.Count, listed.Count,
                    $"Iteration {iteration}: ListAll count mismatch. Expected {expected.Count}, got {listed.Count}");

                foreach (var info in listed)
                {
                    Assert.IsTrue(expected.ContainsKey(info.Name),
                        $"Iteration {iteration}: Unexpected tool '{info.Name}' in ListAll");
                    var orig = expected[info.Name];
                    Assert.AreEqual(orig.Category, info.Category,
                        $"Iteration {iteration}: Category mismatch for '{info.Name}'");
                    Assert.AreEqual(orig.Description, info.Description,
                        $"Iteration {iteration}: Description mismatch for '{info.Name}'");
                }
            }
        }

        /// <summary>
        /// Property: ListByCategory 分组正确——每个分类下的工具 Category 一致，且总数等于 ListAll。
        /// </summary>
        [Test]
        public void ListByCategory_GroupingIsCorrect()
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var registry = new ToolRegistry();
                var tools = GenerateRandomTools(Rng.Next(1, 30));

                foreach (var tool in tools)
                    registry.Register(tool);

                var allListed = registry.ListAll();
                var usedCategories = allListed.Select(t => t.Category).Distinct().ToList();

                int totalFromCategories = 0;
                foreach (var cat in usedCategories)
                {
                    var catTools = registry.ListByCategory(cat);
                    foreach (var t in catTools)
                    {
                        Assert.AreEqual(cat, t.Category,
                            $"Iteration {iteration}: Tool '{t.Name}' in category '{cat}' has mismatched Category '{t.Category}'");
                    }
                    totalFromCategories += catTools.Count;
                }

                Assert.AreEqual(allListed.Count, totalFromCategories,
                    $"Iteration {iteration}: Sum of category counts ({totalFromCategories}) != ListAll count ({allListed.Count})");
            }
        }

        /// <summary>
        /// Property: Resolve 对所有已注册工具返回非 null，对未注册名称返回 null。
        /// </summary>
        [Test]
        public void Resolve_FindsAllRegistered_ReturnsNullForUnknown()
        {
            for (int iteration = 0; iteration < 100; iteration++)
            {
                var registry = new ToolRegistry();
                var tools = GenerateRandomTools(Rng.Next(1, 20));

                foreach (var tool in tools)
                    registry.Register(tool);

                // 去重后的名称集合
                var registeredNames = new HashSet<string>(tools.Select(t => t.Name));

                foreach (var name in registeredNames)
                {
                    Assert.IsNotNull(registry.Resolve(name),
                        $"Iteration {iteration}: Resolve('{name}') should not be null");
                }

                // 随机不存在的名称
                var fakeName = $"__nonexistent_{iteration}_{Rng.Next()}";
                Assert.IsNull(registry.Resolve(fakeName),
                    $"Iteration {iteration}: Resolve('{fakeName}') should be null");
            }
        }

        /// <summary>
        /// Property: 重复注册同名工具时，后者覆盖前者。
        /// </summary>
        [Test]
        public void DuplicateRegister_LastOneWins()
        {
            for (int iteration = 0; iteration < 50; iteration++)
            {
                var registry = new ToolRegistry();
                var name = $"dup_tool_{iteration}";

                var first = new FakeTool(name, "cat_a", "first version");
                var second = new FakeTool(name, "cat_b", "second version");

                registry.Register(first);
                registry.Register(second);

                var resolved = registry.Resolve(name);
                Assert.IsNotNull(resolved);
                Assert.AreEqual("cat_b", resolved.Category,
                    $"Iteration {iteration}: Should be overwritten to cat_b");

                var listed = registry.ListAll();
                Assert.AreEqual(1, listed.Count,
                    $"Iteration {iteration}: Should have exactly 1 tool after duplicate registration");
            }
        }

        // ---- Helpers ----

        private List<FakeTool> GenerateRandomTools(int count)
        {
            var tools = new List<FakeTool>();
            for (int i = 0; i < count; i++)
            {
                var name = $"tool_{Rng.Next(0, count * 2)}"; // allow some name collisions
                var cat = Categories[Rng.Next(Categories.Length)];
                var desc = $"Description for {name}";
                tools.Add(new FakeTool(name, cat, desc));
            }
            return tools;
        }

        private class FakeTool : IMcpTool
        {
            public string Name { get; }
            public string Category { get; }
            public string Description { get; }
            public string InputSchema => "{\"type\":\"object\"}";

            public FakeTool(string name, string category, string description)
            {
                Name = name;
                Category = category;
                Description = description;
            }

            public Task<ToolResult> Execute(Dictionary<string, object> parameters)
            {
                return Task.FromResult(ToolResult.Success("fake"));
            }
        }
    }
}
