# SC-011 Cross-Generation Event — Design Spec

**Status:** user accepted, 2026-11-02. The user reports compilation passed, EditMode passed, no Console errors, and Play Test passed. EditMode totals and Console warning count were not supplied; `FoundationSceneValidator` result was not reported. This record is based on user report, not captured Editor logs. Static review is not runtime evidence.

**Authority:** `GDD_v4.0.md` 7.4 (cross-generation chains preserve decision consequences, not cutscenes), 17 (SC-011 acceptance: 后代受到先代决定影响), 19.1 (re-check conditions before display and re-validate before commit), 19.2 (succession, adult/regency handoff, no looping branches), 19.3 (stable save points, compatibility, recovery), and 19.4 (first generation makes a costly choice, the second faces its consequence).

**Baseline:** SC-010 is user-accepted (commit `af35633`, tag `demo-SC010`, per coordinator). `PROJECT_STATE.md` and `DECISIONS.md` are dirty stale snapshots; this design task must not edit them. The later implementation/docs task updates the checkpoint and the next target.

## 1. Smallest complete slice

One playable vertical slice, not a chain framework:

1. The accepted first-generation event `event.rainy-arrival` is played and its chosen `optionId` is saved.
2. One real SC-010 head change occurs.
3. The successor receives one follow-up encounter whose visible text and enabled choices depend on the saved option.
4. Save/load works at every stable decision point before, during, and after the handoff.

The existing rainy-arrival event is the generation-one root and keeps its text, options, effects, requirements, weight, priority, cooldown, once scope, and chain membership. Kuroda Iori is an old guest, not a displaced youth. GDD 7.4's displaced-youth/old-retainer pattern is the narrative inspiration for the follow-up, not a claim that Iori is that youth or a family retainer.

## 2. Findings that constrain the design

- `EventRuntimeState` already persists `History` (`eventId`, `optionId`, `resultId`, year, month, summary), cooldowns, once markers, committed result IDs, and the pending queue. The first choice is already durable.
- `chainId` is validated metadata only. No chain scheduling exists.
- The only trigger is `Monthly`; the event engine sorts by priority descending, then ordinal `eventId`, and rolls weight only inside the top-priority group.
- Effects are allowlisted; there is no arbitrary C# execution and no runtime text templating.
- Succession uses `PersonId` and `SuccessionWorld`; event effects use `CharacterId` and `CharacterRelationships`. They are distinct models. The follow-up must not pretend they are interchangeable.
- Extending this local allowlisted engine is smaller and safer than adding an external narrative dependency; no researched dependency beats it for this one-event slice.

### 2.1 Design/plan audit against GDD and current source (2026-11-02)

- **GDD 7.5 "支持人物姓名、称谓、身份等变量替换"** is a general technical specification, not an SC-011 acceptance item. The current engine has no text templating, and adding one would be a separate subsystem. This slice uses two content variants with fixed original text; templating remains future work and is not claimed here.
- **GDD 19.1 "事件展示前重新检查条件" and "选项提交前再次验证"** are preserved for history conditions and option requirements. Succession-count conditions are deliberately **offer-time only** because a pending event must remain resolvable across a head change; re-checking a succession gate at commit would make a saved stable decision uncommittable. This is the one documented deviation, and it is limited to the two succession-count condition kinds. `GameSession` does pass the rule context to the commit call; `EventEngine` ignores only the succession gates there and still re-checks history conditions and option requirements.
- **GDD 19.4** requires an M2 three-generation script with at least two handoffs. SC-011's acceptance is narrower ("后代受到先代决定影响"). This slice is one two-generation component of the M2 script, not the M2 end-to-end script; the second-generation consequence is what SC-011 must prove.
- **Current source check:** `SuccessionPhase`, `SuccessionCount`, append-only event `History`, allowlisted `EventConditionKind`, and the single `Monthly` trigger all exist as the spec assumes. Before this slice, `GameSession.TryApplySave` required exact `eventContentVersion` equality; the `1 → 2` window is therefore an explicit additive change, not pre-existing behavior. Its tests are authored but not executed.
- **Root conditions:** the accepted `event.rainy-arrival` previously used a single redundant `{"type":"Always"}` condition. Task 3 replaced that array with exactly `[{"type":"SuccessionCountEquals","threshold":0}]`; removing the redundant `Always` changes no behavior and keeps the new gate the only event-level condition.

## 3. Chosen approach

