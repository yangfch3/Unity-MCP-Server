# Unity-MCP 高价值自动化工作流 — 脑暴

> 日期：2026-04-18 | 状态：Draft

---

## 现有工具速查

| 工具 | 分类 | 功能 |
|------|------|------|
| `console_getLogs` | debug | 获取最近 N 条日志 |
| `debug_getStackTrace` | debug | 获取最近一条 Error/Exception 堆栈 |
| `debug_getPerformanceStats` | debug | FPS / DrawCall / 内存 |
| `debug_screenshot` | debug | 截图（base64 PNG） |
| `menu_execute` | editor | 执行菜单项 |
| `playmode_control` | editor | 进入/退出/查询 PlayMode |
| `editor_getSelection` | editor | 获取当前选中对象信息 |
| `editor_getHierarchy` | editor | 场景 GO 树 |
| `editor_getProjectStructure` | editor | Assets 目录结构 |
| `editor_getInspector` | editor | 选中对象 Inspector 字段 |
| `build_compile` | build | 触发编译 |
| `build_getCompileErrors` | build | 编译错误列表 |
| `build_runTests` | build | 运行 Test Runner |

---

## 工作流 A — Bug 诊断助手

**场景**：QA 报 Bug → 开发在 Unity 复现 → Console 一堆日志，手动来回点 Hierarchy/Inspector 定位问题。

### 流程

```
console_getLogs(过滤 Error)
  → debug_getStackTrace
  → 从堆栈中解析目标对象名
  → [NEW] editor_findGameObject(name/path)
  → [NEW] editor_setSelection(instanceId/path)
  → editor_getInspector
  → AI 给出诊断结论 & 修复建议
```

### 已有 / 缺口

| 步骤 | 工具 | 状态 |
|------|------|------|
| 读日志 | `console_getLogs` | ✅ 已有，需增强：加 `level` / `keyword` 过滤参数 |
| 读堆栈 | `debug_getStackTrace` | ✅ 已有 |
| 按名称/路径查找 GO | `editor_findGameObject` | ❌ 新增 |
| 选中对象 | `editor_setSelection` | ❌ 新增 |
| 读 Inspector | `editor_getInspector` | ✅ 已有 |

### 价值评估

- **日常频次**：极高（每天）
- **痛点**：手动串联 日志→堆栈→对象→字段 信息链，来回切换窗口
- **AI 附加价值**：面对不熟悉的模块，AI 做第一轮排查，给出初步结论

---

## 工作流 B — 改完代码快速验证

**场景**：改了代码，想一步到位确认：编译是否通过、运行有没有报错、画面效果对不对。

### 流程

```
build_compile
  → build_getCompileErrors
  → [若无错] playmode_control(enter)
  → [等待稳定] console_getLogs(level=Error)
  → debug_screenshot
  → playmode_control(exit)
  → AI 汇总：编译结果 + 运行错误 + 截图
```

### 已有 / 缺口

| 步骤 | 工具 | 状态 |
|------|------|------|
| 编译 | `build_compile` | ✅ 已有 |
| 编译错误 | `build_getCompileErrors` | ✅ 已有 |
| PlayMode | `playmode_control` | ✅ 已有 |
| 读日志(过滤) | `console_getLogs` | ✅ 已有，需增强：加 `level` 过滤 |
| 截图 | `debug_screenshot` | ✅ 已有 |

> 本工作流 **0 个新工具**，仅需对 `console_getLogs` 做小幅增强。
> 价值主要由 AI 层的 **Prompt Workflow** 编排体现。

### 价值评估

- **日常频次**：极高（每天多次）
- **痛点**：手动「编译→点 Play→看 Console→截图→停止」5 步，经常忘记看 Console 就关了
- **AI 附加价值**：自动串联全流程，不遗漏错误

---

## 工作流 C — 场景 / Prefab 变更巡检

**场景**：合了别人的代码，场景或 Prefab 文件有变更，需要确认没改坏东西。

> 注意：部分项目是**单场景 + 多 Prefab** 架构，需要同时覆盖两种粒度。

### C-1：场景变更巡检

```
[NEW] editor_openScene(path)
  → editor_getHierarchy(depth=全量)
  → AI 遍历：检查 MissingScript / 空引用 / 意外删除的节点
  → [抽查] editor_setSelection + editor_getInspector
  → 输出巡检报告
```

### C-2：Prefab 变更巡检

```
[NEW] prefab_getHierarchy(assetPath)   // 不需要打开场景，直接读 Prefab 资源
  → AI 遍历：检查组件完整性 / 字段空引用 / 命名规范
  → 输出巡检报告
```

### 已有 / 缺口

