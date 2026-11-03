# SC-017 Generic Family Graph Implementation Report

Date: 2026-11-03
Branch: `feature/sc013-fixed-three-generation`
Worktree: `C:\Users\panye\My project`
Spec: `docs/superpowers/specs/2026-11-03-sc-017-family-tree-roster-design.md`
Plan: `docs/superpowers/plans/2026-11-03-sc-017-generic-family-tree.md`
Play script: `SC017_DEMO.md`

## Outcome

SC-017 replaces the fixed six-row SC-013 name list with a read-only, data-driven,
scrollable family graph. The graph is derived from `SuccessionState` /
`SuccessionWorld` membership and life data plus the typed `KinshipGraph` facts. It
renders every valid member, keeps unconnected kin visible in a separate band,
keeps retainers/disciples/guests out of kinship lines in their own band, marks the
current head and valid/invalid designated heir, and has no six-person or
three-generation limit. All code is presentation and test code; no domain rule,
save format, package, scene, or prefab was created or changed by this worker.

`FoundationScene.unity` is intentionally not saved or re-generated in this worker
run. The scene already contained uncommitted user changes, and this session has no
allowed route to drive the open Unity Editor's menus. The existing
`AddSC013FamilyRosterPanel` updater now recognises the previous row hierarchy and
replaces it with the scrollable graph hierarchy inside one Undo transaction; the
user must run that menu and the validator in the open Editor.

## Prior art and decision

Scout reviewed at most three GitHub candidates before implementation:

- UnityRuntimeNodeEditor (MIT): runtime editable node editor; moderate/high adaptation.
- RadialTree (MIT): strict-tree layout only.
- Botmasher/kinship-term-explorer: no root LICENSE, Unity 2017/static 3D target.

Decision: **Build** a small custom snapshot/layout using the existing project
models. No package, code, asset, or dependency from those repositories was used.
The full recommendation is recorded in `DECISIONS.md`.

## Implementation

### `Assets/Scripts/Presentation/FamilyTreeGraph.cs` (new)

- `FamilyTreeGraphBuilder.Build(SuccessionState, GameDate)` returns a read-only
  `FamilyTreeGraphSnapshot` of `FamilyTreePersonNode` and `FamilyTreeRelationEdge`.
- Membership comes from `World.Family.Members`; name/life from
  `World.Persons`/`TryGetLife`; edges from `World.Kinship.Links`; head/heir from
  `SuccessionState.HeadPersonId`/`DesignatedHeirId` and
  `SuccessionService.IsEligibleSuccessor`.
- Kin roles: head, spouse, direct kin, collateral kin, adopted child. Household
  roles: retainer, disciple, guest. A kin member with no visible edge is
  `UnconnectedKin`; this is not a loss of membership.
- Edges are included only when both endpoints are valid family members and both
  are kin roles. Removed endpoints and links to household members are ignored;
  the valid member remains visible as unconnected kin.
- Persons and edges are sorted by stable IDs/type in the snapshot constructor, so
  insertion/iteration order cannot change the display. No method mutates state.
- `Age` is optional (`HasAge`), so a missing life record renders `年龄不详` instead
  of throwing or hiding the person.

### `Assets/Scripts/Presentation/FamilyTreeLayout.cs` (new)

- Pure C# layout with `FamilyTreePoint`, `FamilyTreeSize`,
  `FamilyTreeNodePlacement`, `FamilyTreeSectionPlacement`, and
  `FamilyTreeLayoutResult`.
- Generations are derived from parent/child and adoption edges; spouse and sibling
  edges keep both endpoints on the same generation. No name, SC-013 ID, count, or
  preset generation is used.
- Cards are placed in deterministic generation rows at fixed sizes; the widest row
  sets the content width. Unconnected kin and household members are laid out in
  their own labelled bands below all kin.
- The result includes total content width/height for scrolling. Positions are
  non-negative, non-overlapping, finite, and independent of input order.

### `Assets/Scripts/Presentation/FamilyRosterPanel.cs` (rewritten)

- Consumes the snapshot and layout instead of `GameSession.FamilyRoster` rows.
- Renders card instances under a node layer and typed line segments under an edge
  layer inside one `ScrollRect` content (vertical and horizontal scrolling).
- Relationship types are visually distinct and documented in a legend: parent/child
  gold, adoption purple, sibling green, spouse blue. Parent/adoption lines run from
  the parent card's bottom to the child card's top; spouse lines connect side by
  side; sibling lines use a shared detour.
- Sections: `家族亲缘`, `未连接亲族`, `家臣・弟子・客将`. A household member never
  receives a kinship line.
- Card text: name, optional age, role, generation/section, plus `【家主】`,
  `【指定继承人】`, or `【指定继承人·已失效】`; deceased/retired state is appended
  when known.
