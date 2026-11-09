# Town Overview UI Reuse Research (2026-11-08)

## Scope

Read-only check for a Unity 6/uGUI framework that would materially help the three-location overview. The project already has `WorldMapController`, `TownNpcDialoguePanel`, mission panels, `TownNpcAssignments`, and transactional Editor scene-updater patterns. The Scout Orca alias was not configured (`agent_unconfigured`); the lead performed this bounded GitHub review instead. No repository was cloned and no dependency was installed.

## Candidates

### `MrNerverDie/Unity-UI-Framework`

- Repository: <https://github.com/MrNerverDie/Unity-UI-Framework>
- License: no `LICENSE` file found in the repository root; do not reuse code without license clarification.
- Compatibility/activity: UGUI framework; last GitHub push shown as 2017-06-14; the checked project version file was absent, so Unity 6 compatibility is unverified.
- Fit: view/context stack manager for generic screens; not location-themed, and duplicates existing project navigation.
- Effort/risk: adapting would add framework and migration work with no benefit to the three town pages.
- Decision: Build locally.

### `Ymiku/Multi-Tree-Based-Unity-UI-Framework`

- Repository: <https://github.com/Ymiku/Multi-Tree-Based-Unity-UI-Framework>
- License: MIT (`MIT LICENSE`; retain notice if any code is reused).
- Compatibility/activity: UGUI-based; last GitHub push shown as 2017-07-14; Unity 6 compatibility is unverified.
- Fit: multiple UI-tree navigation/context stack; unrelated to the existing town panels and single-root requirement.
- Effort/risk: adds an obsolete navigation model and migration burden for a page layout.
- Decision: Build locally.

### `LiShengYang-yiyi/YIUI`

- Repository: <https://github.com/LiShengYang-yiyi/YIUI>
- License: repository `LICENSE` is ET License; commercial use requires notifying the rights holder, and modified-code redistribution is restricted. This project does not need that dependency.
- Compatibility/activity: README claims Unity 2022.3 through Unity 6000 support; last GitHub push shown as 2026-03-05.
- Fit: large data-driven UI framework tied to ET and several other systems; unsuitable for a small existing uGUI screen.
- Effort/risk: high integration cost and license obligations; adds multiple framework dependencies.
- Decision: Build locally.

## Recommendation

Use existing project uGUI components and scene-generator conventions. Three original city illustrations plus a small location-bound presenter provide the required differentiation; no external repository, package, or copied code materially reduces work.
