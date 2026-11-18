# 《影之一族》完成情况报告 — 2026-11-18

**状态：未完成（PARTIAL）。** 测试报告里仍有 3 条未闭合的 [MAJOR] 缺口，本项目**不能**按「已完成」交付：四个任务工作树未合并进 `main`、M00 用户最终验收未完成、M05 独立复审未通过。另有 12 条 [MINOR] 与 2 条按设计明确不做的条目。

本文只记录其他任务实际报告过的结果。每条结论后面标出来源；标「本卡复核」的是我这次亲手读回的值。

---

## 1. 现在能做什么

- 能构建：StandaloneWindows64 发行构建 `Succeeded`，产物 `Builds/StandaloneWindows64/My project.exe`，183,831,693 B，耗时约 200 s，buildId `build_c5656da0bcf5`。来源 `20261118-build-dogfood-defects.md` §2；本卡复核 exe 667,136 B 存在。
- 能启动：成品 Player 起得来，DX12 + RTX 4070，`Player.log` 里 `exception|LogError|NullReference` 命中 0。来源同上 §3a；本卡复核该日志命中数仍为 0。
- 能从标题玩到月结：标题 →「开始新游戏」→ 剧本选择 → SC-013 营地 HUD（1560-04-01 / 清洲 / 6 人 / 资金 30 / 物资 12）→「结束月份」推进到 1560-05-01 且资金 30→28 → 城镇四条当地委托可交互。核心循环判 PASS。来源同上 §3b。
- 能存读档往返：「存档 → 推进一天 → 读档回到存档时」实测通过（1560-04-01 → 1560-04-02 → 读回 1560-04-01），空槽位干净拒绝（`尚未找到存档。`，日期不变）。来源 `20261118-compile-console-blockers.md` §3 第 5–8 步。
- 全量 EditMode 全绿：2141/2141（两次独立作业）+ 2142/2142（含 F1 新用例）。编译 0 个 `error CS`、0 个 `warning CS`；场景 Validator `RESULT: PASS; Errors: 0`。

## 2. 修好了什么

| 项 | 处置 | 依据 |
|---|---|---|
| 审计 B1「DebugSession 在正常开局被激活，存档被拒绝」 | **判为非缺陷并闭合**。全工程唯一把会话转成调试会话的入口是标题页 `M09DebugModeEntry` 或 `TitleScreenController.StartDebugSession()`；正常开局全路径 `isDebugSessionActive=False`，存档可用；`DebugExit` 会把标记清回 False。里程碑那次 `BLOCKED` 只可能来自当时 Play 会话里点开过调试入口 | `20261118-compile-console-blockers.md` §3 十步读回表；`20261118-build-dogfood-defects.md` §3b 独立复现同一结论 |
| 审计 B2 SAVE-1/SAVE-2（存 → 改 → 读） | **CLOSED（本轮复测）** | 同上 §3 第 5–7 步 |
| 审计 B2 SAVE-4（空槽干净拒绝） | **CLOSED（实测）** | 同上 §3 第 8 步 |
| 审计 B2 SAVE-3（待决继承跨存读档） | **部分闭合**：会话层等价用例 `SuccessionSaveTests.CaptureSave_AtPendingDecision_RoundTripsPhaseAndCandidates` 覆盖，Play Mode 级未单独跑 | 同上 §6 |
| 编译与 Console 阻塞项 | 实测干净：`recompile=up_to_date`、`compilationFailed=false`、`error CS` 0、`warning CS` 0；全流程 Play Mode 交互**零新增** console 条目。运行时唯一 `Debug.LogError` 调用点是 `GameSession.cs:3887`（编年史追加真失败才触发），全工程运行时脚本 0 处 `Debug.LogException` | `20261118-compile-console-blockers.md` §1–§2 |
| 审计 F1「城镇设施全部为占位未开放」 | **代码侧已实现**。新增 `WorldMapController.RefreshFacilitySlots()`：有城镇总览页或无会话时槽位清空并禁用；无总览页的旧场景按 `TownLocalActions.GetAvailable` 顺序填真实当地委托。删掉原写 `"{label}：未开放"` 并强制 `enabled=false` 的循环。用例 `TownMenu_FacilitiesLabeledNotOpenAndDisabled` 改写为 `TownMenu_FacilitySlotsListRealTownEntries` | `20261118-feature-gaps-implementation.md` §2.3；tester 复审 approved（t_343f7b6d review 轮 1） |
| 审计 F2（SC-014/015/016 跨代链） | **按设计已具备，不做新运行时**。三条链在 `event-catalog-v1.json`，事件 ID 全为 `event.sc013.*`，由 `M2FixedScenario.IsScenarioEvent` 做剧本门禁，权重由 `SC018IndependentFamilyEventsTests` 断言 | 同上 §1 |
| 审计 F4（长期模拟存活样本 0） | **口径已写明，无代码缺口**。决策 D3 定为 v1 口径并注明「这不是生存通过」 | 同上 §1；`docs/实现规格-v1.0.md` D3 |

