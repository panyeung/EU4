# M4a Supply Shortage — Demo Candidate

**Status: User accepted the M4a demo.** EditMode, scene validation, and user-reported Play Mode checks are recorded below.

Source implementation commits are based on accepted M3 `a93887c`; relevant fixes are `5fdde00` (enter current town) and `74948b9` (unique mission feedback name and safe scene migration).

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
- Compile: after the final button-label change, the existing Editor logged a successful Tundra build; no C# compiler diagnostics appear in `Logs/Editor.log`.
- EditMode (2026-11-01 16:06:30Z / 06:06:30 local): **214 passed, 0 failed, 0 skipped, 214 total**. This includes `CurrentLocationButton_EntersStartingTownWithoutTravel`, the legacy feedback migration test, and all three M2 layout regressions. Task 2 and Task 3 same-task static reviews approved their respective fixes.
- Console: the user replied `pass` after being asked to clear the Console and run the mission walkthrough. Exact Console counters and individual runtime values were not supplied, so this remains a user-reported result rather than independently verified evidence.
- Scene migration: the reviewed updater renamed the M4 child from `FeedbackText` to `MissionFeedbackText`, preserving the M2 map's unique `FeedbackText`; the saved `FoundationScene` diff contains that single object-name change.
- Project scene validation: the fresh `Logs/SceneValidationReport.txt` records `RESULT: PASS; Errors: 0`, with all M4a panel references bound.
- Play Mode walkthrough: user-reported pass after the request to play the complete Kiyosu → Atsuta → Kiyosu loop; no independent runtime capture was made.
- Visual follow-up: the supplied screenshot showed the current-town button hint wrapping and looking inconsistent beside other town buttons. All three buttons now show only their town names; the separate current-location marker identifies the current town. The user reported the visual re-check passed.
- User acceptance: explicitly accepted by the user on 2026-11-01. Console counters and individual runtime values were not provided; the user reported pass after being asked to clear Console and complete the walkthrough.

## Changed files

Source changes since accepted M3 base `a93887c`:

- `Assets/Scripts/Domain/SupplyShortageMission.cs` and `.meta`
- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Presentation/SupplyShortageMissionPanel.cs` and `.meta`
- `Assets/Scripts/Presentation/WorldMapController.cs`
- `Assets/Scripts/Editor/AddM4SupplyShortageMissionPanel.cs` and `.meta`
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- `Assets/Scenes/FoundationScene.unity` (mission panel integration and reviewed feedback-name migration)
- `Assets/Tests/EditMode/SupplyShortageMissionTests.cs` and `.meta`
- `Assets/Tests/EditMode/GameSessionMissionTests.cs` and `.meta`
- `Assets/Tests/EditMode/SupplyShortageMissionPanelTests.cs` and `.meta`
- `Assets/Tests/EditMode/AddM4SupplyShortageMissionPanelTests.cs` and `.meta`
- Approved design and implementation plan under `docs/superpowers/`.

The current Unity scene change is present in the working tree and remains uncommitted pending user Play Mode acceptance.

## Final checkpoint

The accepted candidate is checkpointed on `feature/m4a-supply-shortage-task` at `9c10959e5792777ea433a16c590034f406dccba1`, tagged `demo-M4a`, and fast-forwarded to local `main`. No remote push was made.
