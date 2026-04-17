using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取当前场景的 GameObject 树结构（名称+组件列表）。
    /// </summary>
    public class HierarchyTool : IMcpTool
    {
        public string Name => "editor_getHierarchy";
        public string Category => "editor";
        public string Description => "获取当前场景的 GameObject 树结构";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"maxDepth\":{\"type\":\"integer\",\"description\":\"最大遍历深度，-1 表示无限制\",\"default\":-1}}}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            int maxDepth = -1;
            if (parameters != null && parameters.TryGetValue("maxDepth", out var raw))
            {
                if (raw is long l) maxDepth = (int)l;
                else if (raw is double d) maxDepth = (int)d;
                else if (raw is int i) maxDepth = i;
            }

            var scene = SceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();

            var sb = new StringBuilder();
            BuildTree(sb, roots, 0, maxDepth);
            return Task.FromResult(ToolResult.Success(sb.ToString()));
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
