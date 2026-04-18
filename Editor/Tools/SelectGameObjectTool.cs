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
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\",\"description\":\"要选中的 GameObject 路径（如 \\\"/Root/Child/Target\\\"）\"}},\"required\":[\"path\"]}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            string path = null;
            if (parameters != null && parameters.TryGetValue("path", out var raw))
                path = raw as string;

            if (string.IsNullOrEmpty(path))
                return Task.FromResult(ToolResult.Error("path 参数不能为空"));

            var go = FindByPath(path);
            if (go == null)
                return Task.FromResult(ToolResult.Error($"未找到: {path}"));

            Selection.activeGameObject = go;

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

        /// <summary>
        /// 按路径查找 GameObject。Prefab Stage 优先，回退 Active Scene。
        /// </summary>
        private static GameObject FindByPath(string path)
        {
            var normalizedPath = path.TrimStart('/');

            // 1. 尝试 Prefab Stage
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                var result = SearchInRoot(stage.prefabContentsRoot, normalizedPath);
                if (result != null) return result;
            }

            // 2. 回退 Active Scene
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in roots)
            {
                var result = SearchInRoot(root, normalizedPath);
                if (result != null) return result;
            }

            return null;
        }

        /// <summary>
        /// 在指定根节点下按路径段逐级查找 GameObject。
        /// </summary>
        private static GameObject SearchInRoot(GameObject root, string path)
        {
            var segments = path.Split('/');
            if (segments.Length == 0 || segments[0] != root.name)
                return null;

            var current = root.transform;
            for (int i = 1; i < segments.Length; i++)
            {
                var child = current.Find(segments[i]);
                if (child == null) return null;
                current = child;
            }

            return current.gameObject;
        }
    }
}
