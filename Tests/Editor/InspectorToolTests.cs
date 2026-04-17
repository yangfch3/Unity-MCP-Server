using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// InspectorTool 测试。
    /// </summary>
    public class InspectorToolTests
    {
        private InspectorTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new InspectorTool();
        }

        [Test]
        public void Execute_NoSelection_ReturnsHint()
        {
            Selection.activeGameObject = null;
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);
            Assert.IsTrue(result.Content[0].Text.Contains("未选中"));
        }

        [Test]
        public void Execute_WithGameObject_ReturnsComponents()
        {
            var go = new GameObject("InspTest");
            go.AddComponent<BoxCollider>();
            try
            {
                Selection.activeGameObject = go;
                var result = _tool.Execute(new Dictionary<string, object>()).Result;
                Assert.IsFalse(result.IsError);
                var json = result.Content[0].Text;
                Assert.IsTrue(json.Contains("InspTest"));
                Assert.IsTrue(json.Contains("Transform"));
                Assert.IsTrue(json.Contains("BoxCollider"));
            }
            finally
            {
                Selection.activeGameObject = null;
                Object.DestroyImmediate(go);
            }
        }
    }
}
