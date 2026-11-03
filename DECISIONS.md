# Decisions

Current design authority: [GDD_v4.0.md](GDD_v4.0.md), 《影之一族》. Older Sengoku Chronicle / Phase 1 / Phase 2 decisions below are historical and apply only when compatible with the current GDD. Current implementation status is in [PROJECT_STATE.md](PROJECT_STATE.md).

## 2026-11-03 - SC-012 uses a saved append-only family chronicle

- Decision: Reuse the existing Unity project and add a plain C# append-only `HistoryLedger`; record only successful event commits, actual relationship value changes (with before/after values), mission stage changes, and succession phase changes. Show the entries in one read-only, scrollable uGUI panel in the existing `FoundationScene`.
- Persistence: Bump save schema v3→v4. Migrate saved v3 event history in its existing order and year/month precision; do not invent missing days or reconstruct old mission, relationship, or succession history. No new package or third-party runtime code.
- Succession scope: Record successful changes to succession phase, current head, or completed result. Merely changing a pending candidate/regent selection inside the same phase is not a separate chronicle event; it remains visible through the succession UI until the next committed transition.
- Implementation: User approved the Chinese design and plan. OpenCode2 implemented the code and EditMode test sources on `feature/sc012-family-chronicle`; the lead owns scene updater execution and documentation. Three sequential OpenCode2 reviews found issues that were routed to OpenCode2 and fixed, including canonical ID integrity and same-phase adult heir handoff. The third review ACCEPTED the current source/test changes by static inspection. `FoundationScene.unity` has not received the updater-generated panel.
- Evidence: The implementation worker reported an offline Roslyn type check of seven assemblies with 0 errors and standalone harness results 22/22, 17/17, and 14/14. Those checks predate the latest code fix; the latest worker reported only static brace/whitespace checks, and the standalone harnesses were not rerun. The user reports EditMode passed and Play Test passed, explicitly accepting SC-012. EditMode counts, Console counts, and the FoundationSceneValidator result were not separately reported. The review also noted missing application-level coverage for append-failure logging/relationship refresh and a v1 extra-field compatibility edge. See `SC012_DEMO.md`.
- Integration gate: User acceptance authorizes the SC-012 checkpoint, `main` merge, `demo-SC012` tag, and GitHub push. SC-011 acceptance remains unchanged.

## 2026-11-02 - SC-011 cross-generation event uses allowlisted conditions, not a new chain runtime

- Decision: Keep the existing `Monthly` trigger and add three allowlisted condition kinds (`EventOptionChosen`, `SuccessionCountEquals`, `SettledSuccessionCountAtLeast`) plus a small `EventRuleContext` built by `GameSession`. Gate the accepted root to raw succession count `0` and represent the single follow-up encounter as two mutually exclusive content variants keyed by the saved root option. Bump catalog/text content version to `2` and accept saved content `1` additively under current `2`; save schema stays `3` with no new field.
- Alternatives: A new `SuccessionCompleted` trigger and handoff hooks; runtime text templating; a new NPC or a `PersonId`/`CharacterId` bridge; an external dialogue/event package.
- Reason: The existing engine, save history, and SC-010 succession state already carry the required facts. The chosen slice is the smallest implementation that proves "后代受到先代决定影响" without a new subsystem, scene, prefab, package, or NPC.
- Constraints: Succession-count conditions are offer-time only so a pending event remains resolvable after a head change; only history conditions are re-checked at commit. The accepted rainy-arrival text, options, requirements, effects, weight, priority, cooldown, once scope, and chain membership are preserved; its only content change is replacing the redundant `Always` condition with `SuccessionCountEquals 0`. GDD 7.5 text templating and GDD 19.4's full three-generation M2 script remain future work.
- Verification and acceptance (2026-11-02): The user reports compilation passed, EditMode passed (exact counts not supplied), no Console errors (warning count not supplied), and Play Test passed; the user explicitly accepted SC-011. FoundationSceneValidator evidence was not supplied. OpenCode2's independent static reviews returned ACCEPT; they are not Unity runtime evidence. The `demo-SC011` checkpoint is authorized for merge/tag/push.

## 2026-11-01 - Adopt Shadow Clan plan v4.0 and reuse the existing project

