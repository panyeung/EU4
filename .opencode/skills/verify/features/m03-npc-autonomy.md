# M03 NPC autonomy and read-only Inspector

The two approved pilot NPCs choose and carry out daily actions. The Inspector explains needs, scores, decisions and rejected candidates without changing simulation state.

## Sub-features

- 正綱 and 信正 autonomously choose among rest, training and work.
- Injury permits rest and rejects training and work.
- Zero supplies rejects work without spending supplies.
- An industry-appointed pilot cannot choose work; the Inspector shows the rejection reason.
- The pilot scope remains exactly two people.

## How to get to it (user POV)

1. Start a new game and choose `固定三代剧本（SC-013）`.
2. Open the daily action page and `打开完整调试面板` to view the read-only Inspector.
3. Open `家族内务` to assign an industry post. Return to the daily action page to inspect the pilot's autonomous plan.

## Driving it with VerifyUi

Current evidence is recorded in `Logs/verify/20261113-m03-npc-autonomy.md`; the 2026-11-12 report below is historical.

- Start from a clean `FoundationScene` in the existing Unity Editor. Use the verify skill's `开始新游戏` → `固定三代剧本（SC-013）` route.
- The fresh run at 1560-04-01 showed 正綱 training and 信正 working. The Inspector reported two pilots and showed each person's needs and selected action.
- Assign 信正 to the farm at `Canvas/IndustryRoot/IndustryPanel/IndustryRowFarm/FarmCandidateContainer/IndustryCandidate_industry.farm_person.sc013.nobumasa`.
- In the daily action page select `Canvas/M02DailyActionRoot/M02DailyActionPanel/M02DailyActionScroll/M02DailyActionContent/M02DailyActionRow_person.sc013.nobumasa`, then interrupt with `Canvas/M02DailyActionRoot/M02DailyActionPanel/M02ActionPanel/M02DailyActionInterruptButton`.
- Open the Inspector and probe `Canvas/M03NpcAiInspectorRoot/M03NpcAiInspectorPanel/M03NpcAiInspectorScroll/M03NpcAiInspectorViewport/M03NpcAiInspectorContent/M03NpcAiInspectorCandidatesText`. Expected: Work is excluded for industry duty, its score is 138 after the -12 interruption memory, and Rest is selected at score 45.
- The 2026-11-13 rerun passed `NpcAutonomy` EditMode 86/86 and the full Editor suite 1947/1947. Current Play Mode also verified injury and zero-supplies restrictions through a temporary in-memory `SaveData` fixture loaded with `GameSession.TryApplySave`; it did not verify how ordinary gameplay creates those states. See the current report for exact Console counts and limits.

## Gotchas

- The Inspector's candidate text lives under the ScrollRect viewport and content. Probe that full path after opening the panel.
- The tactical mission eligibility rule excludes pilots with active daily actions. Do not use a tactical injury result to claim an injured-pilot autonomy Play Mode check unless the pilot is eligible for that mission.
- Injury and zero-supplies UI restrictions were verified in Play Mode on 2026-11-13 with controlled in-memory save data, not through ordinary gameplay setup. M03 final user acceptance remains pending.
- A visual capture used to inspect the Inspector remains at `Assets/Logs/verify/_scratch/20261112-m03-inspector-duty.png` because the cleanup command was blocked by automatic review. Do not treat the capture as durable evidence.
