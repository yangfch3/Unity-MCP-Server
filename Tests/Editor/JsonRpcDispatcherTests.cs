using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// JsonRpcDispatcher 单元测试。
    /// 使用 stub ToolRegistry 和直通 MainThreadQueue 替身，隔离测试协议分发逻辑。
    /// 注意：tools/call 路径依赖 MainThreadQueue.Enqueue → EditorApplication.update，
    /// 在同步测试中会死锁，因此 tools/call 测试使用 SyncMainThreadQueue 绕过。
    /// </summary>
    public class JsonRpcDispatcherTests
    {
        private ToolRegistry _registry;
        private JsonRpcDispatcher _dispatcher;

        [SetUp]
        public void SetUp()
        {
            _registry = new ToolRegistry();
            _registry.Register(new StubTool("echo_test", "test", "A stub tool"));

            // 使用直通队列：Enqueue 直接在当前线程执行，不依赖 EditorApplication.update
            var queue = new SyncMainThreadQueue();
            _dispatcher = new JsonRpcDispatcher(_registry, queue);
        }

        // --- initialize ---

        [Test]
        public void Initialize_ReturnsServerInfo()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{}}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("\"result\""), $"Should contain result: {response}");
            Assert.IsTrue(response.Contains("\"protocolVersion\""), $"Should contain protocolVersion: {response}");
            Assert.IsTrue(response.Contains("\"capabilities\""), $"Should contain capabilities: {response}");
            Assert.IsTrue(response.Contains("\"serverInfo\""), $"Should contain serverInfo: {response}");
            Assert.IsTrue(response.Contains("\"id\":1"), $"Should echo id: {response}");
        }

        // --- tools/list ---

        [Test]
        public void ToolsList_ReturnsRegisteredTools()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/list\",\"params\":{}}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("\"result\""), $"Should contain result: {response}");
            Assert.IsTrue(response.Contains("\"tools\""), $"Should contain tools array: {response}");
            Assert.IsTrue(response.Contains("echo_test"), $"Should contain registered tool name: {response}");
        }

        // --- tools/call success ---

        [Test]
        public void ToolsCall_ValidTool_ReturnsResult()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"tools/call\",\"params\":{\"name\":\"echo_test\",\"arguments\":{\"msg\":\"hello\"}}}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("\"result\""), $"Should contain result: {response}");
            Assert.IsTrue(response.Contains("\"content\""), $"Should contain content: {response}");
            Assert.IsTrue(response.Contains("\"isError\":false"), $"Should not be error: {response}");
        }

        // --- error: invalid JSON → -32700 ---

        [Test]
        public void InvalidJson_Returns32700()
        {
            var response = _dispatcher.Dispatch("not json at all {{{").GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("-32700"), $"Should contain error code -32700: {response}");
            Assert.IsTrue(response.Contains("Parse error"), $"Should contain 'Parse error': {response}");
        }

        // --- error: unknown method → -32601 ---

        [Test]
        public void UnknownMethod_Returns32601()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":4,\"method\":\"nonexistent/method\",\"params\":{}}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("-32601"), $"Should contain error code -32601: {response}");
            Assert.IsTrue(response.Contains("Method not found"), $"Should contain 'Method not found': {response}");
            Assert.IsTrue(response.Contains("\"id\":4"), $"Should echo id: {response}");
        }

        // --- error: tool not found → -32602 ---

        [Test]
        public void ToolsCall_NonexistentTool_Returns32602()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":5,\"method\":\"tools/call\",\"params\":{\"name\":\"no_such_tool\"}}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("-32602"), $"Should contain error code -32602: {response}");
            Assert.IsTrue(response.Contains("not found"), $"Should mention 'not found': {response}");
        }

        // --- error: tools/call missing params → -32602 ---

        [Test]
        public void ToolsCall_MissingParams_Returns32602()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":6,\"method\":\"tools/call\"}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("-32602"), $"Should contain error code -32602: {response}");
        }

        // --- error: tools/call missing tool name → -32602 ---

        [Test]
        public void ToolsCall_MissingToolName_Returns32602()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":7,\"method\":\"tools/call\",\"params\":{}}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("-32602"), $"Should contain error code -32602: {response}");
        }

        // --- error: missing method field → -32600 ---

        [Test]
        public void MissingMethod_Returns32600()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":8}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("-32600"), $"Should contain error code -32600: {response}");
            Assert.IsTrue(response.Contains("Invalid Request"), $"Should contain 'Invalid Request': {response}");
        }

        // --- string id echoed correctly ---

        [Test]
        public void StringId_EchoedCorrectly()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"id\":\"abc-123\",\"method\":\"initialize\",\"params\":{}}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("\"id\":\"abc-123\""), $"Should echo string id: {response}");
        }

        // --- null id (notification-style) ---

        [Test]
        public void NullId_HandledGracefully()
        {
            var json = "{\"jsonrpc\":\"2.0\",\"method\":\"initialize\"}";
            var response = _dispatcher.Dispatch(json).GetAwaiter().GetResult();

            Assert.IsTrue(response.Contains("\"id\":null"), $"Should have null id: {response}");
            Assert.IsTrue(response.Contains("\"result\""), $"Should still return result: {response}");
        }

        // ---- Helpers ----

        /// <summary>
        /// 直通 MainThreadQueue：实现 IMainThreadQueue 直接同步执行 action，
        /// 不依赖 EditorApplication.update，避免同步测试死锁。
        /// </summary>
        private class SyncMainThreadQueue : IMainThreadQueue
        {
            public Task<ToolResult> Enqueue(System.Func<Task<ToolResult>> action)
            {
                try
                {
                    return action();
                }
                catch (System.Exception ex)
                {
                    return Task.FromResult(ToolResult.Error($"SyncMainThreadQueue: {ex.Message}"));
                }
            }
        }

        private class StubTool : IMcpTool
        {
            public string Name { get; }
            public string Category { get; }
            public string Description { get; }
            public string InputSchema => "{\"type\":\"object\"}";

            public StubTool(string name, string category, string description)
            {
                Name = name;
                Category = category;
                Description = description;
            }

            public Task<ToolResult> Execute(Dictionary<string, object> parameters)
            {
                return Task.FromResult(ToolResult.Success("stub ok"));
            }
        }
    }
}
