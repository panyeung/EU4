# SC-011 Cross-Generation Event Implementation Plan

**Status:** user accepted, 2026-11-02. The user reports compilation passed, EditMode passed, no Console errors, and Play Test passed. EditMode totals and Console warning count were not supplied; `FoundationSceneValidator` result was not reported. This record is based on user report, not captured Editor logs.

**Audit note (2026-11-02):** GDD 7.5 text templating is out of scope for this slice (two fixed variants instead); GDD 19.1's event re-check and atomic-commit rules are preserved for history conditions and option requirements, except that succession-count conditions are offer-time only so a stable pending decision stays resolvable; GDD 19.4's three-generation M2 script is broader than SC-011's narrower acceptance criterion. The root's existing redundant `Always` condition is replaced by exactly `SuccessionCountEquals 0`. At implementation time no Editor for this project was available; the user later reported compilation and EditMode passed.

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After the first generation chooses how to receive the rainy-night guest, one real SC-010 head change occurs, and the successor receives one follow-up encounter whose visible text and enabled choices depend on that saved choice.

**Architecture:** Keep the existing monthly trigger and allowlisted content engine. Add three condition kinds (one history lookup, two succession-count gates) and a small `EventRuleContext`; gate the accepted root to `SuccessionCount == 0`; add two mutually exclusive follow-up variants (one encounter); use the existing event-choice panel and SC-010 succession panel. No new trigger, save field, NPC, scene, package, or text-template subsystem.

**Tech Stack:** Unity 6000.6.6f1, existing pure C# Domain/Events/Persistence assemblies, existing uGUI, NUnit EditMode tests, Orca supervised OpenCode implementation and independent read-only review.

**Spec:** `docs/superpowers/specs/2026-11-02-sc-011-cross-generation-event-design.md`

## Global Constraints

- Work only in `C:\Users\panye\My project` on `feature/sc011-cross-generation-event`. Do not create a project, worktree, branch, package, asset, or scene.
- Do not edit scenes, prefabs, `ProjectSettings`, `Library`, `Temp`, `SampleScene.unity`, `Assets/1.unity`, `graphify-out/`, `nul`, or unrelated dirty docs. No scene updater or `FoundationSceneValidator` change is expected; the existing event-choice and succession panels are reused.
- Preserve every `.meta` file and GUID. The two JSON resources are edited in place and keep their existing metas.
- Preserve the accepted rainy-arrival text, both option definitions, their requirements and effects, weight, priority, cooldown, once scope, and chain membership. The only root change is the new `SuccessionCountEquals 0` event condition.
- Do not add a new root event, NPC, displaced-youth character, third-generation content, or external dependency.
- Do not edit `PROJECT_STATE.md` or `DECISIONS.md` in this task; the later implementation/docs task updates the checkpoint and next target.
- Do not run batchmode or a second Unity instance. Use the already-open Editor for compilation and tests. Do not use screenshots or Computer Use. The user runs the Play Test.
- No commit, merge, push, or tag. The lead owns integration and checkpoint delivery. Checkpoint merge/tag waits for explicit user acceptance.
- Keep `PersonId`/`SuccessionWorld` and `CharacterId`/`CharacterRelationships` distinct. The only cross-generation carrier is persisted event history; follow-up effects target the existing NPC `npc.kuroda.iori` as the family's standing with an old guest.

## Review Focus

1. The accepted root is unchanged in text/options/effects/requirements and is not offered after the first handoff.
2. Exactly one follow-up variant can be eligible for a given root history, with deterministic ordering and no weighted ambiguity.
3. Adult handoff schedules after the next month; regency defers until the explicit majority handoff; extinction/pending succession/regency never schedule.
4. Repeated succession cannot duplicate or reopen the chain; loading a pre-follow-up save replays only that rolled-back timeline.
5. A pending event can always be resolved after a later head change (succession gates are offer-time only).
6. Save/load restores root choice, pending follow-up, once markers, relationship delta, and succession state; content `1 → 2` is additive-only and unknown IDs still reject atomically.
7. No scene, prefab, ProjectSettings, package, or unrelated-doc change.

---

### Task 1: Content contract and validator

**Files:**
- Modify: `Assets/Scripts/Events/EventCatalogData.cs`
- Modify: `Assets/Scripts/Events/EventCatalogValidator.cs`
- Test: `Assets/Tests/EditMode/EventCatalogValidatorTests.cs`

