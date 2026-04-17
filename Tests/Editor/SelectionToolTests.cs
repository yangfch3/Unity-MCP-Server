using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// SelectionTool 测试。
    /// </summary>
    public class SelectionToolTests
    {
        private SelectionTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new SelectionTool();
        }

        [Test]
        public void Execute_NoSelection_ReturnsEmptyLists()
        {
            Selection.activeGameObject = null;
            Selection.objects = new Object[0];

            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);
            var json = result.Content[0].Text;
            Assert.IsTrue(json.Contains("\"gameObjects\":[]") || json.Contains("\"gameObjects\":["));
        }

        [Test]
        public void Execute_WithSelectedGameObject_ReturnsInfo()
        {
            var go = new GameObject("SelTest");
            try
            {
                Selection.activeGameObject = go;
                var result = _tool.Execute(new Dictionary<string, object>()).Result;
                Assert.IsFalse(result.IsError);
                var json = result.Content[0].Text;
                Assert.IsTrue(json.Contains("SelTest"));
            }
            finally
            {
                Selection.activeGameObject = null;
                Object.DestroyImmediate(go);
            }
        }
    }
}
