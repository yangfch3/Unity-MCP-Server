using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// FindGameObjectsTool 单元测试。
    /// </summary>
    public class FindGameObjectsToolTests
    {
        private FindGameObjectsTool _tool;
        private List<GameObject> _created;

        [SetUp]
        public void SetUp()
        {
            _tool = new FindGameObjectsTool();
            _created = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            HierarchyToolTestHelper.CleanupGameObjects(_created);
        }

        // ── 辅助方法 ──

        private GameObject Create(string name, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent);
            _created.Add(go);
            return go;
        }

        private Dictionary<string, object> Exec(Dictionary<string, object> args)
        {
            var result = _tool.Execute(args).Result;
            Assert.IsFalse(result.IsError, "Expected success but got error: " +
                (result.Content.Count > 0 ? result.Content[0].Text : ""));
            return MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
        }

        private ToolResult ExecRaw(Dictionary<string, object> args)
        {
            return _tool.Execute(args).Result;
        }

        private List<object> GetResults(Dictionary<string, object> json)
        {
            return json["results"] as List<object>;
        }

        // ══════════════════════════════════════════════
        // 1. 元数据断言
        // ══════════════════════════════════════════════

        [Test]
        public void Name_IsEditorFindGameObjects()
        {
            Assert.AreEqual("editor_findGameObjects", _tool.Name);
        }

        [Test]
        public void Category_IsEditor()
        {
            Assert.AreEqual("editor", _tool.Category);
        }

        // ══════════════════════════════════════════════
        // 2. InputSchema 结构验证
        // ══════════════════════════════════════════════

        [Test]
        public void InputSchema_ContainsExpectedProperties()
        {
            var schema = MiniJson.Deserialize(_tool.InputSchema) as Dictionary<string, object>;
            Assert.IsNotNull(schema);

            var properties = schema["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(properties);
            Assert.IsTrue(properties.ContainsKey("namePattern"), "Missing namePattern");
            Assert.IsTrue(properties.ContainsKey("componentType"), "Missing componentType");
            Assert.IsTrue(properties.ContainsKey("maxResults"), "Missing maxResults");
            Assert.IsTrue(properties.ContainsKey("activeOnly"), "Missing activeOnly");
        }

        // ══════════════════════════════════════════════
        // 3. 参数校验
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_NoParams_ReturnsError()
        {
            var result = ExecRaw(new Dictionary<string, object>());
            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_EmptyNamePatternAndComponentType_ReturnsError()
        {
            var result = ExecRaw(new Dictionary<string, object>
            {
                { "namePattern", "" },
                { "componentType", "" }
            });
            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_MaxResults_Zero_ReturnsError()
        {
            var result = ExecRaw(new Dictionary<string, object>
            {
                { "namePattern", "test" },
                { "maxResults", 0L }
            });
            Assert.IsTrue(result.IsError);
        }

        [Test]
        public void Execute_MaxResults_Negative_ReturnsError()
        {
            var result = ExecRaw(new Dictionary<string, object>
            {
                { "namePattern", "test" },
                { "maxResults", -5L }
            });
            Assert.IsTrue(result.IsError);
        }

        // ══════════════════════════════════════════════
        // 4. 名称搜索
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_NameSubstring_MatchesWithoutWildcard()
        {
            Create("FooBarBaz");
            Create("Other");

            var json = Exec(new Dictionary<string, object> { { "namePattern", "Bar" } });
            var results = GetResults(json);

            Assert.AreEqual(1, results.Count);
            var item = results[0] as Dictionary<string, object>;
            Assert.AreEqual("FooBarBaz", item["name"]);
        }

        [Test]
        public void Execute_NameWildcard_Star_Matches()
        {
            Create("PlayerShip");
            Create("EnemyShip");
            Create("Asteroid");

            var json = Exec(new Dictionary<string, object> { { "namePattern", "*Ship" } });
            var results = GetResults(json);

            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void Execute_NameWildcard_QuestionMark_Matches()
        {
            Create("Cat");
            Create("Car");
            Create("Cart");

            var json = Exec(new Dictionary<string, object> { { "namePattern", "Ca?" } });
            var results = GetResults(json);

            // "Cat" and "Car" match "Ca?", "Cart" does not (4 chars vs 3)
            Assert.AreEqual(2, results.Count);
        }

        [Test]
        public void Execute_NameSearch_CaseInsensitive()
        {
            Create("MyCamera");

            var json = Exec(new Dictionary<string, object> { { "namePattern", "mycamera" } });
            var results = GetResults(json);

            Assert.GreaterOrEqual(results.Count, 1);
            var item = results[0] as Dictionary<string, object>;
            Assert.AreEqual("MyCamera", item["name"]);
        }

        // ══════════════════════════════════════════════
        // 5. 组件搜索
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_ComponentType_FiltersCorrectly()
        {
            var go1 = Create("WithCollider");
            go1.AddComponent<BoxCollider>();
            Create("WithoutCollider");

            var json = Exec(new Dictionary<string, object> { { "componentType", "BoxCollider" } });
            var results = GetResults(json);

            Assert.AreEqual(1, results.Count);
            var item = results[0] as Dictionary<string, object>;
            Assert.AreEqual("WithCollider", item["name"]);
        }

        [Test]
        public void Execute_ComponentType_CaseInsensitive()
        {
            var go = Create("LightObj");
            go.AddComponent<Light>();

            var json = Exec(new Dictionary<string, object> { { "componentType", "light" } });
            var results = GetResults(json);

            Assert.GreaterOrEqual(results.Count, 1);
            var found = false;
            foreach (var r in results)
            {
                var d = r as Dictionary<string, object>;
                if ((string)d["name"] == "LightObj") { found = true; break; }
            }
            Assert.IsTrue(found, "Should find LightObj with case-insensitive component search");
        }

        // ══════════════════════════════════════════════
        // 6. 组合搜索（AND 语义）
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_CombinedSearch_AND_Semantics()
        {
            var go1 = Create("EnemyA");
            go1.AddComponent<BoxCollider>();
            var go2 = Create("EnemyB"); // no collider
            var go3 = Create("FriendA");
            go3.AddComponent<BoxCollider>();

            var json = Exec(new Dictionary<string, object>
            {
                { "namePattern", "Enemy" },
                { "componentType", "BoxCollider" }
            });
            var results = GetResults(json);

            Assert.AreEqual(1, results.Count);
            var item = results[0] as Dictionary<string, object>;
            Assert.AreEqual("EnemyA", item["name"]);
        }

        // ══════════════════════════════════════════════
        // 7. 结果限制
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_MaxResults_TruncatesResults()
        {
            for (int i = 0; i < 5; i++)
                Create($"Item{i}");

            var json = Exec(new Dictionary<string, object>
            {
                { "namePattern", "Item" },
                { "maxResults", 2L }
            });
            var results = GetResults(json);

            Assert.AreEqual(2, results.Count);
            Assert.AreEqual(true, json["truncated"]);
            Assert.AreEqual((long)5, json["totalFound"]);
        }

        [Test]
        public void Execute_MaxResults_NotTruncated_NoTruncatedField()
        {
            Create("OnlyOne");

            var json = Exec(new Dictionary<string, object>
            {
                { "namePattern", "OnlyOne" },
                { "maxResults", 10L }
            });
            var results = GetResults(json);

            Assert.AreEqual(1, results.Count);
            Assert.IsFalse(json.ContainsKey("truncated"), "Should not contain 'truncated' when not truncated");
        }

        // ══════════════════════════════════════════════
        // 8. 空结果
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_NoMatch_ReturnsEmptyResultsNotError()
        {
            var json = Exec(new Dictionary<string, object>
            {
                { "namePattern", "NonExistentXYZ_12345" }
            });
            var results = GetResults(json);

            Assert.AreEqual(0, results.Count);
            Assert.AreEqual((long)0, json["count"]);
        }

        // ══════════════════════════════════════════════
        // 9. 递归搜索
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_RecursiveSearch_FindsDeeplyNestedGO()
        {
            var root = Create("RecRoot");
            var child = Create("RecChild", root.transform);
            var grandchild = Create("RecGrandchild", child.transform);
            var deep = Create("RecDeepTarget", grandchild.transform);

            var json = Exec(new Dictionary<string, object> { { "namePattern", "RecDeepTarget" } });
            var results = GetResults(json);

            Assert.AreEqual(1, results.Count);
            var item = results[0] as Dictionary<string, object>;
            Assert.AreEqual("RecDeepTarget", item["name"]);
            Assert.AreEqual("/RecRoot/RecChild/RecGrandchild/RecDeepTarget", item["path"]);
        }

        // ══════════════════════════════════════════════
        // 10. activeOnly 过滤
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_ActiveOnly_True_SkipsInactiveGO()
        {
            var active = Create("ActiveGO");
            var inactive = Create("InactiveGO");
            inactive.SetActive(false);

            var json = Exec(new Dictionary<string, object>
            {
                { "namePattern", "GO" },
                { "activeOnly", true }
            });
            var results = GetResults(json);

            // Only ActiveGO should be found
            Assert.AreEqual(1, results.Count);
            var item = results[0] as Dictionary<string, object>;
            Assert.AreEqual("ActiveGO", item["name"]);
        }

        [Test]
        public void Execute_ActiveOnly_False_IncludesInactiveGO()
        {
            var active = Create("VisibleGO");
            var inactive = Create("HiddenGO");
            inactive.SetActive(false);

            var json = Exec(new Dictionary<string, object>
            {
                { "namePattern", "GO" },
                { "activeOnly", false }
            });
            var results = GetResults(json);

            // Both should be found
            Assert.GreaterOrEqual(results.Count, 2);
            var names = new List<string>();
            foreach (var r in results)
            {
                var d = r as Dictionary<string, object>;
                names.Add((string)d["name"]);
            }
            Assert.IsTrue(names.Contains("VisibleGO"), "Should contain active GO");
            Assert.IsTrue(names.Contains("HiddenGO"), "Should contain inactive GO");
        }

        [Test]
        public void Execute_ActiveOnly_Default_IsTrue()
        {
            var active = Create("DefaultActiveGO");
            var inactive = Create("DefaultInactiveGO");
            inactive.SetActive(false);

            // 不传 activeOnly，默认应为 true
            var json = Exec(new Dictionary<string, object>
            {
                { "namePattern", "Default" }
            });
            var results = GetResults(json);

            var names = new List<string>();
            foreach (var r in results)
            {
                var d = r as Dictionary<string, object>;
                names.Add((string)d["name"]);
            }
            Assert.IsTrue(names.Contains("DefaultActiveGO"), "Should contain active GO");
            Assert.IsFalse(names.Contains("DefaultInactiveGO"), "Should NOT contain inactive GO by default");
        }

        // ══════════════════════════════════════════════
        // 11. 返回结构
        // ══════════════════════════════════════════════

        [Test]
        public void Execute_ResultItem_ContainsAllRequiredFields()
        {
            var go = Create("StructTestGO");
            go.AddComponent<BoxCollider>();

            var json = Exec(new Dictionary<string, object> { { "namePattern", "StructTestGO" } });
            var results = GetResults(json);

            Assert.AreEqual(1, results.Count);
            var item = results[0] as Dictionary<string, object>;

            // name
            Assert.IsTrue(item.ContainsKey("name"), "Missing 'name'");
            Assert.AreEqual("StructTestGO", item["name"]);

            // path
            Assert.IsTrue(item.ContainsKey("path"), "Missing 'path'");
            Assert.AreEqual("/StructTestGO", item["path"]);

            // instanceID
            Assert.IsTrue(item.ContainsKey("instanceID"), "Missing 'instanceID'");
            Assert.AreEqual((long)go.GetInstanceID(), item["instanceID"]);

            // components
            Assert.IsTrue(item.ContainsKey("components"), "Missing 'components'");
            var comps = item["components"] as List<object>;
            Assert.IsNotNull(comps);
            Assert.IsTrue(comps.Contains("Transform"), "Should contain Transform");
            Assert.IsTrue(comps.Contains("BoxCollider"), "Should contain BoxCollider");
        }

        [Test]
        public void Execute_TopLevel_ContainsResultsAndCount()
        {
            Create("CountTestGO");

            var json = Exec(new Dictionary<string, object> { { "namePattern", "CountTestGO" } });

            Assert.IsTrue(json.ContainsKey("results"), "Missing 'results'");
            Assert.IsTrue(json.ContainsKey("count"), "Missing 'count'");
            Assert.AreEqual((long)1, json["count"]);
        }
    }
}
