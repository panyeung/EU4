# SC-010 Demo — Initial Head Retirement and Succession

## What this slice proves

A directly playable FoundationScene flow in which the first family head retires, the
player chooses a lawful successor, control transfers exactly once without replacing
permanent person/family IDs or resetting the campaign, a minor heir can be placed under
a regent and receive full control after an explicit adult handoff, a no-candidate family can make one
crisis adoption/rebuild decision or end explicitly as a terminal extinction state that stops
time and travel until an earlier save is loaded, and save/load restores both a
pending decision and a completed handoff.

All names, IDs and content are original to this project.

## Scene setup (lead only, once)

1. Open `Assets/Scenes/FoundationScene.unity` in the already-running Unity Editor.
2. Run `Tools/Sengoku Chronicle/Add SC-010 Succession Panel`.
3. Run `Tools/Sengoku Chronicle/Validate Foundation Scene`.
4. The report `Logs/SceneValidationReport.txt` must say `PASS SC-010 succession panel
   hierarchy, bindings, ordering, raycast, and layout.` and end with `RESULT: PASS`.

Do not run batchmode or a second Unity instance.

## Play Test A — Adult succession with an invalid designated heir

1. Enter Play Mode in FoundationScene.
   Expected: the title overlay reads `战国立志传 · Phase 1 演示` and offers `新游戏`
   and `继续`. Click `新游戏`.
   Expected: the title overlay hides. The top-left succession HUD shows
   `当代家主：明智 晴信（32岁）` and the buttons `退休并安排继承` and
   `示范：无继承人危机`. The date HUD starts at `1560-04-01`.
2. Click `退休并安排继承`.
   Expected: the succession modal opens. The status explains that the original
   designated heir is dead/left/disqualified and must be replaced. Exactly three
   candidates are listed: 明智 正綱 (24, collateral kin, adult), 明智 時姫 (14,
   direct kin, minor), 明智 信正 (8, direct kin, minor). 明智 景祐, 明智 千恵 and
   佐近 are not listed as candidates.
3. Click 明智 正綱, then `确认交接`.
   Expected: the modal closes. The HUD head becomes `明智 正綱`. The status reports the
   completed succession record (`succession.1.person.akechi.masatsuna`).
4. Record the current date, location, mission stages, merit and NPC relationship values
   before and after step 3.
   Expected: all are unchanged. The retired 明智 晴信 still exists with the same
   permanent person ID and is marked retired; 明智 正綱 keeps the same person ID and is
   now the only Head. The family ID `family.akechi` is unchanged.
5. With the prior handoff complete, note that the HUD's `退休并安排继承` button is
   interactive again. The confirm button is hidden because there is nothing left to
   confirm: the previous handoff is single-shot. Clicking `退休并安排继承` starts a new,
   separate succession cycle for the current head (明智 正綱); it does not repeat or
   re-apply the completed transfer. If the player does not start that new retirement, the
   head, succession count and result record do not change. Starting it is valid and, once
   resolved with a lawful candidate (plus a regent when that candidate is a minor),
   increments the succession count again.

## Play Test B — Save/load at a pending decision and after completion

1. Start a new game, click `退休并安排继承`, select 明智 正綱 but do not confirm.
2. Click the map HUD's `存档` button.
   Expected: the save succeeds while the succession decision is pending and the
   save/load status text confirms it.
3. Change the live state (for example select a different candidate), then click `读档`.
   Expected: the same `AwaitingSuccessionChoice` phase, the same pending candidate, the
   same head and the same three candidates are restored. The campaign date, location,
   mission stages, merit and NPC relationships are unchanged.
4. Confirm the restored decision.
   Expected: one transfer occurs and the completed record matches Play Test A.
5. Click `存档` again, then `读档`.
   Expected: the completed phase, new head, succession count and result record are
   restored. Confirming again does not transfer a second time.

## Play Test C — Minor heir, regency, and explicit adult handoff

Note: the succession modal is drawn above the clock panel and completely covers the
`推进一天` and `结束月份` buttons, so the clock cannot be clicked while the modal is open;
resolve the succession decision (or click `完成成年交权`) to get the clock controls back.
The modal can also overlap the succession HUD status line, so each step below names the
surface that is actually readable in the state being checked. Refusal reasons that go only
to the clock panel's diagnostics are not treated as visible play-test expectations.

1. Start a new game and click `退休并安排继承`.
2. Select 明智 時姫 (14).
   Expected: the modal asks for an adult regent and `确认交接` is disabled until one is
   chosen. The eligible regents are 明智 千恵, 明智 正綱 and 佐近. Disciples and guests
   are not eligible in this prototype.
