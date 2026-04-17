# Requirements Document

## Introduction

为现有 Unity MCP 插件扩展三类共 10 个新工具，增强 Agent 对 Unity Editor 运行时状态的感知能力、编辑器操作能力和构建流程参与能力。所有新工具基于已有的 `IMcpTool` 接口实现，通过 `ToolRegistry` 反射自动发现机制注册，无需修改核心代码。

三类工具定位：
- **debug 类**：帮助 Agent 理解运行时状态（堆栈、性能、截图）
- **editor 类**：帮助 Agent 感知和操作编辑器（选中对象、场景层级、项目结构、Inspector 属性）
- **build 类**：帮助 Agent 参与构建流程（编译、错误获取、测试运行）

## Glossary

- **MCP_Server**: 运行在 Unity Editor 进程内的 MCP 协议服务端
- **Agent**: 通过 MCP 协议连接到 MCP_Server 的外部客户端程序
- **Tool_Registry**: MCP_Server 内部的工具注册中心，通过反射自动发现所有 IMcpTool 实现
- **Tool_Category**: 工具的逻辑分组标识（debug / editor / build）
- **StackTrace_Tool**: debug 类工具，用于获取最近一条 Error/Exception 的完整堆栈信息
- **Performance_Tool**: debug 类工具，用于获取 FPS、DrawCall、内存占用等关键性能指标
- **Screenshot_Tool**: debug 类工具，用于截取 Game/Scene 视图截图并返回 base64 编码
- **Selection_Tool**: editor 类工具，用于获取当前 Hierarchy/Project 中选中的对象信息
- **Hierarchy_Tool**: editor 类工具，用于获取当前场景的 GameObject 树结构
- **ProjectStructure_Tool**: editor 类工具，用于获取 Assets 目录结构
- **Inspector_Tool**: editor 类工具，用于获取选中对象的 Inspector 序列化字段值
- **Compile_Tool**: build 类工具，用于触发脚本编译并返回编译结果
- **CompileErrors_Tool**: build 类工具，用于获取当前编译错误列表
- **TestRunner_Tool**: build 类工具，用于运行 Unity Test Runner 测试并返回结果
- **GameObject_Tree**: 场景中 GameObject 的父子层级结构，包含名称和挂载的组件列表
- **Serialized_Field**: Unity Inspector 中可见的序列化字段，包含字段名、类型和当前值

## Requirements

### Requirement 1: 获取错误堆栈信息

**User Story:** 作为 Unity 开发者，我希望 Agent 能获取最近一条 Error/Exception 的完整堆栈，以便 Agent 辅助定位代码问题。

#### Acceptance Criteria

1. WHEN Agent 调用 StackTrace_Tool, THE StackTrace_Tool SHALL 返回 Unity Console 中最近一条 Error 或 Exception 级别日志的完整堆栈信息
2. THE StackTrace_Tool SHALL 在返回结果中包含错误消息和完整的调用堆栈文本
3. WHEN Unity Console 中无 Error 或 Exception 级别日志, THE StackTrace_Tool SHALL 返回空结果并附带提示信息"当前无错误日志"

### Requirement 2: 获取性能统计指标

**User Story:** 作为 Unity 开发者，我希望 Agent 能获取关键性能指标，以便 Agent 辅助分析性能瓶颈。

#### Acceptance Criteria

1. WHEN Agent 调用 Performance_Tool, THE Performance_Tool SHALL 返回当前帧率（FPS）、DrawCall 数量和总内存占用量
2. THE Performance_Tool SHALL 以结构化 JSON 格式返回性能数据，包含 fps、drawCalls、memoryUsedMB 字段
3. WHILE Unity Editor 未处于 PlayMode, THE Performance_Tool SHALL 仍返回可用的编辑器性能指标

### Requirement 3: 截取视图截图

**User Story:** 作为 Unity 开发者，我希望 Agent 能截取当前 Game 或 Scene 视图的截图，以便 Agent "看到"当前画面并提供视觉反馈。

#### Acceptance Criteria

1. WHEN Agent 调用 Screenshot_Tool 并指定视图类型参数（game 或 scene）, THE Screenshot_Tool SHALL 截取对应视图的当前画面
2. THE Screenshot_Tool SHALL 将截图以 PNG 格式的 base64 编码字符串返回
3. WHEN Agent 调用 Screenshot_Tool 未指定视图类型参数, THE Screenshot_Tool SHALL 默认截取 Game 视图
4. IF 指定的视图窗口当前未打开, THEN THE Screenshot_Tool SHALL 返回包含"视图未打开"的错误信息

### Requirement 4: 获取选中对象信息

**User Story:** 作为 Unity 开发者，我希望 Agent 能感知我当前在 Hierarchy 或 Project 中选中的对象，以便 Agent 理解我的操作意图。

#### Acceptance Criteria

1. WHEN Agent 调用 Selection_Tool, THE Selection_Tool SHALL 返回当前 Hierarchy 和 Project 面板中所有选中对象的信息
2. THE Selection_Tool SHALL 为每个选中的 GameObject 返回名称、instanceID 和在场景中的路径
3. THE Selection_Tool SHALL 为每个选中的 Project 资源返回资源名称和资源路径
4. WHEN 当前无任何选中对象, THE Selection_Tool SHALL 返回空列表

### Requirement 5: 获取场景层级结构

**User Story:** 作为 Unity 开发者，我希望 Agent 能获取当前场景的 GameObject 树结构，以便 Agent 理解场景组成。

#### Acceptance Criteria

