# Contributing Guide

Welcome to Unity MCP Server development! This document is for contributors, covering development setup, testing, coding standards, and more.

## Development Setup

### Core Members

1. Clone the main repository:

```bash
git clone https://github.com/yangfch3/Unity-MCP-Server.git
```

2. Create a feature branch from `main`, develop on it, then open a Pull Request to merge into `main`.

### External Contributors

1. Fork this repository on GitHub to your own account
2. Clone your fork:

```bash
git clone https://github.com/<your-username>/Unity-MCP-Server.git
```

3. Add the upstream remote for syncing:

```bash
git remote add upstream https://github.com/yangfch3/Unity-MCP-Server.git
```

4. Create a feature branch from the latest `main`, develop on it, then open a Pull Request to the upstream repository.

### Install to Host Project via Local Path

Install the cloned repository into your Unity host project as a local package:

- Unity Editor → Window → Package Manager → `+` → Add package from disk → Select `package.json` in the repository root

Or edit your host project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "file:../../path/to/unity-mcp"
  }
}
```

> Replace `../../path/to/unity-mcp` with the actual relative path from your host project to this repository.

## Enabling Built-in Tests

This package includes EditMode unit tests (located in `Tests/Editor/`). To run them in your host project's Test Runner, add `testables` to your host project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "file:../../path/to/unity-mcp"
  },
  "testables": [
    "com.yangfch3.unity-mcp"
  ]
}
```

After saving, open Unity Editor → Window → General → Test Runner to see and run the package's test cases.

## Project Structure

```
Editor/
├── Core/           # Core interfaces and data models
│   ├── IMcpTool.cs         # Unified tool interface
│   ├── ToolResult.cs       # Execution result model
│   └── ToolRegistry.cs     # Tool registry (auto-discovery via reflection)
├── Protocol/       # MCP protocol layer
│   ├── JsonRpcDispatcher.cs  # JSON-RPC 2.0 dispatcher
│   └── MiniJson.cs           # Lightweight JSON parser
├── Server/         # HTTP server and lifecycle
│   ├── McpServer.cs          # HttpListener server
│   ├── McpServerManager.cs   # Lifecycle management (static singleton)
│   └── MainThreadQueue.cs    # Main thread dispatch queue
├── Tools/          # Built-in tool implementations (debug/editor/build categories)
│   ├── ...
└── UI/             # Editor UI
    └── ConfigPanel.cs
```

## Coding Standards

- **Namespaces**: `UnityMcp.Editor`, tool sub-namespace: `UnityMcp.Editor.Tools`
- **Log Prefix**: Use module prefixes like `[McpServer]` / `[ToolRegistry]`
- **Tool Naming**: `{category}_{action}` (e.g., `console_getLogs`, `menu_execute`)
- **XML Doc Comments**: Required for all public members
- **File Encoding**: UTF-8 LF, no BOM, trailing newline

## Testing Requirements

All new or modified features must include corresponding tests. PRs without tests will not be merged.

### Basic Rules

- Framework: NUnit (Unity Test Runner EditMode)
- Test files go in `Tests/Editor/`, naming: `{ToolName}Tests.cs`
- Test namespace: `UnityMcp.Editor.Tests`
- Each new Tool must include at least:
  - Name / Category property correctness
  - Parameter validation (missing, invalid) error returns
  - ToolRegistry auto-discovery
  - Core functionality positive cases
- When modifying existing Tools, add tests covering the changed behavior
- Shared test helpers go in `Tests/Editor/` helper files to avoid duplication

### Property Tests (Optional but Recommended)

For security validation, filtering logic, or combinatorial input scenarios, property tests are recommended:
- Use `[Category("Slow")]` attribute; CI can skip via `--where "cat != Slow"`
- Minimum 100 random iterations
- Annotate with corresponding Property and Requirement in comments

### Pre-Submit Checklist

- Ensure all tests pass: Unity Editor → Window → General → Test Runner → Run All
- After adding a new Tool, update assertions in `Tests/Editor/ToolRegistryTests.cs`

## Branch Management

- `main` branch is the development mainline; daily development happens directly on `main`
- Releases are tagged on `main` with Git Tags in the format: `v{major}.{minor}.{patch}` (e.g., `v0.1.0`)
- Users can lock specific versions via Git Tags when installing

## Spec Post-Check

After completing a round of Spec Coding, type "Spec 后处理" or "spec post check" in the AI Agent chat. The agent will automatically check and sync Spec documents, Steering documents, README, and CONTRIBUTING for consistency.
