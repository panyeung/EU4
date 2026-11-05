# SC-024 Supplemental GitHub Reuse Research

**Date:** 2026-11-05
**Scope:** GitHub-only supplement to [`sc024-reuse-research.md`](sc024-reuse-research.md). Looked for existing Unity implementations relevant to family doctrine persistence/progression and its save/UI seams. Reviewed at most three candidates. No repository was cloned, installed, or copied; no Unity or tests were run.

## Recommendation

**Decision: Build** the SC-024 doctrine state and progression on the in-project patterns in the existing report. The closest GitHub candidates are generic save managers. They provide no creed or ambition model, deterministic settlement integration, council event transaction, stable domain validation, or project-specific migration. One package has a potentially reusable generic migration hook, but adding and adapting a save framework would duplicate the existing versioned save DTO and transaction ownership with little task-specific savings. No candidate meaningfully reduces SC-024 implementation work.

## Candidates

### 1. JoshuaLetessier/UnitySaveSystem

- **Repository:** [GitHub](https://github.com/JoshuaLetessier/UnitySaveSystem)
- **License:** MIT ([repository license](https://github.com/JoshuaLetessier/UnitySaveSystem/blob/main/LICENSE)); permissive, with notice-retention requirement.
- **Relevance:** Generic typed save managers, JSON/PlayerPrefs backends, schema migration hook, multiple slots, metadata, and a JSON save browser. Of the candidates reviewed, its migration seam and save browser have the closest surface-level overlap with SC-024 save/UI work.
- **Unity compatibility:** README documents UPM packaging but gives no minimum or tested Unity version. Unity 6 compatibility is therefore **unverified**; API/package compatibility review would be required.
- **Maintenance/activity:** GitHub showed 86 commits, 0 stars, 0 forks, one open issue, and no release listing in the page reviewed. Commit dates were not exposed in the rendered history, so recent maintenance could not be verified.
- **Dependencies/security:** The README lists modular backends and an Editor package workflow; backend-specific dependency manifests were not independently available in the rendered source. README describes a SHA-256 checksum as corruption detection, and correctly says it is not anti-cheat. No security audit or vulnerability status was established.
- **Integration effort and fit:** Moderate to high. It would add package/module configuration and require mapping the existing `SaveData`/validator/migrator flow into a new generic persistence system. Its migration hook handles data-shape changes generically, not the approved v8-to-v9 default-creed migration or validation/atomic apply rules. It does not implement doctrine progression, event effects, or family UI.
- **Decision:** **Do not reuse for SC-024.** Reconsider only if the project independently chooses to replace its save infrastructure.

### 2. ahmedAwedni/Unity-Universal-Save-Load-System

- **Repository:** [GitHub](https://github.com/ahmedAwedni/Unity-Universal-Save-Load-System)
- **License:** MIT ([license text](https://github.com/ahmedAwedni/Unity-Universal-Save-Load-System/blob/main/LICENSE)); permissive, with notice-retention requirement.
- **Relevance:** A small `ISaveable`/JSON manager with multiple slots. Its README says it was tested in Unity 6+, so it provides a current-version example of simple persistence.
- **Unity compatibility:** README explicitly says “Tested in Unity6+.” The source uses built-in Unity APIs including scene-wide `FindObjectsOfType<MonoBehaviour>(true)`; compatibility is stated by the author, not independently tested here.
- **Maintenance/activity:** GitHub showed 8 commits, 0 stars, 0 forks, and no listed issues or releases. The visible history did not expose commit dates, so current maintenance is unclear and community/activity signals are very small.
- **Dependencies/security:** README claims no third-party dependencies and uses Unity `JsonUtility`. Source inspection shows direct `File.WriteAllText` to the final save path and applies deserialized entries to all matching active-scene `ISaveable` objects. No checksums, schema migration, temporary-file replacement, or transaction/rollback boundary are documented. This is a modest integrity/robustness concern for the project's save flow, not evidence of a reported vulnerability.
- **Integration effort and fit:** Low for a basic scene-object save; high for SC-024. It requires adapting domain state to scene-discovered `MonoBehaviour`s and does not cover versioned save DTOs, deterministic migration, validation-before-apply, stable domain IDs, or rollback. It offers no doctrine UI or progression logic.
- **Decision:** **Do not reuse.** Its Unity 6 claim is useful as a comparison, but the architecture conflicts with the existing domain-owned state and centralized save format.

### 3. CarterGames/SaveManager

- **Repository:** [GitHub](https://github.com/CarterGames/SaveManager)
- **License:** GPL-3.0 ([repository license](https://github.com/CarterGames/SaveManager/blob/main/LICENSE.txt)); **not permissive**, so it does not qualify for adoption under the requested permissive-license criterion.
- **Relevance:** Modular ScriptableObject-based local save system with editor save inspection and custom save objects.
- **Unity compatibility:** README says it is developed/maintained for Unity 2020.3.x and tested pre-release on 2020.3.0f1. No Unity 6 compatibility claim was found.
- **Maintenance/activity:** GitHub showed 215 commits, 44 stars, and 4 forks. The repository has a changelog and documentation folders, but commit dates were not exposed in the rendered page; recent activity could not be verified.
- **Dependencies/security:** `package.json` and README identify a Unity package with modular functionality. No security audit or dependency risk assessment was found in reviewed docs. The README advertises encryption, but that alone does not establish safe or appropriate save integrity/security for this project.
- **Integration effort and fit:** High. It would bring a ScriptableObject save-object model and editor tooling into a project already using explicit domain DTO blocks, validators, and deterministic migrations; it would not implement family doctrine behavior. Its license and unverified Unity 6 compatibility independently rule it out.
- **Decision:** **Reject** on license and compatibility; no adaptation warranted.

## Comparison with project patterns

SC-024 already has `FamilyResearchState` conventions for validated transitions, stable IDs, cloning, and restore validation; pure deterministic settlement results; `GameSession` transaction/rollback boundaries; typed event catalog/engine validation; and versioned `SaveData` DTO migrations. These cover the actual SC-024 rules and persistence seams without introducing an external runtime package. The candidates supply generic persistence mechanics only and do not improve the planned family creed/ambition projection or command UI. Follow the custom, in-project design documented in [`sc024-reuse-research.md`](sc024-reuse-research.md).

## Search and verification notes

GitHub search terms included Unity strategy/family creed or ambition progression, deterministic Unity progression/save systems, and Unity family/dynasty strategy projects. The search surfaced generic save managers rather than a reusable family doctrine or strategy-game component. Candidate README/docs, repository license indicators/files, visible repository activity metadata, and relevant package/source details were reviewed through GitHub pages. Activity dates and a formal security audit were not consistently visible; those gaps are recorded instead of inferred. This is research evidence only, not a Unity compatibility test.
