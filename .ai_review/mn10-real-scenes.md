# MN10 — 真实开发场景分析

> 日期：2026-04-18 | 技术栈：C# + xLua | 状态：Draft

---

## 场景总览

| # | 场景 | 触发频率 | 建议载体 |
|---|------|---------|---------|
| S1 | C# 改动 → 删 Gen → 编译 → xLua Generate → 编译 → 进游验证 | 每天多次 | **MCP 工作流** |
| S2 | Lua 改动 → 进游运行到改动逻辑 → 检查报错 | 每天多次 | **MCP 工作流** |
| S3 | 基于 Log 链条做逻辑推理、定位计算结果不符预期 | 每天 | **MCP 工作流** |
| S4 | 选中异常 Log，快捷复制本条 + 前 N 条上下文 | 每天（QA 高频） | **Unity Editor 插件** |

---

## S1 — C# + xLua 全量编译验证

### 用户操作流程（手动）

```
1. 修改 C# 代码
2. 删除 xLua Gen 目录（旧 Wrap 引用了已改变的 C# 签名）
3. 等待 Unity 自动编译（或手动 Refresh）
4. 检查是否有编译错误 → 有则修复 → 重复 3
5. 执行菜单 XLua > Generate Code
6. 等待编译
7. 检查是否有编译错误 → 有则修复 → 重复 5-6
8. 进入 PlayMode 运行游戏
9. 人工操作到相关功能
10. 检查 Console 有无运行时报错
11. 有报错 → 基于报错 + 前文 Log 排查
```

### MCP 自动化方案

```
[NEW] asset_deleteFolder("Assets/XLua/Gen")  // 必须走文件系统，见下方说明
  → build_compile
  → build_getCompileErrors
  → [若有错] 返回错误，等用户修
  → [若无错] menu_execute("XLua/Generate Code")  // 路径需确认
  → build_compile
  → build_getCompileErrors
  → [若有错] 返回错误，等用户修
  → [若无错] playmode_control(enter)
  → [等待] console_getLogs(level=Error)
  → [有报错] console_getLogs(count=N) + debug_getStackTrace → AI 分析
  → playmode_control(exit)
```

### 工具缺口

| 需求 | 现有能力 | 缺口 |
|------|---------|------|
| 删除 Gen 目录 | ~~`menu_execute` 触发 XLua > Clear~~ | **必须新增 `asset_deleteFolder`**（见备注） |
| 触发 xLua Generate | `menu_execute` | ✅ 可用（需知道菜单路径） |
| 编译 | `build_compile` | ✅ 已有 |
| 编译错误 | `build_getCompileErrors` | ✅ 已有 |
| 进游 | `playmode_control` | ✅ 已有 |
| 过滤 Error 日志 | `console_getLogs` | ⚠️ 需增强：加 `level` 过滤 |
| 堆栈 | `debug_getStackTrace` | ✅ 已有 |
| 综合 Log 分析 | `console_getLogs` | ⚠️ 需增强：获取「某条 Error 前 N 条」上下文 |

> **⚠️ 备注：为什么删目录不能用 `menu_execute`**
>
> 实测发现：当 C# 已存在编译错误时（典型场景：旧 Wrap 引用了已变更的 C# 签名），
> Unity 的 xLua 菜单项会被禁用或执行失败。而「删 Gen 目录」恰恰就发生在这个编译报错的时刻。
> 因此必须绕过 Unity 编辑器菜单，直接走文件系统操作删除目录，然后 `AssetDatabase.Refresh()`。

### 小结

> 核心链路靠已有工具基本可跑通。
> **P0 缺口**：`asset_deleteFolder`（文件系统删目录 + Refresh）。
> **P0 增强**：`console_getLogs` 加过滤 + 上下文模式。

---

## S2 — Lua 改动后运行验证

### 用户操作流程（手动）

```
1. 修改 Lua 文件（外部编辑器）
2. 进入 PlayMode（Lua 不需要 C# 编译）
3. 人工操作运行到改动逻辑
4. 观察 Console 有无报错
5. 有报错 → 基于报错内容 + 前文 Log 排查
```

### MCP 自动化方案

```
playmode_control(enter)
  → [用户手动操作到目标逻辑]
  → playmode_control(status) → [若 Playing 且未暂停] playmode_control(pause)
  → console_getLogs(level=Error)
  → [有报错] console_getLogContext(errorIndex, before=10) + debug_getStackTrace
  → AI 分析 Lua 报错 + 上下文 Log
```

### 关键点

- Lua 报错走 `Debug.LogError`，堆栈里是 xLua 的 C# 调用栈，**Lua 侧堆栈通常在 message 文本里**
- AI 需要能解析 Lua 堆栈格式（`[string "xxx.lua"]:line: message`）
- 最大价值在于：**AI 能结合报错前的若干条 Log 推断执行路径**

### 工具缺口

| 需求 | 缺口 |
|------|------|
| 获取某条 Error 前的 N 条 Log（上下文） | `console_getLogs` 增强 或 新增 `console_getLogContext` |

### 小结

> 工具层面几乎不缺，核心增强点是 **Log 上下文获取**。
> AI 层面需要能识别 xLua 的 Lua 堆栈格式。

---

## S3 — 基于 Log 链条的逻辑推理

### 用户操作流程（手动）

```
1. 看到某条 Log 输出的计算结果和预期不符
2. 往上翻 Console，找到同一逻辑流程中前面打印的 Log
3. 逐条比对：哪个分支被触发了？中间变量值是否正确？
4. 定位到第一个「值不对」的 Log → 缩小 Bug 范围
```

### MCP 自动化方案

