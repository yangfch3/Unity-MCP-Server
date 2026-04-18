using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取当前场景的 GameObject 树结构（名称+组件列表）。
    /// 支持 root 参数：缺省/空串=Prefab Stage 优先，回退 Active Scene；"selection"=以当前选中 GameObject 为根。
    /// </summary>
    public class HierarchyTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_getHierarchy";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "获取当前场景的 GameObject 树结构，支持 root 参数指定根节点来源";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"maxDepth\":{\"type\":\"integer\",\"description\":\"最大遍历深度，-1 表示无限制\",\"default\":-1},\"root\":{\"type\":\"string\",\"description\":\"根节点来源：缺省或空串=Prefab Stage 优先，回退 Active Scene；\\\"selection\\\"=以当前选中 GameObject 为根\",\"default\":\"\"}}}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            int maxDepth = -1;
            if (parameters != null && parameters.TryGetValue("maxDepth", out var raw))
            {
                if (raw is long l) maxDepth = (int)l;
                else if (raw is double d) maxDepth = (int)d;
                else if (raw is int i) maxDepth = i;
            }

            string root = null;
            if (parameters != null && parameters.TryGetValue("root", out var rootRaw))
            {
                root = rootRaw as string;
            }

            GameObject[] roots;

            if (string.IsNullOrEmpty(root))
            {
                roots = ResolveDefaultRoots();
            }
            else if (root == "selection")
            {
                var go = Selection.activeGameObject;
                if (go == null)
                    return Task.FromResult(ToolResult.Error("当前没有选中任何 GameObject"));
                roots = new[] { go };
            }
            else
            {
                return Task.FromResult(ToolResult.Error(
                    $"不支持的 root 值: \"{root}\"。支持的值: 缺省/空串、\"selection\""));
            }

            var sb = new StringBuilder();
            BuildTree(sb, roots, 0, maxDepth);
            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }

        /// <summary>
        /// 解析缺省根节点：Prefab Stage 优先，回退 Active Scene。
        /// </summary>
        private static GameObject[] ResolveDefaultRoots()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                return new[] { stage.prefabContentsRoot };

            return SceneManager.GetActiveScene().GetRootGameObjects();
        }

        private static void BuildTree(StringBuilder sb, GameObject[] gameObjects, int depth, int maxDepth)
        {
            sb.Append('[');
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (i > 0) sb.Append(',');
                var go = gameObjects[i];
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(go.name));
                sb.Append(",\"active\":");
                sb.Append(go.activeSelf ? "true" : "false");

                // 组件列表
                sb.Append(",\"components\":[");
                var components = go.GetComponents<Component>();
                bool first = true;
                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c] == null) continue;
                    if (!first) sb.Append(',');
                    sb.Append(MiniJson.SerializeString(components[c].GetType().Name));
                    first = false;
                }
                sb.Append(']');

                // 子节点
                sb.Append(",\"children\":");
                if (maxDepth == -1 || depth < maxDepth)
                {
                    var t = go.transform;
                    var children = new GameObject[t.childCount];
                    for (int ci = 0; ci < t.childCount; ci++)
                        children[ci] = t.GetChild(ci).gameObject;
                    BuildTree(sb, children, depth + 1, maxDepth);
                }
                else
                {
                    sb.Append("[]");
                }

                sb.Append('}');
            }
            sb.Append(']');
        }
    }
}
