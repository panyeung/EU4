# M4b Two Simple Missions — Accepted Demo
> 历史档案说明：本文件属于旧 Sengoku Chronicle 方向，保留其当时的计划或验收记录。当前设计与开发路线以 [GDD_v4.0.md](GDD_v4.0.md) 为准；不要把旧 M1–M7 代号、Phase 1/2 状态或功能范围当作《影之一族》当前规格。


**Status: Accepted by the user on 2026-11-01.**
**Checkpoint:** `70ac62f0d8a9866488ae194c3cf55c68a8be2e29`, tagged `demo-M4b` and fast-forwarded into local `main`. No remote push was made.

## Scope

- Suzu's sealed letter: accept in Kiyosu, deliver to Aya in Nagoya, receive +5 merit once.
- Nagoya market tally: accept from Jirobei in Nagoya, investigate with Genzaemon in Kiyosu, report to Jirobei in Nagoya, receive +5 merit once.
- M4a remains unchanged at +10 merit. Total merit across all three Phase 1 missions is capped at 20.

## Current implementation

- Plain-C# mission models and named `GameSession` actions are implemented.
- A fixed two-card extension to the existing M4a panel and a transactional FoundationScene updater are authored.
- EditMode coverage is authored for mission rules, aggregate merit, panel behavior, and scene-updater validation/rollback.
- Static review approved the mission/session changes and the revised M4b UI geometry; the acceptance feedback is kept to one line and layout assertions cover internal separation.
- The design and plan are in `docs/superpowers/specs/2026-11-01-m4b-two-simple-missions-design.md` and `docs/superpowers/plans/2026-11-01-m4b-two-simple-missions.md`.

## Verification status

- Source checks: source-only whitespace checks pass; all six new `.meta` GUIDs occur once in Assets, Packages, and ProjectSettings. The saved Unity scene diff includes serializer-produced trailing whitespace on blank YAML fields (`m_Name:` / `m_Text:`); it was left as saved by the validated updater.
- Unity compilation: after `Assets → Refresh` in the existing Editor, `Logs/Editor.log` was updated at 2026-11-01 06:55:35 and records Tundra build successes at lines 9144 and 9150. No C# compiler diagnostics appear after those builds.
- EditMode: the existing Editor's Run All finished 2026-11-01 17:01:32–17:01:33Z. `C:/Users/panye/AppData/LocalLow/DefaultCompany/My project/TestResults.xml` reports 322 passed, 0 failed, 0 skipped; the EditMode suite lists 318 passed, and the four `TownNpcAssignmentsTests` also passed in the same run. The report includes `AddM4bMissionBoardTests` (4/4 passed).
- FoundationScene: the existing Editor logged `M4b mission board validated; FoundationScene saved.` and modified `Assets/Scenes/FoundationScene.unity` through the updater. Fresh `Logs/SceneValidationReport.txt` (2026-11-01 07:04:48 local) reports the scene loaded and clean, both M4a and M4b hierarchy/bindings/containment passing, and `RESULT: PASS; Errors: 0`.
- Console: the user reported the requested playtest passed after clearing Console, including the red-error check; no exact counter snapshot or error-line details were supplied. The Editor log contains service/licensing diagnostics and the rollback warning emitted by a passing fault-injection test; no runtime C# exception lines appear after the latest Play Mode entry.
- Play Mode: the user reported `Playtest passed` after the requested loops and checks. The Editor log records a Play Mode entry and no C# exception lines afterward, but it does not capture the observed merit/date values; those remain user-reported.
- Independent technical QA: ACCEPT; no blocking code or scene defects. The reviewer also confirmed FoundationScene preserves existing serialized object IDs.
- Kanban workflow: no Kanban review tools are configured in this session; the independent review result is recorded here.
- User acceptance: the user explicitly accepted M4b on 2026-11-01 after the reported Play Mode pass.

## Play test path after verification

1. Start a new game in Kiyosu; accept the sealed-letter mission.
2. Travel to Nagoya; deliver the letter and verify +5 merit.
3. Accept the market-tally mission from Nagoya; travel to Kiyosu and investigate.
4. Return to Nagoya and report; verify another +5 merit.
5. Complete M4a as a regression and verify aggregate merit reaches 20 at most.
6. Repeat final actions and attempt wrong-location actions; rewards, date, and location must remain unchanged.

The user accepted the playable demo on 2026-11-01 before the M4b checkpoint, merge, and tag were created.
