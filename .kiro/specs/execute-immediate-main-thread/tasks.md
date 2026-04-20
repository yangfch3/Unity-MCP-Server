# 实现计划：execute-immediate-main-thread

## 概述

为 `ExecuteImmediateTool` 添加双模式执行能力：主线程模式（默认）和后台模式（opt-in）。实现分为四个阶段：响应格式升级、新增 `RunOnMainThread` 方法、`Execute` 分支逻辑、测试覆盖。

## 任务

- [x] 1. 扩展响应格式，新增 warning 字段
  - [x] 1.1 修改 `BuildResponse` 方法，添加 `warning` 参数
    - 在 `Editor/Tools/ExecuteImmediateTool.cs` 中为 `BuildResponse` 添加第三个参数 `warning`（默认空字符串）
    - 响应 JSON 格式变为 `{success, output, error, warning}`
    - 更新所有现有 `BuildResponse` 调用点，传入空字符串作为 warning
    - 确保现有后台模式行为不受影响
    - _Requirements: 5.3_

  - [x] 1.2 更新 `InputSchema`，声明 `mainThread` 可选布尔参数
    - 在 `InputSchema` JSON 的 `properties` 中添加 `mainThread` 字段，类型 `boolean`
    - 描述文本：`"指定是否在主线程执行。true（默认）可调用 Unity API 但无超时保护；false 在后台线程执行，有 5s 超时保护但不可调用 Unity API"`
    - `mainThread` 不加入 `required` 数组
    - _Requirements: 1.1_

- [x] 2. 实现主线程执行模式
  - [x] 2.1 新增 `RunOnMainThread` 方法
    - 在 `Editor/Tools/ExecuteImmediateTool.cs` 中添加 `private static (string output, string error) RunOnMainThread(MethodInfo method)`
    - 保存并恢复 `Console.Out`（try/finally）
    - 使用 `StringWriter` 捕获 Console 输出
    - 直接调用 `method.Invoke(null, null)`，不创建新线程
    - 捕获 `TargetInvocationException`（取 `InnerException`）和通用 `Exception`
    - 异常时返回已捕获的部分输出 + 错误消息和堆栈
    - _Requirements: 3.1, 3.3, 3.4_

  - [x] 2.2 修改 `Execute` 方法，添加 `mainThread` 参数解析和分支逻辑
    - 解析 `mainThread` 参数：未提供时默认 `true`
    - `mainThread` 为 `true` 或未提供时调用 `RunOnMainThread`
    - `mainThread` 为 `false` 时调用现有 `RunWithTimeout`
    - 主线程模式执行后，设置 warning 文本：`"[WARNING: mainThread mode] No timeout protection. Infinite loops will freeze the Editor. Use mainThread:false for timeout safety."`
    - 后台模式 warning 为空字符串
    - 调用 `BuildResponse(output, error, warning)` 构建响应
    - _Requirements: 1.2, 1.3, 1.4, 3.2, 3.5_

- [x] 3. 检查点 — 确保编译通过
  - 使用 `getDiagnostics` 验证 `ExecuteImmediateTool.cs` 无编译错误，有问题时询问用户。

