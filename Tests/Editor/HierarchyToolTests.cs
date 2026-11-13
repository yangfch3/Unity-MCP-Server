using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
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
            Selection.activeGameObject = null;
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Execute_EmptyScene_ReturnsArrayWithExistingObjects()
        {
            // 注意：EditMode 测试中不存在 Prefab Stage，ResolveDefaultRoots 走的是 Active Scene 分支。
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
            // maxDepth=0 时根节点不展开 children，只给 childCount
            Assert.IsTrue(json.Contains("DepthParent"));
            Assert.IsFalse(json.Contains("DepthChild"));
            Assert.IsTrue(json.Contains("\"childCount\":1"));
        }

        [Test]
        public void Execute_OmitsDefaultFields()
        {
            var go = new GameObject("LeafActive");
            _created.Add(go);
            var inactive = new GameObject("LeafInactive");
            _created.Add(inactive);
            inactive.SetActive(false);

            var nodes = MiniJson.Deserialize(_tool.Execute(new Dictionary<string, object>()).Result.Content[0].Text) as List<object>;
            Dictionary<string, object> activeNode = null, inactiveNode = null;
            foreach (var n in nodes)
            {
                var d = (Dictionary<string, object>)n;
                if ((string)d["name"] == "LeafActive") activeNode = d;
                if ((string)d["name"] == "LeafInactive") inactiveNode = d;
            }
            Assert.IsNotNull(activeNode);
            Assert.IsFalse(activeNode.ContainsKey("active"));
            Assert.IsFalse(activeNode.ContainsKey("children"));
            Assert.IsFalse(activeNode.ContainsKey("components"), "Transform-only node should omit components");
            Assert.AreEqual(false, inactiveNode["active"]);
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
            Assert.IsFalse(json.Contains("\"Transform\""));
        }

        // ── root 参数相关测试 ──

        [Test]
        public void Execute_RootEmptyString_EquivalentToDefault()
        {
            var go = new GameObject("RootEmptyTest");
            _created.Add(go);

            var defaultResult = _tool.Execute(new Dictionary<string, object>()).Result;
            var emptyRootResult = _tool.Execute(new Dictionary<string, object> { { "root", "" } }).Result;

            Assert.IsFalse(defaultResult.IsError);
            Assert.IsFalse(emptyRootResult.IsError);
            Assert.AreEqual(defaultResult.Content[0].Text, emptyRootResult.Content[0].Text);
        }

        [Test]
        public void Execute_RootSelection_NoSelection_ReturnsError()
        {
            Selection.activeGameObject = null;

            var args = new Dictionary<string, object> { { "root", "selection" } };
            var result = _tool.Execute(args).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_RootSelection_WithSelection_ReturnsSubtreeWithPath()
        {
            var parent = new GameObject("SelParent");
            _created.Add(parent);
            var child = new GameObject("SelChild");
            _created.Add(child);
            child.transform.SetParent(parent.transform);
            var grandchild = new GameObject("SelGrandchild");
            _created.Add(grandchild);
            grandchild.transform.SetParent(child.transform);

            Selection.activeGameObject = child;

            var args = new Dictionary<string, object> { { "root", "selection" } };
            var result = _tool.Execute(args).Result;

            Assert.IsFalse(result.IsError);
            var json = result.Content[0].Text;

            // 返回的是包装对象，包含 selectionPath 和 children
            var parsed = MiniJson.Deserialize(json) as Dictionary<string, object>;
            Assert.IsNotNull(parsed, "Selection mode should return a JSON object, not an array");

            // 验证 selectionPath
            Assert.IsTrue(parsed.ContainsKey("selectionPath"), "Should contain 'selectionPath' field");
            var selPath = parsed["selectionPath"] as string;
            Assert.AreEqual("/SelParent/SelChild", selPath);

            // 验证 children 包含选中节点的子树
            var children = parsed["children"] as List<object>;
            Assert.IsNotNull(children);
            Assert.AreEqual(1, children.Count);
            var rootNode = children[0] as Dictionary<string, object>;
            Assert.IsNotNull(rootNode);
            Assert.AreEqual("SelChild", rootNode["name"]);

            // 子树包含 grandchild
            Assert.IsTrue(json.Contains("SelGrandchild"));
        }

        [Test]
        public void Execute_RootInvalidValue_ReturnsError()
        {
            var args = new Dictionary<string, object> { { "root", "invalid_value" } };
            var result = _tool.Execute(args).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void InputSchema_ContainsRootAndMaxDepthProperties()
        {
            var schema = MiniJson.Deserialize(_tool.InputSchema) as Dictionary<string, object>;
            Assert.IsNotNull(schema);

            var properties = schema["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("root"), "InputSchema should contain 'root' property");
            Assert.IsTrue(properties.ContainsKey("maxDepth"), "InputSchema should contain 'maxDepth' property");
        }
    }
}
