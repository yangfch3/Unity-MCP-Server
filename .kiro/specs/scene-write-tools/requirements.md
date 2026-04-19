# Requirements Document

## Introduction

为 Unity MCP Server 新增一组"写与改"类场景操作工具（9 个），补全当前仅有"读与查"类工具的能力缺口。这些工具使外部 AI Agent 能够通过 MCP 协议直接操纵 Unity 场景中的 GameObject 及其组件，实现创建、删除、修改层级结构、切换显隐、启停组件、修改 Transform 属性和 Inspector 序列化字段等操作。

所有工具遵循现有架构约定：实现 `IMcpTool` 接口、由 ToolRegistry 反射自动发现、支持 `instanceID` 和 `path` 双模式定位 GameObject、Prefab Stage 优先回退 Active Scene。

## Glossary

- **Agent**: 通过 MCP 协议连接 Unity Editor 的外部 AI Agent（如 Kiro、Cursor、Claude Desktop）
- **GO**: GameObject 的缩写，Unity 场景中的基本实体
- **Scene**: Unity 当前活跃场景（Active Scene），或 Prefab Stage 中的 Prefab 内容
- **Prefab_Stage**: Unity 编辑器中编辑 Prefab 的隔离环境，通过 `PrefabStageUtility.GetCurrentPrefabStage()` 检测
- **Component**: 挂载在 GO 上的功能组件（如 Transform、Camera、BoxCollider）
- **Component_Type**: 组件的简短类名（如 "BoxCollider"），不含命名空间前缀
- **SerializedProperty**: Unity 序列化系统暴露的可编辑字段，通过 `SerializedObject` / `SerializedProperty` API 访问
- **Transform**: GO 的空间变换组件，包含 position、rotation、scale 等属性
- **RectTransform**: 继承自 Transform 的 UI 布局组件，额外包含 anchoredPosition、sizeDelta、pivot、anchorMin、anchorMax 等属性
- **AddGOTool**: `editor_addGameObject` 工具
- **AddCompTool**: `editor_addComponent` 工具
- **DeleteGOTool**: `editor_deleteGameObject` 工具
- **RemoveCompTool**: `editor_removeComponent` 工具
- **ReparentTool**: `editor_reparentGameObject` 工具
- **SetActiveTool**: `editor_setActive` 工具
- **SetCompEnabledTool**: `editor_setComponentEnabled` 工具
- **SetTransformTool**: `editor_setTransform` 工具
- **SetFieldTool**: `editor_setField` 工具
- **Target_GO**: 通过 `instanceID` 或 `path` 参数定位到的目标 GameObject
- **Undo_Record**: 通过 `Undo.RegisterCreatedObjectUndo` / `Undo.DestroyObjectImmediate` / `Undo.RecordObject` 等 API 记录的可撤销操作

## Requirements

### Requirement 1: 添加 GameObject

**User Story:** 作为 Agent，我想在当前场景或 Prefab Stage 中创建新的 GameObject，以便通过 MCP 协议构建场景结构。

#### Acceptance Criteria

1. WHEN Agent 调用 AddGOTool 并提供 `name` 参数, THE AddGOTool SHALL 在当前编辑上下文中创建一个以该名称命名的空 GO
2. WHEN Agent 提供 `parentInstanceID` 或 `parentPath` 参数, THE AddGOTool SHALL 将新创建的 GO 设为指定父节点的子对象
3. WHEN Agent 未提供父节点参数, THE AddGOTool SHALL 将新 GO 创建为场景根级对象
4. WHILE 处于 Prefab_Stage, THE AddGOTool SHALL 在 Prefab 内容中创建 GO（未指定父节点时挂载到 Prefab 根节点下）
5. WHILE 未处于 Prefab_Stage, THE AddGOTool SHALL 在 Active Scene 中创建 GO
6. THE AddGOTool SHALL 返回新创建 GO 的 `name`、`path`、`instanceID` 信息
7. THE AddGOTool SHALL 通过 Undo_Record 记录创建操作，使用户可在 Unity Editor 中撤销
8. IF 指定的父节点不存在, THEN THE AddGOTool SHALL 返回错误信息，说明未找到指定的父节点
9. WHEN Agent 未提供 `name` 参数或提供空字符串, THE AddGOTool SHALL 使用 "GameObject" 作为默认名称

