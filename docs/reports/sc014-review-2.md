# SC-014 流亡少年与家系遗产 — Follow-Up Independent Read-Only Review

Date: 2026-11-03
Reviewer: independent read-only OpenCode2 follow-up review worker
Branch: `feature/sc013-fixed-three-generation`
Worktree: `C:\Users\panye\My project` (HEAD `88fe7c6`, worktree dirty)
Inputs: `docs/reports/sc014-review.md` (§7 findings), the two SC-014 JSON resources,
`PROJECT_STATE.md`, `ARCHITECTURE.md`, `SC014_DEMO.md`, and the SC-014 design/plan.

## Verdict

**ACCEPT** for the corrected SC-014 data/documentation slice.

All **six** non-blocking findings recorded in `docs/reports/sc014-review.md` §7 are resolved in the current
worktree, with **zero blocking defects** and **zero new structural or reference defects** introduced by the
corrections. SC-011 remains JSON-equal to `HEAD`.

This is again a **static, data-and-document review only**. It is **not** Unity Editor compilation, EditMode
execution, Console inspection, `FoundationSceneValidator`, Play Mode, save/load verification, or user
acceptance. No runtime behavior is claimed. Only this report was written; no implementation file and no prior
report was edited.

## 1. Resolution of the six findings

| # | Finding (review 1 §7) | Current evidence | Status |
|---|---|---|---|
| 1 | Aid disabled reason called the family “黑田家系”, unlike the rest of the chain | `Assets/Resources/Events/event-catalog-v1.json:382` now reads `"reason": "需要伊织家系与明智家的信任至少为 5。"`. A repo-wide `grep -rn "黑田家系"` over `Assets`, `PROJECT_STATE.md`, `ARCHITECTURE.md`, `DECISIONS.md`, `SC014_DEMO.md`, `docs/superpowers` returns **no hits**. The same phrase “伊织家系与明智家的信任” is used by the summaries at `event-text-zh-v1.json:157`, `:192`, `:242`. | Resolved |
| 2 | `ARCHITECTURE.md` rule list omitted the entry gate `SuccessionCountEquals` | `ARCHITECTURE.md:33` now enumerates `EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`、`RelationshipAtLeast` 与 `RelationshipDelta`. Matches the catalog: only those five existing rule types are exercised by the four new nodes (plus the baseline `Always` option requirement). | Resolved |
| 3 | `PROJECT_STATE.md:10` scoped the work to “SC-001–SC-013” | `PROJECT_STATE.md:10` now reads “SC-001–SC-014 首批任务”; no `SC-001–SC-013` string remains in the document. | Resolved |
| 4 | Arrival prose was season-specific (“入夏的傍晚”) although the entry can appear in any month | `event-text-zh-v1.json:147` now reads “黑田伊织带着一名受伤的少年站在清洲门外。少年是他家系的远亲，因战乱流亡到此。…” — no season or month reference. A scan of all 22 new text rows for `春/夏/秋/冬/寒/暑/雪/梅雨/入夏/季节` returns nothing; the only time-of-day words are “这一夜” (refuse option, `:162`) and “天亮后” (refuse summary, `:167`), which are scene timing rather than a season. | Resolved |
| 5 | Design said the youth becomes a retainer “经过两次交接”, conflicting with the one-handoff generation-2 text | Design `docs/superpowers/specs/2026-11-03-sc-014-refugee-legacy-design.md:15` now says “少年在**一次**交接后的第二代便已是伊织家系的家臣”. This matches `event-text-zh-v1.json:182` (“正綱继位后…如今已是伊织家系的家臣”) and the gate `SettledSuccessionCountAtLeast 1` (`event-catalog-v1.json:317-321`). “两次交接” now appears only in the legitimate generation-3 rows (design `:28-29`). | Resolved |
| 6 | `SC014_DEMO.md` modified-file list omitted the SC-014 design/plan artifacts | `SC014_DEMO.md:10` lists the six implementation files; `:11` lists design, plan and the first review report as non-implementation records. A repo-wide `grep -rl "SC-014\|refugee"` (excluding `Library`/`Temp`/`graphify-out`) returns exactly those nine files, so the list was complete for the artifacts that existed when the corrections were made (this follow-up report postdates it; see §5). | Resolved |