```
playmode_control(status) → [若 Playing 且未暂停] playmode_control(pause)
  → console_getLogs(keyword="目标关键字", count=30)
  → AI 分析 Log 链条：
    - 识别逻辑分支标记（如 "[BattleCalc]", "[Damage]" 等前缀）
    - 追踪数值变化
    - 定位第一个偏离预期的节点
  → 给出结论：「第 N 条 Log 显示 XXX 值为 YY，预期应为 ZZ，可能是 ... 导致」
```

### 工具缺口

| 需求 | 缺口 |
|------|------|
| 按关键字过滤 Log | `console_getLogs` 增强：加 `keyword` 参数 |
| 大量 Log 获取 | `console_getLogs` 增强：`count` 上限可能需要调大 |

### 小结

> 工具侧改动极小（getLogs 加过滤）。
> **真正的价值在 AI 推理层**：给 AI 一串带前缀的 Log，让它做逻辑链分析。
> 这是 MCP + LLM 最天然的结合点 — 人翻 Log 慢，AI 扫 Log 快。

---

## S4 — QA 快捷复制 Log 上下文

### 用户操作流程（手动）

```
1. QA 在 Console 中看到一条异常 Log
2. 想复制这条 Log + 前面 5/10/15/20 条作为 Bug 描述
3. 目前只能手动一条条选、复制、粘贴
```

### 分析：为什么这个应该做成 Unity Editor 插件而非 MCP

| 维度 | MCP | Unity 插件 |
|------|-----|-----------|
| 交互方式 | 需要切到 AI 对话窗口输入指令 | Console 窗口右键菜单，一键操作 |
| 操作延迟 | 走 HTTP 请求 + AI 处理 | 毫秒级本地操作 |
| 目标用户 | 开发（懂 AI 工具） | QA（要求零学习成本） |
| 核心需求 | 不需要 AI 理解，纯机械操作 | 纯 UI 交互 + 剪贴板 |

**结论：这是一个纯 Editor 插件需求，不适合走 MCP。**

### 插件设计要点

```
功能：Console 窗口扩展
- 右键选中的 Log 条目 → 弹出菜单：
  - "复制本条 + 前 5 条"
  - "复制本条 + 前 10 条"
  - "复制本条 + 前 20 条"
- 复制格式：
  [时间] [Level] 消息内容
  [时间] [Level] 消息内容  ← 选中的这条
- 直接写入系统剪贴板

技术路线：
- 方案 A：Hook Console Window（反射访问内部 API，版本兼容性差）
- 方案 B：独立的 Log Viewer 窗口（EditorWindow），
  监听 Application.logMessageReceived，自带右键菜单
  → 推荐此方案，稳定可控
```

---

## 汇总：需要做什么

### MCP 侧（服务 S1 / S2 / S3）

| 优先级 | 改动 | 类型 | 难度 |
|--------|------|------|------|
| **P0** | `asset_deleteFolder(path)` 删除指定目录 + AssetDatabase.Refresh | 新增 | 低 |
| **P0** | `console_getLogs` 增加 `level` 过滤参数 | 增强现有 | 极低 |
| **P0** | `console_getLogs` 增加 `keyword` 过滤参数 | 增强现有 | 极低 |
| **P0** | `console_getLogs` 增加 `beforeIndex` / 上下文模式 | 增强现有 | 低 |
| P1 | `console_clearLogs` 清空日志缓冲区 | 新增 | 极低 |
| P1 | 确认 xLua 菜单路径，文档化 `menu_execute` 用法 | 文档 | — |

> P0 改动涉及 **两个文件**：新增 `AssetDeleteFolderTool.cs` + 增强 `ConsoleTool.cs`。

### Unity 插件侧（服务 S4）

| 改动 | 类型 | 难度 |
|------|------|------|
| Log Viewer 窗口 + 右键复制上下文 | 独立 EditorWindow | 中 |

> 与 MCP 项目解耦，可独立开发。

### AI Prompt 层（服务 S1 / S2 / S3 的编排）

| 工作流 | 需要的 Prompt 编排 |
|--------|-------------------|
| S1 - xLua 编译验证 | 串联 delete → compile → menu → compile → play → check 的多步骤流程 |
| S2 - Lua 运行验证 | play → 等待用户操作 → 主动 check error 的交互式流程 |
| S3 - Log 逻辑推理 | 给 AI 一批 Log，让它做链式分析的 system prompt |

---

## 附录：日志缓冲区策略

### 现状

当前 `ConsoleTool` 维护了 **1000 条**环形缓冲区（`MaxBufferSize = 1000`）。
高频打 Log 的游戏模块（如每帧输出的战斗计算日志）可能十几秒就刷满。

### 风险

报错发生 → 用户发现 → 让 AI 查，这段窗口期内后续 Log 可能把报错及其上下文挤出缓冲区。

### 应对方案

| 措施 | 效果 | 优先级 |
|------|------|--------|
| **AI 兜底暂停** — AI 分析日志前先检测 PlayMode 状态，若 Playing 且未暂停则自动 pause（用户通常已手动暂停，此为兜底） | 阻止分析过程中新 Log 继续冲刷 | **P0**（Prompt 工作流层面，零工具成本） |
| **缓冲区扩容** — `MaxBufferSize` 从 1000 提高到 5000 | 拉长「报错到 AI 介入」的安全窗口 | **P0**（改一行代码） |
| Error 自动快照 — 每次 Error/Exception 时自动保存「Error + 前 50 条」到独立快照列表（保留最近 10 个） | 彻底消除窗口期风险，报错现场永不丢失 | P2（优化项） |

> **结论**：P0 阶段做「前序暂停 + 扩容」即可覆盖绝大多数场景，Error 快照作为后续优化。
