# Requirements Document

## Introduction

扩展现有的 `editor_getHierarchy` 工具（HierarchyTool），增加可选的 `root` 参数，使其能够根据不同的根节点来源遍历 GameObject 树。缺省行为自动感知 Prefab Mode 与 Scene Mode；传入 `"selection"` 时以当前选中对象为根遍历子树，覆盖 DontDestroyOnLoad 和 Prefab Mode 下的高频使用场景。

同时新增 `editor_selectGameObject` 工具（SelectGameObjectTool），支持通过路径程序化选中 GameObject，打通"获取子树 → 定位目标节点 → 查看 Inspector"的自动化调试链路。

## Glossary

- **HierarchyTool**: 实现 `IMcpTool` 接口的工具类，工具名为 `editor_getHierarchy`，负责获取 GameObject 树结构
- **Root_Parameter**: HierarchyTool 的可选字符串参数 `root`，用于指定遍历的根节点来源
- **Prefab_Stage**: Unity 的 Prefab 编辑模式，通过 `PrefabStageUtility.GetCurrentPrefabStage()` 获取，非 null 时表示当前处于 Prefab 编辑状态
- **Active_Scene**: Unity 当前激活的场景，通过 `SceneManager.GetActiveScene()` 获取
- **Selection**: Unity Editor 中当前选中的 GameObject，通过 `Selection.activeGameObject` 获取
- **MaxDepth_Parameter**: HierarchyTool 的可选整数参数 `maxDepth`，控制遍历深度，-1 表示无限制
- **SelectGameObjectTool**: 新增的 `IMcpTool` 实现，工具名为 `editor_selectGameObject`，通过路径程序化选中 Hierarchy 中的 GameObject
- **GameObject_Path**: GameObject 在 Hierarchy 中的完整路径，格式为 `"/Root/Child/Target"`，与 HierarchyTool 输出的树结构对应

## Requirements

### Requirement 1: 缺省模式下自动感知 Prefab Stage

**User Story:** 作为 AI Agent，我希望在不传 `root` 参数时，HierarchyTool 能自动检测当前是否处于 Prefab Mode 并返回对应的根节点树，以便获取正确的编辑上下文。

#### Acceptance Criteria

1. WHEN Root_Parameter is not provided and Prefab_Stage is not null, THE HierarchyTool SHALL return the GameObject tree rooted at the Prefab_Stage root GameObject.
2. WHEN Root_Parameter is not provided and Prefab_Stage is null, THE HierarchyTool SHALL return the GameObject tree of all root GameObjects in the Active_Scene.
3. WHEN Root_Parameter is provided as an empty string and Prefab_Stage is not null, THE HierarchyTool SHALL treat the empty string identically to a missing Root_Parameter and return the Prefab_Stage root GameObject tree.
4. WHEN Root_Parameter is provided as an empty string and Prefab_Stage is null, THE HierarchyTool SHALL treat the empty string identically to a missing Root_Parameter and return the Active_Scene root GameObjects tree.

### Requirement 2: Selection 模式

**User Story:** 作为 AI Agent，我希望传入 `root` 为 `"selection"` 时，HierarchyTool 以当前选中的 GameObject 为根遍历子树，以便在 PlayMode 下获取 DontDestroyOnLoad 中选中对象的子树，或在 Prefab Mode 下获取选中节点的子树。

#### Acceptance Criteria

1. WHEN Root_Parameter is "selection" and Selection.activeGameObject is not null, THE HierarchyTool SHALL return the GameObject tree rooted at the active selected GameObject (including the selected GameObject itself as the root node). NOTE: Only `Selection.activeGameObject` (single object) is used; when multiple GameObjects are selected in the Editor, only the active one is used and the rest are ignored.
2. WHEN Root_Parameter is "selection" and Selection.activeGameObject is null, THE HierarchyTool SHALL return an error result with a descriptive message indicating no GameObject is currently selected.

### Requirement 3: 无效 root 参数处理

**User Story:** 作为 AI Agent，我希望传入不支持的 `root` 值时，HierarchyTool 返回明确的错误信息，以便快速定位参数错误。

#### Acceptance Criteria

1. IF Root_Parameter is provided with a value other than empty string or "selection", THEN THE HierarchyTool SHALL return an error result with a descriptive message listing the supported values.

### Requirement 4: MaxDepth 参数兼容性

**User Story:** 作为 AI Agent，我希望 `maxDepth` 参数在所有 `root` 模式下保持一致的行为，以便统一控制遍历深度。

#### Acceptance Criteria

1. THE HierarchyTool SHALL apply MaxDepth_Parameter consistently across all Root_Parameter modes (missing/empty, "selection").
2. WHEN MaxDepth_Parameter is -1, THE HierarchyTool SHALL traverse the full depth of the GameObject tree regardless of Root_Parameter mode.
3. WHEN MaxDepth_Parameter is 0, THE HierarchyTool SHALL return only the root-level GameObjects without any children regardless of Root_Parameter mode.

### Requirement 5: InputSchema 更新

**User Story:** 作为 AI Agent，我希望 HierarchyTool 的 InputSchema 正确描述新增的 `root` 参数，以便 MCP 客户端能正确发现和使用该参数。

#### Acceptance Criteria

1. THE HierarchyTool SHALL expose an InputSchema that includes a `root` property of type string with a description explaining the supported values and default behavior.
2. THE HierarchyTool SHALL keep the existing `maxDepth` property in the InputSchema unchanged.

### Requirement 6: SelectGameObject 工具 — 通过路径选中 GameObject

**User Story:** 作为 AI Agent，我希望能通过路径程序化选中 Hierarchy 中的 GameObject，以便在获取子树结构后自动定位目标节点，再通过 `editor_getInspector` 查看其详细字段值，完成自动化调试链路。

#### Acceptance Criteria

1. WHEN a valid GameObject_Path is provided and the target GameObject exists in the current scene or Prefab_Stage, THE SelectGameObjectTool SHALL set `Selection.activeGameObject` to the target GameObject and return a success result containing the selected object's name and path.
2. WHEN a valid GameObject_Path is provided but no matching GameObject is found, THE SelectGameObjectTool SHALL return an error result with a descriptive message indicating the path could not be resolved.
3. WHEN the path parameter is not provided or is empty, THE SelectGameObjectTool SHALL return an error result with a descriptive message indicating the path is required.
4. THE SelectGameObjectTool SHALL search for the target GameObject in the current Prefab_Stage first (if active), then fall back to the active scene, consistent with HierarchyTool's priority logic.

### Requirement 7: SelectGameObject 工具 — InputSchema

**User Story:** 作为 AI Agent，我希望 SelectGameObjectTool 的 InputSchema 正确描述 `path` 参数，以便 MCP 客户端能正确发现和使用该工具。

#### Acceptance Criteria

1. THE SelectGameObjectTool SHALL expose an InputSchema that includes a required `path` property of type string with a description explaining the expected format (e.g., `"/Root/Child/Target"`).
2. THE SelectGameObjectTool SHALL have Name `"editor_selectGameObject"` and Category `"editor"`.
