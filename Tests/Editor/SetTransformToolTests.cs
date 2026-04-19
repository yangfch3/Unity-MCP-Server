using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// SetTransformTool 单元测试。
    /// </summary>
    public class SetTransformToolTests
    {
        private SetTransformTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new SetTransformTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorSetTransform()
        {
            Assert.AreEqual("editor_setTransform", _tool.Name);
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
            Assert.IsTrue(properties.ContainsKey("localPosition"), "InputSchema should contain 'localPosition'");
            Assert.IsTrue(properties.ContainsKey("localRotation"), "InputSchema should contain 'localRotation'");
            Assert.IsTrue(properties.ContainsKey("localScale"), "InputSchema should contain 'localScale'");
            Assert.IsTrue(properties.ContainsKey("anchoredPosition"), "InputSchema should contain 'anchoredPosition'");
            Assert.IsTrue(properties.ContainsKey("sizeDelta"), "InputSchema should contain 'sizeDelta'");
            Assert.IsTrue(properties.ContainsKey("pivot"), "InputSchema should contain 'pivot'");
            Assert.IsTrue(properties.ContainsKey("anchorMin"), "InputSchema should contain 'anchorMin'");
            Assert.IsTrue(properties.ContainsKey("anchorMax"), "InputSchema should contain 'anchorMax'");
        }

        [Test]
        public void Execute_SetLocalPosition_SetsCorrectly()
        {
            var go = new GameObject("PosGO");
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "localPosition", new List<object> { 1.0, 2.0, 3.0 } }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(1f, go.transform.localPosition.x, 0.001f);
            Assert.AreEqual(2f, go.transform.localPosition.y, 0.001f);
            Assert.AreEqual(3f, go.transform.localPosition.z, 0.001f);
        }

        [Test]
        public void Execute_SetLocalRotation_SetsCorrectly()
        {
            var go = new GameObject("RotGO");
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "localRotation", new List<object> { 45.0, 90.0, 0.0 } }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(45f, go.transform.localEulerAngles.x, 0.001f);
            Assert.AreEqual(90f, go.transform.localEulerAngles.y, 0.001f);
            Assert.AreEqual(0f, go.transform.localEulerAngles.z, 0.001f);
        }

        [Test]
        public void Execute_SetLocalScale_SetsCorrectly()
        {
            var go = new GameObject("ScaleGO");
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "localScale", new List<object> { 2.0, 2.0, 2.0 } }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(2f, go.transform.localScale.x, 0.001f);
            Assert.AreEqual(2f, go.transform.localScale.y, 0.001f);
            Assert.AreEqual(2f, go.transform.localScale.z, 0.001f);
        }

        [Test]
        public void Execute_SetMultipleProperties_SetsAll()
        {
            var go = new GameObject("MultiGO");
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "localPosition", new List<object> { 1.0, 2.0, 3.0 } },
                { "localScale", new List<object> { 2.0, 2.0, 2.0 } }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(1f, go.transform.localPosition.x, 0.001f);
            Assert.AreEqual(2f, go.transform.localPosition.y, 0.001f);
            Assert.AreEqual(3f, go.transform.localPosition.z, 0.001f);
            Assert.AreEqual(2f, go.transform.localScale.x, 0.001f);
            Assert.AreEqual(2f, go.transform.localScale.y, 0.001f);
            Assert.AreEqual(2f, go.transform.localScale.z, 0.001f);
        }

        [Test]
        public void Execute_NoPropertyParams_ReturnsError()
        {
            var go = new GameObject("NoPropGO");
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path }
            }).Result;

            Assert.IsTrue(result.IsError, "Should return error when no transform properties are provided");
        }

        [Test]
        public void Execute_NonRectTransform_WithRTParams_ReturnsError()
        {
            var go = new GameObject("NonRTGO");
            _created.Add(go);

            var path = HierarchyToolTestHelper.GetGameObjectPath(go);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", path },
                { "anchoredPosition", new List<object> { 10.0, 20.0 } }
            }).Result;

            Assert.IsTrue(result.IsError, "Should return error when passing RT params to non-RectTransform GO");
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
                { "localPosition", new List<object> { 5.0, 6.0, 7.0 } }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("name"), "JSON should contain 'name'");
            Assert.IsTrue(json.ContainsKey("path"), "JSON should contain 'path'");
            Assert.IsTrue(json.ContainsKey("localPosition"), "JSON should contain 'localPosition'");
            Assert.IsTrue(json.ContainsKey("localEulerAngles"), "JSON should contain 'localEulerAngles'");
            Assert.IsTrue(json.ContainsKey("localScale"), "JSON should contain 'localScale'");

            Assert.AreEqual("JSONCheck", json["name"]);
            Assert.AreEqual(expectedPath, json["path"]);
        }

        [Test]
        public void Execute_NonExistentGO_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", "/NonExistentObject" },
                { "localPosition", new List<object> { 1.0, 2.0, 3.0 } }
            }).Result;

            Assert.IsTrue(result.IsError, "Should return error for non-existent GO");
        }
    }
}
