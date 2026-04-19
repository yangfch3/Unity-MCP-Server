using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityMcp.Editor
{
    /// <summary>
    /// 工具注册中心。管理所有 IMcpTool 的注册、查找与分类列出。
    /// 支持反射自动发现所有 IMcpTool 非抽象实现类。
    /// </summary>
    public class ToolRegistry
    {
        private readonly Dictionary<string, IMcpTool> _tools = new Dictionary<string, IMcpTool>();

        /// <summary>注册单个工具。重复注册时覆盖并输出警告。</summary>
        public void Register(IMcpTool tool)
        {
            if (tool == null)
                throw new ArgumentNullException(nameof(tool));

            if (_tools.ContainsKey(tool.Name))
            {
                Debug.LogWarning($"[ToolRegistry] Tool '{tool.Name}' already registered, overwriting.");
            }

            _tools[tool.Name] = tool;
        }

        /// <summary>按名称查找工具，未找到返回 null。</summary>
        public IMcpTool Resolve(string name)
        {
            if (name == null)
                return null;

            _tools.TryGetValue(name, out var tool);
            return tool;
        }

        /// <summary>返回所有已注册工具的信息列表。</summary>
        public List<ToolInfo> ListAll()
        {
            return _tools.Values
                .Select(t => new ToolInfo(t.Name, t.Category, t.Description, t.InputSchema))
                .ToList();
        }

        /// <summary>返回指定分类下的工具信息列表。</summary>
        public List<ToolInfo> ListByCategory(string category)
        {
            if (category == null)
                return new List<ToolInfo>();

            return _tools.Values
                .Where(t => t.Category == category)
                .Select(t => new ToolInfo(t.Name, t.Category, t.Description, t.InputSchema))
                .ToList();
        }

        /// <summary>
        /// 反射扫描当前 AppDomain 中所有实现 IMcpTool 的非抽象类，
        /// 实例化并注册。新工具只需实现接口即可被自动发现。
        /// </summary>
        public void AutoDiscover()
        {
            var toolType = typeof(IMcpTool);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (System.Reflection.ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type.IsAbstract || type.IsInterface || type.IsNested || !toolType.IsAssignableFrom(type))
                        continue;

                    try
                    {
                        var instance = (IMcpTool)Activator.CreateInstance(type);
                        Register(instance);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ToolRegistry] Failed to instantiate {type.FullName}: {ex.Message}");
                    }
                }
            }
        }
    }

    /// <summary>工具信息 DTO，用于列表返回。</summary>
    public class ToolInfo
    {
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
        public string InputSchema { get; }

        public ToolInfo(string name, string category, string description, string inputSchema)
        {
            Name = name;
            Category = category;
            Description = description;
            InputSchema = inputSchema;
        }
    }
}
