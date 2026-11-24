# 项目状态

更新时间：2026-11-23（檀香山时间；UTC 2026-11-24）
状态：IN PROGRESS

本文件记录当前工作状态。设计目标、里程碑和测试口径以 [checkpoint.md](checkpoint.md) 为准。界面与美术规则以 [ui设计规划.md](ui设计规划.md) 为准。

## 当前基线

- 仓库：`https://github.com/panyeung/EU4.git`。
- `main` 与 `origin/main` 同步。游戏代码基线为 `2bca9d1`。之后的主线提交只更新文档。本文件最初在 `feature/project-state-docs` 独立工作树编写；该文档分支已合入 `main`，不代表当前分支状态。
- Unity 版本：`6000.6.6f1`，URP 2D，Windows。
- 主工作树：`C:\Users\panye\My project`，基线工作区干净。本文件最初在 `feature/project-state-docs` 独立工作树编写，以免干扰 v1 演示签核。
- 本工程当前有单一 Unity Editor 实例：PID `93704`，`6000.6.6f1`，Pipeline 端口 `7800`，`-projectPath` 为 `C:\Users\panye\My project`。连接状态有两份证据：`Logs/verify/20261118-m08-full-verification.md` 记录 2026-11-18 20:18:31Z / 20:18:35Z / 20:18:40Z 连续三次 `status=ready`、`playMode=stopped`、`compiling=false`；本次文档核对另做一次只读查询，`lastHeartbeat` 为 2026-11-18T23:33:38Z，返回 `ready`、`playMode=stopped`。这些只说明 Editor 可连接，不代表运行时行为。任何 Unity 操作前重查进程、`-projectPath` 与 `unity pipeline list`；已有实例时复用，不得启动第二实例，也不得据此启动 batchmode。

## 当前阶段

**v1 Windows 演示待最终技术签核与用户验收。** 演示包可运行，但整项目尚未完成。`tester` 于 2026-11-18 13:12Z 对 `t_39a5b047` 给出 RETURN，要求先修正文档里的过期当前状态再复审；本轮修订由 `t_1df7a5a1` 执行。签核通过后交付演示包和一到两分钟试玩步骤；用户验收仍未完成。

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
| 用户验收 | 未完成 | 只有用户试玩后才能闭合 |

Git 忽略 `Logs/` 与 `Builds/`。这些日志和 exe 目前只在本机工作区。GitHub 分支克隆不包含它们。交付演示时，确认用户能访问此工作区，或另行提供构建包。

不能把自动测试、Editor Play Mode 或 Player 冒烟替代用户验收，也不能把 v1 演示可玩等同于整项目完成。

## 未完成工作

1. 完成 `t_1df7a5a1` 的 RETURN 修订及其后的独立复审，确认 `t_39a5b047` 技术签核通过后，交付 v1 演示并等待用户验收。
2. `t_0a1e8ce8` 的 M08 长周期回顾已有自动化 PlayMode 路线证据：SC-013 固定种子 `5343414E4B4F3031`，从 1560-04-01 经生产按钮回调到 1615-04-01（660 次成功月结）与 1660-04-01（1200 次），两轮同版本复跑的 33 条决策台账逐字节一致。证据见 `Logs/verify/m08-100-20261123-1530/`、`...-1533/` 与 `docs/reports/m08-55-review-playmode-20261119.md`。独立只读复审 `task_3a6d66bdcf4d` 结论 ACCEPT；这不替代用户手动验收。
3. M08 战役回顾面板已在上述自动化路线中于 1615-04-01 与 1660-04-01 自动打开，标题、上下文、正文区块与确认动作均有断言。仍缺实体鼠标/键盘输入、EventSystem 命中、Player 构建与用户手动验收证据。百年终点粮食 0、连续短缺 53 个月：路线能到百年，但经营状态不健康。
4. M09 窗口拖动尚无真实硬件输入证据。已有合成输入结果不算通过。
5. `Assets/Tests/PlayMode` 现含 `M08CampaignReviewPlayModeTests`，覆盖上述 55/100 年路线；其他运行时路径仍按手工配方留证。

checkpoint 记录的 M08B D3 基线中，`unmanaged` 1000 种子均未到达 50 年或 100 年。这个结果是已接受的风险基线，不是长期生存通过；它与单条 SC-013 PlayMode 路线不矛盾。SC-021 与 SC-023 已集成进 `main`，但集成事实本身不代表所有相关运行时体验均已验收。

## Git 与协作状态

- 可在不同工作树并行开发。每张卡只安排一个写者，Unity 测试串行执行。
- 工作完成后，把改动整合到 `main` 并推送。推送后核实远端 ref。保留回滚快照分支，不把快照并入功能分支。
- `origin/panyeung/ui-m04-family` 的部分文件改动已在 `main`。直接合并旧分支历史会让场景和 UI 文件冲突。保留该引用，不重放旧历史。
- `checkpoint.md` 顶部原先引用旧分支和旧基线。本次文档分支已改为当前代码基线。合入前由独立复审检查这项更正。
- v1 演示运行说明见 [v1 Windows 演示包](docs/reports/v1-demo-package-20261118.md)。
- 下次启动 Unity 前，重新检查本工程 Editor 进程及 `-projectPath`，并核实 `unity pipeline list` 与任务卡状态。`coden` 执行测试。`tester` 只审代码与证据。
