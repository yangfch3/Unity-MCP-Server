# Implementation Plan: Scene Write Tools

## Overview

为 Unity MCP Server 新增 9 个"写与改"类场景操作工具，包含 3 个共享辅助类（GameObjectResolveHelper、ComponentTypeHelper、VectorParseHelper）、对 SelectGameObjectTool 的小幅重构、9 个新工具实现，以及对应的单元测试和属性测试。所有代码使用 C#，遵循现有 IMcpTool 架构。

## Tasks

- [x] 1. 创建共享辅助类
  - [x] 1.1 创建 GameObjectResolveHelper
    - 新建 `Editor/Tools/GameObjectResolveHelper.cs`
    - 实现 `Resolve(Dictionary<string, object> parameters)` 方法：从参数中提取 instanceID/path，定位 GameObject
    - 实现 `Resolve(params, instanceIDKey, pathKey)` 重载：支持自定义参数名（用于 AddGO 的 parentInstanceID/parentPath）
    - 实现 `FindByPath(string path)`：从 SelectGameObjectTool 提取的路径查找逻辑，Prefab Stage 优先回退 Active Scene
    - 实现 `SearchInRoot(GameObject root, string normalizedPath)`：从 SelectGameObjectTool 提取的逐级查找逻辑
    - instanceID 优先于 path，空字符串视为未提供
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

  - [x] 1.2 创建 ComponentTypeHelper
    - 新建 `Editor/Tools/ComponentTypeHelper.cs`
    - 实现 `FindType(string shortName)`：遍历 AppDomain 所有程序集，大小写不敏感查找继承自 Component 的类型
    - 实现 `FindComponent(GameObject go, string typeName)`：在 GO 上按类名大小写不敏感查找第一个匹配组件
    - _Requirements: 2.2, 4.2, 7.2, 9.10_

  - [x] 1.3 创建 VectorParseHelper
    - 新建 `Editor/Tools/VectorParseHelper.cs`
    - 实现 ParseVector2、ParseVector3、ParseVector4、ParseColor、ParseRect 静态方法
    - 实现 ToFloat 内部辅助方法（long/double/int → float）
    - 数组长度不足或类型无法转换时抛出 ArgumentException
    - _Requirements: 8.8, 9.3_

- [x] 2. 重构 SelectGameObjectTool 使用 GameObjectResolveHelper
  - [x] 2.1 重构 SelectGameObjectTool
    - 将 `FindByPath` 和 `SearchInRoot` 方法调用替换为 `GameObjectResolveHelper.FindByPath` 和 `GameObjectResolveHelper.SearchInRoot`
    - 删除 SelectGameObjectTool 中的 `FindByPath` 和 `SearchInRoot` 私有方法
    - 行为保持不变，确保现有测试全部通过
    - _Requirements: 10.4_

  - [x] 2.2 为 GameObjectResolveHelper 编写单元测试
    - 新建 `Tests/Editor/GameObjectResolveHelperTests.cs`
    - 测试 instanceID 定位、path 定位、instanceID 优先于 path、空参数错误、不存在的 GO 错误
    - _Requirements: 10.1, 10.2, 10.3_

  - [x] 2.3 为 ComponentTypeHelper 编写单元测试
    - 新建 `Tests/Editor/ComponentTypeHelperTests.cs`
    - 测试 FindType 大小写不敏感匹配、FindComponent 查找、未找到返回 null
    - _Requirements: 2.2, 4.2, 7.2, 9.10_

  - [x] 2.4 为 VectorParseHelper 编写单元测试
    - 新建 `Tests/Editor/VectorParseHelperTests.cs`
    - 测试 ParseVector2/3/4、ParseColor、ParseRect 正确解析，以及数组长度不足时抛异常
    - _Requirements: 8.8, 9.3_

  - [ ]* 2.5 属性测试：双模式定位等价性（Property 12）
    - 新建 `Tests/Editor/GameObjectResolveHelperPropertyTests.cs`
    - **Property 12: 双模式定位等价性**
    - 100 次随机迭代，验证 instanceID 和 path 定位到同一 GO
    - **Validates: Requirements 10.1**

  - [ ]* 2.6 属性测试：instanceID 优先于 path（Property 13）
    - 在 `Tests/Editor/GameObjectResolveHelperPropertyTests.cs` 中添加
    - **Property 13: instanceID 优先于 path**
    - 100 次随机迭代，同时提供不同 GO 的 instanceID 和 path，验证定位到 instanceID 对应的 GO
    - **Validates: Requirements 10.2**

  - [ ]* 2.7 属性测试：组件类型名大小写不敏感匹配（Property 1）
    - 新建 `Tests/Editor/ComponentTypeHelperPropertyTests.cs`
    - **Property 1: 组件类型名大小写不敏感匹配**
    - 100 次随机迭代，对已知组件类型名生成随机大小写变体，验证 FindType 返回相同 Type
    - **Validates: Requirements 2.2, 4.2, 7.2, 9.10**

