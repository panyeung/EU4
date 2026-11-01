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

The reviewed updater migrated and saved `Assets/Scenes/FoundationScene.unity`, renaming the M4 feedback text to `MissionFeedbackText` to preserve the M2 map's unique `FeedbackText`. The fresh project-wide validator report is `RESULT: PASS; Errors: 0`, with all M4a references bound. The user reported pass for the Kiyosu → Atsuta → Kiyosu walkthrough and later visual re-check, then explicitly accepted the demo on 2026-11-01. Exact Console counters and runtime values were not supplied, and no independent runtime capture was made. The accepted candidate is being checkpointed, tagged `demo-M4a`, and fast-forwarded into local `main`; no remote push is made.
