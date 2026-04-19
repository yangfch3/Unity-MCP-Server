# Implementation Plan: editor_findGameObjects Tool

## Overview

为 Unity MCP Server 新增 `editor_findGameObjects` 搜索工具。实现步骤：先提取共享辅助方法（`GetGameObjectPath`），再实现核心工具类（通配符匹配、名称/组件过滤、递归搜索），然后更新 ToolRegistry 测试和 README 文档，最后通过属性测试验证正确性。

## Tasks

- [x] 1. 提取共享辅助方法 `GetGameObjectPath`
  - [x] 1.1 创建共享辅助类 `GameObjectPathHelper`（`Editor/Tools/GameObjectPathHelper.cs`），将 `GetGameObjectPath` 提取为 `internal static` 方法
    - 从 `HierarchyTool.GetGameObjectPath` 和 `SelectGameObjectTool.GetGameObjectPath` 中提取逻辑
    - 新类放在 `UnityMcp.Editor.Tools` 命名空间
    - _Requirements: 1.4_
  - [x] 1.2 修改 `HierarchyTool.cs` 和 `SelectGameObjectTool.cs`，将各自的 `GetGameObjectPath` 替换为调用共享方法
    - 删除两个文件中的 private `GetGameObjectPath` 方法
    - 替换为 `GameObjectPathHelper.GetGameObjectPath(go)` 调用
    - _Requirements: 1.4_
  - [x] 1.3 修改 `HierarchyToolTestHelper.cs`，将其 `GetGameObjectPath` 替换为调用共享方法
    - 测试辅助类中的同名方法也统一为调用 `GameObjectPathHelper`
    - _Requirements: 1.4_

- [x] 2. 实现 `FindGameObjectsTool` 核心类
  - [x] 2.1 创建 `Editor/Tools/FindGameObjectsTool.cs`，实现 `IMcpTool` 接口
    - Name = `editor_findGameObjects`，Category = `editor`
    - 实现 `InputSchema`（namePattern, componentType, maxResults, activeOnly）
    - 实现 `Execute` 方法：参数解析、校验、根节点解析、递归搜索、JSON 构建
    - 实现 `internal static WildcardMatch(pattern, text)` 通配符匹配（`*`/`?`，大小写不敏感）
    - 实现 `MatchesName`（无通配符时子串匹配）、`MatchesComponent`（简短类名匹配）
    - 实现 `SearchRecursive`（递归遍历，activeOnly 过滤，maxResults 限制 + totalFound 计数）
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 3.1, 3.2, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 7.1, 7.2, 7.3, 7.4, 8.1, 8.2, 8.3_

- [x] 3. 编写单元测试
  - [x] 3.1 创建 `Tests/Editor/FindGameObjectsToolTests.cs`
    - 元数据断言：Name、Category
    - InputSchema 结构验证：包含 namePattern、componentType、maxResults、activeOnly
    - 参数校验：无参数返回错误、空字符串视为未提供、maxResults < 1 返回错误
    - 名称搜索：子串匹配、通配符匹配（`*`/`?`）、大小写不敏感
    - 组件搜索：按类型过滤、大小写不敏感
    - 组合搜索：AND 语义
    - 结果限制：maxResults 截断、truncated 标记、totalFound 计数
    - 空结果：返回空数组和 count:0 而非错误
    - 递归搜索：深层嵌套 GO 能被找到
    - activeOnly 过滤：true 时跳过 inactive GO，false 时包含所有
    - 返回结构：每项包含 name、path、instanceID、components
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 2.1, 2.2, 2.3, 3.1, 3.2, 4.3, 4.4, 4.5, 4.6, 5.1, 5.2, 5.3, 5.4, 6.1, 6.2, 6.3, 8.1, 8.2, 8.3_

- [x] 4. 更新 ToolRegistry 测试和 README
  - [x] 4.1 更新 `Tests/Editor/ToolRegistryTests.cs`
    - 在 `AutoDiscover_FindsAllExpectedTools` 中添加 `editor_findGameObjects` 断言
    - 在 `ListByCategory_Editor_ReturnsCorrectTools` 中添加 `editor_findGameObjects` 断言
    - 更新 `Assert.GreaterOrEqual` 的工具总数
    - _Requirements: 7.4_
  - [x] 4.2 更新 `README.md` 和 `README_EN.md` 的 Editor 工具表格
    - 在 Editor 工具表格中添加 `editor_findGameObjects` 行
    - _Requirements: 7.2_

- [x] 5. Checkpoint — 编译并运行单元测试
  - 确保所有测试通过，如有问题请询问用户。

- [ ] 6. 编写属性测试
  - [ ]* 6.1 创建 `Tests/Editor/FindGameObjectsToolPropertyTests.cs` — Property 1: 名称过滤无误报
    - **Property 1: Name filtering — no false positives**
    - 100 次迭代，随机 GO 树 + 随机通配符模式，验证返回结果的每个 name 都匹配该模式
    - **Validates: Requirements 1.1, 1.2**
  - [ ]* 6.2 Property 2: 子串回退等价性
    - **Property 2: Substring fallback equivalence**
    - 100 次迭代，随机无通配符字符串，验证 pattern 搜索结果 == `*pattern*` 搜索结果
    - **Validates: Requirements 1.3**
  - [ ]* 6.3 Property 5: maxResults 上限与截断准确性
    - **Property 5: Result count respects maxResults and truncation is accurate**
    - 100 次迭代，随机 maxResults 值，验证 count ≤ maxResults，truncated 标记正确
    - **Validates: Requirements 5.2, 5.4**
  - [ ]* 6.4 Property 6: 输出结构完整性
    - **Property 6: Output structure completeness**
    - 100 次迭代，随机搜索条件，验证返回 JSON 包含 results/count，每项包含 name/path/instanceID/components
    - **Validates: Requirements 1.4, 6.1, 6.2**
  - [ ]* 6.5 Property 7: 非法 maxResults 始终拒绝
    - **Property 7: Invalid maxResults always rejected**
    - 100 次迭代，随机非正整数，验证 IsError = true
    - **Validates: Requirements 8.2**

- [x] 7. Final Checkpoint — 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户。

## Notes

- 标记 `*` 的任务为可选，可跳过以加速 MVP
- 每个任务引用了对应的需求条款以便追溯
- 属性测试使用项目现有模式（`System.Random` + 100 次循环），标记 `[Category("Slow")]`
- Property 3（组件过滤）和 Property 4（交集语义）因 EditMode 中动态添加组件的限制，由单元测试中的具体示例覆盖
