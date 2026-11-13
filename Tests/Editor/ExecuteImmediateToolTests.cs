#if !UNITY_6000_OR_NEWER
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
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
        private const string PrefKey = ExecuteImmediateTool.DisablePrefKey;

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
            EditorPrefs.SetBool(PrefKey, false);
            var result = _tool.Execute(new Dictionary<string, object>()).Result;
            Assert.IsTrue(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("missing"));
            Assert.IsFalse(json.ContainsKey("warning"));
        }

        [Test]
        public void Execute_ToggleOff_ReturnsError()
        {
            EditorPrefs.SetBool(PrefKey, true);
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
            EditorPrefs.SetBool(PrefKey, false);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "invalid c# code {" }
            }).Result;
            Assert.IsTrue(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            var error = (string)json["error"];
            // Compiler errors should include line number info like "(1,..."
            Assert.IsTrue(error.Contains("(") && error.Contains(","), "Error should contain line number info");
            Assert.IsFalse(json.ContainsKey("warning"));
        }

        [Test]
        public void Execute_NoRunMethod_ReturnsError()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "class Foo { }" }
            }).Result;
            Assert.IsTrue(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("No entry point found"));
            Assert.IsFalse(json.ContainsKey("warning"));
        }

        [Test]
        public void Execute_SuccessfulExecution_ReturnsOutput()
        {
            EditorPrefs.SetBool(PrefKey, false);
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
            Assert.IsFalse(json.ContainsKey("warning"));
        }

        [Test]
        public void Execute_RuntimeException_ReturnsErrorAndPartialOutput()
        {
            EditorPrefs.SetBool(PrefKey, false);
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
            Assert.IsTrue(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["output"]).Contains("before"));
            Assert.IsTrue(((string)json["error"]).Contains("boom"));
            Assert.IsFalse(json.ContainsKey("warning"));
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
            EditorPrefs.SetBool(PrefKey, false);
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
            Assert.IsFalse(json.ContainsKey("error"), "success response should omit empty error");
        }

        [Test]
        public void Execute_MainThread_ExceptionContainsStackTrace()
        {
            EditorPrefs.SetBool(PrefKey, false);
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
            Assert.IsTrue(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.AreEqual(false, json["success"]);
            var error = (string)json["error"];
            Assert.IsTrue(error.Contains("main_thread_boom"), "error should contain exception message");
            Assert.IsTrue(error.Contains("StackTrace") || error.Contains("at "), "error should contain stack trace");
            StringAssert.DoesNotContain("[0x", error, "no-symbol IL offset suffix should be stripped");
            Assert.IsTrue(((string)json["output"]).Contains("partial"), "output should contain partial output before exception");
        }

        [Test]
        public void Execute_BackgroundModeFalse_SuccessWithoutWarning()
        {
            EditorPrefs.SetBool(PrefKey, false);
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
            Assert.IsFalse(json.ContainsKey("warning"));
        }

        [Test]
        public void Execute_ToggleOff_RejectsMainThreadTrue()
        {
            EditorPrefs.SetBool(PrefKey, true);
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
            EditorPrefs.SetBool(PrefKey, true);
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
            EditorPrefs.SetBool(PrefKey, false);
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

        // --- 跨帧模式（IEnumerator Run）---

        /// <summary>等待跨帧 Task 完成的最大帧数，防止断言前死等。</summary>
        private const int GuardFrames = 3000;

        /// <summary>驱动跨帧 Task 直到完成，返回解析后的响应 JSON。</summary>
        private static IEnumerator DriveUntilComplete(Task<ToolResult> task)
        {
            int guard = 0;
            while (!task.IsCompleted && guard++ < GuardFrames)
                yield return null;
        }

        private static Dictionary<string, object> ParseResult(Task<ToolResult> task)
        {
            Assert.IsTrue(task.IsCompleted, "cross-frame execution should complete");
            var json = MiniJson.Deserialize(task.Result.Content[0].Text) as Dictionary<string, object>;
            Assert.AreEqual(!(bool)json["success"], task.Result.IsError, "isError should mirror success");
            return json;
        }

        public static void ThrowForFormatTest()
        {
            throw new System.InvalidOperationException("format_boom");
        }

        [Test]
        public void FormatError_TrimsTrailingReflectionFrames()
        {
            System.Exception inner = null;
            try
            {
                typeof(ExecuteImmediateToolTests).GetMethod(nameof(ThrowForFormatTest)).Invoke(null, null);
            }
            catch (System.Reflection.TargetInvocationException ex)
            {
                inner = ex.InnerException;
            }

            Assert.IsNotNull(inner);
            StringAssert.Contains("System.Reflection.", inner.StackTrace, "precondition: raw stack has reflection frames");
            var formatted = ExecuteImmediateTool.FormatError(inner);
            StringAssert.StartsWith("format_boom\n", formatted);
            StringAssert.Contains(nameof(ThrowForFormatTest), formatted);
            StringAssert.DoesNotContain("System.Reflection.", formatted);
            StringAssert.DoesNotContain("\r", formatted);
        }

        [Test]
        public void InputSchema_MentionsIEnumeratorEntryPoint()
        {
            var schema = MiniJson.Deserialize(_tool.InputSchema) as Dictionary<string, object>;
            var props = schema["properties"] as Dictionary<string, object>;
            var codeProp = props["code"] as Dictionary<string, object>;
            Assert.IsTrue(((string)codeProp["description"]).Contains("IEnumerator Run()"),
                "code description should document the IEnumerator entry point");
        }

        [Test]
        public void Execute_NoRunMethod_ErrorMentionsBothSignatures()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "class Foo { }" }
            }).Result;
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            var error = (string)json["error"];
            Assert.IsTrue(error.Contains("void Run()"), "error should mention void Run()");
            Assert.IsTrue(error.Contains("IEnumerator Run()"), "error should mention IEnumerator Run()");
        }

        [Test]
        public void Execute_IEnumeratorWithBackgroundMode_IsRejected()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var code = @"
