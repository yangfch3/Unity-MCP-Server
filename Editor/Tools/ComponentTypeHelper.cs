using System;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// 共享辅助类：提供组件类型查找等通用方法。
    /// 通过简短类名（大小写不敏感）在已加载程序集中查找 Component 子类型，
    /// 或在 GameObject 上查找第一个匹配的组件实例。
    /// </summary>
    internal static class ComponentTypeHelper
    {
        /// <summary>
        /// 通过简短类名查找继承自 <see cref="Component"/> 的 <see cref="Type"/>（大小写不敏感）。
        /// 遍历 <see cref="AppDomain.CurrentDomain"/> 中所有已加载程序集。
        /// </summary>
        /// <param name="shortName">组件的简短类名，如 "BoxCollider"。</param>
        /// <returns>匹配的 <see cref="Type"/>，未找到时返回 <c>null</c>。</returns>
        internal static Type FindType(string shortName)
        {
            if (string.IsNullOrEmpty(shortName))
                return null;

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException)
                {
                    continue;
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract)
                        continue;
                    if (!typeof(Component).IsAssignableFrom(type))
                        continue;
                    if (string.Equals(type.Name, shortName, StringComparison.OrdinalIgnoreCase))
                        return type;
                }
            }

            return null;
        }

        /// <summary>
        /// 在指定 <see cref="GameObject"/> 上查找第一个类型名匹配的组件（大小写不敏感）。
        /// </summary>
        /// <param name="go">目标 GameObject。</param>
        /// <param name="typeName">组件的简短类名，如 "BoxCollider"。</param>
        /// <returns>匹配的 <see cref="Component"/>，未找到时返回 <c>null</c>。</returns>
        internal static Component FindComponent(GameObject go, string typeName)
        {
            if (go == null || string.IsNullOrEmpty(typeName))
                return null;

            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                    continue;
                if (string.Equals(comp.GetType().Name, typeName, StringComparison.OrdinalIgnoreCase))
                    return comp;
            }

            return null;
        }
    }
}
