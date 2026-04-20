using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取当前 Unity 项目的根目录路径和 Assets 路径。
    /// </summary>
    public class ProjectPathTool : IMcpTool
    {
        public string Name => "editor_getProjectPath";
        public string Category => "editor";
        public string Description => "获取当前 Unity 项目的根目录路径";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            string assetsPath = Application.dataPath;
            string projectPath = Directory.GetParent(assetsPath).FullName;

            // 统一使用正斜杠
            projectPath = projectPath.Replace('\\', '/');
            assetsPath = assetsPath.Replace('\\', '/');

            var sb = new StringBuilder();
            sb.Append("{\"projectPath\":");
            sb.Append(MiniJson.SerializeString(projectPath));
            sb.Append(",\"assetsPath\":");
            sb.Append(MiniJson.SerializeString(assetsPath));
            sb.Append('}');

            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }
    }
}
