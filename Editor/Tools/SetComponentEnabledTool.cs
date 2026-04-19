using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：修改 GameObject 上指定组件的启用/禁用状态。
    /// </summary>
    public class SetComponentEnabledTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_setComponentEnabled";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "修改 GameObject 上指定组件的启用/禁用状态";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"目标 GameObject 的 instanceID\"},\"path\":{\"type\":\"string\",\"description\":\"目标 GameObject 的路径（如 \\\"/Root/Child\\\"）\"},\"componentType\":{\"type\":\"string\",\"description\":\"组件类型名（如 \\\"BoxCollider\\\"）\"},\"enabled\":{\"type\":\"boolean\",\"description\":\"启用/禁用状态\"}},\"required\":[\"componentType\",\"enabled\"]}";

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

                // 3. Extract enabled param
                if (parameters == null || !parameters.TryGetValue("enabled", out var rawEnabled) || rawEnabled == null)
                    return Task.FromResult(ToolResult.Error("enabled 为必填参数"));

                bool enabled;
                if (rawEnabled is bool b)
                    enabled = b;
                else
                    return Task.FromResult(ToolResult.Error("enabled 为必填参数"));

                // 4. Find component on GO
                var comp = ComponentTypeHelper.FindComponent(go, typeName);
                if (comp == null)
                    return Task.FromResult(ToolResult.Error($"在 {go.name} 上未找到 {typeName} 组件"));

                // 5. Set enabled based on component type
                if (comp is Behaviour behaviour)
                {
                    Undo.RecordObject(behaviour, "Set Enabled");
                    behaviour.enabled = enabled;
                }
                else if (comp is Renderer renderer)
                {
                    Undo.RecordObject(renderer, "Set Enabled");
                    renderer.enabled = enabled;
                }
                else
                {
                    return Task.FromResult(ToolResult.Error(
                        $"组件 {comp.GetType().Name} 不支持启停操作（需继承自 Behaviour 或 Renderer）"));
                }

                // 6. Return JSON result
                var goPath = GameObjectPathHelper.GetGameObjectPath(go);
                var sb = new StringBuilder();
                sb.Append("{\"componentType\":");
                sb.Append(MiniJson.SerializeString(comp.GetType().Name));
                sb.Append(",\"name\":");
                sb.Append(MiniJson.SerializeString(go.name));
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(goPath));
                sb.Append(",\"enabled\":");
                sb.Append(enabled ? "true" : "false");
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
