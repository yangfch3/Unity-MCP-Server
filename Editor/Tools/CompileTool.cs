using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：触发脚本编译并返回编译结果（成功/失败+错误列表）。
    /// </summary>
    public class CompileTool : IMcpTool
    {
        public string Name => "build_compile";
        public string Category => "build";
        public string Description => "触发脚本编译并返回编译结果";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

        private static readonly TimeSpan CompileTimeout = TimeSpan.FromSeconds(60);

        public async Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            var tcs = new TaskCompletionSource<List<CompilerMessage>>();
            var allMessages = new List<CompilerMessage>();
            bool compilationStarted = false;

            Action<object> startedHandler = null;
            startedHandler = (obj) =>
            {
                compilationStarted = true;
            };

            Action<string, CompilerMessage[]> assemblyHandler = (assemblyName, messages) =>
            {
                lock (allMessages)
                {
                    allMessages.AddRange(messages);
                }
            };

            Action<object> finishedHandler = null;
            finishedHandler = (obj) =>
            {
                CompilationPipeline.compilationStarted -= startedHandler;
                CompilationPipeline.compilationFinished -= finishedHandler;
                CompilationPipeline.assemblyCompilationFinished -= assemblyHandler;
                lock (allMessages)
                {
                    tcs.TrySetResult(new List<CompilerMessage>(allMessages));
                }
            };

            CompilationPipeline.compilationStarted += startedHandler;
            CompilationPipeline.assemblyCompilationFinished += assemblyHandler;
            CompilationPipeline.compilationFinished += finishedHandler;

            AssetDatabase.Refresh();

            // 短暂等待，检测是否真的触发了编译
            await Task.Delay(2000);
            if (!compilationStarted && !tcs.Task.IsCompleted)
            {
                // 无需编译，清理回调直接返回成功
                CompilationPipeline.compilationStarted -= startedHandler;
                CompilationPipeline.compilationFinished -= finishedHandler;
                CompilationPipeline.assemblyCompilationFinished -= assemblyHandler;
                return ToolResult.Success("{\"success\":true,\"errors\":[],\"message\":\"无需编译，代码已是最新\"}");
            }

            // 等待编译完成或超时
            var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(CompileTimeout));
            if (completedTask != tcs.Task)
            {
                CompilationPipeline.compilationStarted -= startedHandler;
                CompilationPipeline.compilationFinished -= finishedHandler;
                CompilationPipeline.assemblyCompilationFinished -= assemblyHandler;
                return ToolResult.Error("编译超时（60 秒）");
            }

            var result = tcs.Task.Result;

            // 筛选错误
            var errors = new List<CompilerMessage>();
            foreach (var msg in result)
            {
                if (msg.type == CompilerMessageType.Error)
                    errors.Add(msg);
            }

            var sb = new StringBuilder();
            sb.Append("{\"success\":");
            sb.Append(errors.Count == 0 ? "true" : "false");
            sb.Append(",\"errors\":[");
            for (int i = 0; i < errors.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var err = errors[i];
                sb.Append("{\"file\":");
                sb.Append(MiniJson.SerializeString(err.file ?? ""));
                sb.Append(",\"line\":");
                sb.Append(err.line);
                sb.Append(",\"column\":");
                sb.Append(err.column);
                sb.Append(",\"message\":");
                sb.Append(MiniJson.SerializeString(err.message ?? ""));
                sb.Append('}');
            }
            sb.Append("]}");

            return ToolResult.Success(sb.ToString());
        }
    }
}
