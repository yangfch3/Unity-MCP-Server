# Implementation Plan: Unity MCP Minimum Experiment

## Overview

按依赖顺序构建 Unity Editor 内的 MCP 服务插件。先搭建 Package 骨架和核心接口，再逐层实现工具注册、协议分发、HTTP 服务、内置工具，最后集成联调。所有代码位于 Editor 程序集，使用 C#。

## Tasks

- [x] 1. 搭建 Unity Package 骨架与核心接口
  - [x] 1.1 创建 Package 目录结构与 package.json
    - 创建 `package.json`（name、version、displayName、description、unity 最低版本）
    - 创建 `Editor/` 目录及 Assembly Definition（Editor only）
    - 创建 `Tests/Editor/` 目录及测试 Assembly Definition
    - _Requirements: 5.1, 5.2, 5.4_

  - [x] 1.2 定义 IMcpTool 接口与 ToolResult 数据模型
    - 定义 `IMcpTool` 接口：Name、Category、Description、InputSchema、Execute
    - 定义 `ToolResult` 类（成功/错误两种状态，content 列表）
    - _Requirements: 6.2_

- [x] 2. 实现 ToolRegistry 工具注册中心
  - [x] 2.1 实现 ToolRegistry 核心逻辑
    - 实现 Register、Resolve、ListAll、ListByCategory 方法
    - 实现 AutoDiscover 反射扫描机制（扫描所有 IMcpTool 非抽象实现类）
    - 处理重复注册（忽略或覆盖，需明确策略）
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [ ]* 2.2 编写 ToolRegistry 属性测试
    - **Property 4: ToolRegistry 注册完整性与分组正确性**
    - 使用 FsCheck 生成随机 IMcpTool 集合，验证 ListAll 数量一致、字段一致、ListByCategory 分组正确
    - **Validates: Requirements 6.1, 6.3, 6.4**

  - [ ]* 2.3 编写 ToolRegistry 单元测试
    - 测试注册、查找、按分类列出、重复注册处理
    - _Requirements: 6.1, 6.3, 6.4_

- [x] 3. 实现 MainThreadQueue 主线程调度
  - [x] 3.1 实现 MainThreadQueue
    - 实现线程安全的 Enqueue 方法
    - 通过 `EditorApplication.update` 回调在主线程逐帧消费队列
    - 支持超时机制（10s）
    - _Requirements: 5.1_

  - [ ]* 3.2 编写 MainThreadQueue 单元测试
    - 测试入队/出队、主线程执行验证
    - _Requirements: 5.1_

- [x] 4. Checkpoint — 核心基础设施验证
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. 实现 JsonRpcDispatcher 协议分发
  - [x] 5.1 实现 JsonRpcDispatcher
    - 解析 JSON-RPC 2.0 请求（jsonrpc、id、method、params）
    - 路由 `initialize` → 返回 server capabilities（tools capability）
    - 路由 `tools/list` → 调用 ToolRegistry.ListAll()
    - 路由 `tools/call` → 调用 ToolRegistry.Resolve + MainThreadQueue 调度执行
    - 未知 method 返回 -32601，无效 JSON 返回 -32700，工具名不存在返回 -32602
    - _Requirements: 1.2, 6.3_

  - [ ]* 5.2 编写 JsonRpcDispatcher 单元测试
    - 测试合法请求路由、非法 JSON 解析、未知 method 错误码、工具名不存在错误
    - _Requirements: 1.2_

- [x] 6. 实现 McpServer HTTP 服务
  - [x] 6.1 实现 McpServer 与 HttpListener
    - Start(port)：后台线程启动 HttpListener 监听 `http://localhost:{port}/`
    - Stop()：停止监听、释放线程和端口资源
    - HandleRequest：读取 HTTP 请求体，交给 JsonRpcDispatcher 处理，写回响应
    - POST 请求处理（JSON-RPC 消息）、GET 请求占位（返回 405 或空 SSE）
    - 设置正确的 Content-Type 和 CORS 头
    - 端口冲突捕获 HttpListenerException 并设置错误状态
    - _Requirements: 1.2, 1.3, 1.5, 5.3_

  - [ ]* 6.2 编写 McpServer 单元测试
    - 测试启动/停止生命周期、端口冲突处理
    - _Requirements: 1.2, 1.3, 1.5, 5.3_

