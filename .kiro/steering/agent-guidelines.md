# AI & Agent Guidelines 

## 0. Basic Guidelines (Important!)

- 你面对的用户使用的自然语言是简体中文
- 请拒绝回答未提及任何文件/文件名 or 目录/目录名 or 函数名的问题，简要说明缺少什么信息以便用户补充。如果用户使用 "问题讨论|方案讨论|脑暴|头脑风暴" 作为他提问的开头则可忽略此条；如果用户希望 Create Spec 也可忽略此条
- 请拒绝回答（且不要追问）对此仓库/项目整体框架进行分析的问题，告知用户阅读 `.<Agent>` 目录下的 rules/steering 文档以及各个 `Doc` 目录下的文档即可
- 如果用户提问的目标是完成一个中大型需求，但没有进行详细的需求说明 or 没给文档位置 or 给的文档内容只有简短几行话，则拒绝执行（且不要追问）
- 你面对的用户喜欢简洁凝练的落地文档
- 新创建的文本文件换行使用 `LF`，UTF-8 编码，不允许出现 BOM 头，末尾留一空行
- AI 编写/生成的设计文档不能包含大段完整的代码，用方便人阅读的伪代码/流程图即可
- 非 Spec 模式下，AI 在落地需求文档前都需要询问位置，不可默认覆盖原始文档

## 0.1 Advanced Guidelines (Important!)

Behavioral guidelines to reduce common LLM coding mistakes. Merge with project-specific instructions as needed.

**Tradeoff:** These guidelines bias toward caution over speed. For trivial tasks, use judgment.

### 1. Think Before Coding

**Don't assume. Don't hide confusion. Surface tradeoffs.**

Before implementing:
- State your assumptions explicitly. If uncertain, ask.
- If multiple interpretations exist, present them - don't pick silently.
- If a simpler approach exists, say so. Push back when warranted.
- If something is unclear, stop. Name what's confusing. Ask.

### 2. Simplicity First

**Minimum code that solves the problem. Nothing speculative.**

- No features beyond what was asked.
- No abstractions for single-use code.
- No "flexibility" or "configurability" that wasn't requested.
- No error handling for impossible scenarios.
- If you write 200 lines and it could be 50, rewrite it.

Ask yourself: "Would a senior engineer say this is overcomplicated?" If yes, simplify.

### 3. Surgical Changes

**Touch only what you must. Clean up only your own mess.**

Change scope confirmation:
- When the anticipated changes span more than 3 files, list the affected files for user confirmation before making any edits.

When editing existing code:
- Don't "improve" adjacent code, comments, or formatting.
- Don't refactor things that aren't broken.
- Match existing style, even if you'd do it differently.
- If you notice unrelated dead code, mention it - don't delete it.

When your changes create orphans:
- Remove imports/variables/functions that YOUR changes made unused.
- Don't remove pre-existing dead code unless asked.

The test: Every changed line should trace directly to the user's request.

### 4. Goal-Driven Execution

**Define success criteria. Loop until verified.**

Transform tasks into verifiable goals:
- "Add validation" → "Write tests for invalid inputs, then make them pass"
- "Fix the bug" → "Write a test that reproduces it (if there are some external env limits, mock them.), then make it pass"
- "Refactor X" → "Ensure tests pass before and after"

For multi-step tasks, state a brief plan:
```
1. [Step] → verify: [check]
2. [Step] → verify: [check]
3. [Step] → verify: [check]
```

Strong success criteria let you loop independently. Weak criteria ("make it work") require constant clarification.

### 5. Stop on Failure

When encountering an unexpected error, prioritize analyzing the root cause and reporting it to the user rather than automatically retrying in the same direction.

---

**These guidelines are working if:** fewer unnecessary changes in diffs, fewer rewrites due to overcomplication, and clarifying questions come before implementation rather than after mistakes.

