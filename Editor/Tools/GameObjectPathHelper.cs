using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// 共享辅助类：提供 GameObject 路径计算等通用方法。
    /// </summary>
    internal static class GameObjectPathHelper
    {
        /// <summary>
        /// 计算 GameObject 的绝对路径（如 "/Root/Child/Target"）。
        /// </summary>
        internal static string GetGameObjectPath(GameObject go)
        {
            var path = go.name;
            var t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return "/" + path;
        }
    }
}
