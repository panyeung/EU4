# Project State - Sengoku Chronicle

Audit date: 2026-10-31 (Pacific/Honolulu)

## Accepted milestone

- M1 is accepted.
- M2 Play Demo is accepted, merged to `main` at `f3094e8`, and tagged `demo-M2`.
- M3 Town NPC Dialogue is accepted, merged to `main` at `2ca9463`, and tagged `demo-M3`.

## Current milestone

- M3 Characters and Relationships is accepted on `main` at `2ca9463` and tagged `demo-M3`, based on accepted M2 plus M3-01 through M3-03 work.
- Scene integration, verification, independent review, and user acceptance for M3 are complete. The independent read-only review found no Critical or Important findings; minor follow-ups are recorded below.
- The separate `C:\Users\panye\EU4-m3` worktree contains uncommitted M3-04 changes and remains untouched.

## Verification evidence

- Unity 6000.6.6f1 was open on this exact worktree with `FoundationScene` loaded.
- Unity script compilation completed; the current project `Logs/Editor.log` contains no C# compiler diagnostics. The EditMode suite loaded and ran, providing an additional compile check.
- `FoundationSceneValidator` wrote `Logs/SceneValidationReport.txt`: `RESULT: PASS; Errors: 0`. It found all scene objects and serialized controller/dialogue references and recorded the hierarchy and RectTransforms.
- Unity Test Runner EditMode: 136 passed, 0 failed.
- Unity Test Runner PlayMode: no tests are present.
- Manual Play Mode: from Kiyosu on 1560-04-01, travel preview to Atsuta showed a one-day trip and 1560-04-02 arrival. Atsuta opened with only Kuroda Iori in the NPC roster at relationship 0. Accepting the dialogue branch displayed “Trust is earned.” and relationship +10. Returning to the town and then the map preserved Atsuta and 1560-04-02.
- After the walkthrough and exiting Play Mode, the Editor Console counters showed 0 errors and 0 warnings.
- The independent review noted minor follow-ups: dialogue instances are not reclaimed/reset, character role-to-ID validation could be stricter, and the scene validator reports RectTransform values without enforcing layout bounds. It did not judge visual correctness of every layout.

## Worktree and next step

- The published `feature/m3-accepted-integration` branch is preserved at the accepted M3 checkpoint. The separate `C:\Users\panye\EU4-m3` M3-04 worktree remains untouched.
- `M3_DEMO.md` contains the user-facing acceptance path and evidence summary.
- The Kanban review tools were not available; the independent read-only review and user acceptance are recorded in this handoff.
- M4a is the active candidate; later milestones remain out of scope until M4a is accepted.

## M4a candidate update — 2026-11-01

M3 is accepted; M4a is the active candidate on `feature/m4a-supply-shortage-task` in its isolated worktree. The mission model, uGUI panel/controller, updater, validator, and EditMode coverage are implemented. The current-location entry defect and duplicate feedback-name regression have been corrected; Task 2 and Task 3 same-task reviews approved their scoped changes. All location buttons show only their town names, with current location identified by the separate marker; the user reported the visual re-check passed.

Latest existing-Editor EditMode run (2026-11-01 16:06:30Z): 214 total, 214 passed, 0 failed, 0 skipped. This includes exact current-town/noncurrent button-label assertions, the migration coverage, and three formerly failing M2 layout tests. The Editor log contains a successful Tundra build after the final label change and no C# compiler diagnostics. The user reported a pass after being asked to clear Console and play the mission; exact counters and runtime values were not supplied.

The reviewed updater migrated and saved `Assets/Scenes/FoundationScene.unity`, renaming the M4 feedback text to `MissionFeedbackText` to preserve the M2 map's unique `FeedbackText`. The fresh project-wide validator report is `RESULT: PASS; Errors: 0`, with all M4a references bound. The user reported pass for the Kiyosu → Atsuta → Kiyosu walkthrough and later visual re-check, then explicitly accepted the demo on 2026-11-01. Exact Console counters and runtime values were not supplied, and no independent runtime capture was made. Accepted checkpoint `9c10959e5792777ea433a16c590034f406dccba1` is tagged `demo-M4a` and fast-forwarded into local `main`; no remote push was made.

## Current status update — 2026-11-01

