# SC-016 M2 委托旧约 Implementation Plan

> **For agentic workers:** Implement through the supervised Orca OpenCode2 worker required by this project; a separate OpenCode2 worker performs the read-only review after implementation.

**Goal:** Add the fifth GDD-aligned M2 cross-generation chain, recording the relationship consequences of a ledger-delivery commission from Harunobu's generation to Nobumasa's.

**Architecture:** Extend only the current JSON event catalog and localization data. Reuse existing event history, generation gates, relationship deltas, and chronicle recording; add no runtime code or save state.

**Tech Stack:** Unity 6.6 project; JSON event catalog and Chinese localization.

**Spec:** [SC-016 design](../specs/2026-11-03-sc-016-m2-commission-legacy-design.md)

## Global Constraints

- Stay in `C:\Users\panye\My project`; do not create another Unity project.
- OpenCode2 owns code/data implementation; the lead owns Editor integration and Play Mode.
- Preserve all existing user worktree changes and `.meta`/GUIDs; do not touch `Library` or `Temp`.
- Do not add packages, assets, scenes, prefabs, `ProjectSettings`, code, or save fields.
- Do not change accepted SC-011/SC-013/SC-014/SC-015 event or localization rows.
- Do not commit, push, merge, or tag before explicit user acceptance.

## Review Focus

- Duplicate IDs: static validation must assert global uniqueness for all new event and option IDs.
- Missing localized strings: every new `textId`, summary ID, and option ID must resolve.
- Early or late branch availability: verify source-option gates and settled-succession thresholds for each generation.
- Wrong relationship effects: verify each of the three decision nodes (six options) targets `npc.sakai.jirobei` with the specified `+5/-5`, and the four endings have no effects.
- Schedule regression: verify priorities `8/80/800` against existing SC-011/013/014/015 order, and compare existing event/localization rows unchanged.

## Files and Ownership

- 修改 `Assets/Resources/Events/event-catalog-v1.json`：只追加新链与 7 个事件。
- 修改 `Assets/Resources/Localization/event-text-zh-v1.json`：只追加 SC-016 所需标题、正文、选项和摘要文本。
- 修改 `PROJECT_STATE.md`、`ARCHITECTURE.md`、`DECISIONS.md`：只追加 SC-016 的状态/数据约定，不改写既有历史。
- 新建 `SC016_DEMO.md`：写明 Play Mode 前置、四条路线逐步操作、预期屏幕变化、失败条件和纯文本结果清单。
- 新建 `docs/reports/sc016-implementation.md`：记录实际改动和仅已完成的静态检查。
- 不编辑其他文件。

## Do Not Edit

- 不改任何 C#、测试、场景、Prefab、`ProjectSettings`、存档 schema、包或资产。
- 不改写 SC-011/013/014/015 的现有 JSON 行或本地化文本；不得覆盖已有用户工作区改动。
- 不运行 Unity、测试、场景 updater 或保存场景；本轮实现 worker 仅负责内容和文档，Play Mode 由协调者在用户授权下执行。
- 不提交、推送、合并或打标签。

## Task 1: Add the Commission Chain and Handoff Documentation

- [x] Read the spec, the current JSON schema, and the specified file list; confirm unrelated dirty files remain untouched.
- [x] Append `chain.sc013.commission-legacy` and seven events: the first-generation commission, two mutually exclusive second-generation nodes, and four mutually exclusive third-generation endings.
- [x] Use existing `EventOptionChosen`, succession-count conditions, and `RelationshipDelta`. Apply only the six specified `+5/-5` deltas (two per decision event) to `npc.sakai.jirobei`; ending events have empty effects.
- [x] Set priorities to `8/80/800`. Confirm required M2 events and existing SC-011 ordering remain ahead, while SC-016 remains ahead of SC-015 and SC-014 within each generation.
- [x] Add localized strings and the files listed above. The handoff states the chain is optional and that missing its first-generation event does not block succession.
- [x] Parse both JSON documents and check global ID uniqueness, localized references, chain membership, source-option uniqueness, generation gates, relationship targets/effects, mutually exclusive endings, and priority ordering.
- [x] Compare the existing SC-011/013/014/015 event and localization rows against the pre-edit snapshot; they are unchanged.
- [x] Record static-check and subsequent lead verification results in `docs/reports/sc016-implementation.md`; keep Unity, tests, validator, Play Mode, save/load, and user acceptance as separate evidence for the lead.

**Acceptance:** Seven nodes form one reachable three-generation chain with four endings; the chronicle records the actual relationship deltas; the data implementation changes only the listed JSON and documentation files; static checks pass; runtime claims remain separate for the lead to verify. The later test-fixture-only adjustment is recorded below.

## Lead Verification Results (2026-11-02, local time)

- The existing Unity 6.6.6f1 Editor compiled/refreshed scripts, then its EditMode Test Runner passed 778/778 tests, with 0 failures and 0 skipped/inconclusive cases. `TestResults.xml` records the exact result; this is Editor evidence, not user acceptance.
- Under the user's temporary authorization, the lead ran four new SC-013 M2 sessions for the four SC-016 routes. The observed endings were “夜路仍然安全”, “账册见了天日”, “旧账已清”, and “商路不再相通”.
- `FoundationSceneValidator`, save/load verification, and formal user acceptance remain outstanding. No checkpoint, push, merge, or tag was created.
