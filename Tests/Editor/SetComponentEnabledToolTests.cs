using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// SetComponentEnabledTool 单元测试。
    /// </summary>
    public class SetComponentEnabledToolTests
    {
        private SetComponentEnabledTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new SetComponentEnabledTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorSetComponentEnabled()
        {
            Assert.AreEqual("editor_setComponentEnabled", _tool.Name);
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
            Assert.IsTrue(properties.ContainsKey("instanceID"), "InputSchema should contain 'instanceID'");
            Assert.IsTrue(properties.ContainsKey("path"), "InputSchema should contain 'path'");
            Assert.IsTrue(properties.ContainsKey("componentType"), "InputSchema should contain 'componentType'");
            Assert.IsTrue(properties.ContainsKey("enabled"), "InputSchema should contain 'enabled'");
        }

        [Test]
        public void Execute_EnableBehaviour_SetsEnabled()
        {
            var go = new GameObject("EnableCam");
            var cam = go.AddComponent<Camera>();
            cam.enabled = false;
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Camera" },
                { "enabled", true }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsTrue(cam.enabled, "Camera should be enabled after setting enabled=true");
        }

        [Test]
        public void Execute_DisableBehaviour_SetsEnabled()
        {
            var go = new GameObject("DisableCam");
            var cam = go.AddComponent<Camera>();
            cam.enabled = true;
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Camera" },
                { "enabled", false }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsFalse(cam.enabled, "Camera should be disabled after setting enabled=false");
        }

        [Test]
        public void Execute_EnableRenderer_SetsEnabled()
        {
            var go = new GameObject("EnableRenderer");
            var mr = go.AddComponent<MeshRenderer>();
            mr.enabled = false;
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "MeshRenderer" },
                { "enabled", true }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsTrue(mr.enabled, "MeshRenderer should be enabled after setting enabled=true");
        }

        [Test]
        public void Execute_DisableRenderer_SetsEnabled()
        {
            var go = new GameObject("DisableRenderer");
            var mr = go.AddComponent<MeshRenderer>();
            mr.enabled = true;
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "MeshRenderer" },
                { "enabled", false }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsFalse(mr.enabled, "MeshRenderer should be disabled after setting enabled=false");
        }

        [Test]
        public void Execute_NonEnableableComponent_ReturnsError()
        {
            var go = new GameObject("NonEnableable");
            go.AddComponent<MeshFilter>();
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "MeshFilter" },
                { "enabled", true }
            }).Result;

            Assert.IsTrue(result.IsError, "MeshFilter is not Behaviour or Renderer, should return error");
        }

        [Test]
        public void Execute_ReturnsCorrectJSON()
        {
            var go = new GameObject("JSONCheck");
            var cam = go.AddComponent<Camera>();
            cam.enabled = true;
            _created.Add(go);

            var expectedPath = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", expectedPath },
                { "componentType", "Camera" },
                { "enabled", false }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("componentType"), "JSON should contain 'componentType'");
            Assert.IsTrue(json.ContainsKey("name"), "JSON should contain 'name'");
            Assert.IsTrue(json.ContainsKey("path"), "JSON should contain 'path'");
            Assert.IsTrue(json.ContainsKey("enabled"), "JSON should contain 'enabled'");

            Assert.AreEqual("Camera", json["componentType"]);
            Assert.AreEqual("JSONCheck", json["name"]);
            Assert.AreEqual(expectedPath, json["path"]);
            Assert.AreEqual(false, json["enabled"]);
        }

        [Test]
        public void Execute_ComponentNotFound_ReturnsError()
        {
            var go = new GameObject("NoCam");
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Camera" },
                { "enabled", true }
            }).Result;

            Assert.IsTrue(result.IsError, "Should return error when component not found on GO");
        }

        [Test]
        public void Execute_MissingComponentType_ReturnsError()
        {
            var go = new GameObject("MissingType");
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "enabled", true }
            }).Result;

            Assert.IsTrue(result.IsError, "Should return error when componentType is missing");
        }

        [Test]
        public void Execute_MissingEnabled_ReturnsError()
        {
            var go = new GameObject("MissingEnabled");
            go.AddComponent<Camera>();
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Camera" }
            }).Result;

            Assert.IsTrue(result.IsError, "Should return error when enabled param is missing");
        }

        [Test]
        public void Execute_NonExistentGO_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", "/NonExistentObject" },
                { "componentType", "Camera" },
                { "enabled", true }
            }).Result;

            Assert.IsTrue(result.IsError, "Should return error for non-existent GO");
        }
    }
}
