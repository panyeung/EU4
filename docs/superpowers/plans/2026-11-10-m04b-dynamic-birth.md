# M04B Dynamic Family Birth Simulation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** Add deterministic monthly pregnancy and childbirth to the existing family simulation, with durable gender/pregnancy data and visible roster status.

**Architecture:** Keep person gender and active pregnancies in the existing succession-family aggregate; pure C# rules plan conception and due births, while `GameSession.TryAdvanceToNextMonth` applies the plan inside its existing transaction. Reuse the persisted `WorldSimulation` stream, stable person IDs, family graph, SaveData migration chain, and existing roster page.

**Tech Stack:** Unity 6.6.6f1, C#, existing uGUI, EditMode Test Runner, current SaveData JSON schema and `FoundationSceneValidator`; no packages.

**Spec:** [M04B design](../specs/2026-11-10-m04b-dynamic-birth-design.md)

## Global Constraints

- Stay in `C:\Users\panye\My project`, current `main`; create no Unity project, branch, or worktree.
- OpenCode2 owns all code changes. Preserve all pre-existing dirty/untracked files and Unity `.meta` GUIDs; do not touch `Library` or `Temp`.
- Do not change scenes or ProjectSettings. Reuse the current `FamilyRosterPanel` scene binding; no new scene root.
- Do not run batchmode against the project held by the user's Editor. Lead performs Unity validation and Play Mode after the implementation and read-only review.
- Keep M03 autonomy restricted to its current pilots; newborns do not become pilots.
- Birth rules: 3% monthly conception chance; 9 monthly steps; gestating parent Female age 16–40, other parent Male age 16–60; both alive and current family members; one active pregnancy per female/pair; multiple male spouses may each have a pregnancy.
- Gender values are `Unspecified`, `Male`, `Female`; v12 migration sets all rows to `Unspecified`, then resolves only authored stable IDs through `PersonGenderCatalog`; it never infers from names.
- Keep month settlement atomic across date, pregnancy, succession-world members/kinship, newborn sub-states, random stream, and chronicle.
- Do not add external code, packages, paid services, or commercial game content.

## Review Focus

- **Unknown/invalid gender:** migrated unknown characters remain playable but are ineligible; undefined values are rejected. Cover in Task 1 and Task 3.
- **Calendar boundary:** conception/due months crossing December and years settle exactly once. Cover in Task 2 and Task 4.
- **Polygamy and duplicate pregnancy:** each female and each pair has at most one active pregnancy while a male may have pregnancies with different spouses. Cover in Task 2 and Task 4.
- **Stable generation:** same seed/state yields the same conception, newborn gender, ID, and name; IDs/names cannot collide with registered persons. Cover in Task 2 and Task 4.
- **Atomic rollback:** injected month-settlement failure restores all family, pregnancy, random, date, and chronicle state. Cover in Task 4.

## File Ownership Map

- `Assets/Scripts/Domain/PersonState.cs`: `PersonGender` and immutable person gender.
- `Assets/Scripts/Domain/PersonGenderCatalog.cs`: explicit stable-ID gender for authored scenario people, shared by scenario initialization and old-save migration.
- `Assets/Scripts/Domain/FamilyPregnancyState.cs` and `FamilyPregnancyRules.cs`: ordered pregnancy state, eligibility, deterministic monthly planning, progress/due dates.
- `Assets/Scripts/Domain/SuccessionState.cs`: own pregnancy state under `SuccessionWorld`, where family persons and kinship already live.
- `Assets/Scripts/Application/FamilyBirthNaming.cs`: stable newborn ID and original sex-specific display-name selection.
- `Assets/Scripts/Application/FamilyBirthSettlement.cs`: pure monthly plan for due births and new conceptions.
- `Assets/Scripts/Application/GameSession.cs`: stage/apply monthly birth plan and include it in rollback; initialize existing per-person state for newborns.
- `Assets/Scripts/Domain/DailyActionState.cs`: add one idle current-location row for a newborn.
- `Assets/Scripts/Domain/HistoryLedger.cs` and `FamilyChronicleEntry.cs`: append one birth chronicle using the existing family-formation category.
- `Assets/Scripts/Persistence/SaveData.cs`, `SaveDataValidator.cs`, `SaveDataMigrator.cs`: v13 gender/pregnancy mapping, validation, capture/restore, v12 migration.
- `Assets/Scripts/Presentation/FamilyRosterPanel.cs`: show current pregnancy month and expected month on the gestating person's existing card.
- Tests stay in `Assets/Tests/EditMode/`; every new Unity C# file receives and preserves its `.meta` file.

