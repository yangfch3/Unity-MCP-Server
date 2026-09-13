using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    public class AssetCreateToolTestScriptableObject : ScriptableObject
    {
    }

    /// <summary>
    /// AssetCreateTool 测试。
    /// </summary>
    public class AssetCreateToolTests
    {
        private const string TestFolder = "Assets/UnityMcpAssetCreateToolTests";

        private AssetCreateTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new AssetCreateTool();

            if (AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.DeleteAsset(TestFolder);

            AssetDatabase.CreateFolder("Assets", "UnityMcpAssetCreateToolTests");
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TestFolder))
                AssetDatabase.DeleteAsset(TestFolder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Test]
        public void NameAndCategory_AreCorrect()
        {
            Assert.AreEqual("asset_create", _tool.Name);
            Assert.AreEqual("editor", _tool.Category);
        }

        [Test]
        public void MissingKind_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "path", TestFolder + "/MissingKind.asset" }
            }).Result;

            Assert.IsTrue(result.IsError);
            StringAssert.Contains("kind parameter is required", result.Content[0].Text);
        }

        [Test]
        public void UnsupportedKind_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "prefab" },
                { "path", TestFolder + "/Prefab.asset" }
            }).Result;

            Assert.IsTrue(result.IsError);
            StringAssert.Contains("unsupported asset kind", result.Content[0].Text);
        }

        [Test]
        public void InvalidAssetPath_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "physicMaterial" },
                { "path", "../Outside.asset" }
            }).Result;

            Assert.IsTrue(result.IsError);
            StringAssert.Contains("path must be an .asset file under the Assets folder", result.Content[0].Text);
        }

        [Test]
        public void MissingParentDirectory_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "physicMaterial" },
                { "path", TestFolder + "/Missing/Material.asset" }
            }).Result;

            Assert.IsTrue(result.IsError);
            StringAssert.Contains("parent directory not found", result.Content[0].Text);
        }

        [Test]
        public void CreateScriptableObject_CreatesAssetAndReturnsMetadata()
        {
            string path = TestFolder + "/Config.asset";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "scriptableObject" },
                { "path", path },
                { "typeName", typeof(AssetCreateToolTestScriptableObject).FullName }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<AssetCreateToolTestScriptableObject>(path));
            StringAssert.Contains("\"assetPath\":\"" + path + "\"", result.Content[0].Text);
            StringAssert.Contains("\"kind\":\"scriptableObject\"", result.Content[0].Text);
            Assert.IsFalse(string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)));
        }

        [Test]
        public void ScriptableObjectWithNonScriptableType_ReturnsError()
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "scriptableObject" },
                { "path", TestFolder + "/InvalidType.asset" },
                { "typeName", typeof(Material).FullName }
            }).Result;

            Assert.IsTrue(result.IsError);
            StringAssert.Contains("type must derive from ScriptableObject", result.Content[0].Text);
        }

        [Test]
        public void CreateMaterial_CreatesAssetWithRequestedShader()
        {
            Shader shader = FindAvailableShader();
            Assert.IsNotNull(shader, "No shader available for AssetCreateTool test");

            string path = TestFolder + "/Material.asset";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "material" },
                { "path", path },
                { "shaderName", shader.name }
            }).Result;

            Assert.IsFalse(result.IsError);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.IsNotNull(material);
            Assert.AreEqual(shader.name, material.shader.name);
        }

        [Test]
        public void CreatePhysicMaterial_CreatesAsset()
        {
            string path = TestFolder + "/PhysicMaterial.asset";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "physicMaterial" },
                { "path", path }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<PhysicMaterial>(path));
        }

        [Test]
        public void CreatePhysicsMaterial2D_CreatesAsset()
        {
            string path = TestFolder + "/PhysicsMaterial2D.asset";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "physicsMaterial2D" },
                { "path", path }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path));
        }

        [Test]
        public void ExistingAsset_DefaultConflictStrategyReturnsError()
        {
            string path = TestFolder + "/Conflict.asset";
            CreatePhysicMaterial(path);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "physicMaterial" },
                { "path", path }
            }).Result;

            Assert.IsTrue(result.IsError);
            StringAssert.Contains("asset already exists", result.Content[0].Text);
        }

        [Test]
        public void ExistingAsset_UniqueConflictStrategyCreatesUniqueAsset()
        {
            string path = TestFolder + "/Unique.asset";
            CreatePhysicMaterial(path);

            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "physicMaterial" },
                { "path", path },
                { "conflictStrategy", "unique" }
            }).Result;

            Assert.IsFalse(result.IsError);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<PhysicMaterial>(TestFolder + "/Unique 1.asset"));
        }

        [Test]
        public void AutoDiscovery_FindsTool()
        {
            var registry = new ToolRegistry();
            registry.AutoDiscover();

            var tool = registry.Resolve("asset_create");
            Assert.IsNotNull(tool);
            Assert.AreEqual("asset_create", tool.Name);
        }

        private void CreatePhysicMaterial(string path)
        {
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "kind", "physicMaterial" },
                { "path", path }
            }).Result;

            Assert.IsFalse(result.IsError);
        }

        private static Shader FindAvailableShader()
        {
            foreach (Shader shader in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (shader != null && Shader.Find(shader.name) != null)
                    return shader;
            }

            return null;
        }
    }
}