No correction altered the behaviour described in the first review: the same four events, gates, options,
relationship deltas, and priorities are still present (re-verified in §2).

## 2. Structural and reference re-check

Both resources were independently parsed with Python 3.14 (`json.load`, `utf-8-sig`) and compared with
`git show HEAD:<file>`.

| Check | Result |
|---|---|
| Event IDs unique (9), chain IDs unique (3), option pairs unique (21), text IDs unique (50) | pass, 0 duplicates |
| Every event/option/summary text reference resolves; no unused text entry | 0 missing, 0 unused |
| Chain membership bidirectional for all three chains, including `chain.sc013.refugee-legacy` (`linkedEventIds` = arrival/loyalty/aid/rival) | pass |
| Each `EventOptionChosen` source event/option exists and is not self-referential; at most one per event | 5/5 resolve, max 1 per event |
| New-node gates | `refugee-arrival`: `SuccessionCountEquals 0`, priority `5`; `refugee-loyalty`: chosen `…arrival.shelter` + `SettledSuccessionCountAtLeast 1`, priority `50`; `refugee-aid`: chosen `…loyalty.honor-old-oath` + `SettledSuccessionCountAtLeast 2`, priority `500`; `refugee-rival`: chosen `…loyalty.enforce-new-policy` + `SettledSuccessionCountAtLeast 2`, priority `500` — all as documented |
| Branch/termination | `refuse` and `betray` have no downstream consumer; `honor-old-oath` → aid only; `enforce-new-policy` → rival only; aid/rival options are terminal; aid and rival require different last choices of the same source event, so they stay mutually exclusive |
| Scenario gating | all four new events `scenario: "sc013"`; the three SC-011 rows remain scenario-neutral |
| Rules/effects inventory | condition types only `SuccessionCountEquals`/`EventOptionChosen`/`SettledSuccessionCountAtLeast`; requirements only `Always`/`RelationshipAtLeast`; effects only `RelationshipDelta`; the 14 deltas across the catalog target `npc.kuroda.iori`, range `-10…+5`; aid `request-aid` requires `RelationshipAtLeast 5` with the corrected reason, `stand-alone` is `Always` with no effect |
| Text completeness | no empty title/body in any of the 50 rows |
| `contentVersion` | `2` in both files, unchanged from `HEAD` |
| Line deltas vs `HEAD` | catalog `286/0`, text `160/0`; the SC-014 blocks remain catalog `:19-27` (chain) and `:256-446` (four events), text `:144-253` (22 entries) |

The corrections were inline string replacements, so the line totals are unchanged from the first review. That
is sufficient for structure and semantics but, as before, cannot prove byte equality against any earlier
uncommitted snapshot.

## 3. Preservation of existing content

### SC-011 — independently verified against `HEAD`

- `event.rainy-arrival`, `event.rainy-arrival.legacy-honored`, `event.rainy-arrival.legacy-deferred` and
  `chain.rainy-arrival` are **JSON-equal** to `HEAD` (all fields, conditions, options and effects).
- All **18** `HEAD` text entries are unchanged; none missing.
- No deletions in either resource file (`--numstat` = `286 0` and `160 0`).

### SC-013 — still not independently verifiable against `HEAD`

`HEAD` (`88fe7c6`) contains only the SC-011 slice (3 events, 1 chain, 18 text rows). SC-013 remains
uncommitted, so its rows are **not byte-comparable to `HEAD`**; this review does not claim otherwise. Current
SC-013 rows still match the accepted baseline documented in `docs/reports/sc013-review-final.md:104-112`
(pledge `SuccessionCountEquals 0` / priority `1000`; finale `SettledSuccessionCountAtLeast 2` /
priority `1000`; ten `event.sc013.*`/`option.sc013.*`/`summary.sc013.*` texts). Limitation unchanged:
same-line-count edits inside the uncommitted SC-013 block cannot be excluded without an authoritative
snapshot.

## 4. Branch and milestone gates

- `HEAD` = `88fe7c6` on `feature/sc013-fixed-three-generation`.
- `main` and `origin/main` are also `88fe7c6`; **no tag points at `HEAD`** (`git tag --points-at HEAD` empty).
- Therefore no merge, tag, or push was performed during the corrections, consistent with
  `SC014_DEMO.md:14` and the SC-014 decision record.

