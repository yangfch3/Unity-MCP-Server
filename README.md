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
| `menu_execute` | editor | 按路径执行 Unity 菜单项 |
| `playmode_control` | editor | 进入/退出/查询 PlayMode 状态 |

## 安装

### Unity Package Manager (本地路径)

1. 克隆本仓库
2. Unity Editor → Window → Package Manager → `+` → Add package from disk
3. 选择本仓库根目录的 `package.json`

或在 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.gamedevkit.unity-mcp": "file:../../path/to/unity-mcp"
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
├── Tools/          # 内置工具实现
│   ├── ConsoleTool.cs
│   ├── MenuTool.cs
│   └── PlayModeTool.cs
└── UI/             # Editor 界面
    └── ConfigPanel.cs
```

## 要求

- Unity 2022.3+
- 仅 Editor 环境，不影响运行时构建

## License

MIT
