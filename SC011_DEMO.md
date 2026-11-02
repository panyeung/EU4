# SC-011 Demo — Cross-Generation Rainy-Night Guest

## What this slice proves

The first generation chooses how to receive the rainy-night guest. After one real SC-010
head change, the successor receives one follow-up encounter whose visible text and options
depend on that saved choice. The root choice, the pending follow-up, the once-only markers,
the relationship value, and the succession state all survive save/load.

All names, IDs and text are original to this project. The follow-up is one encounter with
two mutually exclusive variants, not two independent events.

## Evidence boundary

- User acceptance recorded 2026-11-02: the user reports compilation passed, EditMode passed,
  no Console errors, and Play Test passed. EditMode totals and Console warning count were
  not supplied. This record reflects the user's report; no Editor logs were captured here.
- The `FoundationSceneValidator` result was not reported and is not claimed as passed.
- Focused EditMode coverage includes root-pending and post-root-choice save/load, a second
  succession before the follow-up is scheduled, a regency checkpoint, scheduling-failure
  no-mutation behavior, and v1/v2 content-version migration. The user reports EditMode passed,
  but exact per-suite totals were not supplied.
- No scene, prefab, `ProjectSettings`, package, or asset-GUID change is expected for SC-011.
  Do not run a scene updater unless the lead identifies a scene regression.

## Play Test A — Honored (sheltered) branch, then save/load

1. Enter Play Mode in `FoundationScene`. At the title overlay click `新游戏`.
   Expected: date `1560-04-01`; head `明智 晴信`; no pending event.
2. Click `结束月份`.
   Expected: `雨夜来客` appears with `请他进屋歇息` and `请他明晨再来` (the latter is
   disabled at the default relationship value 0). The root text is unchanged from the
   accepted demo.
3. Choose `请他进屋歇息`.
   Expected: the existing +5 trust summary appears; the event panel closes; the root event
   is recorded as chosen once.
4. Click `退休并安排继承`, choose `明智 正綱`, then `确认交接`.
   Expected: the head becomes `明智 正綱`; the handoff record starts with
   `succession.1.person.akechi.masatsuna`; date, location, mission stages, merit and other
   relationship values are unchanged. Save with `存档`.
5. Click `结束月份`.
   Expected: `旧客的雨夜回访` appears with `重申先代的门诺`, `按当下利害重议`, and
   `止于这一夜`. The body mentions the old guest and the earlier hot tea. Variant B's
   `门房里的旧雨` and its options must not appear.
6. Do not choose yet. Click `读档` to restore the step-4 save, then click `结束月份`.
   Expected: the same `旧客的雨夜回访` with the same three options. This proves save/load at
   the post-handoff stable point. (The game has one save slot, so do this before saving
   again.)
7. Choose `重申先代的门诺`, then click `存档`.
   Expected: feedback confirms the old pledge and the +5 trust summary. Click `结束月份`
   again. Expected: no `雨夜来客` repeat and no second legacy encounter. Click `读档` and
   `结束月份` once more. Expected: the resolved follow-up does not reappear.
8. Record each step's pass/fail and the visible text in the test record. Do not use
   screenshots.

## Play Test B — Deferred branch

1. Start a new game. Before the first `结束月份`, travel to Atsuta and use 黒田伊织's town
   dialogue to raise his trust to at least 1 (accept his proposition), then return to the
   map. This is required because `请他明晨再来` keeps its accepted
   `RelationshipAtLeast 1` requirement.
2. Click `结束月份`, choose `请他明晨再来`, then retire to `明智 正綱` and confirm.
3. Click `结束月份`.
   Expected: `门房里的旧雨` appears with `补上先代欠下的礼数`, `说明当日不得已`, and
   `旧事不必再提`. Variant A's `旧客的雨夜回访` text and options must not appear.
4. Choose `补上先代欠下的礼数`.
   Expected: the +5 trust summary appears. Save, load, and advance one more month.
   Expected: the deferred encounter does not repeat.
5. Record pass/fail and the visible text for each step. Do not use screenshots.

## Out of scope for this Play Test

- The regency path (minor heir and explicit adult handoff) is covered by authored EditMode
  tests in the implementation plan (not executed), not by this user script. It requires
  many month turns and is not part of the smallest slice.
- A second or third generation is out of scope. SC-011 proves one successor facing one
  predecessor decision; GDD 19.4's full three-generation M2 script remains future work.
- Runtime text templating, faction politics, economy costs, chronicles, a new NPC, and a
  new scene are not part of this slice.

## What must not be claimed

- Do not invent EditMode totals, Console warning counts, or a scene-validator result.
- Keep compilation, EditMode, Console, scene validation, Play Mode, and user acceptance as
  separate evidence; static review is not runtime evidence.
- User acceptance is recorded. Future changes still require their own verification and
  acceptance before being treated as an accepted demo.