using System.Collections;
public class Test
{
    public static IEnumerator Run() { yield return null; }
}";
            var result = _tool.Execute(new Dictionary<string, object>
            {
                { "code", code },
                { "mainThread", false }
            }).Result;
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("mainThread:true"),
                "IEnumerator entry with mainThread:false should be rejected");
        }

        [UnityTest]
        public IEnumerator Execute_CrossFrame_CapturesOutputAndReportsFrames()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var code = @"
using System;
using System.Collections;
public class Test
{
    public static IEnumerator Run()
    {
        Console.WriteLine(""first"");
        for (int i = 0; i < 5; i++) yield return null;
        Console.WriteLine(""last"");
    }
}";
            var task = _tool.Execute(new Dictionary<string, object> { { "code", code } });
            yield return DriveUntilComplete(task);

            var json = ParseResult(task);
            Assert.AreEqual(true, json["success"], "Error: " + (json.ContainsKey("error") ? json["error"] : "N/A"));
            var output = (string)json["output"];
            Assert.IsTrue(output.Contains("first") && output.Contains("last"),
                "output should be captured across frames, got: " + output);
            Assert.GreaterOrEqual(System.Convert.ToInt32(json["frames"]), 5,
                "should report at least the 5 yielded frames");
            Assert.IsTrue(json.ContainsKey("elapsedMs"));
            Assert.IsFalse(json.ContainsKey("warning"));
        }

        [UnityTest]
        public IEnumerator Execute_CrossFrame_NestedCoroutineRuns()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var code = @"
using System;
using System.Collections;
public class Test
{
    private static int _inner;

    public static IEnumerator Run()
    {
        yield return Inner();
        Console.WriteLine(""outer resumed, innerTicks="" + _inner);
    }

