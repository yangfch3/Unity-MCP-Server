using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// SetActiveTool 单元测试。
    /// </summary>
    public class SetActiveToolTests
    {
        private SetActiveTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new SetActiveTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorSetActive()
        {
            Assert.AreEqual("editor_setActive", _tool.Name);
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
            Assert.IsTrue(properties.ContainsKey("active"), "InputSchema should contain 'active' property");
        }

        [Test]
        public void Execute_SetActiveTrue_ActivatesGO()
        {
            var go = new GameObject("InactiveGO");
            go.SetActive(false);
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "active", true }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsTrue(go.activeSelf, "GO should be active after SetActive(true)");
        }

        [Test]
        public void Execute_SetActiveFalse_DeactivatesGO()
        {
            var go = new GameObject("ActiveGO");
            _created.Add(go);
            Assert.IsTrue(go.activeSelf, "GO should start active");

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "active", false }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsFalse(go.activeSelf, "GO should be inactive after SetActive(false)");
        }

        [Test]
        public void Execute_ReturnsCorrectJSON()
        {
            var go = new GameObject("JSONCheck");
            _created.Add(go);

            var expectedPath = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", expectedPath },
                { "active", false }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("name"), "JSON should contain 'name'");
            Assert.IsTrue(json.ContainsKey("path"), "JSON should contain 'path'");
            Assert.IsTrue(json.ContainsKey("activeSelf"), "JSON should contain 'activeSelf'");

            Assert.AreEqual("JSONCheck", json["name"]);
            Assert.AreEqual(expectedPath, json["path"]);
            Assert.AreEqual(false, json["activeSelf"]);
        }

        [Test]
        public void Execute_MissingActive_ReturnsError()
        {
            var go = new GameObject("NoActive");
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
                { "active", true }
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
