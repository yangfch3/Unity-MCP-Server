---
name: spec-post-check
description: "Spec 后处理流程：在一轮 Spec Coding 结束后，检查并同步本次改动涉及的 Spec 文档以及 Steering 文档。当用户说出「Spec 后处理」「后处理流程」「spec post check」「收尾检查」或类似表达时触发此 skill。即使用户只是说「帮我收个尾」或「检查一下文档有没有漏更新的」，也应该触发。"
---

# Spec 后处理流程

当用户表达"Spec 后处理流程"意思时，执行以下两步检查。

## 步骤 1：Spec 文档一致性检查

读取当前 Spec 目录下的 `requirements.md`、`design.md`、`tasks.md`，对照实际已实现的代码，检查：

- 后续迭代中新增/修改的需求是否已同步到 requirements.md
- 后续迭代中新增/修改的接口、数据模型、正确性属性是否已同步到 design.md
- 后续迭代中新增/修改的实现任务和测试任务是否已同步到 tasks.md

如发现不一致，直接执行更新。

## 步骤 2：Steering 更新检查

判断本次 Spec 的内容是否涉及框架级别的修改（如新增/修改 Frame Core、新增公共工具、修改关键约定等）。

- 如果是框架级修改：检查 `.kiro/steering/` 下的 `product.md`、`structure.md`、`tech.md` 是否需要同步更新，如需则直接执行。
- 如果不是框架级修改（纯业务逻辑、UI 面板等）：跳过，无需更新 steering。

## 输出要求

- 结果陈述简明扼要，禁止长篇大论
- 每步只说：检查了什么 → 是否需要更新 → 已更新 / 无需更新
