# Design Document

## Overview

本设计将 README.md 和 README_EN.md 中 "Editor 工具" 部分的单一大表格拆分为四个功能子分组（Query / Mutation / Project / Asset），并新增 steering 规则文件确保未来新增工具时 AI 自动同步更新文档。

这是一个纯文档变更，不涉及任何代码修改。

## Architecture

无代码架构变更。变更范围：

- `README.md` — Editor 工具表格区域
- `README_EN.md` — Editor Tools 表格区域
- `.kiro/steering/readme-tool-docs.md` — 新增 steering 文件

## Components and Interfaces

### Editor 工具子分组结构

当前的单一 `#### Editor 工具` 表格将替换为四个五级标题子表格：

```
#### Editor 工具
##### Query（查询）
  - editor_getHierarchy, editor_getInspector, editor_getSelection,
    editor_findGameObjects, editor_getProjectPath, editor_getProjectStructure

##### Mutation（修改）
  - editor_addGameObject, editor_deleteGameObject, editor_setField,
    editor_setTransform, editor_setActive, editor_reparentGameObject,
    editor_addComponent, editor_removeComponent, editor_setComponentEnabled,
    editor_selectGameObject

##### Project（项目）
  - menu_execute, playmode_control

##### Asset（资产）
  - asset_deleteFolder
```

英文版使用相同结构，五级标题为 Query / Mutation / Project / Asset。

### Steering 文件内容

文件路径：`.kiro/steering/readme-tool-docs.md`

包含：
- YAML front-matter，`inclusion: always`
- 指示 AI 在新增 `IMcpTool` 实现时同步更新两个 README 的子分组表格
- 描述四个子分组的分类规则
- 指示当新工具不属于现有分组时创建新分组

## Data Models

不适用（纯文档变更）。

## Error Handling

不适用（纯文档变更）。

## Testing Strategy

本需求为纯文档变更，不涉及代码修改，因此：

- 不需要单元测试或属性测试
- 验证方式：人工审查 README 中的子分组结构是否正确、工具分配是否完整、steering 文件格式是否正确
