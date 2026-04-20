# 设计文档：execute-immediate-main-thread

## 概述

为 `ExecuteImmediateTool` 添加双模式执行能力。当前实现仅支持后台线程执行（带超时保护），新增主线程直接执行模式作为默认行为，使 AI Agent 可调用完整 Unity API。

核心变更：
- 新增 `mainThread` 可选布尔参数（默认 `true`）
- 主线程模式：直接在当前线程调用入口方法，无超时保护，可访问 Unity API
- 后台模式（`mainThread: false`）：保持现有 `RunWithTimeout` 行为不变

## 架构

### 执行流程

```mermaid
flowchart TD
    A[Execute 入口] --> B{EditorPrefs 开关?}
    B -->|disabled| C[返回错误: 工具已禁用]
    B -->|enabled| D[编译代码 CompileCode]
    D --> E{编译成功?}
    E -->|No| F[返回编译错误]
    E -->|Yes| G[查找入口点 FindEntryPoint]
    G --> H{找到 Run()?}
    H -->|No| I[返回: 无入口点]
    H -->|Yes| J{mainThread 参数?}
    J -->|true / 未提供| K[RunOnMainThread]
    J -->|false| L[RunWithTimeout 现有逻辑]
    K --> M[构建响应 + 警告]
    L --> N[构建响应]
```

### 设计决策

1. **默认主线程**：由于 `JsonRpcDispatcher` 已通过 `MainThreadQueue` 将所有 `IMcpTool.Execute` 调度到主线程，主线程模式只需直接调用 `method.Invoke`，无需额外线程调度。

2. **保留 `_executionLock`**：两种模式共享同一把锁，防止并发编译/执行冲突。

3. **警告独立字段**：主线程模式在响应 JSON 中通过独立的 `warning` 字段返回警告文本，不污染 `output` 字段的数据语义。Agent 可程序化区分用户代码输出和系统警告。

4. **错误返回策略**：区分两类错误——"工具不可用"（EditorPrefs 开关关闭）走 `ToolResult.Error`，由 MCP 协议层处理；"用户代码/输入层面的错误"（编译失败、无入口点、运行时异常等）走 `ToolResult.Success(BuildResponse(...))`，返回结构化 JSON 让 Agent 自行解析处理。

5. **警告语义**：warning 是事前告知——让 Agent 在决策时知悉主线程模式的冻结风险，而非运行时保护。若用户代码真正死循环，Execute 永远不会返回，Agent 也收不到任何响应。

## 组件与接口

### InputSchema 变更

```
现有: { code: string (required) }
新增: { code: string (required), mainThread: boolean (optional, default true), timeout: integer (optional, default 5000) }
```

参数描述：
- `mainThread`: `"指定是否在主线程执行。true（默认）可调用 Unity API 但无超时保护；false 在后台线程执行，有超时保护但不可调用 Unity API"`
- `timeout`: `"后台模式超时时间（毫秒），仅 mainThread:false 时生效，默认 5000"`

### 新增方法：RunOnMainThread

伪代码：

```
RunOnMainThread(method):
    保存 originalOut = Console.Out
    创建 StringWriter writer
    try:
        Console.SetOut(writer)
        method.Invoke(null, null)
        output = writer.ToString()
        return (output, "")
    catch TargetInvocationException ex:
        output = writer.ToString()
        return (output, ex.InnerException.Message + "\n" + ex.InnerException.StackTrace)
    catch Exception ex:
        output = writer.ToString()
        return (output, ex.Message + "\n" + ex.StackTrace)
    finally:
        Console.SetOut(originalOut)
```

### Execute 方法变更

伪代码（仅展示分支逻辑变更）：

```
Execute(parameters):
    ... 现有的参数提取、开关检查、编译、入口点查找 ...

    // 解析 mainThread 参数
    useMainThread = true  // 默认值
    if parameters 包含 "mainThread":
        useMainThread = (bool)parameters["mainThread"]

    // 分支执行
    if useMainThread:
        (output, error) = RunOnMainThread(entryPoint)
        warning = "[WARNING: mainThread mode] No timeout protection. Infinite loops will freeze the Editor. Use mainThread:false for timeout safety."
    else:
        timeoutMs = DefaultTimeoutMs  // 5000
        if parameters 包含 "timeout":
            timeoutMs = clamp(parameters["timeout"], 1000, 30000)
        (output, error) = RunWithTimeout(entryPoint, timeoutMs)
        warning = ""

    return BuildResponse(output, error, warning)
```

## 数据模型

### 请求参数

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| code | string | ✅ | - | 要编译执行的 C# 源代码 |
| mainThread | boolean | ❌ | true | true=主线程执行（可调 Unity API），false=后台线程（有超时保护） |
| timeout | integer | ❌ | 5000 | 后台模式超时时间（毫秒），仅 mainThread:false 时生效，范围 1000–30000 |

### 响应格式（两种模式统一）

```json
{
  "success": true/false,
  "output": "Console 输出（纯用户代码输出，不含系统警告）",
  "error": "异常消息和堆栈（仅失败时非空）",
  "warning": "系统警告文本（仅主线程模式时非空）"
}
```

## 正确性属性