1. WHEN Agent 调用 Hierarchy_Tool, THE Hierarchy_Tool SHALL 返回当前活动场景中所有根级 GameObject 及其子级的树结构
2. THE Hierarchy_Tool SHALL 为每个 GameObject 返回名称、activeSelf 状态和挂载的组件类型名称列表
3. WHEN Agent 调用 Hierarchy_Tool 并指定 maxDepth 参数, THE Hierarchy_Tool SHALL 将树结构遍历限制在指定深度内
4. WHEN Agent 未指定 maxDepth 参数, THE Hierarchy_Tool SHALL 默认使用深度 -1 表示遍历全部层级

### Requirement 6: 获取项目目录结构

**User Story:** 作为 Unity 开发者，我希望 Agent 能获取 Assets 目录结构，以便 Agent 理解项目布局。

#### Acceptance Criteria

1. WHEN Agent 调用 ProjectStructure_Tool, THE ProjectStructure_Tool SHALL 返回 Assets 目录下的文件和文件夹结构
2. WHEN Agent 指定 maxDepth 参数, THE ProjectStructure_Tool SHALL 将目录遍历限制在指定深度内
3. WHEN Agent 未指定 maxDepth 参数, THE ProjectStructure_Tool SHALL 默认使用深度 3
4. THE ProjectStructure_Tool SHALL 为每个条目返回名称、类型（file 或 directory）和相对路径
5. THE ProjectStructure_Tool SHALL 排除 .meta 文件以减少输出噪音

### Requirement 7: 获取 Inspector 属性

**User Story:** 作为 Unity 开发者，我希望 Agent 能读取选中对象的 Inspector 属性值，以便 Agent 理解对象的配置。

#### Acceptance Criteria

1. WHEN Agent 调用 Inspector_Tool, THE Inspector_Tool SHALL 返回当前选中 GameObject 上所有组件的序列化字段值
2. THE Inspector_Tool SHALL 为每个组件返回组件类型名称及其下所有可见 Serialized_Field 的字段名、类型和当前值
3. WHEN 当前无选中 GameObject, THE Inspector_Tool SHALL 返回包含"未选中任何 GameObject"的提示信息
4. WHEN 选中多个 GameObject, THE Inspector_Tool SHALL 仅返回第一个选中对象的 Inspector 属性

### Requirement 8: 触发脚本编译

**User Story:** 作为 Unity 开发者，我希望 Agent 写完代码后能触发编译并获取结果，以便 Agent 自验证代码正确性。

#### Acceptance Criteria

1. WHEN Agent 调用 Compile_Tool, THE Compile_Tool SHALL 触发 Unity Editor 的脚本编译流程
2. WHEN 编译完成, THE Compile_Tool SHALL 返回编译结果，包含成功/失败状态和错误列表
3. THE Compile_Tool SHALL 在错误列表中为每条错误包含文件路径、行号、列号和错误消息
4. IF 编译过程中发生超时（超过 60 秒未完成）, THEN THE Compile_Tool SHALL 返回超时错误信息
5. IF 当前无代码变更需要编译, THEN THE Compile_Tool SHALL 直接返回成功结果并附带提示信息"无需编译，代码已是最新"

### Requirement 9: 获取编译错误列表

**User Story:** 作为 Unity 开发者，我希望 Agent 能获取当前的编译错误列表，以便 Agent 在不触发新编译的情况下了解代码状态。

#### Acceptance Criteria

1. WHEN Agent 调用 CompileErrors_Tool, THE CompileErrors_Tool SHALL 返回当前 Unity Editor 中所有未解决的编译错误
2. THE CompileErrors_Tool SHALL 为每条错误返回文件路径、行号、列号、错误代码和错误消息
3. WHEN 当前无编译错误, THE CompileErrors_Tool SHALL 返回空列表并附带提示信息"当前无编译错误"

### Requirement 10: 运行测试

**User Story:** 作为 Unity 开发者，我希望 Agent 能运行 Unity Test Runner 中的测试并获取结果，以便 Agent 验证代码变更的正确性。

#### Acceptance Criteria

1. WHEN Agent 调用 TestRunner_Tool 并指定测试模式参数（EditMode 或 PlayMode）, THE TestRunner_Tool SHALL 运行对应模式下的所有测试
2. WHEN Agent 调用 TestRunner_Tool 并指定 testFilter 参数, THE TestRunner_Tool SHALL 仅运行名称匹配过滤条件的测试
3. THE TestRunner_Tool SHALL 返回测试执行摘要，包含总数、通过数、失败数和跳过数
4. THE TestRunner_Tool SHALL 为每个失败的测试返回测试名称和失败消息
5. WHEN Agent 未指定测试模式参数, THE TestRunner_Tool SHALL 默认运行 EditMode 测试

### Requirement 11: 工具注册与分类一致性

**User Story:** 作为 MCP 插件维护者，我希望所有新工具遵循已有的接口和命名规范，以保持代码库一致性。

#### Acceptance Criteria

1. THE Tool_Registry SHALL 通过反射自动发现并注册所有 10 个新工具，无需修改 MCP_Server 核心代码
2. THE debug 类工具 SHALL 使用 "debug" 作为 Tool_Category 值
3. THE editor 类工具 SHALL 使用 "editor" 作为 Tool_Category 值
4. THE build 类工具 SHALL 使用 "build" 作为 Tool_Category 值
5. THE 每个新工具 SHALL 遵循 `{category}_{action}` 的命名规范（如 debug_getStackTrace、editor_getSelection、build_compile）
6. THE 每个新工具 SHALL 提供符合 JSON Schema 规范的 InputSchema 属性，描述其接受的参数