| 步骤 | 工具 | 状态 |
|------|------|------|
| 打开场景 | `editor_openScene` | ❌ 新增 |
| 读场景树 | `editor_getHierarchy` | ✅ 已有 |
| 读 Prefab 结构 | `prefab_getHierarchy` | ❌ 新增（或复用 getHierarchy 加 assetPath 参数） |
| 选中对象 | `editor_setSelection` | ❌ 新增（同工作流 A） |
| 读 Inspector | `editor_getInspector` | ✅ 已有 |

### 价值评估

- **日常频次**：中（每次合代码后）
- **痛点**：人工逐节点检查极其枯燥，容易遗漏
- **AI 附加价值**：自动化巡检 + 规则化检查（MissingScript、空引用等）

---

## 工作流 D — 运行时代码注入（GM 命令）

**场景**：PlayMode 运行中，想注入一段代码快速调试。例如：
- 给玩家加 10000 金币
- 传送到指定坐标
- 切换到指定关卡
- 打印某个 Manager 的内部状态

### 流程

```
playmode_control(status)  // 确认在 PlayMode
  → [NEW] runtime_executeCode(codeSnippet)
  → 返回执行结果（返回值 / 异常信息）
  → console_getLogs  // 可选：捕获执行产生的日志
```

### 已有 / 缺口

| 步骤 | 工具 | 状态 |
|------|------|------|
| 查询 PlayMode | `playmode_control` | ✅ 已有 |
| 执行代码片段 | `runtime_executeCode` | ❌ 新增（核心） |
| 读日志 | `console_getLogs` | ✅ 已有 |

### 实现思路

1. **编译方式**：通过 `System.Reflection.Emit` 或 `Microsoft.CSharp.CSharpCodeProvider` 在 Editor 进程内动态编译并执行
2. **安全边界**：仅限 Editor + PlayMode 下可用；非 PlayMode 拒绝执行
3. **返回值**：将表达式最后一行的返回值序列化为 JSON 返回
4. **超时**：设置执行超时（如 5s），防止死循环卡死 Editor

### 价值评估

- **日常频次**：中高（调试阶段每天多次）
- **痛点**：传统方式需要写 GM 面板或 Console 命令系统，项目早期往往没有
- **AI 附加价值**：万能后门，一个工具替代大量专用调试工具；AI 可以根据用户自然语言意图生成代码片段

---

## 工作流 E — 性能回归监控

**场景**：版本迭代后帧率下降 / 内存上涨，需要快速定位是否是本次提交引起的。

### 流程

```
playmode_control(enter)
  → [等几秒稳定]
  → debug_getPerformanceStats  // 采样 1
  → [等 N 秒]
  → debug_getPerformanceStats  // 采样 2
  → debug_screenshot
  → playmode_control(exit)
  → AI 对比两次采样 + 与历史基线对比 → 输出结论
```

### 已有 / 缺口

| 步骤 | 工具 | 状态 |
|------|------|------|
| PlayMode | `playmode_control` | ✅ 已有 |
| 性能数据 | `debug_getPerformanceStats` | ✅ 已有，可增强：加 `triangles` / `textureMem` 等指标 |
| 截图 | `debug_screenshot` | ✅ 已有 |

> 本工作流 **0 个新工具**。增强点在 AI 层：多次采样 + 基线对比逻辑。

### 价值评估

- **日常频次**：中低（每周 / 每个里程碑）
- **痛点**：手动对比 Profiler 数据费时
- **AI 附加价值**：自动化采样 + 结构化对比报告

---

## 新增工具汇总 & 优先级

| 优先级 | 工具 | 服务的工作流 | 实现难度 |
|--------|------|-------------|---------|
| P0 | `editor_setSelection(instanceId/path)` | A, C | 低 |
| P0 | `editor_findGameObject(name/path/tag)` | A, C | 低 |
| P0 | `console_getLogs` 增加 `level` / `keyword` 过滤 | A, B | 极低（改现有） |
| P1 | `runtime_executeCode(code)` | D | 高 |
| P1 | `editor_openScene(path)` | C | 低 |
| P1 | `prefab_getHierarchy(assetPath)` | C | 中 |
| P2 | `debug_getPerformanceStats` 增加更多指标 | E | 低 |

### 实现顺序建议

```
Phase 1（补齐诊断闭环）:
  console_getLogs 增强 → editor_findGameObject → editor_setSelection
  → 即可跑通 工作流 A + B

Phase 2（巡检 + GM）:
  editor_openScene → prefab_getHierarchy → runtime_executeCode
  → 即可跑通 工作流 C + D

Phase 3（性能增强）:
  debug_getPerformanceStats 指标扩展
  → 工作流 E 完善
```
