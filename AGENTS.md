# 影之一族 — Unity 项目指令

## 语言

- 本项目的一切文字用简体中文。范围包括对用户的回复、提交信息、文档、代码注释、任务卡、结项说明与阶段报告。
- 例外只有三类。代码标识符与类型名。Unity 与第三方 API 的原文名称。既有英文日志路径与命令输出。

## 项目

- 引擎 Unity 6，渲染管线 URP 2D，语言 C#，平台 Windows。
- 工程路径 `C:\Users\panye\My project`。
- 游戏方向。《影之一族》按 `checkpoint.md` 的设计规划与里程碑执行。UI 与美术资产按 `ui设计规划.md`。旧 Sengoku Chronicle M1–M7 文档仅作历史档案。

## 团队

本项目通过 Orca 管理工作树与 agent。主控在当前会话负责决策，其他 agent 通过 Orca 在独立工作树中执行。

| 角色 | Agent | 派工方式 | 负责 |
|---|---|---|---|
| 主控 | 当前会话 | 直接处理小任务；复杂任务通过 Orca 创建工作树并启动 OpenCode2 | 规划、验收标准、测试方案、技术签核、对用户汇报 |
| 实现 | OpenCode2（`opencode2`） | 通过 Orca 创建独立工作树，在其中运行 `opencode2` 或 `opencode2 run <任务说明>` | 所有代码实现与修正、测试代码、Unity 测试执行、经既有 updater 改场景 |
| 复审 | Orca 中可用的独立 agent | `orca agent launch --agent <agent> --worktree <工作树> --prompt <只读复审说明> --json` | 独立复审代码与证据，列出可执行修正项或通过结论 |
| 研究与分析 | Orca 中可用的 agent | 只读研究可在当前工作树执行；需修改文件时创建独立工作树 | Unity API 与包调研、GitHub 复用调研、许可与兼容性核对、架构分析 |

所有代码实现与代码修正均由 OpenCode2 执行。其他 agent 仅做研究或只读复审，不修改代码。派工前运行 `orca worktree list --json` 和 `orca agent list --json`，核实工作树和 agent 状态。短小任务由主控直接完成；需要持续执行或独立复审的任务使用 Orca 工作树，并在提示中写明范围、禁止事项、交付物和证据要求。

## 项目安全

- 保留 Unity `.meta` 文件与 GUID。
- 不改 `Library` 与 `Temp`。
- 未经批准不删场景或预制体。
- 未经批准不装包。
- 改动前先看 `git status`。
- 不覆盖用户未提交的改动。
- 改动验证过再说成功。
- 除非获批，改动限制在本工程内。

## 验证

- Unity 开工前先查正在运行的 Editor 进程与其 `-projectPath`。每个工程目录只留一个 Editor，绝不对用户 Editor 占用的目录跑 batchmode。
- 用那个 Editor 的 Test Runner 与 `Editor.log` 做编译与测试。标准日志在 `%LOCALAPPDATA%\Unity\Editor\Editor.log`，另看 `<project>\Logs\Editor.log`。
- 本机没有 .NET SDK，`dotnet` 不是构建路径。构建路径是 Unity Pipeline bridge（`com.unity.pipeline` 加 `unity` CLI，Editor 端口 7800–7849）。对已开的 Editor 使用 `unity command editor_status`、`console`、`console_status`、`recompile`、`run_script`、`open_scene`、`editor_play` 与 `editor_stop`、`editor_focus`、`capture_game_view`，不要走 GUI 自动化。
- 编译、EditMode、Validator、Console 计数、Play Mode、存读档、用户验收，各自作为独立证据记录。不要从编译结果或测试运行推断运行时行为。没有运行时证据就不得声称运行时行为。
- 项目验证配方在 `.opencode/skills/` 的 `verify` 与 `succession-invariants`。2026-11-10 的 Unity Pipeline 决策条记录在 `checkpoint.md` 决策记录里，含可用命令与其限制。
- 不要跨工作树并行跑测试。它们共用 `TestResults.xml`，会互相覆盖证据。
- 每个里程碑以可玩 demo 收尾。要求编译通过、Console 零错误、场景正确、主循环可玩，范围内还要验存读档。

## 省 token

- 减少重复上下文。
- 复用已有调研。
- 只派边界清楚的任务。
- 只回结论。
- 不重复读没变的文件。

## GitHub 优先策略

实现新功能之前。

1. 先在现有代码里找可复用的方案。
2. 让 Orca 中的研究 agent 在 GitHub 上找相关仓库、库或现成实现。
3. 优先 Unity 6 兼容的方案。
4. 评估许可与商用限制、Unity 与 URP 兼容性、维护活跃度、依赖与安全风险、实现成本。
5. 把复用与自研做对比。
6. 有成熟轻量的方案能用就用。
7. 未经批准不装包、不克隆仓库、不抄外部代码。
8. 确实没有合适方案时，做一个聚焦的自研实现。