- [x] 7. Checkpoint — 服务链路验证
  - Ensure all tests pass, ask the user if questions arise.

- [x] 8. 实现三个内置工具
  - [x] 8.1 实现 ConsoleTool (console_getLogs)
    - 通过 `Application.logMessageReceived` 捕获日志到内存缓冲区
    - Execute：按请求条数 N 返回最近 min(N, 总数) 条日志
    - 每条日志包含 level（Log/Warning/Error）、timestamp、message
    - 空日志时返回空列表
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [ ]* 8.2 编写 ConsoleTool 属性测试
    - **Property 1: Console 日志检索正确性**
    - 使用 FsCheck 生成随机日志缓冲区和请求条数，验证返回条数、字段完整性、顺序一致性
    - **Validates: Requirements 2.1, 2.2, 2.3, 2.4**

  - [ ]* 8.3 编写 ConsoleTool 单元测试
    - 测试正常取日志、空日志、请求数超出可用数
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 8.4 实现 MenuTool (menu_execute)
    - Execute：调用 `EditorApplication.ExecuteMenuItem(path)`
    - 返回 false 时返回"菜单路径不存在"错误信息
    - 成功时返回确认信息
    - _Requirements: 3.1, 3.2, 3.3_

  - [ ]* 8.5 编写 MenuTool 属性测试
    - **Property 2: 无效菜单路径返回错误**
    - 使用 FsCheck 生成随机非法菜单路径字符串，验证均返回错误响应
    - **Validates: Requirements 3.2**

  - [ ]* 8.6 编写 MenuTool 单元测试
    - 测试合法路径执行、非法路径错误响应
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 8.7 实现 PlayModeTool (playmode_control)
    - Execute：根据 action 参数（enter/exit/status）操作 `EditorApplication.isPlaying`
    - 重复进入/退出时返回提示信息而非执行操作
    - status 查询返回当前 PlayMode 状态
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ]* 8.8 编写 PlayModeTool 属性测试
    - **Property 3: PlayMode 状态查询一致性**
    - 验证 status 查询返回值与 EditorApplication.isPlaying 一致
    - **Validates: Requirements 4.3**

  - [ ]* 8.9 编写 PlayModeTool 单元测试
    - 测试进入/退出/查询、重复进入/退出的提示信息
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

- [x] 9. Checkpoint — 内置工具验证
  - Ensure all tests pass, ask the user if questions arise.

- [x] 10. 实现 ConfigPanel 与服务集成
  - [x] 10.1 实现 ConfigPanel EditorWindow（最简版）
    - 菜单入口 `Window/MCP Server` 打开 EditorWindow
    - 端口输入框（默认 8090），启动/停止按钮
    - 服务状态显示（Running/Stopped）、已连接 Agent 数量、错误信息
    - 配置持久化到 EditorPrefs（key 前缀 `McpServer_`）
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [x] 10.2 集成 ToolRegistry 自动发现与 McpServer 启动流程
    - ConfigPanel 启动时触发 ToolRegistry.AutoDiscover()
    - 将 ToolRegistry 注入 McpServer/JsonRpcDispatcher
    - 确保 Domain Reload 后服务自动停止、状态重置
    - _Requirements: 5.3, 6.2, 6.5_

- [x] 11. 端到端集成验证
  - [ ]* 11.1 编写集成测试
    - 测试完整流程：启动服务 → HTTP POST initialize → tools/list → tools/call → 停止服务
    - 验证端口释放、无残留线程
    - _Requirements: 1.2, 1.3, 5.3_

- [x] 12. Final checkpoint — 全量验证
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- 标记 `*` 的子任务为可选，可跳过以加速 MVP 交付
- 属性测试使用 FsCheck；若 Unity 集成遇阻，可在独立 .NET 项目中运行或用手写随机生成器替代
- 所有代码位于 Editor 程序集，不影响运行时构建
- ConfigPanel 为最简版，完整 UI 打磨可后续迭代
