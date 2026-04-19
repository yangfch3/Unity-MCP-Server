using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：运行指定的 Unity Test Runner 测试并返回结果。
    /// </summary>
    public class TestRunnerTool : IMcpTool
    {
        public string Name => "build_runTests";
        public string Category => "build";
        public string Description => "运行 Unity Test Runner 测试并返回结果";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"mode\":{\"type\":\"string\",\"enum\":[\"EditMode\",\"PlayMode\"],\"description\":\"测试模式\",\"default\":\"EditMode\"},\"testFilter\":{\"type\":\"string\",\"description\":\"测试名称过滤（可选）\"}}}";

        public async Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            string mode = "EditMode";
            string testFilter = null;

            if (parameters != null)
            {
                if (parameters.TryGetValue("mode", out var modeRaw) && modeRaw is string m)
                    mode = m;
                if (parameters.TryGetValue("testFilter", out var filterRaw) && filterRaw is string f)
                    testFilter = f;
            }

            TestMode testMode;
            switch (mode)
            {
                case "PlayMode":
                    testMode = TestMode.PlayMode;
                    break;
                default:
                    testMode = TestMode.EditMode;
                    break;
            }

            // PlayMode 下无法运行测试（Test Runner 会挂起等待退出 PlayMode）
            if (EditorApplication.isPlaying)
                return ToolResult.Error("当前处于 PlayMode，请先退出 PlayMode 再运行测试（playmode_control exit）");

            var tcs = new TaskCompletionSource<bool>();
            var results = new List<ITestResultAdaptor>();

            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            var callbacks = new TestCallbacks(results, tcs);
            api.RegisterCallbacks(callbacks);

            var filter = new Filter
            {
                testMode = testMode
            };
            if (!string.IsNullOrEmpty(testFilter))
            {
                filter.testNames = new[] { testFilter };
            }

            var settings = new ExecutionSettings(filter);
            api.Execute(settings);

            // 等待完成或超时（120s）
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(120)));
            api.UnregisterCallbacks(callbacks);
            UnityEngine.Object.DestroyImmediate(api);

            if (completed != tcs.Task)
                return ToolResult.Error("测试运行超时（120 秒）");

            // 汇总结果（results 中已经只有叶子节点）
            int total = 0, passed = 0, failed = 0, skipped = 0;
            var failures = new List<(string name, string message)>();

            foreach (var r in results)
            {
                total++;
                switch (r.TestStatus)
                {
                    case TestStatus.Passed:
                        passed++;
                        break;
                    case TestStatus.Failed:
                        failed++;
                        failures.Add((r.FullName, r.Message ?? ""));
                        break;
                    case TestStatus.Skipped:
                        skipped++;
                        break;
                }
            }

            var sb = new StringBuilder();
            sb.Append("{\"summary\":{\"total\":");
            sb.Append(total);
            sb.Append(",\"passed\":");
            sb.Append(passed);
            sb.Append(",\"failed\":");
            sb.Append(failed);
            sb.Append(",\"skipped\":");
            sb.Append(skipped);
            sb.Append("},\"failures\":[");
            for (int i = 0; i < failures.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"testName\":");
                sb.Append(MiniJson.SerializeString(failures[i].name));
                sb.Append(",\"message\":");
                sb.Append(MiniJson.SerializeString(failures[i].message));
                sb.Append('}');
            }
            sb.Append("]}");

            return ToolResult.Success(sb.ToString());
        }

        private class TestCallbacks : ICallbacks
        {
            private readonly List<ITestResultAdaptor> _results;
            private readonly TaskCompletionSource<bool> _tcs;

            public TestCallbacks(List<ITestResultAdaptor> results, TaskCompletionSource<bool> tcs)
            {
                _results = results;
                _tcs = tcs;
            }

            public void RunStarted(ITestAdaptor testsToRun) { }

            public void RunFinished(ITestResultAdaptor result)
            {
                CollectLeafResults(result, _results);
                _tcs.TrySetResult(true);
            }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result) { }

            private static void CollectLeafResults(ITestResultAdaptor result, List<ITestResultAdaptor> list)
            {
                if (result.HasChildren)
                {
                    foreach (var child in result.Children)
                        CollectLeafResults(child, list);
                }
                else
                {
                    list.Add(result);
                }
            }
        }
    }
}
