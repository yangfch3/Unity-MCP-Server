using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// RemoveComponentTool 单元测试。
    /// </summary>
    public class RemoveComponentToolTests
    {
        private RemoveComponentTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new RemoveComponentTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorRemoveComponent()
        {
            Assert.AreEqual("editor_removeComponent", _tool.Name);
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
            Assert.IsTrue(properties.ContainsKey("componentType"), "InputSchema should contain 'componentType' property");
        }

        [Test]
        public void Execute_ValidComponent_RemovesComponent()
        {
            var go = new GameObject("RemoveCompTarget");
            _created.Add(go);
            go.AddComponent<BoxCollider>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "BoxCollider" }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNull(go.GetComponent<BoxCollider>(), "GO should not have BoxCollider after removal");
        }

        [Test]
        public void Execute_CaseInsensitive_RemovesComponent()
        {
            var go = new GameObject("CaseInsensitiveRemove");
            _created.Add(go);
            go.AddComponent<BoxCollider>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "boxcollider" }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNull(go.GetComponent<BoxCollider>(), "GO should not have BoxCollider even with lowercase type name");
        }

        [Test]
        public void Execute_ReturnsCorrectJSON()
        {
            var go = new GameObject("JSONCheckRemove");
            _created.Add(go);
            go.AddComponent<BoxCollider>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "BoxCollider" }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("componentType"), "JSON should contain 'componentType'");
            Assert.IsTrue(json.ContainsKey("name"), "JSON should contain 'name'");
            Assert.IsTrue(json.ContainsKey("path"), "JSON should contain 'path'");

            Assert.AreEqual("BoxCollider", json["componentType"]);
            Assert.AreEqual("JSONCheckRemove", json["name"]);
            Assert.AreEqual(path, json["path"]);
        }

        [Test]
        public void Execute_TransformCannotBeRemoved()
        {
            var go = new GameObject("TransformRemoveTarget");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Transform" }
            }).Result;

            Assert.IsTrue(result.IsError);
            Assert.IsNotNull(go.GetComponent<Transform>(), "Transform should still exist");
        }

        [Test]
        public void Execute_RectTransformCannotBeRemoved()
        {
            var go = new GameObject("RectTransformRemoveTarget");
            _created.Add(go);
            go.AddComponent<RectTransform>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "RectTransform" }
            }).Result;

            Assert.IsTrue(result.IsError);
            Assert.IsNotNull(go.GetComponent<RectTransform>(), "RectTransform should still exist");
        }

        [Test]
        public void Execute_ComponentNotFound_ReturnsError()
        {
            var go = new GameObject("NoCamera");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Camera" }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_MissingComponentType_ReturnsError()
        {
            var go = new GameObject("MissingTypeRemove");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_NonExistentGO_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", "/NonExistentObject" },
                { "componentType", "BoxCollider" }
            }).Result;

            Assert.IsTrue(result.IsError);
        }
    }
}
