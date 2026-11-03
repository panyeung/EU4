# SC-013 Fixed Scenario Prior-Art Review

Date: 2026-11-03

## Decision

**Build from the existing project code.** `GameSession`, the validated event catalog/runtime, `SuccessionService`, existing save DTOs, the SC-012 family chronicle, and transactional FoundationScene generators already cover the needed primitives. The candidate repositories either provide a different abstraction, have uncertain Unity 6 compatibility, or add dependencies and integration work without supplying the fixed-generation rules required by SC-013. No package, repository, or external code was installed, cloned, or copied.

This report was prepared by the project lead acting in the Scout role; the current Orca worker-agent list exposes general coding agents but no separate `scout` agent ID. Research was limited to these three candidate repositories.

## Candidates

### 1. [mRemAiello/Unity-Game-Utilities](https://github.com/mRemAiello/Unity-Game-Utilities)

- **License:** MIT.
- **Compatibility:** The repository describes itself as a Unity package but does not state a Unity 6 compatibility floor in the inspected README.
- **Maintenance:** GitHub showed 585 commits; the repository page was recently crawled. No release history was shown in the inspected page.
- **Dependencies/security:** The README lists Addressables 2.7.6+, the New Input System, Tri-Inspector, and DOTween. Adopting it would add several packages beyond this slice.
- **Fit/effort:** Its event module broadcasts typed ScriptableObject events and keeps inspector call history; it does not implement durable ordered narrative choices, succession gates, or family data. Adapting it would duplicate the existing SC-005/SC-011 event engine and add high integration effort.
- **Decision:** **Build** with existing project event code; do not adopt the package.
- **Sources:** [event module README](https://github.com/mRemAiello/Unity-Game-Utilities/blob/master/Runtime/Events/README.md), [package overview and requirements](https://github.com/mRemAiello/Unity-Game-Utilities), [package metadata](https://github.com/mRemAiello/Unity-Game-Utilities/blob/master/package.json).

### 2. [TeodorVecerdi/DialogueGraph](https://github.com/TeodorVecerdi/DialogueGraph)

- **License:** MIT.
- **Compatibility:** The README explicitly lists Unity 2020.3 LTS and 2021.2; other versions are untested. Unity 6 compatibility is therefore unverified.
- **Maintenance:** GitHub showed 95 commits and 215 stars. The repository topic page lists its last update as 2023-08-25.
- **Dependencies/security:** Delivered as a Unity package with editor graph tooling; no package is needed for this task. The README marks itself WIP and contains unfinished sections, including `TODO` material.
- **Fit/effort:** It offers node-based branching conversation authoring, but SC-013 is a small fixed event chain already represented by the existing event catalog. Integrating its editor, package metadata, samples, and save behavior would add more work than the authored chain.
- **Decision:** **Build** with the existing event catalog; do not install/adapt the graph package.
- **Sources:** [repository README](https://github.com/TeodorVecerdi/DialogueGraph), [license](https://github.com/TeodorVecerdi/DialogueGraph/blob/main/LICENSE.md), [activity](https://github.com/TeodorVecerdi/DialogueGraph/commits/main).

### 3. [mboracingoz/UnityEventArchitecture](https://github.com/mboracingoz/UnityEventArchitecture)

- **License:** No license was visible on the inspected public repository page; commercial-use permission is unverified.
- **Compatibility:** No Unity version is stated in the README.
- **Maintenance:** The page showed 18 commits, 0 stars, and no releases; activity and maintenance level are unclear.
- **Dependencies/security:** The README describes a generic ScriptableObject event-channel demo and lists no dependency requirements; without an explicit license it is not suitable for reuse.
- **Fit/effort:** It demonstrates decoupled event broadcasts, not deterministic story stages, save/load history, or lawful succession. It does not fill a gap in this project.
- **Decision:** **Build** with existing code; do not reuse due to unclear license and weak feature fit.
- **Sources:** [repository README](https://github.com/mboracingoz/UnityEventArchitecture), [repository activity](https://github.com/mboracingoz/UnityEventArchitecture/commits/main).

## Implementation Guidance

- Keep the accepted SC-011 event data and requirements unchanged.
- Add only SC-013-specific catalog/localization rows and scenario checks, reusing existing stable IDs/history/relationship/save flows.
- Do not add packages or copy external snippets. No external assets are required, so `ASSETS.md` needs no new source entry.
- If later requirements change to a general-purpose narrative graph/editor, repeat the prior-art review before selecting a dependency.
