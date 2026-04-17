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

| Tool | Category | Description |
|------|----------|-------------|
| `console_getLogs` | debug | Get recent N log entries from Unity Console |
| `debug_getStackTrace` | debug | Get full stack trace of the latest Error/Exception |
| `debug_getPerformanceStats` | debug | Get FPS, DrawCall, memory usage and other performance metrics |
| `debug_screenshot` | debug | Capture Game/Scene view screenshot (base64 PNG) |
| `menu_execute` | editor | Execute a Unity menu item by path |
| `playmode_control` | editor | Enter/exit/query PlayMode state |
| `editor_getSelection` | editor | Get currently selected GameObject and Asset info |
| `editor_getHierarchy` | editor | Get scene GameObject tree structure (depth-limited) |
| `editor_getProjectStructure` | editor | Get Assets directory structure (depth-limited) |
| `editor_getInspector` | editor | Get serialized field values of the selected object's Inspector |
| `build_compile` | build | Trigger script compilation and return results |
| `build_getCompileErrors` | build | Get current compile error list |
| `build_runTests` | build | Run Unity Test Runner tests and return results |

## Installation

### Git URL (Recommended)

1. Unity Editor → Window → Package Manager → `+` → Add package from git URL
2. Enter the following URL:

```
https://github.com/<owner>/unity-mcp.git
```

Or edit your project's `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/<owner>/unity-mcp.git"
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
https://github.com/<owner>/unity-mcp.git#v0.1.0
```

Corresponding `Packages/manifest.json` configuration:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/<owner>/unity-mcp.git#v0.1.0"
  }
}
```

Without a Tag, it tracks the latest commit on the default branch:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/<owner>/unity-mcp.git"
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