- Opening, closing, scrolling, refreshing, and session refresh only rebuild clones.
  `BindReferences` deliberately does not build runtime clones so the transactional
  scene updater cannot serialize generated cards.
- Read-only test seams expose node/edge/section counts, text by person ID,
  relationship type, last layout size, and scroll content size.

### `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs` (rewritten)

- Creates `FamilyRosterRoot` → panel → title, legend, `FamilyRosterScroll`
  (`ScrollRect` + `RectMask2D`), content, edge layer, node layer, templates, heir
  line, close button, plus the unchanged derived ending panel.
- One Undo group, validate-before-save, and rollback without reloading the scene.
- Recognises the exact previous SC-013 row hierarchy (`FamilyRosterRowTemplate` +
  vertical content) and replaces it inside the same transaction. Any other partial
  roster object is refused without overwriting it.
- Validation checks unique hierarchy/bindings, scroll content/viewport, two-axis
  scrolling, mask, active/inactive templates, fonts, images, raycast targets,
  containment, ordering, and no overlap with title/legend/heir/close. It never
  checks SC-013 names, IDs, or member counts.

### `Assets/Scripts/Editor/FoundationSceneValidator.cs` (modified)

The family-panel reference list now checks the graph bindings (`legendText`,
`scrollRect`, `edgeLayer`, `nodeLayer`, `nodeTemplate`, `edgeTemplate`,
`sectionTemplate`) instead of the removed `rowTemplate`. The full hierarchy check
remains delegated to `AddSC013FamilyRosterPanel.Validate`.

### Tests (new/extended)

- `Assets/Tests/EditMode/FamilyTreeGraphTests.cs` (new): SC-013 exact members and
  five edges, ages/head/heir, unconnected kin retained, household separation even
  when an invalid kinship fact points at a retainer/disciple/guest, removed endpoint
  handling, no mutation, null state.
- `Assets/Tests/EditMode/FamilyTreeLayoutTests.cs` (new): more than six members and
  more than three generations, deterministic output after input reversal, collateral
  / spouse / adoption / sibling edges preserved, unique finite positions and bounds,
  household band below kin, empty graph.
- `Assets/Tests/EditMode/FamilyRosterPanelTests.cs` (rewritten): SC-013 nodes and
  typed edges, legend, head/heir markers, separate retainer section, unconnected
  kin, 12-person six-generation scroll content, no duplicate clones after
  close/reopen, no succession/kinship mutation, session-change marker refresh, and
  lawful standard succession moving the head marker.
- `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs` (rewritten): scrollable
  graph hierarchy and bindings, idempotence and injected-failure rollback, preflight,
  live panel rendering, scroll/control separation, legacy row-hierarchy replacement,
  duplicate/missing binding rejection, root ordering.

## Review corrections (independent review `task_b2add2abf46a`)

Date: 2026-11-03. Scope: only the three actionable findings and their focused
tests/documentation; no scene, save, package, GDD, or unrelated file changed.

### 1. Legacy root migration no longer destroys unrecognized data

`AddSC013FamilyRosterPanel.FindLegacyHierarchy` now requires the entire legacy
subtree to match the exact previous SC-013 generator shape before the root is
destroyed. Every descendant must be one of the known legacy objects or the single
`Label` text under a legacy button, and every component must be one of the
components that generator created (`RectTransform`/`Transform`/`CanvasRenderer`
plus the expected `Image`, `Text`, `Button`, `LayoutElement`,
`VerticalLayoutGroup`, `FamilyRosterPanel`, or `M2EndingPanel`). A user-added
child, a custom/extra component, an unexpected graphic, or a duplicate button
label makes migration refuse with `ExistingPartialControl`; the legacy objects
and components remain untouched and no Undo group is opened. A read-only
inspection of the current `FoundationScene.unity` legacy subtree found exactly
that whitelisted component inventory.

Regression tests:
`UpdaterRefusesLegacyRootWithUnrecognizedChild` and
`UpdaterRefusesLegacyRootWithUnrecognizedComponent` in
`Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs`. The existing
`UpdaterUpgradesLegacyRowHierarchyInsideOneTransaction` fixture now also covers
the real legacy button labels and `LayoutElement` components.

### 2. Spouse connectors route around intervening person cards

Relation-line construction moved to the pure `FamilyTreeRouting` planner in
`FamilyTreeLayout.cs`. A spouse link keeps the direct side-to-side line only when
that line is clear. If another card lies on it, the connector takes an orthogonal
detour outside the card row (preferring the gap below the cards, with deterministic
fallbacks), and the planner rejects any candidate that intersects a non-endpoint
card. `FamilyRosterPanel` now uses that planner and exposes a read-only
`RelationPath`/`RelationPathFor` test seam.

