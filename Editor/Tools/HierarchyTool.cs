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
        public string Description => "获取 GameObject 树（名称+组件，省略 Transform；active 仅 false 时输出）";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"maxDepth\":{\"type\":\"integer\",\"description\":\"最大遍历深度，-1 不限；被截断节点输出 childCount\",\"default\":" + DefaultMaxDepth + "},\"root\":{\"type\":\"string\",\"enum\":[\"\",\"selection\"],\"description\":\"空串=Prefab Stage 优先，回退 Active Scene；selection=以当前选中为根\",\"default\":\"\"}}}";

        private const int DefaultMaxDepth = 5;

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            int maxDepth = DefaultMaxDepth;
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

            if (string.IsNullOrEmpty(root))
            {
                var roots = ResolveDefaultRoots();
                var sb = new StringBuilder();
                BuildTree(sb, roots, 0, maxDepth);
                return Task.FromResult(ToolResult.Success(sb.ToString()));
            }
            else if (root == "selection")
            {
                var go = Selection.activeGameObject;
                if (go == null)
                    return Task.FromResult(ToolResult.Error("当前没有选中任何 GameObject"));

                var sb = new StringBuilder();
                sb.Append("{\"selectionPath\":");
                sb.Append(MiniJson.SerializeString(GameObjectPathHelper.GetGameObjectPath(go)));
                sb.Append(",\"children\":");
                BuildTree(sb, new[] { go }, 0, maxDepth);
                sb.Append('}');
                return Task.FromResult(ToolResult.Success(sb.ToString()));
            }
            else
            {
                return Task.FromResult(ToolResult.Error(
                    $"不支持的 root 值: \"{root}\"。支持的值: 缺省/空串、\"selection\""));
            }
        }

        /// <summary>
        /// 解析缺省根节点：Prefab Stage 优先，回退 Active Scene + DontDestroyOnLoad。
        /// </summary>
        private static GameObject[] ResolveDefaultRoots()
        {
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
                return new[] { stage.prefabContentsRoot };

            var sceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();
            var ddolRoots = DdolSceneHelper.GetRootGameObjects();
            if (ddolRoots.Length == 0)
                return sceneRoots;

            var combined = new GameObject[sceneRoots.Length + ddolRoots.Length];
            sceneRoots.CopyTo(combined, 0);
            ddolRoots.CopyTo(combined, sceneRoots.Length);
            return combined;
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
                if (!go.activeSelf)
                    sb.Append(",\"active\":false");

                // 组件列表（Transform 必然存在，省略）
                var components = go.GetComponents<Component>();
                bool first = true;
                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c] == null || components[c].GetType() == typeof(Transform)) continue;
                    sb.Append(first ? ",\"components\":[" : ",");
                    sb.Append(MiniJson.SerializeString(components[c].GetType().Name));
                    first = false;
                }
                if (!first) sb.Append(']');

                // 子节点：无子节点不输出；超深度只输出 childCount
                var t = go.transform;
                if (t.childCount > 0)
                {
                    if (maxDepth == -1 || depth < maxDepth)
                    {
                        var children = new GameObject[t.childCount];
                        for (int ci = 0; ci < t.childCount; ci++)
                            children[ci] = t.GetChild(ci).gameObject;
                        sb.Append(",\"children\":");
                        BuildTree(sb, children, depth + 1, maxDepth);
                    }
                    else
                    {
                        sb.Append(",\"childCount\":").Append(t.childCount);
                    }
                }

                sb.Append('}');
            }
            sb.Append(']');
        }
    }
}
