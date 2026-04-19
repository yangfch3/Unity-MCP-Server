using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：通过路径程序化选中 Hierarchy 中的 GameObject。
    /// Prefab Stage 优先查找，回退 Active Scene。
    /// </summary>
    public class SelectGameObjectTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_selectGameObject";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "通过路径选中 Hierarchy 中的 GameObject";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\",\"description\":\"要选中的 GameObject 路径（如 \\\"/Root/Child/Target\\\"）\"},\"instanceID\":{\"type\":\"integer\",\"description\":\"要选中的 GameObject 的 instanceID（与 path 二选一，优先使用）\"}}}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            // 优先使用 instanceID
            int instanceID = 0;
            bool hasInstanceID = false;
            if (parameters != null && parameters.TryGetValue("instanceID", out var rawId) && rawId != null)
            {
                hasInstanceID = true;
                if (rawId is long l) instanceID = (int)l;
                else if (rawId is double d) instanceID = (int)d;
                else if (rawId is int i) instanceID = i;
            }

            string path = null;
            if (parameters != null && parameters.TryGetValue("path", out var raw))
                path = raw as string;

            GameObject go;

            if (hasInstanceID)
            {
                go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (go == null)
                    return Task.FromResult(ToolResult.Error($"未找到 instanceID: {instanceID}"));
            }
            else
            {
                if (string.IsNullOrEmpty(path))
                    return Task.FromResult(ToolResult.Error("path 或 instanceID 参数至少提供一个"));

                go = GameObjectResolveHelper.FindByPath(path);
                if (go == null)
                    return Task.FromResult(ToolResult.Error($"未找到: {path}"));
            }

            Selection.activeGameObject = go;

            // 计算实际路径用于返回
            string actualPath = path ?? GameObjectPathHelper.GetGameObjectPath(go);

            var sb = new StringBuilder();
            sb.Append("{\"name\":");
            sb.Append(MiniJson.SerializeString(go.name));
            sb.Append(",\"path\":");
            sb.Append(MiniJson.SerializeString(actualPath));
            sb.Append(",\"instanceID\":");
            sb.Append(go.GetInstanceID());
            sb.Append('}');

            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }
    }
}
