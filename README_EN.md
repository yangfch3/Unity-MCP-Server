# Unity MCP Server

[中文](README.md) | English

Unity Editor plugin that exposes Unity Editor capabilities to external AI Agents via [MCP (Model Context Protocol)](https://modelcontextprotocol.io/).

Agents (e.g., Kiro, Cursor, Claude Code) can connect to Unity Editor through the standard MCP protocol and invoke editor functions.

## Philosophy

This plugin is positioned as an AI Agent's **Unity perception layer + diagnostic toolchain**:

- **Perception first** — Prioritizes reading scene state, gathering context, and assisting diagnostics, enabling the Agent to fully understand the current project state and support development decisions.
- **Structured writes** — Provides safe, auditable write operations (e.g., modifying properties, adding/removing nodes), but does not aim to cover every Unity Editor GUI interaction.
- **Not a replacement for the Editor** — The goal is to enhance collaboration efficiency between engineers and Agents, not to replicate the entire Editor inside a chat window.

## Features

- **Streamable HTTP Transport** — Based on MCP 2025-03-26 spec, single HTTP endpoint
- **In-Process** — No external Node.js/Python process required, runs directly inside the Editor
- **Extensible Tool System** — Implement the `IMcpTool` interface to register new tools with zero core code changes
- **Domain Reload Auto-Recovery** — Service automatically restarts after entering/exiting PlayMode

### Built-in Tools

For full parameter details and usage examples, see the [Tools Reference](Docs/TOOLS_EN.md).

#### Debug Tools

| Tool | Description |
|------|-------------|
| `console_getLogs` | Get Unity Console logs (with filtering) |
| `console_clearLogs` | Clear the log buffer |
| `debug_getStackTrace` | Get full stack trace of latest Error/Exception |
| `debug_getPerformanceStats` | Get FPS, DrawCall, memory metrics |
| `debug_screenshot` | Capture Game/Scene view screenshot |

#### Editor Tools

##### Query

| Tool | Description |
|------|-------------|
| `editor_getHierarchy` | Get GameObject tree structure |
| `editor_getInspector` | Get Inspector serialized field values |
| `editor_getSelection` | Get currently selected object info |
| `editor_findGameObjects` | Search GameObjects by name/component |
| `editor_getProjectPath` | Get project root directory path |
| `editor_getProjectStructure` | Get Assets directory structure |

##### Mutation

| Tool | Description |
|------|-------------|
| `editor_addGameObject` | Add a GameObject |
| `editor_deleteGameObject` | Delete a GameObject |
| `editor_setField` | Modify serialized field values |
| `editor_setTransform` | Modify Transform properties |
| `editor_setActive` | Set active state |
| `editor_reparentGameObject` | Change parent node |
| `editor_addComponent` | Add a component |
| `editor_removeComponent` | Remove a component |
| `editor_setComponentEnabled` | Enable/disable a component |
| `editor_selectGameObject` | Select a specified GameObject |

##### Project

| Tool | Description |
|------|-------------|
| `menu_execute` | Execute a Unity menu item by path |
| `playmode_control` | Control PlayMode state |

##### Asset

| Tool | Description |
|------|-------------|
| `asset_deleteFolder` | Delete an Assets subdirectory |

#### Build Tools

| Tool | Description |
|------|-------------|
| `build_compile` | Trigger script compilation |
| `build_getCompileErrors` | Get compile error list |
| `build_runTests` | Run Test Runner tests |

#### Code Tools (Experimental, Unity 2022 Mono only)

| Tool | Description |
|------|-------------|
| `code_executeImmediate` | Compile and execute C# code (dual main-thread/background mode) |

> Must be manually enabled in Window → MCP Server panel. See [Tools Reference](Docs/TOOLS_EN.md) for details.

## Installation

### Git URL (Recommended)

1. Unity Editor → Window → Package Manager → `+` → Add package from git URL
2. Enter the following URL:

```
https://github.com/yangfch3/Unity-MCP-Server.git
```

Or edit your project's `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/yangfch3/Unity-MCP-Server.git"
  }
}
```

### Local Path

1. Clone this repository
2. Unity Editor → Window → Package Manager → `+` → Add package from disk
3. Select `package.json` in the repository root

Or add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "file:../../path/to/unity-mcp"
  }
}
```

## Version Update

After installing via Git URL, UPM locks the current commit hash in the host project's `packages-lock.json`. It will not auto-update afterwards.

To lock a specific version, append a Git Tag to the URL:

```
https://github.com/yangfch3/Unity-MCP-Server.git#v0.3.0
```

Corresponding `Packages/manifest.json` configuration:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/yangfch3/Unity-MCP-Server.git#v0.3.1"
  }
}
```

Without a Tag, it tracks the latest commit on the default branch:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/yangfch3/Unity-MCP-Server.git"
  }
}
```

To update, change the `#tag` suffix in `manifest.json` to the new version, or re-add the package via UPM GUI with the new Tag URL.

## Usage

### Start the Server

1. Unity Editor → Window → MCP Server
2. Set the port (default 8090), click Start
3. Copy the configuration JSON from the panel

### Configure Your Agent

Add the following to your Agent's MCP configuration file (e.g., `mcp.json`):

```json
{
  "mcpServers": {
    "unity-mcp": {
      "url": "http://localhost:8090/"
    }
  }
}
```

## Extension: Adding Custom Tools

Implement the `IMcpTool` interface in any Editor assembly. The tool will be automatically discovered and registered on server startup:

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityMcp.Editor;

public class MyCustomTool : IMcpTool
{
    public string Name => "my_custom_tool";
    public string Category => "custom";
    public string Description => "My custom tool";
    public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

    public Task<ToolResult> Execute(Dictionary<string, object> parameters)
    {
        return Task.FromResult(ToolResult.Success("Hello from custom tool!"));
    }
}
```

## Requirements

- Unity 2022.3+
- Editor only, does not affect runtime builds

## Contributing

Contributions are welcome! See [CONTRIBUTING_EN.md](CONTRIBUTING_EN.md) for details.

## License

MIT
