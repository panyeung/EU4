# Decisions

## 2026-10-29 - Use the existing M2 branch for the demo candidate

- Decision: Keep M2 on `feature/m2-world-map` and keep M3 separate until M2 is accepted.
- Alternatives: Start a new integration branch or combine M2 with the later M3 branch.
- Reason: The existing branch contains the M2 implementation and verified scene. This preserves the existing branch history.

## 2026-10-29 - Use built-in UI and defer later Phase 1 systems

- Decision: Keep the M2 UI on Unity's built-in uGUI components and built-in runtime font. Leave facilities as locked placeholders; do not add packages or third-party assets.
- Alternatives: Add an external UI package or include facility, character, mission, and save systems in M2.
- Reason: The roadmap scopes M2 to map travel and town-menu entry/return. Later systems remain in their dependent milestones.

## 2026-10-29 - Separate the town screen from map controls

- Decision: Keep the world-map travel panel below M1 controls, hide map controls while the town menu is open, and make the town panel background opaque.
- Alternatives: Leave both panels visible under a translucent overlay or create a separate town scene.
- Reason: The user's demo feedback identified overlapping panels. Reusing the existing town panel keeps the flow in one scene while making the active screen clear.

## 2026-10-29 - Isolate M3 integration from existing user work

- Decision: Integrate the accepted M2 base and committed M3-01 through M3-03 work on `feature/m3-accepted-integration`, preserving the existing M3 worktree and its uncommitted M3-04 changes.
- Alternatives: Merge into or update the existing M3 worktree, or discard its WIP and restart M3.
- Reason: The existing M3 worktree has changes in the same scene and controller files, so an isolated branch avoids overwriting or disturbing them while retaining useful M3 work.

## 2026-10-31 - Keep M4a as one fixed C# mission

- Decision: Implement the supply-shortage mission as a mission-specific plain C# state model, coordinated by `GameSession` and displayed by one uGUI panel. Use existing Atsuta NPC Kuroda Iori as the merchant and award +10 merit exactly once on a valid report in Kiyosu.
- Alternatives: Extend branching dialogue effects, add a reusable mission framework, add another NPC, or use a package/sample.
- Reason: This is the first complete mission; the fixed state transitions and one-time reward are directly testable without coupling mission rules to UI/dialogue or adding unused infrastructure. Research found no external implementation with a better fit than a focused custom model.
- Scope: Mission actions do not advance time or location. `InitNewGame` resets mission state and merit. Persistence is deferred to M5. No new package or asset is required.
- Status: Approved design; implementation is in progress and remains a demo candidate until scene validation, full verification, and user Play Mode evaluation pass.

## 2026-11-01 - Add two fixed M4b missions

- Decision: Use Suzu's Kiyosu-to-Nagoya sealed-letter delivery and Jirobei's Nagoya-to-Kiyosu market-tally investigation with Genzaemon, then report back to Jirobei. Award +5 merit once per completed mission.
- Alternatives: Leave the two Phase 1 mission slots empty, add new NPCs, or introduce reusable quest authoring.
- Reason: This completes the GDD's three-mission scope using only the existing five NPCs and three towns. The focused models follow the approved M4a pattern; research found external quest frameworks add unnecessary dependencies and infrastructure.
- Constraints: Mission actions never advance time; existing travel does. No package, asset, persistence, or generic framework is added. M5 remains the persistence milestone.
- Verification snapshot (2026-11-01): latest source compiled in the existing Editor; EditMode Run All passed 322/322; FoundationScene updater saved and the fresh validator report passed with zero errors; independent technical QA accepted with no blocking defects. The user reported the requested Play Mode loop passed and explicitly accepted M4b. Exact runtime values and Console counters were not supplied. Kanban review tools were unavailable in this session.
## 2026-11-01 - Keep the mission panel within its validated town footprint

- Decision: Show the existing M4a mission by default and add a page toggle to the two M4b rows inside the same 280-by-240 root.
- Alternatives: Expand the root across the town controls or move existing NPC and return controls.
- Reason: Static layout review found the expanded root would cover the NPC roster and part of the return control. The page switch preserves the scene's existing control positions and the M4a default flow.
- Verification: The M4b validator checks separation from the town title, NPC roster, and return button; updater tests assert the controls do not overlap and their geometry rolls back on failure. All four M4b updater tests passed, and the fresh FoundationScene validator passes.
