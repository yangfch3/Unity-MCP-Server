---
name: ui-click-debug
description: UI 点击问题排查。当用户说「UI 无法点击」「按钮点不了」「点击没反应」「排查点击」或类似表达时触发。优先使用运行时射线检测，一次调用定位遮挡问题。
---

# UI 点击问题排查

## 核心策略

**优先执行运行时射线检测**，一次 MCP 调用定位问题，避免逐层 Inspector 排查。

## 执行流程

### Step 1: 获取目标节点

调用 `editor_getSelection` 获取用户选中的 UI 节点路径。

### Step 2: 编写并执行射线检测代码

使用 `code_executeImmediate` 执行检测代码。**需结合项目仓库和 PlayMode 实时情况即时编写**，伪代码逻辑：

```
1. 通过路径/名称找到目标 GameObject
2. 获取其 RectTransform 世界坐标 → 转换为屏幕坐标
3. 构造 PointerEventData，调用 EventSystem.RaycastAll
4. 遍历命中结果，输出每个对象的：
   - 完整路径
   - sortingOrder / depth
   - 是否为目标节点
5. 判断 results[0] 是否为目标，若不是则输出遮挡警告
```

### Step 3: 分析日志结果

- **`[0]` 不是目标** → 被遮挡，报告遮挡者
- **目标不在列表** → 检查 RaycastTarget、CanvasGroup、激活状态
- **目标是 `[0]`** → 检查 Button.interactable、事件绑定

### Step 4: 输出结论

1. **问题原因**：一句话
2. **遮挡者**：路径 + sortingOrder（如适用）
3. **修复建议**：一句话