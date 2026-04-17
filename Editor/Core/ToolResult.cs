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

        /// <summary>创建成功结果（单张图片，base64 编码）</summary>
        public static ToolResult SuccessImage(string base64Data, string mimeType)
        {
            return new ToolResult(false, new List<ContentItem>
            {
                new ContentItem("image", base64Data, mimeType)
            });
        }
    }

    /// <summary>
    /// MCP 内容项。支持 text 和 image 两种类型。
    /// text: { "type": "text", "text": "..." }
    /// image: { "type": "image", "data": "base64...", "mimeType": "image/png" }
    /// </summary>
    public class ContentItem
    {
        public string Type { get; }
        public string Text { get; }

        /// <summary>image 类型的 base64 编码数据（text 类型时为 null）</summary>
        public string Data { get; }

        /// <summary>image 类型的 MIME 类型（text 类型时为 null）</summary>
        public string MimeType { get; }

        /// <summary>创建 text 类型内容项</summary>
        public ContentItem(string type, string text)
        {
            Type = type;
            Text = text;
        }

        /// <summary>创建 image 类型内容项</summary>
        public ContentItem(string type, string data, string mimeType)
        {
            Type = type;
            Data = data;
            MimeType = mimeType;
        }
    }
}
