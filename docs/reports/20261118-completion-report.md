# 《影之一族》完成情况报告 — 2026-11-18

**状态：未完成（PARTIAL）。** 测试报告里仍有未闭合的 [MAJOR] 缺口，本项目**不能**按「已完成」交付：四个任务工作树未合并进 `main`、M00 用户最终验收未完成、M05 独立复审未通过、F1 城镇设施槽的实现当前不在 `main`（报告说做完了，树里没有）。另有 12 条 [MINOR] 与 2 条按设计明确不做的条目。

本文只记录其他任务实际报告过的结果，外加本卡亲手读回的值。标「本卡复核」的是我这次实际执行的检查。

---

## 1. 现在能做什么

- 能构建：StandaloneWindows64 发行构建 `Succeeded`，产物 `Builds/StandaloneWindows64/My project.exe`，183,831,693 B，耗时约 200 s，buildId `build_c5656da0bcf5`。来源 `20261118-build-dogfood-defects.md` §2；本卡复核 exe 667,136 B 存在。
- 能启动：成品 Player 起得来，DX12 + RTX 4070，`Player.log` 里 `exception|LogError|NullReference` 命中 0。来源同上 §3a；本卡复核该日志命中数仍为 0。
- 能从标题玩到月结：标题 →「开始新游戏」→ 剧本选择 → SC-013 营地 HUD（1560-04-01 / 清洲 / 6 人 / 资金 30 / 物资 12）→「结束月份」推进到 1560-05-01 且资金 30→28 → 城镇当地委托可交互。核心循环判 PASS。来源同上 §3b。
- 能存读档往返：「存档 → 推进一天 → 读档回到存档时」实测通过（1560-04-01 → 1560-04-02 → 读回 1560-04-01），空槽位干净拒绝（`尚未找到存档。`，日期不变）。来源 `20261118-compile-console-blockers.md` §3 第 5–8 步。
- 全量 EditMode 曾全绿：2141/2141（两次独立作业）+ 2142/2142（含 F1 新用例；该次源码现已不在树中，见第 2 节 F1 行）。
- 编译 0 个 `error CS`、0 个 `warning CS`；场景 Validator `RESULT: PASS; Errors: 0`。本卡复核 `Logs/SceneValidationReport.txt` 末行一致。

## 2. 修好了什么

| 项 | 处置 | 依据 |
|---|---|---|
| 审计 B1「DebugSession 在正常开局被激活，存档被拒绝」 | **判为非缺陷并闭合**。全工程唯一把会话转成调试会话的入口是标题页 `M09DebugModeEntry` 或 `TitleScreenController.StartDebugSession()`；正常开局全路径 `isDebugSessionActive=False`，存档可用；`DebugExit` 会把标记清回 False。里程碑那次 `BLOCKED` 只可能来自当时 Play 会话里点开过调试入口 | `20261118-compile-console-blockers.md` §3 十步读回表；`20261118-build-dogfood-defects.md` §3b 独立复现同一结论 |
| 审计 B2 SAVE-1/SAVE-2（存 → 改 → 读） | **CLOSED（本轮复测）** | 同上 §3 第 5–7 步 |
| 审计 B2 SAVE-4（空槽干净拒绝） | **CLOSED（实测）** | 同上 §3 第 8 步 |
| 审计 B2 SAVE-3（待决继承跨存读档） | **部分闭合**：会话层等价用例 `SuccessionSaveTests.CaptureSave_AtPendingDecision_RoundTripsPhaseAndCandidates` 覆盖，Play Mode 级未单独跑 | 同上 §6 |
| 编译与 Console 阻塞项 | 实测干净：`recompile=up_to_date`、`compilationFailed=false`、`error CS` 0、`warning CS` 0；全流程 Play Mode 交互**零新增** console 条目。运行时唯一 `Debug.LogError` 调用点是 `GameSession.cs:3887`（编年史追加真失败才触发），全工程运行时脚本 0 处 `Debug.LogException` | `20261118-compile-console-blockers.md` §1–§2 |
| 审计 F1「城镇设施全部为占位未开放」 | **做过、复审通过、但现在不在树里**。`t_343f7b6d` 的实现（新增 `WorldMapController.RefreshFacilitySlots()`，删掉写 `"{label}：未开放"` 并强制禁用的循环）与两条新用例曾让全量达到 2142/2142，`tester` 复审 approved。本卡复核：`main` 的 `WorldMapController.cs` 里 `RefreshFacilitySlots` 命中 0、「未开放」命中 5，两条 F1 用例都不在树里，四个任务分支也都没有这份实现。唯一副本在快照提交 `f69295b`（`WorldMapController.cs` blob `01a0b97`、`WorldMapUiTests.cs` blob `6d288c0`、`TownActivityRoutingTests.cs` blob `50afc9e`），本卡已把该快照分支推送到 origin 以防再丢 | `20261118-feature-gaps-implementation.md` §2.3；本卡用 `git grep` 核了七个 ref |
| 审计 F2（SC-014/015/016 跨代链） | **按设计已具备，不做新运行时**。三条链在 `event-catalog-v1.json`，事件 ID 全为 `event.sc013.*`，由 `M2FixedScenario.IsScenarioEvent` 做剧本门禁，权重由 `SC018IndependentFamilyEventsTests` 断言 | 同上 §1 |
| 审计 F4（长期模拟存活样本 0） | **口径已写明，无代码缺口**。决策 D3 定为 v1 口径并注明「这不是生存通过」 | 同上 §1；`docs/实现规格-v1.0.md` D3 |