Regression tests:
`SpouseRoute_AvoidsInterveningPersonCard` and
`SpouseRoute_KeepsDirectLineWhenNoCardIntervenes` in
`Assets/Tests/EditMode/FamilyTreeLayoutTests.cs`; and
`SpouseConnectorRoutesAroundInterveningPersonCard` in
`Assets/Tests/EditMode/FamilyRosterPanelTests.cs`, which asserts the rendered
spouse polyline does not cross the card placed between the spouses.

### 3. Known death/retirement is shown without an age

`FamilyTreePersonNode` now carries `HasLife` separately from `HasAge`. Card text
moved to the pure `FamilyTreeCardText.Format` helper: a known life record shows
`已故` or `已隐居` even when the age renders as `年龄不详`, while a missing life
record never claims a known death or retirement.

Regression tests:
`Build_ReportsKnownLifeStatusWhenAgeIsUnavailable` in
`Assets/Tests/EditMode/FamilyTreeGraphTests.cs`; and
`CardTextShowsKnownDeathAndRetirementWithoutAge` in
`Assets/Tests/EditMode/FamilyRosterPanelTests.cs`.

### Correction files

- `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`
- `Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- `Assets/Scripts/Presentation/FamilyTreeGraph.cs`
- `Assets/Scripts/Presentation/FamilyTreeLayout.cs`
- `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs`
- `Assets/Tests/EditMode/FamilyRosterPanelTests.cs`
- `Assets/Tests/EditMode/FamilyTreeGraphTests.cs`
- `Assets/Tests/EditMode/FamilyTreeLayoutTests.cs`
- `docs/reports/sc017-implementation.md` (this section)

### Correction verification

- **Roslyn type-check:** Presentation, Editor, and EditModeTests assemblies
  rebuilt with Unity's bundled compiler and reference set; **0 errors**.
- **Pure routing/card-text harness:** **77/77 checks passed**, including the
  intervening-card detour, the clear-line direct path, the exact M2 scenario row
  order, and the known-status-without-age text cases.
- **Unity Editor compilation:** **NOT RUN** for the correction sources.
- **EditMode Test Runner:** **NOT RUN**.
- **Console counts:** **NOT INSPECTED**.
- **Scene updater / save / validator:** **NOT RUN**; `FoundationScene` was not
  touched by this correction.
- **Play Mode / user acceptance:** **NOT RUN / PENDING**.

Static compilation and the pure harness are not Unity Editor, Test Runner,
Play Mode, or user-acceptance evidence.

## Review correction 2 (independent re-review `task_ffbd3fbf8fa9`)

Date: 2026-11-03. Scope: the remaining Important spouse-route finding only. No scene,
save, package, GDD, or unrelated source was changed.

### Guaranteed spouse route replaces the least-crossing fallback

`FamilyTreeRouting` no longer keeps a candidate that merely has the fewest card
intersections when the direct line and all six standard detour lanes fail. The six
standard lanes are still tried first and are accepted only when
`CountCardIntersections` is exactly zero. After that, a clearance-aware rectilinear
visibility grid builds a route:

- every non-endpoint card is expanded by `CardClearance` (12 units); endpoint cards
  keep their real bounds, so the connector still starts and ends exactly on the
  endpoint card side boundaries;
- grid coordinates come from every expanded card edge plus one outer coordinate line
  beyond the complete graph bounds;
- only axis-aligned grid edges that cross no expanded rectangle are connected, using
  breadth-first search;
- the selected polyline is re-validated against every real person card before it is
  returned, so no segment can pass through a card;
- if the endpoint ports themselves are enclosed by clearance-expanded cards, the grid
  cannot connect them. `TryBuildPath` then returns `false`, `BuildPath` returns an
  empty path, and `FamilyRosterPanel.CreateRelation` records the relation but draws no
  line. There is no path-through-card fallback.

Regression test:
`SpouseRoute_GuaranteedOuterRouteWhenAllStandardCandidatesAreBlocked` in
`Assets/Tests/EditMode/FamilyTreeLayoutTests.cs`. Its synthetic graph places cards on
all six old standard lanes (below, above, and two further lanes in each direction),
then asserts:

- all six standard candidates are blocked by a non-endpoint card;
- the selected connector still starts and ends on the endpoint card side boundaries;
- every selected segment intersects no person card, including the endpoints;
- the selected route leaves the complete card bounds (the outer corridor).

`SpouseRoute_AvoidsInterveningPersonCard`,
`SpouseRoute_KeepsDirectLineWhenNoCardIntervenes`, and the rendered-panel test
`SpouseConnectorRoutesAroundInterveningPersonCard` remain unchanged and passing in the
static harness.

### Correction 2 files

- `Assets/Scripts/Presentation/FamilyTreeLayout.cs`
- `Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- `Assets/Tests/EditMode/FamilyTreeLayoutTests.cs`
- `docs/reports/sc017-implementation.md` (this section)
- `SC017_DEMO.md` (routing expectation)

