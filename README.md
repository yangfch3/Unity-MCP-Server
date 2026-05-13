# Unity MCP Server

中文 | [English](README_EN.md)

Unity Editor 插件，通过 [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) 将 Unity Editor 能力暴露给外部 AI Agent。

Agent（如 Kiro、Cursor、Claude Code）可通过标准 MCP 协议连接到 Unity Editor，调用编辑器功能。

## 理念

本插件定位为 AI Agent 的 **Unity 感知层 + 诊断工具链**：

- **感知优先** — 优先覆盖"读取场景状态、获取上下文、辅助诊断"等高频场景，让 Agent 能充分理解当前工程状态并辅助开发决策。
- **结构化写入** — 提供安全、可审计的写操作能力（如修改属性、增删节点），但不追求覆盖 Unity Editor GUI 的所有交互。
- **不替代编辑器** — 目标是增强工程师与 Agent 协作的效率，而非将 Editor 的全部操作搬到对话框里。

## 特性

- **Streamable HTTP 传输** — 基于 MCP 2025-03-26 规范，单一 HTTP 端点
- **进程内运行** — 无需外部 Node.js/Python 进程，直接在 Editor 内启动
- **可扩展工具系统** — 实现 `IMcpTool` 接口即可注册新工具，零修改核心代码
- **Domain Reload 自动恢复** — 进入/退出 PlayMode 后服务自动重启

### 内置工具

完整的参数说明和使用示例见 [工具详细文档](Docs/TOOLS.md)。

#### Debug 工具

| 工具 | 功能 |
|------|------|
| `console_getLogs` | 获取 Unity Console 日志（支持过滤） |
| `console_clearLogs` | 清空日志缓冲区 |
| `debug_getStackTrace` | 获取最近 Error/Exception 的完整堆栈 |
| `debug_getPerformanceStats` | 获取 FPS、DrawCall、内存等性能指标 |
| `debug_screenshot` | 截取 Game/Scene 视图截图 |

#### Editor 工具

##### Query（查询）

| 工具 | 功能 |
|------|------|
| `editor_getHierarchy` | 获取 GameObject 树结构 |
| `editor_getInspector` | 获取 Inspector 序列化字段值 |
| `editor_getSelection` | 获取当前选中对象信息 |
| `editor_findGameObjects` | 按名称/组件搜索 GameObject |
| `editor_getProjectPath` | 获取项目根目录路径 |
| `editor_getProjectStructure` | 获取 Assets 目录结构 |

##### Mutation（修改）

| 工具 | 功能 |
|------|------|
| `editor_addGameObject` | 添加 GameObject |
| `editor_deleteGameObject` | 删除 GameObject |
| `editor_setField` | 修改序列化字段值 |
| `editor_setTransform` | 修改 Transform 属性 |
| `editor_setActive` | 修改激活状态 |
| `editor_reparentGameObject` | 修改父节点 |
| `editor_addComponent` | 添加组件 |
| `editor_removeComponent` | 移除组件 |
| `editor_setComponentEnabled` | 启用/禁用组件 |
| `editor_selectGameObject` | 选中指定 GameObject |

##### Project（项目）

| 工具 | 功能 |
|------|------|
| `menu_execute` | 按路径执行 Unity 菜单项 |
| `playmode_control` | 控制 PlayMode 状态 |

##### Asset（资产）

| 工具 | 功能 |
|------|------|
| `asset_deleteFolder` | 删除 Assets 子目录 |

#### Build 工具

| 工具 | 功能 |
|------|------|
| `build_compile` | 触发脚本编译 |
| `build_getCompileErrors` | 获取编译错误列表 |
| `build_runTests` | 运行 Test Runner 测试 |

#### Code 工具（实验性，仅 Unity 2022 Mono）

| 工具 | 功能 |
|------|------|
| `code_executeImmediate` | 动态编译并执行 C# 代码（支持主线程/后台双模式） |

> 需在 Window → MCP Server 面板手动开启。详见 [工具详细文档](Docs/TOOLS.md)。

## 安装

### Git URL 安装（推荐）

1. Unity Editor → Window → Package Manager → `+` → Add package from git URL
2. 输入以下 URL：

```
https://github.com/yangfch3/Unity-MCP-Server.git
```

或直接编辑宿主项目的 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/yangfch3/Unity-MCP-Server.git"
  }
}
```

### 本地路径安装

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

## 版本更新

UPM 通过 Git URL 安装后，会在宿主项目的 `packages-lock.json` 中锁定当前 commit hash。后续不会自动更新。

如需锁定特定版本，可在 URL 末尾追加 Git Tag：

```
https://github.com/yangfch3/Unity-MCP-Server.git#v0.3.0
```

对应 `Packages/manifest.json` 配置：

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/yangfch3/Unity-MCP-Server.git#v0.3.1"
  }
}
```

不带 Tag 则跟踪默认分支最新 commit：

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/yangfch3/Unity-MCP-Server.git"
  }
}
```

更新版本时，修改 `manifest.json` 中的 `#tag` 后缀为新版本号，或在 UPM GUI 重新 Add package from git URL 输入新 Tag 的 URL 即可。

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

## 要求

- Unity 2022.3+
- 仅 Editor 环境，不影响运行时构建

## 参与贡献

欢迎参与本项目的开发，详见 [CONTRIBUTING.md](CONTRIBUTING.md)。

## License

MIT