## 3. 测试通过验证了什么，没验证什么

验证过的：

- 编译：0 error / 0 warning CS（Editor.log 全文 grep）。来源 `20261118-compile-console-blockers.md` §1。
- 全量 EditMode：作业 `d95f750dc6624ac5a3fe521e432c818d` → 2141/2141、failed 0、skipped 0（06:10:51Z–06:12:17Z）；副本 `Logs/debug-leak-20261118/TestResults.xml`。本卡复核该 XML：`testcasecount="2141" passed="2141" failed="0" result="Passed"`。
- 全量 EditMode（含 F1）：`testcasecount=2142 passed=2142 failed=0`（06:23:57Z–06:28:10Z）；副本 `Logs/verify/20261118-features/TestResults-editmode-full-2142.xml`。本卡复核该 XML 同为 2142/2142/0。**注意**：这次 2142 的源码已不在 `main`，该结果不能再从当前树复现。
- 里程碑那一轮另有 2141/2141，副本 `Logs/verify/20261118-milestone/TestResults.xml`。本卡复核同为 2141/2141/0。
- 场景 Validator：`RESULT: PASS; Errors: 0`。本卡复核 `Logs/SceneValidationReport.txt` 末行一致。
- 构建与成品运行：见第 1 节。
- 存档安全：真实槽 `save-slot-1.json` sha256 `1e61ed9cab…` 全程未变。dogfood 卡里有一次误写默认槽（隔离重定向 BLOCKED 后 `SaveThroughPanel` 落到真实槽），已用 `.bak` 恢复并复核 hash 与 mtime 回到基线。

**没有**验证过的（不得推断为通过）：

- 对当前 `HEAD`（`06e3eba`，在 `5cd8521` 之上只增文档）重跑全量测试：没有。所有测试结果都产生在合并前的工作树状态上。
- 成品 exe 的逐屏 GUI 点击：headless 会话里 `computer_use` 的 click/focus 被审批门控拦截，交互改由 Editor Play Mode 承担。来源 `20261118-build-dogfood-defects.md` §6。
- 完整隔离槽存/推进/读闭环：dogfood 那轮因 Editor 主线程卡死未独立复现，引用的是 `t_dd30ab75` 的 Play Mode 证据。
- M09 真机窗口拖动、用户最终验收。
- 构建产物与 `main` HEAD 的对应关系：那次构建基于含未提交改动的工作树（起点 `a40cf5b` + 35 修改 / 39 未跟踪），构建报告里 `building a player, but you have uncompiled code changes` 就是这个事实。

## 4. 已知缺口（Known gaps / not done）

