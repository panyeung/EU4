# M7 Merit & Promotion — Candidate

**Status: Candidate; Play Mode acceptance pending.**
**Branch:** `feature/m5-save-slot` (user directed continuing on the same branch and worktree)
**Design:** `docs/superpowers/specs/2026-11-01-m7-merit-promotion-design.md`
**Plan:** `docs/superpowers/plans/2026-11-01-m7-merit-promotion.md`

## Scope

- Pure-C# promotion rules: four game-abstracted ranks from total merit — 足轻(0) / 番头(5) / 组头(10) / 武士头(20). These are simplified growth labels, not historical office claims.
- A permanent rank line on the map screen: current merit, current rank, and the next rank's threshold (「最高」 at the top rank).
- The M6 demo-completion summary now names the final rank.
- No new merit sources, no mission reward changes, **no save-format change** (rank is derived from stages via merit and never stored), no new package.

## Implementation

- `Domain/PromotionRules.cs`: `PromotionRank` enum + ascending immutable threshold table; `RankFor` (negatives clamp to 0), `ChineseName`, `MinimumMerit`, `TryNextRank` (false at top), `TierCount`.
- `GameSession`: derived `Rank` and `RankName` properties from `Merit`.
- `Presentation/RankPanel.cs`: same idempotent `Refresh()` + `Update()` poll as `DemoSummaryPanel`; `Bind(session, text)`; no-session guard leaves the text untouched.
- `DemoSummaryPanel`: summary format extended to 「Phase 1 演示完成。总功绩：{0} · {1}。」 with the rank name.
- `Editor/AddM7RankPanel.cs`: transactional updater creating `Canvas/M2WorldMapRoot/M7RankText` at (-450,100) size 240×40 font 11, inserted before `TownMenuRoot`, attaching `RankPanel` to the GameSession object and binding it; validates uniqueness, parent/sibling order, containment, and separation from all map controls plus the M5/M6 texts; one Undo group, validate-before-save, rollback on failure. Menu `Tools/Sengoku Chronicle/Add M7 Rank Panel`; batch entry `RunBatchIntegration()`.
- `FoundationSceneValidator`: reports `FOUND RankPanel`, its bindings, and runs `AddM7RankPanel.Validate`.

## Verification evidence (observed)

- **Compilation:** Unity 6000.6.6f1 batchmode; `m7-batchmode-1.log` and `m7-batchmode-final.log` contain no `error CS` lines.
- **EditMode (pre-integration):** `m7-editmode-1.xml` reports `total="425" passed="425" failed="0" inconclusive="0" skipped="0"` (start 2026-11-01 22:19:48Z) — the 401 M1–M6 tests plus 24 new M7 tests (PromotionRules boundary/name/chain tests, RankPanel display tests, updater structure/rollback/refusal tests, and the extended DemoSummary rank assertion).
- **Scene integration:** `RunBatchIntegration` opened FoundationScene, ran the transactional updater, and saved (`m7-integration.log`: `M7 rank integration completed and FoundationScene was saved.`); no rollback warnings. The scene diff is +96/−1; the single changed line is Unity re-normalizing the dormant `m_MaxSize` (0..40→44) on `M6TitleLabel`, whose `m_BestFit: 0` — an inert serialization artifact, not a code edit.
- **Scene validation:** fresh `Logs/SceneValidationReport.txt` reports `FOUND RankPanel: GameSession`, `BOUND GameSession.rankText -> M7RankText (Text)`, `PASS M7 rank panel hierarchy, bindings, containment, and map separation.`, and `RESULT: PASS; Errors: 0`.
- **EditMode (post-integration, final):** `m7-final-editmode.xml` reports `total="425" passed="425" failed="0" inconclusive="0" skipped="0"` (start 2026-11-01 22:36:35Z).
- **Console:** not captured. Batchmode logs are not the Editor Console, so no Console counters are claimed.
- **Play Mode:** not run yet; see the acceptance path below.

## Required Play Mode acceptance path

1. Open this worktree in Unity 6000.6.6f1, load `Assets/Scenes/FoundationScene.unity`, clear the Console.
2. Play → title → 新游戏. Confirm the map's top-left rank line reads 「功名：0 · 足轻（下一阶 番头 5）」.
3. Complete the sealed-letter mission (+5 merit) → line shows 番头 (next 组头 10); complete the market tally (+5) → 组头; complete the supply shortage (+10) → 武士头（最高）and the demo summary line reads 「Phase 1 演示完成。总功绩：20 · 武士头。」.
4. 存档 → exit and re-enter Play → 继续 → the rank line and summary show the same rank; retry a completed mission's final action → merit and rank stay unchanged.
5. Report the observed values and Console counters.

User acceptance is required before the final M7 checkpoint, merge to `main`, or a `demo-M7` tag.