---

### Task 1: Add PersonGender and annotate authored scenario people

**Files:**
- Modify: `Assets/Scripts/Domain/PersonState.cs`
- Create: `Assets/Scripts/Domain/PersonGenderCatalog.cs` and `.meta`
- Modify: `Assets/Scripts/Domain/SuccessionState.cs`, `Assets/Scripts/Domain/SuccessionService.cs`
- Modify: `Assets/Scripts/Application/M2FixedScenario.cs`, `SC020EducationScenario.cs`, `SC021FamilyResearchScenario.cs`, `SC022FamilyFormationScenario.cs`, `SC023SeclusionScenario.cs`, `SC024FamilyDoctrineScenario.cs`
- Test: `Assets/Tests/EditMode/PersonGenderTests.cs` and `.meta`

**Interfaces:** `PersonState(PersonId id, string displayName, PersonGender gender = PersonGender.Unspecified)` exposes immutable `Gender`; existing two-argument construction remains source-compatible. `SuccessionWorld.TryAddPerson` rejects undefined gender enum values.

- [ ] Write tests: default gender is `Unspecified`; explicit `Male`/`Female` survives capture; undefined gender cannot enter the world; authored scenario IDs resolve from the explicit catalog.
- [ ] Run the focused tests and confirm the new assertions fail against the old model.
- [ ] Add the enum/property/validation and set gender at every fixed scenario person-construction site, including crisis-adopted people.
- [ ] Re-run the focused tests; confirm existing two-argument `PersonState` callers still compile and default to `Unspecified`.

### Task 2: Add pregnancy state, eligibility, and deterministic birth naming

**Files:**
- Create: `Assets/Scripts/Domain/FamilyPregnancyState.cs` and `.meta`
- Create: `Assets/Scripts/Domain/FamilyPregnancyRules.cs` and `.meta`
- Create: `Assets/Scripts/Application/FamilyBirthNaming.cs` and `.meta`
- Modify: `Assets/Scripts/Domain/SuccessionState.cs`
- Test: `Assets/Tests/EditMode/FamilyPregnancyRulesTests.cs`, `FamilyPregnancyStateTests.cs`, `FamilyBirthNamingTests.cs` and `.meta` files

**Interfaces:** `FamilyPregnancyState.Records` returns stable gestating-person order; each `FamilyPregnancyRecord` carries gestating ID, other-parent ID, conception date, and due date. `FamilyPregnancyRules.PlanMonth(SuccessionState state, GameDate currentDate, GameDate nextDate, DeterministicRandomStream random) -> FamilyPregnancyMonthPlan` returns proposed starts, completions and cancellations without mutating state. `FamilyBirthNaming.Create(SuccessionState state, PersonId parentA, PersonId parentB, GameDate birthDate, PersonGender gender) -> FamilyBirthName` returns a stable ID and display name or a failed result with reason.

- [ ] Write and run failing tests for gender/age/life/membership eligibility, duplicate spouse links, one-pregnancy-per-female, polygamy, invalid dates, nine month boundaries and stable random results.
- [ ] Implement sorted cloneable pregnancy records with validated start/cancel/complete operations; store them on `SuccessionWorld`.
- [ ] Implement monthly 3% pair rolls using only `RandomStreamKind.WorldSimulation`; consume no random values for ineligible pairs.
- [ ] Implement original male/female given-name tables; derive IDs from the stable sorted parent pair and conception date, and add a stable suffix only if a display-name collision exists.
- [ ] Run focused tests; verify record order, equality/replay, and collision behavior.

### Task 3: Persist gender and pregnancy state as schema v13

**Files:**
- Modify: `Assets/Scripts/Persistence/SaveData.cs`, `SaveDataValidator.cs`, `SaveDataMigrator.cs`
- Test: `Assets/Tests/EditMode/SaveDataMigratorTests.cs`, `SaveDataValidatorTests.cs`, `SuccessionSaveTests.cs`, `GameSessionSaveTests.cs`

**Interfaces:** `SuccessionPersonSaveEntry.gender` stores the `PersonGender` numeric value; `SuccessionSaveData.pregnancies` stores stable pregnancy records. `SaveDataValidator.CurrentVersion` becomes 13. v12 migration sets every gender to `Unspecified`, resolves only IDs in `PersonGenderCatalog`, creates an empty pregnancy list, and preserves all other fields; it must not infer sex from names.

