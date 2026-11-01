# M3 Plan - Town NPC Dialogue

## Goal

Deliver the Phase 1 character interaction loop: five fictional NPCs with stable IDs, town-specific rosters, branching dialogue, a relationship change shown in the UI, and safe diagnostics for unknown NPC IDs.

## Tasks

1. Carry committed M3 character, relationship, and dialogue rules onto the accepted M2 base. **Done** on `feature/m3-accepted-integration`.
2. Integrate town rosters and dialogue UI in FoundationScene, keeping the accepted M2 map layout and return-to-map flow. **Done** in the current worktree.
3. Verify all UI references and RectTransforms with the FoundationScene text validator; update layout through the existing transactional updater if required. **Done**; validator reports PASS with 0 errors.
4. Compile in Unity 6000.6.6f1; run EditMode tests and available PlayMode tests; check Console errors. **Done**; 136 EditMode tests passed, no PlayMode tests are present, and Console showed 0 errors/warnings.
5. Walk the Play Mode demo path and perform an independent diff/evidence review. **Done**; the review found no Critical or Important findings and listed minor follow-ups in `PROJECT_STATE.md`.
6. Update state/demo docs, push the feature branch, and deliver the M3 Play Demo for user acceptance. **Done**; branch is published, the user accepted the demo, and the milestone is merged and tagged `demo-M3`.

## Acceptance

The player can travel to a town, view only that town's NPCs, start a conversation, choose a branch, see the relationship result, and return without losing the world-map state. All five NPCs are assigned, and invalid IDs produce a diagnostic instead of an exception.
