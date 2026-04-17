# 贡献指南

欢迎参与 Unity MCP Server 的开发！本文档面向协作开发者，介绍开发环境搭建、测试、编码规范等内容。

## 开发环境搭建

### 1. 克隆仓库

```bash
git clone https://github.com/<owner>/unity-mcp.git
```

### 2. 本地路径安装到宿主项目

将克隆的仓库以本地路径方式安装到你的 Unity 宿主项目中：

- Unity Editor → Window → Package Manager → `+` → Add package from disk → 选择本仓库根目录的 `package.json`

或直接编辑宿主项目的 `Packages/manifest.json`：

```json
{
  "dependencies": {
    "com.yangfch3.unity-mcp": "file:../../path/to/unity-mcp"
  }
}
```

> 将 `../../path/to/unity-mcp` 替换为本仓库相对于宿主项目的实际路径。

## 启用 Package 内置测试

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

配置完成后，打开 Unity Editor → Window → General → Test Runner，即可看到并运行本 Package 的测试用例。

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
├── Tools/          # 内置工具实现（分 debug/editor/build... 数类）
│   ├── ...
└── UI/             # Editor 界面
    └── ConfigPanel.cs
```

## 编码规范

- **命名空间**：`UnityMcp.Editor`，工具子命名空间：`UnityMcp.Editor.Tools`
- **日志前缀**：统一使用 `[McpServer]` / `[ToolRegistry]` 等模块前缀
- **工具命名**：`{category}_{action}`（如 `console_getLogs`、`menu_execute`）
- **XML 文档注释**：覆盖所有 public 成员
- **文件编码**：UTF-8 LF，无 BOM，末尾留空行

## 分支管理

- `main` 分支作为开发主线，日常开发直接在 `main` 上进行
- 发布时在 `main` 上打 Git Tag，格式：`v{major}.{minor}.{patch}`（如 `v0.1.0`）
- 使用者可通过 Git Tag 锁定特定版本安装

## Spec 收尾

完成一轮 Spec Coding 后，在 AI Agent 对话中输入「Spec 后处理」或「spec post check」，Agent 会自动检查并同步 Spec 文档、Steering 文档、README 及 CONTRIBUTING 的一致性。
