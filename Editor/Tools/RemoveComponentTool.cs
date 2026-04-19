using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：移除指定 GameObject 上的组件。
    /// </summary>
    public class RemoveComponentTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_removeComponent";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "移除指定 GameObject 上的组件";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"目标 GameObject 的 instanceID\"},\"path\":{\"type\":\"string\",\"description\":\"目标 GameObject 的路径（如 \\\"/Root/Child\\\"）\"},\"componentType\":{\"type\":\"string\",\"description\":\"要移除的组件类型名（如 \\\"BoxCollider\\\"）\"}},\"required\":[\"componentType\"]}";

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

                // 3. Find component on GO
                var comp = ComponentTypeHelper.FindComponent(go, typeName);
                if (comp == null)
                    return Task.FromResult(ToolResult.Error($"在 {go.name} 上未找到 {typeName} 组件"));

                // 4. Prevent removing Transform/RectTransform
                if (comp is Transform || comp is RectTransform)
                    return Task.FromResult(ToolResult.Error("Transform 组件不可移除"));

                // 5. Remove component with Undo support
                var actualTypeName = comp.GetType().Name;
                var goName = go.name;
                var path = GameObjectPathHelper.GetGameObjectPath(go);

                Undo.DestroyObjectImmediate(comp);

                // 6. Return JSON result
                var sb = new StringBuilder();
                sb.Append("{\"componentType\":");
                sb.Append(MiniJson.SerializeString(actualTypeName));
                sb.Append(",\"name\":");
                sb.Append(MiniJson.SerializeString(goName));
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(path));
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