## 5. Residual non-blocking observations

None of these require a change before acceptance:

1. The plan (`docs/superpowers/plans/2026-11-03-sc-014-refugee-legacy.md:27`) still lists only
   `EventOptionChosen`/`SettledSuccessionCountAtLeast`/`RelationshipAtLeast`/`RelationshipDelta`. It is the
   historical planning artifact, not the architecture description corrected for finding 2; the shipped
   catalog and `ARCHITECTURE.md:33` are consistent.
2. `SC014_DEMO.md:10-11` cannot list this second review because it postdates the file; add it when the demo
   record is next touched. The current list is complete for the artifacts that existed at correction time.
3. `event-catalog-v1.json:238` (SC-013 finale) still phrases its requirement as “黑田伊织的信任” — a
   person-level gate by SC-013 design, deliberately different from SC-014’s family-level
   “伊织家系与明智家的信任”. No change requested.
4. `PROJECT_STATE.md` has an SC-014 section (`:60-66`) but no SC-014 row in the checkpoint table; consistent
   with SC-014 being implemented but unaccepted, and with the SC-013 row’s own “待验证” wording.

## 6. Limitations

- No Unity Editor was started, no compilation, no EditMode tests, no new or existing tests were run, no
  Console check, no `FoundationSceneValidator`, no Play Mode, no save/load, no screenshots, no Computer Use,
  no Play Test.
- Runtime ordering, option-availability rendering, the corrected disabled-reason string in the UI,
  chronicle de-duplication, save/load of a pending SC-014 event, and the “extra month after the finale”
  sequence remain **unverified**; they are inferred from data/document reading only.
- Play Test and user acceptance remain the user’s to perform; this report does not accept SC-014 and does not
  authorize a checkpoint, merge, tag, or push.

## 7. Reproduction commands

```sh
git status --short && git log --oneline -3 && git branch -vv && git tag --points-at HEAD
git diff --numstat HEAD -- Assets/Resources/Events/event-catalog-v1.json \
  Assets/Resources/Localization/event-text-zh-v1.json
grep -rn "黑田家系" Assets PROJECT_STATE.md ARCHITECTURE.md DECISIONS.md SC014_DEMO.md docs/superpowers
grep -rln "SC-014\|refugee" --include=*.md --include=*.json --include=*.cs . \
  | grep -v "^./Library\|^./Temp\|^./graphify-out"
PYTHONIOENCODING=utf-8 python - <<'PY'
import json, subprocess
def load(p): return json.load(open(p, encoding='utf-8-sig'))
cat = load('Assets/Resources/Events/event-catalog-v1.json')
txt = load('Assets/Resources/Localization/event-text-zh-v1.json')
head = json.loads(subprocess.check_output(['git','show','HEAD:Assets/Resources/Events/event-catalog-v1.json']).decode('utf-8-sig'))
head_txt = json.loads(subprocess.check_output(['git','show','HEAD:Assets/Resources/Localization/event-text-zh-v1.json']).decode('utf-8-sig'))
ev = {e['eventId']: e for e in cat['events']}
ch = {c['chainId']: c for c in cat['chains']}
print('SC-011 events equal:', [(e['eventId'], ev.get(e['eventId']) == e) for e in head['events']])
print('SC-011 chain equal:', [(c['chainId'], ch.get(c['chainId']) == c) for c in head['chains']])
ht = {t['textId']: t for t in txt['entries']}
print('HEAD texts changed/missing:', [t['textId'] for t in head_txt['entries'] if ht.get(t['textId']) != t])
for cid, c in ch.items():
    members = {e['eventId'] for e in cat['events'] if e.get('chainId') == cid}
    print(cid, 'bidirectional:', set(c['linkedEventIds']) == members)
ids = {t['textId'] for t in txt['entries']}
need = [e['textId'] for e in cat['events']] + [t for e in cat['events'] for o in e['options'] for t in (o['textId'], o['summaryTextId'])]
print('missing text refs:', [n for n in need if n not in ids], 'unused:', [i for i in ids if i not in set(need)])
PY
```
