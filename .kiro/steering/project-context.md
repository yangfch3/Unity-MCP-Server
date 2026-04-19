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
├── Tools/      # Built-in tools (17 tools in debug/editor/build categories), GameObjectPathHelper shared utility
└── UI/         # ConfigPanel EditorWindow
```

## Key Design Constraints

- All Unity API calls must execute on the main thread (dispatched via MainThreadQueue)
- All `IMcpTool.Execute` calls are already dispatched to the main thread by `JsonRpcDispatcher` via `MainThreadQueue`; tool implementations do not need to handle thread dispatch internally
- HttpListener runs on a background thread
- Adding a new tool only requires implementing the `IMcpTool` interface; ToolRegistry auto-discovers via reflection
- After Domain Reload, the service auto-recovers using an EditorPrefs flag
- Namespace: `UnityMcp.Editor`, tool sub-namespace: `UnityMcp.Editor.Tools`

## Unity MCP Tool Usage Guidelines

- `build_compile` and `build_runTests` will trigger Domain Reload, which aborts the MCP server's HTTP thread and causes the current request to fail. Only call these tools at explicit checkpoint tasks (e.g., "Compile Checkpoint", "Final Test Checkpoint"), NOT after every sub-task.
- For incremental code validation during implementation, use `getDiagnostics` (IDE static analysis) instead of `build_compile`. It does not trigger Domain Reload.
- When `build_compile` or `build_runTests` returns a connection error (e.g., "fetch failed"), it is likely due to Domain Reload. Wait briefly and retry once if needed.

## Coding Standards

- File encoding: UTF-8 LF, no BOM, trailing newline
- XML doc comments on all public members
- Log prefix: `[McpServer]` / `[ToolRegistry]`
- Tool naming: `{category}_{action}` (e.g., `console_getLogs`, `menu_execute`)

## Testing Standards

- All new or modified Tools must include corresponding unit tests
- Test framework: NUnit (Unity Test Runner EditMode), files in `Tests/Editor/`
- Each new Tool requires: Name/Category assertion, parameter validation tests, ToolRegistry auto-discovery test, core functionality tests
- New Tools must be added to `Tests/Editor/ToolRegistryTests.cs` assertions
- Property tests (random input, 100+ iterations) recommended for security/filtering logic, tagged `[Category("Slow")]`
- Shared test helpers go in dedicated helper files under `Tests/Editor/` to avoid duplication