## 3. 测试通过验证了什么，没验证什么

验证过的：

- 编译：0 error / 0 warning CS（Editor.log 全文 grep）。来源 `20261118-compile-console-blockers.md` §1。
- 全量 EditMode：作业 `d95f750dc6624ac5a3fe521e432c818d` → 2141/2141、failed 0、skipped 0（06:10:51Z–06:12:17Z）；副本 `Logs/debug-leak-20261118/TestResults.xml`。本卡复核该 XML：`testcasecount="2141" passed="2141" failed="0" result="Passed"`。
- 全量 EditMode（含 F1）：`testcasecount=2142 passed=2142 failed=0`（06:23:57Z–06:28:10Z）；副本 `Logs/verify/20261118-features/TestResults-editmode-full-2142.xml`。本卡复核该 XML 同为 2142/2142/0。
- 里程碑那一轮另有 2141/2141，副本 `Logs/verify/20261118-milestone/TestResults.xml`。本卡复核同为 2141/2141/0。
- 场景 Validator：`RESULT: PASS; Errors: 0`。本卡复核 `Logs/SceneValidationReport.txt` 末行一致。
- 构建与成品运行：见第 1 节。
- 存档安全：真实槽 `save-slot-1.json` sha256 `1e61ed9cab…` 全程未变。dogfood 卡里有一次误写默认槽（隔离重定向 BLOCKED 后 `SaveThroughPanel` 落到真实槽），已用 `.bak` 恢复并复核 hash 与 mtime 回到基线。

**没有**验证过的（不得推断为通过）：

- 成品 exe 的逐屏 GUI 点击：headless 会话里 `computer_use` 的 click/focus 被审批门控拦截，交互改由 Editor Play Mode 承担。来源 `20261118-build-dogfood-defects.md` §6。
- 完整隔离槽存/推进/读闭环：dogfood 那轮因 Editor 主线程卡死未独立复现，引用的是 `t_dd30ab75` 的 Play Mode 证据。
- M09 真机窗口拖动、用户最终验收。
- 本报告涉及的构建基于**含未提交改动的工作树**（起点 `a40cf5b` + 35 修改 / 39 未跟踪），不等于 `main` HEAD `5cd8521` 的干净构建。构建报告里那条 `building a player, but you have uncompiled code changes` 就是这个事实的告警。

## 4. 已知缺口（Known gaps / not done）