### Requirement 2: 添加 Component

**User Story:** 作为 Agent，我想给指定的 GO 添加组件，以便通过 MCP 协议为场景对象赋予功能。

#### Acceptance Criteria

1. WHEN Agent 提供 Target_GO 定位参数和 `componentType` 参数, THE AddCompTool SHALL 在 Target_GO 上添加指定类型的组件
2. THE AddCompTool SHALL 通过简短类名查找组件类型，匹配时忽略大小写
3. THE AddCompTool SHALL 返回添加成功的组件类型名称和 Target_GO 的 `name`、`path`、`instanceID`
4. THE AddCompTool SHALL 通过 Undo_Record 记录添加操作
5. IF 指定的 Component_Type 未找到, THEN THE AddCompTool SHALL 返回错误信息，说明未找到该组件类型
6. IF Target_GO 不存在, THEN THE AddCompTool SHALL 返回错误信息，说明未找到指定的 GO
7. WHEN Agent 未提供 `componentType` 参数, THE AddCompTool SHALL 返回错误信息，说明 componentType 为必填参数

### Requirement 3: 删除 GameObject

**User Story:** 作为 Agent，我想删除场景中的指定 GO，以便通过 MCP 协议清理不需要的对象。

#### Acceptance Criteria

1. WHEN Agent 提供 Target_GO 定位参数, THE DeleteGOTool SHALL 删除该 GO 及其所有子对象
2. THE DeleteGOTool SHALL 通过 `Undo.DestroyObjectImmediate` 执行删除，使用户可撤销
3. THE DeleteGOTool SHALL 返回被删除 GO 的 `name` 和 `path` 信息
4. IF Target_GO 不存在, THEN THE DeleteGOTool SHALL 返回错误信息，说明未找到指定的 GO
5. WHEN Agent 未提供任何定位参数, THE DeleteGOTool SHALL 返回错误信息，说明需要提供 instanceID 或 path

### Requirement 4: 移除 Component

**User Story:** 作为 Agent，我想移除指定 GO 上的某个组件，以便通过 MCP 协议调整对象功能。

#### Acceptance Criteria

1. WHEN Agent 提供 Target_GO 定位参数和 `componentType` 参数, THE RemoveCompTool SHALL 移除 Target_GO 上第一个匹配的指定类型组件
2. THE RemoveCompTool SHALL 通过简短类名匹配组件类型，忽略大小写
3. THE RemoveCompTool SHALL 通过 `Undo.DestroyObjectImmediate` 执行移除，使用户可撤销
4. THE RemoveCompTool SHALL 返回被移除的组件类型名称和 Target_GO 的 `name`、`path`
5. IF Target_GO 上不存在指定类型的组件, THEN THE RemoveCompTool SHALL 返回错误信息，说明未找到该组件
6. IF 指定的组件为 Transform 或 RectTransform, THEN THE RemoveCompTool SHALL 返回错误信息，说明 Transform 组件不可移除
7. IF Target_GO 不存在, THEN THE RemoveCompTool SHALL 返回错误信息

### Requirement 5: 修改父节点（Reparent）

**User Story:** 作为 Agent，我想修改 GO 的父节点，以便通过 MCP 协议调整场景层级结构。

#### Acceptance Criteria

1. WHEN Agent 提供 Target_GO 定位参数和新父节点参数（`newParentInstanceID` 或 `newParentPath`）, THE ReparentTool SHALL 将 Target_GO 移动到新父节点下
2. WHEN Agent 将新父节点参数设为 null 或空字符串, THE ReparentTool SHALL 将 Target_GO 移动到场景根级
3. WHILE 处于 Prefab_Stage 且 Agent 将新父节点参数设为 null 或空字符串, THE ReparentTool SHALL 将 Target_GO 移动到 Prefab 根节点下（而非 Active Scene 根级），因为 Prefab Stage 中不存在独立的场景根级
4. THE ReparentTool SHALL 通过 `Undo.SetTransformParent` 记录操作，使用户可撤销
5. THE ReparentTool SHALL 返回 Target_GO 移动后的 `name`、`path`、`instanceID`
6. IF Target_GO 不存在, THEN THE ReparentTool SHALL 返回错误信息
7. IF 新父节点不存在, THEN THE ReparentTool SHALL 返回错误信息，说明未找到指定的新父节点
8. THE ReparentTool SHALL 支持可选的 `worldPositionStays` 参数（boolean，默认 true），控制移动时是否保持世界坐标不变

