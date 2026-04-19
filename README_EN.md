# Unity MCP Server

[中文](README.md) | English

Unity Editor plugin that exposes Unity Editor capabilities to external AI Agents via [MCP (Model Context Protocol)](https://modelcontextprotocol.io/).

Agents (e.g., Kiro, Cursor, Claude Desktop) can connect to Unity Editor through the standard MCP protocol and invoke editor functions.

## Features

- **Streamable HTTP Transport** — Based on MCP 2025-03-26 spec, single HTTP endpoint
- **In-Process** — No external Node.js/Python process required, runs directly inside the Editor
- **Extensible Tool System** — Implement the `IMcpTool` interface to register new tools with zero core code changes
- **Domain Reload Auto-Recovery** — Service automatically restarts after entering/exiting PlayMode

### Built-in Tools

#### Debug Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| `console_getLogs` | Get recent N log entries from Unity Console (supports level/keyword filtering, context mode) | `count`: int (default 20), `level`: Error\|Warning\|Log, `keyword`: string, `beforeIndex`: int |
| `console_clearLogs` | Clear the log buffer | None |
| `debug_getStackTrace` | Get full stack trace of the latest Error/Exception | None |
| `debug_getPerformanceStats` | Get FPS, DrawCall, memory usage and other performance metrics | None |
| `debug_screenshot` | Capture Game/Scene view screenshot (base64 PNG) | `view`: game\|scene (default game) |

#### Editor Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| `menu_execute` | Execute a Unity menu item by path | `path`: string (required) |
| `playmode_control` | Enter/exit/query PlayMode state | `action`: enter\|exit\|status (required) |
| `editor_getSelection` | Get currently selected GameObject and Asset info | None |
| `editor_getHierarchy` | Get GameObject tree structure (supports Prefab Stage, Selection subtree, depth-limited) | `maxDepth`: int (default -1 unlimited), `root`: string (default "", optional "selection") |
| `editor_selectGameObject` | Select a GameObject in the Hierarchy by path or instanceID | `path`: string, `instanceID`: int (either one, instanceID takes priority) |
| `editor_getProjectStructure` | Get Assets directory structure (depth-limited) | `maxDepth`: int (default 3) |
| `editor_getInspector` | Get serialized field values of the selected object's Inspector | None |
| `editor_findGameObjects` | Search GameObjects in scene by name/component type | `namePattern`: string, `componentType`: string, `maxResults`: int (default 50), `activeOnly`: bool (default true) |
| `editor_addGameObject` | Add a GameObject to Prefab Stage or Active Scene | `name`: string (default "GameObject"), `parentInstanceID`: int, `parentPath`: string |
| `editor_deleteGameObject` | Delete a GameObject and all its children | `instanceID`: int, `path`: string (either one) |
| `editor_addComponent` | Add a component to a specified GameObject | `instanceID`/`path`, `componentType`: string (required) |
| `editor_removeComponent` | Remove a component from a specified GameObject | `instanceID`/`path`, `componentType`: string (required) |
| `editor_reparentGameObject` | Change a GameObject's parent | `instanceID`/`path`, `newParentInstanceID`: int, `newParentPath`: string, `worldPositionStays`: bool (default true) |
| `editor_setActive` | Set a GameObject's active state | `instanceID`/`path`, `active`: bool (required) |
| `editor_setComponentEnabled` | Enable/disable a component | `instanceID`/`path`, `componentType`: string, `enabled`: bool (required) |
| `editor_setTransform` | Modify Transform / RectTransform properties | `instanceID`/`path`, `localPosition`: [x,y,z], `localRotation`: [x,y,z], `localScale`: [x,y,z], `anchoredPosition`: [x,y], `sizeDelta`: [w,h], `pivot`: [x,y], `anchorMin`: [x,y], `anchorMax`: [x,y] |
| `editor_setField` | Modify a component's serialized field value | `instanceID`/`path`, `componentType`: string, `fieldName`: string, `value`: any (required) |
| `asset_deleteFolder` | Delete a specified Assets subdirectory and refresh AssetDatabase | `path`: string (required) |

#### Build Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| `build_compile` | Trigger script compilation and return results | None |
| `build_getCompileErrors` | Get current compile error list | None |
| `build_runTests` | Run Unity Test Runner tests and return results | `mode`: EditMode\|PlayMode (default EditMode), `testFilter`: string |

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
https://github.com/yangfch3/Unity-MCP-Server.git#v0.1.0
```

Corresponding `Packages/manifest.json` configuration:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/yangfch3/Unity-MCP-Server.git#v0.1.0"
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
