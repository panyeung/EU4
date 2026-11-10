# 试玩与验收指南
本文件由原始 Markdown 文档合并而成。各节保留原文，标题中的来源路径用于追溯。
## 目录
- [SC010_DEMO.md](#sc010-demo)
- [SC011_DEMO.md](#sc011-demo)
- [SC012_DEMO.md](#sc012-demo)
- [SC013_DEMO.md](#sc013-demo)
- [SC014_DEMO.md](#sc014-demo)
- [SC015_DEMO.md](#sc015-demo)
- [SC016_DEMO.md](#sc016-demo)
- [SC017_DEMO.md](#sc017-demo)
- [SC018_DEMO.md](#sc018-demo)
- [SC019_DEMO.md](#sc019-demo)
- [SC020_DEMO.md](#sc020-demo)
- [SC021_DEMO.md](#sc021-demo)
- [SC022_DEMO.md](#sc022-demo)
- [SC023_DEMO.md](#sc023-demo)
- [SC024_DEMO.md](#sc024-demo)

---

<a id="sc010-demo"></a>
## 来源：SC010_DEMO.md

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


---

<a id="sc011-demo"></a>
## 来源：SC011_DEMO.md

# SC-011 Demo — Cross-Generation Rainy-Night Guest

## What this slice proves

The first generation chooses how to receive the rainy-night guest. After one real SC-010
head change, the successor receives one follow-up encounter whose visible text and options
depend on that saved choice. The root choice, the pending follow-up, the once-only markers,
the relationship value, and the succession state all survive save/load.

All names, IDs and text are original to this project. The follow-up is one encounter with
two mutually exclusive variants, not two independent events.

## Evidence boundary

- User acceptance recorded 2026-11-02: the user reports compilation passed, EditMode passed,
  no Console errors, and Play Test passed. EditMode totals and Console warning count were
  not supplied. This record reflects the user's report; no Editor logs were captured here.
- The `FoundationSceneValidator` result was not reported and is not claimed as passed.
- Focused EditMode coverage includes root-pending and post-root-choice save/load, a second
  succession before the follow-up is scheduled, a regency checkpoint, scheduling-failure
  no-mutation behavior, and v1/v2 content-version migration. The user reports EditMode passed,
  but exact per-suite totals were not supplied.
- No scene, prefab, `ProjectSettings`, package, or asset-GUID change is expected for SC-011.
  Do not run a scene updater unless the lead identifies a scene regression.

## Play Test A — Honored (sheltered) branch, then save/load

1. Enter Play Mode in `FoundationScene`. At the title overlay click `新游戏`.
   Expected: date `1560-04-01`; head `明智 晴信`; no pending event.
2. Click `结束月份`.
   Expected: `雨夜来客` appears with `请他进屋歇息` and `请他明晨再来` (the latter is
   disabled at the default relationship value 0). The root text is unchanged from the
   accepted demo.
3. Choose `请他进屋歇息`.
   Expected: the existing +5 trust summary appears; the event panel closes; the root event
   is recorded as chosen once.
4. Click `退休并安排继承`, choose `明智 正綱`, then `确认交接`.
   Expected: the head becomes `明智 正綱`; the handoff record starts with
   `succession.1.person.akechi.masatsuna`; date, location, mission stages, merit and other
   relationship values are unchanged. Save with `存档`.
5. Click `结束月份`.
   Expected: `旧客的雨夜回访` appears with `重申先代的门诺`, `按当下利害重议`, and
   `止于这一夜`. The body mentions the old guest and the earlier hot tea. Variant B's
   `门房里的旧雨` and its options must not appear.
6. Do not choose yet. Click `读档` to restore the step-4 save, then click `结束月份`.
   Expected: the same `旧客的雨夜回访` with the same three options. This proves save/load at
   the post-handoff stable point. (The game has one save slot, so do this before saving
   again.)
7. Choose `重申先代的门诺`, then click `存档`.
   Expected: feedback confirms the old pledge and the +5 trust summary. Click `结束月份`
   again. Expected: no `雨夜来客` repeat and no second legacy encounter. Click `读档` and
   `结束月份` once more. Expected: the resolved follow-up does not reappear.
8. Record each step's pass/fail and the visible text in the test record. Do not use
   screenshots.

## Play Test B — Deferred branch

1. Start a new game. Before the first `结束月份`, travel to Atsuta and use 黒田伊织's town
   dialogue to raise his trust to at least 1 (accept his proposition), then return to the
   map. This is required because `请他明晨再来` keeps its accepted
   `RelationshipAtLeast 1` requirement.
2. Click `结束月份`, choose `请他明晨再来`, then retire to `明智 正綱` and confirm.
3. Click `结束月份`.
   Expected: `门房里的旧雨` appears with `补上先代欠下的礼数`, `说明当日不得已`, and
   `旧事不必再提`. Variant A's `旧客的雨夜回访` text and options must not appear.
4. Choose `补上先代欠下的礼数`.
   Expected: the +5 trust summary appears. Save, load, and advance one more month.
   Expected: the deferred encounter does not repeat.
5. Record pass/fail and the visible text for each step. Do not use screenshots.

## Out of scope for this Play Test

- The regency path (minor heir and explicit adult handoff) is covered by authored EditMode
  tests in the implementation plan (not executed), not by this user script. It requires
  many month turns and is not part of the smallest slice.
- A second or third generation is out of scope. SC-011 proves one successor facing one
  predecessor decision; GDD 19.4's full three-generation M2 script remains future work.
- Runtime text templating, faction politics, economy costs, chronicles, a new NPC, and a
  new scene are not part of this slice.

## What must not be claimed

- Do not invent EditMode totals, Console warning counts, or a scene-validator result.
- Keep compilation, EditMode, Console, scene validation, Play Mode, and user acceptance as
  separate evidence; static review is not runtime evidence.
- User acceptance is recorded. Future changes still require their own verification and
  acceptance before being treated as an accepted demo.


---

<a id="sc012-demo"></a>
## 来源：SC012_DEMO.md

# SC-012 Play Test — 家族编年史

## 当前状态与证据边界

状态：SC-012 已获用户验收（2026-11-03）。用户报告 EditMode 通过、Play Test 通过。具体 EditMode 数量、Console error/warning 数及 FoundationSceneValidator 结果未单独报告；源码静态审查 ACCEPT 不替代这些运行证据。

- 项目：`C:\Users\panye\My project`
- 分支：`feature/sc012-family-chronicle`
- 场景：`Assets/Scenes/FoundationScene.unity`
- 不要新建 Unity 项目或场景，不要操作 `SampleScene.unity`。
- 复审确认当前 `FoundationScene.unity` 没有 `FamilyChronicle` 对象；未运行 updater、保存并通过 Validator 前，Play Test UI 不可用，必须停在 Play Mode 之前。
- OpenCode2 报告：早期实现的 Unity 随附 Roslyn 对七个程序集静态类型检查 0 错误，三个独立逻辑 harness 为 22/22、17/17、14/14；第二轮修复后又报告七个程序集静态类型检查 0 错误。成人继承人交接修复及最后一轮 review 之后，worker 仅报告 brace/whitespace 静态检查；harness 未在后续修复后重跑。这些都不是 Unity 编译或 Unity Test Runner 证据。
- Unity 编译、Unity EditMode 执行、Console error/warning 数、SC-012 updater、FoundationSceneValidator、Play Mode 和用户验收目前均未取得。
- 复审范围为静态源码与测试源检查；无 Unity 编译、EditMode 执行或运行时验证。复审留下的边界：append 失败日志/关系刷新缺少应用层测试；v1 存档带非空 chronicle 时会忽略该额外字段，v2/v3 则会拒绝。
- 不使用截图或 Computer Use 操作 Unity。Play Test 结果用本文末尾的纯文本清单报告。

## 用户验收记录（2026-11-03）

- 用户报告：EditMode 通过；Play Test 通过；明确接受 SC-012。
- 未提供：EditMode Passed/Failed/Skipped 数量、Console error/warning 数、FoundationSceneValidator 报告内容。
- 只读复审：OpenCode2 第三轮静态复审 ACCEPT 源码/测试源；没有运行 Unity 或测试。
- 集成：checkpoint `4443ccc` 已快进至 `main` 并推送到 `origin/main`；标签 `demo-SC012` 已推送。feature refs 在 ancestry 核对后删除。

## 开始试玩前：在现有 Unity Editor 完成验证

1. 确认 Git 当前分支为 `feature/sc012-family-chronicle`，并确认 `C:\Users\panye\My project` 是正在打开的项目路径。只保留该项目的一个 Unity Editor；不要对被 Editor 占用的目录启动 batchmode。
2. 在该 Editor 打开 `Assets/Scenes/FoundationScene.unity`。等待编译结束；Console 有编译错误就停止，记录错误文本，不进入 Play Mode。
3. 打开 Test Runner，选择 EditMode，运行全量测试。至少确认 `HistoryLedgerTests`、`FamilyChronicleSaveTests`、`GameSessionChronicleTests`、`FamilyChroniclePanelTests`、`AddSC012FamilyChroniclePanelTests` 和现有全量套件。记录 Passed / Failed / Skipped 数量；失败时停止并报告，不继续试玩。
4. 在菜单运行 `Tools → Sengoku Chronicle → Add SC-012 Family Chronicle Panel`。预期操作成功且未报告对象缺失、重复或布局校验错误。若 updater 拒绝因场景有未保存更改，请先检查场景里是否有需要保留的更改；不要丢弃或覆盖它们。
5. 保存 `FoundationScene.unity`。确认 `SampleScene.unity` 没有被打开或保存。
6. 运行 `Tools → Sengoku Chronicle → Validate Foundation Scene`。检查 `Logs/SceneValidationReport.txt` 中 SC-012 PASS 行及最终 `RESULT: PASS`。记录结果和错误数；未通过时停止，不进入 Play Mode。
7. 记录 Editor Console 的 error 与 warning 数，再进入 `FoundationScene` 的 Play Mode。

## Play Test：一局内完成关系、任务、事件、继承和存读档

### A. 新局与空历史

1. 在标题界面点击 `新游戏`。
   预期：回到新局初始状态，日期显示 `1560-04-01`，当前位置为清洲。
2. 点击地图上的 `家族编年史`。
   预期：打开标题为 `家族编年史` 的只读面板，显示“尚无家族记录”的空状态；关闭后日期、地点和游戏状态不变。
3. 点击 `关闭`。

### B. 产生一条真实关系变化

1. 在地图选择目的地 `热田`，点击 `确认出发`；旅行完成后进入热田城镇菜单。
2. 打开黑田伊织的对话，选择 `A proposition.`，再选择 `Accept`。
   预期：对话结束；打开 `家族编年史` 后出现一条关系记录，包含黑田伊织的稳定 ID/名称及关系变化前后值（默认局应为 `0 → 10`）。关系行的月份与当前游戏日期一致。
3. 点击 `关闭`。记录此时日期与关系值，供后面核对。

### C. 记录补给短缺任务四次阶段变化

1. 在热田菜单点击 `返回地图`。选择目的地 `清洲`，点击 `确认出发`。
2. 在清洲任务面板点击 `领取任务`。
   预期：任务进入已接受阶段。打开编年史，应出现一条任务记录，阶段前后值不同。
3. 关闭面板，返回地图，选择 `热田` 并点击 `确认出发`。
4. 在热田任务面板点击 `调查补给`。
   预期：出现第二条补给任务阶段记录，顺序晚于领取记录。
5. 点击 `与黑田伊织交涉`。
   预期：出现第三条任务阶段记录，前后阶段值不同。若该动作同时改变关系，关系变化也应单独记录其前后值。
6. 返回地图，旅行到 `清洲`，点击 `报告并领取奖励`。
   预期：出现第四条任务阶段记录；任务显示已完成/奖励已领取。再次点击该按钮或在错误地点尝试报告不应产生重复任务记录或重复奖励；如果按钮不可用，不要通过隐藏调试入口测试。

### D. 记录事件选择及其关系效果

1. 点击 `结束月份`。
   预期：出现 SC-011 的 `雨夜来客` 事件选择界面。若本次没有出现，停止并记录日期、事件状态和可见提示。
2. 选择 `请他进屋歇息`。
   预期：事件选择成功并关闭事件界面。反馈确认选项及关系效果。打开编年史，应新增事件记录；若这次选项确实改变关系值，还应新增一条独立关系记录。按序号显示时，事件和关系变化顺序稳定，且都晚于此前任务记录。
3. 关闭编年史并记录事件后当前关系值。

### E. 记录家主交接和后代事件

1. 点击 `退休并安排继承`，选择成年继承人 `明智 正綱`，点击 `确认交接`。
   预期：家主切换为明智正綱。编年史新增继承记录，内容反映实际阶段变化；同月内序号排在此前记录之后。
2. 点击 `结束月份`，如遇其他实际弹出的事件，选择一个可用选项后再继续推进月份。
   预期：SC-011 的跨代后续事件 `旧客的雨夜回访` 出现，内容与先代选择一致；不要出现相反分支的文本。
3. 选择 `重申先代的门诺`。
   预期：后续事件成功结算，编年史增加该事件记录；如关系数值实际改变，也增加一条前后值明确的关系记录。
4. 点击 `家族编年史`。检查所有记录按年/月和追加顺序排列；关系行显示前后数值；任务与继承行显示阶段变化。用滚轮向上、向下检查长列表，最下面应能看到最新记录。关闭再打开一次，行数和内容不应重复。

### F. 保存、读档并比较

1. 在稳定状态点击 `存档`。记录编年史行数、日期、当前家主、当前位置和各关系行的前后值。
   预期：状态文本显示 `已保存当前进度。`。
2. 点击 `读档`。
   预期：状态文本显示 `已载入存档。`；日期、家主、地点和编年史行数恢复到刚才记录的值。
3. 再次打开编年史，逐行核对类别、标题、摘要、关系前后值和顺序。
   预期：保存前后的记录完全一致，没有因读档而新增、丢失、改写或重复。关闭/打开面板也不新增记录。

## 停止条件

- 编译错误、EditMode 失败、updater 失败或 Validator 非 PASS：停止，不进入 Play Mode。
- 任一步出现不符合预期的日期、家主、关系数值、任务阶段、事件、记录顺序或存读档结果：停止后续步骤，记录具体步骤和可见文本。
- 不要修改场景 YAML、`ProjectSettings`、存档文件或隐藏调试状态来绕过失败；不要为报告问题拍 Unity 截图。

## 纯文本结果清单

```text
项目路径：
分支：
场景：
Unity 编译：通过 / 失败（错误文本：）
EditMode：Passed __ / Failed __ / Skipped __
Console：Errors __ / Warnings __
Updater：成功 / 失败（消息：）
FoundationSceneValidator：PASS / FAIL（报告末行及错误数：）
Play Test A 新局空历史：通过 / 失败
Play Test B 关系变化：通过 / 失败（实际值 __ → __；关系记录行：）
Play Test C 任务四阶段：通过 / 失败（记录数与顺序：）
Play Test D 事件及关系：通过 / 失败（选择、关系值、记录行：）
Play Test E 继承及后续事件：通过 / 失败（可见文本、记录顺序：）
Play Test F 保存/读档：通过 / 失败（加载前后记录数：__ / __）
最终判断：通过 / 未通过
附加可见错误或提示：
```

通过并由用户明确确认后，协调方才会按项目规则创建 checkpoint、推送分支、合并 `main`、标记并推送 `demo-SC012`；在此之前不执行这些操作。


---

<a id="sc013-demo"></a>
## 来源：SC013_DEMO.md

# SC-013 M2 固定三代剧本 — 试玩脚本

更新时间：2026-11-03

本文件是 SC-013 的纯文本人工试玩脚本。它覆盖场景生成、Unity 验证准备、逐步 Play Test 操作、预期结果、失败判据和结果清单。不要使用截图或 Computer Use 操作 Unity；结果用文末清单报告。

## 0. 当前状态与证据边界

- 代码、事件/文本内容、测试源和文档已实现；八个程序集已用 Unity 随附 Roslyn 做静态类型检查，0 错误；两个纯逻辑 harness 分别通过 30 项和 33 项检查。
- **这些不是 Unity Editor 编译、Unity Test Runner、FoundationSceneValidator、Play Mode 或用户验收证据。**
- `FoundationScene.unity` 尚未运行 SC-013 updater，尚未保存，也未运行 validator。原因是实现期间现有 Unity Editor 正打开该项目，项目规则禁止对其目录启动 batchmode，也没有可用的 Unity bridge/MCP。因此**先完成第 1 节，再进入 Play Mode**。
- SC-011 的事件 ID、条件、效果、选项可用性与结果保持不变；SC-013 事件仅以 `scenario: "sc013"` 在 M2 固定剧本中生效。
- 不新增终局存档字段；结局由两次已落定继承和已提交终局事件历史派生。
- 实现、文件清单与证据边界见 `docs/reports/sc013-implementation.md`。

## 1. 试玩前：在现有 Unity Editor 生成并验证场景

1. 确认当前分支为 `feature/sc013-fixed-three-generation`，项目路径为 `C:\Users\panye\My project`。只保留这一个项目 Editor；不要对该目录启动 batchmode，也不要新建项目或场景。
2. 在 Editor 打开 `Assets/Scenes/FoundationScene.unity`，等待编译结束。若 Console 有编译错误，停止并记录完整错误文本；不要进入 Play Mode。随后保存 `FoundationScene.unity`，或确认场景没有未保存修改：两个 updater 在场景为 dirty 时会拒绝执行。
3. 运行菜单 `Tools/Sengoku Chronicle/Add SC-013 Family Roster Panel`。
   - 预期 Console：`SC-013 family roster and ending panel validated; FoundationScene saved.`
   - 预期场景：`Canvas/FamilyRosterRoot` 下出现 `FamilyRosterOpenButton`、隐藏的 `FamilyRosterPanel`、隐藏的 `M2EndingPanel`。
4. 运行菜单 `Tools/Sengoku Chronicle/Add SC-013 Title Scenario Buttons`。
   - 预期 Console：`SC-013 title scenario buttons validated; FoundationScene saved.`
   - 预期 `Canvas/M6TitleRoot` 下新增 `SC013StandardSampleButton` 与 `SC013CrisisDemoButton`，状态文字下移。
5. 运行菜单 `Tools/Sengoku Chronicle/Validate Foundation Scene`，打开 `Logs/SceneValidationReport.txt`。
   - 必须出现：`PASS SC-013 family roster and ending panel hierarchy, bindings, ordering, and layout.`
   - 必须出现：`PASS SC-013 title scenario entries, bindings, and layout.`
   - 检查 `TITLE SCREEN CONTROLLER REFERENCES` 中 `standardSampleButton` 与 `crisisDemoButton` 均为 BOUND。
   - 当前场景存在一个**既有的 SC-005 反馈条位置错误**（`Feedback text must be a top-anchored banner clear of the map HUD controls.`）。它不属于 SC-013；若仅剩该既有错误，记录为“既有错误，SC-013 两项 PASS”，不要声称整体 Validator PASS。
6. 保存 `FoundationScene.unity`。确认 `SampleScene.unity` 没有被打开或保存。

## 2. 逐步 Play Test

每一步都在 `FoundationScene` 的 Play Mode 中按顺序执行。除非特别说明，不要用 Console 或代码绕过 UI。

### 2.1 标题与 M2 新游戏

1. 点击 Play。
   - 预期：标题覆盖层出现，包含“新游戏”“继续”“SC-010 标准样例”“SC-010 危机示范”和状态提示。
2. 点击“新游戏”。
   - 预期：标题隐藏；日期为 `1560-04-01`；位置为清洲；地图与 HUD 可见。
   - 预期：HUD 显示“当代家主：明智 晴信（70岁）”，状态行显示“固定继承人：明智 正綱（46岁）”，并提示先处理“继承前的家议”。

### 2.2 六人家族名册

3. 点击右上角“家族名册”按钮，面板直接打开（生成器只创建一个打开按钮）。
   - 预期：六行，顺序为：
     1. 第一代 · 家主 · 明智 晴信（70岁）【家主】
     2. 第一代 · 配偶 · 明智 千恵（66岁）
     3. 第二代 · 直系亲族 · 明智 正綱（46岁）【指定继承人】
     4. 第三代 · 直系亲族 · 明智 信正（22岁）
     5. 第一代 · 旁系亲族 · 明智 時姫（44岁）
     6. 家臣 · 佐近（40岁）
   - 预期：亲属行显示晴信的配偶/子女/兄弟姐妹、正綱的父母、信正的父亲、時姫的兄弟姐妹；佐近为“无亲属记录”。
   - 预期：底部显示“固定继承人：明智 正綱（46岁）”。
4. 点击“关闭”。名册不改变任何游戏状态。

### 2.3 第一代代价选择与雨夜来客

5. 点击“结束月份”。
   - 预期：日期变为 `1560-05-01`；事件面板显示“继承前的家议”。
6. 在事件面板保存一次（使用现有 M5 存档按钮/面板）。
   - 预期：保存成功；此时不要先做选择。
7. 确认“守住对伊织的承诺”和“收回承诺，先保家族”两个选项都可用。
   - 预期：两项均无“不可用”提示；前者预览“信任增加五点”，后者预览“信任下降五点”。
8. 选择“收回承诺，先保家族”。
   - 预期：事件面板关闭；反馈说明承诺被收回，信任下降五点。
   - 预期：对黑田伊织的关系从 `0` 变为 `-5`。
   - 预期：编年史新增一条“事件 继承前的家议”和一条“关系 0 → -5”；只各一条。
9. 点击“结束月份”。
   - 预期：日期变为 `1560-06-01`；事件面板显示 SC-011 的“雨夜来客”。
   - 预期：此时“退休并安排继承”仍不可用，状态说明第一代尚未回应雨夜来客。
10. 选择“请他进屋歇息”。
    - 预期：关系从 `-5` 变为 `0`；编年史新增“事件 雨夜来客”和“关系 -5 → 0”。
    - 预期：HUD 状态变为“主线条件已满足”；“退休并安排继承”可用。

### 2.4 第一次合法交接

11. 点击“退休并安排继承”。
    - 预期：家督立即交接给“明智 正綱”；不再要求从旁系候选人中选择。
    - 预期：HUD 显示“当代家主：明智 正綱（46岁）”与“固定继承人：明智 信正（22岁）”。
    - 预期：编年史新增一条继承记录，前/后家主为晴信 → 正綱；继承次数为 1。
    - 预期：关系仍为 `0`，已提交事件历史不重置。

### 2.5 第二代旧事与第二次交接

12. 点击“结束月份”。
    - 预期：日期变为 `1560-07-01`；事件面板显示“旧客的雨夜回访”（因为第一代选择了“请他进屋歇息”）。
    - 预期：在解决该事件前，“退休并安排继承”不可用。
13. 选择“重申先代的门诺”。
    - 预期：关系从 `0` 变为 `5`；编年史新增“事件 旧客的雨夜回访”和“关系 0 → 5”。
    - 预期：HUD 显示固定继承人信正，退休可用。
14. 点击“退休并安排继承”。
    - 预期：家督交接给“明智 信正”；继承次数为 2；固定继承人显示“无（当前已是最后一代）”。
    - 预期：状态行同时提示第三代尚未面对终局；此时仍不能再次交接。
    - 预期：编年史新增正綱 → 信正的继承记录；关系仍为 `5`。

### 2.6 第三代终局、两个结局与稳定点存读档

15. 点击“结束月份”。
    - 预期：日期变为 `1560-08-01`；事件面板显示“第三代的家门”。
    - 预期：在终局解决前，“退休并安排继承”不可用，并说明第三代尚未面对终局。
16. 在终局事件面板保存一次。
    - 预期：保存成功；待处理事件仍为“第三代的家门”。
17. 确认“请伊织再度援手”可用（信任为 `5`），“不欠旧交，独自承担”始终可用。
18. 选择“请伊织再度援手”。
    - 预期：事件面板关闭；出现“第三代结局 · 旧交来援”面板。
    - 预期：结局面板正文提到伊织应邀而来，并引用先代毁诺、第二代续接旧交、信任为 5。
    - 预期：编年史新增“事件 第三代的家门”，只一条。
19. 点击“继续游玩”。
    - 预期：结局面板关闭；可以继续“推进一天”或“结束月份”；游戏不结束、不重置。
20. 读取第 16 步的存档。
    - 预期：待处理事件恢复为“第三代的家门”；家督为信正；继承次数为 2；关系为 `5`；六人名册与亲属关系不变；编年史条数与读档前一致，没有重复记录。
21. 选择“不欠旧交，独自承担”。
    - 预期：出现“第三代结局 · 独自承担”面板；正文与“旧交来援”明显不同，提到独自承担旧事。
    - 预期：关闭后仍可继续游玩。
22. 打开“家族编年史”，核对：
    - 两次继承记录：晴信 → 正綱、正綱 → 信正。
    - 四次事件选择：继承前的家议、雨夜来客、旧客的雨夜回访、第三代的家门（共四条事件记录）。
    - 关系变化：`0 → -5`、`-5 → 0`、`0 → 5`（各一条）。
    - 没有重复的事件、关系或继承记录。

### 2.7 独立入口回归

23. 结束当前 Play Mode，再重新点击 Play，回到标题界面。
24. 点击“SC-010 标准样例”。
    - 预期：进入 SC-010 标准家族；HUD 不再显示 SC-013 固定继承人；六人名册入口仍可打开但显示标准家族成员（不是 SC-013 的六人固定名册）。
25. 回到标题界面，点击“SC-010 危机示范”。
    - 预期：进入 SC-010 危机家族（晴信、千恵、景祐、佐近），没有任何 SC-013 名册或 M2 事件残留。
26. 确认“新游戏”仍进入 SC-013 M2；标题界面三个入口互不污染。

## 3. 预期结果汇总

| 项目 | 预期 |
|---|---|
| 新游戏默认 | SC-013 M2 固定剧本，`1560-04-01` |
| 家族名册 | 六人、身份、年龄 70/66/46/22/44/40、三代标签、亲属描述、固定继承人正綱 |
| 第一代代价 | “收回承诺”使黑田伊织关系 `0 → -5`；两项始终可选 |
| SC-011 回归 | “雨夜来客”仍在第一代后续月份出现，选项行为不变 |
| 第一次交接 | 晴信 → 正綱，继承次数 1，固定继承人变为信正 |
| 第二代旧事 | 旧客回访按 SC-011 分支出现；“重申门诺”使关系 `0 → 5` |
| 第二次交接 | 正綱 → 信正，继承次数 2 |
| 终局门槛 | 两次交接后下月出现；`seek-aid` 在关系 `>= 5` 可用，在 `4` 不可用 |
| 两个结局 | “旧交来援”与“独自承担”标题/正文不同；关闭后仍可继续 |
| 编年史 | 四类真实变化各记录一次；无重复 |
| 存读档 | 稳定点恢复待处理事件、家督、继承人、关系、历史、名册和编年史；无重复奖励/记录 |
| SC-010 入口 | 标准样例与危机示范独立可用，不残留 M2 数据 |

## 4. 失败判据

出现以下任一情况即记为失败，记录步骤号、屏幕文本和 Console 文本：

- 标题界面缺少“新游戏”“SC-010 标准样例”“SC-010 危机示范”中的任一项；或“新游戏”没有进入 SC-013 M2。
- 名册不是六人、姓名/身份/年龄/代际/亲属/固定继承人与上表不符；或打开名册改变游戏状态。
- 第一代未完成“继承前的家议”和“雨夜来客”就能交接；或完成后仍不能交接。
- 交接给正綱以外的旁系；或第二次交接不到信正；或交接时丢弃待处理事件。
- “收回承诺”不是 `0 → -5`，或“请他进屋歇息”不是 `-5 → 0`，或“重申门诺”不是 `0 → 5`；或同一变化记录两次。
- 终局在第二次交接前出现；或关系为 `4` 时 `seek-aid` 可用；或两个结局标题/正文相同。
- 编年史缺少任一事件/关系/继承记录，或读档后新增重复记录。
- 读档后待处理事件、家督、继承次数、关系、名册或编年史与存档不一致。
- 结局后不能继续推进时间/月份，或游戏被强制结束/重置。
- Console 出现红色错误；或 `Logs/SceneValidationReport.txt` 缺少两条 SC-013 PASS；或 `SampleScene.unity` 被保存/修改。
- 任何步骤只能靠 Console、调试代码或手改场景 YAML 才能继续。

## 5. 结果清单（纯文本报告）

```text
Project / branch:
Unity version:
Scene:
SC-013 updater A (Family Roster):
SC-013 updater B (Title Scenario Buttons):
Unity 编译：通过 / 失败（错误文本：）
EditMode：Passed __ / Failed __ / Skipped __（若未运行写“未运行”）
Console error 数：
Console warning 数：
FoundationSceneValidator：PASS / FAIL / 未运行
  SC-013 roster PASS：是 / 否
  SC-013 title PASS：是 / 否
  既有 SC-005 反馈条错误：存在 / 不存在 / 未检查
Play Mode 步骤 1–26：逐项通过 / 失败（失败步骤与文本：）
存读档稳定点：通过 / 失败（差异：）
最终结局比较：通过 / 失败（差异：）
用户可理解性/情感反馈：
其他备注：
```

## 6. 证据边界

- 本文件中的“预期”是实现约定，不是已取得的运行证据。
- 只有用户实际完成 Play Mode 并给出文本结果后，才能把 SC-013 标记为试玩验收。
- 用户验收前不得创建 SC-013 checkpoint、合并 `main`、打标签或推送。
- 不要修改场景 YAML、`ProjectSettings`、存档文件或隐藏调试状态来绕过失败；不要为报告问题拍 Unity 截图。


---

<a id="sc014-demo"></a>
## 来源：SC014_DEMO.md

# SC-014 流亡少年与家系遗产 — 交付说明与试玩手册

更新时间：2026-11-03

本文件是 SC-014 的纯文本交付说明和后续人工试玩手册。它记录实现状态、证据边界，以及用户之后可在现有 Unity Editor 中执行的步骤；**不把任何项标记为已测试、已通过或已验收**。

## 0. 当前状态与证据边界

- 已实现：在 SC-013 固定 M2 剧本中追加第二条跨代事件链 `chain.sc013.refugee-legacy`，包含四个事件节点和完整中文文本。
- 实现文件：`Assets/Resources/Events/event-catalog-v1.json`、`Assets/Resources/Localization/event-text-zh-v1.json`、`PROJECT_STATE.md`、`ARCHITECTURE.md`、`DECISIONS.md`、`SC014_DEMO.md`。
- SC-014 规划/审阅记录（非实现改动）：设计稿 `docs/superpowers/specs/2026-11-03-sc-014-refugee-legacy-design.md`、实现计划 `docs/superpowers/plans/2026-11-03-sc-014-refugee-legacy.md`、首轮和修正后独立只读复审报告 `docs/reports/sc014-review.md` 与 `docs/reports/sc014-review-2.md`。
- 未修改：Unity 场景、预制体、`ProjectSettings`、存档 schema、package、测试文件、`Library`、`Temp`。
- 未运行：Unity Editor、Unity 编译、EditMode 测试、Console 检查、`FoundationSceneValidator`、Play Mode、存读档试玩。
- 未执行：新增测试、提交、推送、合并、打标签、用户验收。
- 实现侧只做了一次性 JSON 结构/引用检查（唯一 ID、文本引用、链成员、源选项、分支互斥、关系目标），结果为 0 项结构性错误。**这不是测试运行，也不替代 Unity 证据。**
- 独立审阅：首轮 OpenCode2 只读审阅 ACCEPT，列出的六项非阻断问题已修复；复审报告 `docs/reports/sc014-review-2.md` 再次 ACCEPT。SC-011 与 `HEAD` 完全相等；SC-013 未提交，无法逐字节对比 `HEAD`。这两轮审阅都不是 Unity 运行或用户验收证据。

## 1. 实现内容

### 1.1 链与节点

| 世代 | 事件 ID | 前置条件 | 选项与关系效果 |
|---|---|---|---|
| 第一代 | `event.sc013.refugee-arrival` | `scenario: "sc013"`；`SuccessionCountEquals 0`（首次交接前） | 收留 `option.sc013.refugee-arrival.shelter`：`+5`，开启后代；拒绝 `...refuse`：`-5`，终止；出卖 `...betray`：`-10`，终止。 |
| 第二代 | `event.sc013.refugee-loyalty` | 第一代选择收留；`SettledSuccessionCountAtLeast 1` | 尊重旧约 `...honor-old-oath`：`+5`，通往盟友节点；服从新政 `...enforce-new-policy`：`-5`，通往竞争节点。 |
| 第三代（盟友） | `event.sc013.refugee-aid` | 第二代选择尊重旧约；`SettledSuccessionCountAtLeast 2` | 请求援助 `...request-aid`：需要关系 `>= 5`，无额外关系变化；独自承担 `...stand-alone`：始终可用，无额外关系变化。 |
| 第三代（竞争） | `event.sc013.refugee-rival` | 第二代选择服从新政；`SettledSuccessionCountAtLeast 2` | 修补旧怨 `...mend-grievance`：`+5`；维持距离 `...keep-distance`：`-5`。 |

所有关系效果的目标都是既有 NPC `npc.kuroda.iori`。文本中的关系值明确指“伊织家系与明智家的信任”，不声明模拟未建模的个人关系。未新增人物、关系模型、存档字段、事件规则类型或条件类型。

### 1.2 调度兼容

- 新增事件优先级分别为 `5`、`50`、`500`，低于同阶段既有的 SC-013 承诺/终局事件和 SC-011 旧客回访事件。
- 因此既有确定性月度排程不被抢占；现有 SC-011/SC-013 事件顺序和测试预期保持不变。
- **重要限制：** 现有 M2 退休门禁不要求 SC-014 节点。若要走完整链，玩家必须在每次交接前**额外结束一个月**，直到 SC-014 事件出现，再执行交接。若直接按原 SC-013 试玩脚本交接，本链会被跳过。
- 这是数据-only 实现的结果；若要让 SC-014 成为强制主线，需要后续代码改动退休门禁，超出本任务文件所有权。

## 2. 试玩前须知

- 本文件是交给用户之后执行的文本手册；当前没有 Play Test 或 Unity 证据。
- 请使用现有 Unity Editor 和 `FoundationScene`，按项目规则由用户本人执行 Play Test；不要使用截图或 Computer Use 代替试玩。
- 本任务未修改场景，因此不需要为 SC-014 运行新的场景 updater；仍建议先完成 `SC013_DEMO.md` 第 1 节的场景接入与验证，确认既有 SC-013 流程可玩。
- 现有 SC-013/SC-011 事件文本、ID、条件、效果和选项可用性应保持不变；SC-014 只是新增事件行和文本行。
- 第一代事件中，成年黑田伊织作为现有家臣出场，替自家家系的一名受伤流亡少年求情。少年不是伊织本人，本链不是回忆/闪回，也不改写 SC-011 已验收的成年伊织故事；关系数值仍指“伊织家系与明智家的信任”。

## 3. 推荐完整链 A：收留 → 尊重旧约 → 盟友

以下步骤假设从新局开始，并且**不**在 SC-014 事件出现前交接。括号内为示例关系变化，实际值取决于此前选择。

1. 新游戏，点击“结束月份”。
   - 预期：事件面板显示“继承前的家议”。选择“守住对伊织的承诺”（`+5`）或“收回承诺，先保家族”（`-5`）。
2. 点击“结束月份”。
   - 预期：事件面板显示“雨夜来客”。选择“请他进屋歇息”（`+5`）或“请他明晨再来”（无关系变化）。
3. 再点击“结束月份”。
   - 预期：事件面板显示 SC-014 的“门前的流亡少年”。成年黑田伊织带着一名受伤的家族少年站在门外，替少年求情。
   - 确认三个选项都可用：“收留少年”“拒绝收留”“交给追兵”。
   - 选择“收留少年”。
   - 预期：关系增加五点；反馈/摘要提到“伊织家系与明智家的信任增加五点”。
4. 现在点击“退休并安排继承”。
   - 预期：家督交接给“明智 正綱”；此前不要跳过第 3 步，否则本链不会启动。
5. 点击“结束月份”。
   - 预期：显示 SC-011 的“旧客的雨夜回访”或“门房里的旧雨”（取决于第 2 步选择）。完成该事件。
6. 再点击“结束月份”。
   - 预期：事件面板显示 SC-014 的“旧臣的忠诚”。
   - 选择“尊重旧臣的旧约”。
   - 预期：关系增加五点；反馈提到旧臣记住了新家主的体面。
7. 点击“退休并安排继承”。
   - 预期：家督交接给“明智 信正”；继承次数为 2。
8. 点击“结束月份”。
   - 预期：显示 SC-013 终局“第三代的家门”。完成终局选择。
9. 关闭结局面板，再点击“结束月份”。
   - 预期：事件面板显示 SC-014 的“流亡家系的援助”。
   - 确认“请伊织家系出手”是否可用：关系 `>= 5` 时应可用；关系低于 `5` 时应禁用并显示信任不足的理由。
   - 选择“请伊织家系出手”或“谢绝旧交，独自承担”。
10. 打开“家族编年史”，核对新事件记录与关系变化各只出现一次，没有重复结算。
11. 可选存读档检查：在任一 SC-014 待处理事件面板保存，再读取该存档。
    - 预期：待处理事件、已提交历史与关系值恢复一致，编年史不重复。
    - 本轮未执行此检查；若未执行，在结果清单写“未检查”，不要记为通过。

## 4. 完整链 B：收留 → 服从新政 → 竞争

1. 按第 3 节步骤 1–4 完成第一代并交接给正綱。
2. 完成第二代 SC-011 旧客回访事件。
3. 再点击“结束月份”，显示 SC-014 的“旧臣的忠诚”。
   - 选择“以新政策压下旧约”。
   - 预期：关系下降五点；反馈提到旧约被新政压下，怨望留到下一代。
4. 点击“退休并安排继承”，交接给信正。
5. 点击“结束月份”，完成 SC-013 终局“第三代的家门”。
6. 关闭结局面板，再点击“结束月份”。
   - 预期：事件面板显示 SC-014 的“流亡家系的竞争”，**不**显示“流亡家系的援助”。
   - 选择“修补两家的旧怨”（`+5`）或“维持距离，各走各路”（`-5`）。
7. 打开“家族编年史”，核对竞争节点的关系变化只记录一次。

## 5. 终止分支：拒绝或出卖

1. 按第 3 节步骤 1–2 完成“继承前的家议”和“雨夜来客”。
2. 再点击“结束月份”，显示“门前的流亡少年”。
   - 选择“拒绝收留”（`-5`）或“交给追兵”（`-10`）。
3. 交接给正綱；继续结束月份并完成 SC-011 回访和终局。
   - 预期：不会出现“旧臣的忠诚”“流亡家系的援助”或“流亡家系的竞争”。
   - 预期：编年史仍记录第一代的选择及一次关系变化，但本链不再延续。

## 6. 预期结果汇总

| 项目 | 预期 |
|---|---|
| 第一代入口 | 在首次交接前的额外月份出现，标题“门前的流亡少年”；成年伊织替家族少年求情；三个选项均可用 |
| 收留 | 关系 `+5`；后代节点可继续 |
| 拒绝 | 关系 `-5`；本链终止 |
| 出卖 | 关系 `-10`；本链终止 |
| 第二代 | 仅在收留且至少一次交接后出现；两个选项均可用，分别为 `+5` / `-5` |
| 第三代盟友 | 仅在第二代尊重旧约且两次交接后出现；“请求援助”在关系 `>= 5` 可用，低于 `5` 禁用并给出理由 |
| 第三代竞争 | 仅在第二代服从新政且两次交接后出现；两个选项分别 `+5` / `-5` |
| 互斥 | 同一局中不会同时出现盟友与竞争节点 |
| 编年史 | 每个已提交事件和每次实际关系变化各记录一次；无重复 |
| 既有内容 | SC-011/SC-013 事件文本、ID、条件、效果、选项可用性不因本链改变 |

## 7. 失败判据

出现以下任一情况即记为失败，并记录步骤号、屏幕文本和 Console 文本：

- 首次交接前的额外月份没有出现“门前的流亡少年”。
- 事件的正文把少年写成伊织本人、改成回忆/闪回，或改写 SC-011 已验收的成年伊织故事。
- 未选择“收留”却出现“旧臣的忠诚”；或未完成第一次交接就出现第二代节点。
- 未完成第二次交接就出现第三代节点；或盟友与竞争节点同时出现。
- 关系为 `4` 或更低时“请伊织家系出手”仍可用；或关系 `>= 5` 时不可用。
- 收留不是 `+5`、拒绝不是 `-5`、出卖不是 `-10`；或第二代/第三代节点的关系变化与上表不符。
- 拒绝/出卖后仍出现后续节点；或同一事件/关系变化被记录两次。
- 既有 SC-011/SC-013 事件文本、顺序或选项行为发生变化。
- Console 出现红色错误；或按原 SC-013 流程直接交接时错误地出现 SC-014 事件。
- 需要靠 Console、调试代码或手改 JSON/场景 YAML 才能继续。

## 8. 结果清单（纯文本报告）

```text
Project / branch:
Unity version:
Scene:
Unity 编译：通过 / 失败（错误文本：）
EditMode：Passed __ / Failed __ / Skipped __（若未运行写“未运行”）
Console error 数：
Console warning 数：
FoundationSceneValidator：PASS / FAIL / 未运行
SC-014 第一代入口出现月份：
SC-014 第一代选择：收留 / 拒绝 / 出卖
SC-014 第二代选择：尊重旧约 / 服从新政 / 未出现
SC-014 第三代节点：援助 / 竞争 / 未出现
SC-014 关系数值变化：__
编年史重复检查：通过 / 失败（差异：）
SC-014 待处理事件存读档（可选）：通过 / 失败 / 未检查（差异：）
既有 SC-011/SC-013 回归：通过 / 失败（差异：）
Play Mode 步骤 1–11：逐项通过 / 失败（失败步骤与文本：）
用户可理解性/情感反馈：
其他备注：
```

## 9. 验证限制与后续

- 本文件中的所有“预期”都是实现约定，不是已取得的运行证据。
- 只有用户实际完成 Play Mode 并给出文本结果后，才能把 SC-014 标记为试玩验收；验收前不得创建 SC-014 checkpoint、合并 `main`、打标签或推送。
- 当前实现没有让 SC-014 成为强制主线；若产品上要求“必经”，需要另立代码任务修改 `M2FixedScenario` 的退休门禁，并补充相应测试。该改动不在本任务允许的文件范围内。


---

<a id="sc015-demo"></a>
## 来源：SC015_DEMO.md

# SC-015 三代家书 — 交付说明与纯文本试玩手册

更新时间：2026-11-03

本文件是 SC-015 的交付说明、试玩手册与当前运行证据索引。A/B/C/D 四条路线均已在 Play Mode 到达预期第三代内容；运行证据记录在 `docs/reports/sc015-playtest.md`。用户验收仍待确认。

## 0. 当前状态与证据边界

- 已实现：在 SC-013 固定 M2 剧本中追加第三条跨代事件链 `chain.sc013.family-letters`，包含七个事件节点、十个选项和完整中文文本。
- 实现文件：`Assets/Resources/Events/event-catalog-v1.json`、`Assets/Resources/Localization/event-text-zh-v1.json`、`PROJECT_STATE.md`、`ARCHITECTURE.md`、`DECISIONS.md`、`SC015_DEMO.md`，以及实现记录 `docs/reports/sc015-implementation.md`。
- 规划文件（非实现改动）：设计稿 `docs/superpowers/specs/2026-11-03-sc-015-three-generation-family-letters-design.md`、实现计划 `docs/superpowers/plans/2026-11-03-sc-015-three-generation-family-letters.md`。
- 未修改：Unity 场景、预制体、`ProjectSettings`、存档 schema、package、测试文件、代码、`Library`、`Temp`；未改写既有 SC-011、SC-013、SC-014 事件或文本。
- 已运行：现有 Unity 6.6 Editor（`C:\Users\panye\My project`，`FoundationScene`）中的 Play Mode；经用户本次临时授权使用 Computer Use。A/B/C/D 四条路线均到达预期第三代家书事件；路线 D 的首次操作未复现，逐步重测后通过。
- 本次未运行：EditMode、`FoundationSceneValidator`、存读档验证；未保存测试进度、提交、推送、合并、打标签，也未取得用户验收。Play Mode 停止后 Console 计数为 0 errors、0 warnings、0 logs。未单独取得编译日志，因此不单独声明编译门通过。
- 实现侧只做了一次性静态 JSON 检查（解析、唯一 ID、文本引用、链成员、源选项、分支互斥、世代门槛、空效果、优先级关系），结果为 10 组检查、0 错误。**这不是测试运行，也不替代 Unity 证据。**
- 两轮独立 OpenCode2 只读审阅均为 `ACCEPT`；结论和静态证据分别见 `docs/reports/sc015-review.md` 与 `docs/reports/sc015-review-2.md`。这不代表 Unity 运行验证或用户 Play Test 已通过。

## 1. 实现内容

### 1.1 链与节点

| 世代 | 事件 ID | 前置条件 | 选项与后续 |
|---|---|---|---|
| 第一代 | `event.sc013.family-letters.first` | `scenario: "sc013"`；`SuccessionCountEquals 0`（首次交接前） | 完整公开 `...first.reveal`；暂封家书 `...first.seal`。两个选项都无数值效果。 |
| 第二代（公开路径） | `event.sc013.family-letters.second-open` | 第一代选择公开；`SettledSuccessionCountAtLeast 1` | 保留原始人名与细节 `...second-open.preserve`；整理为简要家史 `...second-open.summarize`。 |
| 第二代（封存路径） | `event.sc013.family-letters.second-sealed` | 第一代选择封存；`SettledSuccessionCountAtLeast 1` | 由正綱拆阅并完整记录 `...second-sealed.open`；继续封存交给后代 `...second-sealed.keep`。 |
| 第三代（完整公开） | `event.sc013.family-letters.third-full-public` | 公开后保留原貌；`SettledSuccessionCountAtLeast 2` | 唯一选项 `...third-full-public.receive`；展示完整记名的家史。 |
| 第三代（简要传述） | `event.sc013.family-letters.third-short-history` | 公开后整理简史；`SettledSuccessionCountAtLeast 2` | 唯一选项 `...third-short-history.receive`；展示部分人名被略去的简史。 |
| 第三代（第二代拆阅） | `event.sc013.family-letters.third-opened-by-heir` | 封存后由正綱拆阅；`SettledSuccessionCountAtLeast 2` | 唯一选项 `...third-opened-by-heir.receive`；展示正綱抄录后传下的完整旧事。 |
| 第三代（继续封存） | `event.sc013.family-letters.third-sealed` | 封存后继续封存；`SettledSuccessionCountAtLeast 2` | 唯一选项 `...third-sealed.receive`；展示仍未被读过的木匣。 |

所有新增选项只使用 `Always` 要求，`effects` 均为空。家书是千恵记录家族经历的原创内容，不新增 NPC、历史事实、关系模型、任务、家族数值或存档 schema 字段。提交本链选项时，选择、事件历史、一次性标记与已提交结果使用既有 `eventRuntime` 存档字段；不新增字段，也不改变关系、任务或家族数值。晴信、千恵、正綱、信正均沿用 SC-013 固定剧本已有的人物关系。

### 1.2 调度与优先级

- `EventEngine` 按 priority **降序**排程：**数值越大，越早出现**。这是现有 SC-013 必须事件使用 `1000`、SC-014 使用 `5/50/500` 的原因。
- 本链优先级为 `6`、`60`、`600`：
  - 高于同世代 SC-014 的 `5`、`50`、`500`，因此两者都满足时 SC-015 先出现；
  - 低于 SC-013 必须事件的 `1000`（第一代「继承前的家议」、第三代「第三代的家门」）；
  - 低于第一/二代 SC-011 旧客回访的 `10`、`100`，因此既有 SC-011 事件顺序不变。
- 每月只排一个最高优先级事件。完整链需要玩家在每次交接前**额外结束月份**，依次让本链节点出现；M2 退休门禁不要求本链，若按最短 SC-013 流程直接交接，本链会被跳过。这是数据-only 实现的预期限制。

## 2. 试玩前须知

- 本文件包含四条分支步骤；已执行结果见 `docs/reports/sc015-playtest.md`。如需回归，可按各路线从新游戏重跑。
- 使用项目现有 Unity Editor 与 `FoundationScene`。本次由助手在用户临时授权下使用 Computer Use 试玩；后续复测仍按用户当次授权执行。
- 本任务未新增 UI 或场景内容，因此不需要为 SC-015 运行场景 updater。若 SC-013 的家族名册/结局面板尚未接入场景，请先完成 `SC013_DEMO.md` 第 1 节的接入与验证。
- 四条分支互斥；每条分支都需要从标题界面重新「新游戏」，不要在一条分支中试图看到全部四种结尾。
- 下列步骤中的“结束月份”均指游戏内月末推进按钮，不是现实月份。
- 文中“预期”是设计约定，不是已取得的运行结果。

## 3. 分支 A：公开 → 保留原貌 → 完整记名

1. 标题界面选择「新游戏」。预期进入 SC-013 固定 M2 剧本，第一代家主为晴信。
2. 点击「结束月份」。
   - 预期：事件面板显示「继承前的家议」。选择任一选项（本链不受该选择影响）。
3. 再点击「结束月份」。
   - 预期：显示 SC-011「雨夜来客」。选择任一选项。
4. 再点击「结束月份」。
   - 预期：显示 SC-015「千恵的家书」，正文说明晴信与千恵在灯下整理家书，需在交接前决定公开或封存。
   - 选择「完整公开家书」。
   - 预期：摘要显示「家书留在案上」，说明晴信决定完整公开，千恵把原稿留给正綱；**没有关系或家族数值变化**。
5. 点击「退休并安排继承」。
   - 预期：家督交接给「明智 正綱」。
6. 点击「结束月份」。
   - 预期：显示 SC-011「旧客的雨夜回访」或「门房里的旧雨」（取决于第 3 步选择）。完成该事件。
7. 再点击「结束月份」。
   - 预期：显示 SC-015「已公开的家书」，标题与正文明确正綱读到千恵原稿。
   - 选择「保留原始人名与细节」。
   - 预期：摘要显示「原稿完整留存」；无数值变化。
8. 点击「退休并安排继承」。
   - 预期：家督交接给「明智 信正」，继承次数为 2。
9. 点击「结束月份」。
   - 预期：显示 SC-013 终局「第三代的家门」。完成终局选择。
10. 在 M2 结局面板中完成「第三代的家门」终局选择后，先点击该面板上的「继续游玩」按钮关闭面板；再点击「结束月份」。
    - 预期：显示 SC-015「完整记名的家书」，标题与正文明确信正收到晴信、千恵、正綱共同保存下来的完整家书。
    - 选择唯一选项「收下这份完整家史」。
    - 预期：摘要显示「三代人名都在纸上」；无数值变化。
11. 打开「家族编年史」，核对「千恵的家书」「已公开的家书」「完整记名的家书」各只出现一次，且没有出现其他三个第三代结尾。

## 4. 分支 B：公开 → 整理简史 → 简要传述

1. 按第 3 节步骤 1–6 完成第一代并交接给正綱；第 4 步选择「完整公开家书」。
2. 再点击「结束月份」。在出现「已公开的家书」时，选择「整理为简要家史」。
   - 预期：摘要显示「家史变得简明」，说明正綱略去部分人名与枝节；无数值变化。
3. 点击「退休并安排继承」，交接给信正。
4. 完成第三代「第三代的家门」终局，然后在 M2 结局面板上点击「继续游玩」关闭面板。
5. 再点击「结束月份」。
   - 预期：显示 SC-015「简要传述的家史」，而不是「完整记名的家书」。
   - 正文明确信正读到的是正綱整理过的简史，部分人名已不在纸上。
   - 选择唯一选项「接过这册简史」。
   - 预期：摘要显示「家史以简略的形式传下」；无数值变化。
6. 打开「家族编年史」，核对本分支的「已公开的家书」「简要传述的家史」各出现一次，且没有出现「完整记名的家书」。

## 5. 分支 C：封存 → 正綱拆阅 → 第二代打开

1. 标题界面重新「新游戏」，按第 3 节步骤 1–3 完成前两个既有事件。
2. 在第一次交接前出现「千恵的家书」时，选择「暂封家书」。
   - 预期：摘要显示「木匣上了封」，说明晴信决定暂封，匣中内容仍无人读过；无数值变化。
3. 点击「退休并安排继承」，交接给正綱。
4. 完成第二代 SC-011 旧客回访事件。
5. 再点击「结束月份」。
   - 预期：显示 SC-015「尚未拆封的家书」，而不是「已公开的家书」。
   - 正文明确正綱收到晴信当家时封好的木匣。
   - 选择「由正綱拆阅并记录」。
   - 预期：摘要显示「封蜡在正綱手中打开」，说明正綱拆阅并完整抄录；无数值变化。
6. 点击「退休并安排继承」，交接给信正。
7. 完成第三代「第三代的家门」终局，然后在 M2 结局面板上点击「继续游玩」关闭面板。
8. 再点击「结束月份」。
   - 预期：显示 SC-015「正綱拆阅后的家书」，而不是「仍在封存的家书」。
   - 正文明确信正读到正綱拆阅后留下的完整记录。
   - 选择唯一选项「读完后合上家书」。
   - 预期：摘要显示「封存的家书在第二代打开」；无数值变化。
9. 打开「家族编年史」，核对「尚未拆封的家书」「正綱拆阅后的家书」各出现一次，且没有出现「仍在封存的家书」。

## 6. 分支 D：封存 → 继续封存 → 木匣仍封着

1. 标题界面重新「新游戏」，按第 3 节步骤 1–3 完成前两个既有事件。
2. 在第一次交接前出现「千恵的家书」时，选择「暂封家书」。
3. 点击「退休并安排继承」，交接给正綱。
4. 完成第二代 SC-011 旧客回访事件。
5. 再点击「结束月份」，在「尚未拆封的家书」中选择「继续封存交给后代」。
   - 预期：摘要显示「木匣仍封着」，说明正綱没有拆阅；无数值变化。
6. 点击「退休并安排继承」，交接给信正。
7. 完成第三代「第三代的家门」终局，然后在 M2 结局面板上点击「继续游玩」关闭面板。
8. 再点击「结束月份」。
   - 预期：显示 SC-015「仍在封存的家书」，而不是「正綱拆阅后的家书」。
   - 正文明确晴信选择封存、正綱没有拆阅，匣中内容至今无人读过。
   - 选择唯一选项「让木匣继续封着」。
   - 预期：摘要显示「家书仍未被读」；无数值变化。
9. 打开「家族编年史」，核对「尚未拆封的家书」「仍在封存的家书」各出现一次，且没有出现「正綱拆阅后的家书」。

## 7. 预期结果汇总

| 项目 | 预期 |
|---|---|
| 第一代入口 | 首次交接前、完成「继承前的家议」与「雨夜来客」后的额外月份出现；标题「千恵的家书」；两个选项均可用 |
| 公开路径 | 第一代选公开后，第二代显示「已公开的家书」；不显示「尚未拆封的家书」 |
| 封存路径 | 第一代选封存后，第二代显示「尚未拆封的家书」；不显示「已公开的家书」 |
| 完整记名结尾 | 公开 + 保留原貌；第三代显示「完整记名的家书」 |
| 简要传述结尾 | 公开 + 整理简史；第三代显示「简要传述的家史」 |
| 第二代拆阅结尾 | 封存 + 由正綱拆阅；第三代显示「正綱拆阅后的家书」 |
| 继续封存结尾 | 封存 + 继续封存；第三代显示「仍在封存的家书」 |
| 互斥 | 同一局中第二代只会出现一个节点，第三代只会出现一个结尾 |
| 数值 | 本链所有选项 effects 为空；不新增存档 schema 字段，也不改变关系、任务或家族数值；选择、历史、一次性与结果记录写入既有事件运行时存档字段 |
| 编年史 | 每个已提交 SC-015 事件各记录一次；无重复 |
| 既有内容 | SC-011/SC-013/SC-014 的事件文本、ID、条件、效果、选项可用性与既有调度顺序不变 |
| 优先级 | 同世代同时满足时，SC-015 先于 SC-014；SC-013 必须事件仍先于两者 |

## 8. 失败判据

出现以下任一情况即记为失败，并记录步骤号、屏幕文本和 Console 文本：

- 第一次交接后才首次出现「千恵的家书」，或在继承次数大于 0 时仍出现。
- 未选择公开却出现「已公开的家书」；或未选择封存却出现「尚未拆封的家书」。
- 第一次交接前出现第二代节点；或第二次交接前出现第三代节点。
- 同一局同时出现两个第二代节点，或同时出现两个以上第三代结尾。
- 结尾与来源选择不匹配（例如选择「整理为简要家史」却出现「完整记名的家书」）。
- 本链任何选项造成关系值、任务阶段或家族数值变化，或新增/改动存档 schema 字段；仅在既有事件历史、一次性标记与已提交结果字段中记录选项，不视为失败。
- 摘要文本声称本链改变了关系、任务或家族数值。
- SC-015 节点被记录两次，或编年史重复记录同一事件。
- 同世代 SC-015 与 SC-014 同时满足时，SC-014 先出现（说明优先级或排程不符）。
- 既有 SC-011/SC-013/SC-014 事件文本、顺序或选项行为发生变化。
- Console 出现红色错误；或必须靠 Console、调试代码、手改 JSON/场景 YAML 才能继续。
- 按最短 SC-013 流程直接交接时错误地出现 SC-015 事件（应为不出现，因为本链是可选内容）。

## 9. 结果清单（纯文本报告）

```text
Project / branch:
Unity version:
Scene:
Unity 编译：通过 / 失败（错误文本：）
EditMode：Passed __ / Failed __ / Skipped __（若未运行写“未运行”）
Console error 数：
Console warning 数：
FoundationSceneValidator：PASS / FAIL / 未运行
SC-015 第一代入口出现月份：
SC-015 第一代选择：完整公开 / 暂封
SC-015 第二代节点：已公开 / 尚未拆封 / 未出现
SC-015 第二代选择：保留原貌 / 整理简史 / 正綱拆阅 / 继续封存 / 未出现
SC-015 第三代结尾：完整记名 / 简要传述 / 第二代拆阅 / 继续封存 / 未出现
本链关系/家族数值变化：无（预期） / 有（差异：）
编年史重复检查：通过 / 失败（差异：）
SC-015 待处理事件存读档（可选）：通过 / 失败 / 未检查（差异：）
既有 SC-011/SC-013/SC-014 回归：通过 / 失败（差异：）
Play Mode 分支步骤：逐项通过 / 失败（失败步骤与文本：）
用户可理解性/情感反馈：
其他备注：
```

## 10. 验证限制与后续

- 本文件中的所有“预期”都是实现约定，不是已取得的运行证据。
- 本次 Play Mode 由助手在用户临时授权下执行，四条路线均通过运行步骤；这不等于用户验收。用户明确验收前不得创建 SC-015 checkpoint、合并 `main`、打标签或推送。
- 本次没有新增或运行自动化测试。现有 Unity Editor 中已执行 Play Mode；结果、未覆盖项与本次只读源码追踪结论见 `docs/reports/sc015-playtest.md`。编译日志、EditMode、场景验证与存读档证据仍未取得。
- SC-013/SC-014 仍未提交，因此本次“既有内容未改”的静态比较使用实现前的工作区快照，而不是与 `HEAD` 对比。
- 两轮独立只读审阅均为 `ACCEPT`，包括首轮反馈修正后的复审；具体结论和未验证边界见 `docs/reports/sc015-review-2.md`。静态审阅不替代用户后续 Play Test。
- 若产品上要求三代家书成为强制主线，需要另立代码任务修改 `M2FixedScenario` 的退休门禁并补充测试；该改动不在本任务允许的文件范围内。


---

<a id="sc016-demo"></a>
## 来源：SC016_DEMO.md

# SC-016 M2 委托旧约 — 交付说明与纯文本试玩手册

更新时间：2026-11-03

本文件是 SC-016 的交付说明、试玩手册与当前证据索引。数据实现只追加事件目录与中文文本；后续验证只修正了既有 SC-013 标题入口 EditMode 测试夹具。助手在用户明确的临时授权下完成四条 Play Mode 路线；用户正式验收、场景验证与存读档验证仍待完成。

## 0. 当前状态与证据边界

- 已实现：在 SC-013 固定 M2 剧本中追加第五条跨代事件链 `chain.sc013.commission-legacy`，包含七个事件节点、十个选项和二十七条中文文本。
- 实现文件：`Assets/Resources/Events/event-catalog-v1.json`、`Assets/Resources/Localization/event-text-zh-v1.json`、`PROJECT_STATE.md`、`ARCHITECTURE.md`、`DECISIONS.md`、`SC016_DEMO.md`，以及实现记录 `docs/reports/sc016-implementation.md`。
- 规划文件（非实现改动）：设计稿 `docs/superpowers/specs/2026-11-03-sc-016-m2-commission-legacy-design.md`、实现计划 `docs/superpowers/plans/2026-11-03-sc-016-m2-commission-legacy.md`。
- 数据实现未修改 Unity 场景、预制体、`ProjectSettings`、存档 schema、package、`Library`、`Temp`，也未改写既有 SC-011、SC-013、SC-014、SC-015 事件或文本。验证阶段仅调整 `Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs` 的 Preview Scene Canvas 几何夹具；没有改生产代码或场景文件。
- 数据实现阶段的静态检查：一次性检查 JSON 解析、唯一 ID、文本引用、链成员、源选项、分支互斥、世代门槛、关系目标/数值、空结尾效果和优先级关系，10 组检查、0 错误。**这不是测试运行，也不替代 Unity 证据。**
- Unity 验证（现有 Unity 6.6.6f1 Editor，2026-11-02 本机时间）：Assets > Refresh 后 Editor 显示 `Compiling Scripts`；随后 EditMode 全量 778/778 通过，0 失败、0 跳过。结果文件：`C:\Users\panye\AppData\LocalLow\DefaultCompany\My project\TestResults.xml`。
- Play Mode（助手在用户临时授权下执行）：A `接受→继续守密` 到达“夜路仍然安全”；B `接受→公开真相` 到达“账册见了天日”；C `拒绝→主动补偿` 到达“旧账已清”；D `拒绝→拒绝再牵连` 到达“商路不再相通”。四条路线各用新的 SC-013 M2 会话，编年史结果与对应路线一致，没有保存测试进度。
- 测试完成后 Unity Console 计数为 0 errors、14 warnings、8 logs（包含本次 Editor 会话中已有的记录；不作为零警告结论）。`FoundationSceneValidator`、存读档验证、用户正式验收仍待完成；未提交、推送、合并或打标签。

## 1. 实现内容

### 1.1 链与节点

| 世代 | 事件 ID | 前置条件 | 选项与后续 |
|---|---|---|---|
| 第一代 | `event.sc013.commission-legacy.first` | `scenario: "sc013"`；`SuccessionCountEquals 0`（首次交接前） | 接下夜路递送 `...first.accept`：酒井关系 `+5`；婉拒委托 `...first.refuse`：`-5`。分别开放第二代守约或失约节点。 |
| 第二代（守约） | `event.sc013.commission-legacy.second-kept` | 第一代选择接受；`SettledSuccessionCountAtLeast 1` | 继续守密 `...second-kept.keep-secret`：`+5`；公开路线真相 `...second-kept.reveal`：`-5`。 |
| 第二代（失约） | `event.sc013.commission-legacy.second-broken` | 第一代选择拒绝；`SettledSuccessionCountAtLeast 1` | 主动补偿 `...second-broken.compensate`：`+5`；拒绝再被牵连 `...second-broken.distance`：`-5`。 |
| 第三代（守约） | `event.sc013.commission-legacy.third-safe` | 第二代继续守密；`SettledSuccessionCountAtLeast 2` | 唯一选项 `...third-safe.receive`；`effects` 为空。 |
| 第三代（公开） | `event.sc013.commission-legacy.third-public` | 第二代公开真相；`SettledSuccessionCountAtLeast 2` | 唯一选项 `...third-public.receive`；`effects` 为空。 |
| 第三代（补偿） | `event.sc013.commission-legacy.third-cleared` | 第二代主动补偿；`SettledSuccessionCountAtLeast 2` | 唯一选项 `...third-cleared.receive`；`effects` 为空。 |
| 第三代（决裂） | `event.sc013.commission-legacy.third-severed` | 第二代拒绝再牵连；`SettledSuccessionCountAtLeast 2` | 唯一选项 `...third-severed.receive`；`effects` 为空。 |

所有关系效果都指向既有 NPC `npc.sakai.jirobei`（酒井次郎兵卫），不新增人物、关系模型、任务、家族数值或存档 schema 字段。三个决策事件共六个选项使用 `RelationshipDelta`；四个第三代结尾只记录故事结果，不伪造数值变化。本链提交选项时，选择、事件历史、一次性标记与已提交结果继续使用既有 `eventRuntime` 存档字段；实际关系前后值由 SC-012 编年史在提交边界记录。

### 1.2 调度与优先级

- `EventEngine` 按 priority **降序**排程：**数值越大，越早出现**。
- 本链优先级为 `8`、`80`、`800`：
  - 低于 SC-013 必须事件的 `1000`（第一代「继承前的家议」、第三代「第三代的家门」）；
  - 低于 SC-011 的 `10`、`100`，因此既有 SC-011 事件顺序不变；
  - 高于同世代 SC-015 的 `6`、`60`、`600`，也高于 SC-014 的 `5`、`50`、`500`，因此两者都满足时 SC-016 先出现。
- 每月只排一个最高优先级事件。完整链需要玩家在每次交接前**额外结束月份**，依次让本链节点出现；M2 退休门禁不要求本链，若按最短 SC-013 流程直接交接，本链会被跳过。首次交接前若未触发入口，本链不会迟到启动，也不阻塞 M2 交接。

## 2. 试玩前须知

- 本文件包含四条分支步骤；这些是设计约定，不是已取得的运行结果。
- 使用项目现有 Unity Editor 与 `FoundationScene`。本任务未新增 UI 或场景内容，因此不需要为 SC-016 运行场景 updater。若 SC-013 的家族名册/结局面板尚未接入场景，请先完成 `SC013_DEMO.md` 第 1 节的接入与验证。
- 四条分支互斥；每条分支都需要从标题界面重新「新游戏」，不要在一条分支中试图看到全部四种结尾。
- 下列步骤中的“结束月份”均指游戏内月末推进按钮，不是现实月份。
- 第一代入口出现前，需先完成同月优先的「继承前的家议」与「雨夜来客」；第三代结尾出现前，需先完成优先的「第三代的家门」终局。

## 3. 分支 A：接受 → 继续守密 → 夜路仍然安全

1. 标题界面选择「新游戏」。预期进入 SC-013 固定 M2 剧本，第一代家主为晴信。
2. 点击「结束月份」。
   - 预期：事件面板显示「继承前的家议」。选择任一选项（本链不受该选择影响）。
3. 再点击「结束月份」。
   - 预期：显示 SC-011「雨夜来客」。选择任一选项。
4. 再点击「结束月份」。
   - 预期：显示 SC-016「账册托付」，正文说明酒井次郎兵卫请明智家沿夜路递送一册商路账册，晴信需在交接前答复。
   - 选择「接下夜路递送」。
   - 预期：摘要显示「受托夜行」，说明酒井对家族的信任增加五点；编年史记录一次指向酒井次郎兵卫的关系变化，前后值相差 `+5`。
5. 点击「退休并安排继承」。
   - 预期：家督交接给「明智 正綱」。
6. 点击「结束月份」。
   - 预期：显示 SC-011「旧客的雨夜回访」或「门房里的旧雨」（取决于第 3 步选择）。完成该事件。
7. 再点击「结束月份」。
   - 预期：显示 SC-016「接过的夜路」，正文明确先代接下的账册已经送到，酒井询问是否继续守密。
   - 选择「继续守密」。
   - 预期：摘要显示「夜路仍被守住」，酒井对家族的信任增加五点；编年史再记录一次 `+5` 变化。
8. 点击「退休并安排继承」。
   - 预期：家督交接给「明智 信正」，继承次数为 2。
9. 点击「结束月份」。
   - 预期：显示 SC-013 终局「第三代的家门」。完成终局选择。
10. 在 M2 结局面板中完成「第三代的家门」终局选择后，先点击该面板上的「继续游玩」按钮关闭面板；再点击「结束月份」。
    - 预期：显示 SC-016「夜路仍然安全」，标题与正文说明两代人守住的夜路仍可通行。
    - 选择唯一选项「确认委托继续」。
    - 预期：摘要显示「委托关系得以延续」；本节点无数值变化。
11. 打开「家族编年史」，核对「账册托付」「接过的夜路」「夜路仍然安全」各只出现一次，且没有出现其他三个第三代结尾；两次关系记录的目标均为酒井次郎兵卫，变化分别为 `+5`、`+5`。

## 4. 分支 B：接受 → 公开真相 → 账册见了天日

1. 按第 3 节步骤 1–5 完成第一代；第 4 步选择「接下夜路递送」。
2. 交接给正綱后，完成第二代 SC-011 旧客回访事件。
3. 再点击「结束月份」。在出现「接过的夜路」时，选择「公开路线真相」。
   - 预期：摘要显示「夜路见了天日」，酒井对家族的信任下降五点；编年史记录一次 `-5` 变化。
4. 点击「退休并安排继承」，交接给信正。
5. 完成第三代「第三代的家门」终局，然后在 M2 结局面板上点击「继续游玩」关闭面板。
6. 再点击「结束月份」。
   - 预期：显示 SC-016「账册见了天日」，而不是「夜路仍然安全」。
   - 正文明确夜路秘密已经公开，账册上的名字不再是秘密。
   - 选择唯一选项「为公开的旧事作结」。
   - 预期：摘要显示「家族以公开真相了结委托」；无数值变化。
7. 打开「家族编年史」，核对本分支的「账册托付」「接过的夜路」「账册见了天日」各出现一次，且没有出现「夜路仍然安全」；两次关系记录为 `+5`、`-5`。

## 5. 分支 C：拒绝 → 主动补偿 → 旧账已清

1. 标题界面重新「新游戏」，按第 3 节步骤 1–3 完成前两个既有事件。
2. 在第一次交接前出现「账册托付」时，选择「婉拒这趟委托」。
   - 预期：摘要显示「委托被退回」，酒井对家族的信任下降五点；编年史记录一次 `-5` 变化。
3. 点击「退休并安排继承」，交接给正綱。
4. 完成第二代 SC-011 旧客回访事件。
5. 再点击「结束月份」。
   - 预期：显示 SC-016「未完的交代」，而不是「接过的夜路」。
   - 正文明确先代拒绝委托导致夜路改由外人接手并出了差错，酒井询问是否愿意补上交代。
   - 选择「主动补偿」。
   - 预期：摘要显示「旧账有了交代」，酒井对家族的信任增加五点；编年史再记录一次 `+5` 变化。
6. 点击「退休并安排继承」，交接给信正。
7. 完成第三代「第三代的家门」终局，然后在 M2 结局面板上点击「继续游玩」关闭面板。
8. 再点击「结束月份」。
   - 预期：显示 SC-016「旧账已清」，而不是「商路不再相通」。
   - 正文明确先代拒绝、正綱补偿，账册上的亏欠已经补齐。
   - 选择唯一选项「确认旧账已清」。
   - 预期：摘要显示「家族修复早年的拒绝」；无数值变化。
9. 打开「家族编年史」，核对「账册托付」「未完的交代」「旧账已清」各出现一次，且没有出现「商路不再相通」；两次关系记录为 `-5`、`+5`。

## 6. 分支 D：拒绝 → 拒绝再牵连 → 商路不再相通

1. 标题界面重新「新游戏」，按第 3 节步骤 1–3 完成前两个既有事件。
2. 在第一次交接前出现「账册托付」时，选择「婉拒这趟委托」。
3. 点击「退休并安排继承」，交接给正綱。
4. 完成第二代 SC-011 旧客回访事件。
5. 再点击「结束月份」，在「未完的交代」中选择「拒绝再被牵连」。
   - 预期：摘要显示「两家渐行渐远」，酒井对家族的信任下降五点；编年史再记录一次 `-5` 变化。
6. 点击「退休并安排继承」，交接给信正。
7. 完成第三代「第三代的家门」终局，然后在 M2 结局面板上点击「继续游玩」关闭面板。
8. 再点击「结束月份」。
   - 预期：显示 SC-016「商路不再相通」，而不是「旧账已清」。
   - 正文明确两家的商路已经多年不相通，对方只为封存旧账而来。
   - 选择唯一选项「接受商路断绝」。
   - 预期：摘要显示「酒井家系不再信任明智家」；无数值变化。
9. 打开「家族编年史」，核对「账册托付」「未完的交代」「商路不再相通」各出现一次，且没有出现「旧账已清」；两次关系记录为 `-5`、`-5`。

## 7. 预期结果汇总

| 项目 | 预期 |
|---|---|
| 第一代入口 | 首次交接前、完成「继承前的家议」与「雨夜来客」后的额外月份出现；标题「账册托付」；两个选项均可用 |
| 守约路径 | 第一代选接受后，第二代显示「接过的夜路」；不显示「未完的交代」 |
| 失约路径 | 第一代选拒绝后，第二代显示「未完的交代」；不显示「接过的夜路」 |
| 夜路仍然安全 | 接受 + 继续守密；第三代显示「夜路仍然安全」 |
| 账册见了天日 | 接受 + 公开真相；第三代显示「账册见了天日」 |
| 旧账已清 | 拒绝 + 主动补偿；第三代显示「旧账已清」 |
| 商路不再相通 | 拒绝 + 拒绝再牵连；第三代显示「商路不再相通」 |
| 互斥 | 同一局中第二代只会出现一个节点，第三代只会出现一个结尾 |
| 关系数值 | 六个决策选项分别对 `npc.sakai.jirobei` 应用 `+5`/`-5`；四个结尾 `effects` 为空；不新增存档 schema 字段 |
| 编年史 | 每个已提交 SC-016 事件各记录一次；实际关系前后值与设计差值一致；无重复 |
| 既有内容 | SC-011/SC-013/SC-014/SC-015 的事件文本、ID、条件、效果、选项可用性与既有调度顺序不变 |
| 优先级 | 同世代同时满足时，SC-016 先于 SC-015 与 SC-014；SC-013 必须事件与 SC-011 仍先于 SC-016 |

## 8. 失败判据

出现以下任一情况即记为失败，并记录步骤号、屏幕文本和 Console 文本：

- 第一次交接后才首次出现「账册托付」，或在继承次数大于 0 时仍出现。
- 未选择接受却出现「接过的夜路」；或未选择拒绝却出现「未完的交代」。
- 第一次交接前出现第二代节点；或第二次交接前出现第三代节点。
- 同一局同时出现两个第二代节点，或同时出现两个以上第三代结尾。
- 结尾与来源选择不匹配（例如选择「公开路线真相」却出现「夜路仍然安全」）。
- 任一决策选项的关系变化目标不是酒井次郎兵卫，或数值不是规定的 `+5`/`-5`。
- 四个第三代结尾任一选项造成关系、任务或家族数值变化。
- 编年史前后值与实际选择不符、出现重复记录，或缺少本链已提交事件。
- 同世代 SC-016 与 SC-015/SC-014 同时满足时，SC-015 或 SC-014 先出现（说明优先级或排程不符）。
- 既有 SC-011/SC-013/SC-014/SC-015 事件文本、顺序或选项行为发生变化。
- Console 出现红色错误；或必须靠 Console、调试代码、手改 JSON/场景 YAML 才能继续。
- 按最短 SC-013 流程直接交接时错误地出现 SC-016 事件（应为不出现，因为本链是可选内容）。

## 9. 结果清单（纯文本报告）

```text
Project / branch:
Unity version:
Scene:
Unity 编译：通过 / 失败（错误文本：）
EditMode：Passed __ / Failed __ / Skipped __（若未运行写“未运行”）
Console error 数：
Console warning 数：
FoundationSceneValidator：PASS / FAIL / 未运行
SC-016 第一代入口出现月份：
SC-016 第一代选择：接受 / 婉拒
SC-016 第二代节点：接过的夜路 / 未完的交代 / 未出现
SC-016 第二代选择：继续守密 / 公开真相 / 主动补偿 / 拒绝再牵连 / 未出现
SC-016 第三代结尾：夜路仍然安全 / 账册见了天日 / 旧账已清 / 商路不再相通 / 未出现
酒井关系前后值：__ → __（第一次）；__ → __（第二次）
编年史重复检查：通过 / 失败（差异：）
SC-016 待处理事件存读档（可选）：通过 / 失败 / 未检查（差异：）
既有 SC-011/SC-013/SC-014/SC-015 回归：通过 / 失败（差异：）
Play Mode 分支步骤：逐项通过 / 失败（失败步骤与文本：）
用户可理解性/情感反馈：
其他备注：
```

## 10. 验证限制与后续

- 本文件中的所有“预期”都是实现约定，不是已取得的运行证据。
- 已取得的 Play Mode、EditMode 与 Console 结果见本节开头及 `docs/reports/sc016-implementation.md`；它们不等同于用户正式验收。
- 尚未运行 `FoundationSceneValidator` 或存读档验证。用户明确验收前不得创建 SC-016 checkpoint、合并 `main`、打标签或推送。
- 静态 JSON 检查结果见 `docs/reports/sc016-implementation.md`；Unity 全量 EditMode 与四条助手 Play Mode 路线结果分别记录，不以静态检查替代。
- SC-013/SC-014/SC-015 仍未提交，因此本次“既有内容未改”的静态比较使用实现前的工作区快照，而不是与 `HEAD` 对比。
- 若产品上要求委托旧约成为强制主线，需要另立代码任务修改 `M2FixedScenario` 的退休门禁并补充测试；该改动不在本任务允许的文件范围内。


---

<a id="sc017-demo"></a>
## 来源：SC017_DEMO.md

# SC-017 通用家谱展示 — 交付说明与纯文本试玩手册

更新时间：2026-11-03

本文件是 SC-017 的交付说明、场景接入步骤与用户试玩清单。实现目标：把 SC-013 固定六行名册改为只读、数据驱动、可滚动的通用家谱；家谱从真实家族成员与 `KinshipGraph` 关系生成，不写死六人或三代，未连接亲族与家臣分区显示。

## 0. 当前状态与证据边界

- 已实现：图快照/构建器、确定性布局、可滚动面板、typed 关系线、未连接亲族区、家臣/弟子/客将区、家主/指定继承人标记、事务式场景 updater 与 validator 扩展，以及对应 EditMode 测试源。
- 实现与审查 worker 未操作 Unity；随后用户于 2026-11-03 明确接受本次演示。用户未回传 EditMode Passed/Failed/Skipped 数量、Console 计数或 Validator 报告原文。
- 已取得：Unity 随附 Roslyn 对八个程序集编译 0 错误；纯图/布局 harness 86/86 通过（既有 77/77 保持不变，新增 9 项拥挤配偶线回归）。**这两项都不是 Unity Editor 编译、EditMode、Play Mode 或用户验收。**
- 顺序只读复审：由 Codex fallback 完成（OpenCode reviewer launcher 不可用），结论 ACCEPT；无 Critical/Important。轻微备注：空路由失败分支没有 FamilyRosterPanel 专项测试；源码检查确认该分支记录空路径且不绘制线段。复审未运行 Unity 或测试。
- 集成状态：checkpoint `3ae2ade`（`feat(sc013-017): consolidate M2 family demo`）已推送到 `origin/main`，标签 `demo-SC017` 已推送。此 checkpoint 包含 SC-013–SC-017 累积 M2 演示内容；不要把本文件中的 SC-017 用户验收扩写成对未单独验收内容的独立验收结论。

## 1. 场景接入（必须由用户在已打开的 Unity Editor 完成）

当前 `FoundationScene.unity` 已有用户未提交修改，且仍可能是旧的六行名册层级。场景 updater 已支持在同一 Undo 事务内识别并替换旧层级。

1. 在 Unity 中打开 `Assets/Scenes/FoundationScene.unity`。
2. 若场景有未保存修改，先按 `Ctrl+S` 保存（updater 会拒绝脏场景）；确认没有编译错误。
3. 运行菜单：`Tools > Sengoku Chronicle > Add SC-013 Family Roster Panel`。
   - 预期 Console 日志：`SC-017 generic family graph panel validated; FoundationScene saved.`
   - 若日志为 `refused or rolled back`，场景不会保存新层级；记录结果代码并停止，不要手工改场景。
4. 运行菜单：`Tools > Sengoku Chronicle > Validate Foundation Scene`。
5. 打开 `Logs/SceneValidationReport.txt`，确认末行 `RESULT: PASS; Errors: 0`，并看到：
   - `FAMILY GRAPH PANEL REFERENCES` 下列出的 14 个绑定均非空；
   - `PASS SC-013 family roster and ending panel hierarchy, bindings, ordering, and layout.`
6. 运行 Test Runner 的 EditMode：至少执行 `FamilyTreeGraphTests`、`FamilyTreeLayoutTests`、`FamilyRosterPanelTests`、`AddSC013FamilyRosterPanelTests`，记录 Passed/Failed/Skipped。
7. 建议再运行一次 updater 确认幂等：日志应再次成功，且场景层级不重复。

如果第 3 步失败或第 5 步不是 PASS，请把完整日志文本发回；不要删除场景或手工重建 UI。

## 2. 画面预期

打开面板后：

- 标题为「家族名册」；下方图例说明连线颜色：亲子 金、收养 紫、手足 绿、配偶 蓝。
- 亲族区按世代从上到下排列，可上下滚动；人数较多时也可左右滚动。
- 人物卡显示姓名、年龄（缺失时显示「年龄不详」）、家族身份、世代或分区；已知已故或已隐居时追加「已故」/「已隐居」，即使年龄不可用。
- 当前家主带 `【家主】`；有效固定继承人带 `【指定继承人】`；失效指定继承人带 `【指定继承人·已失效】`。
- `未连接亲族` 区显示没有任何可见亲缘连线的家族成员。
- `家臣・弟子・客将` 区单独显示佐近等非亲属成员；他们不会被画上亲缘线。
- 关系线只连接亲族区人物；亲子/收养线从父母卡下方连到子女卡上方；配偶线在无遮挡时横向连接，中间有人物卡时先尝试六条标准绕行线，六条都被遮挡时使用卡片整体边界之外的 clearance-aware 外围路径；若端点端口本身不可达则不画该条线，绝不穿过人物卡。手足线在卡片下方绕行。
- 面板底部仍显示固定继承人摘要和「关闭」按钮。

## 3. 试玩 A：SC-013 初始家谱

1. 标题界面选择「新游戏」（SC-013 固定 M2 剧本）。
2. 点击右上角「家族名册」。
3. 检查：
   - 晴信、千恵、正綱、信正、時姫 均在亲族区；
   - 晴信带【家主】，正綱带【指定继承人】；
   - 佐近只出现在家臣区，且没有任何连线连到佐近；
   - 晴信与千恵之间有蓝色配偶线；
   - 晴信与正綱、千恵与正綱、正綱与信正之间有金色亲子线；
   - 晴信与時姫之间有绿色手足线；
   - 底部摘要为「固定继承人：明智 正綱（46岁）」。
4. 滚动内容，确认所有卡片和连线都能看到，标题、图例、继承人摘要和关闭按钮不被内容遮挡。
5. 关闭后重新打开，确认人物卡与连线没有重复。
6. 记录打开前/后的继承次数：仅打开、关闭、滚动、刷新面板时，继承次数与家族数据不应变化。

## 4. 试玩 B：家主标记随合法继承更新

1. 回到标题界面（或重新启动 Play Mode），选择「SC-010 标准样例」。
2. 打开「家族名册」，确认晴信带【家主】；底部显示「当前模式没有固定继承人。」
3. 打开继承面板，执行一次合法交接：
   - 点击退休/交接；
   - 选择「正直」或正綱（合法成年继承人）；
   - 确认继承。
4. 重新打开或观察「家族名册」：
   - 新家主（正綱）卡片应带【家主】；
   - 原家主晴信不再带【家主】；
   - 其他人物、关系线仍在。
5. 再次关闭/打开面板，确认标记保持且无重复卡片。

> 说明：标准样例用于验证通用标记逻辑；SC-013 固定三段继承需要按 `SC013_DEMO.md` 的事件顺序完成，若只想验证本功能，试玩 B 已足够。

## 5. 试玩 C：大图滚动（可选）

自动化测试已构造 12 人、6 代、含旁系/配偶/收养/未连接亲族的家庭并验证滚动边界。当前没有调试 UI 手动构造该家庭；如后续 debug mode 提供数据编辑，应复用同一面板并检查：

- 超过 6 人、超过 3 代仍全部显示；
- 内容高度大于视口，可滚动到最后一代；
- 未连接亲族与家臣区位于亲族区下方；
- 任意输入顺序不改变位置。

## 6. 验收清单（请逐项回复通过/失败）

- [ ] updater 成功并保存 FoundationScene。
- [ ] validator 报告 `RESULT: PASS; Errors: 0`，且 family graph 绑定无缺失。
- [ ] EditMode 聚焦测试与全量测试结果已记录（Passed/Failed/Skipped）。
- [ ] SC-013 家谱显示 5 名亲族 + 佐近在家臣区，且佐近无亲缘线。
- [ ] 图例四种关系类型可区分，实际连线颜色与图例一致。
- [ ] 当前家主与有效固定继承人标记正确。
- [ ] 未连接亲族区可见（标准样例或测试数据）。
- [ ] 打开/关闭/滚动/刷新不改变继承与亲属数据，不产生重复卡片/连线。
- [ ] 合法继承后家主标记更新。
- [ ] 面板内容可滚动，底部继承摘要和关闭按钮始终可用。
- [ ] 标准/危机 SC-010 流程与既有 SC-011/SC-013 事件行为无回归。
- [ ] Console 错误/警告计数已记录。
- [x] 用户明确接受本次演示（2026-11-03）。

## 7. 改动文件

新增：

- `Assets/Scripts/Presentation/FamilyTreeGraph.cs`（+ `.meta`）
- `Assets/Scripts/Presentation/FamilyTreeLayout.cs`（+ `.meta`）
- `Assets/Tests/EditMode/FamilyTreeGraphTests.cs`（+ `.meta`）
- `Assets/Tests/EditMode/FamilyTreeLayoutTests.cs`（+ `.meta`）
- `SC017_DEMO.md`
- `docs/reports/sc017-implementation.md`

修改：

- `Assets/Scripts/Presentation/FamilyRosterPanel.cs`
- `Assets/Scripts/Editor/AddSC013FamilyRosterPanel.cs`
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- `Assets/Tests/EditMode/FamilyRosterPanelTests.cs`
- `Assets/Tests/EditMode/AddSC013FamilyRosterPanelTests.cs`
- `PROJECT_STATE.md`
- `DECISIONS.md`

未修改：GDD、存档 schema、SC-010/SC-011/M2 规则、package、项目/场景/预制体（本 worker 未保存场景）、`Library`、`Temp`、`.meta` GUID。

## 8. 证据索引

| 项目 | 结果 | 说明 |
|---|---|---|
| 静态类型检查 | 8/8 程序集 0 错误 | Unity 随附 Roslyn；不是 Editor 编译 |
| 图/布局 harness | 86/86 通过 | 纯 C#，不覆盖 uGUI 渲染；前 77 项为既有检查，新增 9 项为拥挤配偶线路由回归 |
| Unity Editor 编译 | 用户报告本次步骤通过 | 未回传编译日志 |
| EditMode | 用户报告本次步骤通过 | Passed/Failed/Skipped 数量未提供 |
| Console | 用户验收本次演示 | error/warning 数量未提供 |
| 场景 updater / validator | 用户报告本次步骤通过 | 未回传 Validator 报告原文 |
| Play Mode | 用户报告通过 | 2026-11-03 用户明确验收 |
| 用户验收 | 已接受（2026-11-03） | 用户回复“通过，验收”；数值和日志未提供。 |

## 9. 已知限制与非目标

- 本轮只读；不提供增删成员、编辑亲属关系、debug mode 或存档编辑器。
- 关系线使用 uGUI `Image` 段绘制，没有箭头；方向通过“父母在上、子女在下”和亲子/收养线路径表达。
- 过宽世代会启用横向滚动，而不是自动换行；这是为了保证同一世代仍横向对齐。
- 没有人物肖像、纹章或外部美术资产。
- 面板只消费 `SuccessionState`/`SuccessionWorld`/`KinshipGraph`；不读取 `M2FixedScenario` 的固定展示行作为渲染数据源。


---

<a id="sc018-demo"></a>
## 来源：SC018_DEMO.md

# SC-018 M2 独立家族事件 — 交付说明与纯文本试玩手册

更新时间：2026-11-03

本文件是 SC-018 的交付说明、试玩步骤与证据边界。实现目标：把 M2 事件目录从 23 项补到 30 项，新增七个可独立触发、每局一次的家族生活事件；同时增加按 `(当前家主 PersonId, 家人 PersonId)` 人物对键控的家庭数值关系、v5 存档兼容与编年史姓名/前后值显示。五条既有跨代事件链与 SC-011–SC-017 的既有事件定义保持不变。

## 0. 当前状态与证据边界

- 已实现：七个 `event.sc018.*` 事件、十四个选项、三十五条原创中文文本；全部使用 `Monthly` 触发、`PerGame` 单次范围、`scenario: "sc013"`，并且没有 `chainId`。
- 已实现：`FamilyRelationships` 人物对状态，键为 `(当前家主 PersonId, 家人 PersonId)`；继承后新家主只读取自己的键，旧家主记录保留，不复制数值。
- 已实现：`FamilyRelationshipDelta` 事件效果、运行时事务式应用与回滚、v4→v5 存档迁移、家庭关系编年史姓名与精确前后值；NPC `CharacterRelationships` 行为保持不变。
- 已取得：两个 JSON 解析与静态引用检查通过（30 个事件、5 条链、139 条文本、0 错误）；八个程序集使用 Unity 6.6.6f1 随附 Roslyn 编译通过（0 诊断）；纯 C# 逻辑 harness 82/82 通过。审查修正后复跑同一组 JSON 检查、八程序集编译、Player 程序集编译与 82/82 harness，结果一致；新增检查覆盖四个跨世代事件的世代中性措辞，以及家庭关系事件在 `±100` 边界夹取并报告真实前后值。
- 已取得（2026-11-03，协调方授权验证）：Unity Editor 编译、EditMode 824/824、`FoundationScene` Play Mode 主流程与 v5 存读档验证；详见第 0.1 节。
- 仍未取得：`FoundationSceneValidator` 未运行，本文档不声明场景验证通过；用户 Play Test 与验收仍待用户本人完成。
- 本实现没有新增场景 UI；事件面板沿用现有反馈文本，编年史沿用现有关系记录面板。除非现有场景本身尚未接入 SC-013/SC-017，否则不需要为 SC-018 运行场景 updater。

### 0.1 2026-11-03 协调方授权验证结果（用户验收仍待办）

以下结果由协调方在用户临时授权下，使用项目现有 Unity 6.6.6f1 Editor 与 `Assets/Scenes/FoundationScene.unity` 实际执行并记录；它们不是本实现 worker 的运行结果，也不替代用户验收。

- Play Mode（通过）：从标题界面「新游戏」开始一局全新的 M2 剧本；按第 3.1 节的顺序完成五个前置事件（SC-013 继承前的家议、雨夜来客、SC-016 账册托付、SC-015 千恵的家书、SC-014 门前的流亡少年），再处理七个 SC-018 独立事件；七个事件各出现且仅出现一次；随后完成两次交接、SC-011 旧客回访与 SC-013 信任门槛的第三代结尾；在家族编年史中核对了每个家庭关系事件的前后值。
- 存读档（通过）：保存一个 schema-v5 存档槽，槽内含四条家庭关系记录与 30 条编年史条目；读取该存档后继续推进月份，再次读取同一存档，确认回到 1561 年 7 月的状态与历史。
- 首次运行与干净重跑：第一次运行时因试玩步骤操作失误漏掉三个前置事件，该次结果作废且未保存；随后按本手册流程从新局重跑，上述干净重跑通过。首次失误属于测试步骤问题，不作为代码缺陷记录。
- 用户存档保护：测试前备份了用户存档槽 JSON 与其 `.bak`，测试后按字节原样还原；没有留下被改动的用户存档。
- EditMode（通过）：`%USERPROFILE%\AppData\LocalLow\DefaultCompany\My project\TestResults.xml` 记录 824 passed / 0 failed / 0 skipped，时间 `2026-11-03 21:12:19Z`–`2026-11-03 21:12:24Z`（XML 原值）；结果包含 `SC018IndependentFamilyEventsTests` 的 20 个用例，全部通过。
- 编译（通过）：`Logs/Editor.log` 中最新一次脚本重编译成功；协调方记录 `*** Tundra build success` 位于第 8280 与 10579 行，按 1-based `grep` 复核为第 8279 与 10578 行（同两条记录，仅计数基准差一行）。同一会话此前曾出现 `AddSC013FamilyRosterPanelTests.cs` 的 `CS0117`/`CS7036` 临时编译错误（约第 7575–7586、8154–8165 行），这些错误在后续重编译中消失；本节如实保留该先后顺序，不把临时错误当作最终状态，也不隐藏它们。
- Console（Play Mode 之前）：0 errors、0 warnings，另有 1 条信息级 `NoSubscription` 消息重复出现五次。测试之后的 Console 最终计数未在本次记录中直接观察，因此不声明测试后为零。
- 未运行：`FoundationSceneValidator` 未运行，本文档不声明场景验证通过。
- 用户验收：仍待用户本人试玩确认。协调方的授权验证不构成用户验收，也不代表里程碑已被接受。

## 1. 实现内容与人物适配

### 1.1 七个事件

关系值按 `(当前家主 PersonId, 家人 PersonId)` 的有序人物对存储；下表的箭头只表示键的存储顺序，不表示情感方向。事件摘要描述的是该家人对当前家主的信任变化。第一代家主为晴信，因此下表的起始值都是对应人物对的 `0`。

| 事件 ID | 中文标题 | 主题 | 两个选项的即时家庭关系变化 |
|---|---|---|---|
| `event.sc018.sibling-duty` | 家人的夜巡 | 家人请求暂歇夜巡、另找代班 | 准她暂歇、另找帮手：時姫 `+5`；劝她以职责为先：時姫 `-5` |
| `event.sc018.child-study` | 灯下的课业 | 家人请求延后晨课、继续读书 | 准许读书：信正 `+5`；先完成晨课：信正 `-5` |
| `event.sc018.parent-care` | 病榻前的轮值 | 家人病中，家中争论陪护方式 | 家主亲自陪护：千恵 `+5`；按房次轮值：千恵 `-5` |
| `event.sc018.heirloom` | 旧茶碗的归处 | 长房与常用者对旧物保管有分歧 | 按长幼收进正堂：正綱 `+5`、時姫 `-5`；交给常用者：正綱 `-5`、時姫 `+5` |
| `event.sc018.spouse-visit` | 探亲的行李 | 家人希望回娘家探望旧亲 | 亲自陪同：千恵 `+5`；说明事务、改日再行：千恵 `-5` |
| `event.sc018.adoption-welcome` | 家中的位置 | 旁系亲属询问自己是否算家中一员 | 明确当作自家人：時姫 `+5`；提醒她旁系自有分寸：時姫 `-5` |
| `event.sc018.family-assembly` | 家议上的分工 | 家议讨论来年分工的方式 | 让各房先开口：正綱 `+5`、信正 `+5`；家主直接定案：正綱 `-5`、信正 `-5` |

### 1.2 人物适配说明（重要）

当前工程的家庭名册有六名 `person.sc013.*` 成员：晴信（家主）、千恵（配偶）、正綱（直系）、信正（直系）、時姫（旁系）、佐近（家臣）。当前名册没有 `AdoptedChild` 角色的在册成员。

因此本轮按设计授权做了内容适配：

- 七个事件仍以明智家的家人相处、课业、病中陪护、旧物保管、探亲、归属与家议为主题；关系变化只落在在册家族成员身上，不新增人物、不新增收养关系、不把任何现有成员标成养子/养女。
- 時姫、千恵与晴信的亲缘称谓会随世代变化（例如時姫在第一代是晴信的手足，在第二、三代则是正綱/信正的旁系长辈）。因此 `sibling-duty`、`child-study`、`parent-care`、`spouse-visit` 的正文、选项与摘要都改用不依赖当代家主具体亲缘称谓的中性措辞，不再写死“手足”“子女”“长辈”“配偶”“归宁”。
- `event.sc018.adoption-welcome` 保留“归属与接纳”的独立家族主题，改写为旁系亲属時姫询问自己是否也算家中一员；文本没有声称她是养女，也没有新增亲属关系。
- 所有家庭关系效果都指向有效的非家主 `PersonId`，不会指向晴信本人；目标若可能在后续世代成为家主（正綱、信正），该事件使用现有 `SuccessionCountEquals 0` 条件限定在第一代，避免新家主成为自己的关系目标。

## 2. 试玩前准备

1. 使用项目现有 Unity 6.6.6f1 Editor 和 `Assets/Scenes/FoundationScene.unity`。不要在项目文件夹上启动 batchmode，也不要另建项目。
2. 确认 Console 没有编译错误；若场景本身尚未接入 SC-013/SC-017 的事件面板、家族名册或结局面板，先按 `SC013_DEMO.md` / `SC017_DEMO.md` 完成既有接入。本任务没有新增场景对象，因此不需要为本任务运行 updater。
3. 标题界面选择「新游戏」进入 SC-013 固定 M2 剧本。若需要重跑，可用现有「保存/读取」控件在开始前留一个存档；存档 schema 已升为 v5，旧 v4 存档会迁移为“家庭关系为空”，不会推算历史好感。
4. 下列“结束月份”均指游戏内月末推进按钮，不是现实月份。
5. 试玩期间不要保存场景、不要修改 JSON/场景/预制体；只需记录结果。

## 3. 如何遇到全部七个事件

七个新事件的优先级都是 `1`，低于所有既有事件。它们会在同月可用的更高优先级事件处理完之后出现；同优先级之间由既有加权随机决定顺序，所以七个事件的**出现顺序可能不同**，但每个 `PerGame` 事件一局只会出现一次。

### 3.1 固定前置步骤

1. 标题界面选择「新游戏」。
2. 点击「结束月份」。
   - 预期事件：「继承前的家议」。选择任一选项。
3. 点击「结束月份」。
   - 预期事件：「雨夜来客」。选择任一选项。
4. 点击「结束月份」。
   - 预期事件：「账册托付」（SC-016 第一代节点，优先级 `8`）。选择任一选项。
5. 点击「结束月份」。
   - 预期事件：「千恵的家书」（SC-015 第一代节点，优先级 `6`）。选择任一选项。
6. 点击「结束月份」。
   - 预期事件：「门前的流亡少年」（SC-014 第一代节点，优先级 `5`）。选择任一选项。
7. 此时家庭关系基线应全部为 `0`：人物对（晴信, 千恵）`0`、（晴信, 正綱）`0`、（晴信, 信正）`0`、（晴信, 時姫）`0`、（晴信, 佐近）`0`。

> 上述五个前置事件的顺序与既有优先级一致：SC-013 家议 `1000`、雨夜来客 `10`、SC-016 `8`、SC-015 `6`、SC-014 `5`，优先级数值越大越早。SC-013 最短路线只要求家议与雨夜来客；本手册额外列出 SC-014–SC-016，是为了在进入 SC-018 前覆盖这些可选链的第一代节点。若某次运行不打算覆盖它们，可按 `SC013_DEMO.md` 的最短路线先交接；该差异只影响可选内容，不改变 SC-018 的优先级设计要求。

### 3.2 逐个处理 SC-018

1. 再次点击「结束月份」。预期出现七个 SC-018 标题中的一个；具体是哪一个由加权随机决定。
2. 按第 4 节对应行选择建议选项，记录事件标题、选项标题和摘要中的关系变化。
3. 解决后再次点击「结束月份」，继续遇到下一个 SC-018 事件。
4. 三个带 `SuccessionCountEquals 0` 条件的事件（灯下的课业、旧茶碗的归处、家议上的分工）只在第一次交接前出现；如果第一次交接前没有处理，它们不会在第二代/第三代重新启动。其余四个事件在后续世代仍可出现，目标始终是不会成为家主的千恵/時姫；这四个事件的文本已按世代中性措辞编写，不依赖当代家主的具体亲缘称谓。
5. 如果中途出现其他既有可选事件，先按既有手册完成它，再继续结束月份；SC-018 仍然可用。
6. 一直重复，直到七个标题都至少出现一次。因为每个事件都是 `PerGame`，同一个标题不会第二次出现。
7. 七个都处理完后，再结束月份不应再出现新的 SC-018 事件；随后进入第 5 节的固定路线。

> 若只想快速看到某个事件，可使用同优先级加权随机重开一局；不要为了调整顺序修改 JSON、优先级或存档。

## 4. 建议选项与预期关系变化（全选“正向”）

所有家庭关系键在开局时不存在，读取值为 `0`。以下按“第 3.1 节基线”计算；同一家人的多个 `+5` 会累计，因为都是加法，最终值不受出现顺序影响。

| 事件标题 | 建议选项 | 目标与变化 | 该人物对（家主, 家人）起始值 | 预期结束值 |
|---|---|---|---|---|
| 家人的夜巡 | 准時姫暂歇，另找帮手 | 時姫 `+5` | 晴信→時姫 `0` | `5` |
| 灯下的课业 | 准他读完这一卷 | 信正 `+5` | 晴信→信正 `0` | `5` |
| 病榻前的轮值 | 由家主亲自陪护 | 千恵 `+5` | 晴信→千恵 `0` | `5` |
| 旧茶碗的归处 | 按长幼收进正堂 | 正綱 `+5`；時姫 `-5` | 晴信→正綱 `0`；晴信→時姫 `5` | 正綱 `5`；時姫 `0` |
| 探亲的行李 | 亲自陪千恵探亲 | 千恵 `+5` | 晴信→千恵 `5` | `10` |
| 家中的位置 | 告诉她，旁系也是这个家的人 | 時姫 `+5` | 晴信→時姫 `0`（茶碗事件后） | `5` |
| 家议上的分工 | 让各房先开口 | 正綱 `+5`；信正 `+5` | 晴信→正綱 `5`；晴信→信正 `5` | 正綱 `10`；信正 `10` |

全部七个“正向”选项完成后，若前置步骤完全按第 3.1 节执行，最终预期为：

| 人物对（家主, 家人） | 预期值 |
|---|---|
| 晴信 → 千恵 | `10` |
| 晴信 → 正綱 | `10` |
| 晴信 → 信正 | `10` |
| 晴信 → 時姫 | `5` |
| 晴信 → 佐近 | `0`（未涉及） |

### 4.1 反向选项（可选交叉验证）

如果想验证另一半选项，可在另一局选择以下任意一项；每项都应产生与摘要一致的 `-5`（旧茶碗为两人相反的 `±5`）：

- 家人的夜巡 →「劝時姫以职责为先」：時姫 `-5`。
- 灯下的课业 →「晨课不可荒废」：信正 `-5`。
- 病榻前的轮值 →「按房次轮值」：千恵 `-5`。
- 旧茶碗的归处 →「交给最常使用它的人」：正綱 `-5`、時姫 `+5`。
- 探亲的行李 →「说明家中事务，改日再行」：千恵 `-5`。
- 家中的位置 →「提醒她旁系自有分寸」：時姫 `-5`。
- 家议上的分工 →「由家主直接定案」：正綱 `-5`、信正 `-5`。

关系值的验收标准是**本次选择的前后差值**等于表中 `±5`；若此前已有其他事件改变同一人物对，绝对起始值会不同，但差值仍应一致。编年史应记录对应关系条目的 `前值 → 后值`。关系值一律夹取在 `-100..100`；若事件开始时已接近边界，本次实际变化可能小于 `±5`，编年史必须记录真实前值/后值（例如 `98 → 100`），而不是请求值。

### 4.2 继承后不复制旧家主数值（可选验证）

核心语义由聚焦 EditMode 测试覆盖。若要在 Play Mode 观察：

1. 第一代只处理六个 SC-018 事件，保留「家中的位置」（目标時姫）不处理；在「旧茶碗的归处」选择「交给最常使用它的人」，使人物对（晴信, 時姫）达到 `10`。
2. 完成第一次交接，家督变为正綱。
3. 结束月份并完成 SC-011 旧客回访；如出现其他可选链节点，先完成它们，不要在这一步交接。
4. 继续结束月份，直到出现「家中的位置」。
5. 选择「告诉她，旁系也是这个家的人」。
6. 预期编年史显示 `正綱 → 時姫` 的关系从 `0 → 5`，而不是继承晴信此前的 `10`；同时 `晴信 → 時姫` 的旧记录仍在编年史中。

## 5. 固定 M2 剧本路线仍应可完成

七个 SC-018 事件处理完后，按既有最短路线验证固定事件没有被遮挡：

1. 点击「退休并安排继承」，预期交接给「明智 正綱」。
2. 点击「结束月份」，预期出现 SC-011「旧客的雨夜回访」或「门房里的旧雨」；完成该事件。
3. 点击「退休并安排继承」，预期交接给「明智 信正」。
4. 点击「结束月份」，预期出现 SC-013 终局「第三代的家门」；完成选择。
5. 预期 M2 结局面板出现；点击「继续游玩」关闭面板后，家族编年史可看到本局事件记录。

若第 2 或第 4 步被 SC-018 事件抢先，或在固定事件仍可用时先出现 SC-018，属于优先级失败，请停止并回报。

## 6. 停止与回报条件

遇到以下任一情况，请停止试玩并回报，不要修改场景、JSON 或存档：

- 七个标题中有任何一个从未出现，或同一个标题在一局内出现第二次。
- 任一 SC-018 事件的选项标题、摘要或目标家人与第 1、4 节不符。
- 摘要没有说明关系上升/下降五点，或编年史前后差值不是 `±5`。
- 关系值超出 `-100..100`，边界处未被夹取（应停在边界并记录真实前后值），或出现未知/非家族人物 ID。
- 家庭关系指向家主本人，或在继承后错误复制了上一任家主的数值。
- SC-018 事件在「继承前的家议」「雨夜来客」「旧客回访」「第三代的家门」仍可用时抢先出现。
- 固定 M2 路线无法交接、被待处理事件卡住，或终局面板不出现。
- Console 出现红色错误、异常、缺失引用或 UI 报错；或保存/读取后事件/家庭关系状态异常。
- 场景需要新增对象才能显示事件，或需要修改 `ProjectSettings`、package、场景、预制体。

回报时请附：步骤编号、事件标题与 ID、选项标题与 ID、选择前后的家庭关系值与人物对、Console 原文（如有）、是否可复现，以及当前是否已保存/读取过存档。

## 7. 结果清单（请逐项回复通过/失败）

以下清单由用户本人试玩后确认。协调方 2026-11-03 的授权验证结果见第 0.1 节，不替代本清单的签署。

- [ ] 标题界面「新游戏」进入 SC-013，第一代家主为晴信。
- [ ] 第 3.1 节五个前置事件按预期出现，家庭关系基线全部为 `0`。
- [ ] 七个 SC-018 标题全部出现，每个只出现一次。
- [ ] 每个事件都有两个可理解、可选择的选项。
- [ ] 七个“正向”选项的摘要均说明 `+5`（旧茶碗为两人 `+5/-5`，家议为两人各 `+5`），编年史差值一致。
- [ ] 所有目标均为现有非家主家族成员；没有新增人物或收养关系。
- [ ] 全选正向后的最终关系值符合第 4 节表（或差值一致，若前置步骤不同）。
- [ ] 第一代结束后，正綱与家人的新人物对值从 `0` 开始，没有复制晴信的数值；晴信旧记录仍在。
- [ ] SC-018 未在固定 M2 事件仍可用时抢先出现。
- [ ] 两代交接与 SC-011 旧客回访仍可完成。
- [ ] 第三代「第三代的家门」与 M2 结局面板仍可完成。
- [ ] 保存/读取后家庭关系、事件历史与编年史姓名/前后值保持。
- [ ] Console 错误/警告计数已记录。
- [ ] 未修改场景、预制体、`ProjectSettings`、package、`Library`/`Temp` 或既有 `.meta` GUID。
- [ ] EditMode 聚焦测试结果已记录（Passed/Failed/Skipped）。
- [ ] 用户明确接受或拒绝本次演示。

## 8. 改动文件与证据索引

新增：

- `Assets/Scripts/Domain/FamilyRelationships.cs`（+ `.meta`）
- `Assets/Tests/EditMode/SC018IndependentFamilyEventsTests.cs`（+ `.meta`）
- `SC018_DEMO.md`
- `docs/reports/sc018-implementation.md`
- `docs/superpowers/specs/2026-11-03-sc-018-independent-family-events-design.md`
- `docs/superpowers/plans/2026-11-03-sc-018-independent-family-events.md`

修改：

- `Assets/Resources/Events/event-catalog-v1.json`（7 个事件）
- `Assets/Resources/Localization/event-text-zh-v1.json`（35 条文本）
- `Assets/Scripts/Events/EventCatalogData.cs`
- `Assets/Scripts/Events/EventCatalogValidator.cs`
- `Assets/Scripts/Events/EventEngine.cs`
- `Assets/Scripts/Events/EventRuleContext.cs`
- `Assets/Scripts/Domain/FamilyChronicleEntry.cs`（仅注释：subject 说明加入 person-pair）
- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Persistence/SaveData.cs`
- `Assets/Scripts/Persistence/SaveDataValidator.cs`
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- `Assets/Tests/EditMode/SaveDataValidatorTests.cs`（schema 版本断言更新）
- `PROJECT_STATE.md`

未修改：场景、预制体、`ProjectSettings`、package、关系算法中的 NPC 行为、事件条件类型、五条既有链、既有事件定义与文本、`Library`、`Temp`。

| 证据 | 结果 | 说明 |
|---|---|---|
| 两个 JSON 解析 | 通过 | Python 3.14.8 `json.load` |
| 静态结构/引用检查 | 0 错误 | 30 事件 / 5 链 / 139 文本；7 个新事件；35 条新文本 |
| 既有内容保持 | 通过 | 23 个既有事件、5 条链、104 条既有文本与实现前快照 JSON 相等；文本前缀逐字节保持（忽略 CRLF） |
| 八程序集 Roslyn 编译 | 通过 | Unity 6.6.6f1 随附 csc；Domain/Persistence/Events/Application/Presentation/Editor/PlayerMovement2D/EditModeTests 全部退出码 0、0 诊断；最终续作复跑结果一致 |
| 纯 C# 逻辑 harness | 82/82 通过 | 人物对键控、继承前后人物对、上下限夹取、事件路径边界夹取与真实前后值、事件事务与回滚、NPC 行为、v5 保存校验、v4/v3/v1 迁移、编年史姓名/前后值；审查修正后复跑结果一致 |
| 跨世代措辞静态检查 | 通过（host-side + EditMode） | 四个跨世代事件的标题/正文/选项/摘要均不含写死的“手足/子女/长辈/配偶/归宁”等亲缘称谓；对应 EditMode 断言随 824/824 通过 |
| `GameSession` 拒绝自身/非成员/非当前家主 | 已加入聚焦测试 | 拒绝时不改变存储、不写编年史；随 EditMode 824/824 执行通过 |
| v4 存档 presence 规则 | 通过（host-side + EditMode） | v4 缺少 `chronicle` 被拒绝；v4 含 `chronicle` 且无 `familyRelationships` 可迁移；对应 EditMode 用例随 824/824 通过 |
| Unity Editor 编译 | 通过（2026-11-03） | `Logs/Editor.log` 最新 `*** Tundra build success`（协调方记录第 8280、10579 行；1-based grep 复核为第 8279、10578 行）；此前 `AddSC013FamilyRosterPanelTests.cs` 的 CS0117/CS7036 临时错误见第 0.1 节 |
| EditMode Test Runner | 824/824 通过，0 失败、0 跳过 | `%USERPROFILE%\AppData\LocalLow\DefaultCompany\My project\TestResults.xml`，2026-11-03 21:12:19Z–21:12:24Z；含 `SC018IndependentFamilyEventsTests` 20 个用例 |
| Console 计数 | Play Mode 前 0 errors / 0 warnings | 另有 1 条信息级 `NoSubscription` 消息重复五次；测试后的最终计数未直接观察，不声明为零 |
| `FoundationSceneValidator` | 未运行 | 本任务未改场景；不声明场景验证通过 |
| Play Mode | 通过（协调方授权验证） | 七个事件各一次、两次交接、SC-011 回访、SC-013 信任门槛结尾、编年史前后值、v5 存读档恢复 1561-07；见第 0.1 节 |
| 用户 Play Test | 待办 | 用户验收仍未完成；授权验证不替代用户验收 |


---

<a id="sc019-demo"></a>
## 来源：SC019_DEMO.md

# SC-019 家族产业首切片试玩与验收说明

日期：2026-11-03  
最后更新：2026-11-04  
状态：代码、EditMode 与场景 validator 已通过；协调方按用户临时授权完成指定 Play Mode 路线。累计 checkpoint `714cb3a` 已按用户指示推送到 `origin/main`；用户正式验收仍待办，存读档、跨月日推进/旅行和资源短缺分支尚未覆盖。
范围：M3 的第一个纵向切片（农庄 + 商馆 + 人员任命 + 每月自动结算），**不是完整 M3**，不包含成长、婚姻、收养、教育、隐居、摄政、家训、抱负、产业升级、市场竞价或情报消耗。

## 1. 运行前检查

1. 确认当前 Unity Editor 打开的是 `C:\Users\panye\My project`，场景为 `Assets/Scenes/FoundationScene.unity`。
2. 确认 Console 没有红色编译错误；本切片审查修正后在 Unity `6000.6.6f1` 的 EditMode 全量测试中取得 `923/923` 通过（`2026-11-04 04:15:29Z–04:15:35Z`，`Logs/sc019-final-correction-all.xml`；修正前基线为 `921/921`）。
3. 本 worker 已通过事务式生成器把 `IndustryRoot` / `IndustryPanel` 写入 `FoundationScene`；产业打开按钮位于左上角继承 HUD 正下方（左上锚定 `(20,-250)`，200×44），不再覆盖 `SuccessionHud` 的家主/年龄行。`FoundationSceneValidator` 对 SC-019 的检查为 `PASS SC-019 industry panel hierarchy, bindings, ordering, and layout.`，2026-11-04 最终报告为 `RESULT: PASS; Errors: 0`（`Logs/SceneValidationReport.txt`）。
4. 生成器没有留下运行时候选按钮；场景中只保留隐藏的 `IndustryCandidateButtonTemplate`，候选按钮在 Play Mode 中按当前家族成员动态生成。

## 2. 新局初始值

从标题界面选择「新游戏」（SC-013 固定三代剧本）后，点击左上角继承 HUD 正下方的「产业」按钮（与 HUD 同左边界，位于其下 20 px；不会遮住家主姓名与年龄行）。

预期：

- 资金 `30`、物资 `12`、情报 `0`。
- 农庄：等级 1、1 个职位、3/4 工时；状态「空缺：无人负责，本月无产出」；负责人「空缺」。
- 商馆：等级 1、1 个职位、3/4 工时；状态「空缺：无人负责，本月无产出」；负责人「空缺」。
- 六名现有成员均为合格候选：晴信、千恵、正綱、信正、時姫、佐近。
- 报告区显示「本月尚未结算。」。

## 3. 任命与 4/3 时间预算

1. 在农庄行点击「明智 晴信（剩余 4/4）」。
2. 在商馆行点击「明智 千恵（剩余 4/4）」。

预期：

- 农庄负责人显示「明智 晴信（3/4 已用，1 可用）」。
- 商馆负责人显示「明智 千恵（3/4 已用，1 可用）」。
- 晴信不再出现在农庄候选列表；千恵不再出现在商馆候选列表。
- 晴信或千恵都不能再担任另一个产业职位（`3 + 3 > 4`）。
- 任命本身不扣资源、不推进日期、不写编年史。

替换与解除：

- 对已占用产业点击另一名候选，会原子替换负责人；旧负责人的 3 单位预留立即释放。
- 点击「解除」清空该产业负责人；空产业再次解除是幂等成功。
- 候选按钮按 3 列 × 2 行排列在各自产业行内，六个候选互不重叠且都落在面板内，可逐个点击。
- 被拒绝的任命会在面板消息行显示可操作原因（例如「该人物本月时间不足：一个职位需要 3/4 单位。」）；任命暂停时同时显示顶部「任命暂停：…」。
- 继承交接或固定危机示范替换名册后，失效任命会被立即清理，面板消息行显示「已移除失效产业任命：…」，不会留下不可用职位。
- 有待处理事件、继承决策、幼主成年交权或家族灭亡时，任命被拒绝，面板顶部显示「任命暂停：…」。

## 4. 结束 1560-04：满员月结

在管理界面点击「结束本月」。

预期：

- 日期推进到 `1560-05-01`。
- 资金 `30 → 35`、物资 `12 → 13`、情报 `0 → 1`。
- 报告区显示完整月结摘要：农庄毛收成 `+2 资金/+4 物资`，保留 `1 物资`维护，净 `+2 资金/+3 物资`；商馆支付 `6 资金/2 物资`维护，产出 `+9 资金/+1 情报`；月末 `资金 30→35，物资 12→13，情报 0→1`。
- 编年史新增**一条**「产业月结」，类别显示「产业」，详情显示「资金 30 → 35」，摘要包含逐产业明细；不显示 `industry.farm` 或 `person.sc013.*` 原始 ID。
- 若月结后排定事件，事件面板出现，月份暂停；必须先处理事件，才能继续结束月份。

## 5. 跨月防护

1. 在新局中先点击「推进一天」直到 `1560-04-30`，再点击「推进一天」。
2. 在 `1560-04-30` 选择旅行目的地（例如那古野，1 天路程）。

预期：

- 月末时「推进一天」按钮保持可点击；点击后被拒绝，日期保持 `1560-04-30`，时钟面板下方可见提示「日期将跨月：请使用「结束本月」完成月度结算。」。
- 多日推进若目标日期跨月，整笔拒绝，日期不变。
- 旅行预览返回失败并显示「行程将跨越月末：请先结束本月，再安排旅行。」；确认旅行也拒绝，日期与地点都不变。
- 只有「结束本月」会执行产业月结并推进到下月 1 日。
- 月结过程中若发生异常，日期、资源、`lastSettledMonthIndex`、随机流、事件运行状态与编年史全部回滚；面板显示「月结异常，已回滚…」，不会出现半结算。

## 6. 空缺、短缺与恢复

1. 只任命商馆（例如千恵），解除农庄。
2. 连续结束月份并处理事件，直到物资不足以支付商馆维护。

预期：

- 商馆状态变为「资源不足：缺 X 资金、Y 物资，本月停产」。
- 停产月份不扣维护费、不产出；资源永不为负；任命保留。
- 重新任命农庄负责人后，农庄从自身收成中保留维护，净 `+2 资金/+3 物资`，可逐步恢复。
- 农庄从 `0` 资金/`0` 物资也能运作；物资满仓 `999999` 时，净产 3 先算再夹上限，满仓仍为 `999999`，不会掉到 `999998`。

## 7. 两次继承交接

1. 完成第一代前置事件（「继承前的家议」与「雨夜来客」），并让晴信任农庄、千恵任商馆。
2. 执行第一次交接（晴信 → 正綱）。

预期：

- 晴信退休后，农庄负责人自动解除；状态「空缺」。
- 商馆仍由千恵负责，因为千恵仍是合法成员。
- 正綱不会自动接管农庄。
- 正綱可用自己的 4 单位任命一个职位；不能再接第二个职位。
- 编年史新增**一条**「产业人事」，来源为继承；重复清理不会重复追加。

3. 完成第二代旧客回访事件，并让正綱担任一个产业职位。
4. 执行第二次交接（正綱 → 信正）。

预期：

- 正綱的职位自动解除；信正不自动接管。
- 千恵的合法职位保留。
- 信正可用自己的 4 单位重新任命。

固定危机示范（标题界面「危机示范」）：

- 进入示范会替换为危机名册；若之前任命了名册外人物（例如時姫），该职位立即清空并显示「已移除失效产业任命：…」。
- 仍在危机名册中的合法任命（例如千恵）保留。

## 8. 保存、读档与迁移

1. 在管理稳定点保存。
2. 读取该存档，检查资源、两名负责人、`lastSettledMonthIndex` 与编年史。
3. 再次结束月份，确认只结算当前月一次。

预期：

- 读档恢复资金/物资/情报、两产业任命与已结算月份；不补算、不追加编年史。
- 读档后没有瞬时月结报告时，面板显示最后一条「产业」编年史摘要；资源本身来自存档。
- 同一存档重复读取并重复操作，结果与编年史条数一致。
- v5 旧档迁移到 v6 时保留 SC-018 `familyRelationships`、全部编年史、事件历史、继承、任务、日期与地点；产业初始化为 `30/12/0`、两产业空缺、`lastSettled = 存档月份 - 1`，不补算历史收益、不追加编年史。
- 已含 `industry` 根字段的 v5/v4/v3/v2/v1 载荷被拒绝，不静默替换。
- v6 载荷若缺少 `industry` 或显式写 `"industry": null`，整份读档被拒绝并保留原文件；不会用初始状态静默补全。

## 9. 停止与报告条件

出现以下任一情况立即停止试玩并报告，不得继续声称通过：

- 资源为负、超过 `999999` 或出现欠款/自动变卖。
- 同一月份重复扣费或产出。
- 日推进/旅行跨月未被拒绝。
- 旧家主职位未解除，或新家主自动接管。
- 读档后重复月结或丢失任命。
- 存档无法保存、v5 迁移丢失 SC-018 关系/编年史。
- 场景生成器部分写入，或 Console 出现红色错误。
- 面板显示原始稳定 ID、空报告错误或任命按钮状态与规则不一致。

## 10. 证据边界

- 本 worker 已取得：Unity 编译 0 `error CS`；审查修正后 EditMode `923/923` 通过（`Logs/sc019-final-correction-all.xml`，`2026-11-04T04:15:29Z–04:15:35Z`；修正前基线 `921/921`，`Logs/sc019-correction-all4.xml`）；`FoundationSceneValidator` 整体 `PASS; Errors: 0`（`Logs/sc019-final-correction-validator.log` 与 `Logs/SceneValidationReport.txt`）；v6 存读档、v6 null 拒绝与 v5 迁移 EditMode 测试通过。
- 审查修正覆盖：产业打开按钮不与 `SuccessionHud`、编年史/名册按钮重叠且保持在画布内，validator 能捕获按钮压住 HUD 的回归；月结注入故障发生在事件排程消耗随机流之后，回滚后随机流与事件运行态都恢复到结算前，恢复重试仍成功。
- OpenCode2 worker **未运行** Play Mode；协调方于 2026-11-04 在用户本次临时授权下，使用 Unity 6000.6.6f1 / `FoundationScene` 完成新局主路线。
- 本次协调方 Play Mode 与 EditMode、场景 validator 分开记录；用户正式验收仍待完成。停止 Play Mode 时 Console 为 0 errors / 0 warnings / 1 info，唯一信息项为 Unity AI `NoSubscription`。
- 本次未保存场景或游戏存档；现有 `save-slot-1.json` 与 `.bak` 时间未变。之后用户明确要求累计集成，SC-019 随 checkpoint `714cb3a` 合并推送到 `origin/main`；用户正式验收仍待办，未打 tag。

## 11. 2026-11-04 协调方 Play Mode 记录（用户临时授权）

运行环境：Unity `6000.6.6f1`，普通 Editor，项目 `C:\Users\panye\My project`，场景 `FoundationScene`。`Logs/Editor.log` 记录 `Tundra build success`；进入 Play Mode 后从标题界面「新游戏」开始。没有运行 EditMode 测试或 batchmode。

实际路线与观察：

1. 新局日期为 `1560-04-01`，产业初始资源 `30/12/0`，农庄与商馆空缺；产业入口能打开面板，候选列表可见。
2. 任命晴信负责农庄、千恵负责商馆后，两人分别显示 `3/4 已用，1 可用`；任命成功提示可见。
3. 结束 4 月后日期到 `1560-05-01`，月结资源为 `35/13/1`；面板摘要与产业编年史显示农庄/商馆明细及资源前后值，没有显示稳定 ID。随即出现「继承前的家议」，处理后继续。
4. 再结束一个月到 `1560-06-01`，资源到 `40/14/2`，出现「雨夜来客」；处理事件后执行固定继承，家主从晴信交接给正綱。
5. 交接后，晴信负责的农庄任命自动清除，千恵的商馆任命保留；正綱可用自己的预算重新任命农庄，显示 `3/4 已用，1 可用`。再结束一个月到 `1560-07-01`，资源到 `45/15/3`，并出现「旧客的雨夜回访」事件。
6. 停止 Play Mode 后，编辑器回到 Scene View。最终 Console 为 `0 errors / 0 warnings / 1 info`；唯一信息日志是 Unity AI `NoSubscription`（`Debug.Log`），未见游戏运行异常。原有 `save-slot-1.json` 和 `.bak` 均未改写。

本次实测通过了产业入口、初始资源、人员任命、连续月结、月结编年史、事件暂停处理、一次继承清理和新家主任命；**未覆盖**存读档、日推进/旅行跨月拦截、资源短缺恢复、第二次继承及危机示范。该运行证据不等于用户正式验收；未提交、推送、合并或打 tag。

观察到标题界面仍显示旧文案「战国立志传 · Phase 1 演示」；「新游戏」实际进入 SC-013 固定剧本。此文案不影响本次产业路线，但需按 GDD v4.0 后续核对。


---

<a id="sc020-demo"></a>
## 来源：SC020_DEMO.md

# SC-020 子女培养与人物成长示范：试玩与验收说明

日期：2026-11-04  
状态：代码、EditMode 测试源码、静态编译与离线领域 harness 已完成；**EditMode、场景生成/Validator、Console 与 Play Mode 尚未由本 worker 执行**。累计 checkpoint `714cb3a` 已按用户指示推送到 `origin/main`；本切片用户 Play Test 与正式验收仍 pending。
范围：M3 的独立教育纵向切片——青叶家 1560 年、15 岁千景的一次年度培养与 16 岁成年交接。**不是完整 M3**，不包含婚姻、出生、收养、家谱编辑、导师自由创建、隐居、摄政、完整人物成长或长期人口模拟。

> **实施边界（务必先读）**：本 OpenCode2 Dispatch 没有可用的 Unity bridge/MCP，也没有通过 Computer Use 操作 Editor。`Logs/Editor.log` 最后记录的 `Entering Playmode with Reload Domain disabled.` 之后没有可确认的退出/当前 dirty 记录，因此无法在不碰 GUI 的前提下证明当前场景可安全写入。按照协调方 2026-11-04 的指示，本 worker **没有执行** `Tools/Sengoku Chronicle/Add SC-020 Character Education`、**没有保存** `FoundationScene`、**没有运行** EditMode Test Runner 或 `FoundationSceneValidator`，也不声称这些步骤已通过。下列第 0 节必须由具备 Editor 操作的集成人员完成，之后用户才能开始第 1 节。

## 0. 集成人员前置步骤（不属于用户 Play Test）

1. 确认 Unity `6000.6.6f1` 只打开 `C:\Users\panye\My project`，活动场景为 `Assets/Scenes/FoundationScene.unity`，且**不在 Play Mode**。
2. 如果场景有未保存改动，先由用户决定保存或放弃；不要覆盖用户未提交的场景改动。
3. 在 Console 无编译错误后，执行菜单 `Tools/Sengoku Chronicle/Add SC-020 Character Education`。生成器应在一个 Undo 组内新增：
   - 标题页按钮 `SC020EducationDemoButton`（标签「SC-020 子女培养示范」）；
   - `Canvas/SC020EducationRoot`、打开按钮 `SC020EducationOpenButton`（标签「子女培养」）；
   - 隐藏的 `SC020EducationPanel` 与方向/导师模板。
4. 执行 `Tools/Sengoku Chronicle/Validate Foundation Scene`，确认报告写入 `Logs/SceneValidationReport.txt` 且包含：
   - `PASS SC-020 character-education panel, title entry, bindings, and layout.`
   - `RESULT: PASS; Errors: 0`
5. 运行 EditMode Test Runner（Run All），记录实际 totals；确认 `SC020EducationSessionTests`、`CharacterGrowthRulesTests`、`CharacterDevelopmentStateTests`、`CharacterGrowthSaveTests`、`CharacterEducationPanelTests`、`AddSC020CharacterEducationTests` 均无失败。
6. 以上任一步失败：停止，不进入用户 Play Test，把错误原文与 Console 计数交给协调方。

## 1. 用户 Play Test 准备

1. 从标题界面确认原有「新游戏」「继续」「SC-010 标准样例」「SC-010 危机示范」入口仍在。
2. 点击「**SC-020 子女培养示范**」。
3. 预期：标题隐藏，地图/时钟显示日期 `1560-01-01`，子女培养面板自动打开。

若标题没有 SC-020 入口或日期不是 `1560-01-01`，停止并报告“场景生成/入口失败”。

## 2. 开局固定值

预期面板与名册显示：

| 人物 | 年龄 | 身份/关系 |
|---|---:|---|
| 青叶 宗一郎 | 40 | 家主，导师专长：武艺 |
| 青叶 小夜 | 38 | 配偶，导师专长：学问 |
| 青叶 千景 | 15 | 直系亲属，本年培养对象 |

千景五项能力：**忍术 15 / 武艺 12 / 学问 20 / 经营 10 / 社交 13**。家庭环境固定为“有利”，千景资质固定为“学问”。

## 3. 方向与导师预览

1. 在方向按钮中选择「**学问**」。
2. 导师列表应出现宗一郎与小夜；默认选中第一位（宗一郎）。
3. 预期预览：**学问 20 → 27**；说明包含基础 +4、资质契合 +2、家庭环境有利 +1。
4. 改选「**青叶 小夜（学问）**」。
5. 预期预览：**学问 20 → 29**；说明包含基础 +4、资质契合 +2、导师专长 +2、家庭环境有利 +1，共 +9。
6. 改选「武艺」并分别选择宗一郎/小夜，确认预览按资质/导师专长变化（不应出现 +9）。
7. 选择「自由成长」。
8. 预期：五项各 `+1`（忍术 16、武艺 13、学问 21、经营 11、社交 14），无导师。

此步骤只计算预览；**确认提交前能力值不得改变**。

## 4. 提交计划

1. 回到「学问」并选择小夜。
2. 点击「提交年度计划」。
3. 预期：消息显示已提交 15 岁计划；面板显示“本年计划：学问／导师 青叶 小夜”；五项能力仍为 `15/12/20/10/13`。
4. 再改选「武艺」并由宗一郎指导，点击提交；预期计划被替换，能力仍不变。
5. 最后重新提交「学问」+ 小夜，供后续年度结算验证。

预期：提交/替换不写编年史、不立即获得成长。

## 5. 保存/读取前置验证

1. 打开现有存读档面板，保存。
2. 读取同一存档。
3. 打开培养面板，确认日期仍为 `1560-01-01`、计划仍为「学问／小夜」、能力仍为 `15/12/20/10/13`。
4. 确认编年史中没有新的“教育/培养”记录。

## 6. 推进到 1560 年 12 月

连续点击「结束本月」，从 1560-01 推进到 1560-12。每月如出现事件，按界面提示完成选择后再继续。

到 `1560-12-01` 时预期：

- 千景仍为 15 岁。
- 学问仍为 20。
- 编年史仍无教育结算记录。
- 面板仍显示本年计划。

若 12 月之前出现能力增长或教育编年史，停止并报告“提前结算”。

## 7. 关闭 12 月（唯一一次年度结算）

1. 在 `1560-12-01` 点击「结束本月」。
2. 如出现 1561 年 1 月事件，先完成选择。
3. 预期日期：`1561-01-01`；千景年龄：16；面板显示已成年、儿童年度培养控件禁用。
4. 预期能力：**忍术 15 / 武艺 12 / 学问 29 / 经营 10 / 社交 13**（只有学问 +9）。
5. 打开编年史：应恰好新增 **1** 条教育/培养记录，包含：
   - 千景姓名（不显示原始 `person.` ID）；
   - 方向「学问」；
   - 导师「青叶 小夜」；
   - 学问 `20 → 29`；
   - 简短原因（基础/资质/导师/环境）。
6. 记录条数与摘要原文，供回报。

## 8. 重复与幂等验证

1. 再点击一次「结束本月」（1561-01 → 1561-02），完成可能出现的事件。
2. 预期千景仍 16 岁、学问仍 29、教育编年史仍只有 1 条。
3. 保存当前存档，读取；再次打开面板，确认能力/结果不变。
4. 确认不能为 16 岁千景提交新的儿童年度计划（按钮禁用；直接尝试也不应产生成长）。

## 9. 既有剧本回归

1. 返回标题（若界面无返回入口，读取标题前状态或重启 Play Mode 后回标题）。
2. 依次进入「SC-010 标准样例」「SC-010 危机示范」「新游戏」。
3. 预期三个既有入口行为与 SC-019 验收时一致；SC-020 的家族、日期、教育面板不会替换或污染 SC-013 标准剧本。
4. 若「新游戏」仍从 `1560-04-01` 开始且家族为明智家，记录通过。

## 10. 明确失败停止条件

出现以下任一情况，停止并保留现场，不要继续推月或保存覆盖：

- 标题没有 SC-020 入口，或进入后日期不是 `1560-01-01`。
- 开局能力不是 `15/12/20/10/13`，或年龄不是 40/38/15。
- 预览不是 `20 → 27`（宗一郎）/`20 → 29`（小夜）。
- 提交计划后能力立即变化，或提交时写编年史。
- 12 月前结算教育，或 12 月结算后能力不是学问 29。
- 16 岁后仍可提交儿童计划，或同一年重复增长。
- 教育编年史不是 1 条、出现原始 `person.` ID、或缺少导师/前后值。
- SC-013/SC-010 既有入口被替换或无法进入。
- Console 出现红色错误。

## 11. 纯文本回报清单

请按以下格式回报（不要只说“通过了”）：

```text
SC-020 Play Test
Editor: 6000.6.6f1 / C:\Users\panye\My project
Scene: Assets/Scenes/FoundationScene.unity
入口: SC-020 子女培养示范
开局日期/年龄: 1560-01-01 / 40-38-15
开局能力: 忍术__ 武艺__ 学问__ 经营__ 社交__
预览-宗一郎: 学问 __ → __
预览-小夜: 学问 __ → __
提交后能力不变: 是/否
存档读取后计划保留: 是/否
12-01 时年龄/学问: __ / __
1561-01-01 时年龄/能力: __ / 忍术__ 武艺__ 学问__ 经营__ 社交__
教育编年史条数: __
编年史摘要原文: ____
重复结束月份后条数/学问: __ / __
16 岁后儿童计划禁用: 是/否
SC-010 标准/危机/新游戏回归: 通过/失败（说明）
Console: __ errors / __ warnings / __ info
失败或异常原文: ____
结论: 通过 / 不通过
```

用户 Play Test 由用户本人执行并回报；本文件不把任何自动化或静态检查当作 Play Mode 或用户验收。


---

<a id="sc021-demo"></a>
## 来源：SC021_DEMO.md

# SC-021 家族研究与技艺传承示范：试玩与验收说明

日期：2026-11-04  
状态：代码、场景集成、Unity 编译、UI 收尾聚焦测试与全量 EditMode `1105/1105`、`FoundationSceneValidator` 已完成；Mochi 已执行无截图 Play Mode 主路线，**用户正式验收 pending**。本切片集成提交在 feature 分支 `panyeung/sc021-scene-integration`，未合并 `main`、未打 tag。
范围：M3 的家族研究纵向切片——影山家从 `1560-01-01` 开始，安排一项原创家族技艺「烟幕调制术」的研究，在两次月结后把完成的研究保存为家族知识。**不是完整 M3**，不包含战斗/任务中实际施放技艺、个人掌握、师徒教授、大型科技树、研究随机事件、可视化节点编辑器或多人协作。

## 0. 场景集成状态（已完成，2026-11-04）

本切片已在任务 worktree `C:\Users\panye\orca\workspaces\My project\sc021-scene-integration` 的独立 Unity `6000.6.6f1` Editor 中完成场景接入；该 Editor 已关闭，未操作或修改用户 checkout `C:\Users\panye\My project`。

- `Tools/Sengoku Chronicle/Add SC-021 Family Research` 已执行并保存 `FoundationScene`：新增 `SC021ResearchRoot`、隐藏的 `SC021ResearchPanel`、`SC021ResearchOpenButton`、研究者模板、全部文本行与标题入口 `SC021ResearchDemoButton`。
- `Tools/Sengoku Chronicle/Validate Foundation Scene` 输出 `PASS SC-021 family-research panel, title entry, bindings, and layout.`；SC-019、SC-020、SC-022 与既有条目的 PASS 行保持。整体结果为 `RESULT: FAIL; Errors: 1`，唯一错误是既有 SC-009 反馈条位置检查，不属于 SC-021。
- 聚焦 EditMode：`126 passed / 0 failed / 0 skipped / 0 inconclusive`（场景集成轮 `125/125`，复审 follow-up 新增正向 v7 JSON 迁移回归测试后为 `126/126`），覆盖本 demo 涉及的 Catalog/State/Settlement/Session/Save/View/Panel/Generator/Scenario、编年史、存档迁移/校验与标题入口；记录见 `Logs/sc021-integration-tests.txt`、`.xml` 与 `Logs/sc021-followup-focused.xml`。
- 全量 EditMode：复审 follow-up 在同一 worktree 通过 batchmode Test Runner 运行 `1103 passed / 0 failed / 0 skipped / 0 inconclusive`；记录见 `Logs/sc021-followup-full.xml`。这是 Unity Test Runner 结果，不是 Play Mode 证据。
- Console 在场景集成轮聚焦测试后最近 11 条为 `0 errors / 4 warnings / 7 info`；其中 3 条 warning 来自负向测试刻意触发的回滚日志，1 条是 Unity AI 账号网络提示，不是本切片运行错误。复审 follow-up 使用 batchmode，不呈现 Editor Console UI，因此不新增 Console 条目计数；三份 Unity 日志的 `error CS` 均为 0。
- 2026-11-04 UI 收尾：活动研究月只保留一条进度结果——状态行只显示研究者，进度行显示 `P/N（每次月结推进 1 点）`，月结摘要不再同时出现在消息行；灭亡继承状态改为可读文案，不再渲染原始 `extinction.*` 结果 ID。新增聚焦回归 `2/2`（活动月单一进度结果、灭亡可读状态），全量 EditMode `1105/1105`（`0` failed/skipped/inconclusive，`Logs/sc021-worker-full-editmode.txt`）。`FoundationSceneValidator` 重跑仍为 `RESULT: FAIL; Errors: 1`（既有 SC-009 反馈条布局），SC-021 行 `PASS`；测试后 Console 最近 20 条为 `2 errors / 12 warnings / 6 info`，全部来自负向测试刻意触发的编年史 append 失败日志、生成器回滚 warning 与故障注入 warning，不是本切片回归。
- 2026-11-04 复审 follow-up：开局/未安排计划时月推进率只在进度行显示一次；暂停只在结果行显示原因与保持的 `P/N` 一次，状态行只保留研究者、进度行暂停时不承诺月结推进、消息行不重复同一报告；灭亡状态改为「已无符合资格的继承人，家业无人继承」。`SC021ResearchCostText` 生成器默认文案与 `FoundationScene` 序列化文本同步为当前费用行。聚焦 EditMode `83/83`、全量 EditMode `1105/1105`（均 `0` failed/skipped/inconclusive），场景验证 SC-021 行 `PASS`、整体仍为既有 SC-009 单项 `1` error；编译 `error CS` 为 `0`。测试后 Console 为 `32` 条（`2 errors / 21 warnings / 9 info`），来自负向/故障注入测试与本地 relay 诊断，不是本切片运行回归；本次未运行 Play Mode。
- 2026-11-04 Mochi 无截图 Play Mode：在 Unity `6000.6.6f1` 的现有 feature worktree Editor 中，从 SC-021 标题入口开始；通过测试存档核对开局 `1560-01-01`、资金/物资 `2/2`，安排研究无即时扣费；第一次月结后 `1560-02-01`、资源 `1/1`、进度 `1`；第二次月结后 `1560-03-01`、资源 `0/0`、研究计划清空、`technique.smoke-mixing` 成为家族知识且只有一条研究编年史；继承后家主为 `person.sc021.heir`（影山小太郎）、家族知识保留；再推进一个月没有新增研究记录或研究费用。禁用截图期间无法直接读取画面文案；准确 UI 字符串由 EditMode 回归断言覆盖。没有通过 UI 构造资源短缺暂停路线，Play Mode 未覆盖该分支。Play Mode 后无新 C# 编译错误或运行时异常记录；测试前保存槽及备份均按 SHA-256 原样恢复。此记录是 Mochi 的 Play Mode 证据，不代表用户正式验收。

如需在干净工作区重跑，菜单仍是 `Tools/Sengoku Chronicle/Add SC-021 Family Research`，随后 `Tools/Sengoku Chronicle/Validate Foundation Scene`；生成器要求活动场景为 `Assets/Scenes/FoundationScene.unity`、不在 Play Mode 且未 dirty。

## 1. 用户 Play Test 准备

1. 从标题界面确认原有入口仍在：新游戏、继续、SC-010 标准样例、SC-010 危机示范、SC-020 子女培养示范、SC-022 婚姻与收养示范。
2. 点击「**SC-021 家族研究示范**」。
3. 预期：标题隐藏，地图/时钟显示日期 `1560-01-01`；家族研究面板保持关闭，需由玩家点击「家族研究」打开。

> 说明：SC-021 是标题界面上的独立示范按钮，启动固定影山家剧本；它不是 SC-013「标准样例 / 危机示范」成对按钮的一部分，也不改变其他入口或剧本。

若标题没有 SC-021 入口或日期不是 `1560-01-01`，停止并报告“场景生成/入口失败”。

## 2. 开局固定值

点击「家族研究」打开面板，预期：

| 人物 | 年龄 | 身份 | 研究资格 |
|---|---:|---|---|
| 影山 宗次 | 40 | 家主 | 可担任 |
| 影山 奈绪 | 38 | 配偶 | 可担任 |
| 影山 小太郎 | 18 | 直系亲属 | 可担任 |

- 资源：**资金 2 / 物资 2 / 情报 0**。
- 进度：`0/2（每次月结推进 1 点）`；技艺名「烟幕调制术」；家族知识：尚无记录。
- 费用说明：每月消耗 1 资金、1 物资；完成后写入家族知识，不授予个人掌握。

## 3. 安排研究

1. 点击「影山 宗次」。
2. 预期：状态行只显示研究者「影山 宗次」；进度行显示 `0/2（每次月结推进 1 点）`；不立即扣资源，不增加进度，不写编年史。
3. 再点击「影山 奈绪」。
4. 预期：研究者替换为奈绪，进度仍为 `0/2`；替换研究者不重置已有进度。

## 4. 第一次月结

1. 关闭研究面板，点击「结束本月」。
2. 如出现事件，按界面提示完成选择后再继续。
3. 到达 `1560-02-01` 后重新打开研究面板，预期：
   - 资金 `1`、物资 `1`；
   - 研究进度 `1/2`；
   - 研究者仍为奈绪；
   - 编年史尚无“研究”完成记录（研究只在完成时写入一条可读记录）。

## 5. 资源短缺暂停

1. 不要完成第二次月结；先通过现有产业面板或存读档把资金或物资降到 `0`（若无法安全构造，跳过本节并在结果清单注明“未验证”）。
2. 点击「结束本月」。
3. 预期：
   - 月份仍正常推进；
   - 资源不变成负数，也不部分扣除；
   - 研究进度保持 `1/2`；
   - 暂停只在结果行显示一次：「研究暂停：资金不足 1。进度保持 1/2，本月不推进，也不消耗资源。」（物资短缺时显示「物资不足 1」；两项都缺时合并列出，不会出现「物资不足 0」）；状态行只保留研究者，进度行只显示 `1/2` 且不再承诺月结推进，消息行不再重复同一段暂停报告。
4. 恢复资源到至少 `1/1` 后重试月结；研究应继续。

## 6. 第二次月结与完成

1. 确保资金与物资至少 `1/1`，且研究者仍为有效成年家族成员、未担任产业职位。
2. 点击「结束本月」。
3. 到达 `1560-03-01` 后预期：
   - 资金 `0`、物资 `0`；
   - 面板显示「已完成：烟幕调制术 已记录为家族知识」；
   - 家族知识行显示「烟幕调制术」；
   - 编年史新增一条「研究」记录，摘要含技艺名、研究者姓名、`1 → 2` 进度和完成月份；
   - 编年史不显示 `research.`、`technique.`、`person.` 原始 ID。
   - 面板不再显示研究者按钮，也不再显示「每月消耗 1 资金、1 物资」；费用行改为「该技艺已记录为家族知识，无需再次研究，也不会再产生研究月度费用。」
4. 再次结束月份：预期不会重复完成、不会重复扣研究费用、不会新增第二条研究编年史。

## 7. 无效研究者与暂停

1. 若能从名册或产业面板让研究者死亡、退休、未成年、离开当前家族或担任产业职位，验证该月研究暂停且资源/进度不变。
2. 若无法在 UI 中安全构造这些状态，记录“未验证”，不要把领域测试通过当作 Play Mode 证据。

## 8. 保存/读取

1. 打开现有存读档面板，保存。
2. 读取同一存档。
3. 预期：日期、资源、家族知识「烟幕调制术」保持一致；没有进行中的研究计划，也不会因读档补算研究月份。
4. 再结束一个月，确认不会重复授予知识或重复写编年史。
5. 暂停计划存档：若研究者的状态使其无法继续（死亡、退休、未成年、离开家族或担任产业职位），保存并读取后，打开研究面板应**立即**在结果行看到「研究暂停：<原因>进度保持 <当前进度>/2，本月不推进，也不消耗资源。」，无需先结束月份；状态行只保留研究者，进度行只显示 `P/N` 且不承诺月结推进，消息行不重复同一段暂停报告。存档中没有单独的「暂停」标记：计划仍是活动计划，暂停由当前研究者资格与月度资源即时投影，不修改计划、资源或进度。之后结束月份不得扣资源、推进进度或授予知识。若无法在 UI 中安全构造，记录“未在 UI 验证；由存档/领域测试覆盖”。

## 9. 继承后保留家族知识

1. 打开现有继承面板，点击「退休并安排继承」。
2. 如果界面直接按指定继承人交接，家主应立即变为「影山 小太郎」；如果出现候选选择，选择「影山 小太郎」并确认。
3. 预期：
   - 家族知识「烟幕调制术」仍存在；
   - 新家主不会因此自动获得个人掌握；
   - 当前没有进行中的研究计划；
   - 之后结束月份不会重复完成该技艺。
4. 若继承界面在当前固定剧本不可用，记录“未在 UI 验证；由领域测试覆盖”，不要声称 Play Mode 已验证继承。

## 10. 停止条件

出现以下任一情况立即停止并报告，不继续试玩、不保存场景：

- 标题没有 SC-021 入口，或点击后日期/家族不是影山家 `1560-01-01`。
- 面板不显示三名合格研究者，或显示已记录技艺为「已记录」但开局资金/物资不是 `2/2`。
- 一次成功月结扣除的资金或物资不是恰好 `1/1`，或进度增加不是恰好 `1`。
- 资源不足时出现负数、部分扣费、进度变化或月份卡死。
- 完成时没有新增恰好一条可读研究编年史，或出现原始 ID。
- 读档/继承后家族知识丢失，或新家主自动获得个人掌握。
- 出现 Console error、未处理异常或面板按钮无响应。

## 11. 结果清单（用户填写）

| 项目 | 预期 | 实际 | 通过 |
|---|---|---|---|
| SC-021 标题入口与 `1560-01-01` | 是 |  | [ ] |
| 三名合格研究者 | 3 |  | [ ] |
| 安排/替换研究者保留进度 | 是 |  | [ ] |
| 第一次月结资源 `1/1`、进度 `1/2` | 是 |  | [ ] |
| 资金短缺暂停且不扣费/不推进 | 是 |  | [ ] |
| 物资短缺暂停且不扣费/不推进 | 是 |  | [ ] |
| 第二次月结完成且资源 `0/0` | 是 |  | [ ] |
| 研究编年史可读且无原始 ID | 是 |  | [ ] |
| 完成后不再显示研究者按钮/月度费用 | 是 |  | [ ] |
| 进入 SC-021 后研究面板不自动弹出 | 是 |  | [ ] |
| 暂停计划存读档后不扣费/不推进 | 是 |  | [ ] |
| 重复月结不重复完成 | 是 |  | [ ] |
| 存档/读档保留家族知识 | 是 |  | [ ] |
| 继承保留知识且不授予个人掌握 | 是 |  | [ ] |
| Console errors | 0 |  | [ ] |

用户验收：____________________　日期：____________________

> Play Test 由用户执行并报告；本 worker 不操作 Unity 进行 Play Test、不截图、不代替用户验收。已验证的任务文件按项目规则提交并推送到 feature 分支；只有用户明确接受后，协调方才可合并 `main` 或打 tag。


---

<a id="sc022-demo"></a>
## 来源：SC022_DEMO.md

# SC-022 婚姻与日常收养演示

**状态：** 代码、EditMode 测试、场景集成与助手 Play Mode 测试已完成；累计 checkpoint `714cb3a` 已按用户指示推送到 `origin/main`；用户正式验收状态仍待明确确认
**日期：** 2026-11-04  
**设计：** `docs/superpowers/specs/2026-11-04-sc-022-family-formation-design.md`  
**计划：** `docs/superpowers/plans/2026-11-04-sc-022-family-formation.md`

## 启动

1. 在已打开的 Unity `6000.6.6f1` 项目中打开 `Assets/Scenes/FoundationScene.unity`。
2. 进入 Play Mode，标题界面选择 **「SC-022 婚姻与收养示范」**。
3. 会话从 **1560-05-01** 开始，家族为原创的 **浅羽家**；默认不会自动打开管理面板。

## 婚姻

1. 点击 HUD 左侧 **「婚姻与收养」** 按钮；面板默认隐藏，此时应打开。
2. 在左栏「婚姻」选择 **浅羽 政宗**。信息行应显示其现有配偶 **浅羽 千代**。
3. 候选人列表应只显示合资格的成年非成员：**水无月 佐保**、**白河 千寻**、**浅羽 梅**。
   - **浅羽 宗圆**（直系血亲）和 **浅羽 与一**（直系养亲）不应出现。
   - **浅羽 梅** 是政宗的兄弟姐妹；按批准规则，兄弟姐妹婚姻不被额外禁止。
4. 选择 **水无月 佐保**，确认信息应说明将新增成员与配偶关系，且不可撤销；点击 **「确认登记婚姻」**。
5. 成功后名册/家谱/编年史刷新，佐保从婚姻候选列表消失。
6. 再次选择 **白河 千寻** 并确认；政宗现在有多位正式配偶，千代、佐保、千寻的配偶事实都应保留。
7. 打开 **家族编年史**，应看到「家族关系／婚姻登记」记录，标题与摘要使用人物姓名，不显示原始 ID。

## 普通收养

1. 在右栏「收养」选择 **浅羽 政宗**。
2. 候选人应包含儿童 **小野寺 千松**（8 岁）和成年人 **小野寺 佐吉**（16 岁）。
   - **浅羽 与一** 已有同一方向收养边，不应出现。
   - **浅羽 宗圆** 会造成亲子链循环，不应出现。
   - **浅羽 千代** 是正式配偶，不应出现。
3. 选择 **小野寺 千松**，确认信息应说明新增养子/养女、保留既有血缘事实、不改变继承状态；点击 **「确认办理收养」**。
4. 成功后千松获得 `AdoptedChild` 成员身份和 `Adoption` 有向边；家主、继承阶段、指定继承人、继承次数、危机收养标记均不应变化。
5. 编年史应新增「家族关系／普通收养」记录，摘要包含收养人与候选人姓名。

## 拒绝与原子性

1. 尝试对本人、正式配偶、重复关系、直系血亲/养亲、未成年婚姻候选人、已入家成员或亲子链循环对象提交命令。
2. 面板应显示具体中文原因；失败后成员、亲属边、编年史、继承、日期、资源与 RNG 均不得变化。
3. 有待处理事件或继承决策时，面板应显示暂停原因并禁用确认按钮。

## 保存/读取

1. 完成一次婚姻和一次收养后，使用既有存档按钮保存。
2. 返回标题或重新读取存档。
3. 候选人、成员角色、配偶/收养边、编年史顺序与显示文字应一致；未加入家庭的候选人仍应存在，但不应被误列为家庭成员。

## 证据边界

- 本文件只提供用户 Play Test 步骤。实现 worker 未进入 Play Mode、未运行 Play Test、未截图或使用 Computer Use 操作 Unity。
- 静态检查、Unity 编译、EditMode 测试、Console 计数、`FoundationSceneValidator` 与 Play Mode 结果分别记录在 `docs/reports/sc022-implementation.md`。


---

<a id="sc023-demo"></a>
## 来源：SC023_DEMO.md

# SC-023 隐居与后见人演示

**状态：** SC-023 场景集成、聚焦 EditMode 与代理 Play Mode 已完成；SC-023/SC-021 专项场景检查通过。整体 Validator 仍有一条既存 Feedback banner 布局错误；SC-020 负例因示范家族没有退休成员未能在 UI 中直接复现。等待用户验收，未合并或打 tag。
**日期：** 2026-11-05  
**设计：** `docs/superpowers/specs/2026-11-05-sc-023-seclusion-design.md`  
**计划：** `docs/superpowers/plans/2026-11-05-sc-023-seclusion.md`

## 启动

1. 在已打开的 Unity `6000.6.6f1` 项目中打开 `Assets/Scenes/FoundationScene.unity`。
2. 进入 Play Mode，标题界面选择 **「SC-023 隐居与后见人示范」**。
3. 会话从 **1560-12-01** 开始，家族为原创的 **橘家**：
   - **橘 宗严**（62 岁，现任家主，固定导师专长：学问）
   - **橘 赖久**（32 岁，成年继承人）
   - **橘 千代**（10 岁，赖久之女，固定资质：学问，家庭环境有利）

> 该标题入口需要先由集成执行者在现有 Editor 中运行
> `Tools/Sengoku Chronicle/Add SC-023 Seclusion`，并确认
> `FoundationSceneValidator` 的 SC-023 行通过。本实现 worker 未编辑场景。

## 退休交接

1. 使用现有继承面板发起退休。由于开局已指定成年继承人 **橘 赖久**，交接应立即完成。
2. 确认 **橘 赖久** 成为新家主；**橘 宗严** 保留为退休的直系亲族（`DirectKin`），不再是继承候选。
3. 打开家族名册/家谱，应显示宗严仍在当前家族中，且身份为退休亲属；不得显示为现任家主或继承候选。
4. 导师资格必须由继承编年史中的真实家督交接记录证明：宗严的交接记录应以其为旧家主（`FromHeadId`）。仅有“退休直系亲族”身份不足以成为导师。

## 指定隐居导师

1. 打开现有 **子女培养** 面板；SC-023 会自动打开同一面板，不创建第二套教育 UI。
2. 培养对象应为 **橘 千代**（10 岁）。
3. 选择培养方向 **学问**；导师列表应包含：
   - **橘 赖久**（现任家主）
   - **橘 宗严｜隐居家主／前任家主（学问）**
4. 选择 **橘 宗严**。预览应显示：
   - 方向：学问
   - 导师：橘 宗严（学问·隐居家主／前任家主）
   - 预计：学问 **18 → 27**（基础 +4、资质 +2、导师专长 +2、有利环境 +1）
   - 提交前五项能力、资源和编年史都不应变化。
5. 点击确认提交。当前年度计划应显示导师 **橘 宗严**，但不应立即结算成长。

## 年度结算与编年史

1. 使用现有推进月份按钮，让日期从 **1560-12-01** 进入 **1561-01-01**。
2. 千代的学问应从 **18** 变为 **27**；其他能力不变。
3. 最近结算行应显示方向、导师 **橘 宗严**、实际变化和规则说明。
4. 家族编年史应新增一条教育记录，摘要包含 **橘 千代**、**橘 宗严**、培养方向和实际数值变化；不得显示 `person.sc023.*` 原始 ID。

## 保存/读取

1. 在提交计划后、结算前保存；读取后应恢复同一培养对象、方向、导师 **橘 宗严** 和预览值。
2. 在结算后再次保存并读取；应恢复成长值、结算结果、导师姓名和编年史。
3. 存档 schema 不新增字段；仍使用现有 `development` 计划/结果 DTO。

## 失效回退

1. 如测试环境能构造导师死亡、离族或被移出家庭的状态，保留原计划并推进年结。
2. 年结应按既有「无有效导师则自由成长」规则结算，结果行与编年史应明确显示回退原因。
3. 读取含失效导师计划的存档不应失败；预览、提交、结算与存档校验共用同一资格规则。
4. 若存档中出现“退休但从未担任家主”的 `DirectKin`，该人物不得出现在导师列表；若计划错误引用该人物，存档校验应拒绝，而不是把它当作前任家主或静默回退。

## SC-020 回归

1. 从标题选择 **「SC-020 子女培养示范」**，确认青叶家起始值、导师列表与既有预览不变。
2. SC-020 的退休成员仍不得成为导师；SC-023 的放宽只在其固定家族 ID 下生效。

## 本轮执行结果（2026-11-05）

- Play Mode：SC-023 从 1560-12-01 开始；退休宗严后赖久成为家主，宗严留在家族树中显示为退休直系亲属；千代仍为第三代。
- 教育：学问导师列表包含现任家主赖久和有真实交接记录的隐居前任家主宗严。选宗严后预览为学问 18→27（基础 +4、资质契合 +2、导师专长 +2、有利家庭环境 +1）；提交计划时千代能力值未立即改变。
- 存档：结算前保存/读取保留培养对象、方向、宗严导师与预览；结算后保存/读取保留学问 27、导师和结算结果。
- 年度结算与编年史：推进至 1561-01-01 后学问为 27；编年史显示千代、宗严、18→27 和规则分解，未显示 `person.sc023.*` 原始 ID。
- SC-020 回归入口可启动，现有青叶场景及培养预览可见；其家族树只有家主、配偶与子女，没有退休人物，因此未能直接验证“退休但从未担任家主者不在导师候选”负例。
- Unity Console：两轮 Play Mode 退出后显示 0 errors / 0 warnings / 0 logs。聚焦 EditMode fixture `AddSC013TitleScenarioButtonsTests` 为 4/4 passed（不是全量套件）。
- 场景验证：SC-023 与 SC-021 专项行均 PASS；整体 `FoundationSceneValidator` 仍为 1 error，内容是 `Feedback text must be a top-anchored banner clear of the map HUD controls.`，与本次功能无关，未改动。
- 代理 Play Mode 不等于用户验收；用户验收仍待完成。

## 证据边界

- 本文件记录测试步骤和本轮代理执行结果。代理使用现有 Unity Editor、截图与 Computer Use 执行 Play Mode；这不替代用户验收。
- Unity 编译、EditMode、Console 计数、场景生成/Validator 与 Play Mode 必须分别取得证据；静态源码检查不能替代其中任何一项。
- 详细静态检查与未验证项见 `docs/reports/sc023-implementation.md`。


---

<a id="sc024-demo"></a>
## 来源：SC024_DEMO.md

# SC-024 家训与个人抱负示范：试玩与验收说明

日期：2026-11-06
当前状态更新（2026-11-07）：M01 已由用户明确验收通过。当前 `main` 的 `Assets/Scenes/FoundationScene.unity` 已包含 SC-024 对象（18 处匹配）；以下日期为 2026-11-06 的试玩与验证记录，关于 HEAD 未接入和验收待办的说法仅是当时快照。用户验收不应被解释为下文所有未覆盖路径均已验证。
2026-11-06 历史状态：代码、事件内容、存档 v9、面板与事务式场景生成器已在 `main`。当时 `HEAD` 的 `FoundationScene` 不含 SC-024，而未提交工作区已接入。本文件是纯文本试玩脚本。前一轮聚焦 EditMode 为 76/76；2026-11-06 验证补充已通过全量 EditMode 1184/1184、当时场景 Validator（0 errors）及代理 Play Mode 关键路径。后续确认景继的学问年度计划可提交、信正可选“培养继承人”并显示 0/5；“第三代的家门”弹窗未能通过 Computer Use 选择，未到年度结算。教育成长结算与进度推进未验证，名册视口中的佐近卡片显示需复核。以上为当时记录，不覆盖本文开头的当前状态。
范围：`docs/superpowers/specs/2026-11-05-sc-024-family-creed-ambition-design.md` 与 `docs/superpowers/plans/2026-11-05-sc-024-family-creed-ambition.md` 的纵向切片：一个跨代家训、每任家主一项个人抱负、一次可见的家族议事与存档恢复。**不是完整 M3/M01 验收**；不包含派系、忠诚度、家族投票、随机检定或新的经济系统。

> 以下为 2026-11-06 时的集成说明快照：当时 `HEAD` 尚无 SC-024 场景对象。当前 `main` 已包含该接入；开始新的试玩时应以当前 Unity 场景和 Validator 结果为准，不要按旧快照判断是否缺少场景对象。

## 0. 集成者前置步骤（不属于用户 Play Test）

1. 确认 Unity `6000.6.6f1` 只打开 `C:\Users\panye\My project`，活动场景为 `Assets/Scenes/FoundationScene.unity`，且**不在 Play Mode**。
2. 确认场景没有未保存改动；生成器在场景 dirty、非目标场景或 Play Mode 时会拒绝执行。不要覆盖用户未提交的场景改动。
3. **仅当当前场景缺少 SC-024 对象时**，在 Console 无编译错误后执行菜单 `Tools/Sengoku Chronicle/Add SC-024 Family Doctrine`。当前工作区已经含 `Canvas/FamilyDoctrineRoot`、`Canvas/FamilyDoctrineOpenButton`、`M6TitleRoot/SC024FamilyDoctrineDemoButton`，因此不要重复运行；`HEAD` 场景才需要这一步。
   - 预期 Console：`SC-024 family-doctrine panel validated; FoundationScene saved.`
   - 预期场景：新增 `Canvas/FamilyDoctrineRoot`（保存为 inactive）、`Canvas/FamilyDoctrineOpenButton`（active，`FamilyDoctrinePanel` 组件位于该按钮上）、`M6TitleRoot/SC024FamilyDoctrineDemoButton`。
   - 生成器必须在一个 Undo 组内完成；异常时只能用 Undo 回滚，不得重载场景或手改 YAML。
4. 执行 `Tools/Sengoku Chronicle/Validate Foundation Scene`，打开 `Logs/SceneValidationReport.txt`：
   - 必须出现 `PASS SC-024 family-doctrine panel, title entry, bindings, containment, and layout.`
   - 必须出现 `BOUND GameSession.familyDoctrineDemoButton -> SC024FamilyDoctrineDemoButton (Button)`。
   - 整体结果若仍为 `RESULT: FAIL; Errors: 1`，且唯一错误是既有的 `Feedback text must be a top-anchored banner clear of the map HUD controls.`，记录为“SC-024 专项 PASS；整体仍有既有 SC-009 反馈条错误”，不要声称整体 PASS。
   - 若 SC-024 行不是 PASS，停止，不要进入用户试玩。
5. 记录本次生成/验证的 Unity 版本、Editor PID、场景 SHA-256、Console 计数和 Validator 原文。当前仓库里的 `Logs/SceneValidationReport.txt` 是 2026-11-05 的历史报告，**早于当前未提交的场景接入**，不能直接当作本次结果。M01 修正轮次没有运行 Validator。

### 0.1 M01/SC-024 修正状态（代码已编译并跑过聚焦 EditMode）

- `M2FixedScenario` 的退休门禁是路线感知：`sc024` 路线第一代只要求本路线自己的家训议事事件，第二代也不再依赖由雨夜来客派生的旧客回访；两次既有交接都不等待 SC-013 专属的 pledge/雨夜链。普通 SC-013 路线仍保留原有 pledge + 雨夜来客 + 旧客回访门禁。
- `SC024FamilyDoctrineScenario` 增加一名仅 SC-024 存在的第四代未成年子嗣（`person.sc024.nobumasa-heir`，明智 景继，1560-04-01 时 10 岁），从信正建立父子关系；普通 `M2FixedScenario.CreateSuccession` 仍是原六人。
- 初设继承人仍为正綱；完成两次既有交接后，信正成为家主并自动指定该子嗣。该子嗣是合格的年度教育对象，因此“培养继承人”可在 SC-024 中推进。
- 路线边界已收紧：`ExpectedHeir`、`SyncDesignation`、名册追加和“第四代”标签只在路线为 `sc024` 时使用该子嗣。若家系数据中带有景继但路线是 `sc013` 或空，固定继承人仍按 SC-013 原规则（第三代无固定继承人），家族名册仍为六人。回归用例 `Sc024_FamilyDataKeepsSc013RulesWhenTheRouteIsNotSc024` 覆盖此边界。
- `Assets/Tests/EditMode/SC024M01RegressionTests.cs` 是已跟踪文件（`6958b3a` 新增），不是未跟踪改动。
- **第 0 节代码修正轮的历史结果：** 在打开的 Unity `6000.6.6f1`（PID 90052）中编译成功（`*** Tundra build success`，无 `error CS`）；聚焦 EditMode 76/76 通过（SC024M01 11、State 20、Session 14、Panel 12、Save 12、M2FixedScenario 3、M2StoryGate 4）。该轮尚未运行 Validator 或 Play Mode；后续验证见本文状态与第 8 节。

## 1. 用户 Play Test 准备

1. 在已完成第 0 节的 Unity Editor 中打开 `Assets/Scenes/FoundationScene.unity`，进入 Play Mode。
2. 标题界面应保留既有入口：新游戏、继续、SC-010 标准样例、SC-010 危机示范、SC-020 子女培养示范、SC-021 家族研究示范、SC-022 婚姻与收养示范、SC-023 隐居与后见人示范，以及新增的 **「SC-024 家训与抱负示范」**。
3. 点击 **「SC-024 家训与抱负示范」**。
   - 预期：标题隐藏；日期为 `1560-04-01`；地点为清洲；地图与 HUD 可见。
   - 预期：家族为 SC-013 明智家，并额外包含 SC-024 专属的第四代子嗣；不是新的家族或新的存档。
   - 预期：家训面板**不会自动打开**；需点击 HUD 左侧的 **「家训与抱负」** 按钮。

若标题没有 SC-024 入口，停止并报告“场景生成/入口失败”。若进入后日期不是 `1560-04-01`，也停止。

## 2. 开局固定值

在 Play Mode 中打开「家族名册」核对：

| 人物 | 年龄 | 身份 |
|---|---:|---|
| 明智 晴信 | 70 | 家主 |
| 明智 千恵 | 66 | 配偶 |
| 明智 正綱 | 46 | 直系亲族／指定继承人 |
| 明智 信正 | 22 | 直系亲族 |
| 明智 時姫 | 44 | 旁系亲族 |
| 佐近 | 40 | 家臣 |
| 明智 景继 | 10 | 直系亲族（信正之子；SC-024 专属第四代） |

- 初设指定继承人仍为 **明智 正綱**；景继不会取代开局指定。
- 新局默认家训：**义理至上**。
- 新局默认家主抱负：**发展家业 0/3**（`InitFamilyDoctrineDemoScenario` 自动为晴信选定，不需要玩家再点）。
- 初始家族关系：晴信与正綱 `0`，晴信与信正 `0`。面板不直接显示这两个数值；只能在编年史的关系记录中核对。

## 3. 家族方针面板基线

1. 关闭名册，点击 HUD 左侧 **「家训与抱负」**。
2. 预期面板逐行显示：
   - 标题：`家训与抱负`
   - 家训：`当前家训：义理至上（RighteousnessFirst）`
   - 可选家训：`可选家训：守秘为先 / 义理至上（当前） / 唯才是举`
   - 议事方式：`议事方式：各房先开口；正綱关系 +5，信正关系 +5`
   - 家主：`当前家主：明智 晴信`
   - 抱负：`当前抱负：发展家业 进行中 0/3`
   - 阻止原因：`当前家主已选定抱负，不能更换。`
   - 消息：`家主已选定抱负：发展家业。`
   - 三个抱负按钮：**发展家业**、**传承家技**、**培养继承人**，全部不可点击。
3. 点击「关闭」。面板关闭，不改变日期、家训、关系、资源或编年史。

## 4. 家训议事路径 A：维持义理至上

1. 点击「结束本月」。
   - 预期：日期变为 `1560-05-01`。
   - 预期：事件面板显示 **「各房议事」**，正文为：“家训义理至上。晴信让各房先开口，正綱与信正当众陈述家中近况与难处。”
   - 预期三个选项：
     - **各房先开口**
     - **改为守秘为先**
     - **改为唯才是举**
2. 选择 **「各房先开口」**。
   - 预期事件面板关闭；顶部反馈条显示：“正綱与信正都感到被家主信任，对家主的关系各增加五点；家训仍为义理至上。”
   - 预期家训仍为 **义理至上**。
   - 预期编年史恰好新增 **3** 条：
     1. 事件记录：标题 **各房议事**；
     2. 关系记录：标题 **明智 晴信 与 明智 正綱**，摘要含 `0 → 5（家族议事）`；
     3. 关系记录：标题 **明智 晴信 与 明智 信正**，摘要含 `0 → 5（家族议事）`。
   - 预期不出现 `person.sc013.*`、`event.sc024.*`、`option.sc024.*` 原始 ID。
3. 打开「家训与抱负」面板复核：家训行、议事方式行与第 3 节相同；抱负仍为 `发展家业 进行中 0/3`；阻止原因仍可见。
4. 再点击「结束本月」。
   - 预期：日期变为 `1560-06-01`；不会再次出现“各房议事”。
   - 预期：出现 SC-011 的 **「雨夜来客」**（这是场景中立事件，不属于 SC-024 家训事件）；按既有选项完成即可，不要把它当作 SC-024 失败。

## 5. 家训议事路径 B：改革遍历三种家训

本路径从新的 SC-024 入口重新开始，用来验证“改革后只有新家训的议事版本可提交”。每一步的关系值都是家族关系值（晴信 ↔ 正綱 / 信正），不是 NPC 关系。

1. 重新进入「SC-024 家训与抱负示范」，确认日期回到 `1560-04-01`。
2. 点击「结束本月」，在 **「各房议事」** 中选择 **「改为守秘为先」**。
   - 预期反馈：“正綱与信正对家主的信任各下降五点，家训改为守秘为先。”
   - 预期家训变为 **守秘为先**。
   - 预期编年史新增 3 条：事件 **各房议事**；关系 **明智 晴信 与 明智 正綱** `0 → -5`；关系 **明智 晴信 与 明智 信正** `0 → -5`。
   - 打开面板：`当前家训：守秘为先（SecrecyFirst）`；`议事方式：小范围议事；正綱关系 +5，信正关系 -5`。
3. 点击「结束本月」。
   - 预期：日期 `1560-06-01`；出现 **「密议之夜」**（守秘为先版本），正文为：“家训守秘为先。晴信只召正綱与信正入内室，请二人就近日家中事务各陈所见。”
   - 预期选项：
     - **小范围议事**
     - **改为义理至上**
     - **改为唯才是举**
4. 选择 **「改为唯才是举」**。
   - 预期反馈：“正綱与信正对家主的信任各下降五点，家训改为唯才是举。”
   - 预期家训变为 **唯才是举**。
   - 预期编年史新增 3 条：事件 **密议之夜**；关系 **明智 晴信 与 明智 正綱** `-5 → -10`；关系 **明智 晴信 与 明智 信正** `-5 → -10`。
   - 打开面板：`当前家训：唯才是举（MeritFirst）`；`议事方式：按所长分工；正綱关系 -5，信正关系 +5`。
5. 点击「结束本月」。
   - 预期：日期 `1560-07-01`；出现 **「按才分议」**（唯才是举版本），正文为：“家训唯才是举。晴信请正綱与信正各陈所长，按才干重新分派家中事务。”
   - 预期选项：
     - **按所长分工**
     - **改为守秘为先**
     - **改为义理至上**
6. 选择 **「按所长分工」**。
   - 预期反馈：“正綱因职责加重而对家主信任下降五点，信正因才干被重用而上升五点；家训仍为唯才是举。”
   - 预期家训仍为 **唯才是举**。
   - 预期编年史新增 3 条：事件 **按才分议**；关系 **明智 晴信 与 明智 正綱** `-10 → -15`；关系 **明智 晴信 与 明智 信正** `-10 → -5`。
   - 此时三种家训版本各出现一次；再结束月份不会重复这三个家训事件。

> 若在第 4 步或第 6 步看到旧家训版本的选项仍可提交，或改革后家训/关系/编年史三者不一致，停止并报告“改革原子性失败”。

## 6. 发展家业抱负路径

本路径从新的 SC-024 入口重新开始，保持默认 **义理至上** 与 **发展家业 0/3**。

1. 进入 SC-024，点击「结束本月」，在 **「各房议事」** 中选择 **「各房先开口」**。日期应为 `1560-05-01`，关系 `0 → 5` 各一条。
2. 打开 **「产业」** 面板。
   - 在 **农庄** 行选择 **明智 晴信**。
   - 预期：农庄任命显示 **明智 晴信**；资源、日期、编年史与抱负进度都不立即变化。
3. 关闭产业面板，点击「结束本月」。
   - 预期：日期 `1560-06-01`；产业完成一次月结；抱负进度变为 **1/3**。
   - 预期：出现「雨夜来客」时，选择任一可用选项完成；这不影响抱负进度。
4. 再点击「结束本月」。
   - 预期：日期 `1560-07-01`；抱负进度 **2/3**。
5. 再点击「结束本月」。
   - 预期：日期 `1560-08-01`；抱负进度 **3/3**，面板显示 `当前抱负：发展家业 已完成 3/3`。
   - 预期：编年史恰好新增 **1** 条完成记录：
     - 标题：**明智 晴信的抱负达成**
     - 摘要：`家主明智 晴信完成抱负「发展家业」：进度 3/3。未获得额外资源或能力。`
   - 预期：不额外发放资源或能力。
6. 再点击「结束本月」。
   - 预期：日期继续推进；抱负仍为 `3/3`，不会新增第二条“明智 晴信的抱负达成”。

说明：农庄月结的资源数值沿用 SC-019 既有规则；本 demo 以抱负进度和完成编年史为 SC-024 通过条件，不把资源数值差异归因于 SC-024。若产业月结本身被阻挡（空缺、短缺或未运营），先按 SC-019 的说明处理，不要把“未推进”直接判为 SC-024 缺陷。

## 7. 保存/读取

1. 在路径 A 的议事选择完成后（例如 `1560-05-01`，无待处理事件）打开现有存读档面板，保存。
2. 读取同一存档。
   - 预期：日期、家训、面板中的议事方式、抱负进度和编年史条数完全一致。
   - 预期：不会重复议事事件，也不会重复关系记录。
3. 若在路径 B 改革后保存并读取，再结束月份：
   - 预期：只出现当前家训对应的议事版本；旧家训版本不可提交。
4. 在路径 B 完成 `3/3` 后保存并读取：
   - 预期：抱负仍为 `已完成 3/3`；完成编年史仍只有 1 条；再结束月份不重复完成。
5. 若读取后家训回到默认值、抱负丢失、重复议事或编年史重复，停止并报告“存档/路线恢复失败”。
6. 本切片存档应为 schema v9、`rulesVersion 5`、事件内容版本 3；普通 v8 存档迁移后应为默认家训 **义理至上**、无抱负、无 SC-024 路线。用户无需手改 JSON；由现有存读档面板完成。

## 8. 第二代/第三代与其余抱负路径（关键部分已 Play Mode 验证）

设计稿要求覆盖三类抱负与至少两代交接。2026-11-06 代理 Play Mode 已实际完成两次既有交接，依次由晴信交给正綱、再交给信正；第二次交接后名册显示第四代景继为指定继承人。正綱选择“传承家技”后，界面显示 1/1 完成。存档与读档也已在该路线验证成功，读档恢复至 1560-08-01、信正为家主。后续验证确认景继可提交学问与武艺年度教育计划；首年结算后学问成长 +4、信正“培养继承人”进度达到 4/5，第二年结算后抱负面板显示“已完成 5/5”。“发展家业 3/3”未验证；最终里程碑验收仍待办。家族名册显示多代关系连线与五张可见人物卡；当前已检查的滚动位置未能确认景继卡片，不足以判定图中缺少该节点，需复核完整滚动范围。

### 8.1 传承家技（需要新家主）

1. 完成一次交接，使 **明智 正綱** 成为家主，且他尚未选定抱负。
2. 打开「家训与抱负」：预期 `当前抱负：尚未选定。`，三个抱负按钮可用。
3. 点击 **「传承家技」**：预期消息 `家主已选定抱负：传承家技。`；抱负行 `传承家技 进行中 0/1`；三个按钮变为不可点击。
4. 打开「家族研究」，选择 **明智 正綱** 为研究者，选择 **烟幕调制术**；连续两次成功月结后研究完成。
5. 预期：抱负进度变为 `1/1`；编年史新增 **明智 正綱的抱负达成**，摘要含 `传承家技`、`1/1`、`未获得额外资源或能力`；选择抱负前已拥有的技艺不得重复计数。

> M01 修正后，SC-024 的两次既有交接都不再依赖 SC-013 的 pledge、雨夜来客或由雨夜来客派生的旧客回访。若仍看到“继承前的家议”或“旧客回访”阻止原因，停止并记录为路线门禁回归，不要用 Console/调试绕过。

### 8.2 培养继承人（第四代子嗣）

1. 设计目标是当前指定继承人的年度教育正向成长累计 5 点。
2. SC-024 现在额外包含 **明智 景继**（`person.sc024.nobumasa-heir`，开局 10 岁，信正之子）。正綱与信正仍依次作为前两代继承人。
3. 完成两次既有交接后，信正成为家主，景继成为指定继承人；此时「培养继承人」与年度教育计划都应以景继为对象。
4. 预期：为景继提交年度培养计划后，年度结算产生正向成长并推进抱负；普通 SC-013 仍只有原六人，不应出现景继。若 SC-013 出现该子嗣，停止并记录为场景隔离失败。
5. 若在 UI 中无法选定该抱负或无法把景继作为教育对象，记录为“教育/指定继承人缺口”，不要判为通过。

## 9. 既有入口回归

1. 结束 Play Mode，重新进入标题。
2. 依次确认「新游戏」「SC-010 标准样例」「SC-010 危机示范」「SC-020 子女培养示范」「SC-021 家族研究示范」「SC-022 婚姻与收养示范」「SC-023 隐居与后见人示范」仍可进入。
3. 预期：SC-024 的家训、抱负、关系或面板不会出现在上述入口；SC-013 标准/危机入口行为与既有一致。
4. 若 SC-024 污染其他剧本，或既有入口被替换/失效，停止并报告。

## 10. 停止条件

出现以下任一情况立即停止，不继续推月、不覆盖存档、不保存场景：

- 标题没有 SC-024 入口，或进入后日期不是 `1560-04-01`。
- 面板缺少三行家训、三种抱负、当前家主或阻止原因；打开面板改变游戏状态。
- 第一个月没有出现当前家训对应的议事事件，或选项与当前家训不匹配。
- 维持/改革后的关系值、家训或编年史条数与第 4/5 节不符；同一变化被记录两次。
- 改革后旧家训事件仍可提交，或家训/关系/编年史不是一次原子提交。
- 发展家业在有合格农庄月结时没有推进，或完成记录重复/超过 `3/3`。
- 存读档后家训、抱负、事件路线、关系或编年史丢失/重复。
- UI 退休按钮仍被“继承前的家议”“雨夜来客”或“旧客回访”挡住，即使本路线家训议事已完成（记录为路线门禁回归）。
- 出现 Console 红色错误、未处理异常、按钮无响应，或需要 Console/调试代码才能继续。
- 整体 Validator 的 SC-024 行不是 PASS，或场景被保存/修改。

## 11. 结果清单（纯文本报告）

```text
SC-024 Play Test
Project / branch:
Unity version:
Scene:
SC-024 生成器已运行: 是 / 否
SC-024 Validator 行 PASS: 是 / 否 / 未运行
整体 Validator: PASS / FAIL（错误数：____；原文：____）
标题入口「SC-024 家训与抱负示范」: 是 / 否
开局日期 / 家族: ____ / ____
面板默认家训 / 抱负: ____ / ____
议事事件标题 / 选项: ____ / ____
路径 A 维持义理: 家训____ 正綱__→__ 信正__→__ 编年史条数__
路径 B 改革: 守秘____ 唯才____ 家训最终____ 正綱__→__ 信正__→__ 编年史条数__
旧家训选项改革后不可提交: 是 / 否
发展家业: 0/3 → __/3 → __/3；完成记录条数__
完成摘要原文: ____
存读档（议事前后）: 通过 / 失败（差异：____）
存读档（抱负完成前后）: 通过 / 失败（差异：____）
传承家技路径: 通过 / 未到达（说明：____）
培养继承人路径: 通过 / 未到达 / 教育对象不可用（说明：____）
既有入口回归: 通过 / 失败（说明：____）
Console error 数: ____
Console warning 数: ____
失败或异常原文: ____
结论: 通过 / 不通过
用户验收: 接受 / 不接受 / 待定
日期:
```

## 12. 证据边界

- 本文件中的“预期”是实现约定，不是已经取得的运行证据。
- 2026-11-06 后续验证：全量 EditMode 1184/1184 通过；当前未提交工作区场景的 `FoundationSceneValidator` 为 PASS、0 errors；代理 Play Mode 验证家训议事关系变化、两次交接、景继指定、传承家技 1/1 与同实例存读档。续测验证两年年度教育计划均成功提交，首年学问成长 +4、抱负由 0/5 到 4/5，第二年结算后“培养继承人 已完成 5/5”。退出 Play Mode 后 Console 计数为 0 logs / 0 warnings / 0 errors；EditMode 期间两条错误日志来自通过的回滚测试。名册可见多代连线与五张人物卡；当前检查范围未确认景继节点是否位于更下方的可滚动区域，不据此判定缺失；佐近卡片仍未确认。用户存档与备份 SHA-256 均为 `F1C0630E5DD91BAA0B18607CA4367956DB637A8BB4DD6DA21D1E79212BDA4CBE`。M01 最终验收待办。场景状态仍须区分 `HEAD`（无 SC-024 对象）与当前未提交工作区（已有接入）。
- 既有的独立代理 Play Mode 补充记录在 `docs/reports/sc024-aid-ending-save-load-playtest-2026-11-06.md`：它记录的是当前工作区未提交场景接入下的一次伊织援助结局与存读档路径，**属于代理 Play Mode，不等于用户 Play Test 或用户接受**。
- 编译、EditMode、Console、场景生成/Validator 与 Play Mode 必须分别记录。历史 20/20、63/63、10/10、70/70 都是早先 worker 的日志，不是本轮运行；其中 70/70 早于 `main` 的合并解决，不能直接引用为当前通过。
- Play Mode 验证由协调方在授权下执行；代理 Play Mode 不替代用户的最终里程碑接受。只有用户明确接受后，协调方才能创建最终 checkpoint、合并 `main` 或打 tag。
