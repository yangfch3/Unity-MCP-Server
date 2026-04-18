using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityMcp.Editor.Tests
{
    /// <summary>
    /// HierarchyTool / SelectGameObjectTool 测试共享辅助方法。
    /// </summary>
    internal static class HierarchyToolTestHelper
    {
        /// <summary>
        /// 生成随机 GameObject 树，返回根节点和所有节点列表。
        /// </summary>
        /// <param name="maxDepth">最大深度（0 = 仅根节点）。</param>
        /// <param name="maxChildren">每个节点的最大子节点数。</param>
        /// <param name="random">可选的 Random 实例，默认自动创建。</param>
        /// <returns>元组：(root, allNodes)。</returns>
        internal static (GameObject root, List<GameObject> allNodes) CreateRandomTree(
            int maxDepth, int maxChildren, System.Random random = null)
        {
            if (random == null) random = new System.Random();
            var allNodes = new List<GameObject>();
            var root = new GameObject($"Root_{random.Next(1000)}");
            allNodes.Add(root);
            if (maxDepth > 0)
                AddChildren(root.transform, 1, maxDepth, maxChildren, random, allNodes);
            return (root, allNodes);
        }

        /// <summary>
        /// 计算 GameObject 的完整路径（如 "/Root/Child/GrandChild"）。
        /// </summary>
        internal static string GetGameObjectPath(GameObject go)
        {
            if (go == null) return string.Empty;
            var parts = new List<string>();
            var current = go.transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }
            parts.Reverse();
            return "/" + string.Join("/", parts);
        }

        /// <summary>
        /// 解析 HierarchyTool 输出的 JSON 字符串，返回最大嵌套深度。
        /// 深度 0 表示根数组中的节点本身，1 表示其直接子节点，以此类推。
        /// 供 Property 3（maxDepth 一致性）属性测试使用。
        /// </summary>
        internal static int MeasureJsonTreeDepth(string json)
        {
            var parsed = MiniJson.Deserialize(json);
            if (parsed is List<object> arr)
                return MeasureArrayDepth(arr);
            return -1;
        }

        /// <summary>
        /// TearDown 时销毁所有测试 GameObject。
        /// </summary>
        internal static void CleanupGameObjects(List<GameObject> list)
        {
            if (list == null) return;
            foreach (var go in list)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            list.Clear();
        }

        // ── private helpers ──

        private static void AddChildren(
            Transform parent, int currentDepth, int maxDepth, int maxChildren,
            System.Random random, List<GameObject> allNodes)
        {
            int count = random.Next(1, maxChildren + 1);
            for (int i = 0; i < count; i++)
            {
                var child = new GameObject($"Node_d{currentDepth}_{i}_{random.Next(1000)}");
                child.transform.SetParent(parent);
                allNodes.Add(child);
                if (currentDepth < maxDepth)
                    AddChildren(child.transform, currentDepth + 1, maxDepth, maxChildren, random, allNodes);
            }
        }

        /// <summary>递归测量 JSON 节点数组的最大深度。</summary>
        private static int MeasureArrayDepth(List<object> nodes)
        {
            if (nodes == null || nodes.Count == 0) return -1;
            int max = 0;
            foreach (var item in nodes)
            {
                if (item is Dictionary<string, object> dict &&
                    dict.TryGetValue("children", out var childrenRaw) &&
                    childrenRaw is List<object> children)
                {
                    int childDepth = MeasureArrayDepth(children);
                    int nodeDepth = childDepth < 0 ? 0 : 1 + childDepth;
                    if (nodeDepth > max) max = nodeDepth;
                }
            }
            return max;
        }
    }
}
