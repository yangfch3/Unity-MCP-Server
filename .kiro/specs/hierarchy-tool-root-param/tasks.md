# Implementation Plan: hierarchy-tool-root-param

## Overview

扩展 `HierarchyTool` 增加 `root` 参数（缺省=Prefab Stage 优先/Active Scene，`"selection"`=activeGameObject 单选），新增 `SelectGameObjectTool`（`editor_selectGameObject`）通过路径选中 GameObject。实现语言：C#。

## Tasks

- [x] 1. 扩展 HierarchyTool 增加 root 参数路由逻辑
  - [x] 1.1 更新 HierarchyTool.InputSchema，新增 `root` 属性（type: string, default: ""）
    - 保留现有 `maxDepth` 属性不变
    - _Requirements: 5.1, 5.2_
  - [x] 1.2 实现 root 参数解析与路由分支
    - 在 `Execute` 方法中解析 `params["root"]`，默认 null
    - root 为 null 或空串 → 调用 `ResolveDefaultRoots()`
    - root 为 `"selection"` → 调用 `ResolveSelectionRoot()`
    - 其他值 → 返回 `ToolResult.Error`，列出支持的值
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 3.1_
  - [x] 1.3 实现 ResolveDefaultRoots 方法（Prefab Stage 优先）
    - 使用 `PrefabStageUtility.GetCurrentPrefabStage()` 检测 Prefab Stage
    - 非 null 时返回 `[stage.prefabContentsRoot]`
    - null 时回退 `SceneManager.GetActiveScene().GetRootGameObjects()`
    - 需添加 `using UnityEditor.SceneManagement` / `UnityEditor.Experimental.SceneManagement`（视 Unity 版本）
    - _Requirements: 1.1, 1.2, 1.3, 1.4_
  - [x] 1.4 实现 ResolveSelectionRoot 方法（Selection 单选语义）
    - 使用 `Selection.activeGameObject`（仅单选）
    - 非 null 时返回 `[go]`
    - null 时返回 `ToolResult.Error("当前没有选中任何 GameObject")`
    - _Requirements: 2.1, 2.2_

- [x] 2. 新增 SelectGameObjectTool
  - [x] 2.1 创建 `Editor/Tools/SelectGameObjectTool.cs`，实现 `IMcpTool` 接口
    - Name = `"editor_selectGameObject"`，Category = `"editor"`
    - InputSchema 包含 required `path` 属性
    - _Requirements: 7.1, 7.2_
  - [x] 2.2 实现 Execute 方法与 FindByPath 逻辑
    - path 为空/缺失 → `ToolResult.Error`
    - `FindByPath`: Prefab Stage 优先查找，回退 Active Scene
    - `SearchInRoot`: 按 `/` 分割路径，逐级 `Transform.Find`
    - 找到 → 设置 `Selection.activeGameObject`，返回 `{name, path, instanceID}`
    - 未找到 → `ToolResult.Error`，包含传入路径
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

- [x] 3. Checkpoint — 编译验证
  - Ensure all code compiles without errors, ask the user if questions arise.

- [x] 4. 创建测试辅助与 HierarchyTool 单元测试
  - [x] 4.1 创建 `Tests/Editor/HierarchyToolTestHelper.cs`
    - `CreateRandomTree(maxDepth, maxChildren)` — 生成随机 GameObject 树，返回根节点和所有节点列表
    - `GetGameObjectPath(go)` — 计算 GameObject 完整路径
    - `MeasureJsonTreeDepth(json)` — 解析 JSON 输出，返回最大嵌套深度
    - `CleanupGameObjects(list)` — TearDown 时销毁所有测试 GO
    - _Requirements: 2.1, 4.1_
  - [x] 4.2 在 `Tests/Editor/HierarchyToolTests.cs` 中新增 root 参数相关测试
    - `root=""` 与缺省行为等价（返回 Active Scene 根对象）
    - `root="selection"` 无选中对象时返回 `IsError`
    - `root="selection"` 有选中对象时返回以该对象为根的子树
    - 无效 root 值返回 `IsError`
    - InputSchema 包含 `root` 和 `maxDepth` 属性
    - _Requirements: 1.3, 1.4, 2.1, 2.2, 3.1, 5.1, 5.2_
  - [ ]* 4.3 编写属性测试：Property 1 — Selection 模式返回选中节点的子树
    - **Property 1: Selection 模式返回选中节点的子树**
    - 生成随机 GameObject 树，随机选中一个节点，验证 `root="selection"` 返回的树以该节点为根
    - 标记 `[Category("Slow")]`，至少 100 次迭代
    - **Validates: Requirements 2.1**
  - [x]* 4.4 编写属性测试：Property 2 — 无效 root 值一律返回错误
    - **Property 2: 无效 root 值一律返回错误**
    - 生成随机字符串（排除 "" 和 "selection"），验证均返回 `IsError`
    - 标记 `[Category("Slow")]`
    - **Validates: Requirements 3.1**
  - [ ]* 4.5 编写属性测试：Property 3 — maxDepth 在所有模式下一致限制树深度
    - **Property 3: maxDepth 在所有模式下一致限制树深度**
    - 生成随机树 + 随机 maxDepth (0..10)，在缺省和 selection 模式下验证输出树深度不超过 maxDepth
    - 标记 `[Category("Slow")]`
    - **Validates: Requirements 4.1, 4.2, 4.3**

- [x] 5. SelectGameObjectTool 单元测试与属性测试
  - [x] 5.1 创建 `Tests/Editor/SelectGameObjectToolTests.cs`
    - Name = `"editor_selectGameObject"`，Category = `"editor"` 断言
    - InputSchema 包含 required `path` 属性
    - path 为空时返回 `IsError`
    - 有效路径选中目标 GO 后 `Selection.activeGameObject` 指向该 GO
    - 不存在的路径返回 `IsError`
    - _Requirements: 6.1, 6.2, 6.3, 7.1, 7.2_
  - [ ]* 5.2 编写属性测试：Property 4 — 有效路径正确选中目标 GameObject
    - **Property 4: 有效路径正确选中目标 GameObject**
    - 生成随机 GameObject 树，随机选一个节点计算路径，调用 SelectGameObjectTool，验证 `Selection.activeGameObject` 指向该节点
    - 标记 `[Category("Slow")]`
    - **Validates: Requirements 6.1**
  - [x]* 5.3 编写属性测试：Property 5 — 不存在的路径返回错误
    - **Property 5: 不存在的路径返回错误**
    - 生成随机路径字符串（不匹配任何现有 GO），验证返回 `IsError`
    - 标记 `[Category("Slow")]`
    - **Validates: Requirements 6.2**

- [x] 6. 更新 ToolRegistryTests 并最终验证
  - [x] 6.1 在 `Tests/Editor/ToolRegistryTests.cs` 中添加 `editor_selectGameObject` 断言
    - `AutoDiscover_FindsAllExpectedTools` 中新增 `Assert.Contains("editor_selectGameObject", names)`
    - 更新 `Assert.GreaterOrEqual` 的工具总数为 16
    - `ListByCategory_Editor_ReturnsCorrectTools` 中新增 `Assert.Contains("editor_selectGameObject", names)`
    - _Requirements: 7.2_

- [x] 7. Final checkpoint — 全部测试通过
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties, tagged `[Category("Slow")]`
- Unit tests validate specific examples and edge cases
- 测试辅助文件 `HierarchyToolTestHelper.cs` 被属性测试和单元测试共享
