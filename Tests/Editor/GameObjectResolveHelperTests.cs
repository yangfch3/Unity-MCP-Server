using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// GameObjectResolveHelper 单元测试。
    /// </summary>
    public class GameObjectResolveHelperTests
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

        [Test]
        public void Resolve_ByInstanceID_ReturnsCorrectGO()
        {
            var go = new GameObject("ResolveById");
            _created.Add(go);

            var (result, error) = GameObjectResolveHelper.Resolve(new Dictionary<string, object>
            {
                { "instanceID", (long)go.GetInstanceID() }
            });

            Assert.IsNull(error);
            Assert.AreEqual(go, result);
        }

        [Test]
        public void Resolve_ByPath_ReturnsCorrectGO()
        {
            var root = new GameObject("PathRoot");
            _created.Add(root);
            var child = new GameObject("PathChild");
            _created.Add(child);
            child.transform.SetParent(root.transform);

            var path = HierarchyToolTestHelper.GetGameObjectPath(child);
            var (result, error) = GameObjectResolveHelper.Resolve(new Dictionary<string, object>
            {
                { "path", path }
            });

            Assert.IsNull(error);
            Assert.AreEqual(child, result);
        }

        [Test]
        public void Resolve_InstanceID_TakesPriorityOverPath()
        {
            var goA = new GameObject("PriorA");
            _created.Add(goA);
            var goB = new GameObject("PriorB");
            _created.Add(goB);

            var (result, error) = GameObjectResolveHelper.Resolve(new Dictionary<string, object>
            {
                { "instanceID", (long)goA.GetInstanceID() },
                { "path", HierarchyToolTestHelper.GetGameObjectPath(goB) }
            });

            Assert.IsNull(error);
            Assert.AreEqual(goA, result);
        }

        [Test]
        public void Resolve_NoParams_ReturnsError()
        {
            var (result, error) = GameObjectResolveHelper.Resolve(new Dictionary<string, object>());

            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.IsTrue(error.Contains("instanceID"), $"Error should mention instanceID, got: {error}");
            Assert.IsTrue(error.Contains("path"), $"Error should mention path, got: {error}");
        }

        [Test]
        public void Resolve_InvalidInstanceID_ReturnsError()
        {
            var (result, error) = GameObjectResolveHelper.Resolve(new Dictionary<string, object>
            {
                { "instanceID", (long)999999999 }
            });

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void Resolve_InvalidPath_ReturnsError()
        {
            var (result, error) = GameObjectResolveHelper.Resolve(new Dictionary<string, object>
            {
                { "path", "/NoSuchRoot/NoSuchChild" }
            });

            Assert.IsNull(result);
            Assert.IsNotNull(error);
        }

        [Test]
        public void Resolve_EmptyStringPath_TreatedAsNull()
        {
            // 空字符串应视为未提供，等同于无参数 → 返回错误
            var (result, error) = GameObjectResolveHelper.Resolve(new Dictionary<string, object>
            {
                { "path", "" }
            });

            Assert.IsNull(result);
            Assert.IsNotNull(error);
            Assert.IsTrue(error.Contains("instanceID"), $"Error should mention instanceID, got: {error}");
            Assert.IsTrue(error.Contains("path"), $"Error should mention path, got: {error}");
        }

        [Test]
        public void Resolve_CustomKeys_Works()
        {
            var parent = new GameObject("CustomKeyParent");
            _created.Add(parent);

            var (result, error) = GameObjectResolveHelper.Resolve(
                new Dictionary<string, object>
                {
                    { "parentInstanceID", (long)parent.GetInstanceID() }
                },
                "parentInstanceID",
                "parentPath");

            Assert.IsNull(error);
            Assert.AreEqual(parent, result);
        }

        [Test]
        public void FindByPath_ValidPath_ReturnsGO()
        {
            var root = new GameObject("FindRoot");
            _created.Add(root);
            var child = new GameObject("FindChild");
            _created.Add(child);
            child.transform.SetParent(root.transform);

            var path = HierarchyToolTestHelper.GetGameObjectPath(child); // "/FindRoot/FindChild"
            var result = GameObjectResolveHelper.FindByPath(path);

            Assert.AreEqual(child, result);
        }

        [Test]
        public void FindByPath_InvalidPath_ReturnsNull()
        {
            var result = GameObjectResolveHelper.FindByPath("/NonExistent/Path");

            Assert.IsNull(result);
        }
    }
}
