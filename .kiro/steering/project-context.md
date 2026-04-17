---
inclusion: always
---

# Unity MCP Server — Project Context

## Project Overview

MCP (Model Context Protocol) server plugin for Unity Editor, distributed as a Unity Package. Allows external AI Agents to access Unity Editor capabilities via the standard MCP protocol.

## Tech Stack

- C# / Unity 2022.3+
- Editor assembly only (does not affect runtime builds)
- Streamable HTTP transport (MCP 2025-03-26 spec)
- No external dependencies (built-in MiniJson replaces Newtonsoft.Json)

## Code Structure

```
Editor/
├── Core/       # IMcpTool interface, ToolResult, ToolRegistry
├── Protocol/   # JsonRpcDispatcher, MiniJson
├── Server/     # McpServer, McpServerManager, MainThreadQueue
├── Tools/      # Built-in tools (13 tools in debug/editor/build categories)
└── UI/         # ConfigPanel EditorWindow
```

## Key Design Constraints

- All Unity API calls must execute on the main thread (dispatched via MainThreadQueue)
- HttpListener runs on a background thread
- Adding a new tool only requires implementing the `IMcpTool` interface; ToolRegistry auto-discovers via reflection
- After Domain Reload, the service auto-recovers using an EditorPrefs flag
- Namespace: `UnityMcp.Editor`, tool sub-namespace: `UnityMcp.Editor.Tools`

## Coding Standards

- File encoding: UTF-8 LF, no BOM, trailing newline
- XML doc comments on all public members
- Log prefix: `[McpServer]` / `[ToolRegistry]`
- Tool naming: `{category}_{action}` (e.g., `console_getLogs`, `menu_execute`)