### Requirement 6: 修改 GameObject 显隐

**User Story:** 作为 Agent，我想切换 GO 的激活状态，以便通过 MCP 协议控制对象的显示与隐藏。

#### Acceptance Criteria

1. WHEN Agent 提供 Target_GO 定位参数和 `active` 参数（boolean）, THE SetActiveTool SHALL 设置 Target_GO 的 `activeSelf` 为指定值
2. THE SetActiveTool SHALL 通过 Undo_Record 记录操作
3. THE SetActiveTool SHALL 返回 Target_GO 的 `name`、`path` 和修改后的 `activeSelf` 值
4. IF Target_GO 不存在, THEN THE SetActiveTool SHALL 返回错误信息
5. WHEN Agent 未提供 `active` 参数, THE SetActiveTool SHALL 返回错误信息，说明 active 为必填参数

### Requirement 7: 修改 Component 启停

**User Story:** 作为 Agent，我想启用或禁用 GO 上的指定组件，以便通过 MCP 协议精细控制组件行为。

#### Acceptance Criteria

1. WHEN Agent 提供 Target_GO 定位参数、`componentType` 和 `enabled` 参数, THE SetCompEnabledTool SHALL 设置 Target_GO 上第一个匹配组件的 `enabled` 属性
2. THE SetCompEnabledTool SHALL 通过简短类名匹配组件类型，忽略大小写
3. THE SetCompEnabledTool SHALL 通过 Undo_Record 记录操作
4. THE SetCompEnabledTool SHALL 返回组件类型名称、Target_GO 的 `name`、`path` 和修改后的 `enabled` 值
5. IF Target_GO 上指定类型的组件不存在, THEN THE SetCompEnabledTool SHALL 返回错误信息
6. IF 指定类型的组件不支持 `enabled` 属性（即不继承自 `Behaviour` 也不继承自 `Renderer`）, THEN THE SetCompEnabledTool SHALL 返回错误信息，说明该组件不支持启停操作
7. IF Target_GO 不存在, THEN THE SetCompEnabledTool SHALL 返回错误信息

### Requirement 8: 修改 Transform / RectTransform 属性

**User Story:** 作为 Agent，我想修改 GO 的空间变换属性（位置、旋转、缩放等），以便通过 MCP 协议精确布局场景对象。

#### Acceptance Criteria

1. WHEN Agent 提供 Target_GO 定位参数和一个或多个 Transform 属性参数, THE SetTransformTool SHALL 修改 Target_GO 的对应属性
2. THE SetTransformTool SHALL 支持以下 Transform 属性：`localPosition`（Vector3）、`localRotation`（Vector3，欧拉角）、`localScale`（Vector3）
3. WHEN Target_GO 拥有 RectTransform 组件, THE SetTransformTool SHALL 额外支持：`anchoredPosition`（Vector2）、`sizeDelta`（Vector2）、`pivot`（Vector2）、`anchorMin`（Vector2）、`anchorMax`（Vector2）
4. THE SetTransformTool SHALL 通过 Undo_Record 记录操作
5. THE SetTransformTool SHALL 返回 Target_GO 的 `name`、`path` 和修改后的各属性值
6. IF Target_GO 不存在, THEN THE SetTransformTool SHALL 返回错误信息
7. WHEN Agent 未提供任何属性参数, THE SetTransformTool SHALL 返回错误信息，说明至少需要提供一个属性参数
8. THE SetTransformTool SHALL 接受数组格式的向量值（如 `[1, 2, 3]` 表示 Vector3，`[0.5, 0.5]` 表示 Vector2）