- [ ] Add tests for v12→v13 migration preserving all legacy values, resolving only authored IDs through `PersonGenderCatalog`, leaving arbitrary IDs `Unspecified`, and adding no pregnancies.
- [ ] Add tests for round-trip preservation of known gender and an in-flight pregnancy.
- [ ] Add rejection tests for unknown gender enum, unknown parent IDs, a non-member gestating parent, a dead gestating parent, invalid dates, duplicate pregnancies, and non-stable ordering; allow a deceased other parent whose identity remains a family member.
- [ ] Implement DTO capture/restore/clone, validation, and migration using the existing version chain.
- [ ] Run focused migration/save tests; verify a v12 fixture and a v13 pregnancy round-trip.

### Task 4: Integrate monthly conception and birth transactionally

**Files:**
- Create: `Assets/Scripts/Application/FamilyBirthSettlement.cs` and `.meta`
- Modify: `Assets/Scripts/Application/GameSession.cs`, `Assets/Scripts/Domain/DailyActionState.cs`, `Assets/Scripts/Domain/HistoryLedger.cs`, `Assets/Scripts/Domain/FamilyChronicleEntry.cs`
- Test: `Assets/Tests/EditMode/FamilyPregnancySessionTests.cs`, `GameSessionSaveTests.cs`, `GameSessionMonthTurnTests.cs`

**Interfaces:** `FamilyBirthSettlement.Plan(SuccessionState succession, GameDate currentDate, GameDate nextDate, DeterministicRandomStream random) -> FamilyBirthSettlementPlan` returns an immutable plan containing the `FamilyPregnancyMonthPlan`, due newborn facts, and chronicle facts. It delegates eligibility/progress to `FamilyPregnancyRules.PlanMonth`; `GameSession` applies the plan once inside `TryAdvanceToNextMonth` and restores its family and random effects on any failure. `GameSession.FamilyPregnancies` exposes the sorted read-only pregnancy records.

- [ ] Write failing session tests: due birth on the ninth month boundary once; dead gestating parent cancels; the other parent's death preserves the birth; child gets both parent links and sibling facts; no same-month immediate re-conception.
- [ ] Add newborn rows to `CharacterDevelopmentState` with `EducationScenarioProvider.InitialValuesFor` and to `DailyActionState` at `CurrentLocation`; leave M03 autonomy and other unrelated state untouched.
- [ ] Append one `家族新生` chronicle entry only when birth commits; conception alone does not append a chronicle.
- [ ] Add a failure-injection regression proving month rollback restores clock, succession members/kinship/gender, pregnancy state, daily/growth rows, RNG, and chronicle; retry must match the no-failure path, using a succession snapshot captured through `SuccessionSaveData` or an equivalent deep snapshot.
- [ ] Run focused session/month/save tests and ensure no partial family mutations survive any failure path.

### Task 5: Show pregnancy in the existing family UI and update reports

**Files:**
- Modify: `Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- Test: `Assets/Tests/EditMode/FamilyRosterPanelTests.cs`
- Modify: `PROJECT_STATE.md`
- Create: `docs/reports/m04b-pregnancy-simulation.md`

**Interfaces:** `GameSession` exposes a read-only pregnancy snapshot; the existing roster refresh projects the active gestating person's progress and expected month without creating scene objects or changing time.

- [ ] Write a panel test that a pregnant member shows `怀孕 · 第 n/9 月 · 预计 YYYY-MM`, and a non-pregnant member has no pregnancy label.
- [ ] Project the read-only status from the game-session snapshot; preserve the existing family roster root, layout, scrolling and navigator bindings.
- [ ] Record the proposed rules, schema migration, test totals, compilation, Validator, Console and Play Mode evidence separately in the report and `PROJECT_STATE.md`.
- [ ] Review `git status` and the staged diff to ensure only M04B files are included; preserve all pre-existing user WIP.

## Handoff and verification gate

- OpenCode2 implements Tasks 1–5 on the current `main` checkout and keeps scene/ProjectSettings untouched.
- After implementation, request a separate sequential OpenCode read-only review of only the M04B diff; correct findings on the same task and re-review.
- Lead checks the active Editor process/projectPath, compiles and runs focused/full EditMode in its Test Runner, runs `FoundationSceneValidator`, records Console counts, then executes Play Mode on the current scene including save/load while pregnancy is in flight and birth on month nine.
- After the applicable review and Unity verification gates pass, commit only verified M04B files and push to `origin/main`. Do not tag or declare M04 complete; M04 final acceptance remains a separate user decision.