- [x] 3. Checkpoint — 确保共享辅助类和重构完成
  - 确保所有测试通过，ask the user if questions arise.

- [x] 4. 实现 AddGameObjectTool 和 DeleteGameObjectTool
  - [x] 4.1 实现 AddGameObjectTool
    - 新建 `Editor/Tools/AddGameObjectTool.cs`
    - 实现 IMcpTool 接口，Name = "editor_addGameObject"，Category = "editor"
    - 支持 name（默认 "GameObject"）、parentInstanceID、parentPath 参数
    - Prefab Stage 中未指定父节点时挂载到 Prefab 根节点下
    - 使用 Undo.RegisterCreatedObjectUndo 记录操作
    - 返回 name、path、instanceID
    - try-catch 包裹核心逻辑
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8, 1.9_

  - [x] 4.2 实现 DeleteGameObjectTool
    - 新建 `Editor/Tools/DeleteGameObjectTool.cs`
    - 实现 IMcpTool 接口，Name = "editor_deleteGameObject"，Category = "editor"
    - 使用 GameObjectResolveHelper 定位 GO
    - 使用 Undo.DestroyObjectImmediate 删除 GO 及所有子对象
    - 返回被删除 GO 的 name、path
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [x] 4.3 为 AddGameObjectTool 编写单元测试
    - 新建 `Tests/Editor/AddGameObjectToolTests.cs`
    - 测试元数据、默认名称、指定父节点、无父节点创建到根级、父节点不存在返回错误
    - _Requirements: 1.1, 1.2, 1.3, 1.6, 1.8, 1.9_

  - [x] 4.4 为 DeleteGameObjectTool 编写单元测试
    - 新建 `Tests/Editor/DeleteGameObjectToolTests.cs`
    - 测试元数据、正常删除、删除含子对象的 GO、GO 不存在返回错误、无参数返回错误
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5_

  - [ ]* 4.5 属性测试：创建 GO 返回值与实际一致（Property 2）
    - 新建 `Tests/Editor/AddGameObjectToolPropertyTests.cs`
    - **Property 2: 创建 GO 返回值与实际一致**
    - 100 次随机迭代，验证返回 JSON 中的 name/path/instanceID 与场景中实际 GO 一致
    - **Validates: Requirements 1.1, 1.6**

  - [ ]* 4.6 属性测试：创建 GO 的父子关系正确（Property 3）
    - 在 `Tests/Editor/AddGameObjectToolPropertyTests.cs` 中添加
    - **Property 3: 创建 GO 的父子关系正确**
    - 100 次随机迭代，指定父节点创建 GO，验证 transform.parent 正确
    - **Validates: Requirements 1.2**

  - [ ]* 4.7 属性测试：删除 GO 及所有子对象（Property 4）
    - 新建 `Tests/Editor/DeleteGameObjectToolPropertyTests.cs`
    - **Property 4: 删除 GO 及所有子对象**
    - 100 次随机迭代，创建随机深度子层级 GO 后删除，验证所有后代引用变为 null
    - **Validates: Requirements 3.1**

- [x] 5. 实现 AddComponentTool 和 RemoveComponentTool
  - [x] 5.1 实现 AddComponentTool
    - 新建 `Editor/Tools/AddComponentTool.cs`
    - 使用 GameObjectResolveHelper 定位 GO，ComponentTypeHelper.FindType 查找类型
    - 使用 Undo.AddComponent 添加组件
    - 返回 componentType、name、path、instanceID
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7_

  - [x] 5.2 实现 RemoveComponentTool
    - 新建 `Editor/Tools/RemoveComponentTool.cs`
    - 使用 GameObjectResolveHelper 定位 GO，ComponentTypeHelper.FindComponent 查找组件
    - Transform/RectTransform 不可移除，返回错误
    - 使用 Undo.DestroyObjectImmediate 移除组件
    - 返回 componentType、name、path
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

  - [x] 5.3 为 AddComponentTool 编写单元测试
    - 新建 `Tests/Editor/AddComponentToolTests.cs`
    - 测试元数据、正常添加组件、componentType 缺失、类型未找到、GO 不存在
    - _Requirements: 2.1, 2.3, 2.5, 2.6, 2.7_

  - [x] 5.4 为 RemoveComponentTool 编写单元测试
    - 新建 `Tests/Editor/RemoveComponentToolTests.cs`
    - 测试元数据、正常移除、组件不存在、Transform 不可移除、GO 不存在
    - _Requirements: 4.1, 4.4, 4.5, 4.6, 4.7_

  - [ ]* 5.5 属性测试：Transform 组件不可移除（Property 5）
    - 新建 `Tests/Editor/RemoveComponentToolPropertyTests.cs`
    - **Property 5: Transform 组件不可移除**
    - 100 次随机迭代，尝试移除 Transform/RectTransform，验证始终返回错误且组件仍存在
    - **Validates: Requirements 4.6**

