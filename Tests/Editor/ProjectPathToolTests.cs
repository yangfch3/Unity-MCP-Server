using System.Collections.Generic;
using NUnit.Framework;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Tests.Editor
{
    /// <summary>
    /// ProjectPathTool 单元测试。
    /// </summary>
    public class ProjectPathToolTests
    {
        private ProjectPathTool _tool;

        [SetUp]
        public void SetUp()
        {
            _tool = new ProjectPathTool();
        }

        [Test]
        public void Name_ReturnsExpected()
        {
            Assert.AreEqual("editor_getProjectPath", _tool.Name);
        }

        [Test]
        public void Category_ReturnsEditor()
        {
            Assert.AreEqual("editor", _tool.Category);
        }

        [Test]
        public void Execute_ReturnsSuccess()
        {
            var result = _tool.Execute(null).Result;
            Assert.IsFalse(result.IsError);
        }

        [Test]
        public void Execute_ContainsProjectPath()
        {
            var result = _tool.Execute(null).Result;
            var text = result.Content[0].Text;
            Assert.IsTrue(text.Contains("\"projectPath\":"));
        }

        [Test]
        public void Execute_ContainsAssetsPath()
        {
            var result = _tool.Execute(null).Result;
            var text = result.Content[0].Text;
            Assert.IsTrue(text.Contains("\"assetsPath\":"));
        }

        [Test]
        public void Execute_PathsUseForwardSlash()
        {
            var result = _tool.Execute(null).Result;
            var text = result.Content[0].Text;
            var json = MiniJson.Deserialize(text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            var projectPath = json["projectPath"] as string;
            Assert.IsNotNull(projectPath);
            Assert.IsFalse(projectPath.Contains("\\"), "projectPath should use forward slashes");
        }

        [Test]
        public void Execute_AssetsPathEndsWithAssets()
        {
            var result = _tool.Execute(null).Result;
            var text = result.Content[0].Text;
            var json = MiniJson.Deserialize(text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            var assetsPath = json["assetsPath"] as string;
            Assert.IsNotNull(assetsPath);
            Assert.IsTrue(assetsPath.EndsWith("/Assets"), "assetsPath should end with /Assets");
        }

        [Test]
        public void Execute_IgnoresParameters()
        {
            var parameters = new Dictionary<string, object> { { "foo", "bar" } };
            var result = _tool.Execute(parameters).Result;
            Assert.IsFalse(result.IsError);
        }
    }
}
