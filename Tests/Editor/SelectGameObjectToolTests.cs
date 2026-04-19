using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// SelectGameObjectTool 单元测试。
    /// </summary>
    public class SelectGameObjectToolTests
    {
        private SelectGameObjectTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new SelectGameObjectTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            Selection.activeGameObject = null;
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorSelectGameObject()
        {
            Assert.AreEqual("editor_selectGameObject", _tool.Name);
        }

        [Test]
        public void Category_IsEditor()
        {
            Assert.AreEqual("editor", _tool.Category);
        }

        [Test]
        public void InputSchema_ContainsPathAndInstanceIDProperties()
        {
            var schema = MiniJson.Deserialize(_tool.InputSchema) as Dictionary<string, object>;
            Assert.IsNotNull(schema);

            var properties = schema["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("path"), "InputSchema should contain 'path' property");
            Assert.IsTrue(properties.ContainsKey("instanceID"), "InputSchema should contain 'instanceID' property");

            // path 和 instanceID 二选一，不再有 required
            Assert.IsFalse(schema.ContainsKey("required"), "InputSchema should not have 'required' (path and instanceID are alternatives)");
        }

        [Test]
        public void Execute_EmptyPath_NoInstanceID_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object> { { "path", "" } }).Result;
            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_NullPath_NoInstanceID_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_ValidPath_SelectsTargetGameObject()
        {
            var root = new GameObject("SelRoot");
            _created.Add(root);
            var child = new GameObject("SelChild");
            _created.Add(child);
            child.transform.SetParent(root.transform);
            var target = new GameObject("SelTarget");
            _created.Add(target);
            target.transform.SetParent(child.transform);

            var path = HierarchyToolTestHelper.GetGameObjectPath(target);
            var result = _tool.Execute(new Dictionary<string, object> { { "path", path } }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(target, Selection.activeGameObject);

            // 验证返回 JSON 中 name/path/instanceID 的正确性
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual("SelTarget", json["name"]);
            Assert.AreEqual(path, json["path"]);
            Assert.AreEqual((long)target.GetInstanceID(), json["instanceID"]);
        }

        [Test]
        public void Execute_NonExistentPath_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", "/NoSuchRoot/NoSuchChild" }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_ValidInstanceID_SelectsTargetGameObject()
        {
            var root = new GameObject("IdRoot");
            _created.Add(root);
            var child = new GameObject("IdChild");
            _created.Add(child);
            child.transform.SetParent(root.transform);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)child.GetInstanceID() }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(child, Selection.activeGameObject);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual("IdChild", json["name"]);
            Assert.AreEqual((long)child.GetInstanceID(), json["instanceID"]);
        }

        [Test]
        public void Execute_InvalidInstanceID_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)999999999 }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_InstanceID_TakesPriorityOverPath()
        {
            var goA = new GameObject("PriorityA");
            _created.Add(goA);
            var goB = new GameObject("PriorityB");
            _created.Add(goB);

            // 传入 goA 的 instanceID 和 goB 的 path，应选中 goA
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)goA.GetInstanceID() },
                { "path", HierarchyToolTestHelper.GetGameObjectPath(goB) }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(goA, Selection.activeGameObject);
        }
    }
}
