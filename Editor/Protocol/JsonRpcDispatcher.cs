using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace UnityMcp.Editor
{
    /// <summary>
    /// JSON-RPC 2.0 协议分发器。解析请求并路由到对应 handler。
    /// 使用手写 JSON 解析/序列化，避免依赖 Newtonsoft.Json。
    /// </summary>
    public class JsonRpcDispatcher
    {
        private const string ProtocolVersion = "2025-03-26";
        private const string ServerName = "unity-mcp";
        private const string ServerVersion = "0.1.0";

        private readonly ToolRegistry _registry;
        private readonly MainThreadQueue _mainThreadQueue;

        public JsonRpcDispatcher(ToolRegistry registry, MainThreadQueue mainThreadQueue)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _mainThreadQueue = mainThreadQueue ?? throw new ArgumentNullException(nameof(mainThreadQueue));
        }

        /// <summary>
        /// 解析 JSON-RPC 请求并路由到对应 handler。
        /// </summary>
        public async Task<string> Dispatch(string jsonBody)
        {
            // Parse JSON
            Dictionary<string, object> request;
            try
            {
                request = MiniJson.Deserialize(jsonBody) as Dictionary<string, object>;
                if (request == null)
                    return ErrorResponse(null, -32700, "Parse error");
            }
            catch
            {
                return ErrorResponse(null, -32700, "Parse error");
            }

            // Extract id (may be number or string)
            request.TryGetValue("id", out var id);

            // Extract method
            if (!request.TryGetValue("method", out var methodObj) || !(methodObj is string method))
                return ErrorResponse(id, -32600, "Invalid Request");

            // Extract params (optional)
            request.TryGetValue("params", out var paramsObj);
            var parameters = paramsObj as Dictionary<string, object>;

            // Route
            switch (method)
            {
                case "initialize":
                    return HandleInitialize(id);
                case "tools/list":
                    return HandleToolsList(id);
                case "tools/call":
                    return await HandleToolsCall(id, parameters);
                default:
                    return ErrorResponse(id, -32601, "Method not found");
            }
        }

        private string HandleInitialize(object id)
        {
            var result = new StringBuilder();
            result.Append('{');
            result.Append("\"protocolVersion\":\"").Append(ProtocolVersion).Append("\",");
            result.Append("\"capabilities\":{\"tools\":{}},");
            result.Append("\"serverInfo\":{");
            result.Append("\"name\":\"").Append(ServerName).Append("\",");
            result.Append("\"version\":\"").Append(ServerVersion).Append("\"");
            result.Append('}');
            result.Append('}');
            return SuccessResponse(id, result.ToString());
        }

        private string HandleToolsList(object id)
        {
            var tools = _registry.ListAll();
            var sb = new StringBuilder();
            sb.Append("{\"tools\":[");
            for (int i = 0; i < tools.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var t = tools[i];
                sb.Append('{');
                sb.Append("\"name\":").Append(MiniJson.SerializeString(t.Name)).Append(',');
                sb.Append("\"description\":").Append(MiniJson.SerializeString(t.Description)).Append(',');
                sb.Append("\"inputSchema\":").Append(t.InputSchema ?? "{}");
                sb.Append('}');
            }
            sb.Append("]}");
            return SuccessResponse(id, sb.ToString());
        }

        private async Task<string> HandleToolsCall(object id, Dictionary<string, object> parameters)
        {
            if (parameters == null)
                return ErrorResponse(id, -32602, "Invalid params: missing params");

            // Extract tool name
            if (!parameters.TryGetValue("name", out var nameObj) || !(nameObj is string toolName))
                return ErrorResponse(id, -32602, "Invalid params: missing tool name");

            var tool = _registry.Resolve(toolName);
            if (tool == null)
                return ErrorResponse(id, -32602, $"Invalid params: tool '{toolName}' not found");

            // Extract arguments (optional)
            var arguments = parameters.TryGetValue("arguments", out var argsObj)
                ? argsObj as Dictionary<string, object> ?? new Dictionary<string, object>()
                : new Dictionary<string, object>();

            // Execute on main thread
            ToolResult result;
            try
            {
                result = await _mainThreadQueue.Enqueue(() => tool.Execute(arguments));
            }
            catch (Exception ex)
            {
                return ErrorResponse(id, -32603, $"Internal error: {ex.Message}");
            }

            // Build response
            var sb = new StringBuilder();
            sb.Append("{\"content\":[");
            for (int i = 0; i < result.Content.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var item = result.Content[i];
                sb.Append("{\"type\":").Append(MiniJson.SerializeString(item.Type));
                sb.Append(",\"text\":").Append(MiniJson.SerializeString(item.Text)).Append('}');
            }
            sb.Append("],\"isError\":").Append(result.IsError ? "true" : "false").Append('}');
            return SuccessResponse(id, sb.ToString());
        }

        private static string SuccessResponse(object id, string resultJson)
        {
            var sb = new StringBuilder();
            sb.Append("{\"jsonrpc\":\"2.0\",\"id\":");
            sb.Append(SerializeId(id));
            sb.Append(",\"result\":");
            sb.Append(resultJson);
            sb.Append('}');
            return sb.ToString();
        }

        private static string ErrorResponse(object id, int code, string message)
        {
            var sb = new StringBuilder();
            sb.Append("{\"jsonrpc\":\"2.0\",\"id\":");
            sb.Append(SerializeId(id));
            sb.Append(",\"error\":{\"code\":");
            sb.Append(code.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"message\":");
            sb.Append(MiniJson.SerializeString(message));
            sb.Append("}}");
            return sb.ToString();
        }

        private static string SerializeId(object id)
        {
            if (id == null) return "null";
            if (id is string s) return MiniJson.SerializeString(s);
            if (id is long l) return l.ToString(CultureInfo.InvariantCulture);
            if (id is double d) return d.ToString(CultureInfo.InvariantCulture);
            return id.ToString();
        }
    }
}