### Correction 2 verification

- **Roslyn type-check:** Domain, Persistence, Events, Application, Presentation,
  Editor, PlayerMovement2D, and EditModeTests rebuilt with Unity's bundled compiler
  and reference set; **8/8 assemblies exited 0, no errors or warnings**.
- **Pure routing harness:** **86/86 checks passed** against the rebuilt assemblies.
  The first **77/77** are the previous SC-017 checks, unchanged. The nine new checks
  verify each of the six old standard lanes is blocked, the crowded fallback is not a
  blocked direct line, it avoids every card, and it uses the outer corridor.
- **Unity Editor compilation:** **NOT RUN** for this correction.
- **EditMode Test Runner:** **NOT RUN**.
- **Console counts:** **NOT INSPECTED**.
- **Scene updater / save / scene validator:** **NOT RUN**; `FoundationScene` was not
  touched.
- **Play Mode / user acceptance:** **NOT RUN / PENDING**.

Static compilation and the pure harness are not Unity Editor, Test Runner, Play Mode,
or user-acceptance evidence.

## Verification evidence

> The entries below describe the original SC-017 implementation run. The latest
> review-correction results are in **Review corrections** above.

### Obtained in this run

- **Roslyn type-check (8 assemblies, 0 errors)** using Unity's bundled compiler and
  reference set: Domain, Persistence, Events, Application, Presentation, Editor,
  PlayerMovement2D, EditModeTests. Command host:
  `Editor\Data\NetCoreRuntime\dotnet.exe` + `Editor\Data\DotNetSdk\...\csc.dll`;
  log directory `%TEMP%\opencode\sc017-build\`.
- **Pure graph/layout harness: 66/66 checks passed** against the newly compiled
  Domain/Application/Presentation assemblies:
  `%TEMP%\opencode\sc017-harness\`. It covers SC-013 membership/edges/heir, no Sakon
  edge, no state mutation, a 12-person four-generation family, unique finite
  positions, household band separation, deterministic reversed input, adoption /
  spouse / sibling / collateral edges, and empty layout.

### Not obtained

- Unity Editor compilation after the final edits: **NOT RUN**. The open Editor last
  wrote `Logs/Editor.log` at 04:56 local time before the final test rewrite and did
  not automatically re-import afterwards; no refresh was forced because GUI control
  was prohibited. The stale log still contains intermediate errors from the
  pre-rewrite test file, so it is not evidence for the current sources.
- EditMode Test Runner: **NOT RUN**. No allowed route to drive the open Editor.
- `FoundationScene` updater / save / validator: **NOT RUN**. Scene was not touched.
- Console counts: **NOT INSPECTED** for the current sources.
- Play Mode: **NOT RUN** by this worker; the user owns Play Test.
- User acceptance: **ACCEPTED** on 2026-11-03. Exact EditMode totals, Console counts, and the Validator report text were not provided; this records the user's decision without inventing those values.
- Sequential read-only review: **COMPLETED by Codex fallback** because the OpenCode reviewer launcher was unavailable; verdict **ACCEPT**, with no Critical or Important findings. Minor note: no focused `FamilyRosterPanel` test directly covers the empty-route failure branch; static review confirmed it records an empty path and draws no segments. The reviewer ran no Unity or test commands.

Static checks and harness results are not Unity Editor, Test Runner, Play Mode, or
user-acceptance evidence.

## Preserved boundaries

- No commit, push, merge, tag, reset, clean, or branch change.
- No new project, scene, prefab, package, asset, GDD, save-schema, SC-010/SC-011, or
  M2 rule change.
- No kinship editing UI.
- `.meta` files were generated by Unity for the new source/test files; existing
  GUIDs were not modified.
- `Library` / `Temp` were not edited manually.
- Pre-existing dirty files (`FoundationScene.unity`, `SampleScene.unity`,
  `Assets/1.unity`, old-direction markdown, `ProjectSettings/SceneTemplateSettings.json`,
  `graphify-out/`, `nul`, and prior SC-013–SC-016 work) were left in place.

## Final status

- The user reported the provided Unity and Play Test steps passed and explicitly
  accepted SC-017 on 2026-11-03. Exact EditMode totals, Console counts, and the
  Validator report text were not returned, so those numeric/log details remain
  unrecorded.
- The independent read-only review completed with ACCEPT. Its minor note is the
  missing focused panel test for the empty-route branch.
- GitHub integration has not been performed. This branch also contains SC-013–SC-016
  work that is not recorded as user-accepted; preserve it and do not publish the
  accumulated branch as an SC-017-only checkpoint.
