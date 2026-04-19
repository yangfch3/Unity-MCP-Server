using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ReparentGameObjectTool 单元测试。
    /// </summary>
    public class ReparentGameObjectToolTests
    {
        private ReparentGameObjectTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new ReparentGameObjectTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        [Test]
        public void Name_IsEditorReparentGameObject()
        {
            Assert.AreEqual("editor_reparentGameObject", _tool.Name);
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
            Assert.IsTrue(properties.ContainsKey("newParentInstanceID"), "InputSchema should contain 'newParentInstanceID'");
            Assert.IsTrue(properties.ContainsKey("newParentPath"), "InputSchema should contain 'newParentPath'");
            Assert.IsTrue(properties.ContainsKey("worldPositionStays"), "InputSchema should contain 'worldPositionStays'");
        }

        [Test]
        public void Execute_ReparentByPath_MovesToNewParent()
        {
            var go = new GameObject("ReparentChild");
            _created.Add(go);
            var newParent = new GameObject("NewParent");
            _created.Add(newParent);

            var goPath = HierarchyToolTestHelper.GetGameObjectPath(go);
            var parentPath = HierarchyToolTestHelper.GetGameObjectPath(newParent);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", goPath },
                { "newParentPath", parentPath }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(newParent.transform, go.transform.parent);
        }

        [Test]
        public void Execute_ReparentByInstanceID_MovesToNewParent()
        {
            var go = new GameObject("ReparentByID");
            _created.Add(go);
            var newParent = new GameObject("NewParentByID");
            _created.Add(newParent);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)go.GetInstanceID() },
                { "newParentInstanceID", (long)newParent.GetInstanceID() }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(newParent.transform, go.transform.parent);
        }

        [Test]
        public void Execute_MoveToRoot_SetsParentNull()
        {
            var parent = new GameObject("TempParent");
            _created.Add(parent);
            var go = new GameObject("MoveToRoot");
            go.transform.SetParent(parent.transform);
            _created.Add(go);

            // Reparent with no newParent params → should move to scene root (parent = null)
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)go.GetInstanceID() }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNull(go.transform.parent, "Parent should be null after moving to root");
        }

        [Test]
        public void Execute_WorldPositionStays_True_PreservesWorldPosition()
        {
            var newParent = new GameObject("WPSParent");
            newParent.transform.position = new Vector3(10f, 20f, 30f);
            _created.Add(newParent);

            var go = new GameObject("WPSChild");
            go.transform.position = new Vector3(5f, 5f, 5f);
            _created.Add(go);

            var worldPosBefore = go.transform.position;

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)go.GetInstanceID() },
                { "newParentInstanceID", (long)newParent.GetInstanceID() },
                { "worldPositionStays", true }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(newParent.transform, go.transform.parent);
            Assert.That(go.transform.position.x, Is.EqualTo(worldPosBefore.x).Within(0.001f));
            Assert.That(go.transform.position.y, Is.EqualTo(worldPosBefore.y).Within(0.001f));
            Assert.That(go.transform.position.z, Is.EqualTo(worldPosBefore.z).Within(0.001f));
        }

        [Test]
        public void Execute_WorldPositionStays_False_PreservesLocalPosition()
        {
            var newParent = new GameObject("WPSFalseParent");
            newParent.transform.position = new Vector3(10f, 20f, 30f);
            _created.Add(newParent);

            var go = new GameObject("WPSFalseChild");
            go.transform.position = new Vector3(5f, 5f, 5f);
            _created.Add(go);

            // Before reparent, localPosition == position (no parent)
            var localPosBefore = go.transform.localPosition;

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)go.GetInstanceID() },
                { "newParentInstanceID", (long)newParent.GetInstanceID() },
                { "worldPositionStays", false }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.AreEqual(newParent.transform, go.transform.parent);
            // worldPositionStays=false → localPosition stays the same
            Assert.That(go.transform.localPosition.x, Is.EqualTo(localPosBefore.x).Within(0.001f));
            Assert.That(go.transform.localPosition.y, Is.EqualTo(localPosBefore.y).Within(0.001f));
            Assert.That(go.transform.localPosition.z, Is.EqualTo(localPosBefore.z).Within(0.001f));
        }

        [Test]
        public void Execute_ReturnsCorrectJSON()
        {
            var newParent = new GameObject("JSONParent");
            _created.Add(newParent);
            var go = new GameObject("JSONChild");
            _created.Add(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)go.GetInstanceID() },
                { "newParentInstanceID", (long)newParent.GetInstanceID() }
            }).Result;

            Assert.IsFalse(result.IsError);

            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.IsTrue(json.ContainsKey("name"), "JSON should contain 'name'");
            Assert.IsTrue(json.ContainsKey("path"), "JSON should contain 'path'");
            Assert.IsTrue(json.ContainsKey("instanceID"), "JSON should contain 'instanceID'");

            Assert.AreEqual("JSONChild", json["name"]);
            // After reparent, path should reflect new parent
            var expectedPath = HierarchyToolTestHelper.GetGameObjectPath(go);
            Assert.AreEqual(expectedPath, json["path"]);
            Assert.AreEqual((long)go.GetInstanceID(), json["instanceID"]);
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
        public void Execute_NonExistentNewParent_ReturnsError()
        {
            var go = new GameObject("OrphanChild");
            _created.Add(go);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "instanceID", (long)go.GetInstanceID() },
                { "newParentPath", "/NonExistentParent" }
            }).Result;

            Assert.IsTrue(result.IsError);
        }
    }
}