**Option A (chosen): keep the monthly trigger and add three allowlisted condition kinds.**

- `EventOptionChosen` tests the **last committed history entry** for a source event and option. History is append-only, so "last" is deterministic and a duplicate entry cannot create ambiguity.
- `SuccessionCountEquals` and `SettledSuccessionCountAtLeast` read a small `EventRuleContext` supplied by `GameSession`.
- `SuccessionCount` is the raw SC-010 `SuccessionCount` (increments on any handoff, including the start of a regency).
- `SettledSuccessionCount` is `SuccessionCount` only when `SuccessionPhase.None`; it is `0` during a pending succession, a regency, or extinction. This is the "successor actually holds control" count.

**Succession-count conditions are offer-time gates only.** They are evaluated by `ScheduleMonthly`/`GetEligibleEventIds`, not re-evaluated when a pending event is committed. This guarantees that a stable saved event can always be resolved even if the head later changes or the family later ends. `EventOptionChosen` is re-checked at commit because committed history never changes.

No new trigger, no new save field, no new NPC, no new scene, no new package, and no runtime text engine.

**Rejected alternatives:**

- New `SuccessionCompleted` trigger: needs a new scheduling API, new handoff hooks in `GameSession`, and new failure/ordering paths for no visible gain.
- Dynamic text templates: requires a text-variant subsystem; two mutually exclusive content variants are smaller and easier to verify.
- A new NPC or `PersonId`/`CharacterId` bridge: would conflate the two models or expand the roster and save validation.
- External dialogue/event package: adds a dependency for one event and cannot replace the existing validation/commit path.

## 4. Content

### 4.1 Generation-one root (unchanged, one added eligibility condition)

`event.rainy-arrival` remains exactly as accepted:

| Field | Value |
|---|---|
| Trigger | `Monthly` |
| Weight / priority | `10` / `10` |
| Cooldown / once | `3` months / `PerGame` |
| Chain | `chain.rainy-arrival` |
| Options | `option.rainy-arrival.offer-rest` (+5 `npc.kuroda.iori`), `option.rainy-arrival.ask-tomorrow` (unchanged `RelationshipAtLeast 1` requirement, no effect) |
| Text | unchanged `event.rainy-arrival`, option texts and summaries |

One event-level condition is added: `SuccessionCountEquals 0`. It replaces the redundant `Always` entry, so the root's `conditions` array is exactly `[{"type":"SuccessionCountEquals","threshold":0}]`. It prevents the root from being offered after any head change. It does not alter text, options, option requirements, effects, weight, priority, cooldown, once scope, or chain membership. Before the first succession, the accepted demo behaves identically.

### 4.2 One follow-up encounter, two mutually exclusive variants

The engine has no text templating, so the single follow-up step is represented by two event definitions. They share a chain step, have distinct visible text and choice sets, and exactly one can ever be eligible. Calling this "two events" overstates the content: it is one encounter with two narrative variants.

**Variant A — sheltered (root option `offer-rest`)**

| Field | Value |
|---|---|
| Event ID | `event.rainy-arrival.legacy-honored` |
| Trigger | `Monthly` |
| Weight / priority | `1` / `100` |
| Cooldown / once | `0` / `PerGame` |
| Chain | `chain.rainy-arrival` |
| Conditions | `EventOptionChosen(event.rainy-arrival, option.rainy-arrival.offer-rest)`; `SettledSuccessionCountAtLeast 1` |
| Text | `event.rainy-arrival.legacy-honored` (new) |

Title: `旧客的雨夜回访`. Body: `又是一夜细雨。黑田伊织站在清洲院门外，手里仍是那封未拆的家书。他没有提先代的名字，只问新家主是否还记得当年那盏热茶。`

| Option | Text | Effect |
|---|---|---|
| `option.legacy-honored.renew` | `重申先代的门诺` | Iori +5 |
| `option.legacy-honored.terms` | `按当下利害重议` | none |
| `option.legacy-honored.close` | `止于这一夜` | Iori -5 |

Option bodies and summaries:

- `renew`: body `当着伊织的面承认旧约，并请他再次入屋。`; summary `伊织确认这份旧情没有随先代离去，对家族的信任增加五点。`
- `terms`: body `旧情可以续，但条件要按今日的家族处境重写。`; summary `伊织接受了新的往来条件，旧约以另一种方式延续。`
- `close`: body `以家族事务繁忙为由，结束这段旧交。`; summary `伊织收起家书离去，家族与他的旧情转冷五点。`

