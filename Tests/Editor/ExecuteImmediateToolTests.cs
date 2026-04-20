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
            Assert.IsTrue(json.ContainsKey("warning"), "Response should contain 'warning' key");
            Assert.AreEqual("", (string)json["warning"]);
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
            Assert.IsTrue(json.ContainsKey("warning"), "Response should contain 'warning' key");
            Assert.AreEqual("", (string)json["warning"]);
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
            Assert.IsTrue(json.ContainsKey("warning"), "Response should contain 'warning' key");
            Assert.AreEqual("", (string)json["warning"]);
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
            Assert.IsTrue(json.ContainsKey("warning"), "Response should contain 'warning' key");
            Assert.IsNotEmpty((string)json["warning"], "Default mode (mainThread) should include a warning");
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
            Assert.IsTrue(json.ContainsKey("warning"), "Response should contain 'warning' key");
            Assert.IsNotEmpty((string)json["warning"], "Default mode (mainThread) should include a warning");
        }

        [Test]
        public void InputSchema_HasMainThreadOptionalBooleanParameter()
        {
            var schema = MiniJson.Deserialize(_tool.InputSchema) as Dictionary<string, object>;
            var props = schema["properties"] as Dictionary<string, object>;
            Assert.IsTrue(props.ContainsKey("mainThread"), "properties should contain 'mainThread'");

            var mtProp = props["mainThread"] as Dictionary<string, object>;
            Assert.AreEqual("boolean", mtProp["type"]);

            var required = schema["required"] as List<object>;
            Assert.IsFalse(required.Contains("mainThread"), "mainThread should NOT be in required array");
        }

        [Test]
        public void Execute_ExplicitMainThreadTrue_BehavesLikeDefault()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var code = @"
using System;
public class Test
{
    public static void Run()
    {
        Console.WriteLine(""explicit_true"");
    }
}";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", code },
                { "mainThread", true }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.AreEqual(true, json["success"]);
            Assert.IsTrue(((string)json["output"]).Contains("explicit_true"));
            Assert.IsNotEmpty((string)json["warning"], "mainThread:true should include a warning");
        }

        [Test]
        public void Execute_MainThread_ExceptionContainsStackTrace()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var code = @"
using System;
public class Test
{
    public static void Run()
    {
        Console.Write(""partial"");
        throw new InvalidOperationException(""main_thread_boom"");
    }
}";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", code },
                { "mainThread", true }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.AreEqual(false, json["success"]);
            var error = (string)json["error"];
            Assert.IsTrue(error.Contains("main_thread_boom"), "error should contain exception message");
            Assert.IsTrue(error.Contains("StackTrace") || error.Contains("at "), "error should contain stack trace");
            Assert.IsTrue(((string)json["output"]).Contains("partial"), "output should contain partial output before exception");
        }

        [Test]
        public void Execute_BackgroundModeFalse_SuccessWithEmptyWarning()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var code = @"
using System;
public class Test
{
    public static void Run()
    {
        Console.WriteLine(""bg_output"");
    }
}";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", code },
                { "mainThread", false }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.AreEqual(true, json["success"]);
            Assert.IsTrue(((string)json["output"]).Contains("bg_output"));
            Assert.AreEqual("", (string)json["warning"], "Background mode should have empty warning");
        }

        [Test]
        public void Execute_ToggleOff_RejectsMainThreadTrue()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "public class T { public static void Run() {} }" },
                { "mainThread", true }
            }).Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.ToLower().Contains("disabled"));
        }

        [Test]
        public void Execute_ToggleOff_RejectsMainThreadFalse()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "public class T { public static void Run() {} }" },
                { "mainThread", false }
            }).Result;
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.Content[0].Text.ToLower().Contains("disabled"));
        }

        [Test]
        public void Execute_CompilationError_SameForBothModes()
        {
            EditorPrefs.SetBool(PrefKey, true);
            var invalidCode = "this is not valid csharp {{{";

            var resultMain = _tool.Execute(new Dictionary<string, object>
            {
                { "code", invalidCode },
                { "mainThread", true }
            }).Result;
            var resultBg = _tool.Execute(new Dictionary<string, object>
            {
                { "code", invalidCode },
                { "mainThread", false }
            }).Result;

            var jsonMain = MiniJson.Deserialize(resultMain.Content[0].Text) as Dictionary<string, object>;
            var jsonBg = MiniJson.Deserialize(resultBg.Content[0].Text) as Dictionary<string, object>;

            Assert.AreEqual(false, jsonMain["success"]);
            Assert.AreEqual(false, jsonBg["success"]);
            Assert.AreEqual((string)jsonMain["error"], (string)jsonBg["error"],
                "Both modes should produce the same compilation error");
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
                { "code", code },
                { "mainThread", false }
            }).Result;
            Assert.IsFalse(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("timed out"));
            Assert.IsTrue(json.ContainsKey("warning"), "Response should contain 'warning' key");
            Assert.AreEqual("", (string)json["warning"], "Background mode should have empty warning");
        }
    }
}
#endif
