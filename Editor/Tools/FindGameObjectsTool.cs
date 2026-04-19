using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：按名称/组件类型搜索场景中的 GameObject。
    /// 支持通配符模式（* 和 ?）、子串匹配、组件类型过滤、activeOnly 过滤和结果数量限制。
    /// </summary>
    public class FindGameObjectsTool : IMcpTool
    {
        /// <inheritdoc />
        public string Name => "editor_findGameObjects";

        /// <inheritdoc />
        public string Category => "editor";

        /// <inheritdoc />
        public string Description => "按名称/组件类型搜索场景中的 GameObject";

        /// <inheritdoc />
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"namePattern\":{\"type\":\"string\",\"description\":\"名称匹配模式（支持 * 和 ? 通配符，无通配符时为子串匹配）\"},\"componentType\":{\"type\":\"string\",\"description\":\"组件类型简短类名（如 Camera、MeshRenderer），大小写不敏感\"},\"maxResults\":{\"type\":\"integer\",\"description\":\"最大返回数量（默认 50）\",\"default\":50},\"activeOnly\":{\"type\":\"boolean\",\"description\":\"是否仅搜索激活状态的 GO（默认 true）\",\"default\":true}}}";

        /// <inheritdoc />
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            // 1. 解析参数
            string namePattern = null;
            if (parameters != null && parameters.TryGetValue("namePattern", out var npRaw))
            {
                var s = npRaw as string;
                if (!string.IsNullOrEmpty(s)) namePattern = s;
            }

            string componentType = null;
            if (parameters != null && parameters.TryGetValue("componentType", out var ctRaw))
            {
                var s = ctRaw as string;
                if (!string.IsNullOrEmpty(s)) componentType = s;
            }

            int maxResults = 50;
            if (parameters != null && parameters.TryGetValue("maxResults", out var mrRaw))
            {
                if (mrRaw is long l) maxResults = (int)l;
                else if (mrRaw is double d) maxResults = (int)d;
                else if (mrRaw is int i) maxResults = i;
            }

            bool activeOnly = true;
            if (parameters != null && parameters.TryGetValue("activeOnly", out var aoRaw))
            {
                if (aoRaw is bool b) activeOnly = b;
            }

            // 2. 校验
            if (namePattern == null && componentType == null)
                return Task.FromResult(ToolResult.Error("至少需要提供 namePattern 或 componentType 参数"));

            if (maxResults < 1)
                return Task.FromResult(ToolResult.Error("maxResults 必须为正整数"));

            // 3. 解析根节点（Prefab Stage 优先，回退 Active Scene）
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            GameObject[] roots;
            if (stage != null)
                roots = new[] { stage.prefabContentsRoot };
            else
                roots = SceneManager.GetActiveScene().GetRootGameObjects();

            // 4. 递归搜索
            var results = new List<GameObject>();
            int totalFound = 0;
            for (int i = 0; i < roots.Length; i++)
                SearchRecursive(roots[i].transform, namePattern, componentType, activeOnly, results, maxResults, ref totalFound);

            // 5. 构建 JSON
            bool truncated = totalFound > maxResults;
            var sb = new StringBuilder();
            sb.Append("{\"results\":[");
            for (int i = 0; i < results.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var go = results[i];
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(go.name));
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(GameObjectPathHelper.GetGameObjectPath(go)));
                sb.Append(",\"instanceID\":");
                sb.Append(go.GetInstanceID());
                sb.Append(",\"components\":[");
                var comps = go.GetComponents<Component>();
                bool first = true;
                for (int c = 0; c < comps.Length; c++)
                {
                    if (comps[c] == null) continue;
                    if (!first) sb.Append(',');
                    sb.Append(MiniJson.SerializeString(comps[c].GetType().Name));
                    first = false;
                }
                sb.Append("]}");
            }
            sb.Append("],\"count\":");
            sb.Append(results.Count);
            if (truncated)
            {
                sb.Append(",\"truncated\":true,\"totalFound\":");
                sb.Append(totalFound);
            }
            sb.Append('}');

            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }

        /// <summary>
        /// 递归搜索 GameObject 树，应用名称和组件过滤条件。
        /// </summary>
        private static void SearchRecursive(Transform transform, string namePattern, string componentType, bool activeOnly, List<GameObject> results, int maxResults, ref int totalFound)
        {
            var go = transform.gameObject;

            if (activeOnly && !go.activeInHierarchy)
                return;

            bool matches = true;
            if (namePattern != null && !MatchesName(go, namePattern))
                matches = false;
            if (matches && componentType != null && !MatchesComponent(go, componentType))
                matches = false;

            if (matches)
            {
                totalFound++;
                if (results.Count < maxResults)
                    results.Add(go);
            }

            for (int i = 0; i < transform.childCount; i++)
                SearchRecursive(transform.GetChild(i), namePattern, componentType, activeOnly, results, maxResults, ref totalFound);
        }

        /// <summary>
        /// 名称匹配：无通配符时为子串匹配（大小写不敏感），有通配符时使用 WildcardMatch。
        /// </summary>
        private static bool MatchesName(GameObject go, string namePattern)
        {
            if (namePattern.IndexOf('*') < 0 && namePattern.IndexOf('?') < 0)
                return go.name.IndexOf(namePattern, System.StringComparison.OrdinalIgnoreCase) >= 0;

            return WildcardMatch(namePattern, go.name);
        }

        /// <summary>
        /// 组件类型匹配：遍历所有组件，比较简短类名（大小写不敏感）。
        /// </summary>
        private static bool MatchesComponent(GameObject go, string componentType)
        {
            var comps = go.GetComponents<Component>();
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null) continue;
                if (string.Equals(comps[i].GetType().Name, componentType, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 大小写不敏感的通配符匹配。* = 零或多个任意字符，? = 恰好一个字符。
        /// 使用双指针实现。
        /// </summary>
        internal static bool WildcardMatch(string pattern, string text)
        {
            int pIdx = 0, tIdx = 0;
            int starIdx = -1, matchIdx = 0;

            var pLower = pattern.ToLowerInvariant();
            var tLower = text.ToLowerInvariant();

            while (tIdx < tLower.Length)
            {
                if (pIdx < pLower.Length && (pLower[pIdx] == '?' || pLower[pIdx] == tLower[tIdx]))
                {
                    pIdx++;
                    tIdx++;
                }
                else if (pIdx < pLower.Length && pLower[pIdx] == '*')
                {
                    starIdx = pIdx;
                    matchIdx = tIdx;
                    pIdx++;
                }
                else if (starIdx >= 0)
                {
                    pIdx = starIdx + 1;
                    matchIdx++;
                    tIdx = matchIdx;
                }
                else
                {
                    return false;
                }
            }

            while (pIdx < pLower.Length && pLower[pIdx] == '*')
                pIdx++;

            return pIdx == pLower.Length;
        }
    }
}