M2 is accepted per the user's latest milestone confirmation. M3 and M4a are also accepted; M4a checkpoint `9c10959e5792777ea433a16c590034f406dccba1` is tagged `demo-M4a` and present on local `main`. M4b is accepted by the user on 2026-11-01. The existing Editor compiled the latest code; EditMode Run All passed 322/322 with 0 failures or skips; the M4b updater saved `FoundationScene`; and the fresh validator report passed with 0 errors. The user reported the requested M4b playtest passed. Independent technical QA accepted the scoped implementation with no blocking defects. Exact runtime values and Console counters were not supplied. M4b checkpoint `70ac62f0d8a9866488ae194c3cf55c68a8be2e29` is tagged `demo-M4b` and fast-forwarded into local `main`; no remote push was made. Kanban review tools were not available in this session.

## M5 accepted update — 2026-11-01

M1–M4b remain accepted. M5 (versioned single-slot save) is implemented on `feature/m5-save-slot` and was accepted by the user's Play Mode run on 2026-11-01.

Implementation: a new `SengokuChronicle.Persistence` assembly holds `SaveData` (`saveFormatVersion = 1`), `SaveDataValidator`, and `SaveSlotStore`; `GameSession` captures and atomically applies date, location, mission stages, and NPC relationships, with merit derived from stages. A compact `SaveLoadPanel` (`M5SaveLoadRoot`) was added to `FoundationScene` through `AddM5SaveLoadPanel`. Design and plan: `docs/superpowers/specs/2026-11-01-m5-single-slot-save-design.md` and `docs/superpowers/plans/2026-11-01-m5-single-slot-save.md`.

Verification evidence (2026-11-01):

- Compilation: Unity 6000.6.6f1 batchmode imported and compiled this worktree with no `error CS` lines in `m5-batchmode.log`.
- EditMode: the final integrated run (`m5-final-editmode.xml`) reports 381 total, 381 passed, 0 failed, 0 inconclusive, 0 skipped (start 17:47:34Z); the earlier pre-scene run reported the same totals.
- FoundationScene: the updater saved the scene; the diff adds 536 lines and removes none. The fresh `Logs/SceneValidationReport.txt` reports `FOUND SaveLoadPanel: M5SaveLoadRoot`, the M5 hierarchy/binding/separation check passing, and `RESULT: PASS; Errors: 0`.
- Console: not captured. Batchmode logs are not the Editor Console, so no Console counters are claimed.
- Play Mode: the user ran the acceptance path in the Editor on this worktree and reported `pass` on 2026-11-01. The slot written during that run is `save-slot-1.json` (684 bytes, 10:54 local), with `saveFormatVersion: 1`, date `1560-04-05`, location `owari.kiyosu`, `supplyShortageStage: 4`, the other two stages at `0`, and five relationship entries at `0`. Location and relationships were not changed in this run, and the load/no-duplicate-reward/Console details were not independently captured.
- Remote state (verified 2026-11-01): `origin/main` is `24102fe`; tags `demo-M2`, `demo-M3`, `demo-M4a`, and `demo-M4b` exist on `origin`.

Next step: the M5 checkpoint is merged to `main` and tagged `demo-M5` after the user's acceptance.

## M6 candidate update — 2026-11-01

M1–M5 remain accepted. M6 (title screen & Phase 1 demo packaging) is implemented on `feature/m5-save-slot` (same branch and worktree per the user) and is a demo candidate, not accepted.

Implementation: `TitleScreenController` boots Play Mode to a full-screen title overlay (「新游戏」/「继续」, status line), wiring 继续 to the M5 single slot with graceful missing/corrupt/unsupported-version handling and no session mutation on failure; `DemoSummaryPanel` + `GameSession.IsDemoComplete` add the demo closing line on the map; `AddM6TitleScreen` is the transactional scene updater and `FoundationSceneValidator` checks M6 hierarchy/bindings. Design and plan: `docs/superpowers/specs/2026-11-01-m6-title-demo-packaging-design.md` and `docs/superpowers/plans/2026-11-01-m6-title-demo-packaging.md`.

Verification evidence (2026-11-01): batchmode compile without `error CS`; EditMode 401/401 passed (0 failed, 0 skipped) both pre-integration (21:27:53Z) and post-integration (21:45:01Z); `RunBatchIntegration` saved FoundationScene (+764/−12 lines, the removals are Unity re-serialization of existing blocks); `Logs/SceneValidationReport.txt` reports the M6 PASS line and `RESULT: PASS; Errors: 0`. Console counters and Play Mode are not yet captured; see `M6_DEMO.md`.

## M6 accepted update — 2026-11-01

M1–M5 remain accepted. M6 (title screen & Phase 1 demo packaging) is implemented on `feature/m5-save-slot` and was accepted by the user's Play Mode run on 2026-11-01 (`pass`). Exact runtime values and Console counters were not supplied; the acceptance rests on the user's report alongside the recorded compile, EditMode 401/401 (both runs), integration, and validator evidence.

