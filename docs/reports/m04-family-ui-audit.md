# M04 family UI audit (Tasks 1–2)

Date: 2026-11-16. Child worktree branch `panyeung/ui-m04-family`, base `d57ea7c`.
Scope: `docs/UI_ROADMAP.md` Tasks 1–2 against the committed M04 presentation code in
`Assets/Scripts/Presentation`. Presentation-only: no scene, prefab, `ProjectSettings`,
`Library`, `Temp`, GDD or save-schema file was changed, and no Unity Editor run is
claimed here.

## What the committed code already covered

- Generations are derived from structural parent/child and adoption facts, never from
  names, SC-013 IDs or a fixed member count (`FamilyTreeLayout.CalculateDepths`).
  Spouses and siblings share a generation, and the layout widens instead of shrinking.
- The graph scrolls both ways with a horizontal scrollbar, keeps a fixed 196×88 card,
  and separates connected kin, unconnected kin and retainer/disciple/guest bands.
- Only recorded facts are drawn: edges to removed endpoints and to household members are
  dropped, and a person with no recorded link appears in the unconnected band instead of
  being given a relationship.
- The person page separates current state (profile), facts (relations) and history
  (chronicle) into tabs, and its selected person survives refresh.
- SC-022 candidate filtering, one-shot confirmation, rejection messages and close-without
  -confirmation already behaved; SC-010 succession, regency and crisis overlays existed.

## Review findings fixed in this pass

1. The detail column is now a real scroll surface: raycastable viewport, measured content
   height and a visible vertical scrollbar with a gold handle, so long fact lists are
   reachable by wheel, drag or bar.
2. The narrow fallback derives its width from the styled panel instead of restoring the
   serialized 700 px scroll geometry.
3. The detail text width is applied before `preferredHeight` is measured, so wrapped lines
   are not clipped.
4. The formation preview keeps the whole confirmation consequence. It uses the panel's
   help band, measures the needed height and grows inside that band instead of truncating.
5. The preview verdict reports the blocked family state from `FamilyFormationView.CanEdit`
   and `BlockReason` before any per-pair rule text.
6. Cards keep a focusable `Button` (automatic navigation, tinted focus state), the panel
   focuses the selected card when it opens, and the selected card carries a textual
   `已选` badge plus a bold label.
7. A card hit surface with an 8 px drag threshold owns pointer clicks and forwards drags to
   the graph `ScrollRect`, so dragging never selects a person.
8. At narrow widths the detail view becomes a stacked sheet over the graph with its own
   close control, so selected-person facts are never silently hidden.
9. The two-card minimum width uses `FamilyTreeLayout`'s own margins, both cards and their
   gap (476 px). At the 760 px family card the split is 478 px graph + 208 px column.
10. Column height follows the live scroll rect, and every runtime object is marked
    `HideFlags.DontSave` so an editor-side refresh cannot serialize generated clones.
11. Directed parent/adoption facts are named by direction: `亲子（子女）` / `亲子（父母）`,
    `收养（养子/养女）` / `收养（养父母）`, and path steps read
    `晴信 —亲子·子女→ 正綱`.
12. Adoption copy no longer claims inheritance is unaffected: an adopted child is a lawful
    successor by `SuccessionService.IsSuccessorRole`, so the panel and the generator help
    line now say the adopted person becomes eligible while the current succession phase
    does not change.

## Evidence after the second pass (before these three corrections)

Separate claims, in the order they were produced. Nothing below is inferred from an
earlier claim.

1. **Host-side compile (static).** Staged Roslyn type-check with Unity's bundled compiler
   against the child worktree sources: Domain, Persistence, Events, Application,
   Presentation and Editor each 0 errors. Not Unity compilation.
2. **Pure-helper harness (static, logic only).** A console harness over
   `FamilyRosterDetailLayout` and `FamilyKinshipPath` passes 25/25 assertions (split at
   760/740/438/400, two-card minimum, direction names, determinism, cycle safety). It is a
   pure C# helper harness: it does not load UnityEngine, does not drive a panel, and is not
   EditMode, Console, Validator or Play Mode evidence. The earlier "17/17" figure was this
   same kind of helper-only harness in the first pass, not a Unity result.
3. **Unity compile + focused EditMode.** Batchmode Editor bound to the child worktree
   reimported and compiled the project with 0 `error CS`. Focused
   `FamilyRosterPanelTests` run: 25/25 passed, 0 failed, 0 skipped
   (`Logs/m04-focused.xml`, `Logs/m04-focused.log`).
4. **Unity full EditMode.** 1994/1994 passed, 0 failed, 0 skipped
   (`Logs/m04-full-editmode.xml`, `Logs/m04-full-editmode.log`), 0 `error CS`.
