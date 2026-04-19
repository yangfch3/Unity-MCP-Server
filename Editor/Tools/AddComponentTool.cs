using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：给指定 GameObject 添加组件。
    /// </summary>
    public class AddComponentTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_addComponent";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "给指定 GameObject 添加组件";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"目标 GameObject 的 instanceID\"},\"path\":{\"type\":\"string\",\"description\":\"目标 GameObject 的路径（如 \\\"/Root/Child\\\"）\"},\"componentType\":{\"type\":\"string\",\"description\":\"要添加的组件类型名（如 \\\"BoxCollider\\\"）\"}},\"required\":[\"componentType\"]}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            try
            {
                // 1. Resolve target GameObject
                var (go, err) = GameObjectResolveHelper.Resolve(parameters);
                if (go == null)
                    return Task.FromResult(ToolResult.Error(err));

                // 2. Extract componentType
                string typeName = null;
                if (parameters != null && parameters.TryGetValue("componentType", out var rawType))
                    typeName = rawType as string;
                if (string.IsNullOrEmpty(typeName))
                    return Task.FromResult(ToolResult.Error("componentType 为必填参数"));

                // 3. Find component type
                var type = ComponentTypeHelper.FindType(typeName);
                if (type == null)
                    return Task.FromResult(ToolResult.Error($"未找到组件类型: {typeName}"));

                // 4. Add component with Undo support
                Undo.AddComponent(go, type);

                // 5. Return JSON result
                var path = GameObjectPathHelper.GetGameObjectPath(go);
                var sb = new StringBuilder();
                sb.Append("{\"componentType\":");
                sb.Append(MiniJson.SerializeString(type.Name));
                sb.Append(",\"name\":");
                sb.Append(MiniJson.SerializeString(go.name));
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(path));
                sb.Append(",\"instanceID\":");
                sb.Append(go.GetInstanceID());
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