**Interfaces:**
- Add `EventConditionKind.EventOptionChosen`, `SuccessionCountEquals`, `SettledSuccessionCountAtLeast`.
- Add `EventConditionData.sourceEventId` and `sourceOptionId`.
- Add `SC011EventIds` constants for both follow-up event IDs and all six follow-up option IDs.
- Validator rules:
  - `EventOptionChosen` requires non-empty `sourceEventId`/`sourceOptionId`, an existing source event and option, a source event different from the referencing event, the same non-empty `chainId` as the source, and at most one `EventOptionChosen` condition per event.
  - Reject two events that claim the same `(sourceEventId, sourceOptionId)` pair.
  - `SuccessionCountEquals`/`SettledSuccessionCountAtLeast` require `threshold` in `[0, 1000]`, are event-level only, and are allowed only on `Monthly` events.
  - Follow-up events with `EventOptionChosen` must be `PerGame`; the root remains `PerGame`.
  - Existing chain membership, duplicate ID, text, weight, cooldown, and enum checks stay unchanged.

- [x] Author tests for a missing source event, missing source option, cross-chain reference, self-reference, duplicate branch pair, two history conditions on one event, invalid threshold, non-monthly follow-up, and a valid two-variant chain (authored, not executed).
- [x] Extend the DTO/enums and constants without changing any existing serialized field name or order.
- [x] Implement structural validation inline and defer source-reference checks until all events are registered (a condition may reference an event later in the array).
- [x] Run the focused validator tests; the user reports EditMode passed, exact test totals were not supplied.

**Observable check:** `EventCatalogValidatorTests` accepts the SC-011 catalog shape and rejects each malformed branch with an actionable error; the existing rainy-arrival catalog still validates.

---

### Task 2: Rule context and engine semantics

**Files:**
- Create: `Assets/Scripts/Events/EventRuleContext.cs` and Unity-generated `.meta`
- Modify: `Assets/Scripts/Events/EventEngine.cs`
- Test: `Assets/Tests/EditMode/EventEngineTests.cs`

**Interfaces:**
- `public readonly struct EventRuleContext`
  - `EventRuleContext(int successionCount, int settledSuccessionCount)`
  - `int SuccessionCount`, `int SettledSuccessionCount`, `static EventRuleContext Default`
- Add optional `EventRuleContext context = default` to `GetEligibleEventIds`, `ScheduleMonthly`, and `TryChooseEventOption`; existing call sites and tests keep compiling.
- `EventOptionChosen` is true iff the **last** committed history entry whose `eventId` equals `sourceEventId` has the matching `optionId`; false when there is no entry.
- `SuccessionCountEquals` compares `context.SuccessionCount == threshold`.
- `SettledSuccessionCountAtLeast` compares `context.SettledSuccessionCount >= threshold`.
- Succession-count conditions are evaluated only during eligibility/scheduling. `EventOptionChosen` is evaluated at scheduling and again at commit. Document this in XML comments.
- Monthly ordering stays priority descending, ordinal `eventId`, then weighted roll inside the top-priority group. No queue changes.

- [x] Author tests for: root eligible at context `(0,0)`; root ineligible at `(1,0)` and `(1,1)`; follow-up A eligible only with the offer-rest history and settled `1`; follow-up B only with ask-tomorrow; no variant with no history or settled `0`; last-entry wins if history is duplicated; one follow-up candidate selects without ambiguity; a pending root still commits after a handoff; existing rainy-arrival scheduling/effects/requirements are unchanged (authored, not executed).
- [x] Implement `EventRuleContext`, history lookup, and the condition branches behind the optional parameter.
- [x] Keep `TryGetOptionAvailability` unchanged; follow-up options use `Always` requirements.
- [x] Run focused tests; the user reports EditMode passed, exact test totals were not supplied.

**Observable check:** At `(0,0)` the accepted root is the only eligible event; at `(1,1)` exactly one follow-up variant is eligible for each root option; no test relies on random selection between variants.

---

### Task 3: Content, texts, and content version

**Files:**
- Modify: `Assets/Resources/Events/event-catalog-v1.json`
- Modify: `Assets/Resources/Localization/event-text-zh-v1.json`
- Modify: `Assets/Scripts/Persistence/SaveData.cs`
- Test: create `Assets/Tests/EditMode/SC011EventContentTests.cs` and Unity-generated `.meta`

**Interfaces:**
- Catalog/text `contentVersion = 2`; `SaveData.CurrentEventContentVersion = 2`.
- Root `event.rainy-arrival`: add only `{"type":"SuccessionCountEquals","threshold":0}` to `conditions`.
- Extend `chain.rainy-arrival.linkedEventIds` to include both follow-up event IDs.
- Add `event.rainy-arrival.legacy-honored` and `event.rainy-arrival.legacy-deferred` with `trigger: Monthly`, `weight: 1`, `priority: 100`, `cooldownMonths: 0`, `onceScope: PerGame`, the same `chainId`, and the conditions in §4.2 of the spec.
- Add options/effects/text exactly as the spec table; all follow-up option requirements are explicit `Always`.
- Keep resource file names and metas unchanged.