研究报告格式。仓库名与 URL、许可、兼容性、理由、预计接入成本、结论 `复用 / 改造 / 自研`。首轮只查三个最有希望的仓库，先读 README 再克隆。琐碎修复与既有代码维护跳过外部调研。

## Git 与交付

- 在功能或集成分支上工作。用户接受 demo 之后才合并 `main` 与打里程碑 tag。
- 绝不 force-push、改写历史、删除含未合并工作的分支。
- 每个验证门通过后，只提交本任务已验证的文件，把无关本地改动排除在提交外，并推送到当前分支的 `origin`。
- 每个 checkpoint 项闭合后立即提交并推送，然后在 GitHub 上开一个 PR，PR 正文写清下一步（剩余项、顺序、每项要的证据、明确不做什么）。合并仍须用户点头。
- 每次 checkpoint 提交、里程碑合并或打 tag 之后立即推送，并回报远端 ref。
- 只有主控或一个指定的集成 agent 能改场景、预制体或 `ProjectSettings`。并行 agent 在不同工作树里可以改 C# 逻辑、文本数据、测试、文档与调研。
- 玩法规则尽量用纯 C#，用 asmdef，保持 `PlayerMovement2D` 行为不变。
- Editor 场景生成器只用一个 Undo 组，保存前先验证，异常时用 Undo 回滚且不重载场景。生成的 UI 放在一个固定根节点下。

## 并发与共享资源

- 一个任务一个写者。同一棵工作树、同一个 Unity 实例，同一时刻只允许一个写者在跑。
- 并行任务必须落在不同工作树；只有只读任务才可以和写者共用主工作树。
- `TestResults.xml` 全机共用（`%LOCALLOW%\DefaultCompany\My project\`）。测试一律串行；每次跑完立刻 `scripts/copy-testresults.sh <证据目录>`，之后只引用副本。2026-11-17 并行覆盖过一次：主工程 2141 与 M04 工作树 2015 互相顶掉。
- 派活前先查 `unity pipeline list` 与 `orca worktree list --json`，确认没有第二个写者；发现并行写者先停下来合流。

## Orca 复审循环

- OpenCode2 在自己的 Orca 工作树中完成实现，并提交改动文件、测试命令与结果、运行时证据。
- 主控通过 Orca 启动独立只读复审。复审给出修正项时，由 OpenCode2 在原工作树修正后再复审；复审通过并核实证据后，主控才签核。
- 记录同一改动的复审次数。十次仍未通过时停止循环，向用户列出未解决项。
- 实质性功能由主控做最终技术签核。报告改动文件、未决问题与实际测试结果，并分别列出编译证据和 Play Mode 证据。实现完成或复审通过都不等于用户验收。

## 用户协作边界

- 主控负责规划、验收标准与测试方案。OpenCode2 负责玩法与集成代码、测试代码及测试执行，包括 EditMode、Play Mode、场景验证与相应回归检查。
- OpenCode2 回传确切命令、结果文件、测试总数、失败项、Console 计数与运行时证据。主控审这些证据并做技术签核。默认由 OpenCode2 执行测试；用户明确授权本轮代跑时，主控可执行指定 Unity 试玩并做预验收，但代码实现仍由 OpenCode2 负责，预验收不替代用户最终验收。复审 agent 只审查代码与证据，不执行测试。
- 这个测试分工是用户的长期指令。复审 agent 可以审测试与证据，但不执行它们。
- 主控按用户授权代做 Unity 试玩且通过后，创建 PR。PR 正文列出用户试玩验收的逐步操作、每步预期效果、通过条件及未闭合门槛。用户最终验收前不合并 `main`、不打 tag。
- Play Mode 验证获准使用 Computer Use 与截图，用户在场时同样可用。每次开工前查 Editor 进程与其 `-projectPath`，只操作本工程已开的那个。
- 每张 Computer Use 截图在目视检查后立即删除。不在工程或备份里留截图，任务结束时清空 Computer Use 临时缓存。
- 最终里程碑验收在用户手里。没有验收，不得建最终里程碑 checkpoint、不得合并 `main`、不得打 tag。

## 成本与依赖

- 待在本仓库内。不用付费服务、订阅、素材或 API。
- 不抄太閤立志传或其他商业游戏的内容。用原创、CC0 或宽松许可的素材，每个来源与许可登记在 `ui设计规划.md` 第 10 节。新加 Unity 包与理由记在 `checkpoint.md` 决策记录。
- 真阻塞就报给用户，不要靠无限派生后续卡撑着。

## 美术资产

按 `ui设计规划.md` 第 10 节执行。主控生成原创且与参考匹配的素材并登记到 `ui设计规划.md`，OpenCode2 负责实现与接入。OpenCode2 不可用时暂停代码实现并向用户报告，不改派给其他 agent。
