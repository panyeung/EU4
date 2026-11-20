# 项目状态

更新时间：2026-11-20（UTC）
状态：IN PROGRESS

本文件记录当前工作状态。设计目标、里程碑和测试口径以 [checkpoint.md](checkpoint.md) 为准。界面与美术规则以 [ui设计规划.md](ui设计规划.md) 为准。

## 当前基线

- 仓库：`https://github.com/panyeung/EU4.git`。
- `main` 与 `origin/main` 同步。游戏代码基线为 `2bca9d1`。之后的主线提交只更新文档。本文件最初在 `feature/project-state-docs` 独立工作树编写；该文档分支已合入 `main`，不代表当前分支状态。
- Unity 版本：`6000.6.6f1`，URP 2D，Windows。
- 当前主工作树：`C:\Users\panye\My project`，分支 `docs/orca-project-workflow`，跟踪 `origin/docs/orca-project-workflow`；`main` 与 `origin/main` 同步在 `65a021d`，游戏代码基线仍为 `2bca9d1`。当前变更范围仅为文档。
- Unity Editor 当前为 PID `49784`、Unity `6000.6.6f1`、Pipeline 端口 `7801`，`-projectPath` 为本工程。2026-11-19 实时状态为 `ready`、`playMode=stopped`、`compiling=false`；FoundationScene 已加载且 `isDirty=false`。Play Mode 后 Console 实时错误/警告均为 0，未运行自动化测试。累计 Console 仍有两条更早的 CLI/评估命令错误（seq 16、17），不是本次游戏运行错误。任何 Unity 操作前重查进程、`-projectPath` 与 `unity pipeline list`；已有实例时复用，不得启动第二实例，也不得据此启动 batchmode。

## 当前阶段

**v1 Windows 演示的技术签核已通过，待用户最终验收。** `t_39a5b047` 于 2026-11-18 给出的 RETURN 针对文档过期状态；修订后，2026-11-19 的独立只读文档复审为 ACCEPT。主控基于现存构建、测试、Validator、Play Mode 与 Player 冒烟证据签核 M00 技术门。演示包可运行，但整项目尚未完成；用户试玩验收仍未完成。

演示包：

- 程序：`Builds/Demo-v1-2bca9d1-r2/Demo-v1-2bca9d1-r2.exe`。
- 来源：干净代码基线 `2bca9d1`。
- 已记录产物有 218 个文件，共 183,882,557 字节。exe 大小为 667,136 字节，SHA-256 为 `7c5b8f3ecaf9ef88a16646eacc9f817f327e3dde3244ccc627bd2e273188247d`。
- 路线：标题页 → 开始新游戏 → SC-013 → 营地 → 结束月份 → 清洲城镇 → 当地委托。

## 现有验证证据

| 范围 | 现有结果 | 证据边界 |
|---|---|---|
| EditMode | `2214/2214` 通过，源 `c291dd3` | 历史结果，不是本轮重跑 |
| FoundationScene Validator | `PASS; Errors: 0`，源 `c291dd3` | 历史结果，不是本轮重跑 |
| Play Mode 核心循环与隔离存读档 | 已记录通过，源 `c291dd3` | 详见 `Logs/verify/20261118-integration/20261118-1348-core-loop.md`。此证据不代表 Player 存读档或用户验收 |
| Windows Player 构建与冒烟 | 构建成功，报告记 0 错误。冒烟日志无 Exception、LogError、NullReference | 详见 `Logs/verify/v1-demo-build/20261118-r2-demo-build.md`。来源为 `2bca9d1` |
| M08 五十五周年 PlayMode | PR #4 报告端到端用例 `1/1` 通过，含确认后存档、正常月结、读档恢复 | PR 分支证据，未在本轮重跑，不等于真实玩家路线或用户验收 |
| 用户验收 | 未完成 | 只有用户试玩后才能闭合 |

Git 忽略 `Logs/` 与 `Builds/`。这些日志和 exe 目前只在本机工作区。GitHub 分支克隆不包含它们。交付演示时，确认用户能访问此工作区，或另行提供构建包。

不能把自动测试、Editor Play Mode 或 Player 冒烟替代用户验收，也不能把 v1 演示可玩等同于整项目完成。

## 未完成工作

1. M00 技术签核已通过；待用户试玩 v1 演示并给出最终验收结论。
2. M08 全量验证归档卡 `t_0a1e8ce8` 与其 55/100 年运行时记录已获独立复审 ACCEPT；FoundationScene 自动策略路线实际触发两次回顾，55 年确认后隔离槽 `SaveTo/LoadFrom` 往返通过。路线由 `farm-managed-v1` 自动选择，不等于玩家手动经营或用户验收。百年终态粮荒 53 个月、极端通胀 41 个月、粮食库存 0，经济健康未通过。PR #4 另报告五十五周年 PlayMode 用例 `1/1` 通过。证据见 `docs/reports/v1-m08-55-review-user-acceptance-20261119.md` 与 PR #4。
3. M09 窗口拖动尚无真实硬件输入证据。已有合成输入结果不算通过。
4. M08B 经济方向仍待用户在 PR #5 选择 A/B/C/D，并确认方案参数与 1000 种子粮食崩溃率目标。独立文档复审已 ACCEPT；不表示规则获批或代码已实现。
5. `main` 中 `Assets/Tests/PlayMode` 仍为空；PR #4 分支新增的单项测试尚未合并。运行时结果须按对应证据来源分别记录。

checkpoint 记录的 M08B D3 基线中，`unmanaged` 1000 种子均未到达 50 年或 100 年。这个结果是已接受的风险基线，不是长期生存通过。SC-021 与 SC-023 已集成进 `main`，但集成事实本身不代表所有相关运行时体验均已验收。

## Git 与协作状态

- Orca 当前为本项目登记主工作树及 M08 相关工作树；`m08-55-year-review-runtime` 含既有用户设置改动，必须保留。每项任务一个写者，Unity 测试串行执行。
- PR #3、#4、#5 均开放且未合并。PR #5 以 `docs/orca-project-workflow` 为基底；用户最终验收前不合并 `main`、不打里程碑 tag。
- OpenCode2 负责所有代码实现和测试执行。最近一次派工报告其用量达到月度限制；额度恢复前不转交其他 agent 实现代码。只读研究与独立文档复审仍可进行。
- `origin/panyeung/ui-m04-family` 的部分文件改动已在 `main`。直接合并旧分支历史会让场景和 UI 文件冲突。保留该引用，不重放旧历史。
- `checkpoint.md` 顶部原先引用旧分支和旧基线。本次文档分支已改为当前代码基线。合入前由独立复审检查这项更正。
- v1 演示运行说明见 [v1 Windows 演示包](docs/reports/v1-demo-package-20261118.md)。
- 下次启动 Unity 前，重新检查本工程 Editor 进程及 `-projectPath`，并核实 `unity pipeline list` 与 Orca 工作树状态。OpenCode2 执行代码实现和测试；独立复审 agent 只读审代码与证据。