- [x] Author tests that assert contentVersion `2`, root text/options/effects/requirements/weight/priority/cooldown/once/chain remain as accepted, the root has exactly the new `SuccessionCountEquals 0` condition, each root option has exactly one follow-up variant, and the two variants use different option IDs (authored, not executed).
- [x] Edit the JSON data and add original Chinese titles/bodies/summaries from the spec. Do not copy commercial-game text.
- [x] Bump the fallback content version in `SaveData.cs`.
- [x] Validate the catalog through `EventCatalogValidator` as part of the user-reported passing EditMode run; exact test totals were not supplied.

**Observable check:** The catalog validates at version 2; the root is unchanged except for one eligibility condition; variant A has `旧客的雨夜回访` and its three options; variant B has `门房里的旧雨` and its three options; `SC011EventContentTests` asserts the preserved root fields directly from the loaded catalog.

---

### Task 4: Session integration, save compatibility, and regression coverage

**Files:**
- Modify: `Assets/Scripts/Application/GameSession.cs`
- Test: create `Assets/Tests/EditMode/SC011LegacyEventTests.cs` and Unity-generated `.meta`
- Test: modify `Assets/Tests/EditMode/GameSessionSaveTests.cs` and `Assets/Tests/EditMode/SaveDataValidatorTests.cs` only as needed

**Interfaces:**
- Add a private `BuildEventRuleContext()`:
  - `SuccessionCount = Succession.SuccessionCount`
  - `SettledSuccessionCount = Succession.Phase == SuccessionPhase.None ? Succession.SuccessionCount : 0`
- Pass it to `ScheduleMonthly` in `TryAdvanceToNextMonth`, and to `TryChooseEventOption` in `GameSession.TryChooseEventOption`. Do not change `TryGetEventOptionAvailability`.
- In `TryApplySave`, accept content `1` under current content `2` only:
  - `data.eventContentVersion == _eventCatalog.contentVersion || (data.eventContentVersion == 1 && _eventCatalog.contentVersion == 2)`
  - Keep `TryRestoreState` as the authority for every referenced ID; reject unknown IDs atomically.
- No save schema change and no new `EventRuntimeSaveData` field.

- [x] Author adult-handoff test: complete rainy-arrival with offer-rest, confirm 正綱, verify `SuccessionCount == 1` and phase `None`, end month, assert the pending event is `event.rainy-arrival.legacy-honored` (`AdultHandoffSchedulesHonoredVariantForOfferRestChoice`; authored, not executed).
- [x] Author the ask-tomorrow mirror asserting `event.rainy-arrival.legacy-deferred`; raise Iori's relationship to at least `1` first so the root option remains selectable under its accepted `RelationshipAtLeast 1` requirement (`AskTomorrowChoiceRequiresTrustThenSchedulesDeferredVariant`).
- [x] Author a regency test: confirm 時姫 with regent 千恵, end months through the regency, assert no follow-up while `Regency`; complete the majority handoff, end month, assert the follow-up is eligible (`RegencyDefersVariantUntilExplicitAdultHandoff`).
- [x] Author repeated-succession tests: a second succession before the follow-up is scheduled (minor heir → regency) does not duplicate or reopen the chain, and the follow-up is queued exactly once after the successor settles (`SecondSuccessionBeforeFollowUpSchedulesItOnceAfterSuccessorSettles`); a later handoff after the follow-up is committed does not reopen the root or reschedule (`FollowUpDoesNotRepeatAfterAnotherHandoff`).
- [x] Author no-choice test: handoff without rainy-arrival history yields no follow-up and no error (`HandoffWithoutRootHistorySchedulesNothing`).
- [x] Author scheduling-failure coverage at the existing `EventEngine` seam: a failed `ScheduleMonthly` (pending/non-management readiness) leaves the runtime snapshot and the event RNG state unchanged (`SchedulingFailureDoesNotMutateRuntimeOrConsumeEventRandom`). A full session-level date/succession failure injection is **deferred/unreachable without a production hook** because a valid catalog plus the session's own guards leave no reachable `SchedulingFailed` path; no test hook was added and the existing rollback code is unchanged. That session-level check remains a Unity/manual gate.
- [x] Author save/load tests for root pending (`RootPendingSaveRestoresSameRootAndOptions`), after root choice (`AfterRootChoiceSaveDoesNotReofferRoot`), adult handoff before the next month (`PostHandoffSaveReplaysSameVariant`), follow-up pending (`FollowUpPendingSaveKeepsVariantOptionsAndRelationshipValue`), after follow-up choice (`AfterFollowUpChoiceReloadKeepsHistoryAndOnceMarker`), and during/after regency (`RegencyCheckpointBeforeFollowUpReplaysAfterMajority`). Succession-pending save/load remains covered by the existing SC-010 `SuccessionSaveTests`.
- [x] Author compatibility tests: v3 content 1 with valid IDs loads additively; v3 content 1 with an unknown pending event rejects without changing the live session; v3 content 3 rejects (`ContentOneSaveLoadsAdditivelyAndIsRewrittenAsTwo`, `ContentOneSaveWithUnknownPendingEventRejectsAtomically`, `UnknownContentVersionThreeRejectsWithoutChangingTheSession`). Migration seams: a v2 payload missing `eventContentVersion` is rejected as missing metadata, v2 content 1 migrates to schema v3 preserving content 1, and v1 without content metadata migrates to current content 2 (`SaveSlotStoreTests`, `SaveDataValidatorTests`).
- [x] Preserve the existing SC-010 tests and behavior; do not change `SuccessionService`, `SuccessionState`, `SuccessionPanel`, or `EventChoicePanel` unless a compile/test failure proves a minimal fix is required, in which case stop and report before expanding scope.
- [x] Run the full EditMode suite; the user reports it passed, exact test totals were not supplied.

