# M00 收口证据包（2026-11-17）

本目录把 M00 收口的六类证据从被 gitignore 的 `Logs/verify/` 拷进版本控制，便于在 PR 里直接复盘。原文路径保留在每份文件的开头。

## 逐类结论

| 类别 | 判定 | 来源 | 我亲自核过的部分 |
|---|---|---|---|
| 编译 | 通过 | `01-gate-report.md` | `editor_status` 报 `compiling=false`、`domainReloadInProgress=false`，本目录 03 号文件 C1 段 |
| 场景 Validator | PASS；Errors: 0 | 本分支 `Logs/SceneValidationReport.txt`（2026-11-17 17:47:31，1242 行） | 整文件只有一行判定，场景 `FoundationScene`，`Loaded: True; Dirty: False` |
| EditMode 全量 | 2141 / 2141，Failed 0 | 三次独立运行：16:12（`01-gate-report.md`）、17:48–17:52（`04` 号文件）、18:0x（`05` 号文件） | 结果文件副本的 `testcasecount` 与 `passed` 字段 |
| Play Mode 冒烟 | 到 SC-013 营地 HUD，`1560 年 4 月 1 日` / `清洲`，期间新增 error 0 | `01-gate-report.md` | 未重跑（Editor 已卡死），采信该文件的分项表 |
| 存读档 SAVE-1 / SAVE-2 | PASS | `02-save-isolated-SAVE-1-2.md` | 分项表 C0–C8，隔离槽 `ISO-PlayTest-20261118`，v17 / rules 8 |
| 存读档 SAVE-3 / SAVE-4 | PASS | `03-save-gate-SAVE-3-4.md`（17:28–17:39 实测） | 逐条核对读回值：`pendingSuccessorId=person.akechi.tokihime`、`phase=AwaitingRegentChoice`；空槽反馈 `尚未找到存档。`、`lastOk=False`、日期与人数不变 |
| 真实存档保护 | 通过 | 02/03 两份文件 | 前后 `sha256` 与 mtime 四组全等：`1e61ed9c…c92c`（16435 B）、`f1c0630e…4cbe`（8097 B） |

## 为什么还要留下两份 BLOCKED 记录

`04-attempt-1745-blocked.md` 与 `05-attempt-1740-blocked.md` 是 SAVE-3 / SAVE-4 的两次**复验尝试**，都以 BLOCKED 收尾。它们不推翻 17:28–17:39 那次 PASS，但要说清原因，否则下一次读到这里的人会以为功能坏了。

根因是并发事故，不是产品缺陷。`Logs/Editor.log` 里的卡死栈是 `Unity.Pipeline.Editor.Testing.PipelineTestRunner/<>c:<ExecuteSyncMode>b__6_0` + `CancellationTokenSource:NotifyCancellation`：一个会话已经进入 Play Mode，另一个会话同时提交了**同步模式**的 `run_tests`，测试框架在 Play Mode 里做场景还原失败，Editor 主线程从此不再响应任何 pipeline 命令（`editor_status` / `console_status` / `editor_play` / `editor_stop` 全部 30–90 s 超时），而进程本身 `Responding=True`、HTTP 端点 2.1 ms 返回 401。

后果与边界：

- 被测功能没有失败的证据；失败的是「同一时刻只允许一个写者」这条纪律，事故期间 `checkpoint.md` 被并行会话提交了三次（`55fb764`、`7d45c43`、`ca96f4f`）。
- 恢复方式只有一个：**由用户重启那个 Editor**。项目规则禁止按进程名杀 Editor，本轮没有杀，也没有重启。
- 重启后若要独立复验 SAVE-3 / SAVE-4，按 `docs/实现规格-v1.0.md` 的 S1 步骤，用 `scripts/verify/SaveGateProbe.cs` 的两个注入点（`SaveLoadPanel.SetStore` + 场景内 `TitleScreenController.SetStore`）重定向到空目录，再走 `03` 号文件的同一组读回断言。

## 本目录各文件

- `01-gate-report.md`：16:12 那次的门禁报告，含编译、Validator、EditMode、Play Mode、Console 分项表。
- `02-save-isolated-SAVE-1-2.md`：隔离槽「保存 → 推进一天 → 读回」三值一致。
- `03-save-gate-SAVE-3-4.md`：SAVE-3 待处理继承决定跨存读档存活、SAVE-4 空槽位干净拒绝，含分项表 C0–C7。
- `04-attempt-1745-blocked.md`：本分支 coden 的复验尝试（Validator PASS、EditMode 2141/2141、真实槽指纹一致；Play Mode 起不来）。
- `05-attempt-1740-blocked.md`：另一路复验尝试，含 Editor 卡死的栈与时间线。