- [x] 6. 实现 ReparentGameObjectTool 和 SetActiveTool
  - [x] 6.1 实现 ReparentGameObjectTool
    - 新建 `Editor/Tools/ReparentGameObjectTool.cs`
    - 使用 GameObjectResolveHelper 定位目标 GO 和新父节点
    - 支持 worldPositionStays 参数（默认 true）
    - 新父节点为空时移到根级（Prefab Stage 中移到 Prefab 根节点下）
    - 使用 Undo.SetTransformParent 记录操作
    - 返回 name、path（新路径）、instanceID
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 5.6, 5.7, 5.8_

  - [x] 6.2 实现 SetActiveTool
    - 新建 `Editor/Tools/SetActiveTool.cs`
    - 使用 GameObjectResolveHelper 定位 GO
    - 使用 Undo.RecordObject 记录操作，调用 go.SetActive(active)
    - 返回 name、path、activeSelf
    - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_

  - [x] 6.3 为 ReparentGameObjectTool 编写单元测试
    - 新建 `Tests/Editor/ReparentGameObjectToolTests.cs`
    - 测试元数据、正常 reparent、移到根级、GO 不存在、新父节点不存在、worldPositionStays
    - _Requirements: 5.1, 5.2, 5.5, 5.6, 5.7, 5.8_

  - [x] 6.4 为 SetActiveTool 编写单元测试
    - 新建 `Tests/Editor/SetActiveToolTests.cs`
    - 测试元数据、设置 active=true/false、GO 不存在、active 参数缺失
    - _Requirements: 6.1, 6.3, 6.4, 6.5_

  - [ ]* 6.5 属性测试：Reparent 后父子关系正确（Property 6）
    - 新建 `Tests/Editor/ReparentGameObjectToolPropertyTests.cs`
    - **Property 6: Reparent 后父子关系正确**
    - 100 次随机迭代，验证 reparent 后 transform.parent 指向新父节点
    - **Validates: Requirements 5.1**

  - [ ]* 6.6 属性测试：worldPositionStays 保持世界坐标不变（Property 7）
    - 在 `Tests/Editor/ReparentGameObjectToolPropertyTests.cs` 中添加
    - **Property 7: worldPositionStays 保持世界坐标不变**
    - 100 次随机迭代，worldPositionStays=true 时验证世界坐标不变（浮点误差范围内）
    - **Validates: Requirements 5.8**

  - [ ]* 6.7 属性测试：SetActive 正确设置 activeSelf（Property 8）
    - 新建 `Tests/Editor/SetActiveToolPropertyTests.cs`
    - **Property 8: SetActive 正确设置 activeSelf**
    - 100 次随机迭代，验证 SetActive 后 activeSelf 等于指定值
    - **Validates: Requirements 6.1**

- [x] 7. Checkpoint — 确保前 6 个工具实现完成
  - 确保所有测试通过，ask the user if questions arise.

