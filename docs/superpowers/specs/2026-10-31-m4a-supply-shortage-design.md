# M4a Supply Shortage Mission Design

## Intent

Add the first complete Phase 1 mission: the player accepts a supply-shortage investigation in Kiyosu, investigates in Atsuta, negotiates with a merchant, returns to Kiyosu, reports, and receives merit once. This closes the core mission loop without building a reusable quest scripting framework.

## Current state

- M1-M3 are accepted on `main`; M4a is the next roadmap milestone.
- `GameSession` owns the current location, travel clock, and dialogue/relationship use cases.
- M3 added town NPC rosters and branching dialogue. Kuroda Iori is the only NPC assigned to Atsuta; his definition does not currently specify an occupation.
- There is no mission or merit runtime model. `ARCHITECTURE.md` recommends ordinary C# mission state and idempotent reward claims.
- `main` has preexisting local edits. M4a planning and any implementation belong on `feature/m4a-supply-shortage-task` in its clean linked worktree.

## Goals

- Make the single supply-shortage mission playable from acceptance to reward claim.
- Enforce the required order and location for each step.
- Show the current objective, available action, and result in uGUI.
- Award exactly 10 merit once after a valid report in Kiyosu.
- Test state transitions, invalid actions, and duplicate reward claims in EditMode.
- Preserve the current map, travel rules, M3 relationships/dialogue, and fictional cast.

## Non-goals and anti-goals

- No M4b missions, generic quest graph, event bus, arbitrary scripts, mission editor, or third-party package.
- No save/load work; persistent mission state belongs to M5.
- No new scene, free-roaming town, changes to `PlayerMovement2D`, Build Settings, or unrelated `ProjectSettings`.
- Do not let UI call reward mutation directly or let mission rules depend on Unity types.
- Do not award merit on acceptance, investigation, negotiation, or a repeated report.

## Approaches

1. **Focused mission state and explicit actions (recommended working assumption).** A small plain-C# state model owns this mission's ordered stages and reward claim. `GameSession` validates current location and coordinates state. A dedicated uGUI panel displays progress and invokes the explicit application actions. This keeps rules testable and matches the architecture and roadmap.
2. **Drive mission stages through NPC dialogue options.** This reuses the M3 dialogue view, but the current dialogue effect model only changes relationships. Extending it to mutate mission state risks coupling two domains and makes report/reward preconditions less obvious.
3. **Build a reusable mission framework.** This could support more content but conflicts with M4a's explicit scope and adds definitions, generic conditions/effects, and authoring infrastructure before a second mission exists.

The user approved approach 1, Kuroda Iori as the merchant, and a +10 merit reward on 2026-10-31. GitHub research found no suitable reason to import a package: one Unity 6+ MIT sample is still broader than this fixed mission; other MIT samples are older or add event/service infrastructure. Build the focused custom model.

## Proposed rules

Use a mission-specific stage enum with this progression:

`NotAccepted -> Accepted -> Investigated -> Negotiated -> RewardClaimed`

- Accept only while in Kiyosu and `NotAccepted`.
- Investigate only while in Atsuta and `Accepted`.
- Negotiate with Kuroda Iori only while in Atsuta and `Investigated`.
- Report only while in Kiyosu and `Negotiated`.
- A successful report atomically adds 10 merit and advances to `RewardClaimed`; every later report fails without changing merit.
- Mission actions do not advance the clock. Existing travel remains the only time cost in this mission.
- `InitNewGame` resets the mission stage and merit count. M5 will add persistence.
- Use Kuroda Iori as the fictional merchant for this mission, without adding a sixth NPC or changing M3's roster.

The fixed reward amount and merchant assignment were approved by the user on 2026-10-31.

## UI proposal

Add one mission panel under the existing `M2WorldMapRoot` UI hierarchy. It shows mission status/progress, the current location requirement, total merit, contextual action buttons, and result feedback. The actions are accepting in Kiyosu, investigating in Atsuta, negotiating with Kuroda in Atsuta, and reporting/claiming in Kiyosu. Invalid actions remain unavailable in the UI and are also rejected by `GameSession`.

Scene edits stay single-writer and use the existing transactional Unity Editor workflow; generated UI stays under one fixed root and preserves scene `.meta` GUIDs.

## Constraints

- Unity 6000.6.6f1, URP 2D, C#, built-in uGUI, no new package.
- Keep mission rules in plain C# under the existing Domain assembly; keep orchestration in `GameSession`; keep presentation in its own component.
- Use the current Unity Editor for compile/test/scene integration. Never launch a second Editor or batchmode against the same project.
- Preserve original fictional content and license no new assets.
- User Play Mode acceptance is required before the final M4a checkpoint commit, merge, or tag.

## Open implementation details for the builder

- Names of mission-specific result/status structs and exact `GameSession` method signatures.
- The smallest scene binding/generator change that adds the panel without replacing the existing town UI.
- Whether a PlayMode test adds value beyond EditMode UI behavior tests and the manual demo walkthrough.

## Acceptance evidence

- EditMode tests prove the valid route, every wrong-location/wrong-stage rejection, unchanged state after rejection, mission reset, and exactly-once `+10` merit.
- Unity compile succeeds with zero Console errors; available EditMode tests pass; no unrelated ProjectSettings or user WIP changes enter the feature commit.
- In Play Mode, complete the mission from Kiyosu to Atsuta and back; see progress at each step; confirm a valid report awards 10 once and a repeat report cannot award again.
- User explicitly accepts the playable demo before M4a is marked accepted or checkpointed.
