#if !UNITY_6000_OR_NEWER
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CSharp;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：动态编译并执行 C# 代码片段（实验性，仅 Mono）。
    /// </summary>
    public class ExecuteImmediateTool : IMcpTool
    {
        /// <summary>工具名称。</summary>
        public string Name => "code_executeImmediate";

        /// <summary>所属分类。</summary>
        public string Category => "code";

        /// <summary>工具描述。</summary>
        public string Description => "Compile and execute C# code at runtime (experimental, Mono only)";

        /// <summary>JSON Schema 描述参数。</summary>
        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{\"code\":{\"type\":\"string\",\"description\":\"C# source code to compile and execute\"}},\"required\":[\"code\"]}";

        private static readonly object _executionLock = new object();
        private const string PrefKey = "McpServer_CodeExecuteImmediate";
        private const int TimeoutMs = 10000;

        /// <summary>执行工具逻辑。</summary>
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            // Extract code parameter
            string code = null;
            if (parameters != null && parameters.ContainsKey("code"))
                code = parameters["code"] as string;


            if (string.IsNullOrEmpty(code))
                return Task.FromResult(ToolResult.Success(BuildResponse("", "missing 'code' parameter")));

            // Check toggle
            if (!EditorPrefs.GetBool(PrefKey, false))
                return Task.FromResult(ToolResult.Error("code_executeImmediate is disabled. Enable it in Window > MCP Server config panel."));

            lock (_executionLock)
            {
                // Compile
                CompilerResults compileResult = CompileCode(code);
                if (compileResult.Errors.HasErrors)
                {
                    var sb = new StringBuilder();
                    foreach (CompilerError err in compileResult.Errors)
                    {
                        if (!err.IsWarning)
                        {
                            if (sb.Length > 0) sb.Append('\n');
                            sb.AppendFormat("({0},{1}): {2}", err.Line, err.Column, err.ErrorText);
                        }
                    }
                    return Task.FromResult(ToolResult.Success(BuildResponse("", sb.ToString())));
                }

                // Find entry point
                MethodInfo entryPoint = FindEntryPoint(compileResult.CompiledAssembly);
                if (entryPoint == null)
                    return Task.FromResult(ToolResult.Success(BuildResponse("", "No entry point found. Define a public static void Run() method.")));

                // Execute with timeout
                var (output, error) = RunWithTimeout(entryPoint, TimeoutMs);
                return Task.FromResult(ToolResult.Success(BuildResponse(output, error)));
            }
        }

        /// <summary>使用 CSharpCodeProvider 编译源代码。</summary>
        private static CompilerResults CompileCode(string source)
        {
            var provider = new CSharpCodeProvider();
            var options = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false
            };

            // CSharpCodeProvider (mcs) implicitly references its own mscorlib.dll.
            // If we also explicitly add the runtime's mscorlib.dll to
            // ReferencedAssemblies, the compiler sees duplicate type definitions
            // (System.Console, System.Object, etc.) and fails with CS0433.
            //
            // Fix: skip mscorlib.dll — the compiler already knows about it.
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.IsDynamic) continue;
                    if (string.IsNullOrEmpty(asm.Location)) continue;
                    if (!seen.Add(asm.Location)) continue;

                    string fileName = Path.GetFileName(asm.Location);
                    if (fileName.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase))
                        continue;

                    options.ReferencedAssemblies.Add(asm.Location);
                }
                catch
                {
                    // Skip assemblies that throw on Location access
                }
            }

            return provider.CompileAssemblyFromSource(options, source);
        }

        /// <summary>在编译后的程序集中查找 public static void Run() 入口点。</summary>
        private static MethodInfo FindEntryPoint(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var method = type.GetMethod("Run",
                    BindingFlags.Static | BindingFlags.Public,
                    null, Type.EmptyTypes, null);
                if (method != null && method.ReturnType == typeof(void))
                    return method;
            }
            return null;
        }

        /// <summary>在后台线程执行方法，超时后中止。</summary>
        private static (string output, string error) RunWithTimeout(MethodInfo method, int timeoutMs)
        {
            var originalOut = Console.Out;
            var writer = new StringWriter();
            Exception capturedException = null;

            try
            {
                Console.SetOut(writer);

                var thread = new Thread(() =>
                {
                    try
                    {
                        method.Invoke(null, null);
                    }
                    catch (TargetInvocationException ex)
                    {
                        capturedException = ex.InnerException ?? ex;
                    }
                    catch (Exception ex)
                    {
                        capturedException = ex;
                    }
                });
                thread.IsBackground = true;
                thread.Start();

                bool finished = thread.Join(timeoutMs);
                string output = writer.ToString();

                if (!finished)
                {
                    try { thread.Abort(); } catch { /* best-effort */ }
                    return (output, $"Execution timed out ({timeoutMs / 1000}s). Consider restarting MCP Server if subsequent executions behave unexpectedly.");
                }

                if (capturedException != null)
                    return (output, capturedException.Message + "\n" + capturedException.StackTrace);

                return (output, "");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>构建结构化 JSON 响应。</summary>
        private static string BuildResponse(string output, string error)
        {
            bool success = string.IsNullOrEmpty(error);
            var sb = new StringBuilder();
            sb.Append("{\"success\":");
            sb.Append(success ? "true" : "false");
            sb.Append(",\"output\":");
            sb.Append(MiniJson.SerializeString(output ?? ""));
            sb.Append(",\"error\":");
            sb.Append(MiniJson.SerializeString(error ?? ""));
            sb.Append('}');
            return sb.ToString();
        }
    }
}
#endif
