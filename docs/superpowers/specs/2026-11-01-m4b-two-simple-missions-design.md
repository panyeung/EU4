# M4b: Two Simple Missions Design

## Intent

Complete the two remaining Phase 1 missions using the existing map, five NPCs, and M4a's small plain-C# mission pattern. This is a fixed content milestone, not the start of a reusable quest-authoring system.

## Current state and sequence

- M1, M2, M3, and M4a are accepted; M4a is checkpointed as `demo-M4a` and merged into local `main`.
- M4b is the next roadmap milestone and depends on M4a.
- `GameSession` currently owns the fixed M4a model and exposes its merit. The existing UI and safe scene-updater pattern are mission-specific.
- The map has Kiyosu, Atsuta, and Nagoya. Kiyosu–Nagoya and Kiyosu–Atsuta each take one day; Atsuta–Nagoya has no direct route.
- Existing NPCs are Suzu and Genzaemon in Kiyosu, Iori in Atsuta, and Aya and Jirobei in Nagoya.

## Goals

- Add exactly two short, playable mission loops with explicit, ordered stages.
- Use existing NPCs and locations only.
- Award +5 merit for each mission, once, after its final valid action. The three Phase 1 missions therefore award at most 20 total merit including M4a's +10.
- Keep mission actions free of time or location changes; only existing travel advances the date.
- Present all three fixed missions consistently in the existing town UI, with status, objective, available action, and feedback.
- Add EditMode coverage for valid paths, wrong location/stage, repeat actions/rewards, reset, and unchanged clock/location on mission actions.
- Keep persistence out of scope until M5.

## Proposed mission content

### Suzu's sealed letter

1. Accept from Tachibana Suzu in Kiyosu.
2. Deliver it to Hoshino Aya in Nagoya.
3. Complete on successful delivery and claim +5 merit once.

Stages: `NotAccepted -> Accepted -> RewardClaimed`.

### Nagoya market tally

1. Accept from Sakai Jirobei in Nagoya.
2. Travel to Kiyosu and investigate the tally with Mori Genzaemon.
3. Return to Nagoya and report to Sakai Jirobei.
4. Complete on valid report and claim +5 merit once.

Stages: `NotAccepted -> Accepted -> Investigated -> RewardClaimed`.

Each mission may be completed independently and in either order. Their state and once-only reward guards remain in their own fixed mission-specific C# models. `GameSession.Merit` reports the sum of rewards claimed by all three fixed missions.

## Interaction and time rules

- A mission action succeeds only at the specified location and in the specified stage; rejected actions leave all mission, merit, date, and location state unchanged.
- Acceptance and mission actions do not move the player or advance the date.
- Travel uses the existing validated map preview/confirmation flow and consumes its normal one-day route cost.
- Existing M4a stages, +10 award, NPC dialogue, and map behavior remain unchanged.

## UI and scene integration

Extend the existing fixed M4a mission panel in place into a three-entry mission board with two pages under `M2WorldMapRoot/TownMenuRoot`. Keep its root, controller binding, M4a text/actions, and existing serialized references stable; show M4a on the default page and add a toggle to a compact page with both M4b mission rows. Each M4b row has status, objective, feedback, and one contextual action. Every action dispatches to a named `GameSession` use case and mission-specific model. The board is hidden with the town menu and refreshes when the player changes location. Keep its existing 280-by-240 footprint so it does not cover the town title, NPC roster, or return button; validate sibling separation. Avoid duplicate roots or broad scene restructuring.

Generated UI remains under one fixed root. Scene integration is single-writer, validates all references before saving, uses one Undo group, rolls back on failure, and never reloads the scene to roll back.

## Non-goals

- No generic quest engine, quest graph, event bus, arbitrary script system, mission editor, or ScriptableObject content pipeline.
- No new NPC, package, external asset, or scene.
- No save/load work, which belongs to M5.
- No edits to `PlayerMovement2D.cs`, `SampleScene`, Build Settings, or unrelated ProjectSettings.
- No changes to M4a's approved mission arc or reward.

## Reuse research

The existing mission model and uGUI panel are the closest fit. A GitHub scan compared three Unity quest-system candidates: `ahmedAwedni/Unity-Quests-System` (MIT; README claims Unity 6+, but adds a general ScriptableObject/event setup), `mechaniqe/unity-quest-core` (MIT; targets Unity 2021.3 LTS and adds a separate event package and graph tooling), and `SomeoneWeird/UnityQuestSystem` (no clear license or Unity 6 compatibility found). None is proportionate to two fixed missions, so the decision is to build narrowly on the existing project patterns and add no dependency.

## Acceptance evidence

- Unity compilation succeeds with zero new Console errors.
- Full EditMode suite passes, including all M4a regressions and M4b model/session/UI/updater coverage.
- FoundationScene validation passes with all three mission entries bound exactly once.
- In Play Mode, both loops can be completed from their start NPCs through their final action; each awards +5 once, aggregate merit is correct, rejected actions do not change date/location, and the Console has no new red errors.
- User Play Mode acceptance is required before the final M4b checkpoint, merge, or tag.
