# M3 Play Demo Candidate - Town NPC Dialogue

## Start

Open `Assets/Scenes/FoundationScene.unity` in Unity 6000.6.6f1 and press **Play**. Select Atsuta (熱田) on the world map and confirm travel (确认出发).

## Try

1. Confirm the travel preview shows one day and an arrival date of 1560-04-02.
2. In Atsuta, open **Characters** (人物). The roster should show Kuroda Iori (黑田 伊织) with relationship 0.
3. Start the conversation and choose **Accept**. The terminal dialogue should say “Trust is earned.” and show relationship +10.
4. Return to the town, then return to the map. Atsuta and 1560-04-02 should remain current.

The other dialogue choice is covered by EditMode tests; town-specific roster assignments cover all five NPCs across Kiyosu, Atsuta, and Nagoya.

## Verification

- Unity 6000.6.6f1 opened this worktree with `FoundationScene` loaded; script compilation completed with no C# compiler diagnostics in the current Editor log.
- `FoundationSceneValidator`: PASS, 0 errors. It checked required objects, serialized references, hierarchy, and RectTransforms.
- EditMode: 136 passed, 0 failed.
- PlayMode test tab: no tests are present.
- Manual Play Mode: the path above completed; the map retained Atsuta and the travel date after returning from town.
- Console after Play Mode: 0 errors and 0 warnings.

## Status

This is a demo candidate for user evaluation. Independent read-only review found no Critical or Important findings; formal Kanban review tools are unavailable in this session. M3 is not accepted yet. Do not merge or tag before user acceptance.