    private static IEnumerator Inner()
    {
        for (int i = 0; i < 4; i++) { _inner++; yield return null; }
        Console.WriteLine(""inner done"");
    }
}";
            var task = _tool.Execute(new Dictionary<string, object> { { "code", code } });
            yield return DriveUntilComplete(task);

            var json = ParseResult(task);
            Assert.AreEqual(true, json["success"], "Error: " + (json.ContainsKey("error") ? json["error"] : "N/A"));
            var output = (string)json["output"];
            Assert.IsTrue(output.Contains("inner done"), "nested coroutine must actually run");
            Assert.IsTrue(output.Contains("innerTicks=4"),
                "nested coroutine must run to completion before parent resumes, got: " + output);
            Assert.Less(output.IndexOf("inner done"), output.IndexOf("outer resumed"),
                "inner should finish before outer resumes");
        }

        [UnityTest]
        public IEnumerator Execute_CrossFrame_Timeout_KeepsPartialOutput()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var code = @"
using System;
using System.Collections;
public class Test
{
    public static IEnumerator Run()
    {
        Console.WriteLine(""before hang"");
        while (true) yield return null;
    }
}";
            var task = _tool.Execute(new Dictionary<string, object>
            {
                { "code", code },
                { "timeout", 1000L }
            });
            yield return DriveUntilComplete(task);

            var json = ParseResult(task);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("timed out"),
                "should report a timeout, got: " + json["error"]);
            Assert.IsTrue(((string)json["output"]).Contains("before hang"),
                "partial output before the hang should be preserved");
        }

        [UnityTest]
        public IEnumerator Execute_CrossFrame_ExceptionReportsStackTrace()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var code = @"
using System;
using System.Collections;
public class Test
{
    public static IEnumerator Run()
    {
        Console.WriteLine(""before boom"");
        yield return null;
        throw new InvalidOperationException(""cross_frame_boom"");
    }
}";
            var task = _tool.Execute(new Dictionary<string, object> { { "code", code } });
            yield return DriveUntilComplete(task);

            var json = ParseResult(task);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("cross_frame_boom"));
            Assert.IsTrue(((string)json["output"]).Contains("before boom"),
                "output before the exception should be preserved");
        }

        [UnityTest]
        public IEnumerator Execute_WhileCrossFrameRunning_SecondCallIsRejected()
        {
            EditorPrefs.SetBool(PrefKey, false);
            var longRunning = @"
using System.Collections;
public class Test
{
    public static IEnumerator Run()
    {
        for (int i = 0; i < 20; i++) yield return null;
    }
}";
            var first = _tool.Execute(new Dictionary<string, object> { { "code", longRunning } });

            // 第一个尚未完成时插入第二个调用，应被占用标记拒绝
            var second = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "public class Other { public static void Run() {} }" }
            });

            var secondJson = MiniJson.Deserialize(second.Result.Content[0].Text) as Dictionary<string, object>;
            Assert.AreEqual(false, secondJson["success"]);
            Assert.IsTrue(((string)secondJson["error"]).Contains("cross-frame execution"),
                "second call during a cross-frame run should be rejected, got: " + secondJson["error"]);

            yield return DriveUntilComplete(first);
            Assert.AreEqual(true, ParseResult(first)["success"]);

            // 占用释放后应恢复正常
            var third = _tool.Execute(new Dictionary<string, object>
            {
                { "code", "using System; public class Other { public static void Run() { Console.WriteLine(\"ok\"); } }" }
            });
            var thirdJson = MiniJson.Deserialize(third.Result.Content[0].Text) as Dictionary<string, object>;
            Assert.AreEqual(true, thirdJson["success"], "should recover after the cross-frame run finishes");
        }

        [Test, Timeout(20000)]
        public void Execute_Timeout_ReturnsTimeoutError()
        {
            EditorPrefs.SetBool(PrefKey, false);
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
            Assert.IsTrue(result.IsError);
            var json = MiniJson.Deserialize(result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.AreEqual(false, json["success"]);
            Assert.IsTrue(((string)json["error"]).Contains("timed out"));
            Assert.IsFalse(json.ContainsKey("warning"));
        }
    }
}
#endif
