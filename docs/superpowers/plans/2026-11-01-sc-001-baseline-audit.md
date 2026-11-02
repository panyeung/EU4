# SC-001 现有项目基线审计计划

> **执行说明：** 本任务只做规划与只读盘点。后续编程由 OpenCode 负责；本次审计不得修改代码。

**目标：** 确立现有 Unity 项目和仓库为《影之一族》的唯一技术基线，并形成有证据的 M0 复用与缺口报告。

**方法：** 在现有项目内盘点，不新建或迁移 Unity 项目。将现有程序集、领域模型、软件包、场景、测试和 Git 状态与《影之一族》计划逐项对照，在开始实现前列明可复用部分和迁移风险。

**技术栈：** 现有 Unity `6000.6.6f1`、C#、URP `17.6.0`、uGUI、Unity Test Framework `1.8.0`。

**设计依据：** `GDD_v4.0.md` 第 11、12、17 章，以及用户当前提供的 AGENTS.md 中“只使用一个项目”的要求。

## 全局约束

- Unity 项目仅使用 `C:\Users\panye\My project`；不得另建 Unity 项目或同时打开第二个 Editor。
- 保留所有现有分支、提交、本地修改和未跟踪文件；审计期间不得切换分支、暂存或提交。
- 不得编辑 C#、场景、Prefab、`ProjectSettings`、软件包、`.meta` 文件、`Library` 或 `Temp`。
- 执行任何 Unity 验证前，先确认 Editor 实际加载的 `-projectPath`；不得对 Editor 已打开的项目启动 batchmode 或第二个 Editor。
- 编译、测试、Console 或 Play Mode 结果只能依据正确 Editor 中的实际证据报告。
- OpenCode 负责后续实现；本计划只授权只读盘点和报告。

## 审查重点

- 项目身份：此前观察到 PID `56820` 的进程路径位于 `Unity Hub\\resources`，但没有可解析的 `-projectPath`；任何 Unity 验证前都要确认实际 Editor 与加载项目。
- Git 身份与 WIP：指定的 `main` 落后于 `origin/main`，并有用户修改及未跟踪文件；保留这些文件，不要把独立 `feature/m5-save-slot` 工作树的内容误认为指定项目内容。
- 引擎与软件包兼容性：现有项目使用 Unity `6000.6.6f1` 和 URP `17.6.0`；v4.0 计划提及 Unity 6.3 LTS。记录版本差异，SC-001 期间不升级项目。
- 现有存档和领域系统：只检查指定项目中是否有存档 DTO、家族/人物/关系模型、按月推进、固定种子随机数、事件 Schema/引擎和编年史；不得根据其他工作树推断。
- 场景与测试覆盖：区分磁盘上的 `FoundationScene` 与 `EditorBuildSettings` 中启用的场景；区分 EditMode 源码覆盖与实际运行或人工 Play Mode 证据。

## 接口与审计交付

- **基线报告：** `docs/SC001_BASELINE_AUDIT.md`，记录项目路径、分支、提交、WIP、Editor 身份、Unity 与软件包版本、asmdef、相关脚本、场景/构建设置、测试及证据来源。
- **复用与缺口表：** 将每项 M0 能力（家族/人物 ID、关系、按月时钟、固定种子随机数、版本化 JSON 存档、事件 Schema/引擎、文字事件 UI、继承、跨代事件、编年史）标记为“复用”“调整”“缺失”或“待决策”，并附具体路径与证据。
- **后续交接：** 建议下一份由 OpenCode 实施并单独审查的计划。SC-001 本身不授权修改代码或场景。

## 任务

### 任务 1：记录 Git 与工作区状态

- [x] 在 `C:\Users\panye\My project` 记录 `git status --short --branch`、当前提交、远端跟踪状态，以及与规划工作树的差异。
- [x] 只按路径列出已有本地修改和未跟踪文件；不暂存、不清理、不 stash、不 reset、不切换分支。
- [x] 记录 `ProjectVersion.txt` 与现有里程碑历史，不把历史验收当作新方向的实现进度。

### 任务 2：确认 Unity Editor 所属项目

- [ ] 检查运行中的 Unity 进程，确认真实 Editor 可执行文件及 `-projectPath`。PID `56820` 未提供可解析的项目路径，当前加载项目仍未知。
- [x] 将 Editor 验证标记为等待确认项目身份；本次没有启动 Editor 或 batchmode。

### 任务 3：盘点项目结构与软件包

- [x] 记录 `manifest.json` 和 `packages-lock.json` 中 URP、Input System、TextMeshPro、JSON 与测试框架相关版本。
- [x] 列出运行时、Editor、测试 asmdef，以及 Domain、Application、Presentation、Persistence 目录和脚本。
- [x] 列出项目场景，并与 `EditorBuildSettings` 中启用的场景对照。
- [x] 按 EditMode/PlayMode 程序集及范围盘点测试；本次不运行测试。

### 任务 4：对照 M0 盘点现有代码

- [x] 在指定项目内检索稳定 ID、家族/人物/关系类型、按月推进、确定性随机数、存档 DTO/版本、事件定义/Schema/引擎、继承和编年史。
- [x] 为每项能力记录证据路径，并将源码证据与测试、运行证据分开。
- [x] 标出可能复用的 Presentation/更新器模式，以及不应未经审查带入新项目的旧方向假设；详见 `docs/SC001_BASELINE_AUDIT.md`。

### 任务 5：交付审计与交接

- [x] 在 `docs/SC001_BASELINE_AUDIT.md` 编写基线报告和复用/缺口表。
- [x] 为 OpenCode 补充后续 SC-002 起的建议，遵循 GDD v4.0 顺序和单项目约束。
- [x] 检查 Markdown 链接和 `git diff --check`；未修改 Unity 代码、场景、软件包或项目设置。

## 验收标准

- 报告识别指定项目、Git/WIP 状态，并说明能否证实 Unity Editor 正加载该路径。
- 每项 M0 能力都有对应路径和复用/缺口状态；未知项明确标为未知，不猜测。
- 不以静态检查声称运行时或测试结果。
- 审计只修改文档；不修改代码、场景、软件包或项目设置。
