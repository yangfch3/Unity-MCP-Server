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
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"优先于 path\"},\"path\":{\"type\":\"string\",\"description\":\"如 /Root/Child\"}}}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            try
            {
                // 1. Resolve target GO
                var (go, err) = GameObjectResolveHelper.Resolve(parameters);
                if (go == null)
                    return Task.FromResult(ToolResult.Error(err));

                // 2. Save path before deletion
                var goPath = GameObjectPathHelper.GetGameObjectPath(go);

                // 3. Delete with Undo support
                Undo.DestroyObjectImmediate(go);

                // 4. Return JSON with deleted GO's path
                var sb = new StringBuilder();
                sb.Append("{\"path\":");
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
