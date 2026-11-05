# SC-018 Independent Family Events Implementation Report

Date: 2026-11-03
Implementation branch: `feature/sc018-independent-core-events`
Worktree: `C:\Users\panye\My project`
Spec: `docs/superpowers/specs/2026-11-03-sc-018-independent-family-events-design.md`
Plan: `docs/superpowers/plans/2026-11-03-sc-018-independent-family-events.md`
Play script: `SC018_DEMO.md`

## Outcome

The M2 event catalog now contains 30 events and the same five authored chains.
Seven standalone family events were appended under `event.sc018.*`; each is
`Monthly`, `PerGame`, `scenario: "sc013"`, has no `chainId`, and has two options
with visible family-relationship outcomes. The Chinese localization adds 35
original entries (7 events + 14 options + 14 summaries).

The approved family-relationship semantics are implemented as
`(currentHeadPersonId, familyMemberPersonId)` keys. Each pair value is stored
independently of other heads and of other members. Pair order identifies the two
people; the localized event summaries describe the affected family member's trust
change toward the current head. Succession does not copy the outgoing head's
values; old-head records remain attached to the old person and new-head values
read as zero until changed. NPC `CharacterRelationships` behavior is unchanged.

Save format is now v5. v4 saves migrate with an empty family-relationship set;
no values are inferred from kinship, NPC relations, or the current head. The
chronicle records each actual family change with both resolved names and exact
before/after values.