- [x] 8. 实现 SetComponentEnabledTool 和 SetTransformTool
  - [x] 8.1 实现 SetComponentEnabledTool
    - 新建 `Editor/Tools/SetComponentEnabledTool.cs`
    - 使用 GameObjectResolveHelper 定位 GO，ComponentTypeHelper.FindComponent 查找组件
    - 区分 Behaviour 和 Renderer 设置 enabled
    - 不支持 enabled 的组件返回错误
    - 使用 Undo.RecordObject 记录操作
    - 返回 componentType、name、path、enabled
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5, 7.6, 7.7_

  - [x] 8.2 实现 SetTransformTool
    - 新建 `Editor/Tools/SetTransformTool.cs`
    - 使用 GameObjectResolveHelper 定位 GO，VectorParseHelper 解析向量参数
    - 支持 localPosition、localRotation（欧拉角）、localScale
    - RectTransform 额外支持 anchoredPosition、sizeDelta、pivot、anchorMin、anchorMax
    - 非 RectTransform 传入 RT 参数时返回错误
    - 无属性参数时返回错误
    - 使用 Undo.RecordObject 记录操作
    - 返回 name、path 和各属性当前值
    - _Requirements: 8.1, 8.2, 8.3, 8.4, 8.5, 8.6, 8.7, 8.8_

  - [x] 8.3 为 SetComponentEnabledTool 编写单元测试
    - 新建 `Tests/Editor/SetComponentEnabledToolTests.cs`
    - 测试元数据、启用/禁用 Behaviour、启用/禁用 Renderer、不支持 enabled 的组件返回错误、GO 不存在
    - _Requirements: 7.1, 7.4, 7.5, 7.6, 7.7_

  - [x] 8.4 为 SetTransformTool 编写单元测试
    - 新建 `Tests/Editor/SetTransformToolTests.cs`
    - 测试元数据、设置 localPosition/localRotation/localScale、RectTransform 属性、无属性参数错误、非 RT 传入 RT 参数错误
    - _Requirements: 8.1, 8.2, 8.3, 8.5, 8.6, 8.7, 8.8_

  - [ ]* 8.5 属性测试：SetComponentEnabled 正确设置 enabled（Property 9）
    - 新建 `Tests/Editor/SetComponentEnabledToolPropertyTests.cs`
    - **Property 9: SetComponentEnabled 正确设置 enabled**
    - 100 次随机迭代，验证 Behaviour/Renderer 的 enabled 等于指定值
    - **Validates: Requirements 7.1**

  - [ ]* 8.6 属性测试：不支持 enabled 的组件返回错误（Property 10）
    - 在 `Tests/Editor/SetComponentEnabledToolPropertyTests.cs` 中添加
    - **Property 10: 不支持 enabled 的组件返回错误**
    - 100 次随机迭代，对 MeshFilter 等非 Behaviour/Renderer 组件调用，验证始终返回错误
    - **Validates: Requirements 7.6**

  - [ ]* 8.7 属性测试：Transform 属性设置后值一致（Property 11）
    - 新建 `Tests/Editor/SetTransformToolPropertyTests.cs`
    - **Property 11: Transform 属性设置后值一致**
    - 100 次随机迭代，设置 localPosition/localRotation/localScale 后验证值一致（浮点误差范围内）
    - **Validates: Requirements 8.1, 8.2, 8.3, 8.8**

- [x] 9. 实现 SetFieldTool
  - [x] 9.1 实现 SetFieldTool
    - 新建 `Editor/Tools/SetFieldTool.cs`
    - 使用 GameObjectResolveHelper 定位 GO，ComponentTypeHelper.FindComponent 查找组件
    - 使用 SerializedObject/SerializedProperty API 修改字段值
    - 实现 SetPropertyValue 分发：Integer、Boolean、Float、String、Enum（名称/索引）、Vector2/3/4、Color、Rect、ObjectReference
    - 使用 VectorParseHelper 解析向量/颜色/矩形值
    - ApplyModifiedProperties 自动支持 Undo
    - 返回 fieldName、fieldType、newValue
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5, 9.6, 9.7, 9.8, 9.9, 9.10_

  - [x] 9.2 为 SetFieldTool 编写单元测试
    - 新建 `Tests/Editor/SetFieldToolTests.cs`
    - 测试元数据、设置 Integer/Boolean/Float/String 字段、Enum 按名称和索引、Vector3 字段、组件不存在、字段不存在、值类型不匹配
    - _Requirements: 9.1, 9.3, 9.5, 9.7, 9.8, 9.9_

- [x] 10. 更新 ToolRegistry 集成测试
  - [x] 10.1 更新 ToolRegistryTests
    - 在 `Tests/Editor/ToolRegistryTests.cs` 的 `AutoDiscover_FindsAllExpectedTools` 中添加 9 个新工具名称的断言
    - 更新 `ListByCategory_Editor_ReturnsCorrectTools` 添加 9 个新工具的断言
    - 更新工具总数断言为 ≥ 26
    - _Requirements: 11.1, 11.2, 11.3, 11.5_

- [x] 11. Final Checkpoint — 确保所有测试通过
  - 确保所有测试通过，ask the user if questions arise.

## Notes

- 标记 `*` 的子任务为可选（仅属性测试），单元测试为必选（项目规范要求）
- 每个任务引用了具体的 Requirements 编号，确保可追溯性
- Checkpoint 任务确保增量验证
- 属性测试验证设计文档中定义的 Correctness Properties
- 单元测试验证具体用例和边界条件