**Variant B — deferred (root option `ask-tomorrow`)**

| Field | Value |
|---|---|
| Event ID | `event.rainy-arrival.legacy-deferred` |
| Trigger | `Monthly` |
| Weight / priority | `1` / `100` |
| Cooldown / once | `0` / `PerGame` |
| Chain | `chain.rainy-arrival` |
| Conditions | `EventOptionChosen(event.rainy-arrival, option.rainy-arrival.ask-tomorrow)`; `SettledSuccessionCountAtLeast 1` |
| Text | `event.rainy-arrival.legacy-deferred` (new) |

Title: `门房里的旧雨`. Body: `雨声与当年一样。黑田伊织再次来到清洲，却先问起先代为何让他在门房等到天亮。新家主要给这段旧事一个答复。`

| Option | Text | Effect |
|---|---|---|
| `option.legacy-deferred.atone` | `补上先代欠下的礼数` | Iori +5 |
| `option.legacy-deferred.explain` | `说明当日不得已` | none |
| `option.legacy-deferred.dismiss` | `旧事不必再提` | Iori -5 |

Option bodies and summaries:

- `atone`: body `正式迎伊织入内，为当年那一夜的怠慢致歉。`; summary `伊织接受了迟来的礼数，对家族的信任增加五点。`
- `explain`: body `把当年的顾虑与今日的处境一并说清，请他自行判断。`; summary `伊织听完了解释，旧怨没有加深，也没有完全消散。`
- `dismiss`: body `告诉伊织，先代的做法无须由新家主补偿。`; summary `伊织记住了这次拒绝，对家族的信任下降五点。`

All follow-up option requirements are explicit `Always`. Effects stay inside the existing `RelationshipDelta` allowlist and the `[-100, 100]` range. The text frames Iori's standing with the house, not the new head's private relationship; the current model has one family-level NPC relationship scalar and this slice does not claim personal relationships migrate.

No third generation, no new NPC, no new root event, and no change to the accepted rainy-arrival text or option effects/requirements.

## 5. Exact behavior

### 5.1 Generation gating

- Root: offered only while `SuccessionCount == 0`. After any handoff (adult, regency start, crisis adoption), raw count is at least `1`, so the root is never offered again.
- Follow-up: offered only while `SettledSuccessionCount >= 1` and the matching root option is the last committed history entry for `event.rainy-arrival`.
- If the root was never chosen before the first handoff, no follow-up exists. The generation boundary is the first handoff; later choices cannot reopen the root because its offer gate is now false.
- If a root event was already offered before the handoff and is still pending, it remains resolvable after the handoff (offer-time gate). Its legacy can then appear in a later month. This is an explicit edge case, not the Play Test path.

### 5.2 Adult handoff

`ConfirmSuccession` (or a valid designated-heir handoff, or crisis adoption) succeeds with `SuccessionPhase.None` and `SuccessionCount == 1`.

- The handoff itself does not queue an event; SC-010's modal, feedback, and stable state are unchanged.
- On the next `结束月份`, `ScheduleMonthly` sees the root gate false, the follow-up gate true, and the matching history option. It schedules exactly one variant for the new head.
- The event is shown by the existing event-choice panel after the month advances. No new UI or scene binding is required.

### 5.3 Regency handoff

`ConfirmSuccession` for a minor succeeds with `SuccessionPhase.Regency` and `SuccessionCount == 1`.

- `SettledSuccessionCount` is `0` while the regency is active, so neither variant is offered during the regency. This respects GDD 19.2's restricted regency decisions and avoids treating the regent as the recipient.
- Month turns continue under the existing SC-010 rules; no follow-up is scheduled.
- `TryCompleteSuccessionRegency` at the adulthood threshold moves the phase to `None` with the same head and count. `SettledSuccessionCount` becomes `1`.
- On the next `结束月份` after that explicit handoff, the matching variant is scheduled for the now-adult head. The handoff itself still does not advance time or grant a second month budget.
- If the family becomes extinct, settled count stays `0`; no follow-up is offered and time remains blocked.

### 5.4 Duplicate and repeated succession