- Decision: Replace the Sengoku Chronicle / Taikō-inspired game direction with `GDD_v4.0.md` and its M0–M8 milestone sequence. Keep completed M1–M7 artifacts as historical checkpoints.
- Constraint: Use only the existing Unity project and Git repository at `C:\Users\panye\My project`; SC-001 audits and aligns that baseline instead of creating a second project. Keep the current Unity version `6000.6.6f1` until any proposed upgrade is evaluated on a branch.
- Reason: The user designated the supplied v4.0 plan as the new project direction and previously instructed us to use one Unity project.

## 2026-10-29 - Use the existing M2 branch for the demo candidate

- Decision: Keep M2 on `feature/m2-world-map` and keep M3 separate until M2 is accepted.
- Alternatives: Start a new integration branch or combine M2 with the later M3 branch.
- Reason: The existing branch contains the M2 implementation and verified scene. This preserves the existing branch history.

## 2026-10-29 - Use built-in UI and defer later Phase 1 systems

- Decision: Keep the M2 UI on Unity's built-in uGUI components and built-in runtime font. Leave facilities as locked placeholders; do not add packages or third-party assets.
- Alternatives: Add an external UI package or include facility, character, mission, and save systems in M2.
- Reason: The roadmap scopes M2 to map travel and town-menu entry/return. Later systems remain in their dependent milestones.

## 2026-10-29 - Separate the town screen from map controls

- Decision: Keep the world-map travel panel below M1 controls, hide map controls while the town menu is open, and make the town panel background opaque.
- Alternatives: Leave both panels visible under a translucent overlay or create a separate town scene.
- Reason: The user's demo feedback identified overlapping panels. Reusing the existing town panel keeps the flow in one scene while making the active screen clear.

## 2026-10-29 - Isolate M3 integration from existing user work

- Decision: Integrate the accepted M2 base and committed M3-01 through M3-03 work on `feature/m3-accepted-integration`, preserving the existing M3 worktree and its uncommitted M3-04 changes.
- Alternatives: Merge into or update the existing M3 worktree, or discard its WIP and restart M3.
- Reason: The existing M3 worktree has changes in the same scene and controller files, so an isolated branch avoids overwriting or disturbing them while retaining useful M3 work.

## 2026-10-31 - Keep M4a as one fixed C# mission

- Decision: Implement the supply-shortage mission as a mission-specific plain C# state model, coordinated by `GameSession` and displayed by one uGUI panel. Use existing Atsuta NPC Kuroda Iori as the merchant and award +10 merit exactly once on a valid report in Kiyosu.
- Alternatives: Extend branching dialogue effects, add a reusable mission framework, add another NPC, or use a package/sample.
- Reason: This is the first complete mission; the fixed state transitions and one-time reward are directly testable without coupling mission rules to UI/dialogue or adding unused infrastructure. Research found no external implementation with a better fit than a focused custom model.
- Scope: Mission actions do not advance time or location. `InitNewGame` resets mission state and merit. Persistence is deferred to M5. No new package or asset is required.
- Status: Approved design; implementation is in progress and remains a demo candidate until scene validation, full verification, and user Play Mode evaluation pass.

## 2026-11-01 - Add two fixed M4b missions

- Decision: Use Suzu's Kiyosu-to-Nagoya sealed-letter delivery and Jirobei's Nagoya-to-Kiyosu market-tally investigation with Genzaemon, then report back to Jirobei. Award +5 merit once per completed mission.
- Alternatives: Leave the two Phase 1 mission slots empty, add new NPCs, or introduce reusable quest authoring.
- Reason: This completes the GDD's three-mission scope using only the existing five NPCs and three towns. The focused models follow the approved M4a pattern; research found external quest frameworks add unnecessary dependencies and infrastructure.
- Constraints: Mission actions never advance time; existing travel does. No package, asset, persistence, or generic framework is added. M5 remains the persistence milestone.
- Verification snapshot (2026-11-01): latest source compiled in the existing Editor; EditMode Run All passed 322/322; FoundationScene updater saved and the fresh validator report passed with zero errors; independent technical QA accepted with no blocking defects. The user reported the requested Play Mode loop passed and explicitly accepted M4b. Exact runtime values and Console counters were not supplied. Checkpoint `70ac62f0d8a9866488ae194c3cf55c68a8be2e29` is tagged `demo-M4b` and fast-forwarded into local `main`; no remote push was made. Kanban review tools were unavailable in this session.

## 2026-11-01 - Keep the mission panel within its validated town footprint

