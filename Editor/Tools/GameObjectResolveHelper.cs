using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// 共享辅助类：提供 instanceID / path 双模式 GameObject 定位，
    /// 以及 Prefab Stage 优先回退 Active Scene 的路径查找。
    /// </summary>
    internal static class GameObjectResolveHelper
    {
        /// <summary>
        /// 从参数字典中提取 instanceID 和 path，定位 GameObject。
        /// instanceID 优先于 path，空字符串视为未提供。
        /// </summary>
        /// <returns>
        /// go != null 表示成功；go == null 时 errorMessage 包含错误描述。
        /// </returns>
        internal static (GameObject go, string errorMessage) Resolve(Dictionary<string, object> parameters)
        {
            return Resolve(parameters, "instanceID", "path");
        }

        /// <summary>
        /// 从参数字典中按指定 key 名提取 instanceID 和 path，定位 GameObject。
        /// 用于 AddGameObjectTool 的 parentInstanceID / parentPath 等自定义参数名。
        /// </summary>
        /// <returns>
        /// go != null 表示成功；go == null 时 errorMessage 包含错误描述。
        /// </returns>
        internal static (GameObject go, string errorMessage) Resolve(
            Dictionary<string, object> parameters,
            string instanceIDKey,
            string pathKey)
        {
            int instanceID = 0;
            bool hasInstanceID = false;
            if (parameters != null && parameters.TryGetValue(instanceIDKey, out var rawId) && rawId != null)
            {
                hasInstanceID = true;
                if (rawId is long l) instanceID = (int)l;
                else if (rawId is double d) instanceID = (int)d;
                else if (rawId is int i) instanceID = i;
            }

            string path = null;
            if (parameters != null && parameters.TryGetValue(pathKey, out var rawPath))
            {
                path = rawPath as string;
                if (string.IsNullOrEmpty(path))
                    path = null;
            }

            if (hasInstanceID)
            {
                var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
                if (go == null)
                    return (null, $"未找到 instanceID: {instanceID}");
                return (go, null);
            }

            if (path != null)
            {
                var go = FindByPath(path);
                if (go == null)
                    return (null, $"未找到: {path}");
                return (go, null);
            }

            return (null, $"{instanceIDKey} 或 {pathKey} 参数至少提供一个");
        }

        /// <summary>
        /// 按路径查找 GameObject。Prefab Stage 优先，回退 Active Scene。
        /// </summary>
        internal static GameObject FindByPath(string path)
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
        internal static GameObject SearchInRoot(GameObject root, string normalizedPath)
        {
            var segments = normalizedPath.Split('/');
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
