using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：修改 GameObject 的激活状态。
    /// </summary>
    public class SetActiveTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_setActive";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "修改 GameObject 的激活状态";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"目标 GameObject 的 instanceID\"},\"path\":{\"type\":\"string\",\"description\":\"目标 GameObject 的路径（如 \\\"/Root/Child\\\"）\"},\"active\":{\"type\":\"boolean\",\"description\":\"激活状态\"}},\"required\":[\"active\"]}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            try
            {
                // 1. Resolve target GO
                var (go, err) = GameObjectResolveHelper.Resolve(parameters);
                if (go == null)
                    return Task.FromResult(ToolResult.Error(err));

                // 2. Extract active param
                if (parameters == null || !parameters.TryGetValue("active", out var rawActive) || rawActive == null)
                    return Task.FromResult(ToolResult.Error("active 为必填参数"));

                bool active;
                if (rawActive is bool b)
                    active = b;
                else
                    return Task.FromResult(ToolResult.Error("active 为必填参数"));

                // 3. Set active with Undo support
                Undo.RecordObject(go, "Set Active");
                go.SetActive(active);

                // 4. Return JSON with name, path, activeSelf
                var goPath = GameObjectPathHelper.GetGameObjectPath(go);
                var sb = new StringBuilder();
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(go.name));
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(goPath));
                sb.Append(",\"activeSelf\":");
                sb.Append(go.activeSelf ? "true" : "false");
                sb.Append('}');

                return Task.FromResult(ToolResult.Success(sb.ToString()));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Error(ex.Message));
            }
        }
    }
}
