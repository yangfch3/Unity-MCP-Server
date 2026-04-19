using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// DeleteGameObjectTool 单元测试。
    /// </summary>
    public class DeleteGameObjectToolTests
    {
        private DeleteGameObjectTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new DeleteGameObjectTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorDeleteGameObject()
        {
            Assert.AreEqual("editor_deleteGameObject", _tool.Name);
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
            Assert.IsTrue(properties.ContainsKey("instanceID"), "InputSchema should contain 'instanceID' property");
            Assert.IsTrue(properties.ContainsKey("path"), "InputSchema should contain 'path' property");
        }

        [Test]
        public void Execute_ByPath_DeletesGO()
        {
            var go = new GameObject("DeleteByPath");
            // Save reference info before deletion
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path }
            }).Result;

            Assert.IsFalse(result.IsError);
            // Unity overloads == for destroyed objects: go == null after DestroyImmediate
            Assert.IsTrue(go == null, "GO should be null after deletion");
        }

        [Test]
        public void Execute_ByInstanceID_DeletesGO()
        {
            var go = new GameObject("DeleteByID");
            var id = (long)go.GetInstanceID();

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", id }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsTrue(go == null, "GO should be null after deletion");
        }

        [Test]
        public void Execute_DeletesChildrenToo()
        {
            var parent = new GameObject("Parent");
            var child1 = new GameObject("Child1");
            var child2 = new GameObject("Child2");
            var grandchild = new GameObject("GrandChild");
            child1.transform.SetParent(parent.transform);
            child2.transform.SetParent(parent.transform);
            grandchild.transform.SetParent(child1.transform);

            var path = HierarchyToolTestHelper.GetGameObjectPath(parent);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsTrue(parent == null, "Parent should be null after deletion");
            Assert.IsTrue(child1 == null, "Child1 should be null after deletion");
            Assert.IsTrue(child2 == null, "Child2 should be null after deletion");
            Assert.IsTrue(grandchild == null, "GrandChild should be null after deletion");
        }

        [Test]
        public void Execute_ReturnsDeletedGOInfo()
        {
            var go = new GameObject("InfoCheck");
            var expectedPath = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", expectedPath }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("name"), "JSON should contain 'name'");
            Assert.IsTrue(json.ContainsKey("path"), "JSON should contain 'path'");
            Assert.AreEqual("InfoCheck", json["name"]);
            Assert.AreEqual(expectedPath, json["path"]);
        }

        [Test]
        public void Execute_NonExistentGO_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", "/NonExistentObject" }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_NoParams_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>()).Result;

            Assert.IsTrue(result.IsError);
        }
    }
}
