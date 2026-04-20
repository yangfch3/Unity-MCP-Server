---
inclusion: fileMatch
fileMatchPattern: "Docs/*.md,README*.md"
---

# README Tool Documentation Sync

When adding a new `IMcpTool` implementation, you MUST also update the tool tables in both `README.md` and `README_EN.md`, as well as the detailed docs in `Docs/TOOLS.md` and `Docs/TOOLS_EN.md`.

## Editor Tools Sub-Group Classification

The "Editor 工具" / "Editor Tools" section uses four sub-groups (five-level headings):

| Sub-Group | Rule |
|-----------|------|
| **Query** | Read-only tools that retrieve scene, object, or project information (e.g., `editor_get*`, `editor_find*`) |
| **Mutation** | Tools that create, modify, or delete GameObjects, components, or serialized fields (e.g., `editor_add*`, `editor_set*`, `editor_delete*`, `editor_remove*`, `editor_reparent*`, `editor_select*`) |
| **Project** | Cross-scene operations like menu execution and PlayMode control (e.g., `menu_*`, `playmode_*`) |
| **Asset** | Asset management operations on the Assets directory (e.g., `asset_*`) |

## Rules

1. Place the new tool in the correct sub-group table based on the classification above.
2. Add entries to BOTH `README.md` (Chinese) and `README_EN.md` (English) with matching descriptions.
3. Add detailed parameter documentation to BOTH `Docs/TOOLS.md` (Chinese) and `Docs/TOOLS_EN.md` (English) under the correct sub-group heading.
4. If the new tool does not fit any existing sub-group, create a new sub-group with a five-level heading in both READMEs and a three-level heading in both Docs files.
5. Keep tools within each sub-group sorted logically (query tools by scope, mutation tools by operation type).
