using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取 Assets 目录结构（可限定深度），排除 .meta 文件。
    /// 目录节点必带 children（超出深度时为空数组），文件节点只有 name。
    /// </summary>
    public class ProjectStructureTool : IMcpTool
    {
        public string Name => "editor_getProjectStructure";
        public string Category => "editor";
        public string Description => "获取 Assets 目录树（目录带 children，文件只有 name）";
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

            var sb = new StringBuilder();
            BuildDirTree(sb, Application.dataPath, 0, maxDepth);
            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }

        private static void BuildDirTree(StringBuilder sb, string dirPath, int depth, int maxDepth)
        {
            sb.Append('[');
            bool first = true;

            // 先输出子目录
            string[] dirs;
            try { dirs = Directory.GetDirectories(dirPath); }
            catch { dirs = new string[0]; }

            foreach (var dir in dirs)
            {
                if (!first) sb.Append(',');
                first = false;

                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(Path.GetFileName(dir)));
                sb.Append(",\"children\":");
                if (depth < maxDepth - 1)
                    BuildDirTree(sb, dir, depth + 1, maxDepth);
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

                if (!first) sb.Append(',');
                first = false;

                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(Path.GetFileName(file)));
                sb.Append('}');
            }

            sb.Append(']');
        }
    }
}