Next step: the M6 checkpoint is merged to `main` and tagged `demo-M6` after the user's acceptance.

## M7 accepted update — 2026-11-01 (Phase 2)

M1–M6 remain accepted; `demo-M6` is merged and pushed. M7 (功名晋升 / merit & promotion) is the first Phase 2 milestone, implemented on `feature/m5-save-slot` (user directive) and accepted by the user's Play Mode `pass` on 2026-11-01.

Implementation: pure-C# `PromotionRules` maps total merit to four game-abstracted ranks (足轻0/番头5/组头10/武士头20 — simplified growth labels, not historical offices); `GameSession.Rank/RankName` are derived (no save-format change); `RankPanel` adds a permanent map-screen line with next-rank threshold; the M6 demo summary now names the final rank; `AddM7RankPanel` is the transactional scene updater and the project validator checks M7. Design/plan: `docs/superpowers/specs/2026-11-01-m7-merit-promotion-design.md`, `docs/superpowers/plans/2026-11-01-m7-merit-promotion.md`; roadmap in `PHASE2_ROADMAP.md`.

Verification evidence (2026-11-01): batchmode compile without `error CS`; EditMode 425/425 passed (0 failed, 0 skipped) pre-integration (22:19:48Z) and post-integration (22:36:35Z); `RunBatchIntegration` saved FoundationScene (+96/−1; the single removal is Unity re-normalizing the inert `m_MaxSize` on `M6TitleLabel`, `m_BestFit: 0`); `Logs/SceneValidationReport.txt` reports the M7 PASS line and `RESULT: PASS; Errors: 0`. The user ran the Play Mode acceptance path and reported `pass` on 2026-11-01; exact rank values and Console counters were not independently captured.

## 当前方向更新 — 2026-11-01

用户已将项目方向改为 [《影之一族》总计划 v4.0](GDD_v4.0.md)。M1–M7 是旧方向的历史检查点，不代表新方向的实现进度。用户要求只使用 `C:\Users\panye\My project` 一个 Unity 项目；继续沿用 Unity `6000.6.6f1`，不得另建项目。

SC-001 基线审计已完成并记录于 `docs/SC001_BASELINE_AUDIT.md`。SC-002 静态检查确认复用现有 Domain、不新增平行 Core；Domain 12 个源码文件未发现 Unity API 使用，程序集依赖无环且方向向内。独立 QA 发现旧 asmdef 使用无效字段 `useUnityAssemblies: false`，Unity 生成的 Domain 项目仍包含 UnityEngine 引用。OpenCode 已将其替换为 `noEngineReferences: true`；修改后的 Editor 编译和 EditMode 复验仍待完成。

Unity 证据：确认唯一现有 Editor PID `56392` 的 `-projectpath` 指向指定项目，窗口标题为 `FoundationScene`。用户在该 Editor 的 Test Runner 中运行 EditMode Run All；`TestResults.xml`（2026-11-01 14:44:33）报告 425/425 通过，0 失败、0 跳过、0 不确定。相同时间的 `Logs/Editor.log` 未发现 C# 编译错误标记，但含 Unity Licensing 404 与 AI ModelSelector `NoSubscription` 错误信息。通过 Orca 无截图可访问性状态检查未读到 Console 控件，窗口报告位于屏幕外；因此 Console 红色错误计数仍未核实，不得报告为零。

SC-002 OpenCode 独立只读 QA（Run `run_8f5c03efe244`、Task `task_f1da208a9c75`）发现 1 项中等 asmdef 边界缺陷；实现任务 `task_b3c0e82c7a58` 完成最小修正，独立复核任务 `task_d39b0acf9744` 接受该单行变更。修正后的 Domain 生成 `.csproj` 已不含 UnityEngine/UnityEditor 引用；新 Editor.log 中 C# 编译错误为 0。用户在同一 Editor 重跑 EditMode，TestResults.xml 于本地 2026-11-01 15:10:44 更新，425/425 通过。用户报告本次 Unity Console 红色错误数为 0；Editor.log 同时含 Licensing 404 和 AI `NoSubscription` 服务日志，两类证据分别记录。SC-002 不包含可玩演示；本次检查点仅提交并推送功能分支，不合并到 main 或打标签。Kanban/QATester 正式接口不可用，独立 OpenCode 技术复核已接受，不冒充 Kanban 状态。场景验证和 Play Mode 不属于 SC-002；本状态不表示 M0 已完成或新方向已有可玩内容。下一项设计顺序为 GDD 列表中的 SC-003。