### Requirement 9: 修改 Inspector 序列化字段值

**User Story:** 作为 Agent，我想修改选中对象上任意组件的序列化字段值，以便通过 MCP 协议灵活配置组件参数。

#### Acceptance Criteria

1. WHEN Agent 提供 Target_GO 定位参数、`componentType`、`fieldName` 和 `value` 参数, THE SetFieldTool SHALL 修改 Target_GO 上指定组件的指定序列化字段值
2. THE SetFieldTool SHALL 通过 `SerializedObject` / `SerializedProperty` API 执行字段修改
3. THE SetFieldTool SHALL 支持以下 SerializedProperty 类型的写入：Integer、Boolean、Float、String、Enum（按名称或索引）、Vector2、Vector3、Vector4、Color、Rect、ObjectReference（按 instanceID）
4. THE SetFieldTool SHALL 通过 Undo_Record 记录操作（`SerializedObject.ApplyModifiedProperties` 自动支持 Undo）
5. THE SetFieldTool SHALL 返回修改后的字段名称、字段类型和新值
6. IF Target_GO 不存在, THEN THE SetFieldTool SHALL 返回错误信息
7. IF 指定的组件类型在 Target_GO 上不存在, THEN THE SetFieldTool SHALL 返回错误信息
8. IF 指定的 `fieldName` 在该组件上不存在, THEN THE SetFieldTool SHALL 返回错误信息，说明未找到该序列化字段
9. IF 提供的 `value` 与字段类型不兼容, THEN THE SetFieldTool SHALL 返回错误信息，说明值类型不匹配
10. THE SetFieldTool SHALL 通过简短类名匹配 `componentType`，忽略大小写

### Requirement 10: 通用 GO 定位机制

**User Story:** 作为 Agent，我想通过 instanceID 或路径两种方式定位 GO，以便灵活选择最方便的定位方式。

#### Acceptance Criteria

1. THE AddCompTool、DeleteGOTool、RemoveCompTool、ReparentTool、SetActiveTool、SetCompEnabledTool、SetTransformTool、SetFieldTool SHALL 支持 `instanceID`（integer）和 `path`（string）两种定位参数
2. WHEN Agent 同时提供 `instanceID` 和 `path`, THE 工具 SHALL 优先使用 `instanceID`
3. WHEN Agent 未提供任何定位参数, THE 工具 SHALL 返回错误信息，说明需要提供 instanceID 或 path
4. THE 工具 SHALL 在 Prefab_Stage 中优先查找，未找到时回退到 Active Scene（与 SelectGameObjectTool 行为一致）

### Requirement 11: 工具注册与元数据

**User Story:** 作为开发者，我想让所有新工具遵循现有的工具注册机制和命名规范，以便零修改核心代码即可集成。

#### Acceptance Criteria

1. THE 9 个新工具 SHALL 实现 `IMcpTool` 接口
2. THE 9 个新工具 SHALL 使用 `editor` 作为 Category
3. THE 9 个新工具 SHALL 使用以下名称：`editor_addGameObject`、`editor_addComponent`、`editor_deleteGameObject`、`editor_removeComponent`、`editor_reparentGameObject`、`editor_setActive`、`editor_setComponentEnabled`、`editor_setTransform`、`editor_setField`
4. THE 9 个新工具 SHALL 提供符合 JSON Schema 规范的 `InputSchema`
5. THE ToolRegistry SHALL 通过反射自动发现并注册所有 9 个新工具，无需修改 ToolRegistry 代码

### Requirement 12: 错误处理与安全

**User Story:** 作为 Agent，我想在操作失败时获得清晰的错误信息，以便快速修正调用方式。

#### Acceptance Criteria

1. THE 所有写操作工具 SHALL 在参数缺失或无效时返回 `ToolResult.Error`，包含描述性错误信息
2. THE 所有写操作工具 SHALL 将空字符串 `""` 视为未提供（等同于 null），适用于所有 string 类型参数
3. IF 工具执行过程中发生未预期的异常, THEN THE 工具 SHALL 捕获异常并返回包含异常信息的 `ToolResult.Error`，而非抛出异常
