# Requirements Document

## Introduction

为 Unity 2022+ 构建一个最小可行的 MCP (Model Context Protocol) 服务插件。该插件以 Unity Package 形式分发，安装后在 Unity Editor 中提供 MCP 服务端能力，允许外部 Agent 通过 MCP 协议访问 Unity Editor 的部分功能。本次实验聚焦于打通端到端流程，仅实现最核心的三项能力。

## Glossary

- **MCP_Server**: 运行在 Unity Editor 进程内的 MCP 协议服务端，负责监听外部 Agent 连接并分发请求
- **Agent**: 通过 MCP 协议连接到 MCP_Server 的外部客户端程序（如 IDE 插件、CLI 工具等）
- **Console_Tool**: MCP_Server 暴露的工具，用于获取 Unity Console 中的日志信息
- **Menu_Tool**: MCP_Server 暴露的工具，用于调用 Unity Editor 菜单栏中的功能选项
- **PlayMode_Tool**: MCP_Server 暴露的工具，用于控制 Unity Editor 进入或退出 PlayMode
- **Config_Panel**: Unity Editor 中的配置窗口，用于启动/停止 MCP_Server 及监测 Agent 连接状态
- **PlayMode**: Unity Editor 的运行模式，用于在编辑器内运行和测试游戏
- **Tool_Category**: MCP_Server 中对工具的逻辑分组（如 "editor"、"debug" 等），用于组织和发现工具
- **Tool_Registry**: MCP_Server 内部的工具注册中心，负责管理所有 Tool_Category 及其下属工具的注册与查找

## Requirements

### Requirement 1: MCP 服务启停

**User Story:** 作为 Unity 开发者，我希望能在 Unity Editor 中启动和停止 MCP 服务，以便控制外部 Agent 何时可以连接。

#### Acceptance Criteria

1. WHEN Unity Editor 菜单中的 MCP 配置选项被点击, THE Config_Panel SHALL 打开一个 EditorWindow 显示 MCP 服务状态和控制按钮
2. WHEN 用户在 Config_Panel 中点击"启动"按钮, THE MCP_Server SHALL 在本地指定端口上开始监听 MCP 协议连接
3. WHEN 用户在 Config_Panel 中点击"停止"按钮, THE MCP_Server SHALL 断开所有已连接的 Agent 并停止监听
4. WHILE MCP_Server 处于运行状态, THE Config_Panel SHALL 显示当前已连接的 Agent 数量
5. IF MCP_Server 启动时指定端口已被占用, THEN THE MCP_Server SHALL 在 Config_Panel 中显示端口冲突的错误提示
6. WHEN Unity Editor 发生 Domain Reload（如进入/退出 PlayMode）, THE MCP_Server SHALL 自动恢复到 Domain Reload 前的运行状态

### Requirement 2: 获取 Console 日志

**User Story:** 作为 Unity 开发者，我希望外部 Agent 能获取 Unity Console 中最近的日志，以便 Agent 辅助我分析运行时问题。

#### Acceptance Criteria

1. WHEN Agent 调用 Console_Tool 并指定日志条数参数, THE Console_Tool SHALL 返回 Unity Console 中最近 N 条日志记录
2. THE Console_Tool SHALL 在每条日志记录中包含日志级别（Log / Warning / Error）、时间戳和日志内容
3. WHEN Agent 调用 Console_Tool 且 Unity Console 中无日志记录, THE Console_Tool SHALL 返回空列表
4. IF Agent 请求的日志条数超过当前可用日志总数, THEN THE Console_Tool SHALL 返回所有可用日志而非报错

### Requirement 3: 调用 Unity 菜单功能

**User Story:** 作为 Unity 开发者，我希望外部 Agent 能调用 Unity 菜单栏中的功能，以便 Agent 自动化执行编辑器操作。

#### Acceptance Criteria

1. WHEN Agent 调用 Menu_Tool 并提供菜单路径参数, THE Menu_Tool SHALL 执行对应的 Unity Editor 菜单项
2. IF Agent 提供的菜单路径不存在, THEN THE Menu_Tool SHALL 返回包含"菜单路径不存在"的错误信息
3. WHEN Agent 调用 Menu_Tool 成功执行菜单项, THE Menu_Tool SHALL 返回执行成功的确认信息

### Requirement 4: PlayMode 控制

**User Story:** 作为 Unity 开发者，我希望外部 Agent 能控制 Unity Editor 进入和退出 PlayMode，以便 Agent 自动化测试流程。

#### Acceptance Criteria

1. WHEN Agent 调用 PlayMode_Tool 并指定动作为"进入", THE PlayMode_Tool SHALL 使 Unity Editor 进入 PlayMode
2. WHEN Agent 调用 PlayMode_Tool 并指定动作为"退出", THE PlayMode_Tool SHALL 使 Unity Editor 退出 PlayMode
3. WHEN Agent 调用 PlayMode_Tool 查询状态, THE PlayMode_Tool SHALL 返回当前 PlayMode 状态（Playing 或 Stopped）
4. IF Agent 请求进入 PlayMode 但 Unity Editor 已处于 PlayMode, THEN THE PlayMode_Tool SHALL 返回"已处于 PlayMode"的提示信息而非重复进入
5. IF Agent 请求退出 PlayMode 但 Unity Editor 未处于 PlayMode, THEN THE PlayMode_Tool SHALL 返回"未处于 PlayMode"的提示信息而非报错

### Requirement 5: 非侵入性约束

**User Story:** 作为 Unity 开发者，我希望 MCP 插件不影响 Unity 自身功能和项目代码，以便安全地在任何项目中使用。

#### Acceptance Criteria

1. THE MCP_Server SHALL 仅在 Unity Editor 环境下运行，不编译到游戏运行时构建中
2. THE MCP_Server SHALL 将所有代码放置在 Editor 程序集中，不引入任何运行时程序集依赖
3. WHILE MCP_Server 处于停止状态, THE MCP_Server SHALL 不占用任何系统端口或后台线程
4. THE MCP_Server SHALL 以 Unity Package 形式组织，通过 Unity Package Manager 安装和卸载

### Requirement 6: 工具分类与可扩展性

**User Story:** 作为 MCP 插件的维护者，我希望工具按类别组织并支持便捷扩展，以便将来新增 MCP 工具时无需修改已有代码。

#### Acceptance Criteria

1. THE Tool_Registry SHALL 按 Tool_Category 对所有已注册工具进行分组管理
2. THE MCP_Server SHALL 支持通过实现统一接口并注册到 Tool_Registry 的方式新增工具，无需修改 MCP_Server 核心代码
3. WHEN Agent 请求工具列表, THE MCP_Server SHALL 返回按 Tool_Category 分组的工具清单
4. THE Tool_Registry SHALL 为每个工具维护名称、所属 Tool_Category 和描述信息
5. WHEN 新工具注册到 Tool_Registry, THE MCP_Server SHALL 自动将该工具暴露给已连接的 Agent
