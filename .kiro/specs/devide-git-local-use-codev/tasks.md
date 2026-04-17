# 实施计划：分离 Git 远程安装与本地协作开发模式

## 概述

将仓库文档体系从"单一 README 混合受众"重构为"README 面向使用者 + CONTRIBUTING.md 面向协作开发者"的双文档结构。涉及 README.md 重构和 CONTRIBUTING.md 新建，package.json 已满足要求无需修改。

## 任务

- [x] 1. 重构 README.md
  - [x] 1.1 重写安装章节：新增 "Git URL 安装（推荐）" 子章节置于首位，保留 "本地路径安装" 作为备选
    - Git URL 安装提供完整的仓库 URL 和 UPM GUI 操作步骤
    - 本地路径安装保留现有内容
    - _需求: 3.1, 3.2, 3.5_

  - [x] 1.2 新增"版本更新"章节
    - 说明 UPM Git 依赖的版本锁定机制（packages-lock.json 锁定 commit hash）
    - 提供 Git Tag 指定版本的 URL 示例（`<repo_url>#v0.1.0`）
    - 提供 manifest.json 中带 Tag 和不带 Tag 的配置示例
    - 说明更新版本的操作方式
    - _需求: 2.1, 2.2, 2.3, 2.4_

  - [x] 1.3 移除"协作开发"和"项目结构"章节，新增"参与贡献"章节
    - 移除当前的"协作开发"章节全部内容
    - 移除当前的"项目结构"章节全部内容
    - 新增"参与贡献"章节，包含一句话引导和指向 CONTRIBUTING.md 的相对路径链接
    - _需求: 3.3, 3.4, 4.7_

- [x] 2. 检查点 — 确认 README.md 结构
  - 确认 README 章节顺序符合设计：简介 → 特性 → 安装（Git URL → 本地路径）→ 版本更新 → 使用 → 扩展 → 要求 → 参与贡献 → License
  - 确认所有链接有效，如有问题请询问用户

- [x] 3. 新建 CONTRIBUTING.md
  - [x] 3.1 编写"开发环境搭建"章节
    - 克隆仓库步骤
    - 本地路径安装到宿主项目的说明
    - _需求: 4.1, 4.2_

  - [x] 3.2 编写"启用 Package 内置测试"章节
    - testables 配置说明（从 README 迁移）
    - manifest.json 配置示例
    - _需求: 4.3_

  - [x] 3.3 编写"项目结构"章节
    - 从 README 迁移项目结构概览内容
    - _需求: 4.4_

  - [x] 3.4 编写"编码规范"章节
    - 命名空间规则
    - 日志前缀规则
    - 工具命名规则（`{category}_{action}`）
    - 文件编码要求（UTF-8 LF，无 BOM，末尾留空行）
    - _需求: 4.5_

  - [x] 3.5 编写"分支管理"章节
    - main 分支作为开发主线
    - 发布时在 main 上打 Git Tag（`v{major}.{minor}.{patch}`）
    - _需求: 4.6_

- [x] 4. 最终检查点 — 确认所有文档完整性
  - 确认 README.md 中 CONTRIBUTING.md 链接可正确跳转
  - 确认 CONTRIBUTING.md 包含所有从 README 迁移的内容
  - 确认 package.json 未被修改（已满足要求）
  - 如有问题请询问用户

- [x] 5. 补充英文文档与多语言导航
  - [x] 5.1 新建 README_EN.md — 翻译 README.md 为英文版本
  - [x] 5.2 新建 CONTRIBUTING_EN.md — 翻译 CONTRIBUTING.md 为英文版本
  - [x] 5.3 在 README.md 标题下方添加语言切换导航（中文 | English）
  - [x] 5.4 在 README_EN.md 标题下方添加语言切换导航（Chinese | English）

- [x] 6. Steering 文件改为英文
  - [x] 6.1 将 .kiro/steering/project-context.md 内容翻译为英文（保留 front-matter）
  - [x] 6.2 将 .kiro/steering/agent-guidelines.md 内容翻译为英文（保留 front-matter 和已有英文部分）

- [x] 7. SKILL 文件与 CONTRIBUTING 补充
  - [x] 7.1 将 .kiro/skills/spec-post-check/SKILL.md 翻译为英文，补充 CONTRIBUTING 检查步骤（Step 4）
  - [x] 7.2 CONTRIBUTING/CONTRIBUTING_EN 中 Tools 子树用 `...` 简化
  - [x] 7.3 CONTRIBUTING/CONTRIBUTING_EN 末尾新增 Spec 收尾章节
  - [x] 7.4 SKILL.md 和 agent-guidelines.md 中文触发词补充英文等价词

## 备注

- package.json 已满足 Git URL 安装全部要求，无需修改（需求 1 已满足）
- 本特性为纯文档重构，不涉及可执行代码，无需属性基测试
- 各任务引用了具体的需求条目以确保可追溯性