**Observable check:** The user-visible sequence in the spec works at the session level without any scene change; save/load at each stable point restores the same pending variant or the same post-choice state.

---

### Task 5: Verification, independent review, and user Play Test

**Files:** no production files in this task. Do not create a demo doc unless the lead requests it; the Play Test below is the acceptance script.

- [x] Compilation: the user reports passed; an Editor log was not supplied.
- [x] EditMode: the user reports passed; exact passed/failed/skipped counts were not supplied.
- [ ] Console: the user reports no errors; warning count and exact Console totals were not supplied.
- [ ] FoundationScene validator: result/report path not supplied; do not claim a pass.
- [x] Separate read-only OpenCode2 reviews returned ACCEPT; this is static review, not runtime evidence.
- [x] User ran the Play Test and explicitly accepted SC-011 on 2026-11-02; lead checkpoint integration is authorized.

**User-run Play Test (text-only; do not run it for the user, do not use screenshots):**

1. Enter Play Mode in `FoundationScene`, click `新游戏`. Record the starting date `1560-04-01`, head `明智 晴信`, and the visible event/relationship state.
2. Click `结束月份`. Expected: `雨夜来客` appears with `请他进屋歇息` and `请他明晨再来`; the root text matches the accepted demo.
3. Choose `请他进屋歇息`. Expected feedback: the existing +5 trust summary. Save with `存档`. Record the feedback and that the choice is visible.
4. Click `退休并安排继承`, choose `明智 正綱`, then `确认交接`. Expected: head becomes `明智 正綱`, the succession modal closes, the handoff record is `succession.1.person.akechi.masatsuna`; date, location, missions, merit, and other relationship values are otherwise unchanged. Save.
5. Click `结束月份`. Expected: `旧客的雨夜回访` appears with `重申先代的门诺`, `按当下利害重议`, `止于这一夜`. The body mentions the old guest and the earlier hot tea; variant B's `门房里的旧雨` must not appear.
6. Do not choose yet: load the step-4 save (single save slot), then click `结束月份`. Expected: the same `旧客的雨夜回访` and the same three options. Then choose `重申先代的门诺`; expected feedback confirms the old pledge and the +5 trust summary. Save, click `结束月份` again, and expect no rainy-arrival repeat and no second legacy event.
7. Load the post-choice save, then click `结束月份`. Expected: no repeat, and the root is not reopened. This proves save/load at the post-choice stable point.
8. Start a new game. Before the first `结束月份`, travel to Atsuta and use 黒田伊织's town dialogue to raise his trust to at least 1 (accept his proposition), then return to the map. This is required because the accepted `请他明晨再来` option keeps its existing `RelationshipAtLeast 1` requirement and is disabled at the default value 0. Then click `结束月份`, choose `请他明晨再来`, retire to `明智 正綱`, then click `结束月份`. Expected: `门房里的旧雨` appears with `补上先代欠下的礼数`, `说明当日不得已`, `旧事不必再提`; variant A's text and options must not appear.
9. Record each step pass/fail and the visible text in the test record. Do not use screenshots.

