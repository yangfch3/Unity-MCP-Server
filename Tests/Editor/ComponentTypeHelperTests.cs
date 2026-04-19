using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ComponentTypeHelper 单元测试。
    /// </summary>
    public class ComponentTypeHelperTests
    {
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        // ── FindType ──

        [Test]
        public void FindType_ExactName_ReturnsType()
        {
            var result = ComponentTypeHelper.FindType("BoxCollider");
            Assert.AreEqual(typeof(BoxCollider), result);
        }

        [Test]
        public void FindType_CaseInsensitive_ReturnsType()
        {
            var result = ComponentTypeHelper.FindType("boxcollider");
            Assert.AreEqual(typeof(BoxCollider), result);
        }

        [Test]
        public void FindType_MixedCase_ReturnsType()
        {
            var result = ComponentTypeHelper.FindType("BOXCOLLIDER");
            Assert.AreEqual(typeof(BoxCollider), result);
        }

        [Test]
        public void FindType_NonExistent_ReturnsNull()
        {
            var result = ComponentTypeHelper.FindType("NonExistentComponent");
            Assert.IsNull(result);
        }

        [Test]
        public void FindType_EmptyString_ReturnsNull()
        {
            var result = ComponentTypeHelper.FindType("");
            Assert.IsNull(result);
        }

        [Test]
        public void FindType_Null_ReturnsNull()
        {
            var result = ComponentTypeHelper.FindType(null);
            Assert.IsNull(result);
        }

        // ── FindComponent ──

        [Test]
        public void FindComponent_ExactName_ReturnsComponent()
        {
            var go = new GameObject("CompTest");
            _created.Add(go);
            go.AddComponent<BoxCollider>();

            var result = ComponentTypeHelper.FindComponent(go, "BoxCollider");

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<BoxCollider>(result);
        }

        [Test]
        public void FindComponent_CaseInsensitive_ReturnsComponent()
        {
            var go = new GameObject("CompTestCI");
            _created.Add(go);
            go.AddComponent<BoxCollider>();

            var result = ComponentTypeHelper.FindComponent(go, "boxcollider");

            Assert.IsNotNull(result);
            Assert.IsInstanceOf<BoxCollider>(result);
        }

        [Test]
        public void FindComponent_NotFound_ReturnsNull()
        {
            var go = new GameObject("CompTestNF");
            _created.Add(go);

            var result = ComponentTypeHelper.FindComponent(go, "Camera");

            Assert.IsNull(result);
        }

        [Test]
        public void FindComponent_NullGO_ReturnsNull()
        {
            var result = ComponentTypeHelper.FindComponent(null, "BoxCollider");
            Assert.IsNull(result);
        }

        [Test]
        public void FindComponent_EmptyTypeName_ReturnsNull()
        {
            var go = new GameObject("CompTestEmpty");
            _created.Add(go);
            go.AddComponent<BoxCollider>();

            var result = ComponentTypeHelper.FindComponent(go, "");

            Assert.IsNull(result);
        }
    }
}