*属性（Property）是在系统所有合法执行中都应成立的特征或行为——本质上是对系统应做什么的形式化陈述。属性是人类可读规格说明与机器可验证正确性保证之间的桥梁。*

### Property 1: Console 输出捕获（Round-Trip）

*For any* 执行模式（主线程或后台）和任意非空字符串 s，如果用户代码执行 `Console.WriteLine(s)`，则响应的 `output` 字段必须包含 s。

**Validates: Requirements 2.3, 3.3**

### Property 2: 异常消息保留

*For any* 在 MainThread_Mode 下执行的代码，如果用户代码抛出包含消息 m 的异常，则响应的 `error` 字段必须包含 m。

**Validates: Requirements 3.4**

### Property 3: 安全开关普适性

*For any* `mainThread` 参数值（true、false、或未提供），当 EditorPrefs 开关为 disabled 时，ExecuteImmediateTool 必须拒绝执行并返回错误。

**Validates: Requirements 4.1**

### Property 4: 响应格式一致性

*For any* 可成功编译并包含 `Run()` 入口点的代码，无论使用哪种执行模式，响应 JSON 都必须包含 `success`、`output`、`error`、`warning` 四个键。

**Validates: Requirements 5.3**

### Property 5: 主线程模式警告存在性

*For any* 在 MainThread_Mode 下成功执行的代码，响应的 `warning` 字段必须非空且包含冻结风险提示文本。

**Validates: Requirement 3.5**

### Property 6: 主线程模式无超时限制

*For any* 在 MainThread_Mode 下执行的代码，即使执行耗时超过 Background_Mode 的默认超时阈值（5s），仍应正常返回结果而非超时错误。

**Validates: Requirement 3.2**

## 已知局限

1. **Console.SetOut 副作用**：两种模式执行期间均通过 `Console.SetOut` 重定向输出。在主线程模式下，若执行期间有其他 Editor 回调（如 `EditorApplication.update`）写入 Console，其输出会被错误截获到 `output` 中。后台模式同样存在此问题但时间窗口更短。

2. **死循环不可恢复**：主线程模式下若用户代码死循环，Unity Editor 将冻结，只能通过外部手段（任务管理器）终止进程。warning 字段仅作事前告知。

## 错误处理

| 场景 | 处理方式 |
|------|----------|
| `code` 参数缺失/空 | 返回 `ToolResult.Success(BuildResponse("", "missing 'code' parameter", ""))` |
| EditorPrefs 开关关闭 | 返回 `ToolResult.Error(...)` 提示启用（工具不可用级别） |
| 编译失败 | 返回 `ToolResult.Success(BuildResponse("", 编译错误, ""))`（含行号列号） |
| 无 `Run()` 入口点 | 返回 `ToolResult.Success(BuildResponse("", 提示信息, ""))` |
| 主线程模式运行时异常 | 捕获异常，返回 `{success:false, output:部分输出, error:消息+堆栈}` |
| 后台模式超时 | 中止线程，返回超时错误（现有行为） |
| 后台模式运行时异常 | 捕获异常，返回错误（现有行为） |

## 测试策略

### 单元测试（必需）

基于现有 `ExecuteImmediateToolTests.cs` 扩展：

- **InputSchema 验证**：验证 `mainThread` 字段存在、类型为 boolean、不在 required 中
- **默认模式**：不传 `mainThread` 时使用主线程模式（验证无超时、可执行 Unity API 相关代码）
- **显式 true**：`mainThread: true` 行为与默认一致
- **显式 false**：`mainThread: false` 保持后台线程行为（超时保护生效）
- **主线程异常捕获**：主线程模式下抛出异常，验证 error 字段包含消息和堆栈
- **主线程警告文本**：主线程模式执行后，warning 字段包含警告提示
- **安全开关**：开关关闭时，两种模式均被拒绝
- **编译共享**：两种模式对相同无效代码返回相同编译错误

### 属性测试（推荐，标记 `[Category("Slow")]`）

使用手动随机生成（项目无外部 PBT 库依赖），每个属性最少 100 次迭代：

- **Property 1 测试**：生成随机字符串（限定 ASCII 可打印字符，排除 `"`, `\`, `\n` 等需 C# 转义的字符），构造 `Console.WriteLine("{s}")` 代码，分别在两种模式执行，验证 output 包含 s
- **Property 2 测试**：生成随机异常消息（同上字符集限定），构造 `throw new Exception("{m}")` 代码，在主线程模式执行，验证 error 包含 m
- **Property 3 测试**：随机选择 mainThread 参数值（true/false/不传），关闭开关，验证均被拒绝
- **Property 4 测试**：生成合法代码，分别在两种模式执行，验证响应 JSON 包含 `success`、`output`、`error`、`warning` 四个键
- **Property 5 测试**：在主线程模式执行合法代码，验证 `warning` 字段非空且包含冻结风险关键词
- **Property 6 测试**：构造执行耗时略超 10s 的代码（如 `Thread.Sleep(11000)`），在主线程模式执行，验证正常返回而非超时错误（注：此测试耗时较长，标记 `[Category("Slow")]`）

标签格式：`// Feature: execute-immediate-main-thread, Property {N}: {描述}`
