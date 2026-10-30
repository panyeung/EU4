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