5. **FoundationScene validator (read-only, child worktree scene, 23:57).** Every M04 line
   passes: SC-012, SC-013 roster hierarchy and title entries, SC-019, SC-020, SC-021,
   SC-022, SC-023, SC-024. Overall `RESULT: FAIL; Errors: 2` comes from two pre-existing
   scene-content-vs-updater drift lines: `The SC-022 help line does not carry the current
   adoption copy; re-run the updater.` and `The gallery page-root registration is stale;
   re-run the M02 updater.` The run measured the pre-regeneration scene bytes (still the
   `d57ea7c` content); the 23:57 FAIL report itself hashes to `46ed7593…`. Re-running the
   updaters is integration work outside this task. The FAIL is superseded by the 02:35
   PASS in the household follow-up below: the FoundationScene was regenerated between the
   two reports, which repaired both drift lines. Report: `Logs/SceneValidationReport.txt`;
   the 23:57 copy and both ERROR lines are in `Logs/m04-followup2/`.
6. **Play Mode gate (child worktree Editor, port 7803, FoundationScene).** Wide card:
   `HasDetailColumn=true`, graph 478 px at x=-111, detail scrollbar present, viewport
   `raycastTarget=true`, head selected by default with heading `人物详情 · 明智 晴信`;
   clicking 千恵 moves the selection and shows direction-aware facts
   (`- 配偶：明智 晴信`) plus the read-only source line. Narrow card at 438 px: no split,
   graph keeps 378 px, selecting a card opens the sheet with kinship facts,
   `CloseDetailSheet()` returns to the graph, restoring 760 px restores the column.
   Formation: preview carries `路径`, `校验（SC-022）` and the full consequence with no
   ellipsis, and the adoption copy reads `被收养者按继承规则具备继承资格，当前继承阶段不变`.
   Console during the run: `groundTruth.consoleErrors=0`, `compilationFailed=false`; the two
   ring-buffer error entries are my own invalid `unity command list` probes, not game code.
   Scene `isDirty=false` after exiting Play Mode. The blocked-state preview branch
   (`CanEdit=false` + `BlockReason`) compiles but was not exercised at runtime.

The child Editor used for steps 3-6 was started only against
`C:\Users\panye\orca\workspaces\My project\ui-m04-family`; the user Editor on
`C:\Users\panye\My project` was never targeted. The pipeline `quit` command failed with a
`PipelineQuitScheduler`/`DontDestroyOnLoad` error, so the child Editor process was
terminated after confirming the scene was clean. Unity's import wrote
`.vscode/settings.json` and two `ProjectSettings` files into the child worktree; those are
editor-generated, outside this task's scope, and left untouched for the coordinator.

## Re-review corrections (third pass, code and docs only, no Unity run)

The independent re-review found three visible defects in the final bytes. All three are
corrected in source; none required a scene, prefab, `ProjectSettings` or `.vscode` change,
and no Unity Editor was started because M05/M08 child Editors were active.

1. **Detail body re-measured on width change.** `FamilyRosterPanel.ApplyPageLayout` now
   calls `MeasureDetailBody(false)` after it applies either layout branch, and
   `RefreshDetail` calls the same helper with `force: true` once the body is visible. The
   helper compares the body width against `_measuredDetailWidth` and only wraps and
   re-measures when that width changed, so a narrow-sheet to side-column switch no longer
   keeps a body measured at the old width. Layout events only, no per-frame work.
2. **Generator emits the corrected adoption copy.** The SC-022 generator owns one
   `HelpText` constant used by the create path, the repair path now rewrites the help line
   it finds in an existing hierarchy, and `Validate` rejects a help line that does not
   match. The committed scene still shows the stale line at
   `SC022FamilyFormationHelpText` (fileID 215197379) because a scene edit is outside this
   task; the integration worker's regeneration now repairs it, and the SC-022 validator
   line will report the stale copy until that run happens.
3. **No double parchment tint.** The card `Button` keeps `Image.color` as the single
   parchment source; `ColorBlock.normalColor` is white so the tint multiplies once, with a
   warm highlighted/selected multiplier and a slightly darker pressed one. Selection is
   still marked by the `已选` badge and bold label.

Also inspected: the blocked `CanEdit == false` preview path. It already reported the block
reason instead of an "allowed" verdict, but its consequence line still described the write
as if it would happen. It now states that the confirm button is disabled and that nothing
is written until the pending state clears, keeping the rule text intact.

Verification boundary for this pass: staged host-side Roslyn compile of Domain,
Persistence, Events, Application, Presentation and Editor with 0 errors, plus source
inspection. No Unity compile, EditMode run, validator run or Play Mode run was performed in
this pass; the prior pass's EditMode 1994/1994 and Play Mode evidence still describes the
code before these three corrections.

## Household SC-019 embed (follow-up, 2026-11-16)

The initial uniform-fit iteration was superseded by the final scroll-fit correction. The
final code scales by width only, and scrolls vertically only when both conditions hold: the
authored panel is taller than the viewport, and the card can still reserve the 40 px
month-summary strip plus its gap so a positive viewport remains below that strip. It never
shrinks the panel vertically to force the full 760×700 panel onto the 760×560 household
card.

- `HouseholdIndustryFit.CalculateReadableViewport` uses the available width, does not
  upscale, and reports readability at scale ≥ 0.8. At a 620 px band and 760 px authored
  width the scale is 0.8158. When the scaled content is taller than the viewport, the
  authored panel is placed intact in `ScrollRect` content masked by `RectMask2D`, so the
  extra height is reached by scrolling instead of shrinking the text.
