# SC-014 流亡少年与家系遗产 — Independent Read-Only Review

Date: 2026-11-03  
Reviewer: independent read-only OpenCode2 review worker  
Branch: `feature/sc013-fixed-three-generation`  
Worktree: `C:\Users\panye\My project` (HEAD `88fe7c6`, worktree dirty)  
Inputs: `docs/superpowers/specs/2026-11-03-sc-014-refugee-legacy-design.md`,
`docs/superpowers/plans/2026-11-03-sc-014-refugee-legacy.md`,
`Assets/Resources/Events/event-catalog-v1.json`,
`Assets/Resources/Localization/event-text-zh-v1.json`,
`PROJECT_STATE.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `SC014_DEMO.md`,
plus `SC013_DEMO.md` and `docs/reports/sc013-review-final.md` as the SC-013 baseline record.

## Verdict

**ACCEPT** for the SC-014 data/documentation slice, with **six non-blocking findings** and **zero blocking defects**.

The four new nodes form one coherent cross-generation chain, every text/chain/source reference resolves,
the two third-generation nodes are mutually exclusive, the relationship effects are explainable and point at
the existing `npc.kuroda.iori` record, and the SC-011 rows are JSON-equal to `HEAD`.

This verdict is a **static, data-and-document review only**. It is **not** Unity Editor compilation, EditMode
execution, Console inspection, `FoundationSceneValidator`, Play Mode, save/load verification, or user
acceptance. No claim of runtime behavior is made. Only `docs/reports/sc014-review.md` was written.

## 1. Changed-file accounting

`git status --short` / `git diff --stat HEAD` show the worktree still carries the uncommitted SC-013 slice
plus the SC-014 slice. SC-014 content is confined to the six files named in the plan:

| File | Working-tree diff vs `HEAD` |
|---|---|
| `Assets/Resources/Events/event-catalog-v1.json` | 286 insertions / 0 deletions |
| `Assets/Resources/Localization/event-text-zh-v1.json` | 160 insertions / 0 deletions |
| `PROJECT_STATE.md` | +24/−1 (SC-014 section at `:60-66`) |
| `ARCHITECTURE.md` | +14 (SC-014 section at `:31-35`) |
| `DECISIONS.md` | +15 (`:5-10`) |
| `SC014_DEMO.md` | new, untracked |

These totals include the still-uncommitted SC-013 slice, because `HEAD` is the SC-012 checkpoint. §5 isolates
the SC-014 contribution to the two resource files (200 catalog and 110 text lines) against the SC-013 baseline
recorded in `docs/reports/sc013-review-final.md`.

Additional boundary checks:

- `grep -rn "refugee" Assets --include=*.cs` → no matches; no new stable-ID constants, code, or tests.
- `grep -c refugee Assets/Scenes/SampleScene.unity Assets/Scenes/FoundationScene.unity` → `0` / `0`.
- `git diff --stat HEAD -- Assets/Scenes/FoundationScene.unity ProjectSettings Packages` → empty
  (the dirty `SampleScene.unity`, untracked `ProjectSettings/SceneTemplateSettings.json`, `Assets/1.unity`,
  and `graphify-out/` are the pre-existing WIP recorded in `sc013-review-final.md:120`).
- `contentVersion` is `2` in both resource files (`event-catalog-v1.json:2`, `event-text-zh-v1.json:2`),
  unchanged from `HEAD`.

## 2. Structural checks (all pass)

Independently parsed with Python 3.14 (`json.load` with `utf-8-sig`) and compared against
`git show HEAD:<file>`:

| Check | Result |
|---|---|
| Event IDs unique (9 events), option IDs unique, chain IDs unique (3), text IDs unique (50) | no duplicates |
| Every event/option/summary `textId` resolves; no unused text entry | 0 missing, 0 unused |
| Chain membership is bidirectional (`chain.linkedEventIds` == events carrying that `chainId`) for all three chains | pass |
| Every `eventId` carrying a `chainId` is linked by that chain | pass |
| At most one `EventOptionChosen` per event; no two events claim the same `(sourceEventId, sourceOptionId)` | pass |
| Each `EventOptionChosen` source event/option exists, is not self-referential, and shares the source chain | pass |
| `trigger` `Monthly`, `onceScope` `PerGame`, `weight` 1, `cooldownMonths` 0 on all four new events | pass |
| `scenario` is `sc013` on all four new events; SC-011 rows stay scenario-neutral | pass (matches the only accepted gate, `EventCatalogData.cs:140`) |
| Relationship targets resolve to a known NPC and deltas are in `[-100,100]` | pass (`CharacterDefinitions.cs:73`, `CharacterRelationships.cs:27-28`) |
| All text titles/bodies non-empty | pass |
| Text catalog versions match (`2` / `2`); catalog 3→9 events, 1→3 chains, text 18→50 rows, of which 32 text rows are `sc013`-namespaced (10 SC-013 + 22 SC-014) | pass |

New content locations (for review traceability):

- `chain.sc013.refugee-legacy`: `event-catalog-v1.json:19-27` (`chainId` at `:20`).
- `event.sc013.refugee-arrival`: `:256-310`; `…refugee-loyalty`: `:311-357`;
  `…refugee-aid`: `:358-399`; `…refugee-rival`: `:400-446`.
- Chinese text: `event-text-zh-v1.json:144-253` (22 entries × 5 lines), first entry `:145`.

## 3. Generation gates, branch exclusivity, termination

Exact conditions in the shipped catalog:

| Node | Gate (offer-time) | Chain/branch behaviour |
|---|---|---|
| `refugee-arrival` (`:261`) | `SuccessionCountEquals 0` | First generation only; once the first handoff happens the raw count is 1, so the entry can never start late. Matches design “M2 首次交接后入口关闭” and `DECISIONS.md:8`. |
| `refugee-loyalty` (`:317-321`) | `EventOptionChosen(event.sc013.refugee-arrival / option…shelter)` + `SettledSuccessionCountAtLeast 1` | Reachable only after the committed shelter choice and one settled handoff. |
| `refugee-aid` (`:364-368`) | `EventOptionChosen(…loyalty / option…honor-old-oath)` + `SettledSuccessionCountAtLeast 2` | Ally branch; second handoff required. |
| `refugee-rival` (`:406-410`) | `EventOptionChosen(…loyalty / option…enforce-new-policy)` + `SettledSuccessionCountAtLeast 2` | Rival branch; second handoff required. |

Branch/termination trace (computed, not assumed):

- `shelter` → only `refugee-loyalty`; `refuse` and `betray` → **no downstream consumer**, so both terminate
  the chain exactly as the design states. Their summaries record the −5 / −10 result.
- `honor-old-oath` → only `refugee-aid`; `enforce-new-policy` → only `refugee-rival`.
- `aid`/`rival` options are all terminal (no further `EventOptionChosen` references them).
- Mutual exclusion is guaranteed structurally: `aid` and `rival` require **different** last committed
  options of the same source event, so at most one of the two is ever eligible; the engine also schedules
  at most one event per month (`EventEngine.cs:143-165`), and both carry the same priority `500`.

Retirement-gate interaction: `M2FixedScenario.CanBeginRetirement` (`M2FixedScenario.cs:185-266`) requires the
pledge plus the rainy-arrival event for generation 1 (`:209-221`), one SC-011 legacy variant for generation 2
(`:223-232`), and the finale for generation 3 (`:233-241`). It does **not** require any SC-014 node, so the
chain is optional and a player who retires at the first legal moment skips it. That limitation is disclosed in
`PROJECT_STATE.md:65`, `DECISIONS.md:9`, and `SC014_DEMO.md:33-34`, and was verified as true.

Static reachability of the generation-3 nodes after the finale: `GameSession.TryAdvanceToNextMonth`
(`GameSession.cs:1094-1121`) only blocks on family extinction, a pending event, succession pause, or an
unready engine; the ending is a display-only panel reading `GameSession.M2Ending`
(`M2EndingPanel.cs:70-90`). So nothing in source prevents the extra month the demo asks for.
**This is a source-reading inference, not runtime evidence.**

## 4. Relationship meaning

- Every one of the seven relationship deltas targets `npc.kuroda.iori` (`CharacterDefinitions.cs:73`):
  arrival +5/−5/−10 (`:275`, `:288`, `:301`), loyalty ±5 (`:335`, `:348`), rival ±5/−5 (`:424`, `:437`).
- `refugee-aid.request-aid` uses `RelationshipAtLeast 5` with a disabled reason and no effect
  (`:376-383`); `stand-alone` is `Always` with no effect (`:388-392`), so the aid event is always resolvable.
- All 22 new text rows phrase the score as “伊织家系与明智家的信任” (e.g. `event-text-zh-v1.json:157`,
  `:192`, `:202`, `:242`), never as a new relationship with the youth. This matches the design’s
  “不声明为少年建立未建模的独立人物关系”.
- The youth is kept distinct from the adult Iori: arrival text calls him “他家系的远亲”
  (`:147`), which matches GDD §7.4’s framing of the youth as a separate person (`GDD_v4.0.md:492-500`).

## 5. Preservation of existing content

### SC-011 — independently verified against `HEAD`

Parsed both revisions and compared deep JSON equality:

- `event.rainy-arrival`, `event.rainy-arrival.legacy-honored`, `event.rainy-arrival.legacy-deferred`:
  **JSON-equal** to `HEAD` (all fields, including priorities `10`/`100`/`100`, conditions, options, effects).
- `chain.rainy-arrival`: **JSON-equal** to `HEAD`.
- All **18** `HEAD` text entries: **unchanged**, none missing.
- No deletions in either resource file vs `HEAD` (`--numstat` = `286 0` and `160 0`).

### SC-013 — not independently verifiable against `HEAD`; supported by durable report evidence

`HEAD` contains only the SC-011 slice (3 events, 1 chain, 18 text rows); the SC-013 work is uncommitted, so a
`HEAD` comparison of SC-013 rows is impossible. This review does **not** claim one. Instead:

- `docs/reports/sc013-review-final.md:104-112` records the accepted SC-013 baseline: events 3→5,
  chains 1→2, text 18→28, both SC-013 events carrying `scenario: "sc013"`, and
  `git diff --numstat HEAD -- Assets/Resources` = `86/0` (catalog) and `50/0` (text).
- Current worktree: 9 events, 3 chains, 50 text rows, and `286/0` + `160/0`.
- The delta is therefore **200 catalog lines and 110 text lines**. Those are exactly the SC-014 additions:
  1 chain entry (8 lines, `:19-27`) + 4 events (192 lines, `:256-446`), and 22 five-line text entries
  (110 lines, `:144-253`). No other line was added or removed vs `HEAD`.
- The current SC-013 rows still match the accepted SC-013 documentation: pledge (`SuccessionCountEquals 0`,
  priority `1000`, keep/break ±5), finale (`SettledSuccessionCountAtLeast 2`, priority `1000`,
  `seek-aid` requiring relationship ≥5 with reason “需要黑田伊织的信任至少为 5”, stand-alone `Always`), and
  the ten `event.sc013.*`/`option.sc013.*`/`summary.sc013.*` text rows with the titles expected by
  `SC013_DEMO.md:61-114`.

Limitation: the exact-line-count agreement and the behavioural match cannot exclude a **same-line-count edit**
inside the uncommitted SC-013 block. Only an authoritative SC-013 snapshot (commit, patch, or archived file)
would settle byte-level preservation.

## 6. Documentation claims checked

| Claim | Result |
|---|---|
| `ARCHITECTURE.md:34` — new priorities `5`/`50`/`500` sit below the same-stage SC-013 and SC-011 rows | True: existing priorities are pledge/finale `1000`, SC-011 legacy `100`, rainy-arrival `10` |
| `PROJECT_STATE.md:65`, `DECISIONS.md:9` — existing deterministic monthly order is not preempted | True for the existing rows: each new event is eligible only in a month where no higher-priority row is offered; the SC-013 demo’s months still resolve to pledge → rainy-arrival → legacy → finale |
| `SC014_DEMO.md:33` — following the original SC-013 script skips the chain | True: `SC013_DEMO.md:61-94` retires after two generation-1 months and after one generation-2 month, so `refugee-arrival` (priority 5, third month) and `refugee-loyalty` (priority 50, second generation-2 month) never appear |
| `SC014_DEMO.md:44-99` — demo step → expected node mapping (pledge, rainy, arrival, legacy, loyalty, finale, aid/rival) | Matches the priority/gate table exactly |
| `SC014_DEMO.md:105-114` — result summary (+5/−5/−10, ≥5 gate, mutex) | Matches the catalog |
| `SC014_DEMO.md:12-14`, `DECISIONS.md:10`, `PROJECT_STATE.md:66` — no Unity/EditMode/Play Mode/save-load evidence claimed | True; the documents consistently label these as unrun |
| `SC014_DEMO.md:10` — modified-file list | Matches the six files; the design/plan documents created for this feature are not listed (they are not part of the implementation worker’s ownership) |
| `contentVersion` stays `2` | True |

## 7. Non-blocking findings

1. **Terminology inconsistency (player-facing).** The aid disabled reason is
   “需要黑田家系与明智家的信任至少为 5。” (`event-catalog-v1.json:382`), while the rest of the new chain
   names the family “伊织家系” (e.g. `event-text-zh-v1.json:157`, `:192`, `:242`) and the design does the
   same. Worth unifying, since the requirement text is the only place the family is called “黑田家系”.
2. **Incomplete rule list in `ARCHITECTURE.md:33`.** It says all nodes are expressed with
   `EventOptionChosen`, `SettledSuccessionCountAtLeast`, `RelationshipAtLeast`, `RelationshipDelta`, but the
   entry node is also gated by `SuccessionCountEquals` (`event-catalog-v1.json:261`). Wording, not behavior.
3. **`PROJECT_STATE.md:10`** still scopes the work as “SC-001–SC-013 首批任务” although SC-014 now exists.
4. **Season-specific flavour without a month gate.** Arrival text says “入夏的傍晚”
   (`event-text-zh-v1.json:147`), but the event can be offered in any month if the player delays, so it can
   read as winter prose. Cosmetic.
5. **Spec/content wording ambiguity.** The design says the youth becomes a retainer “经过两次交接”
   (design `:15`), while the generation-2 text already calls him “如今已是伊织家系的家臣”
   (`event-text-zh-v1.json:182`) after one handoff. The implementation follows GDD §7.4’s second-generation
   wording; the design sentence is the ambiguous one. No data change required, but the design record should be
   corrected when next edited.
6. **Optional-chain product risk (already disclosed).** SC-014 is never mandatory: the first-generation
   entry disappears at the first handoff, and the third-generation nodes only appear if the player keeps
   ending months after the scenario finale. `PROJECT_STATE.md:65`, `DECISIONS.md:9`, and `SC014_DEMO.md:33-34`
   all state this, so it is a disclosed design consequence, not a hidden defect.

## 8. Limitations

- No Unity Editor was started, no compilation, no EditMode tests, no new or existing tests were run,
  no Console check, no `FoundationSceneValidator`, no Play Mode, no save/load, no screenshots, no Computer Use.
- Runtime ordering, option-availability rendering, the disabled reason string in the UI, chronicle
  de-duplication, save/load of a pending SC-014 event, and the “extra month after the finale” sequence are
  **unverified**. They are inferred from source reading only.
- The catalog was validated against the rules read in `EventCatalogValidator.cs`, reproduced with an
  independent JSON script; `EventCatalogValidator` itself was not executed under Unity.
- SC-013 byte-level preservation is **not** independently verifiable from Git because SC-013 is uncommitted;
  see §5 for the durable evidence actually used.
- Play Test and user acceptance remain the user’s to perform; this report does not accept SC-014.

## 9. Reproduction commands

```sh
git diff --numstat HEAD -- Assets/Resources/Events/event-catalog-v1.json \
  Assets/Resources/Localization/event-text-zh-v1.json
git show HEAD:Assets/Resources/Events/event-catalog-v1.json > /tmp/head-catalog.json
git show HEAD:Assets/Resources/Localization/event-text-zh-v1.json > /tmp/head-text.json
python - <<'PY'
import json, subprocess
def load(p): return json.load(open(p, encoding='utf-8-sig'))
cat, txt = load('Assets/Resources/Events/event-catalog-v1.json'), load('Assets/Resources/Localization/event-text-zh-v1.json')
head = json.loads(subprocess.check_output(['git','show','HEAD:Assets/Resources/Events/event-catalog-v1.json']).decode('utf-8-sig'))
head_txt = json.loads(subprocess.check_output(['git','show','HEAD:Assets/Resources/Localization/event-text-zh-v1.json']).decode('utf-8-sig'))
ev = {e['eventId']: e for e in cat['events']}
for e in head['events']: print(e['eventId'], ev.get(e['eventId']) == e)
ht = {t['textId']: t for t in txt['entries']}
print('changed HEAD texts:', [k for k,v in ((t['textId'],t) for t in head_txt['entries']) if ht.get(k) != v])
PY
grep -rn "refugee" Assets --include=*.cs
```
