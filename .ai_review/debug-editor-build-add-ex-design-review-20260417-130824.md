# debug-editor-build-add-ex-design — 技术需求评审报告

> **评审日期**：2026-04-17
> **评审文档**：`.kiro/specs/debug-editor-build-add-ex/design.md`
> **文档定位**：在已有 Unity MCP 插件上的功能扩展，新增 10 个工具，正式功能迭代需求，目标读者为项目组技术人员

---

## 1. Checklist 评审

### 1.1 结构完整性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 1.1 | 背景/目标/动机 | ⚠️ | Overview 说明了"扩展 10 个新工具"和设计决策，但未说明**为什么**需要这 10 个工具——是用户反馈？Agent 能力不足？缺少动机描述 |
| 1.2 | 功能范围（Scope） | ✅ | 10 个工具清晰列出，分为 debug/editor/build 三类，文件路径和命名都明确 |
| 1.3 | 边界（Out of Scope） | ❌ | 缺失。10 个工具的选择标准是什么？为什么没有其他候选工具（如 AssetBundle、Lighting、Animation 相关）？未说明边界 |
| 1.4 | 术语/缩写定义 | ✅ | IMcpTool、ToolRegistry、MainThreadQueue 等均为项目内已有概念，目标读者已知 |

### 1.2 功能描述清晰度

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 2.1 | 无歧义性 | ⚠️ | 大部分工具描述清晰，但 CompileErrorsTool（第 9 项）的实现方案写了多个备选（GetSystemAssemblyPaths / CodeEditor / 内部 API / 事件缓存），未做决定 |
| 2.2 | 输入/输出/行为 | ✅ | 每个工具都有 InputSchema、Execute 伪代码、Data Models JSON 示例 |
| 2.3 | 异常/边界场景 | ✅ | Error Handling 表格覆盖了 11 种场景，每个工具的异常路径都有说明 |
| 2.4 | 优先级区分 | ❌ | 10 个工具一视同仁，未区分交付优先级。实际上 debug 类工具（错误日志、堆栈）可能比 editor 类工具（项目结构）优先级更高 |

### 1.3 技术可行性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 3.1 | 架构兼容性 | ✅ | 明确声明"零修改核心代码"，复用已有 IMcpTool 接口和基础设施 |
| 3.2 | 风险与依赖 | ⚠️ | ScreenshotTool 依赖 `InternalEditorUtility.ReadScreenPixel`（内部 API，可能跨版本不稳定）但未识别该风险。CompileErrorsTool 的实现方案不确定本身就是风险 |
| 3.3 | 性能指标 | ✅ | 该场景下无需关注——本地 Editor 工具，单 Agent 调用 |
| 3.4 | 安全性 | ✅ | 该场景下无需关注——本地 Editor 插件 |
| 3.5 | 框架影响 | ⚠️ | ContentItem 扩展（新增 Data/MimeType 字段）和 JsonRpcDispatcher 响应序列化修改，与"零修改核心代码"的声明矛盾。这是对已有核心组件的改动，应明确承认并评估影响 |
| 3.6 | 可扩展性 | ✅ | 基于 IMcpTool 接口，扩展模式与已有工具一致 |

### 1.4 一致性与关联性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 4.1 | 内部自洽 | ❌ | Overview 声明"零修改核心代码"，但 ContentItem 扩展章节明确要求修改 ContentItem 类和 JsonRpcDispatcher 的序列化逻辑，前后矛盾 |
| 4.2 | 外部一致 | ✅ | 与已有 design.md（unity-mcp-minimum-experiment）的架构、接口设计一致 |
| 4.3 | 接口引用 | ✅ | 每个工具明确列出了 Unity API 依赖 |

### 1.5 可测试性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 5.1 | 可转化为测试用例 | ✅ | 4 个 Correctness Properties + 每工具测试场景列表 |
| 5.2 | 验收标准 | ⚠️ | Testing Strategy 列出了测试覆盖点，但缺少业务级验收标准（如"Agent 通过 MCP 协议成功调用全部 10 个新工具"） |

### 1.6 可维护性与可读性

| # | 检查项 | 结果 | 说明 |
|---|--------|:----:|------|
| 6.1 | 组织结构 | ✅ | 按组件逐个描述，结构清晰一致 |
| 6.2 | 图表辅助 | ✅ | 目录树结构图 + JSON 示例足够辅助理解，10 个工具的模式相同不需要额外架构图 |

### 统计摘要

| 类型 | 数量 |
|------|------|
| ✅ 通过 | 12 |
| ⚠️ 待改进 | 5 |
| ❌ 缺失/不合格 | 2 |

---

## 2. 自由评审意见

- Overview 声明"零修改核心代码"，但 ScreenshotTool 需要扩展 ContentItem 和修改 JsonRpcDispatcher。这个矛盾需要正视——要么修改声明，要么把 ContentItem 扩展设计为不侵入已有代码的方案（如子类/新类型）。
- CompileErrorsTool 的伪代码写了 4 种备选方案并加了注释说"实际方案：监听事件缓存"，但前面 3 种没删掉，读起来像是还没决定。如果已决定用事件缓存方案，应删除其他备选，减少歧义。
- ConsoleTool 改名（console_getLogs → debug_getLogs）在架构目录树中提到但没有单独说明，这是一个**破坏性变更**（Agent 端已有的 tool name 会失效），需要显式说明影响和迁移策略。

---

## 3. 改进建议

### 🔴 关键问题（必须修改）

1. **解决"零修改核心代码"与 ContentItem 扩展的矛盾** — 修改 Overview 的声明，明确说"除 ContentItem 扩展外零修改核心代码"；或将 image 类型支持设计为不修改已有 ContentItem 的方案。
2. **明确 CompileErrorsTool 的实现方案** — 删除已排除的备选方案，只保留最终确定的方案描述。

### 🟡 建议改进（推荐修改）

1. **补充 Out of Scope** — 说明为什么选择这 10 个工具、哪些候选工具不在本次范围内。
2. **说明 ConsoleTool 改名的影响** — 这是 breaking change，需要明确影响范围和迁移方式（如是否同时支持旧名称？）。
3. **补充背景动机** — 简要说明为什么需要这批工具扩展（Agent 使用反馈？能力缺口？）。
4. **识别 ScreenshotTool 的 API 风险** — `InternalEditorUtility.ReadScreenPixel` 是非公开 API，标注跨 Unity 版本的兼容性风险和备选方案。
5. **区分工具优先级** — 10 个工具的交付顺序或 Must / Nice to have。
6. **添加业务验收标准** — 如"Agent 能成功调用全部 10 个新工具并获得符合 Data Models 描述的响应"。