| # | 严重度 | 缺口 | 为什么还在 |
|---|---|---|---|
| 1 | MAJOR | 四个任务工作树未合并进 `main`：`ui-m04-family` 840b2dd、`ui-m05-management` b1d1062、`ui-m06-m07-missions` d57ea7c、`ui-m08-campaign` a25c969（本卡按 `git worktree list` 复核） | 本批任务没有集成卡。场景不参与三方合并，需在 `main` 侧重放 updater 并重跑 Validator。审计 B3 |
| 2 | MAJOR | M00 用户最终验收未完成 | 验收权在用户。审计 B5 |
| 3 | MAJOR | M05 独立复审未通过 | 唯一一次独立复审判 FAIL（t_e2ddb378）：C2 有 3 处公开成员零断言；C5 不成立，裁切/滚动实现与三条回滚测试只在未提交工作区，已推送的 2f8b7b2 里 `ScrollRect` 命中 0。审计 B4 |
| 4 | MINOR | SAVE-3 只有会话层等价覆盖 | Play Mode 级未单独跑（Editor 卡死与单写者限制） |
| 5 | MINOR | M09 窗口拖动只在合成输入下验过 | 需真实硬件与可交互桌面。审计 B6/P8 |
| 6 | MINOR | 构建含 `uncompiled code changes` 警告 | 构建基于脏工作树，产物与 `main` HEAD 不完全对应 |
| 7 | MINOR | 成品约 22 条 URP 后处理着色器被剥离 | Bloom/景深/运动模糊在构建产物里不生效，属变体裁剪 |
| 8 | MINOR | 城镇菜单三个 `Facility1..3Text` 场景节点仍在 | 运行时已清空并禁用；删节点属场景改动，需同步 `WorldMapLayout.cs`、`UpdateM2WorldMapLayout.cs`、`CreateM2WorldMap.cs`。来源 `20261118-feature-gaps-implementation.md` §6 |
| 9 | MINOR | 世界地图屏「NPC 自主性」按钮与「清洲」卡片局部重叠 | 目视记录，未判定是否为设计意图 |
| 10 | MINOR | 剧本选择页两条按钮显示名均为「SC-010」 | 内部标识为 SC-013 样例，命名与文案不一致，未改 |
| 11 | MINOR | 选 SC-013 后单帧读到「日期未初始化 / 人数 —」 | 面板切换期绑定时序，单帧观察，未稳定复现 |
| 12 | MINOR | 无音频；美术为生成图，无切图、无动画 | 决策 D5 不进 v1 |
| 13 | MINOR | `Assets/Tests/PlayMode` 为空 | v1 运行时验证靠手工 Play Mode 配方，规格列为非目标 |
| 14 | MINOR | `Assets/Scenes/` 内有 stray `TestResults_*.xml` | 清理需批准（P4） |
| 15 | MINOR | 无 CI / 打包流水线 | 唯一构建路径是已开 Editor + Pipeline bridge，v1 只验一次发行构建（P7） |

第 3、9、10 条之外，审计的 P1–P7 polish 与上述 MINOR 是同一批条目，不重复列。

**按设计不做、不算缺口**：SC-014/015/016 三条跨代链不写新运行时（规格非目标）；长期模拟存活样本 0 是决策 D3 的 v1 口径。

## 5. 建议的下一步顺序

1. M05 复审收口（缺口 3）：先把未提交的裁切/滚动实现与三条回滚测试提交，再重派 `tester` 复审。
2. 按 M04 → M05 → M06/M07 → M08 顺序合并进 `main`，每次合并后重跑 Validator 与全量 EditMode（缺口 1）。
3. 在干净的 `main` 上重做一次发行构建，消掉缺口 6。
4. 用户最终验收（缺口 2），随后打里程碑 tag。

## 6. 本次任务的边界

- 只改文档：`README.md` 重写为接手文档，新增本报告，并把三份上游报告按快照原样回填到 `docs/reports/`（见下）。未改任何产品代码、场景、预制体、`ProjectSettings`、包。
- 三份上游报告（`20261118-project-audit-gaplist.md`、`20261118-compile-console-blockers.md`、`20261118-feature-gaps-implementation.md`）此前不在工作树里，只存在于提交 `f69295b`（分支 `snapshot/main-tree-20261118`）。本卡从该提交按字节原样取出并写入 `docs/reports/`，未做任何文字修改，以便本文的引用都能在仓库内打开。
- 未提交、未推送，提交动作另行执行。
- 一处口径不一致，如实记录：dogfood 卡的交接 metadata 记 `minor=7`，而其报告正文逐条列出 5 条 [MINOR]（另 2 条是「3 条 error + 513 warning 非产品缺陷」的归类）。本文以报告正文与后续卡片为准，不采用 7。

## 7. 来源

- `docs/reports/20261118-project-audit-gaplist.md` — 审计与缺口清单（t_174d3bc3，scout，只读）。
- `docs/reports/20261118-compile-console-blockers.md` — 编译/Console 阻塞项处理（t_dd30ab75，coden）。
- `docs/reports/20261118-feature-gaps-implementation.md` — F1–F4 处置（t_343f7b6d，coden；tester 复审 approved）。
- `docs/reports/20261118-build-dogfood-defects.md` — 构建、核心循环 dogfood 与缺陷清单（t_9857270d，tester）。
- `docs/reports/20261118-playable-milestone.md` — v1 可玩里程碑七条分项（t_3193e0ba，主控）。
- `docs/console-基线归因表.md` — Console 每条 error 的归因。
- 任务卡 t_e2ddb378 — M05 独立复审结论（FAIL，无独立报告文件）。
