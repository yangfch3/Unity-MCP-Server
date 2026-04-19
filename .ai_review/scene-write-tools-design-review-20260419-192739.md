# scene-write-tools Design — 技术需求评审报告

> **评审日期**：2026-04-19
> **评审文档**：`.kiro/specs/scene-write-tools/design.md`

---

## 1. Checklist 评审

### 1.1 结构完整性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 1.1 | 背景/目标/动机 | ✅ | Overview 清晰说明了"补全当前仅有读与查类工具的能力缺口"，动机明确 |
| 1.2 | 功能范围（Scope） | ✅ | 9 个工具逐一列出，参数/行为/返回值均有描述 |
| 1.3 | 边界（Out of Scope） | ⚠️ | 未明确列出 Out of Scope。例如：是否支持批量操作（一次创建多个 GO）？是否支持 Prefab Override 操作？是否支持 AssetDatabase 层面的 Prefab 保存？这些是读者可能产生的合理疑问 |
| 1.4 | 术语/缩写定义 | ✅ | requirements.md Glossary 已覆盖所有专有术语（Target_GO、Undo_Record 等），设计文档面向内部技术人员，无额外缺失 |

### 1.2 功能描述清晰度

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 2.1 | 无歧义性 | ⚠️ | 两处需要澄清：(1) SetTransformTool 对非 RectTransform 的 GO 传入 anchoredPosition 等 RectTransform 专属参数时的行为未定义——是静默忽略还是返回错误？(2) SetFieldTool 的 `value` 参数为 `any` 类型，Enum 字段同时支持"按名称或索引"，但设计文档伪代码中 `SetPropertyValue` 的分发逻辑仅用一行概括，对这个最复杂的工具描述不够 |
| 2.2 | 输入/输出/行为 | ✅ | 每个工具均有参数表 + Execute 伪代码 + 返回字段说明。向量参数的数组格式 `[x, y, z]` 也有明确定义 |
| 2.3 | 异常/边界场景 | ✅ | Error Handling 表覆盖了 12 种错误场景，涵盖定位失败、类型未找到、Transform 不可移除、组件不支持 enabled、值类型不匹配等 |
| 2.4 | 优先级区分 | ✅ | 该场景下无需关注——9 个工具均为本次必须实现项，无可选功能 |

### 1.3 技术可行性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 3.1 | 架构兼容性 | ✅ | 完全遵循现有 `IMcpTool` + ToolRegistry 反射自动发现机制。共享 Helper 使用静态类模式，与项目中已有的 `GameObjectPathHelper` 风格一致 |
| 3.2 | 风险与依赖 | ⚠️ | `ComponentTypeHelper.FindType` 需要遍历 `AppDomain.CurrentDomain.GetAssemblies()` 所有程序集。在大型项目中程序集数量可能很多（100+），每次调用都全量遍历可能有性能隐患。未讨论是否需要缓存。另外，`AppDomain.GetAssemblies()` 在 Domain Reload 后结果会变化，如果加缓存需要考虑失效时机 |
| 3.3 | 性能指标 | ✅ | 该场景下无需量化性能指标——写操作工具是单次执行、非批量场景，性能不是核心关注点 |
| 3.4 | 安全性 | ✅ | 所有写操作通过 Undo API 记录，用户可撤销。Editor-only 工具不影响运行时构建 |
| 3.5 | 框架影响 | ✅ | 不修改任何现有核心代码，纯新增。需要更新 ToolRegistryTests 的断言数量，但这是预期的 |
| 3.6 | 可扩展性 | ✅ | 共享 Helper 的设计（GameObjectResolveHelper、ComponentTypeHelper）使未来新增写操作工具只需复用已有基础设施 |

### 1.4 一致性与关联性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 4.1 | 内部自洽 | ⚠️ | `GameObjectResolveHelper` 设计中说"复用 SelectGameObjectTool 中已有的 FindByPath 逻辑"，但 `SelectGameObjectTool.FindByPath` 和 `SearchInRoot` 均为 `private static`。设计文档未说明如何提取——是将这些方法移到 `GameObjectResolveHelper`，还是在 Helper 中重新实现？这会影响 SelectGameObjectTool 是否需要修改 |
| 4.2 | 外部一致 | ✅ | 与 requirements.md 的 12 个 Requirement 逐一对应。13 个 Correctness Properties 均标注了对应的 Requirement 编号 |
| 4.3 | 接口引用 | ✅ | 明确列出了 IMcpTool、ToolResult、MiniJson、GameObjectPathHelper、Undo API、SerializedObject/SerializedProperty 等依赖项。两个 Mermaid 图清晰展示了工具与共享类的依赖关系 |

### 1.5 可测试性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 5.1 | 可转化为测试用例 | ✅ | 13 个 Correctness Properties 均可直接映射为测试用例。测试文件规划详细，每个工具一个测试文件 + 属性测试文件 |
| 5.2 | 验收标准 | ✅ | Correctness Properties 与 requirements.md 的 Acceptance Criteria 形成完整验收链路 |

### 1.6 可维护性与可读性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 6.1 | 组织结构 | ✅ | Overview → Architecture（含设计决策） → Components（含伪代码） → Data Models → Correctness Properties → Error Handling → Testing Strategy，结构完整 |
| 6.2 | 图表辅助 | ✅ | 两个 Mermaid 图分别展示整体架构和工具-Helper 依赖关系，有效辅助理解 |