This implementation worker did not open, drive, or run Unity. Static JSON checks,
an eight-assembly host-side Roslyn compile, and a pure C# logic harness were
performed here. The coordinator later ran Unity verification on 2026-11-03
(Editor compilation, EditMode 824/824, Play Mode, v5 save/load); see
[Unity verification](#unity-verification-coordinator-run-2026-11-03). Scene
validation was not run, and user acceptance is still pending.

This continuation added a real `SuccessionService` handoff test, a
`SaveSlotStore` JSON round-trip test, and a `GameSession` self/non-member
rejection test, then re-ran the JSON validator, the eight-assembly type-check,
the player assembly type-check, and the pure C# harness, which had 67 checks at
that point and was later expanded to 82 in the review-correction pass.

The review-correction pass resolved M1 and L1: the four events whose target
kinship changes across generations now use generation-neutral wording, and
family event deltas clamp to `[-100, 100]` while reporting the actual applied
before/after values. The static validator, eight-assembly type-check, player
type-check, and expanded 82-check host harness were re-run successfully.

## Unity verification (coordinator-run, 2026-11-03)

Performed by the coordinator in the existing Unity 6.6.6f1 Editor on
`Assets/Scenes/FoundationScene.unity`, under the user's temporary authorization.
This section records the coordinator's observed results. It is not a claim by the
implementation worker, and it does not replace user acceptance.

### EditMode

- Result file: `%USERPROFILE%\AppData\LocalLow\DefaultCompany\My project\TestResults.xml`.
- `824 passed / 0 failed / 0 skipped` (`testcasecount="824"`,
  `result="Passed"`); the XML records start/end `2026-11-03 21:12:19Z` and
  `2026-11-03 21:12:24Z`.
- The file contains the
  `SengokuChronicle.Tests.EditMode.SC018IndependentFamilyEventsTests` fixture
  with 20 test cases, all passed.

### Compilation

- `Logs/Editor.log` shows the latest script recompilations completing with
  `*** Tundra build success` at the lines the coordinator recorded as 8280 and
  10579; a 1-based `grep` finds them at 8279 and 10578. These are the same two
  records, differing only in the counting base.
- Earlier in the same session the log contains transient `CS0117`
  (`AddSC013FamilyRosterPanel.RowTemplateName`) and `CS7036`
  (`FamilyRosterPanel.BindReferences` missing `edges`) errors in the unrelated
  `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs` (around lines
  7575–7586 and 8154–8165). Those errors do not appear after the successful
  builds. The sequence is recorded as observed, not hidden.

### Play Mode

- A fresh M2 game was started from the title screen.
- The five prerequisite events were followed in the documented order: SC-013
  family council, rainy arrival, SC-016 ledger entrustment, SC-015 family letter,
  then the SC-014 refugee event. That order matches the authored priorities
  (SC-016 `8` > SC-015 `6` > SC-014 `5`). The SC-018 events were handled after
  them; the SC-011 legacy callback ran later, after the first succession.
- All seven `event.sc018.*` events were observed exactly once each.
- Two successions completed, the SC-011 legacy callback ran, and the SC-013
  trust-gated third-generation aid ending was reached.
- Family-relationship chronicle entries were checked for before/after values.
- A schema-v5 slot was saved with four family-relationship records and 30
  chronicle entries. Loading it, advancing past the save point, then loading
  again restored the July 1561 state and history.
- The first attempt skipped three prerequisite events because of a tester
  procedure error; that attempt was discarded without saving. The clean rerun
  followed the documented flow and passed.

### Save-data safety and Console

- The user's saved slot JSON and its `.bak` were backed up before testing and
  restored byte-for-byte afterwards; no user save was left changed.
- Console before Play Mode showed 0 errors and 0 warnings, plus one informational
  `NoSubscription` message repeated five times. The final Console count after the
  tests was not directly observed, so zero is not claimed for that later point.

### Not covered

- `FoundationSceneValidator` did not run; scene validation is not claimed.
- User Play Test acceptance is still pending. This authorized coordinator run
  does not constitute user acceptance or milestone sign-off.

## Cast mapping and adaptation

The current SC-013 roster contains six `person.sc013.*` members:

| Person ID | Display name | Role |
|---|---|---|
| `person.sc013.harunobu` | 明智 晴信 | Head |
| `person.sc013.chie` | 明智 千恵 | Spouse |
| `person.sc013.masatsuna` | 明智 正綱 | DirectKin |
| `person.sc013.nobumasa` | 明智 信正 | DirectKin |
| `person.sc013.tokihime` | 明智 時姫 | CollateralKin |
| `person.sc013.sakon` | 佐近 | Retainer |

The current roster has no `AdoptedChild` member. Following the design and the
coordinator's roster guard, all seven events keep their approved family themes
with the documented target/wording adaptations and use existing non-head family
members as targets:

- `sibling-duty` → 時姫 (first-generation sibling; later generations an elder
  collateral relative). The text says “家人/時姫”, never “手足”, so it stays
  accurate after succession.
- `child-study` → 信正 (younger direct kin in the fixed line; the first-generation
  head’s grandson). It is generation-one gated and the text no longer calls him a
  “孩子” or a child of the current head.
- `parent-care` → 千恵 (the first-generation spouse, later the head’s mother or
  grandmother). The text says a family member is ill and never calls her
  “长辈”; it is accurate in every generation.
- `heirloom` → 正綱 and 時姫 move in opposite directions (generation-one gated).
- `spouse-visit` → 千恵 (a family member visiting her birth family). The text uses
  “探亲/回娘家” rather than the spouse-specific “归宁” or “配偶”, so it stays
  accurate when the head is her son or grandson.
- `adoption-welcome` → adapted to 時姫 asking whether a collateral relative is
  truly part of the household. The text does **not** call her adopted and adds
  no kinship or adoption fact.
- `family-assembly` → 正綱 and 信正 (generation-one gated).

No character, kinship link, adoption relation, or NPC relationship target was
invented. Events whose target can become a later head (`正綱`, `信正`) carry the
existing `SuccessionCountEquals 0` condition, so a head can never become their
own relationship target. The four ungated events target only 千恵/時姫, who are
not heads in the fixed M2 route. That makes them mechanically safe in later
generations; the earlier version additionally asserted first-generation kinship
roles (“手足”, “配偶”, “归宁”, “孩子”), which the review correctly rejected. Those
four events now use generation-neutral wording and a focused test guards it.

## Content added

| Event ID | Title | Options | Family effects |
|---|---|---|---|
| `event.sc018.sibling-duty` | 家人的夜巡 | `let-rest` / `keep-watch` | 時姫 `+5` / `-5` |
| `event.sc018.child-study` | 灯下的课业 | `allow-study` / `keep-training` | 信正 `+5` / `-5` |
| `event.sc018.parent-care` | 病榻前的轮值 | `nurse-personally` / `rotate-care` | 千恵 `+5` / `-5` |
| `event.sc018.heirloom` | 旧茶碗的归处 | `keep-in-main-hall` / `keep-with-user` | 正綱 `+5`、時姫 `-5` / 正綱 `-5`、時姫 `+5` |
| `event.sc018.spouse-visit` | 探亲的行李 | `accompany` / `postpone` | 千恵 `+5` / `-5` |
| `event.sc018.adoption-welcome` | 家中的位置 | `welcome-as-family` / `keep-guest-distance` | 時姫 `+5` / `-5` |
| `event.sc018.family-assembly` | 家议上的分工 | `open-floor` / `decide-directly` | 正綱、信正各 `+5` / 各 `-5` |

Every effect uses the new `FamilyRelationshipDelta` kind and a typed
`targetPersonId`. Every summary names the affected family member(s) and states
"增加五点" or "下降五点". No new text mentions an NPC name.

## Scheduling and generation gates

`EventEngine` sorts candidates by priority descending and schedules only from the
highest eligible priority group. All seven rows use `priority: 1`, `weight: 10`.
The lowest pre-existing priority is `5` (SC-014 first-generation entry), so the
new optional rows cannot outrank `event.rainy-arrival` (`10`), SC-011 legacy rows
(`100`), or SC-013 pledge/finale (`1000`).

`child-study`, `heirloom`, and `family-assembly` also use
`SuccessionCountEquals 0`. This keeps 正綱/信正 non-heads when those events are
offered. The four ungated rows target only 千恵/時姫, so they are mechanically
safe in generations two and three. Their text is generation-neutral by design:
it never asserts that the target is the current head’s sibling, spouse, child,
or elder. `child-study` remains generation-one gated for the target-membership
reason above, and its text also avoids calling 信正 the head’s child. All seven
share priority `1`; their mutual order is chosen by the existing weighted random
stream, and each is `PerGame`.

## Runtime implementation

### Domain

- `Assets/Scripts/Domain/FamilyRelationships.cs` (new) provides
  `FamilyRelationships`, `FamilyRelationshipKey`, `FamilyRelationshipEntry`, and
  `FamilyRelationshipResult`.
- The store is keyed by ordered `(head, member)` pairs, rejects empty/self pairs,
  clamps to `[-100, 100]`, and snapshots entries in stable head/member order.
- Pair order is a storage key only; the event summaries define the narrative
  meaning (the family member's trust toward the current head).
- It is separate from `CharacterRelationships`; NPC behavior and persistence are
  untouched.

### Events

- `EventCatalogData.EventEffectKind` adds `FamilyRelationshipDelta`;
  `EventEffectData` adds `targetPersonId`.
- `EventCatalogValidator` validates NPC targets for `RelationshipDelta` and
  family person IDs for `FamilyRelationshipDelta`, rejecting mixed target fields
  and invalid deltas. Family membership and self-pairs are enforced at runtime
  because the validator has no active succession state.
- `EventRuleContext` carries the current head `PersonId` and the active family
  member IDs.
- `EventEngine.TryChooseEventOption` accepts an optional `FamilyRelationships`
  store, preflights family effects against the current head/member set, applies
  NPC and family deltas transactionally, rolls both back on failure, and exposes
  `EventChoiceResult.AppliedFamilyRelationships`.
- Family event deltas now clamp to `[-100, 100]` during preflight, matching
  `FamilyRelationships` and plan Task 2. Each commit carries the **actual** applied
  delta (`clamped target − previous`), so rollback and the chronicle use the exact
  before/after values. A delta at an already-full boundary succeeds as a no-op and
  adds no chronicle row. NPC relationship deltas keep their existing
  overflow-rejection behavior; only the family path changed.
- `GameSession.BuildEventRuleContext` supplies the current head and member IDs;
  `TryChooseEventOption` passes the family store and records every applied family
  change. The direct `GameSession` change API only accepts the current head;
  old-head pairs remain readable history and are never rewritten by a later head.

### Persistence

- `SaveData` adds `FamilyRelationshipSaveEntry[] familyRelationships` and
  initializes it to an empty array.
- `SaveDataValidator.CurrentVersion` is `5`; presence validation requires the
  `familyRelationships` root property for v5 and still allows v1–v4 migration.
- `ValidateFamilyRelationships` rejects null entries, unknown IDs, non-member
  references, self-pairs, duplicate keys, and out-of-range values.
- `SaveDataMigrator` adds a v4→v5 branch that preserves existing v4 data and
  initializes an empty family set. v1–v3 paths also initialize it empty. A v4
  payload that already contains family values is rejected rather than silently
  discarded.
- `GameSession.CaptureSave` emits person-pair entries; `TryApplySave` rebuilds
  the store after validating membership and duplicate keys.

### Chronicle and display

- `GameSession.RecordFamilyRelationshipChange` appends a `Relationship`
  chronicle record with the current head and member display names (resolved from
  `SuccessionWorld.TryGetPerson`), a neutral `A 与 B` title, and the exact
  `from → to` values in the summary.
- The existing chronicle panel already renders title/summary plus
  `关系 from → to`, so no scene or panel code changed. Raw `person.sc013.*` IDs
  are never used as the title.
- Event-choice feedback continues to use the localized option summary, which
  states the visible `±5` outcome.

## Test coverage added

`Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs` (new, plus `.meta`)
contains focused tests for:

1. Both catalogs parse, validate, and contain exactly 30 events / 5 chains with
   unique event IDs and exactly seven `event.sc018.*` rows.
2. The five chain IDs and their linked event ID order are unchanged; the 23 prior
   event definitions match a captured baseline of chain, priority, weight, option
   count, condition count, and text ID.
3. All seven new rows are `Monthly`, `PerGame`, `sc013`, cooldown `0`, weight
   positive, chainless, and absent from every chain's `linkedEventIds`.
4. The three generation-sensitive rows have exactly one
   `SuccessionCountEquals 0` condition; the other four have no conditions.
5. Every option uses `FamilyRelationshipDelta` with a valid non-head family
   `PersonId`, no NPC target, a nonzero in-range delta, and a generation gate
   whenever the target can become a later head.
6. All 35 new text IDs exist once, are non-empty, name family members, and every
   summary contains "五点" plus a visible increase/decrease statement; no new text
   names an NPC.
7. New priorities are lower than every mandatory M2/SC-011 event, and all seven
   are eligible alongside the generation-one fixed events with the pledge still
   selected first.
8. Generation-two/three eligibility excludes the gated rows and keeps the four
   mechanically safe rows. `GenerationAdaptiveEventTextAvoidsHeadRelativeRoleClaims`
   then checks all title/body/option/summary text for those four events and fails
   on any head-relative role word (“手足”, “配偶”, “归宁”, “子女”, “孩子”, “长辈”,
   etc.), plus positive assertions for “家人/探亲” wording.
9. `FamilyRelationships` keys by head, preserves old-head values, starts a new
   head at zero, rejects self/empty pairs, and clamps at both ends.
   `SuccessionHandoffKeepsOldHeadPairsAndNewHeadStartsAtZero` additionally runs a
   real designated-heir handoff through `SuccessionService`, then verifies the old
   head remains addressable and the incoming head reads its own zero record.
10. Family event effects apply and report results; self/non-member targets fail;
    a two-effect preflight failure leaves no partial family delta; NPC effects
    still work unchanged.
11. In-memory save round-trip and a real `SaveSlotStore` JSON round-trip preserve
    the pair, exact value, and chronicle names/values; v4 migration adds an empty
    set and a representative v3 migration also initializes it empty; a v4 save with
    values is rejected; self-pairs, unknown members, duplicate person pairs, and
    out-of-range values are rejected by validation.
12. The chronicle records both full display names and exact `0 → 5`, `5 → 10`
    values without raw IDs.
13. `GameSession.TryChangeFamilyRelationship` rejects a self target, an
    unknown non-member, and a non-current head before changing the store or
    appending a chronicle record.
14. A v4 presence probe still requires the `chronicle` root property while
    allowing `familyRelationships` to be absent for migration; a current-version
    probe requires the `familyRelationships` root property.
15. `FamilyEventDeltaClampsAtBothBoundsAndReportsActualValues` drives the event
    path at `98→100` and `-98→-100`, asserts the reported previous/current values
    and the actual `±2` change, verifies a full-boundary no-op adds no fake result,
    and verifies two effects on one pair clamp once with the exact net change.
16. `FamilyChronicleRecordsActualClampedBoundaryValues` asserts the chronicle text
    and stored from/to values are the actual `98 → 100` clamped values, never the
    requested `103`, and that a full-boundary no-op appends no row.
17. `FamilyEventPathChroniclesActualClampedValues` restores a real pending
    `sibling-duty` event into a `GameSession`, starts the pair at `98`, chooses the
    `+5` option, and asserts both the applied result and the chronicle entry are
    `98 → 100`, not `103`.

`Assets/Tests/EditMode/SaveDataValidatorTests.cs` updates the obsolete
"version 5 is unsupported" case to version 4, since `5` is now current.

## Verification evidence

### Obtained in this run

**1. JSON parse and static content validation — passed, 0 errors.**

Command (host-side Python 3.14.8, run from the project root):

```text
PYTHONIOENCODING=utf-8 python C:/Users/panye/AppData/Local/Temp/opencode/sc018/validate.py
events=30 chains=5 texts=139 new=7
targets=person.sc013.chie,person.sc013.masatsuna,person.sc013.nobumasa,person.sc013.tokihime
errors=0
```

The validator checks: content versions `2/2`; 30 events; 5 chains; 139 text
entries; unique event/option/text IDs; complete event/option/summary references;
supported trigger/scope/scenario/condition/requirement/effect values; family
targets in the M2 roster and not the current head; generation gates for
future-head targets; nonzero in-range deltas; summary family names and "五点";
no NPC names in new text; priority below `5`; and byte-prefix preservation of
prior catalog/text content after normalizing CRLF.

**2. Eight-assembly Roslyn type-check — passed, 0 diagnostics.**

Command host:

```text
python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile.py
== Domain exit 0
== Persistence exit 0
== Events exit 0
== Application exit 0
== Presentation exit 0
== Editor exit 0
== EditModeTests exit 0
ALL OK

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile_player.py
exit 0
```

The continuation generated response files from the existing Unity Bee response
files, referenced the freshly compiled project assemblies, added the new
`FamilyRelationships.cs` / `SC018IndependentFamilyEventsTests.cs` sources, and
invoked Unity 6.6.6f1's bundled Roslyn `csc.dll` through its bundled
`dotnet.exe`. Assemblies compiled in dependency order: Domain, Persistence,
Events, Application, Presentation, Editor, PlayerMovement2D, EditModeTests.
Result: every invocation exit code `0` with no compiler output (0 diagnostics).
This is a host-side type-check, **not** Unity Editor compilation or a Test Runner
result.

**3. Pure C# logic harness — 67/67 checks passed (earlier continuation run; the
current host harness count is 82/82, see §6).**

Command (host-side, run from the project root):

```text
python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/run_harness.py
compile exit 0
run exit 0
passed=67 failed=0
```

The harness references the freshly compiled Domain/Persistence/Events assemblies
and exercises the behavior without Unity. It covers:

- person-pair keying, old-head retention, new-head zero start, self/empty
  rejection, upper/lower clamping;
- a real `SuccessionService` designated-heir handoff, confirming the old head
  remains a family member and the new head reads its own zero pair;
- family event application, result reporting, NPC store isolation, self/non-member
  rejection, no partial delta on preflight failure, and preserved NPC effects;
- v5 family save validation, self/unknown/member rejection, duplicate-pair and
  range rejection;
- v4→v5 migration with an empty set, rejection of a v4 save that already contains
  family values, and preserved v3/v1 migration paths;
- a chronicle relationship record with both display names and exact `0 → 5`
  values, including `ChronicleSaveCodec` capture/restore.

**4. Post-continuation re-run — same results, 0 errors / 0 diagnostics / 67
passed at that harness size (later expanded to 82, see §6).**

After adding the `GameSession` self/non-member rejection test, all checks were
re-run from the project root with the same commands:

```text
PYTHONIOENCODING=utf-8 python C:/Users/panye/AppData/Local/Temp/opencode/sc018/validate.py
events=30 chains=5 texts=139 new=7
targets=person.sc013.chie,person.sc013.masatsuna,person.sc013.nobumasa,person.sc013.tokihime
errors=0

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile.py
== Domain exit 0
== Persistence exit 0
== Events exit 0
== Application exit 0
== Presentation exit 0
== Editor exit 0
== EditModeTests exit 0
ALL OK

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile_player.py
exit 0

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/run_harness.py
compile exit 0
run exit 0
passed=67 failed=0
```

An additional one-off host check for the v4 presence rule passed: a v4 payload
without `chronicle` is rejected, while a v4 payload with `chronicle` and no
`familyRelationships` is accepted for migration.

**5. Post-cleanup re-run — same host-side results (67 checks then; 82 after the
review-correction additions in §6).**

After making the pair-direction wording neutral in comments/docs and changing the
family chronicle title from `A → B` to `A 与 B` (the summary still carries the
exact `from → to` values), the same commands were re-run from the project root:

```text
PYTHONIOENCODING=utf-8 python C:/Users/panye/AppData/Local/Temp/opencode/sc018/validate.py
events=30 chains=5 texts=139 new=7
errors=0

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile.py
== Domain exit 0
== Persistence exit 0
== Events exit 0
== Application exit 0
== Presentation exit 0
== Editor exit 0
== EditModeTests exit 0
ALL OK

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile_player.py
exit 0

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/run_harness.py
compile exit 0
run exit 0
passed=67 failed=0
```

The chronicle-title change was only type-checked here; the focused EditMode
assertions still check both display names and the exact numeric summary. The Test
Runner was **NOT RUN** at that point; the coordinator's later 2026-11-03 EditMode
run passed 824/824 (see
[Unity verification](#unity-verification-coordinator-run-2026-11-03)).

These are host-side static/type-check/logic checks only. They are **not** Unity
Editor compilation, EditMode Test Runner, Console counts, scene validation,
Play Mode, or user acceptance.

**6. Review-correction re-run (M1 narrative + L1 clamp) — passed, host-side only;
current host harness 82/82.**

After rewriting the four generation-adaptive event texts, adding the neutral-
wording regression test, and changing family event effects to clamp with the
actual applied delta, all four host-side commands were re-run from the project
root:

```text
PYTHONIOENCODING=utf-8 python C:/Users/panye/AppData/Local/Temp/opencode/sc018/validate.py
events=30 chains=5 texts=139 new=7
targets=person.sc013.chie,person.sc013.masatsuna,person.sc013.nobumasa,person.sc013.tokihime
errors=0

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile.py
== Domain exit 0
== Persistence exit 0
== Events exit 0
== Application exit 0
== Presentation exit 0
== Editor exit 0
== EditModeTests exit 0
ALL OK

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/compile_player.py
exit 0

python C:/Users/panye/AppData/Local/Temp/opencode/sc018-check/run_harness.py
compile exit 0
run exit 0
passed=82 failed=0
```

The validator now also rejects head-relative role words (“手足”, “配偶”, “归宁”,
“子女”, “孩子”, “长辈”, etc.) in the title/body/options/summaries of
`sibling-duty`, `child-study`, `parent-care`, and `spouse-visit`. The host harness
gained boundary checks for the `98→100` and `-98→-100` event paths (each reports
the actual `±2` change) and for a full-boundary `+5` (which succeeds without a
fabricated result). The host harness was **not** extended with the
repeated-effect case. That case — two effects on the same pair clamping once and
reporting one exact net change, with a single chronicle row — is covered only by
the EditMode test `FamilyEventDeltaClampsAtBothBoundsAndReportsActualValues`.
The EditMode suite also adds
`FamilyChronicleRecordsActualClampedBoundaryValues` and
`FamilyEventPathChroniclesActualClampedValues`, which assert the chronicle stores
`98 → 100` rather than the requested `103` (the second drives a real pending
`sibling-duty` event through `GameSession`). All three EditMode tests were
type-checked here; they were later executed by the coordinator's 2026-11-03
EditMode run, which passed 824/824 (see
[Unity verification](#unity-verification-coordinator-run-2026-11-03)). The host
checks above remain host-side checks, not Unity EditMode execution.

### Not obtained by the implementation worker

The following describes this implementation worker's run. The coordinator's
2026-11-03 Unity verification above supersedes the runtime items; scene validation
and user acceptance remain outstanding.

- Unity Editor compilation after this change: **NOT RUN BY THIS WORKER**. The
  user's Editor was running on this project (`Unity.exe`, project path
  `C:\Users\panye\My project`). A read-only look at `Logs/Editor.log` at that time
  showed an older successful Tundra build and earlier SC-017 compilation errors,
  and the log was last written before the current SC-018 source edits, so it was
  **not** evidence for this change then. The coordinator later obtained successful
  builds, recorded at `Logs/Editor.log` lines 8280 and 10579 (1-based `grep`:
  8279 and 10578).
- EditMode Test Runner, including the new `SC018IndependentFamilyEventsTests`:
  **NOT RUN BY THIS WORKER**; later passed 824/824 (20 SC-018 cases) in the
  coordinator's run.
- Console error/warning/log counts: **NOT INSPECTED BY THIS WORKER**; the
  coordinator recorded 0 errors / 0 warnings before Play Mode, plus one
  informational `NoSubscription` message repeated five times. The final count
  after testing was not directly observed.
- `FoundationSceneValidator`: **NOT RUN**; no scene was changed. Still not run.
- Play Mode: **NOT RUN BY THIS WORKER**; the coordinator's authorized Play Mode
  run passed the documented flow.
- Save/load Play Mode verification: **NOT RUN BY THIS WORKER**; the pure harness
  covered save validation, migration, and chronicle codec behavior only. The
  coordinator later verified a schema-v5 slot with four family-relationship
  records and 30 chronicle entries, including a post-save advance and reload back
  to the July 1561 state.
- User Play Test and acceptance: **PENDING**.

Static JSON checks, the host-side Roslyn compile, and the pure harness are not
Unity runtime, Test Runner, Play Mode, or user-acceptance evidence.

## Preservation and workspace hygiene

- Prior 23 event definitions and all five chains are JSON-equal to the pre-change
  snapshot.
- Prior 104 localization entries are JSON-equal to the pre-change snapshot.
- Both catalogs are append-only at the byte level: the original content before the
  final closing bracket is preserved, and only a comma plus the new rows were
  inserted (verified after normalizing CRLF to LF).
- `contentVersion` remains `2` in both event/text catalogs.
- NPC `CharacterRelationships`, NPC event effects, event condition types, the five
  chains, SC-011–SC-017 content, scenes, prefabs, `ProjectSettings`, packages,
  existing `.meta` GUIDs, `Library`, and `Temp` were not changed.
- The only new `.meta` files created by this task are for `FamilyRelationships.cs`
  and the new SC-018 test. The pre-existing untracked `Assets/1.unity.meta` user
  WIP was not created or modified by this task.
- Existing dirty user files were not touched: `Assets/Scenes/SampleScene.unity`,
  `SC017_DEMO.md`, `docs/reports/sc017-implementation.md`, `Assets/1.unity`,
  `Assets/1.unity.meta`, `ProjectSettings/SceneTemplateSettings.json`,
  `graphify-out/`, and `nul`.
- No commit, push, merge, tag, or branch operation was performed.

## Changed files

New:

- `Assets/Scripts/Domain/FamilyRelationships.cs`
- `Assets/Scripts/Domain/FamilyRelationships.cs.meta`
- `Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs`
- `Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs.meta`
- `SC018_DEMO.md`
- `docs/reports/sc018-implementation.md`
- `docs/superpowers/specs/2026-11-03-sc-018-independent-family-events-design.md`
- `docs/superpowers/plans/2026-11-03-sc-018-independent-family-events.md`

Modified:

- `Assets/Resources/Events/event-catalog-v1.json` (7 appended/updated SC-018 events)
- `Assets/Resources/Localization/event-text-zh-v1.json` (35 appended texts)
- `Assets/Scripts/Events/EventCatalogData.cs`
- `Assets/Scripts/Events/EventCatalogValidator.cs`
- `Assets/Scripts/Events/EventEngine.cs`
- `Assets/Scripts/Events/EventRuleContext.cs`
- `Assets/Scripts/Domain/FamilyChronicleEntry.cs` (comment only)
- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Persistence/SaveData.cs`
- `Assets/Scripts/Persistence/SaveDataValidator.cs`
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- `Assets/Tests/EditMode/SaveDataValidatorTests.cs`
- `PROJECT_STATE.md`

Documentation update (2026-11-03, verification record only): `SC018_DEMO.md`,
`PROJECT_STATE.md`, and this report were edited to record the coordinator's Unity
run. No code, test, scene, prefab, `ProjectSettings`, package, `.meta`, save-slot,
`Library`, or `Temp` file was changed by that documentation task.

## Known limitations

- This implementation worker collected no Unity Editor or runtime evidence; the
  coordinator's 2026-11-03 run is recorded above. Scene validation remains unrun.
- The seven events are M2-only (`scenario: "sc013"`); they do not appear in the
  scenario-neutral SC-010 samples.
- All seven share priority `1`; their mutual order is randomized by the existing
  weighted stream and may differ between runs.
- Three events are generation-one gated because their targets can become later
  heads. If the player does not resolve them before the first succession, they do
  not reappear; the four ungated events remain available with safe targets and
  generation-neutral text.
- Event summaries state the authored `±5`; at the `[-100, 100]` boundary the actual
  applied change can be smaller, and the chronicle records the actual before/after
  values rather than the authored delta.
- The current roster has no adopted member, so `adoption-welcome` was adapted to
  an existing collateral relative's belonging question. This preserves the
  approved family-inclusion theme without inventing an adoption fact.
- Family relationships are narrative state only; no succession eligibility rule
  reads them.
- The user must still perform and accept the Play Test. The coordinator ran the
  EditMode suite (824/824), recorded Editor compilation, and observed Console
  counts before Play Mode; `FoundationSceneValidator` remains unrun.


## 后续 Git 集成（2026-11-04）

用户明确要求累计合并并推送后，SC-018 随 SC-018–SC-022 checkpoint `714cb3a` 推送到 `origin/main`。上文关于未提交/未集成的描述记录的是实现时状态；SC-018 用户 Play Test 与验收仍待办。