| # | 严重度 | 缺口 | 为什么还在 |
|---|---|---|---|
| 1 | MAJOR | 四个任务工作树未合并进 `main`：`ui-m04-family` 840b2dd、`ui-m05-management` b1d1062、`ui-m06-m07-missions` d57ea7c、`ui-m08-campaign` a25c969（本卡按 `git worktree list` 复核） | 本批任务没有集成卡。场景不参与三方合并，需在 `main` 侧重放 updater 并重跑 Validator。审计 B3 |
| 2 | MAJOR | M00 用户最终验收未完成 | 验收权在用户。审计 B5 |
| 3 | MAJOR | M05 独立复审未通过 | 唯一一次独立复审判 FAIL（t_e2ddb378）：C2 有 3 处公开成员零断言；C5 不成立，裁切/滚动实现与三条回滚测试只在未提交工作区，已推送的 2f8b7b2 里 `ScrollRect` 命中 0。审计 B4 |
| 4 | MAJOR | F1 城镇设施槽的实现不在 `main`，也不在任何任务分支 | 实现随工作树变化丢失，只有快照 `f69295b` 里有。恢复与重测另开卡（见第 5 节第 1 条） |
| 5 | MAJOR | `main` 工作树的在途副本被外部重置清掉 | `git reflog` 显示 2026-11-17 21:17:53 有一次 `reset: moving to HEAD`。同期集成卡 `t_ec27c2df` 在通知里记「已退回 5cd8521」（`Logs/notifications/outbox.txt` 07:18:50Z），时间与 reflog 吻合，但两条记录的因果未逐条核实。本卡开工时索引里仍有 M04 的在途新增文件（`FamilyKinshipPath.cs`、`HouseholdIndustryFit.cs`、`CanvasLayerOrder.cs` 等），21:22 已不在工作树。这些文件在 `panyeung/ui-m04-family` 上已提交，可从该分支取回，不是永久损失 |
| 6 | MINOR | SAVE-3 只有会话层等价覆盖 | Play Mode 级未单独跑（Editor 卡死与单写者限制） |
| 7 | MINOR | M09 窗口拖动只在合成输入下验过 | 需真实硬件与可交互桌面。审计 B6/P8 |
| 8 | MINOR | 构建含 `uncompiled code changes` 警告 | 构建基于脏工作树，产物与 `main` HEAD 不完全对应 |
| 9 | MINOR | 成品约 22 条 URP 后处理着色器被剥离 | Bloom/景深/运动模糊在构建产物里不生效，属变体裁剪 |
| 10 | MINOR | 城镇菜单三个 `Facility1..3Text` 场景节点仍在 | 运行时文本与禁用状态跟随第 4 条的实现；实现不在树里时它们退回「未开放」占位。删节点属场景改动，需同步 `WorldMapLayout.cs`、`UpdateM2WorldMapLayout.cs`、`CreateM2WorldMap.cs`。来源 `20261118-feature-gaps-implementation.md` §6 |
| 11 | MINOR | 世界地图屏「NPC 自主性」按钮与「清洲」卡片局部重叠 | 目视记录，未判定是否为设计意图 |
| 12 | MINOR | 剧本选择页两条按钮显示名均为「SC-010」 | 内部标识为 SC-013 样例，命名与文案不一致，未改 |
| 13 | MINOR | 选 SC-013 后单帧读到「日期未初始化 / 人数 —」 | 面板切换期绑定时序，单帧观察，未稳定复现 |
| 14 | MINOR | 无音频；美术为生成图，无切图、无动画 | 决策 D5 不进 v1 |
| 15 | MINOR | `Assets/Tests/PlayMode` 为空 | v1 运行时验证靠手工 Play Mode 配方，规格列为非目标 |
| 16 | MINOR | `Assets/Scenes/` 内有 stray `TestResults_*.xml` | 清理需批准（P4） |
| 17 | MINOR | 无 CI / 打包流水线 | 唯一构建路径是已开 Editor + Pipeline bridge，v1 只验一次发行构建（P7） |

审计的 P1–P7 polish 与第 6–17 条是同一批条目，不重复列。

**按设计不做、不算缺口**：SC-014/015/016 三条跨代链不写新运行时（规格非目标）；长期模拟存活样本 0 是决策 D3 的 v1 口径。

