# 需求文档：分离 Git 远程安装与本地协作开发模式

## 简介

当前仓库的 README 混合了"使用者"和"协作开发者"两种受众的内容。本需求将两者分离：README 面向使用者，以 `Add package from git URL` 作为主要安装方式；协作开发相关内容迁移至独立文档。同时需确保 `package.json` 及仓库结构满足 Unity Package Manager 通过 Git URL 安装的要求。

## 术语表

- **UPM**: Unity Package Manager，Unity 内置的包管理器
- **Git_URL_安装**: 通过 UPM 的 "Add package from git URL" 功能，直接从 Git 仓库 URL 安装 Package
- **本地路径安装**: 通过 UPM 的 "Add package from disk" 或 `file:` 协议引用本地目录安装 Package
- **使用者**: 仅需安装并使用本插件的 Unity 开发者
- **协作开发者**: 需要克隆仓库、修改代码、提交 PR 的贡献者
- **README**: 仓库根目录的 `README.md`，面向使用者的主文档
- **协作开发文档**: 面向协作开发者的独立文档（如 `CONTRIBUTING.md` 或 `Docs/` 下的文档）

## 需求

### 需求 1：package.json 满足 Git URL 安装要求

**用户故事：** 作为使用者，我希望通过 Git URL 直接安装本插件，这样我不需要手动克隆仓库。

#### 验收标准

1. THE package.json SHALL 包含 UPM 通过 Git URL 安装所需的全部必要字段（`name`、`version`、`displayName`、`description`、`unity`）
2. THE package.json 中的 `name` 字段 SHALL 遵循 `com.<company>.<package>` 的反向域名格式
3. THE package.json SHALL 位于仓库根目录，使 UPM 能直接通过仓库根 URL 解析到该文件
4. WHEN 使用者在 UPM 中通过 Git URL 添加本仓库时，THE UPM SHALL 成功解析并安装本 Package

### 需求 2：README 提供版本更新指引

**用户故事：** 作为使用者，我希望了解如何更新通过 Git URL 安装的插件版本，这样我能及时获取新功能和修复。

#### 验收标准

1. THE README SHALL 说明 UPM Git 依赖的版本锁定机制（UPM 会在 `packages-lock.json` 中锁定 commit hash）
2. THE README SHALL 提供通过 Git Tag 指定版本的安装 URL 示例（如 `<repo_url>#v0.1.0`）
3. THE README SHALL 说明更新版本的操作方式：重新通过 UPM "Add package from git URL" 输入新版本 Tag 的 URL
4. WHEN 仓库发布新版本时，THE 仓库 SHALL 使用 Git Tag（格式如 `v{major}.{minor}.{patch}`）标记版本，使使用者可通过 Tag 锁定特定版本

### 需求 3：README 以使用者为主要受众

**用户故事：** 作为使用者，我希望 README 简洁明了地告诉我如何安装和使用本插件，不被协作开发的细节干扰。

#### 验收标准

1. THE README SHALL 将 "Add package from git URL" 列为首选安装方式，并提供完整的 Git URL
2. THE README SHALL 保留 "本地路径安装" 作为备选安装方式
3. THE README SHALL 移除当前的"协作开发"章节内容
4. THE README SHALL 包含指向协作开发文档的链接，供有兴趣参与开发的读者跳转
5. THE README 的安装章节 SHALL 按以下顺序组织：Git URL 安装 → 本地路径安装

### 需求 4：协作开发内容迁移至独立文档

**用户故事：** 作为协作开发者，我希望有一份专门的文档指导我如何搭建开发环境、运行测试、提交贡献，这样我能快速上手参与开发。

#### 验收标准

1. THE 协作开发文档 SHALL 作为独立文件存在于仓库中（如 `CONTRIBUTING.md`）
2. THE 协作开发文档 SHALL 包含本地开发环境搭建说明（克隆仓库、本地路径安装到宿主项目）
3. THE 协作开发文档 SHALL 包含启用并运行 Package 内置测试的说明（`testables` 配置）
4. THE 协作开发文档 SHALL 包含项目结构概览
5. THE 协作开发文档 SHALL 包含编码规范要点（命名空间、日志前缀、工具命名规则等）
6. THE 协作开发文档 SHALL 包含分支管理策略说明：`main` 分支作为开发主线，发布时在 `main` 上打 Git Tag
7. WHEN README 中的协作开发章节被移除后，THE README SHALL 包含一条指向协作开发文档的引导链接

### 需求 6：提供英文版文档

**用户故事：** 作为英文用户，我希望有英文版的 README 和 CONTRIBUTING，这样我能无障碍地了解和参与本项目。

#### 验收标准

1. THE 仓库 SHALL 包含 `README_EN.md`，内容为 `README.md` 的英文翻译
2. THE 仓库 SHALL 包含 `CONTRIBUTING_EN.md`，内容为 `CONTRIBUTING.md` 的英文翻译
3. THE README.md SHALL 在标题下方包含语言切换导航（中文 | English）
4. THE README_EN.md SHALL 在标题下方包含语言切换导航（Chinese | English）

### 需求 7：Steering 和 Skill 文件使用英文

**用户故事：** 作为 AI Agent 的使用者，我希望 steering 和 skill 文件使用英文，以便 Agent 更准确地理解指令。

#### 验收标准

1. THE `.kiro/steering/` 下的文件内容 SHALL 使用英文（保留 front-matter 格式）
2. THE `.kiro/skills/spec-post-check/SKILL.md` SHALL 使用英文，同时保留中英文触发词

**用户故事：** 作为使用者，我希望确认通过 Git URL 安装后插件能正常工作，这样我可以放心使用这种安装方式。

#### 验收标准

1. THE 仓库根目录 SHALL 包含 `package.json`，且该文件可被 UPM 的 Git URL 解析器正确识别
2. THE 仓库 SHALL 不依赖 Git URL 安装时无法获取的本地路径或外部资源
3. IF `package.json` 缺少 Git URL 安装所需的字段，THEN THE 需求实施过程 SHALL 补全缺失字段
4. THE `Editor/` 目录及其 `.asmdef` 文件 SHALL 在 Git URL 安装后被 Unity 正确识别为 Editor 程序集
