using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// AddComponentTool 单元测试。
    /// </summary>
    public class AddComponentToolTests
    {
        private AddComponentTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new AddComponentTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorAddComponent()
        {
            Assert.AreEqual("editor_addComponent", _tool.Name);
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
        public void Execute_ValidComponent_AddsComponent()
        {
            var go = new GameObject("AddCompTarget");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "BoxCollider" }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(go.GetComponent<BoxCollider>(), "GO should have BoxCollider after adding");
        }

        [Test]
        public void Execute_CaseInsensitive_AddsComponent()
        {
            var go = new GameObject("CaseInsensitiveTarget");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "boxcollider" }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(go.GetComponent<BoxCollider>(), "GO should have BoxCollider even with lowercase type name");
        }

        [Test]
        public void Execute_ReturnsCorrectJSON()
        {
            var go = new GameObject("JSONCheckComp");
            _created.Add(go);
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
            Assert.IsTrue(json.ContainsKey("instanceID"), "JSON should contain 'instanceID'");

            Assert.AreEqual("BoxCollider", json["componentType"]);
            Assert.AreEqual("JSONCheckComp", json["name"]);
            Assert.AreEqual(path, json["path"]);
            Assert.IsInstanceOf<long>(json["instanceID"]);
        }

        [Test]
        public void Execute_MissingComponentType_ReturnsError()
        {
            var go = new GameObject("MissingTypeTarget");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_EmptyComponentType_ReturnsError()
        {
            var go = new GameObject("EmptyTypeTarget");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "" }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_NonExistentType_ReturnsError()
        {
            var go = new GameObject("FakeTypeTarget");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "FakeComponent" }
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

        [Test]
        public void Execute_NoParams_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>()).Result;

            Assert.IsTrue(result.IsError);
        }
    }
}
