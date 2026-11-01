# M4a Supply Shortage — Demo Candidate

**Status: Candidate assembled; user Play Mode evaluation and acceptance remain pending.** This document records the current evidence and remaining gates; it does not indicate milestone acceptance.

Scene integration commit: `6cba4ba` (`feat(m4a): integrate mission panel in foundation scene`). Source implementation commits are based on accepted M3 `a93887c`; no final checkpoint, merge, or tag has been created.

## Intended playable walkthrough

1. Start a new game in Kiyosu and accept the supply-shortage mission.
2. Travel to Atsuta; investigate the shortage and negotiate with the existing NPC Kuroda Iori.
3. Return to Kiyosu and report to claim exactly +10 merit.
4. Attempt a second report and confirm merit does not increase again.

## Current implementation

- Mission-specific stages live in plain C# and are coordinated through `GameSession`.
- One uGUI panel displays mission progress and available actions.
- A transactional Editor updater and scene validator are implemented for the existing `FoundationScene`.
- No package or asset was added. Mission persistence is deferred to M5.

## Verification evidence

- Unity version: 6000.6.6f1; the single existing Editor is open on this M4a worktree.
- Compile: the existing Editor rebuilt the script and EditMode test assemblies successfully during the latest run (`Tundra build success`; no C# compiler errors found in the current project log).
- EditMode (2026-11-01 05:18:09 local): **203 passed, 0 failed, 0 skipped, 203 total** (1.110 sec). The same-task Task 2 static review approved `c326e63` with no findings. The prior `SendMessage` log failure is cleared.
- Console: the AI Assistant package writes `NoSubscription` as `Debug.Log` informational messages. The latest project log has no C# compiler errors, `Debug.LogError`, or exception from the current test/validation runs. The live Console counter after clearing older test history is still unverified.
- Scene integration: the reviewed updater reported “M4a mission panel validated and FoundationScene saved.” The saved diff adds the mission hierarchy and controller binding; the existing NPC roster return button block remains unchanged.
- Project scene validation: `Logs/SceneValidationReport.txt` records `RESULT: PASS; Errors: 0`; `FoundationScene` was loaded and clean, and all M4a panel references were bound.
- Play Mode walkthrough: pending. Runtime behavior has not been demonstrated in this candidate.
- User acceptance: pending; the user plans to evaluate during Play Test.

## Changed files

Source changes since accepted M3 base `a93887c`:

- `Assets/Scripts/Domain/SupplyShortageMission.cs` and `.meta`
- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and `.meta`
- `Assets/Scripts/Presentation/WorldMapController.cs`
- `Assets/Scripts/Editor/AddM4SupplyShortageMissionPanel.cs` and `.meta`
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- `Assets/Tests/EditMode/SupplyShortageMissionTests.cs` and `.meta`
- `Assets/Tests/EditMode/GameSessionMissionTests.cs` and `.meta`
- `Assets/Tests/EditMode/SupplyShortageMissionPanelTests.cs` and `.meta`
- `Assets/Tests/EditMode/AddM4SupplyShortageMissionPanelTests.cs` and `.meta`
- Approved design and implementation plan under `docs/superpowers/`.

The lead's pending documentation updates are excluded from this source list. The final report will add scene changes, exact final commit range, and the final review status after those gates are complete.

## Remaining gates

- Resolve the sole EditMode failure and obtain a fresh full-suite passing result from the existing Editor.
- Resolve or otherwise establish a zero-error Console state, recording the actual source and evidence.
- Have the lead run the reviewed updater in the existing Editor; validate and inspect `FoundationScene` and its report.
- Play the complete Kiyosu → Atsuta → Kiyosu loop, confirm feedback and exactly-once reward behavior, and record actual results.
- Obtain explicit user Play Mode acceptance before creating the final checkpoint, merging, or tagging.
