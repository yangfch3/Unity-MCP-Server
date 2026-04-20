#if !UNITY_6000_OR_NEWER
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// ExecuteImmediateTool 单元测试。
    /// </summary>
    public class ExecuteImmediateToolTests
    {
        private ExecuteImmediateTool _tool;
        private bool _originalPrefValue;
        private const string PrefKey = "McpServer_CodeExecuteImmediate";

        [SetUp]
        public void SetUp()
        {
            _tool = new ExecuteImmediateTool();
            _originalPrefValue = EditorPrefs.GetBool(PrefKey, false);
        }

        [TearDown]
        public void TearDown()
        {
            EditorPrefs.SetBool(PrefKey, _originalPrefValue);
        }

        [Test]
        public void NameAndCategory()
        {
            Assert.AreEqual("code_executeImmediate", _tool.Name);
            Assert.AreEqual("code", _tool.Category);
        }

        [Test]
        public void InputSchema_HasRequiredCodeParameter()
        {
            var schema = MiniJson.Deserialize(_tool.InputSchema) as Dictionary<string, object>;
            Assert.IsNotNull(schema);

            var props = schema["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(props);
            Assert.IsTrue(props.ContainsKey("code"));

            var codeProp = props["code"] as Dictionary<string, object>;
            Assert.IsNotNull(codeProp);
            Assert.AreEqual("string", codeProp["type"]);

            var required = schema["required"] as List<object>;
            Assert.IsNotNull(required);
            Assert.Contains("code", required);
        }

        [Test]
        public void ToolRegistry_AutoDiscovers()
        {
            var registry = new ToolRegistry();
            registry.AutoDiscover();
            var tool = registry.Resolve("code_executeImmediate");
            Assert.IsNotNull(tool, "code_executeImmediate should be auto-discovered by ToolRegistry");
        }

        [Test]
        public void Execute_MissingCodeParameter_ReturnsError()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("missing"));
        }

        [Test]
        public void Execute_ToggleOff_ReturnsError()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "class Foo { public static void Run() {} }" }
            }).Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.ToLower().Contains("disabled"));
        }

        [Test]
        public void Execute_CompilationError_ReturnsErrorWithLineNumbers()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "invalid c# code {" }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            var error = (string)json["error"];
            // Compiler errors should include line number info like "(1,..."
            Assert.IsTrue(error.Contains("(") && error.Contains(","), "Error should contain line number info");
        }

        [Test]
        public void Execute_NoRunMethod_ReturnsError()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "class Foo { }" }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("No entry point found"));
        }

        [Test]
        public void Execute_SuccessfulExecution_ReturnsOutput()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var code = @"
using System;
public class Test
{
    public static void Run()
    {
        Console.WriteLine(""hello"");
    }
}";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", code }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(true, json["success"], "success should be true. Error: " + (json.ContainsKey("error") ? json["error"] : "N/A"));
            Assert.IsTrue(((string)json["output"]).Contains("hello"));
        }

        [Test]
        public void Execute_RuntimeException_ReturnsErrorAndPartialOutput()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var code = @"
using System;
public class Test
{
    public static void Run()
    {
        Console.Write(""before"");
        throw new Exception(""boom"");
    }
}";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", code }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["output"]).Contains("before"));
            Assert.IsTrue(((string)json["error"]).Contains("boom"));
        }

        [Test, Timeout(20000)]
        public void Execute_Timeout_ReturnsTimeoutError()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var code = @"
using System;
public class Test
{
    public static void Run()
    {
        while (true) { }
    }
}";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", code }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("timed out"));
        }
    }
}
#endif
