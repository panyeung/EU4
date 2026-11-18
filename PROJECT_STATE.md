# 项目状态

更新时间：2026-11-18（UTC）
状态：IN PROGRESS

本文件记录当前工作状态。设计目标、里程碑和测试口径以 [checkpoint.md](checkpoint.md) 为准。界面与美术规则以 [ui设计规划.md](ui设计规划.md) 为准。

## 当前基线

- 仓库：`https://github.com/panyeung/EU4.git`。
- `main` 与 `origin/main` 同步。游戏代码基线为 `2bca9d1`。之后的主线提交只更新文档。本次文档分支从 `main@23ef2cc` 建立。
- Unity 版本：`6000.6.6f1`，URP 2D，Windows。
- 主工作树：`C:\Users\panye\My project`，基线工作区干净。本文件最初在 `feature/project-state-docs` 独立工作树编写，以免干扰 v1 演示签核。
- 当前没有可连接的本工程 Unity Editor。`unity command editor_status` 返回未找到实例。不得据此启动 batchmode。

## 当前阶段

**v1 Windows 演示待技术签核与用户验收。** 演示包可运行，但整项目尚未完成。采集本状态时，`tester` 仍在复审 `t_39a5b047`，尚未给出 ACCEPT 或 RETURN。签核通过后，我会交付演示包和一到两分钟试玩步骤。然后等待用户反馈，再开始下一阶段。

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

1. 完成 `t_39a5b047` 的独立技术签核，随后交付 v1 演示并等待用户验收。
2. M08 验证未闭合。`t_0a1e8ce8` 当前为 blocked，EditMode、场景 Validator、Play Mode 和 M08B 长期模拟都缺本轮证据。卡片记录的旧 Editor PID 已不存在，目前也没有可连接的 Editor。进入该阶段时，先重查 Editor，再由 `coden` 启动唯一实例。模拟产物写入 `Logs/verify/m08b/<policyId>/`。报告只说明当前基线、策略 ID 和种子数，不写生存率结论。
3. M08 战役回顾面板尚无真实 55 年回顾或 100 年结局运行时打开证据。不可用调试跳日期替代。
4. M09 窗口拖动尚无真实硬件输入证据。已有合成输入结果不算通过。
5. `Assets/Tests/PlayMode` 为空。运行时路径仍按手工配方留证。

checkpoint 记录的 M08B D3 基线中，`unmanaged` 1000 种子均未到达 50 年或 100 年。这个结果是已接受的风险基线，不是长期生存通过。SC-021 与 SC-023 已集成进 `main`，但集成事实本身不代表所有相关运行时体验均已验收。

## Git 与协作状态

- 可在不同工作树并行开发。每张卡只安排一个写者，Unity 测试串行执行。
- 工作完成后，把改动整合到 `main` 并推送。推送后核实远端 ref。保留回滚快照分支，不把快照并入功能分支。
- `origin/panyeung/ui-m04-family` 的部分文件改动已在 `main`。直接合并旧分支历史会让场景和 UI 文件冲突。保留该引用，不重放旧历史。
- `checkpoint.md` 顶部原先引用旧分支和旧基线。本次文档分支已改为当前代码基线。合入前由独立复审检查这项更正。
- v1 演示运行说明见 [v1 Windows 演示包](docs/reports/v1-demo-package-20261118.md)。
- 下次启动 Unity 前，重新检查本工程 Editor 进程及 `-projectPath`，并核实 `unity pipeline list` 与任务卡状态。`coden` 执行测试。`tester` 只审代码与证据。
