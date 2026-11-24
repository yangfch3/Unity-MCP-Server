#if !UNITY_6000_OR_NEWER
using System;
using System.CodeDom.Compiler;
using System.Collections;
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
        public string Description => "运行时编译执行 C#（实验，仅 Mono）";

        /// <summary>JSON Schema 描述参数。</summary>
        public string InputSchema =>
            "{\"type\":\"object\",\"properties\":{\"code\":{\"type\":\"string\",\"description\":\"C# 源码，入口为 public static void Run() 或 public static IEnumerator Run()（跨帧：yield return null 等一帧，yield return IEnumerator 嵌套）。用 Console.WriteLine 输出，Debug.Log 不进 output\"},\"filePath\":{\"type\":\"string\",\"description\":\"支持传入代码文件路径；相对 Unity 项目根目录路径或绝对路径\"},\"mainThread\":{\"type\":\"boolean\",\"description\":\"true（默认）可调 Unity API，无超时保护；false 在后台线程，有超时保护但不可调 Unity API。IEnumerator 入口仅支持 true\"},\"timeout\":{\"type\":\"integer\",\"description\":\"毫秒。后台模式默认 5000（1000-30000）；跨帧模式默认 30000（1000-120000）\"}},\"oneOf\":[{\"required\":[\"code\"]},{\"required\":[\"filePath\"]}]}";

        private static readonly object _executionLock = new object();
        public const string DisablePrefKey = "McpServer_DisableCodeExecuteImmediate";
        private const int DefaultTimeoutMs = 5000;
        private const int DefaultCrossFrameTimeoutMs = 30000;
        private const int MaxCrossFrameTimeoutMs = 120000;

        /// <summary>单帧内最多推进的迭代步数，防止不 yield 的嵌套协程冻结 Editor。</summary>
        private const int MaxStepsPerFrame = 1000;

        /// <summary>
        /// 跨帧执行占用标记。跨帧期间 Console.Out 处于重定向状态，
        /// 并发进入会互相覆盖输出，故此期间拒绝其它调用。
        /// 注：McpServer 单线程串行处理请求，经 MCP 调用时不会触发此分支；
        /// 它保护的是直接调用 Execute() 的场景（如测试）。
        /// </summary>
        private static volatile bool _crossFrameBusy;

        /// <summary>执行工具逻辑。</summary>
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            string code = parameters != null && parameters.TryGetValue("code", out var rawCode) ? rawCode as string : null;
            string filePath = parameters != null && parameters.TryGetValue("filePath", out var rawPath) ? rawPath as string : null;
            if (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(filePath))
                return Task.FromResult(Respond("", "missing 'code' or 'filePath' parameter"));
            if (parameters.ContainsKey("code") && parameters.ContainsKey("filePath"))
                return Task.FromResult(Respond("", "Specify either 'code' or 'filePath', not both."));

            // Check toggle
            if (EditorPrefs.GetBool(DisablePrefKey, false))
                return Task.FromResult(ToolResult.Error("code_executeImmediate is disabled. Enable it in Window > MCP Server config panel."));

            if (_crossFrameBusy)
                return Task.FromResult(Respond("",
                    "A cross-frame execution (IEnumerator Run) is still running. Wait for it to finish before issuing another call."));

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                try
                {
                    string path = Path.IsPathRooted(filePath) ? filePath
                        : Path.Combine(Path.GetDirectoryName(Application.dataPath), filePath);
                    code = File.ReadAllText(path);
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                    || ex is ArgumentException || ex is NotSupportedException)
                {
                    return Task.FromResult(Respond("", "Cannot read filePath: " + ex.Message));
                }
            }

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
                    return Task.FromResult(Respond("", sb.ToString()));
                }

                // Find entry point
                MethodInfo entryPoint = FindEntryPoint(compileResult.CompiledAssembly);
                if (entryPoint == null)
                    return Task.FromResult(Respond("", "No entry point found. Define a public static void Run() or public static IEnumerator Run() method."));

                // Parse mainThread parameter (default: true)
                bool useMainThread = true;
                if (parameters != null && parameters.ContainsKey("mainThread"))
                {
                    var raw = parameters["mainThread"];
                    if (raw is bool b) useMainThread = b;
                }

                // IEnumerator entry point: drive across frames on the main thread
                if (entryPoint.ReturnType == typeof(IEnumerator))
                {
                    if (!useMainThread)
                        return Task.FromResult(Respond("",
                            "IEnumerator Run() requires mainThread:true (frames are only driven on the main thread)."));

                    return RunAcrossFrames(entryPoint,
                        ParseTimeout(parameters, DefaultCrossFrameTimeoutMs, MaxCrossFrameTimeoutMs));
                }

                var (output, error) = useMainThread
                    ? RunOnMainThread(entryPoint)
                    : RunWithTimeout(entryPoint, ParseTimeout(parameters, DefaultTimeoutMs, 30000));

                return Task.FromResult(Respond(output, error));
            }
        }

        /// <summary>使用 CSharpCodeProvider 编译源代码。</summary>
        private static CompilerResults CompileCode(string source)
        {
            var provider = new CSharpCodeProvider();
            var options = new CompilerParameters
            {
                GenerateInMemory = true,
                GenerateExecutable = false,
                // mcs defaults to C# 7.0; without this, C# 7.1+ syntax such as the
                // `default` literal fails to compile. Note that mcs never implemented
                // local functions or C# 8 syntax, so those stay unavailable.
                CompilerOptions = "-langversion:latest"
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
                    if (IsExcludedReference(fileName))
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

        /// <summary>
        /// 判断某个程序集是否应排除出引用列表。
        /// mscorlib.dll 由编译器隐式引用；System.CodeDom.dll 与 System.dll
        /// 重复定义了 105 个 CodeDom/Microsoft.CSharp 类型，同时引用会导致
        /// "defined multiple times"。
        /// </summary>
        private static bool IsExcludedReference(string fileName)
        {
            return fileName.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase)
                || fileName.Equals("System.CodeDom.dll", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>解析 timeout 参数，缺省与越界时归一到 [1000, max]。</summary>
        private static int ParseTimeout(Dictionary<string, object> parameters, int defaultMs, int maxMs)
        {
            int timeoutMs = defaultMs;
            if (parameters != null && parameters.ContainsKey("timeout"))
            {
                var raw = parameters["timeout"];
                if (raw is long l) timeoutMs = (int)l;
                else if (raw is double d) timeoutMs = (int)d;
            }
            if (timeoutMs < 1000) timeoutMs = 1000;
            if (timeoutMs > maxMs) timeoutMs = maxMs;
            return timeoutMs;
        }

        /// <summary>
        /// 在编译后的程序集中查找入口点：public static void Run() 或
        /// public static IEnumerator Run()。
        /// </summary>
        private static MethodInfo FindEntryPoint(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                var method = type.GetMethod("Run",
                    BindingFlags.Static | BindingFlags.Public,
                    null, Type.EmptyTypes, null);
                if (method == null)
                    continue;
                if (method.ReturnType == typeof(void) || method.ReturnType == typeof(IEnumerator))
                    return method;
            }
            return null;
        }

        /// <summary>
        /// 在主线程逐帧驱动 IEnumerator 入口点，Task 在迭代结束/超时/异常后完成。
        /// 由 EditorApplication.update 推进，Edit Mode 与 PlayMode 均可用。
        /// 支持 yield return null（等一帧）与 yield return IEnumerator（嵌套，同帧进入子协程第一步）；
        /// 其它 yield 值一律按等一帧处理，不支持 YieldInstruction 语义。
        /// </summary>
        private static Task<ToolResult> RunAcrossFrames(MethodInfo method, int timeoutMs)
        {
            var tcs = new TaskCompletionSource<ToolResult>();
            var originalOut = Console.Out;
            var writer = new StringWriter();
            Console.SetOut(writer);

            IEnumerator root;
            try
            {
                root = method.Invoke(null, null) as IEnumerator;
            }
            catch (Exception ex)
            {
                var inner = (ex as TargetInvocationException)?.InnerException ?? ex;
                Console.SetOut(originalOut);
                tcs.SetResult(Respond(writer.ToString(), FormatError(inner)));
                return tcs.Task;
            }

            if (root == null)
            {
                Console.SetOut(originalOut);
                tcs.SetResult(Respond(writer.ToString(),
                    "IEnumerator Run() returned null."));
                return tcs.Task;
            }

            _crossFrameBusy = true;

            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            int frames = 0;

            EditorApplication.CallbackFunction step = null;
            AssemblyReloadEvents.AssemblyReloadCallback onReload = null;

            Action<string> finish = error =>
            {
                EditorApplication.update -= step;
                AssemblyReloadEvents.beforeAssemblyReload -= onReload;
                Console.SetOut(originalOut);
                _crossFrameBusy = false;
                tcs.TrySetResult(Respond(writer.ToString(), error,
                    frames, (int)watch.ElapsedMilliseconds));
            };

            onReload = () => finish("Aborted by assembly reload (domain reload) before the coroutine finished.");

            step = () =>
            {
                frames++;
                if (watch.ElapsedMilliseconds > timeoutMs)
                {
                    finish($"Cross-frame execution timed out ({timeoutMs} ms, {frames} frames).");
                    return;
                }

                try
                {
                    for (int i = 0; i < MaxStepsPerFrame && stack.Count > 0; i++)
                    {
                        var current = stack.Peek();
                        if (!current.MoveNext())
                        {
                            stack.Pop();     // 子协程结束，同帧继续驱动父级
                            continue;
                        }

                        if (current.Current is IEnumerator nested)
                        {
                            stack.Push(nested);   // 嵌套：同帧进入子协程第一步
                            continue;
                        }

                        return;              // yield return null（或其它值）→ 等下一帧
                    }
                }
                catch (Exception ex)
                {
                    finish(FormatError(ex));
                    return;
                }

                if (stack.Count == 0)
                    finish("");
            };

            AssemblyReloadEvents.beforeAssemblyReload += onReload;
            EditorApplication.update += step;
            return tcs.Task;
        }

        /// <summary>在主线程直接执行方法，无超时保护。</summary>
        private static (string output, string error) RunOnMainThread(MethodInfo method)
        {
            var originalOut = Console.Out;
            var writer = new StringWriter();
            try
            {
                Console.SetOut(writer);
                method.Invoke(null, null);
                return (writer.ToString(), "");
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                return (writer.ToString(), FormatError(inner));
            }
            catch (Exception ex)
            {
                return (writer.ToString(), FormatError(ex));
            }
            finally
            {
                Console.SetOut(originalOut);
            }
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
                    return (output, FormatError(capturedException));

                return (output, "");
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }

        /// <summary>异常信息 + 堆栈，去掉末尾的反射调用与本工具驱动帧，只留用户代码帧。</summary>
        internal static string FormatError(Exception ex)
        {
            var lines = (ex.StackTrace ?? "").Split('\n');
            int end = lines.Length;
            while (end > 0 && IsInfraFrame(lines[end - 1]))
                end--;

            var sb = new StringBuilder(ex.Message);
            for (int i = 0; i < end; i++)
                sb.Append('\n').Append(NoSymbolSuffix.Replace(lines[i].TrimEnd('\r'), ""));
            return sb.ToString();
        }

        /// <summary>无符号帧尾部的 " [0x..] in &lt;guid&gt;:0"，只有 IL 偏移，无行号信息。</summary>
        private static readonly System.Text.RegularExpressions.Regex NoSymbolSuffix =
            new System.Text.RegularExpressions.Regex(@"\s*\[0x[0-9a-fA-F]+\] in <[0-9a-fA-F]+>:0\s*$");

        private static bool IsInfraFrame(string line)
        {
            return line.Trim().Length == 0
                || line.Contains("System.Reflection.")
                || line.Contains("(wrapper managed-to-native)")
                || line.Contains("UnityMcp.Editor.Tools.ExecuteImmediateTool");
        }

        /// <summary>
        /// 构建结构化 JSON 响应；失败时以 isError 返回（内容相同）。
        /// error 为空时不输出；frames/elapsedMs 为负时不输出（仅跨帧模式使用）。
        /// </summary>
        private static ToolResult Respond(string output, string error,
            int frames = -1, int elapsedMs = -1)
        {
            bool success = string.IsNullOrEmpty(error);
            var sb = new StringBuilder();
            sb.Append("{\"success\":");
            sb.Append(success ? "true" : "false");
            sb.Append(",\"output\":");
            sb.Append(MiniJson.SerializeString(output ?? ""));
            if (!success)
            {
                sb.Append(",\"error\":");
                sb.Append(MiniJson.SerializeString(error));
            }
            if (frames >= 0)
            {
                sb.Append(",\"frames\":");
                sb.Append(frames);
            }
            if (elapsedMs >= 0)
            {
                sb.Append(",\"elapsedMs\":");
                sb.Append(elapsedMs);
            }
            sb.Append('}');
            return success ? ToolResult.Success(sb.ToString()) : ToolResult.Error(sb.ToString());
        }
    }
}
#endif
