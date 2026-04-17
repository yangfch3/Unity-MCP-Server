# Unity MCP Server

Unity Editor 插件，通过 [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) 将 Unity Editor 能力暴露给外部 AI Agent。

Agent（如 Kiro、Cursor、Claude Desktop）可通过标准 MCP 协议连接到 Unity Editor，调用编辑器功能。

## 特性

- **Streamable HTTP 传输** — 基于 MCP 2025-03-26 规范，单一 HTTP 端点
- **进程内运行** — 无需外部 Node.js/Python 进程，直接在 Editor 内启动
- **可扩展工具系统** — 实现 `IMcpTool` 接口即可注册新工具，零修改核心代码
- **Domain Reload 自动恢复** — 进入/退出 PlayMode 后服务自动重启

### 内置工具

| 工具 | 分类 | 功能 |
|------|------|------|
| `console_getLogs` | debug | 获取 Unity Console 最近 N 条日志 |
| `debug_getStackTrace` | debug | 获取最近一条 Error/Exception 的完整堆栈 |
| `debug_getPerformanceStats` | debug | 获取 FPS、DrawCall、内存占用等性能指标 |
| `debug_screenshot` | debug | 截取 Game/Scene 视图截图（base64 PNG） |
| `menu_execute` | editor | 按路径执行 Unity 菜单项 |
| `playmode_control` | editor | 进入/退出/查询 PlayMode 状态 |
| `editor_getSelection` | editor | 获取当前选中的 GameObject 和 Asset 信息 |
| `editor_getHierarchy` | editor | 获取场景 GameObject 树结构（可限深度） |
| `editor_getProjectStructure` | editor | 获取 Assets 目录结构（可限深度） |
| `editor_getInspector` | editor | 获取选中对象的 Inspector 序列化字段值 |
| `build_compile` | build | 触发脚本编译并返回结果 |
| `build_getCompileErrors` | build | 获取当前编译错误列表 |
| `build_runTests` | build | 运行 Unity Test Runner 测试并返回结果 |

## 安装

### Unity Package Manager (本地路径)

1. 克隆本仓库
2. Unity Editor → Window → Package Manager → `+` → Add package from disk
3. 选择本仓库根目录的 `package.json`

或在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "file:../../path/to/unity-mcp"
  }
}
```

## 使用

### 启动服务

1. Unity Editor → Window → MCP Server
2. 设置端口（默认 8090），点击 Start
3. 复制面板中的配置 JSON

### 配置 Agent

将以下内容添加到 Agent 的 MCP 配置文件（如 `mcp.json`）：

```json
{
  "mcpServers": {
    "unity-mcp": {
      "url": "http://localhost:8090/"
    }
  }
}
```

## 扩展：添加自定义工具

实现 `IMcpTool` 接口，放在任意 Editor 程序集中，服务启动时会自动发现并注册：

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityMcp.Editor;

public class MyCustomTool : IMcpTool
{
    public string Name => "my_custom_tool";
    public string Category => "custom";
    public string Description => "我的自定义工具";
    public string InputSchema => "{\"type\":\"object\",\"properties\":{}}";

    public Task<ToolResult> Execute(Dictionary<string, object> parameters)
    {
        return Task.FromResult(ToolResult.Success("Hello from custom tool!"));
    }
}
```

## 项目结构

```
Editor/
├── Core/           # 核心接口与数据模型
│   ├── IMcpTool.cs         # 工具统一接口
│   ├── ToolResult.cs       # 执行结果模型
│   └── ToolRegistry.cs     # 工具注册中心（反射自动发现）
├── Protocol/       # MCP 协议层
│   ├── JsonRpcDispatcher.cs  # JSON-RPC 2.0 分发器
│   └── MiniJson.cs           # 轻量 JSON 解析器
├── Server/         # HTTP 服务与生命周期
│   ├── McpServer.cs          # HttpListener 服务端
│   ├── McpServerManager.cs   # 生命周期管理（静态单例）
│   └── MainThreadQueue.cs    # 主线程调度队列
├── Tools/          # 内置工具实现（13 个，分 debug/editor/build 三类）
│   ├── ConsoleTool.cs
│   ├── StackTraceTool.cs
│   ├── PerformanceTool.cs
│   ├── ScreenshotTool.cs
│   ├── MenuTool.cs
│   ├── PlayModeTool.cs
│   ├── SelectionTool.cs
│   ├── HierarchyTool.cs
│   ├── ProjectStructureTool.cs
│   ├── InspectorTool.cs
│   ├── CompileTool.cs
│   ├── CompileErrorsTool.cs
│   └── TestRunnerTool.cs
└── UI/             # Editor 界面
    └── ConfigPanel.cs
```

## 要求

- Unity 2022.3+
- 仅 Editor 环境，不影响运行时构建

## 协作开发

### 启用 Package 内置测试

本 Package 包含 EditMode 单元测试（位于 `Tests/Editor/`）。要在宿主项目的 Test Runner 中运行这些测试，需在宿主项目的 `Packages/manifest.json` 中添加 `testables`：

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

保存后 Unity 会自动 reimport，打开 Window → General → Test Runner 即可看到并运行本 Package 的测试。

## License

MIT
