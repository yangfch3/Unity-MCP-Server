using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// SelectGameObjectTool 属性测试（标记 Slow，可通过 --where "cat != Slow" 跳过）。
    /// </summary>
    public class SelectGameObjectToolPropertyTests
    {
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeGameObject = null;
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        // Feature: hierarchy-tool-root-param, Property 5: 不存在的路径返回错误
        // Validates: Requirements 6.2
        [Test]
        [Category("Slow")]
        public void Property5_NonExistentPaths_AlwaysReturnError()
        {
            var tool = new SelectGameObjectTool();
            var rng = new System.Random(99);

            // Create a known tree so we can be sure random paths don't match
            var root = new GameObject("PropTestRoot");
            _created.Add(root);
            var child = new GameObject("PropTestChild");
            _created.Add(child);
            child.transform.SetParent(root.transform);

            for (int iter = 0; iter < 100; iter++)
            {
                // Generate a random path that won't match any existing GO
                int segments = rng.Next(1, 5);
                var parts = new List<string>();
                for (int s = 0; s < segments; s++)
                {
                    int len = rng.Next(3, 15);
                    var chars = new char[len];
                    for (int i = 0; i < len; i++)
                        chars[i] = (char)rng.Next('a', 'z' + 1);
                    parts.Add("rnd_" + new string(chars));
                }
                string randomPath = "/" + string.Join("/", parts);

                var args = new Dictionary<string, object> { { "path", randomPath } };
                var result = tool.Execute(args).Result;

                Assert.IsTrue(result.IsError,
                    $"Iteration {iter}: path=\"{randomPath}\" should return error but didn't");
            }
        }
    }
}
