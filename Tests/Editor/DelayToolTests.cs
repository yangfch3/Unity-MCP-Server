using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityMcp.Editor;
using UnityMcp.Editor.Tools;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// DelayTool 单元测试。
    /// 使用 [UnityTest] 协程等待 Task.Delay 完成，避免阻塞主线程。
    /// </summary>
    public class DelayToolTests
    {
        private DelayTool _tool;

        /// <summary>等待 Task 完成的最大帧数，防止断言前死等。</summary>
        private const int GuardFrames = 3000;

        [SetUp]
        public void SetUp()
        {
            _tool = new DelayTool();
        }

        [Test]
        public void NameAndCategory()
        {
            Assert.AreEqual("util_delay", _tool.Name);
            Assert.AreEqual("util", _tool.Category);
        }

        [Test]
        public void InputSchema_HasOptionalMsParameter()
        {
            var schema = MiniJson.Deserialize(_tool.InputSchema) as Dictionary<string, object>;
            Assert.IsNotNull(schema);

            var props = schema["properties"] as Dictionary<string, object>;
            Assert.IsNotNull(props);
            Assert.IsTrue(props.ContainsKey("ms"));

            var msProp = props["ms"] as Dictionary<string, object>;
            Assert.AreEqual("integer", msProp["type"]);

            Assert.IsFalse(schema.ContainsKey("required"), "ms should not be required");
        }

        [Test]
        public void ToolRegistry_AutoDiscovers()
        {
            var registry = new ToolRegistry();
            registry.AutoDiscover();
            Assert.IsNotNull(registry.Resolve("util_delay"),
                "util_delay should be auto-discovered by ToolRegistry");
        }

        [UnityTest]
        public IEnumerator Execute_WaitsRequestedDuration()
        {
            var task = _tool.Execute(new Dictionary<string, object> { { "ms", 150L } });

            int guard = 0;
            while (!task.IsCompleted && guard++ < GuardFrames)
                yield return null;

            Assert.IsTrue(task.IsCompleted, "Execute should complete");
            var json = MiniJson.Deserialize(task.Result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsNotNull(json);
            Assert.GreaterOrEqual(System.Convert.ToInt64(json["waitedMs"]), 150L);
        }

        [UnityTest]
        public IEnumerator Execute_BelowMinimum_ClampsToFloor()
        {
            var task = _tool.Execute(new Dictionary<string, object> { { "ms", 5L } });

            int guard = 0;
            while (!task.IsCompleted && guard++ < GuardFrames)
                yield return null;

            Assert.IsTrue(task.IsCompleted, "Execute should complete");
            var json = MiniJson.Deserialize(task.Result.Content[0].Text) as Dictionary<string, object>;
            Assert.GreaterOrEqual(System.Convert.ToInt64(json["waitedMs"]), 100L,
                "ms below 100 should be clamped up to the 100ms floor");
        }

        [UnityTest]
        public IEnumerator Execute_ReportsPlayModeState()
        {
            var task = _tool.Execute(new Dictionary<string, object> { { "ms", 100L } });

            int guard = 0;
            while (!task.IsCompleted && guard++ < GuardFrames)
                yield return null;

            Assert.IsTrue(task.IsCompleted, "Execute should complete");
            var json = MiniJson.Deserialize(task.Result.Content[0].Text) as Dictionary<string, object>;
            Assert.IsTrue(json.ContainsKey("playMode"));
            Assert.Contains((string)json["playMode"], new List<string> { "Stopped", "Paused", "Playing" });
        }

        [UnityTest]
        public IEnumerator Execute_NullParameters_UsesDefault()
        {
            var task = _tool.Execute(null);

            int guard = 0;
            while (!task.IsCompleted && guard++ < GuardFrames)
                yield return null;

            Assert.IsTrue(task.IsCompleted, "Execute should complete");
            Assert.IsFalse(task.Result.IsError);
            var json = MiniJson.Deserialize(task.Result.Content[0].Text) as Dictionary<string, object>;
            Assert.GreaterOrEqual(System.Convert.ToInt64(json["waitedMs"]), 1000L,
                "default should be 1000ms");
        }
    }
}
