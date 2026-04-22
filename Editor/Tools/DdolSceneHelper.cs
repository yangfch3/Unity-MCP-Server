using System.Collections.Generic;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// 共享辅助类：获取 DontDestroyOnLoad 场景中的根 GameObject。
    /// 仅 PlayMode 下有效，Editor 模式返回空数组。
    /// </summary>
    internal static class DdolSceneHelper
    {
        /// <summary>
        /// 获取 DontDestroyOnLoad 场景的所有根 GameObject。
        /// </summary>
        internal static GameObject[] GetRootGameObjects()
        {
            if (!Application.isPlaying)
                return System.Array.Empty<GameObject>();

            var allTransforms = Object.FindObjectsOfType<Transform>(true);
            var roots = new List<GameObject>();
            for (int i = 0; i < allTransforms.Length; i++)
            {
                var t = allTransforms[i];
                if (t.parent == null && t.gameObject.scene.name == "DontDestroyOnLoad")
                    roots.Add(t.gameObject);
            }
            return roots.ToArray();
        }
    }
}
