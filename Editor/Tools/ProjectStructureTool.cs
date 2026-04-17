using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取 Assets 目录结构（可限定深度），排除 .meta 文件。
    /// </summary>
    public class ProjectStructureTool : IMcpTool
    {
        public string Name => "editor_getProjectStructure";
        public string Category => "editor";
        public string Description => "获取 Assets 目录结构";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{\"maxDepth\":{\"type\":\"integer\",\"description\":\"最大遍历深度\",\"default\":3}}}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            int maxDepth = 3;
            if (parameters != null && parameters.TryGetValue("maxDepth", out var raw))
            {
                if (raw is long l) maxDepth = (int)l;
                else if (raw is double d) maxDepth = (int)d;
                else if (raw is int i) maxDepth = i;
            }
            if (maxDepth < 1) maxDepth = 1;

            string assetsPath = Application.dataPath; // 绝对路径到 Assets
            string projectRoot = Directory.GetParent(assetsPath).FullName;

            var sb = new StringBuilder();
            BuildDirTree(sb, assetsPath, projectRoot, 0, maxDepth);
            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }

        private static void BuildDirTree(StringBuilder sb, string dirPath, string projectRoot, int depth, int maxDepth)
        {
            sb.Append('[');
            bool first = true;

            // 先输出子目录
            string[] dirs;
            try { dirs = Directory.GetDirectories(dirPath); }
            catch { dirs = new string[0]; }

            foreach (var dir in dirs)
            {
                string name = Path.GetFileName(dir);
                if (!first) sb.Append(',');
                first = false;

                string relativePath = GetRelativePath(dir, projectRoot);
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(name));
                sb.Append(",\"type\":\"directory\",\"path\":");
                sb.Append(MiniJson.SerializeString(relativePath));

                sb.Append(",\"children\":");
                if (depth < maxDepth - 1)
                    BuildDirTree(sb, dir, projectRoot, depth + 1, maxDepth);
                else
                    sb.Append("[]");

                sb.Append('}');
            }

            // 再输出文件（排除 .meta）
            string[] files;
            try { files = Directory.GetFiles(dirPath); }
            catch { files = new string[0]; }

            foreach (var file in files)
            {
                if (file.EndsWith(".meta")) continue;

                string name = Path.GetFileName(file);
                if (!first) sb.Append(',');
                first = false;

                string relativePath = GetRelativePath(file, projectRoot);
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(name));
                sb.Append(",\"type\":\"file\",\"path\":");
                sb.Append(MiniJson.SerializeString(relativePath));
                sb.Append('}');
            }

            sb.Append(']');
        }

        private static string GetRelativePath(string fullPath, string basePath)
        {
            // 统一分隔符为正斜杠
            string normalized = fullPath.Replace('\\', '/');
            string normalizedBase = basePath.Replace('\\', '/');
            if (!normalizedBase.EndsWith("/"))
                normalizedBase += "/";

            if (normalized.StartsWith(normalizedBase))
                return normalized.Substring(normalizedBase.Length);

            return normalized;
        }
    }
}
