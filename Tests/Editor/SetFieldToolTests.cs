using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// SetFieldTool 单元测试。
    /// </summary>
    public class SetFieldToolTests
    {
        private SetFieldTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new SetFieldTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorSetField()
        {
            Assert.AreEqual("editor_setField", _tool.Name);
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
            Assert.IsTrue(properties.ContainsKey("fieldName"), "InputSchema should contain 'fieldName' property");
            Assert.IsTrue(properties.ContainsKey("value"), "InputSchema should contain 'value' property");
        }

        [Test]
        public void Execute_SetFloatField_SetsCorrectly()
        {
            var go = new GameObject("FloatFieldGO");
            _created.Add(go);
            go.AddComponent<Light>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Light" },
                { "fieldName", "m_Intensity" },
                { "value", 3.5 }
            }).Result;

            Assert.IsFalse(result.IsError, result.Content[0].Text);

            var light = go.GetComponent<Light>();
            Assert.AreEqual(3.5f, light.intensity, 0.001f, "Light intensity should be 3.5");
        }

        [Test]
        public void Execute_SetBoolField_SetsCorrectly()
        {
            var go = new GameObject("BoolFieldGO");
            _created.Add(go);
            go.AddComponent<BoxCollider>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            // BoxCollider.isTrigger serialized as "m_IsTrigger"
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "BoxCollider" },
                { "fieldName", "m_IsTrigger" },
                { "value", true }
            }).Result;

            Assert.IsFalse(result.IsError, result.Content[0].Text);

            var collider = go.GetComponent<BoxCollider>();
            Assert.IsTrue(collider.isTrigger, "BoxCollider.isTrigger should be true");
        }

        [Test]
        public void Execute_ReturnsCorrectJSON()
        {
            var go = new GameObject("JSONCheckField");
            _created.Add(go);
            go.AddComponent<Light>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Light" },
                { "fieldName", "m_Intensity" },
                { "value", 5.0 }
            }).Result;

            Assert.IsFalse(result.IsError, result.Content[0].Text);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("fieldName"), "JSON should contain 'fieldName'");
            Assert.IsTrue(json.ContainsKey("fieldType"), "JSON should contain 'fieldType'");
            Assert.IsTrue(json.ContainsKey("newValue"), "JSON should contain 'newValue'");

            Assert.AreEqual("m_Intensity", json["fieldName"]);
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
                { "componentType", "Camera" },
                { "fieldName", "m_Enabled" },
                { "value", true }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_FieldNotFound_ReturnsError()
        {
            var go = new GameObject("BadFieldGO");
            _created.Add(go);
            go.AddComponent<BoxCollider>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "BoxCollider" },
                { "fieldName", "nonExistentField" },
                { "value", 1.0 }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_MissingComponentType_ReturnsError()
        {
            var go = new GameObject("MissingCompType");
            _created.Add(go);
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "fieldName", "m_Intensity" },
                { "value", 1.0 }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_MissingFieldName_ReturnsError()
        {
            var go = new GameObject("MissingFieldName");
            _created.Add(go);
            go.AddComponent<Light>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Light" },
                { "value", 1.0 }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_MissingValue_ReturnsError()
        {
            var go = new GameObject("MissingValue");
            _created.Add(go);
            go.AddComponent<Light>();
            var path = HierarchyToolTestHelper.GetGameObjectPath(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "componentType", "Light" },
                { "fieldName", "m_Intensity" }
            }).Result;

            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_NonExistentGO_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", "/NonExistentObject" },
                { "componentType", "Light" },
                { "fieldName", "m_Intensity" },
                { "value", 1.0 }
            }).Result;

            Assert.IsTrue(result.IsError);
        }
    }
}
