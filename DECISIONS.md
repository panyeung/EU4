# Decisions

## 2026-10-29 — Use the existing M2 branch for the demo candidate

- Decision: Keep M2 on `feature/m2-world-map` and keep M3 separate until M2 is accepted.
- Alternatives: Start a new integration branch or combine M2 with the later M3 branch.
- Reason: The existing branch contains the M2 implementation and is the project currently open in Unity. This preserves the verified scene and all existing branch history.

## 2026-10-29 — Use built-in UI and defer later Phase 1 systems

- Decision: Keep the M2 UI on Unity's built-in uGUI components and built-in runtime font. Leave facilities as locked placeholders; do not add packages or third-party assets.
- Alternatives: Add an external UI package or include facility, character, mission, and save systems in M2.
- Reason: The roadmap scopes M2 to map travel and town-menu entry/return. Later systems remain in their dependent milestones.
