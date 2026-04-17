---
inclusion: always
---

# Unity MCP Server — 项目上下文

## 项目定位

Unity Editor 的 MCP (Model Context Protocol) 服务插件，以 Unity Package 形式分发。允许外部 AI Agent 通过标准 MCP 协议访问 Unity Editor 功能。

## 技术栈

- C# / Unity 2022.3+
- 仅 Editor 程序集（不影响运行时构建）
- Streamable HTTP 传输（MCP 2025-03-26 规范）
- 无外部依赖（内置 MiniJson 替代 Newtonsoft.Json）

## 代码结构

```
Editor/
├── Core/       # IMcpTool 接口、ToolResult、ToolRegistry
├── Protocol/   # JsonRpcDispatcher、MiniJson
├── Server/     # McpServer、McpServerManager、MainThreadQueue
├── Tools/      # 内置工具（13 个，分 debug/editor/build 三类）
└── UI/         # ConfigPanel EditorWindow
```

## 关键设计约束

- 所有 Unity API 调用必须在主线程执行（通过 MainThreadQueue 调度）
- HttpListener 在后台线程运行
- 新增工具只需实现 `IMcpTool` 接口，ToolRegistry 通过反射自动发现
- Domain Reload 后服务通过 EditorPrefs 标记自动恢复
- 命名空间：`UnityMcp.Editor`，工具子命名空间：`UnityMcp.Editor.Tools`

## 编码规范

- 文件编码 UTF-8 LF，无 BOM，末尾留空行
- XML 文档注释覆盖所有 public 成员
- 日志统一使用 `[McpServer]` / `[ToolRegistry]` 前缀
- 工具命名：`{category}_{action}`（如 `console_getLogs`、`menu_execute`）
