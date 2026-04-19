using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：删除指定的 GameObject 及其所有子对象。
    /// </summary>
    public class DeleteGameObjectTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_deleteGameObject";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "删除指定的 GameObject 及其所有子对象";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"要删除的 GameObject 的 instanceID\"},\"path\":{\"type\":\"string\",\"description\":\"要删除的 GameObject 的路径（如 \\\"/Root/Child\\\"）\"}}}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            try
            {
                // 1. Resolve target GO
                var (go, err) = GameObjectResolveHelper.Resolve(parameters);
                if (go == null)
                    return Task.FromResult(ToolResult.Error(err));

                // 2. Save name and path before deletion
                var goName = go.name;
                var goPath = GameObjectPathHelper.GetGameObjectPath(go);

                // 3. Delete with Undo support
                Undo.DestroyObjectImmediate(go);

                // 4. Return JSON with deleted GO's name and path
                var sb = new StringBuilder();
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(goName));
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(goPath));
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
