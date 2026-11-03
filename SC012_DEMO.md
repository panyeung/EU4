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
