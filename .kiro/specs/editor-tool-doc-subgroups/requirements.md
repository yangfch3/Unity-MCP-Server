# Requirements Document

## Introduction

当前 README.md 和 README_EN.md 中的 "Editor 工具" 部分将 18+ 个工具平铺在一张表格中，随着工具数量增长，可读性下降。本需求将该表格拆分为逻辑子分组（Query / Mutation / Project / Asset），并新增 steering 规则，确保未来新增工具时 AI 自动同步更新 README 子分组表格。

本需求仅涉及文档变更，不涉及任何代码修改。

## Glossary

- **README_CN**: 项目根目录下的 `README.md`（简体中文版）
- **README_EN**: 项目根目录下的 `README_EN.md`（英文版）
- **Editor_Tools_Section**: README 中 "Editor 工具" / "Editor Tools" 标题下的工具列表区域
- **Sub_Group**: Editor_Tools_Section 内按功能职责划分的子表格，每个子表格有独立的五级标题
- **Query_Sub_Group**: 只读查询类工具的子分组，包含获取场景/对象/项目信息的工具
- **Mutation_Sub_Group**: 写入/修改类工具的子分组，包含增删改 GameObject、组件、属性的工具
- **Project_Sub_Group**: 项目级工具的子分组，包含菜单执行、PlayMode 控制等跨场景操作
- **Asset_Sub_Group**: 资产管理类工具的子分组，包含 Assets 目录操作的工具
- **Steering_File**: `.kiro/steering/` 目录下的 Markdown 文件，用于向 AI Agent 提供项目级行为指引

## Requirements

### Requirement 1: 拆分 Editor 工具表格为子分组

**User Story:** 作为项目维护者，我希望 README 中的 Editor 工具列表按功能职责分组展示，以便快速定位特定类型的工具。

#### Acceptance Criteria

1. WHEN 用户查看 README_CN 的 Editor_Tools_Section 时，THE README_CN SHALL 将原有的单一表格替换为以下四个 Sub_Group，每个 Sub_Group 使用五级标题（`#####`）和独立表格展示：Query_Sub_Group、Mutation_Sub_Group、Project_Sub_Group、Asset_Sub_Group
2. THE Query_Sub_Group SHALL 包含以下工具：`editor_getHierarchy`、`editor_getInspector`、`editor_getSelection`、`editor_findGameObjects`、`editor_getProjectPath`、`editor_getProjectStructure`
3. THE Mutation_Sub_Group SHALL 包含以下工具：`editor_addGameObject`、`editor_deleteGameObject`、`editor_setField`、`editor_setTransform`、`editor_setActive`、`editor_reparentGameObject`、`editor_addComponent`、`editor_removeComponent`、`editor_setComponentEnabled`、`editor_selectGameObject`
4. THE Project_Sub_Group SHALL 包含以下工具：`menu_execute`、`playmode_control`
5. THE Asset_Sub_Group SHALL 包含以下工具：`asset_deleteFolder`
6. THE README_CN SHALL 保持每个工具的原有功能描述文本不变
7. THE README_CN SHALL 保持 Editor_Tools_Section 以外的所有内容不变

### Requirement 2: 同步更新英文版 README

**User Story:** 作为国际用户，我希望 README_EN 的 Editor Tools 部分与中文版保持相同的子分组结构，以便获得一致的阅读体验。

#### Acceptance Criteria

1. WHEN README_CN 的 Editor_Tools_Section 完成子分组拆分后，THE README_EN SHALL 采用与 README_CN 相同的四个 Sub_Group 结构和工具分配
2. THE README_EN SHALL 使用对应的英文五级标题：Query、Mutation、Project、Asset
3. THE README_EN SHALL 保持每个工具的原有英文功能描述文本不变
4. THE README_EN SHALL 保持 Editor_Tools_Section 以外的所有内容不变

### Requirement 3: 新增 Steering 规则文件

**User Story:** 作为项目维护者，我希望 AI Agent 在新增工具时自动知道需要更新 README 的子分组表格，以便文档始终与代码保持同步。

#### Acceptance Criteria

1. THE Steering_File SHALL 创建在 `.kiro/steering/` 目录下，文件名为 `readme-tool-docs.md`
2. THE Steering_File SHALL 包含 YAML front-matter，其 `inclusion` 字段设置为 `always`
3. THE Steering_File SHALL 指示 AI Agent：当新增 `IMcpTool` 实现时，必须将新工具添加到 README_CN 和 README_EN 的对应 Sub_Group 表格中
4. THE Steering_File SHALL 描述四个 Sub_Group 的分类规则，使 AI Agent 能正确判断新工具应归入哪个子分组
5. THE Steering_File SHALL 指示 AI Agent：当新增工具不属于现有任何 Sub_Group 时，应创建新的 Sub_Group 并在两个 README 中同步添加
