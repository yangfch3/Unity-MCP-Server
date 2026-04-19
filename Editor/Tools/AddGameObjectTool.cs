using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：在 Prefab Stage 或 Active Scene 中添加 GameObject。
    /// </summary>
    public class AddGameObjectTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_addGameObject";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "在 Prefab Stage 或 Active Scene 中添加 GameObject";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"name\":{\"type\":\"string\",\"description\":\"新 GameObject 的名称（默认 \\\"GameObject\\\"）\"},\"parentInstanceID\":{\"type\":\"integer\",\"description\":\"父节点的 instanceID\"},\"parentPath\":{\"type\":\"string\",\"description\":\"父节点的路径（如 \\\"/Root/Child\\\"）\"}}}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            try
            {
                // 1. Parse name, default to "GameObject"
                string name = null;
                if (parameters != null && parameters.TryGetValue("name", out var rawName))
                    name = rawName as string;
                if (string.IsNullOrEmpty(name))
                    name = "GameObject";

                // 2. Check if parent params are provided
                bool hasParentParam = false;
                if (parameters != null)
                {
                    if (parameters.TryGetValue("parentInstanceID", out var rawPid) && rawPid != null)
                        hasParentParam = true;
                    if (!hasParentParam && parameters.TryGetValue("parentPath", out var rawPp))
                    {
                        var pp = rawPp as string;
                        if (!string.IsNullOrEmpty(pp))
                            hasParentParam = true;
                    }
                }

                GameObject parent = null;

                if (hasParentParam)
                {
                    var (resolved, err) = GameObjectResolveHelper.Resolve(
                        parameters, "parentInstanceID", "parentPath");
                    if (resolved == null)
                        return Task.FromResult(ToolResult.Error(err));
                    parent = resolved;
                }
                else
                {
                    // 3. No parent params: Prefab Stage → prefabContentsRoot; otherwise null (scene root)
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null)
                        parent = stage.prefabContentsRoot;
                }

                // 4. Create new GameObject
                var go = new GameObject(name);

                // 5. Register undo immediately after creation
                Undo.RegisterCreatedObjectUndo(go, "Add GameObject");

                // 6. Set parent
                if (parent != null)
                    go.transform.SetParent(parent.transform, false);

                // 7. Return JSON with name, path, instanceID
                var path = GameObjectPathHelper.GetGameObjectPath(go);
                var sb = new StringBuilder();
                sb.Append("{\"name\":");
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