Regency behavior is covered by authored EditMode tests; the user reports EditMode passed but did not supply per-suite counts. A separate regency Play Test would require many month turns and is not part of the smallest slice.

**Evidence boundary:**

- User-reported results: compilation passed; EditMode passed; no Console errors; Play Test passed and accepted.
- Not supplied: EditMode totals, Console warning count, and FoundationScene validator result. Do not infer these values or claim validator success.
- The read-only code review is not Play Mode evidence. The Play Test acceptance is the user's report dated 2026-11-02.
- User acceptance authorizes the lead to create the checkpoint, merge to `main`, tag `demo-SC011`, and push under project policy.

---

## Implementation status (2026-11-02)

Authored in this task, but **not verified by Unity**:

- `Assets/Scripts/Events/EventCatalogData.cs` — three condition kinds, `sourceEventId`/`sourceOptionId`, `SC011EventIds`.
- `Assets/Scripts/Events/EventCatalogValidator.cs` — structural branch checks plus deferred source-reference checks.
- `Assets/Scripts/Events/EventRuleContext.cs` (+ `.meta`) — immutable offer-time rule context.
- `Assets/Scripts/Events/EventEngine.cs` — optional context parameters, history lookup, offer-time succession gates.
- `Assets/Scripts/Application/GameSession.cs` — `BuildEventRuleContext()`, context wiring, content `1 → 2` additive load window.
- `Assets/Scripts/Persistence/SaveData.cs` — fallback content version `2`.
- `Assets/Resources/Events/event-catalog-v1.json` and `Assets/Resources/Localization/event-text-zh-v1.json` — content version `2`, root gate, two follow-up variants, original Chinese text.
- `Assets/Tests/EditMode/EventCatalogValidatorTests.cs`, `EventEngineTests.cs` — extended coverage, including a direct scheduling-failure no-mutation test.
- `Assets/Tests/EditMode/SC011EventContentTests.cs` and `SC011LegacyEventTests.cs` (+ `.meta` files) — content contract, adult/regency/deferred/second-succession/root-pending-save-load/compatibility coverage.
- `Assets/Tests/EditMode/SaveSlotStoreTests.cs`, `SaveDataValidatorTests.cs` — v2 missing-content-version rejection, v2 content-1 migration, and v1 missing-content-version migration to current content `2`.

**Verification report:** The user reports Unity compilation passed, EditMode passed, no Console errors, and Play Test passed with explicit acceptance. Exact EditMode totals and Console warning count were not supplied; `FoundationSceneValidator` result was not reported. The user-provided results are not accompanied by Editor logs in this worktree.

Offline checks actually run while reconciling this plan (not Unity evidence): both resource JSON files parse as UTF-8, `contentVersion` is `2` in each, the root has exactly `SuccessionCountEquals 0`, the two variants key `offer-rest`/`ask-tomorrow` with `SettledSuccessionCountAtLeast 1`, the chain links all three event IDs, and the two Chinese titles match the spec.

## Review follow-up (2026-11-02)

A read-only review of `task_a51cf1873fb0` found no functional defect. This follow-up corrected documentation and added regression coverage:

- Event-rule citations now point at GDD 19.1 (not GDD 18), the section that actually requires re-checking before display and re-validating before commit.
- The compatibility matrix now states that a schema-v2 payload requires `eventContentVersion` `1` and that missing required metadata is rejected by `SaveDataValidator.ValidatePresence`; only schema v1 may omit the content version and migrate to the current `2`.
- `EventRuleContext`'s comment now matches the code: `GameSession` passes a context at commit, while `EventEngine` ignores succession gates at commit and still re-checks history conditions.
- Added root-pending and post-root-choice save/load tests, a second-succession-before-scheduling test, a regency-checkpoint replay test, a direct scheduling-failure no-mutation test, and the v1/v2 migration tests described above. No production behavior changed.

---

## Rollback and ownership

- If a change requires a scene, prefab, ProjectSettings, package, or new asset beyond the two JSON resources, stop and report; do not expand scope.
- If a test exposes an SC-010 regression, prefer a minimal SC-011-only fix; do not rewrite the succession model.
- The lead owns scene validation, final integration, checkpoint commit, merge, and tag. The implementation and review tasks do not stage or commit.
- Preserve all pre-existing dirty docs and user WIP, including `PROJECT_STATE.md`, `DECISIONS.md`, `SampleScene.unity`, `Assets/1.unity`, `ProjectSettings/SceneTemplateSettings.json`, `graphify-out/`, and `nul`.
