using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：删除指定 Assets 子目录并刷新 AssetDatabase。
    /// 执行前会校验路径安全性，确保目标路径位于 Assets 目录下。
    /// </summary>
    public class AssetDeleteFolderTool : IMcpTool
    {
        /// <summary>工具名称。</summary>
        public string Name => "asset_deleteFolder";

        /// <summary>所属分类。</summary>
        public string Category => "editor";

        /// <summary>工具描述。</summary>
        public string Description => "删除指定 Assets 子目录并刷新 AssetDatabase";

        /// <summary>JSON Schema 描述参数。</summary>
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"path\":{\"type\":\"string\",\"description\":\"要删除的目录路径（相对于项目根目录，如 Assets/XLua/Gen）\"}},\"required\":[\"path\"]}";

        /// <summary>执行删除目录逻辑。</summary>
        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            // 1. Extract path parameter
            string path = null;
            if (parameters != null && parameters.TryGetValue("path", out var rawPath) && rawPath != null)
                path = rawPath.ToString();

            if (string.IsNullOrEmpty(path))
                return Task.FromResult(ToolResult.Error("path parameter is required"));

            // 2. Path safety validation
            string fullPath = ValidatePath(path);
            if (fullPath == null)
                return Task.FromResult(ToolResult.Error($"path is outside the Assets folder: {path}"));

            // 3. Check directory exists
            if (!Directory.Exists(fullPath))
                return Task.FromResult(ToolResult.Error($"directory not found: {path}"));

            // 4. Delete directory
            try
            {
                Directory.Delete(fullPath, true);

                // Delete the .meta file if it exists
                string metaPath = fullPath + ".meta";
                if (File.Exists(metaPath))
                    File.Delete(metaPath);

                AssetDatabase.Refresh();
                return Task.FromResult(ToolResult.Success($"Deleted: {path}"));
            }
            catch (Exception ex)
            {
                return Task.FromResult(ToolResult.Error($"failed to delete: {ex.Message}"));
            }
        }

        /// <summary>
        /// 校验路径安全性，确保解析后的路径位于 Assets 目录下。
        /// </summary>
        /// <param name="path">相对于项目根目录的路径。</param>
        /// <returns>规范化后的完整路径；若路径不安全则返回 null。</returns>
        internal static string ValidatePath(string path)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullPath = Path.GetFullPath(Path.Combine(projectRoot, path));
            string assetsDir = Application.dataPath;

            // Must be exactly Assets dir or a child of it (prevent matching "AssetsExtra" etc.)
            if (fullPath == assetsDir ||
                fullPath.StartsWith(assetsDir + Path.DirectorySeparatorChar))
            {
                return fullPath;
            }

            return null;
        }
    }
}