- `ReferenceGalleryNavigator` reserves `HouseholdMonthMinimumHeight` (40 px) plus
  `HouseholdMonthGap` above the panel and derives the viewport from the rectangle that
  leaves free. It builds the scroll viewport only on that tall-panel branch, restores the
  authored panel to its home parent before reflow/hide, and clips panel content to the
  viewport. A raycastable transparent viewport receives wheel and drag input; other
  decorative composition surfaces remain non-raycast.
- If the card is too short to reserve that strip (`halfWidth <= 0`, no room below the card
  top via `cardTopHalf <= 0`, or `viewportHeight <= 0`), the code does not scroll. It falls
  back to the older `HouseholdIndustryFit.Calculate` uniform width+height fit centred on the
  whole card, and the month summary is omitted rather than clipped over the panel. The
  focused case
  `ShowPage_Household_TallPanelUsesReadableScrollViewportAndReservesMonthStrip(800, 200, …)`
  pins that branch: uniform fit (panel height 184 px), no month block, and
  `HouseholdIndustryBelowReadableScale=true`.
- The final month summary is placed on the household card in the free strip above the panel.
  Its layout is computed from the card and fitted panel rect so the summary cannot overlap
  the visible panel. If the card cannot hold the 40 px minimum summary strip, the summary is
  omitted rather than clipped over the panel.

Verification after the scroll-content edge correction: EditMode focused household cases
12/12, `HouseholdIndustryFitTests` 6/6, full EditMode 2013/2013 (all 0 failed, skipped,
inconclusive); compile completed with `failed=false`, `compilationFailed=false`, and no
`error CS`. The validator returned `RESULT: PASS; Errors: 0` at 02:35, with 25 FOUND, 298
BOUND, 0 MISSING and 0 UNBOUND references. That PASS ran against the regenerated scene
(current SHA-256 `d670dcfd…`, md5 `64e3dea6…`); the 23:57 FAIL report (SHA-256
`46ed7593…`) described the pre-regeneration FoundationScene, which was regenerated at 01:58
between the two reports and repaired both the SC-022 help-copy and gallery page-root drift
lines. The Play Mode run covered the shipped 760×560 panel at 1280×720 and 1280×600, plus
an in-memory 760×700 panel at both sizes. Readable scale was 0.8158; the tall panel had a
620×492 logical viewport, 79.05 px scroll range, exact content edges, and no overlap between
the visible panel and month summary at either scroll extreme.

Console evidence, in the order the stages ran:

1. Scroll-fit Play Mode run: the snapshot is 6 errors / 154 warnings / 141 logs at cursor
   301, and the run left those counts unchanged.
2. Test-only EditMode snapshot, taken after that Play Mode run: 8 errors / 201 warnings /
   174 logs at cursor 383; the two added test errors are the expected negative-test output
   noted above.
3. Validator: added one log only, moving the buffer to 8 errors / 201 warnings / 175 logs at
   cursor 384, with 0 errors and 0 warnings added.

Full buffer counts therefore are not a zero-Console-errors claim.

The scene still serializes `IndustryPanel` at 760×560. The 760×700 scroll runtime check
temporarily changed `sizeDelta` in Play Mode and discarded it on stop; it proves the scroll
path in the live panel, but does not make that path reachable in the shipped scene until
the M05-authored height is integrated. Scene and save data remained unchanged. Detailed
artifacts are machine-local and ignored by Git at `Logs/verify/m04-scroll-fit-2026-11-16.md`
and `Logs/verify/m04-validator-2026-11-16.md`.

## Still unmet, with the exact reason

- **离家 (left-family) state.** `SuccessionPersonLife` stores only `BirthYear`, `Alive` and
  `Retired`; no domain field records leaving the family, so the detail column says nothing
  about it rather than inventing a state. Needs a domain field plus a save schema and
  migration change.
- **Succession support distribution.** `SuccessionCandidate` carries only id, name, age,
  role and the designated flag; no support, faction or approval data exists in the domain,
  so no distribution is shown.
- **Succession consequences and risks in the graph column.** The column shows recorded
  eligibility only. The consequence and risk copy lives in the SC-010 succession overlay,
  which already has the highest presentation priority; duplicating it in the graph would
  create a second source of the same rules.
- **Drawn relationship preview in the formation page.** The preview shows the recorded
  path as text. A drawn mini-graph needs a renderer and a scene/integrator decision about
  its footprint.
- **Left generation gutter.** With the detail column the graph viewport is 478 px; a
  64 px gutter would drop visible cards from two to one, so the per-card `第N代` label and
  the `家族亲缘` band header remain the generation marker. This is a deliberate trade-off
  that the coordinator can overrule.
- **Serialized scene objects.** The detail column, its scrollbar and the formation preview
  are runtime-built (and `DontSave`), because this task may not edit or save the scene. The
  generators and `FoundationSceneValidator` do not yet assert them, so they are not part of
  scene-level evidence.
