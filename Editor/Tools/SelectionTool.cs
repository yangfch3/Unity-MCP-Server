using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UnityMcp.Editor.Tools
{
    /// <summary>
    /// MCP 工具：获取当前 Hierarchy/Project 中选中的对象信息。
    /// </summary>
    public class SelectionTool : IMcpTool
    {
        public string Name => "editor_getSelection";
        public string Category => "editor";
        public string Description => "获取当前 Hierarchy/Project 中选中的对象信息";
        public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

        public Task<ToolResult> Execute(Dictionary<string, object> parameters)
        {
            var sb = new StringBuilder();
            sb.Append("{\"gameObjects\":[");

            var gameObjects = Selection.gameObjects;
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (i > 0) sb.Append(',');
                var go = gameObjects[i];
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(go.name));
                sb.Append(",\"instanceID\":");
                sb.Append(go.GetInstanceID());
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(GetGameObjectPath(go)));
                sb.Append('}');
            }

            sb.Append("],\"assets\":[");

            var guids = Selection.assetGUIDs;
            for (int i = 0; i < guids.Length; i++)
            {
                if (i > 0) sb.Append(',');
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                string name = Path.GetFileName(path);
                sb.Append("{\"name\":");
                sb.Append(MiniJson.SerializeString(name));
                sb.Append(",\"path\":");
                sb.Append(MiniJson.SerializeString(path));
                sb.Append('}');
            }

            sb.Append("]}");
            return Task.FromResult(ToolResult.Success(sb.ToString()));
        }

        private static string GetGameObjectPath(GameObject go)
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
