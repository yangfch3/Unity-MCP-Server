using System.Collections.Generic;

namespace UnityMcp.Editor
{
    /// <summary>
    /// MCP 工具执行结果。支持成功（content 列表）和错误两种状态。
    /// MCP 协议格式: { "content": [{ "type": "text", "text": "..." }] }
    /// </summary>
    public class ToolResult
    {
        public bool IsError { get; }
        public List<ContentItem> Content { get; }

        private ToolResult(bool isError, List<ContentItem> content)
        {
            IsError = isError;
            Content = content;
        }

        /// <summary>创建成功结果（单条文本）</summary>
        public static ToolResult Success(string text)
        {
            return new ToolResult(false, new List<ContentItem>
            {
                new ContentItem("text", text)
            });
        }

        /// <summary>创建成功结果（多条内容）</summary>
        public static ToolResult Success(List<ContentItem> content)
        {
            return new ToolResult(false, content);
        }

        /// <summary>创建错误结果</summary>
        public static ToolResult Error(string message)
        {
            return new ToolResult(true, new List<ContentItem>
            {
                new ContentItem("text", message)
            });
        }
    }

    /// <summary>
    /// MCP 内容项。对应协议中的 { "type": "text", "text": "..." }
    /// </summary>
    public class ContentItem
    {
        public string Type { get; }
        public string Text { get; }

        public ContentItem(string type, string text)
        {
            Type = type;
            Text = text;
        }
    }
}
