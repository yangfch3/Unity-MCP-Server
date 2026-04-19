using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// AddGameObjectTool 单元测试。
    /// </summary>
    public class AddGameObjectToolTests
    {
        private AddGameObjectTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new AddGameObjectTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorAddGameObject()
        {
            Assert.AreEqual("editor_addGameObject", _tool.Name);
        }

        [Test]
        public void Category_IsEditor()
        {
            Assert.AreEqual("editor", _tool.Category);
        }

        [Test]
        public void InputSchema_ContainsExpectedProperties()
        {
            var schema = MiniJson.Deserialize(_tool.InputSchema) as Dictionary<string, object>;
            Assert.IsNotNull(schema);

            var properties = schema["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("name"), "InputSchema should contain 'name' property");
            Assert.IsTrue(properties.ContainsKey("parentInstanceID"), "InputSchema should contain 'parentInstanceID' property");
            Assert.IsTrue(properties.ContainsKey("parentPath"), "InputSchema should contain 'parentPath' property");
        }

        [Test]
        public void Execute_NoParams_CreatesDefaultNamedGO()
        {
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual("GameObject", json["name"]);

            // 清理：通过 instanceID 找到创建的 GO 并加入 _created
            var id = (long)json["instanceID"];
            var go = EditorUtility.InstanceIDToObject((int)id) as GameObject;
            Assert.IsNotNull(go);
            _created.Add(go);
        }

        [Test]
        public void Execute_WithName_CreatesNamedGO()
        {
            var result = _tool.Execute(new Dictionary<string, object> { { "name", "TestGO" } }).Result;
            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual("TestGO", json["name"]);

            var id = (long)json["instanceID"];
            var go = EditorUtility.InstanceIDToObject((int)id) as GameObject;
            Assert.IsNotNull(go);
            _created.Add(go);
        }

        [Test]
        public void Execute_EmptyName_UsesDefault()
        {
            var result = _tool.Execute(new Dictionary<string, object> { { "name", "" } }).Result;
            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual("GameObject", json["name"]);

            var id = (long)json["instanceID"];
            var go = EditorUtility.InstanceIDToObject((int)id) as GameObject;
            Assert.IsNotNull(go);
            _created.Add(go);
        }

        [Test]
        public void Execute_WithParentPath_CreatesUnderParent()
        {
            var parent = new GameObject("AddGOParent");
            _created.Add(parent);

            var parentPath = HierarchyToolTestHelper.GetGameObjectPath(parent);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "name", "Child" },
                { "parentPath", parentPath }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual("Child", json["name"]);

            var id = (long)json["instanceID"];
            var child = EditorUtility.InstanceIDToObject((int)id) as GameObject;
            Assert.IsNotNull(child);
            _created.Add(child);

            Assert.AreEqual(parent.transform, child.transform.parent);
        }

        [Test]
        public void Execute_WithParentInstanceID_CreatesUnderParent()
        {
            var parent = new GameObject("AddGOParentID");
            _created.Add(parent);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "name", "ChildByID" },
                { "parentInstanceID", (long)parent.GetInstanceID() }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual("ChildByID", json["name"]);

            var id = (long)json["instanceID"];
            var child = EditorUtility.InstanceIDToObject((int)id) as GameObject;
            Assert.IsNotNull(child);
            _created.Add(child);

            Assert.AreEqual(parent.transform, child.transform.parent);
        }

        [Test]
        public void Execute_InvalidParent_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "parentPath", "/NonExistent" }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_ReturnsCorrectJSON()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "name", "JSONCheck" }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("name"), "JSON should contain 'name'");
            Assert.IsTrue(json.ContainsKey("path"), "JSON should contain 'path'");
            Assert.IsTrue(json.ContainsKey("instanceID"), "JSON should contain 'instanceID'");

            Assert.AreEqual("JSONCheck", json["name"]);
            Assert.IsInstanceOf<string>(json["path"]);
            Assert.IsInstanceOf<long>(json["instanceID"]);

            var id = (long)json["instanceID"];
            var go = EditorUtility.InstanceIDToObject((int)id) as GameObject;
            Assert.IsNotNull(go);
            _created.Add(go);
        }
    }
}