- The follow-up is once per campaign (`PerGame`) and keyed by the root history entry. It cannot repeat after a second or later succession.
- A second succession before the follow-up is resolved does not duplicate it. If the follow-up is already pending, it remains resolvable because succession gates are offer-time only, and the current head receives it. If it has not yet been scheduled, a pending or active succession keeps settled count at `0`; after the next handoff settles, the follow-up becomes eligible again and is scheduled at the next month-end. This is an edge case; the Play Test completes the follow-up before starting another succession.
- A second succession after the follow-up is resolved does not reopen the root and does not schedule another follow-up. The once marker and history are persisted.
- Loading a save from before the follow-up and replaying the handoff is a rolled-back timeline, not a duplicate. The once marker and history from that save govern.
- Extinction, a pending succession, and an active regency all keep settled count at `0`, so no follow-up is offered.

### 5.5 Deterministic ordering

- Monthly candidate ordering is unchanged: priority descending, then ordinal `eventId`; weighted roll only within the top-priority group.
- At count `0`, the two follow-ups are ineligible, so the accepted root remains the only top-priority candidate. The accepted demo order does not change.
- At settled count `>= 1`, exactly one variant can be eligible: the last root history entry has one option, and the two variants key different options. `weight = 1` with one candidate is a fixed selection. The catalog validator additionally rejects two variants claiming the same `(sourceEventId, sourceOptionId)` pair.
- At most one event is queued per month, as in SC-005–SC-009. No queue semantics change.
- History order is append-only and preserved by save/load; "last entry" is stable.

### 5.6 Failed scheduling

- If `ScheduleMonthly` fails, the existing `TryAdvanceToNextMonth` path restores the RNG snapshot and does not advance the clock or replace runtime state. Outcome is `SchedulingFailed` with the engine message.
- If the catalog/text is unavailable or invalid, outcome is `ContentUnavailable`; no partial event is queued.
- Because there is no persisted "due" flag, a retry on the next `结束月份` attempt is allowed. A deterministic content error keeps failing with the same message; succession state, pending decisions, and saves stay intact.
- If no variant matches (root not chosen, count not settled), scheduling succeeds with no event and the month advances. That is "not due", not a failure.
- Coverage boundary: the no-mutation guarantee is covered by an authored direct `EventEngine` test (a failed `ScheduleMonthly` from pending/non-management readiness leaves the runtime snapshot and event RNG unchanged). Full session-level date/succession failure injection is **deferred/unreachable without a production hook**, because a valid catalog plus the session's own guards leave no reachable `SchedulingFailed` path. No test hook was added and the existing rollback code is unchanged; that session-level check remains a Unity/manual gate.

### 5.7 Stable save points

All points below use the existing `StableGamePhase.Management` or `AwaitingEventChoice` model. No save is taken during command execution.

| Point | Persisted evidence | On load |
|---|---|---|
| Root pending | pending event, phase, history empty | same root and options |
| After root choice | history entry, committed result, once marker, relationship delta | same choice; root not re-offered |
| Succession pending | SC-010 succession fields | same phase/candidates/pending selection |
| Adult handoff, before next month | phase `None`, count `1`, no pending event | next `结束月份` schedules the matching variant |
| Regency active / after majority | phase, head, count, regency flag | no follow-up during regency; after majority the next month schedules it |
| Follow-up pending | pending event ID, phase, history, relationships | same variant and option set |
| After follow-up choice | history with both events, once/committed markers, relationship delta | no repeat; later succession does not reopen the chain |

Authored coverage (not executed): `RootPendingSaveRestoresSameRootAndOptions`, `AfterRootChoiceSaveDoesNotReofferRoot`, `PostHandoffSaveReplaysSameVariant`, `RegencyCheckpointBeforeFollowUpReplaysAfterMajority`, `FollowUpPendingSaveKeepsVariantOptionsAndRelationshipValue`, and `AfterFollowUpChoiceReloadKeepsHistoryAndOnceMarker`. Succession-pending save/load remains covered by the existing SC-010 `SuccessionSaveTests`.

## 6. Content version and save compatibility

- Catalog and text `contentVersion` become `2`; `SaveData.CurrentEventContentVersion` becomes `2`. The resource file names stay `event-catalog-v1.json` and `event-text-zh-v1.json` to avoid asset moves and GUID churn; the internal version is the contract.
- Save schema stays `3`: no new persisted field is added. Pending events, history, once markers, and succession state already exist.
- New saves write content `2`.

Compatibility matrix:

| Save | Saved content | Current content | Result |
|---|---|---|---|
| v3 | `1` | `2` | Accept only if `TryRestoreState` validates every referenced ID; the next save writes `2`. |
| v3 | `2` | `2` | Accept. |
| v3 | any other value | `2` | Reject atomically with the existing content-version message; the live session and the save file are unchanged. |
| v2 | `1` (required) | `2` | `SaveDataValidator.ValidatePresence` rejects a v2 payload that omits `eventContentVersion` as missing required metadata. With `1`, migrate schema to v3, then accept through the same `1 → 2` additive window if the event runtime validates. |
| v1 | missing/default | `2` | `ValidatePresence` deliberately skips the metadata check for v1, so the migrator supplies the current content version (`2`) with schema v3; accept. |

Schema v2 is a pre-content-2 format, so a genuine v2 save carries `eventContentVersion` `1` and must include that field: a v2 payload that omits it is rejected by `SaveDataValidator.ValidatePresence` as missing required metadata rather than defaulted. Schema v1 is the exception: it predates content versioning, so `ValidatePresence` skips the metadata check and the migrator writes the current content version `2`.

The `1 → 2` window is one-way and is allowed because the only previously accepted content is preserved: the root's text, options, effects, and requirements are unchanged and the new events cannot be referenced by a content-1 save. Any unknown saved event/option still fails `TryRestoreState`. There is no `2 → 1` downgrade and no promise for content `3`; a future version must define its own compatibility.

A content-1 save that has a pending root and a completed handoff can still resolve the root because succession-count gates are offer-time only. Its follow-up then appears the next month. This is an explicit migration edge case; the Play Test starts a new game and does not rely on it.

## 7. Acceptance and evidence boundary

The user reported the text-only Play Test passed and explicitly accepted this slice on 2026-11-02. Their report also says compilation passed, EditMode passed, and there were no Console errors. EditMode totals, Console warning count, and `FoundationSceneValidator` result were not provided.

The Play Test covers:

1. New game → `结束月份` → rainy-arrival → choose `请他进屋歇息` → save.
2. Retire → choose 明智 正綱 → confirm; head changes once; date/location/mission/relationship state otherwise intact; save.
3. `结束月份` → variant A (旧客的雨夜回访) → load the step-2 save before choosing (the game has one save slot), `结束月份` again → same variant A; choose `重申先代的门诺`; save; `结束月份` again → no repeat.
4. Load the post-choice save from step 3 → `结束月份` → no repeat and no reopened root.
5. New game → before the first `结束月份`, travel to Atsuta and use 黒田伊织's town dialogue to raise his trust to at least 1 (accept his proposition), then return to the map. This setup is required because the accepted `请他明晨再来` option keeps its existing `RelationshipAtLeast 1` requirement and is disabled at the default value 0. Then `结束月份` → choose `请他明晨再来` → retire to 正綱 → `结束月份` → variant B (门房里的旧雨) with a different body and different option titles; choose `补上先代欠下的礼数`.
6. Record each step pass/fail and the visible text in the test record, without screenshots.

Regency behavior is covered by authored EditMode tests; the user reports EditMode passed but did not provide per-suite counts. Do not infer the scene validator passed. User acceptance authorizes the SC-011 checkpoint integration; it does not supply missing counts or scene-validation evidence.

## 8. Out of scope

- A new monthly root event, a new NPC, a displaced-youth character, or a third-generation event.
- SC-012 chronicle, faction politics, economy costs, and regency-only decision content.
- New packages, external narrative runtimes, dynamic text templating, or arbitrary condition/effect scripts.
- Scene, prefab, `ProjectSettings`, `Library`, or `Temp` changes; no new worktree or branch in this design task.
- Rewriting SC-003/SC-010 models or changing `PlayerMovement2D`.

## 9. Risks and decisions

- **Risk:** treating two content variants as two events. **Decision:** they are one follow-up step; the spec and tests name the encounter and assert exactly one variant can be eligible.
- **Risk:** content-version churn breaking old saves. **Decision:** a narrow `1 → 2` additive window with authored (not yet executed) coverage; any unknown ID still rejects; no general downgrade.
- **Risk:** a pending event becoming uncommittable after a head change. **Decision:** succession-count gates are offer-time only; only history conditions are rechecked at commit.
- **Risk:** Iori being read as a retainer or displaced youth. **Decision:** text calls him 旧客 and the spec explicitly denies the conflation.
- **Open decision:** whether a future slice should model per-head personal relationships. This slice uses the existing family-level scalar and makes no such claim.
