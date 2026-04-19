# Requirements Document

## Introduction

为 Unity MCP Server 新增 `editor_findGameObjects` 工具，支持按名称（模糊/通配符）和组件类型在当前场景中搜索 GameObject。返回匹配结果列表（name、path、instanceID），方便 Agent 直接用于后续的 `editor_selectGameObject` 等操作。

该工具填补现有工具链的缺口：当前只能通过 `editor_getHierarchy` 全量拉取场景树再人工查找目标，大场景下效率极低。

## Glossary

- **FindTool**: `editor_findGameObjects` 工具，实现 `IMcpTool` 接口的 GameObject 搜索工具
- **Agent**: 通过 MCP 协议连接 Unity Editor 的外部 AI Agent（如 Kiro、Cursor、Claude Desktop）
- **Scene**: Unity 当前活跃场景（Active Scene），或 Prefab Stage 中的 Prefab 内容
- **GO**: GameObject 的缩写，Unity 场景中的基本实体
- **Wildcard_Pattern**: 使用 `*` 和 `?` 的通配符模式，`*` 匹配零个或多个任意字符，`?` 匹配恰好一个任意字符
- **Component_Type**: Unity 组件的类型名称（如 "Camera"、"MeshRenderer"），使用简短类名而非完全限定名

## Requirements

### Requirement 1: 按名称搜索 GameObject

**User Story:** 作为 Agent，我想按名称模糊搜索场景中的 GameObject，以便在大场景中快速定位目标对象。

#### Acceptance Criteria

1. WHEN Agent 提供 `namePattern` 参数, THE FindTool SHALL 返回所有名称匹配该 Wildcard_Pattern 的 GO 列表
2. THE FindTool SHALL 在名称匹配时忽略大小写
3. WHEN `namePattern` 不包含通配符 `*` 或 `?`, THE FindTool SHALL 将其视为子串匹配（等价于 `*pattern*`）
4. FOR ALL 返回的每个匹配结果, THE FindTool SHALL 包含 `name`（GO 名称）、`path`（从根节点开始的绝对路径，如 "/Root/Child/Target"）和 `instanceID`（GO 的 instanceID）字段

### Requirement 2: 按组件类型搜索 GameObject

**User Story:** 作为 Agent，我想按挂载的组件类型搜索 GameObject，以便快速找到特定功能的对象（如所有 Camera、所有 Light）。

#### Acceptance Criteria

1. WHEN Agent 提供 `componentType` 参数, THE FindTool SHALL 返回所有挂载了该 Component_Type 的 GO 列表
2. THE FindTool SHALL 在组件类型匹配时忽略大小写
3. THE FindTool SHALL 使用组件的简短类名进行匹配（如 "Camera" 而非 "UnityEngine.Camera"）

### Requirement 3: 组合搜索条件

**User Story:** 作为 Agent，我想同时按名称和组件类型搜索，以便精确缩小搜索范围。

#### Acceptance Criteria

1. WHEN Agent 同时提供 `namePattern` 和 `componentType` 参数, THE FindTool SHALL 仅返回同时满足两个条件的 GO 列表
2. WHEN Agent 未提供任何搜索参数, THE FindTool SHALL 返回参数错误提示，要求至少提供一个搜索条件

### Requirement 4: 搜索范围与上下文

**User Story:** 作为 Agent，我想让搜索工具自动适配当前编辑上下文（Prefab Stage 或 Active Scene），并支持按激活状态过滤，以便搜索结果与当前工作环境一致。

#### Acceptance Criteria

1. WHILE 处于 Prefab Stage, THE FindTool SHALL 仅在 Prefab 内容中搜索
2. WHILE 未处于 Prefab Stage, THE FindTool SHALL 在 Active Scene 的所有根 GO 及其子树中搜索
3. THE FindTool SHALL 递归搜索所有层级的子 GO，不受深度限制
4. THE FindTool SHALL 支持可选的 `activeOnly` 参数（boolean，默认 true）
5. WHEN `activeOnly` 为 true（默认）, THE FindTool SHALL 仅搜索 `activeInHierarchy` 为 true 的 GO
6. WHEN `activeOnly` 为 false, THE FindTool SHALL 搜索所有 GO（包括未激活的）

### Requirement 5: 结果数量限制

**User Story:** 作为 Agent，我想限制返回结果数量，以避免大场景下返回过多数据导致响应过大。

#### Acceptance Criteria

1. THE FindTool SHALL 支持可选的 `maxResults` 参数，类型为正整数
2. WHEN Agent 提供 `maxResults` 参数, THE FindTool SHALL 最多返回指定数量的匹配结果
3. WHEN Agent 未提供 `maxResults` 参数, THE FindTool SHALL 默认最多返回 50 条结果
4. WHEN 匹配结果超过 `maxResults` 限制, THE FindTool SHALL 在返回数据中包含 `truncated: true` 标记和 `totalFound` 总匹配数

### Requirement 6: 返回格式

**User Story:** 作为 Agent，我想获得结构化的搜索结果，以便直接用于后续工具调用。

#### Acceptance Criteria

1. THE FindTool SHALL 以 JSON 格式返回结果，顶层包含 `results` 数组和 `count` 字段
2. FOR ALL 返回的每个结果项, THE FindTool SHALL 包含 `name`（string）、`path`（string）、`instanceID`（int）和 `components`（string 数组，列出该 GO 上所有组件的简短类名）字段
3. WHEN 搜索无匹配结果, THE FindTool SHALL 返回空 `results` 数组和 `count: 0`，而非错误

### Requirement 7: 工具注册与元数据

**User Story:** 作为开发者，我想让新工具遵循现有的工具注册机制，以便零修改核心代码即可集成。

#### Acceptance Criteria

1. THE FindTool SHALL 实现 `IMcpTool` 接口
2. THE FindTool SHALL 使用工具名称 `editor_findGameObjects`，分类为 `editor`
3. THE FindTool SHALL 提供符合 JSON Schema 规范的 `InputSchema`，描述 `namePattern`（string，可选）、`componentType`（string，可选）、`maxResults`（integer，可选，默认 50）和 `activeOnly`（boolean，可选，默认 true）参数
4. THE ToolRegistry SHALL 通过反射自动发现并注册 FindTool，无需修改 ToolRegistry 代码

### Requirement 8: 错误处理

**User Story:** 作为 Agent，我想在参数错误时获得清晰的错误信息，以便快速修正调用方式。

#### Acceptance Criteria

1. WHEN Agent 未提供任何搜索参数（`namePattern` 和 `componentType` 均为空/null/空字符串）, THE FindTool SHALL 返回错误信息，说明至少需要提供一个搜索条件
2. WHEN `maxResults` 参数值小于 1, THE FindTool SHALL 返回错误信息，说明 `maxResults` 必须为正整数
3. THE FindTool SHALL 将空字符串 `""` 视为未提供（等同于 null），`namePattern` 和 `componentType` 均适用此规则
