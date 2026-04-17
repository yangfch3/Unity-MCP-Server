---
name: spec-post-check
description: "Spec post-processing workflow: after a round of Spec Coding, check and sync Spec documents, Steering documents, and Contributing documents affected by the changes. Triggered when the user says 'Spec 后处理', '后处理流程', 'spec post check', '收尾检查', 'post-processing', 'wrap up', 'finalize docs', or similar expressions. Also triggered by phrases like '帮我收个尾', '检查一下文档有没有漏更新的', 'check if any docs need updating', or 'sync the docs'."
---

# Spec Post-Processing Workflow

When the user expresses the intent for "Spec post-processing", execute the following checks.

## Step 1: Spec Document Consistency Check

Read `requirements.md`, `design.md`, and `tasks.md` from the current Spec directory. Compare against the actually implemented code and check:

- Whether requirements added/modified during subsequent iterations have been synced to requirements.md
- Whether interfaces, data models, and correctness properties added/modified during subsequent iterations have been synced to design.md
- Whether implementation tasks and test tasks added/modified during subsequent iterations have been synced to tasks.md

If inconsistencies are found, execute updates directly.

## Step 2: Steering Update Check

Determine whether the Spec's content involves framework-level changes (e.g., adding/modifying Frame Core, adding public utilities, changing key conventions, etc.).

- If framework-level changes: check whether files under `.kiro/steering/` (e.g., `product.md`, `structure.md`, `tech.md`) need to be updated. If so, execute directly.
- If not framework-level changes (pure business logic, UI panels, etc.): skip, no steering update needed.

## Step 3: README Update Check

Determine whether the Spec added/modified user-visible features (e.g., new tools, new APIs, changed configuration methods, etc.).

- If user-visible feature changes are involved: check whether `README.md` and `README_EN.md` in the project root need to be updated (e.g., tool list, usage instructions, configuration instructions). If so, execute directly.
- If only internal refactoring or changes that don't affect user usage: skip, no README update needed.

## Step 4: CONTRIBUTING Update Check

Determine whether the Spec's changes affect contributor-facing content (e.g., project structure changes, new coding conventions, new test patterns, build process changes, etc.).

- If contributor-facing content is affected: check whether `CONTRIBUTING.md` and `CONTRIBUTING_EN.md` need to be updated. If so, execute directly.
- If changes don't affect the contributor workflow: skip, no CONTRIBUTING update needed.

## Output Requirements

- Keep statements concise and to the point; no lengthy explanations
- For each step, only state: what was checked → whether update is needed → updated / no update needed
