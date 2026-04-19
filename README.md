# Unity MCP Server

中文 | [English](README_EN.md)

Unity Editor 插件，通过 [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) 将 Unity Editor 能力暴露给外部 AI Agent。

Agent（如 Kiro、Cursor、Claude Desktop）可通过标准 MCP 协议连接到 Unity Editor，调用编辑器功能。

## 特性

- **Streamable HTTP 传输** — 基于 MCP 2025-03-26 规范，单一 HTTP 端点
- **进程内运行** — 无需外部 Node.js/Python 进程，直接在 Editor 内启动
- **可扩展工具系统** — 实现 `IMcpTool` 接口即可注册新工具，零修改核心代码
- **Domain Reload 自动恢复** — 进入/退出 PlayMode 后服务自动重启

### 内置工具

#### Debug 工具

| 工具 | 功能 | 参数 |
|------|------|------|
| `console_getLogs` | 获取 Unity Console 最近 N 条日志（支持 level/keyword 过滤、上下文模式） | `count`: int (默认 20), `level`: Error\|Warning\|Log, `keyword`: string, `beforeIndex`: int |
| `console_clearLogs` | 清空日志缓冲区 | 无 |
| `debug_getStackTrace` | 获取最近一条 Error/Exception 的完整堆栈 | 无 |
| `debug_getPerformanceStats` | 获取 FPS、DrawCall、内存占用等性能指标 | 无 |
| `debug_screenshot` | 截取 Game/Scene 视图截图（base64 PNG） | `view`: game\|scene (默认 game) |

#### Editor 工具

| 工具 | 功能 | 参数 |
|------|------|------|
| `menu_execute` | 按路径执行 Unity 菜单项 | `path`: string (必填) |
| `playmode_control` | 进入/退出/查询 PlayMode 状态 | `action`: enter\|exit\|status (必填) |
| `editor_getSelection` | 获取当前选中的 GameObject 和 Asset 信息 | 无 |
| `editor_getHierarchy` | 获取 GameObject 树结构（支持 Prefab Stage、Selection 子树，可限深度） | `maxDepth`: int (默认 -1 无限制), `root`: string (默认 ""，可选 "selection") |
| `editor_selectGameObject` | 通过路径或 instanceID 选中 Hierarchy 中的 GameObject | `path`: string, `instanceID`: int (二选一，instanceID 优先) |
| `editor_getProjectStructure` | 获取 Assets 目录结构（可限深度） | `maxDepth`: int (默认 3) |
| `editor_getInspector` | 获取选中对象的 Inspector 序列化字段值 | 无 |
| `editor_findGameObjects` | 按名称/组件类型搜索场景中的 GameObject | `namePattern`: string, `componentType`: string, `maxResults`: int (默认 50), `activeOnly`: bool (默认 true) |
| `editor_addGameObject` | 在 Prefab Stage 或 Active Scene 中添加 GameObject | `name`: string (默认 "GameObject"), `parentInstanceID`: int, `parentPath`: string |
| `editor_deleteGameObject` | 删除指定的 GameObject 及其所有子对象 | `instanceID`: int, `path`: string (二选一) |
| `editor_addComponent` | 给指定 GameObject 添加组件 | `instanceID`/`path`, `componentType`: string (必填) |
| `editor_removeComponent` | 移除指定 GameObject 上的组件 | `instanceID`/`path`, `componentType`: string (必填) |
| `editor_reparentGameObject` | 修改 GameObject 的父节点 | `instanceID`/`path`, `newParentInstanceID`: int, `newParentPath`: string, `worldPositionStays`: bool (默认 true) |
| `editor_setActive` | 修改 GameObject 的激活状态 | `instanceID`/`path`, `active`: bool (必填) |
| `editor_setComponentEnabled` | 修改组件的启用/禁用状态 | `instanceID`/`path`, `componentType`: string, `enabled`: bool (必填) |
| `editor_setTransform` | 修改 Transform / RectTransform 属性 | `instanceID`/`path`, `localPosition`: [x,y,z], `localRotation`: [x,y,z], `localScale`: [x,y,z], `anchoredPosition`: [x,y], `sizeDelta`: [w,h], `pivot`: [x,y], `anchorMin`: [x,y], `anchorMax`: [x,y] |
| `editor_setField` | 修改组件的序列化字段值 | `instanceID`/`path`, `componentType`: string, `fieldName`: string, `value`: any (必填) |
| `asset_deleteFolder` | 删除指定 Assets 子目录并刷新 AssetDatabase | `path`: string (必填) |

#### Build 工具

| 工具 | 功能 | 参数 |
|------|------|------|
| `build_compile` | 触发脚本编译并返回结果 | 无 |
| `build_getCompileErrors` | 获取当前编译错误列表 | 无 |
| `build_runTests` | 运行 Unity Test Runner 测试并返回结果 | `mode`: EditMode\|PlayMode (默认 EditMode), `testFilter`: string |

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
https://github.com/yangfch3/Unity-MCP-Server.git#v0.1.0
```

对应 `Packages/manifest.json` 配置：

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "https://github.com/yangfch3/Unity-MCP-Server.git#v0.1.0"
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
