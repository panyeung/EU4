# M03 NPC Autonomy GitHub Prior-Art Review

**Reviewed:** 2026-11-07
**Scope:** Read-only review of up to three Unity Utility AI repositories before M03 implementation. No packages were installed or source copied.

## Candidates

### ZorPastaman/UtilityAI — https://github.com/ZorPastaman/UtilityAI

- **License:** MIT (`LICENSE.md` on GitHub).
- **Compatibility:** UPM manifest declares Unity `2022.3`; Unity 6 compatibility is plausible from the declared floor but was not independently tested here.
- **Maintenance:** GitHub commit history shows a latest change on 2025-10-16; the main feature development in the visible history is concentrated in 2023.
- **Dependencies:** Requires `com.zor.simple-blackboard`; provides runtime Brain/Action/Consideration types, serialized Brain assets, and debug information.
- **Reason:** Closest cleanly licensed package and useful reference for separating candidate scoring from runtime action execution.
- **Estimated integration effort:** Moderate; package and blackboard dependency, profile/brain configuration, adapting float consideration/debug data to integer deterministic project contracts.
- **Decision:** **Build**. The project has only two agents and three candidates; the package's generic Brain/Blackboard and serialized authoring surface add more moving parts than the needed rules.

### Sylpheed/utility-ai — https://github.com/Sylpheed/utility-ai

- **License:** No license was visible in the reviewed repository page; reuse rights are therefore unconfirmed.
- **Compatibility:** Unity project structure is present, but no Unity version floor or Unity 6 verification was found in the reviewed README.
- **Maintenance:** The page reports 91 commits; a reliable recent-activity date was not available in the reviewed content.
- **Dependencies:** README requires Serialize Reference Extensions in addition to the Utility AI package; uses ScriptableObject behaviors/considerations and a runtime UtilityAgent.
- **Reason:** Describes candidate considerations, action lifecycles, and reasoners relevant to the design.
- **Estimated integration effort:** Moderate to high; dependency, authoring assets, and its action lifecycle would need adaptation to the existing date-driven `DailyActionState`.
- **Decision:** **Do not reuse**. License and Unity compatibility are not established, and its architecture is larger than this slice.

### DevOelgaard/Utility-Ai-Creator-For-Unity — https://github.com/DevOelgaard/Utility-Ai-Creator-For-Unity

- **License:** No license was visible in the reviewed repository page; reuse rights are unconfirmed.
- **Compatibility:** README states Unity `2021.2.10f1` or higher, but Unity 6 compatibility was not independently verified.
- **Maintenance:** Repository activity was not reliably established from the reviewed page.
- **Dependencies:** Git URL package plus an editor authoring tool; README describes response curves and multiple scoring/selecting components, including random selection among high scores.
- **Reason:** Demonstrates the larger authoring and response-curve approach used by general-purpose Utility AI editors.
- **Estimated integration effort:** High relative to the two-person, three-action M03 slice; would require adapting its selection/ticking and editor model to deterministic daily turns.
- **Decision:** **Do not reuse**. License is unconfirmed and the package is more complex than needed.

## Recommendation

Implement the scoped integer Utility score and predefined action lifecycle inside the existing Domain/Application layers. Reuse `GameSession`, `DailyActionState`, `SessionStateChanged`, and the current save/scene-generation patterns. Do not add a package. The project code remains original; this review informed architecture only and did not copy external code.

## Sources

- [ZorPastaman UtilityAI README and repository](https://github.com/ZorPastaman/UtilityAI)
- [ZorPastaman UtilityAI package manifest](https://raw.githubusercontent.com/ZorPastaman/UtilityAI/main/package.json)
- [ZorPastaman UtilityAI commit history](https://github.com/ZorPastaman/UtilityAI/commits/main/)
- [Sylpheed utility-ai README and repository](https://github.com/Sylpheed/utility-ai)
- [DevOelgaard Utility AI Creator README and repository](https://github.com/DevOelgaard/Utility-Ai-Creator-For-Unity)