- Decision: Show the existing M4a mission by default and add a page toggle to the two M4b rows inside the same 280-by-240 root.
- Alternatives: Expand the root across the town controls or move existing NPC and return controls.
- Reason: Static layout review found the expanded root would cover the NPC roster and part of the return control. The page switch preserves the scene's existing control positions and the M4a default flow.
- Verification: The M4b validator checks separation from the town title, NPC roster, and return button; updater tests assert the controls do not overlap and their geometry rolls back on failure. All four M4b updater tests passed, and the fresh FoundationScene validator passes.

## 2026-11-01 - Use a built-in JSON single slot for M5

- Decision: Add a small Persistence assembly with a flat `SaveData` DTO (`saveFormatVersion = 1`), Unity's built-in `JsonUtility`, and an atomic temp-file replace of one slot under `Application.persistentDataPath`.
- Alternatives: Add Newtonsoft JSON through a package, build a generic save framework, or support multiple slots.
- Reason: Phase 1 state is about ten fields of plain data; `JsonUtility` needs no dependency, and the atomic replace preserves the previous slot on failure. Mission stages are stored instead of merit, so a completed reward cannot be duplicated on load.
- Scope: No new package, asset, scene, or serializer abstraction. Save/load UI is one compact map-screen control; scene integration follows the existing transactional updater pattern.
- Status: Implementation and tests are written; Unity compilation, EditMode tests, scene integration, and Play Mode acceptance are still pending.

## 2026-11-01 - M6 title overlay gates Play Mode on the existing scene

- Decision: Package the Phase 1 demo with a full-screen `Canvas/M6TitleRoot` overlay (新游戏/继续 + status line) activated by `TitleScreenController` at Play Mode start, plus a demo-completion line driven by the pure rule `GameSession.IsDemoComplete`. Both roots are saved inactive; the title root is the last Canvas sibling with an opaque raycast-blocking Image. Continue reuses the M5 atomic `GameSession.LoadFrom` path unchanged.
- Alternatives: A separate title scene with `DontDestroyOnLoad` handoff, disabling every gameplay root individually, or a scene reload on New Game.
- Reason: One overlay sibling gates all input with zero change to the save format, the map controller, or the scene set; it keeps the single-writer scene flow and the transactional updater pattern already validated in M2–M5.
- Verification: EditMode 401/401 (0 failed, 0 skipped) pre- and post-integration; the batchmode run saved FoundationScene via `AddM6TitleScreen.RunBatchIntegration`; `Logs/SceneValidationReport.txt` reports the M6 PASS line with `RESULT: PASS; Errors: 0`. Play Mode acceptance pending.

## 2026-11-01 - M7 derives rank from merit with a pure threshold table

- Decision: Add `Domain/PromotionRules` (immutable ascending table: 足轻0/番头5/组头10/武士头20) and derive `GameSession.Rank/RankName` from `Merit`. Rank is display-only, never saved; the map gets one `M7RankText` line via the transactional updater; the M6 summary names the rank.
- Alternatives: Store rank in the save (new field/version), add a XP or event system, or use historical office names with era考据 claims.
- Reason: Derivation keeps the M5 save format frozen and makes rank impossible to forge or double-claim; the threshold table is testable pure C#; the rank names are explicitly game-abstracted growth labels, honoring the GDD's no-unverified-history rule while Phase 1's max merit (20) lands exactly on the top rank as a clean demo ending.
- Verification: EditMode 425/425 (0 failed, 0 skipped) pre- and post-integration; `RunBatchIntegration` saved FoundationScene with `RESULT: PASS; Errors: 0` including the M7 checks. Play Mode acceptance pending.

## 2026-11-02 - Accept SC-004–SC-009 checkpoint

- Status: User accepted the Play Test and confirmed Edit Mode passed. Commit `c29d24a` is on `main`; `demo-SC004-SC009` was pushed.
- Scope: Monthly time, deterministic RNG, versioned saves and migration, event data validation, deterministic event resolution, and the playable event-choice UI.
- Evidence boundary: `TestResults.xml` reports 548/548 passed; Play Mode acceptance is user-reported. FoundationScene still serializes the legacy feedback-banner position; the scene updater and validator were not run for this final layout. Do not infer scene validation or Console cleanliness.
- Next GDD task: SC-010, initial house-head retirement and succession. SC-003 family-model WIP was not included in this checkpoint.