## 5. 建议的下一步顺序

1. 先修 Editor。本卡末段 `unity command editor_status` 无输出返回；另卡记录 PID 96584/7803 的 pipeline 队列被失败的 `run_tests` 占死并停在 Play Mode 的对话框上，另有 PID 53876 需关闭（`Logs/notifications/outbox.txt` 07:27:03Z，卡 `t_e30a06f3`）。Editor 不恢复，下面任何重测都跑不了。
2. 恢复 F1 并重测（缺口 4）。从 `f69295b` 取回 `Assets/Scripts/Presentation/WorldMapController.cs`（blob `01a0b97`）与 `Assets/Tests/EditMode/WorldMapUiTests.cs`（blob `6d288c0`）。`TownActivityRoutingTests.cs` 的第二条 F1 用例（快照 blob `50afc9e`）需人工挑拣，因为 `main` 上这个文件已含 M05 的其他内容。恢复后跑全量 EditMode，确认两条 F1 用例都通过。已开卡 `t_20c3719b`（`coden`）。
3. 先处理第 5 条的并发事故再谈合并：确认主工作树只有一个写者，按需从 `panyeung/ui-m04-family` 取回在途文件。
4. M05 复审收口（缺口 3）：先把裁切/滚动实现与三条回滚测试提交，再重派 `tester` 复审。
5. 按 M04 → M05 → M06/M07 → M08 顺序合并进 `main`，每次合并后重跑 Validator 与全量 EditMode（缺口 1）。做这件事的卡 `t_ec27c2df` 上一轮因 Editor 死锁退回，需先解掉第 1 条。
6. 在干净的 `main` 上重做一次发行构建，消掉缺口 8；随后用户最终验收（缺口 2），再打里程碑 tag。

## 6. 本次任务的边界

- 只改文档：`README.md` 重写为接手文档，新增本报告，并把三份上游报告按快照原样回填到 `docs/reports/`。未改任何产品代码、场景、预制体、`ProjectSettings`、包。
- 三份上游报告（`20261118-project-audit-gaplist.md`、`20261118-compile-console-blockers.md`、`20261118-feature-gaps-implementation.md`）此前不在工作树里，只存在于提交 `f69295b`。本卡从该提交按字节原样取出写入 `docs/reports/`，未做任何文字修改，三份的 `git hash-object` 与快照 blob 完全一致。
- 本卡把本地分支 `snapshot/main-tree-20261118`（`f69295b`）推送到 origin。理由：F1 的唯一副本与 M04 在途文件都只在该快照里，只存本地有丢失风险。这不是合并，`main` 未被动过。
- 未修复 F1，未重跑任何测试，未动任何分支的合并。恢复与重测另开卡派给 `coden`。
- 一处口径不一致，如实记录：dogfood 卡的交接 metadata 记 `minor=7`，而其报告正文逐条列出 5 条 [MINOR]（另 2 条是「3 条 error + 513 warning 非产品缺陷」的归类）。本文以报告正文与后续卡片为准，不采用 7。

## 7. 来源

- `docs/reports/20261118-project-audit-gaplist.md` — 审计与缺口清单（t_174d3bc3，scout，只读）。
- `docs/reports/20261118-compile-console-blockers.md` — 编译/Console 阻塞项处理（t_dd30ab75，coden）。
- `docs/reports/20261118-feature-gaps-implementation.md` — F1–F4 处置（t_343f7b6d，coden；tester 复审 approved）。
- `docs/reports/20261118-build-dogfood-defects.md` — 构建、核心循环 dogfood 与缺陷清单（t_9857270d，tester）。
- `docs/reports/20261118-playable-milestone.md` — v1 可玩里程碑七条分项（t_3193e0ba，主控）。
- `docs/console-基线归因表.md` — Console 每条 error 的归因。
- 任务卡 t_e2ddb378 — M05 独立复审结论（FAIL，无独立报告文件）。
- 提交 `f69295b`（分支 `snapshot/main-tree-20261118`，已推送 origin）— 20261118 集成前的主工作树完整快照。