### 统计摘要

| 类型 | 数量 |
|------|------|
| ✅ 通过 | 15 |
| ⚠️ 待改进 | 4 |
| ❌ 缺失/不合格 | 0 |

---

## 2. 自由评审意见

1. **SelectGameObjectTool 的 FindByPath/SearchInRoot 提取问题**。设计文档说 `GameObjectResolveHelper` 要"复用 SelectGameObjectTool 中已有的 FindByPath 逻辑"，但现有代码中 `FindByPath` 和 `SearchInRoot` 都是 `private static`。实现时有两条路：(a) 将它们移到 `GameObjectResolveHelper` 中，然后 `SelectGameObjectTool` 改为调用 Helper——但这与"不修改任何现有核心代码"的设计原则矛盾；(b) 在 Helper 中重新实现一份——但这引入逻辑重复。建议设计文档明确选择，并在 Overview 的"核心设计决策"中说明。如果选 (a)，需要将 `SelectGameObjectTool` 的变更纳入本次 scope。

2. **SetFieldTool 是 9 个工具中复杂度最高的**，需要处理 11 种 SerializedPropertyType 的写入分发。设计文档中仅用一行 `SetPropertyValue(prop, params["value"])` 概括。建议补充 `SetPropertyValue` 的分发逻辑伪代码，至少覆盖几种非平凡类型的处理方式（如 Enum 按名称/索引、ObjectReference 按 instanceID、Vector/Color 的数组解析）。

3. **向量解析方法的位置决策值得商榷**。设计说 `ParseVector2/ParseVector3` 放在 `SetTransformTool` 内部作为 private 方法，`SetFieldTool` 独立实现自己的版本。但 `SetFieldTool` 也需要处理 Vector2/Vector3/Vector4，两处独立实现相同的解析逻辑会导致重复。建议将向量解析方法提取到一个共享位置（如 `GameObjectResolveHelper` 的同级 Helper，或一个轻量的 `VectorParseHelper`）。

4. **AddGameObjectTool 的 Prefab Stage 默认行为设计合理**。在 Prefab Stage 中未指定父节点时挂载到 Prefab 根节点下（而非创建为"孤立"的场景根级对象），符合 Prefab 编辑的实际语义。这个设计决策与 Requirement 1.4 一致。

5. **Undo API 选择覆盖全面**。9 个工具分别使用了 `RegisterCreatedObjectUndo`、`DestroyObjectImmediate`、`RecordObject`、`SetTransformParent`、`AddComponent`（自动 Undo）、`ApplyModifiedProperties`（自动 Undo）等 API，每种写操作场景都选择了最合适的 Undo 记录方式。

6. **缺少 AddGameObjectTool 在 Prefab Stage 中 Undo 的边界讨论**。`Undo.RegisterCreatedObjectUndo` 在 Prefab Stage 中的行为与 Active Scene 中可能有差异（Prefab Stage 有自己的 Undo 栈）。虽然 Unity 内部应该处理了这个差异，但设计文档未提及这一点。如果实现时遇到问题，这可能成为一个调试盲点。

---

## 3. 改进建议

### 🔴 关键问题（必须修改）

（无）

### 🟡 建议改进（推荐修改）

1. **明确 FindByPath 的提取策略** — 在 Architecture 的"设计决策"中新增一条，说明 `GameObjectResolveHelper.FindByPath` 与 `SelectGameObjectTool.FindByPath` 的关系。推荐将 `FindByPath` 和 `SearchInRoot` 提取到 `GameObjectResolveHelper` 中，然后让 `SelectGameObjectTool` 调用 Helper。这需要在 Overview 中明确"本次会小幅重构 SelectGameObjectTool 的内部实现（行为不变）"。

2. **补充 SetFieldTool.SetPropertyValue 的分发伪代码** — 至少列出 Enum（按名称查找 → 设置 enumValueIndex）、ObjectReference（按 instanceID 查找 Object → 设置 objectReferenceValue）、Vector/Color（解析数组 → 设置各分量）三种非平凡类型的处理流程。

3. **明确 SetTransformTool 对非 RectTransform GO 传入 RectTransform 专属参数的行为** — 建议在 Error Handling 表中增加一行：当 GO 无 RectTransform 组件但传入了 anchoredPosition/sizeDelta 等参数时，静默忽略这些参数（只处理 localPosition/localRotation/localScale），或者返回警告。需要选择并明确写出。

4. **统一向量解析方法的位置** — 将 `ParseVector2`/`ParseVector3`（以及 `SetFieldTool` 需要的 `ParseVector4`/`ParseColor`）提取为共享 internal static 方法，避免 `SetTransformTool` 和 `SetFieldTool` 各自实现一套。

### 🟢 锦上添花（可选优化）

1. **补充 Out of Scope 段落** — 在 Overview 之后增加明确边界：不支持批量操作、不支持 Prefab Override/保存、不支持 AssetDatabase 操作（如创建 Prefab Asset）。

2. **讨论 ComponentTypeHelper.FindType 的缓存策略** — 在 Components and Interfaces 的 `ComponentTypeHelper` 描述中加一句：首个版本不做缓存，如果性能分析表明 `AppDomain.GetAssemblies()` 遍历成为瓶颈，后续可增加 `Dictionary<string, Type>` 缓存（Domain Reload 时清空）。
