using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// HierarchyTool 树遍历与深度限制测试。
    /// </summary>
    public class HierarchyToolTests
    {
        private HierarchyTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new HierarchyTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created)
                if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void Execute_EmptyScene_ReturnsArrayWithExistingObjects()
        {
            // 场景可能有默认对象，只验证返回合法 JSON 数组
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);
            Assert.IsTrue(result.Content[0].Text.StartsWith("["));
        }

        [Test]
        public void Execute_WithGameObjects_ReturnsTree()
        {
            var parent = new GameObject("TestParent");
            _created.Add(parent);
            var child = new GameObject("TestChild");
            _created.Add(child);
            child.transform.SetParent(parent.transform);

            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);
            var json = result.Content[0].Text;
            Assert.IsTrue(json.Contains("TestParent"));
            Assert.IsTrue(json.Contains("TestChild"));
        }

        [Test]
        public void Execute_MaxDepth0_NoChildren()
        {
            var parent = new GameObject("DepthParent");
            _created.Add(parent);
            var child = new GameObject("DepthChild");
            _created.Add(child);
            child.transform.SetParent(parent.transform);

            var args = new Dictionary<string, object> { { "maxDepth", 0L } };
            var result = _tool.Execute(args).Result;
            Assert.IsFalse(result.IsError);
            var json = result.Content[0].Text;
            // maxDepth=0 时根节点的 children 应为空数组
            Assert.IsTrue(json.Contains("DepthParent"));
            Assert.IsFalse(json.Contains("DepthChild"));
        }

        [Test]
        public void Execute_IncludesComponents()
        {
            var go = new GameObject("CompTest");
            _created.Add(go);
            go.AddComponent<BoxCollider>();

            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            var json = result.Content[0].Text;
            Assert.IsTrue(json.Contains("BoxCollider"));
            Assert.IsTrue(json.Contains("Transform"));
        }
    }
}
