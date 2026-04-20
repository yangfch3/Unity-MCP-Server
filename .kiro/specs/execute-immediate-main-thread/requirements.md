# Requirements Document

## Introduction

为 `code_executeImmediate` 工具添加主线程执行模式。当前该工具在后台线程执行用户代码（带超时保护），无法调用 Unity API。新增 `mainThread` 参数后，用户可选择在主线程直接执行代码，从而访问完整的 Unity API，但不具备超时保护（存在死循环冻结 Editor 的风险）。

## Glossary

- **ExecuteImmediateTool**: 动态编译并执行 C# 代码片段的 MCP 工具，工具名为 `code_executeImmediate`
- **Background_Mode**: 后台线程执行模式，在后台线程运行用户代码，具备超时保护，不可调用 Unity API
- **MainThread_Mode**: 默认执行模式，在 Unity 主线程直接运行用户代码，可调用 Unity API，无超时保护
- **MainThreadQueue**: 将异步操作调度到 Unity 主线程执行的队列机制
- **JsonRpcDispatcher**: JSON-RPC 协议分发器，所有 `IMcpTool.Execute` 调用已通过 MainThreadQueue 调度到主线程
- **EditorPrefs_Toggle**: 通过 `EditorPrefs` 键 `McpServer_CodeExecuteImmediate` 控制工具启用/禁用的安全开关

## Requirements

### Requirement 1: mainThread 参数支持

**User Story:** 作为 AI Agent 开发者，我希望通过参数控制代码执行线程，以便在需要调用 Unity API 时选择主线程模式。

#### Acceptance Criteria

1. THE ExecuteImmediateTool SHALL 在 InputSchema 中声明一个名为 `mainThread` 的可选布尔参数，描述为指定是否在主线程执行
2. WHEN `mainThread` 参数未提供，THE ExecuteImmediateTool SHALL 使用 MainThread_Mode 执行用户代码
3. WHEN `mainThread` 参数值为 `true`，THE ExecuteImmediateTool SHALL 使用 MainThread_Mode 执行用户代码
4. WHEN `mainThread` 参数值为 `false`，THE ExecuteImmediateTool SHALL 使用 Background_Mode 执行用户代码

### Requirement 2: 后台线程执行模式（保持现有行为）

**User Story:** 作为 AI Agent 开发者，我希望在显式指定 `mainThread: false` 时仍可使用后台线程模式，以便对不确定性代码保留超时保护能力。

#### Acceptance Criteria

1. WHILE 处于 Background_Mode，THE ExecuteImmediateTool SHALL 在独立后台线程中执行编译后的用户代码
2. WHILE 处于 Background_Mode，THE ExecuteImmediateTool SHALL 在执行超过默认超时时间（5 秒）时中止线程并返回超时错误
3. WHILE 处于 Background_Mode，THE ExecuteImmediateTool SHALL 捕获 Console.WriteLine 输出并包含在响应的 `output` 字段中
4. THE ExecuteImmediateTool SHALL 在 InputSchema 中声明一个名为 `timeout` 的可选整数参数，仅在 Background_Mode 下生效，默认 5000 毫秒
5. WHEN `timeout` 参数提供时，THE ExecuteImmediateTool SHALL 使用该值作为后台模式的超时时间，并将其限制在 1000–30000 毫秒范围内

### Requirement 3: 主线程执行模式

**User Story:** 作为 AI Agent 开发者，我希望在主线程执行代码，以便调用 Unity API（如 `GameObject.Find`、`AssetDatabase` 等）。

#### Acceptance Criteria

1. WHILE 处于 MainThread_Mode，THE ExecuteImmediateTool SHALL 在当前主线程直接调用用户代码的入口方法，不创建新线程
2. WHILE 处于 MainThread_Mode，THE ExecuteImmediateTool SHALL 不施加超时限制
3. WHILE 处于 MainThread_Mode，THE ExecuteImmediateTool SHALL 捕获 Console.WriteLine 输出并包含在响应的 `output` 字段中
4. WHILE 处于 MainThread_Mode，WHEN 用户代码抛出异常，THE ExecuteImmediateTool SHALL 捕获异常并在响应的 `error` 字段中返回异常消息和堆栈信息
5. WHILE 处于 MainThread_Mode，THE ExecuteImmediateTool SHALL 在响应的 `warning` 字段中返回警告文本，提示该模式无超时保护，死循环将冻结 Editor

### Requirement 4: 安全开关兼容

**User Story:** 作为 Unity 开发者，我希望现有的 EditorPrefs 安全开关对两种模式均生效，以便统一管理工具的启用状态。

#### Acceptance Criteria

1. WHEN EditorPrefs_Toggle 为 disabled，THE ExecuteImmediateTool SHALL 拒绝执行并返回错误提示，无论 `mainThread` 参数值为何
2. WHEN EditorPrefs_Toggle 为 enabled，THE ExecuteImmediateTool SHALL 根据 `mainThread` 参数选择对应模式执行

### Requirement 5: 编译阶段共享

**User Story:** 作为开发者，我希望两种模式共享同一编译流程，以避免代码重复和行为不一致。

#### Acceptance Criteria

1. THE ExecuteImmediateTool SHALL 对两种执行模式使用相同的编译逻辑（CSharpCodeProvider）
2. THE ExecuteImmediateTool SHALL 对两种执行模式使用相同的入口点查找逻辑（public static void Run()）
3. THE ExecuteImmediateTool SHALL 对两种执行模式使用相同的 JSON 响应格式（`{success, output, error, warning}`）