- [x] 4. 单元测试
  - [x] 4.1 更新现有测试以适配新响应格式
    - 在 `Tests/Editor/ExecuteImmediateToolTests.cs` 中更新所有解析响应 JSON 的测试
    - 验证响应 JSON 包含 `warning` 键
    - 确保现有测试（编译错误、无入口点、成功执行、运行时异常等）仍然通过
    - _Requirements: 5.3_

  - [x] 4.2 新增 InputSchema 验证测试
    - 验证 `mainThread` 字段存在于 `properties` 中
    - 验证 `mainThread` 类型为 `boolean`
    - 验证 `mainThread` 不在 `required` 数组中
    - _Requirements: 1.1_

  - [x] 4.3 新增主线程模式默认行为测试
    - 不传 `mainThread` 参数时，执行成功代码，验证 `success` 为 `true`、`output` 包含预期输出
    - 验证 `warning` 字段非空且包含冻结风险提示关键词
    - _Requirements: 1.2, 3.5_

  - [x] 4.4 新增主线程模式显式 true 测试
    - 传入 `mainThread: true`，验证行为与默认一致
    - _Requirements: 1.3_

  - [x] 4.5 新增主线程模式异常捕获测试
    - 主线程模式下执行抛出异常的代码
    - 验证 `error` 字段包含异常消息和堆栈
    - 验证 `output` 字段包含异常前的部分输出
    - _Requirements: 3.4_

  - [x] 4.6 新增后台模式显式 false 测试
    - 传入 `mainThread: false`，执行成功代码，验证 `success` 为 `true`
    - 验证 `warning` 字段为空字符串
    - _Requirements: 1.4_

  - [x] 4.7 新增安全开关对两种模式的测试
    - 关闭 EditorPrefs 开关，分别传入 `mainThread: true` 和 `mainThread: false`
    - 验证两种情况均返回 `IsError` 为 `true` 且包含 disabled 提示
    - _Requirements: 4.1_

  - [x] 4.8 新增编译共享测试
    - 对相同的无效代码，分别在两种模式下执行
    - 验证返回相同的编译错误信息
    - _Requirements: 5.1_

- [x] 5. 检查点 — 确保所有测试通过
  - 确保所有测试通过，有问题时询问用户。

- [ ]* 6. 属性测试（推荐）
  - [ ]* 6.1 编写 Property 1 属性测试：Console 输出捕获
    - **Property 1: Console 输出捕获（Round-Trip）**
    - 在 `Tests/Editor/ExecuteImmediateToolPropertyTests.cs` 中创建测试类，标记 `[Category("Slow")]`
    - 生成随机 ASCII 字符串（排除 `"`, `\`, `\n`），构造 `Console.WriteLine("{s}")` 代码
    - 分别在主线程和后台模式执行，验证 `output` 包含 s
    - 最少 100 次迭代
    - **Validates: Requirements 2.3, 3.3**

  - [ ]* 6.2 编写 Property 2 属性测试：异常消息保留
    - **Property 2: 异常消息保留**
    - 生成随机异常消息（同上字符集），构造 `throw new Exception("{m}")` 代码
    - 在主线程模式执行，验证 `error` 包含 m
    - 最少 100 次迭代
    - **Validates: Requirements 3.4**

  - [ ]* 6.3 编写 Property 3 属性测试：安全开关普适性
    - **Property 3: 安全开关普适性**
    - 随机选择 mainThread 参数值（true/false/不传）
    - 关闭 EditorPrefs 开关，验证均被拒绝
    - 最少 100 次迭代
    - **Validates: Requirements 4.1**

  - [ ]* 6.4 编写 Property 4 属性测试：响应格式一致性
    - **Property 4: 响应格式一致性**
    - 生成合法代码，分别在两种模式执行
    - 验证响应 JSON 包含 `success`、`output`、`error`、`warning` 四个键
    - 最少 100 次迭代
    - **Validates: Requirements 5.3**

  - [ ]* 6.5 编写 Property 5 属性测试：主线程模式警告存在性
    - **Property 5: 主线程模式警告存在性**
    - 在主线程模式执行合法代码，验证 `warning` 非空且包含冻结风险关键词
    - 最少 100 次迭代
    - **Validates: Requirement 3.5**

  - [ ]* 6.6 编写 Property 6 属性测试：主线程模式无超时限制
    - **Property 6: 主线程模式无超时限制**
    - 构造执行耗时略超 10s 的代码（`Thread.Sleep(11000)`）
    - 在主线程模式执行，验证正常返回而非超时错误
    - 注意：此测试耗时较长
    - **Validates: Requirement 3.2**

- [x] 7. 最终检查点 — 确保所有测试通过
  - 确保所有测试通过，有问题时询问用户。

## 备注

- 标记 `*` 的任务为可选，可跳过以加速 MVP
- 单元测试（任务 4）为必需，符合项目 Testing Standards
- 属性测试（任务 6）为推荐，标记 `[Category("Slow")]`
- 每个任务引用了对应的需求条款以确保可追溯性
- 实现语言：C#（Unity 2022.3+，Editor assembly）