3. Select 明智 千恵, then `确认交接`.
   Expected: the modal closes. The HUD head reads `当代家主：明智 時姫（14岁）` and the
   status reads `摄政中：家主 明智 時姫（14岁），摄政 明智 千恵。受限决策：摄政期间不能再次使用「退休并安排继承」或更换家主；成年时暂停并等待确认交权。`
   The `受限决策` text names only the retirement and head-change restrictions; it does not
   mention the crisis demo. Separately, the `示范：无继承人危机` button is disabled during the
   regency (non-interactable and drawn with the disabled tint), as is `退休并安排继承`.
   No succession modal is open while the regency is still running, so this status is fully
   readable; it is the surface that names both the restricted actions and the explicit
   adult handoff before the adulthood threshold is reached.
4. Use `结束月份` (resolving any event-choice modal by choosing an available option) until
   the month advance reaches 1562.
   Expected: the advance that enters 1562 opens the succession modal. The modal's own status
   reads `幼主已成年，摄政结束条件已满足。时间推进暂停，请点击「完成成年交权」把控制权交还家主。`
   and the confirm button is relabelled `完成成年交权`. The modal is drawn above the clock
   panel and completely covers `推进一天` and `结束月份`, so those controls cannot be clicked
   while the handoff is pending; do not attempt to click them. The modal also covers the right
   part of the HUD status line, so the player reads the modal's handoff message instead; the
   partly hidden HUD status is not part of this check.
5. Click `完成成年交权`.
   Expected: the modal closes. The feedback line reports
   `继承人已成年，摄政结束，家督控制权交还本人。` and the HUD head becomes 明智 時姫.
   The handoff itself does not advance an extra month, does not
   reset mission stages, merit or NPC relationships, and does not grant a second month
   action budget. Time advancement works again afterwards.
6. Save during the regency before the pause, load, and continue advancing.
   Expected: the regency phase, minor head and regent round-trip, and the same pause and
   explicit handoff still occur at the same threshold.

## Play Test D — No-candidate crisis adoption or extinction

1. Start a new game and click `示范：无继承人危机`.
   Expected: no succession modal opens. The head still reads
   `当代家主：明智 晴信（32岁）`, the status still reads
   `家主在位。可退休并安排继承。`, and the feedback line reports
   `已进入固定示范：无合法继承人危机。`. The HUD does not display any roster.

   Scenario setup only (not shown by the HUD): the fixed crisis roster has 明智 晴信,
   明智 千恵 and 佐近 alive, the designated heir 明智 景祐 dead, and no lawful
   successor remains.
2. Click `退休并安排继承`.
   Expected: the modal states the crisis condition (at least one living adult family
   member as sponsor) and cost (adopt one adult child and record the family crisis).
   `接受危机收养` and `放弃并灭亡` are visible.
3. Click `接受危机收养`.
   Expected: a new adult adopted child `養子 宗次` is created with a permanent ID under
   `person.adopted.family.akechi.*`, receives the `AdoptedChild` role and an Adoption
   kinship link from 晴信, then becomes the new head in the same action. The result
   record starts with `crisis.succession.1.`. The old designation is cleared, and a
   second crisis adoption is not offered.
4. Repeat Play Test D from step 1, then click `放弃并灭亡`.
   Expected: the family phase becomes `Extinct` and the succession modal stays open as a
   persistent terminal overlay. Its status reads `家族已经灭亡：无法再推进时间或旅行，也没有可继续的继承选择。`
   and its body points to reading an earlier save. `退休并安排继承`, `示范：无继承人危机`,
   all candidate buttons and both crisis buttons are hidden or disabled; no new succession
   choice is offered. `推进一天` and `结束月份` are disabled, as are the map travel and
   location buttons, and the date, location and mission state do not change on any attempted
   advance. The map HUD's `存档`/`读档` remain available, so loading an earlier checkpoint
   reopens the pre-extinction state. The HUD offers no way to continue the family in place.

## What is intentionally not claimed

- Unity compilation, EditMode results, Console counts, scene validation and Play Mode
  evidence must be recorded separately by the lead in the existing Editor.
- This text procedure is the Play Test script; it is not evidence that the procedure has
  already been executed.
- `WorldClockPanel.LastAdvanceMessage` is diagnostics only: a C# property plus a Console
  entry, with no visible label bound to it. A clock control covered by the modal cannot be
  clicked, so no on-screen refusal message is claimed for that state.
- SC-011 cross-generation event content, SC-012 chronicle, full faction politics,
  economy costs, and a real action-budget counter are out of scope.
- Regency-only decision content from GDD 4.3 (power struggle, family-policy change, a
  regent refusing to hand over) is not implemented. The regency HUD only states the
  restrictions that actually exist: retirement/crisis actions are disabled and time pauses
  until the explicit handoff.
