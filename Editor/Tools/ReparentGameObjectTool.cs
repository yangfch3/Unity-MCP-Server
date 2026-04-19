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
    /// MCP 工具：修改 GameObject 的父节点。
    /// </summary>
    public class ReparentGameObjectTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_reparentGameObject";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "修改 GameObject 的父节点";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"instanceID\":{\"type\":\"integer\",\"description\":\"目标 GameObject 的 instanceID\"},\"path\":{\"type\":\"string\",\"description\":\"目标 GameObject 的路径（如 \\\"/Root/Child\\\"）\"},\"newParentInstanceID\":{\"type\":\"integer\",\"description\":\"新父节点的 instanceID\"},\"newParentPath\":{\"type\":\"string\",\"description\":\"新父节点的路径\"},\"worldPositionStays\":{\"type\":\"boolean\",\"description\":\"是否保持世界坐标不变（默认 true）\"}}}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            try
            {
                // 1. Resolve target GO
                var (go, err) = GameObjectResolveHelper.Resolve(parameters);
                if (go == null)
                    return Task.FromResult(ToolResult.Error(err));

                // 2. Parse worldPositionStays, default true
                bool worldPositionStays = true;
                if (parameters != null && parameters.TryGetValue("worldPositionStays", out var rawWps) && rawWps is bool wps)
                    worldPositionStays = wps;

                // 3. Check if newParent params are provided
                bool hasNewParentParam = false;
                if (parameters != null)
                {
                    if (parameters.TryGetValue("newParentInstanceID", out var rawNpId) && rawNpId != null)
                        hasNewParentParam = true;
                    if (!hasNewParentParam && parameters.TryGetValue("newParentPath", out var rawNpPath))
                    {
                        var npPath = rawNpPath as string;
                        if (!string.IsNullOrEmpty(npPath))
                            hasNewParentParam = true;
                    }
                }

                Transform newParentTransform;

                if (hasNewParentParam)
                {
                    // 4. Resolve new parent
                    var (newParent, npErr) = GameObjectResolveHelper.Resolve(
                        parameters, "newParentInstanceID", "newParentPath");
                    if (newParent == null)
                        return Task.FromResult(ToolResult.Error("未找到指定的新父节点"));
                    newParentTransform = newParent.transform;
                }
                else
                {
                    // 5. No newParent params: Prefab Stage → prefab root; otherwise null (scene root)
                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null)
                        newParentTransform = stage.prefabContentsRoot.transform;
                    else
                        newParentTransform = null;
                }

                // 6. Reparent with Undo
                Undo.SetTransformParent(go.transform, newParentTransform, worldPositionStays, "Reparent GameObject");

                // 7. Return JSON with name, path (new), instanceID
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
