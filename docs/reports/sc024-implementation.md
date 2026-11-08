# SC-024 家训与个人抱负实现报告

**日期：** 2026-11-06
**分支：** `main`
**范围：** `docs/superpowers/specs/2026-11-05-sc-024-family-creed-ambition-design.md` 与 `docs/superpowers/plans/2026-11-05-sc-024-family-creed-ambition.md` 的代码、事件内容、存档 v9、面板与事务式场景生成器。
**本报告性质：** 文档与验证报告。原始文档轮次的撰写者没有修改 C#、测试或其他代码；M01 修正与复审判定轮次修改了第 0 节所列 C#/测试文件，并在打开的 Unity Editor 中运行了编译与聚焦 EditMode。Unity 场景、`ProjectSettings`、`Library`、`Temp` 未被本轮修改。

## 当前状态更新（2026-11-07）

用户已明确验收 M01。当前 `main` 的 `FoundationScene` 已提交 SC-024 场景接入（`SC024`/`FamilyDoctrine` 匹配 18 处；提交 `18c7b4c`）。下方 2026-11-06 验证结果和历史问题仍按当时范围记录；用户验收不补充未记录的测试步骤、结果或覆盖率。

## 当前验证补充（2026-11-06）

后续在同一 Unity `6000.6.6f1` Editor（PID 90052，`C:\Users\panye\My project`）中完成验证。与下方“修正与复审判定”历史记录分开：

- 全量 EditMode：`TestResults.xml` 为 1184/1184 通过、0 失败；`Logs/Editor.log` 有 Tundra build success。EditMode 期间两条红色日志来自通过的编年史失败回滚用例；不能据此声称测试全程 Console 零错误。
- 当前工作区 FoundationScene Validator：`PASS SC-024 family-doctrine panel, title entry, bindings, containment, and layout.`；`RESULT: PASS; Errors: 0`，报告时间 2026-11-06 15:45。
- 代理 Play Mode：家训议事后正綱与信正关系各从 0 到 5，编年史记录事件和两条关系变化；晴信→正綱→信正两次交接成功，第二次后景继（10 岁）为指定继承人；正綱的“传承家技”抱负显示 1/1 完成；保存与读档成功，恢复到 1560-08-01、信正为家主。退出 Play Mode 后 Console 计数 0 logs / 0 warnings / 0 errors。
- 测试曾写入本地存档槽；退出 Play Mode 后已从任务备份恢复 `save-slot-1.json` 与 `.bak`，两者 SHA-256 均为 `F1C0630E5DD91BAA0B18607CA4367956DB637A8BB4DD6DA21D1E79212BDA4CBE`。临时启动脚本、触发文件、存档备份和截图均已清理。
- 家族名册当前视口只确认五张人物卡，未确认佐近卡片是否在下方；滚轮输入未带来可见位移，列为待复核显示项。后续运行已提交景继学问年度计划并为信正选择“培养继承人”（0/5）；“第三代的家门”弹窗未能通过 Computer Use 选择，年度结算、成长和抱负推进仍未验证。“发展家业 3/3”及三类抱负完整路径也未覆盖。
- 本轮只完成验证记录，没有提交或推送。`FoundationScene` 接入仍仅在未提交工作区；M01 最终里程碑验收待用户明确接受。

## 0. M01/SC-024 修正与复审判定（2026-11-06）

本轮在既有 `main` 工作区完成以下修正，并使用已打开的 Unity `6000.6.6f1`（PID 90052，项目路径 `C:\Users\panye\My project`）取得编译与聚焦 EditMode 结果。没有运行 `FoundationSceneValidator`、Play Mode 或 batchmode。

1. `M2FixedScenario.CanBeginRetirement` 增加路线参数。`sc024` 路线第一代只要求本路线任一 `event.sc024.creed-council.*` 已提交，第二代也不再要求由雨夜来客派生的 SC-011 旧客事件；因此两次既有交接都不依赖 SC-013 的 pledge/雨夜链。无路线（普通 SC-013）仍保留原有 pledge + 雨夜来客 + 旧客回访门禁。
2. `SC024FamilyDoctrineScenario.CreateSuccession` 在复用六人明智家后追加一名仅 SC-024 存在的第四代子嗣 `person.sc024.nobumasa-heir`（明智 景继，1550 年生，1560-04-01 时 10 岁），身份 `DirectKin`，父子关系来自 `person.sc013.nobumasa`。普通 `M2FixedScenario.CreateSuccession` 仍是六人。
3. 初设继承人仍为正綱。第二次既有交接后，信正成为家主，路线感知的 `ExpectedHeir`/`SyncDesignation` 把景继设为指定继承人；家族名册、`FixedHeirSummary`、教育视图与存档往返都使用同一路线与人物 ID。
4. **复审第 3 点修正：** 收紧路线边界。`ExpectedHeir` 不再因“家系里恰好存在景继”而回退到第四代；名册的额外人物追加和“第四代”标签也只在 `sc024` 路线生效。普通 SC-013 或空路线即使载入带有景继的家系数据，仍输出原六人名册，第三代固定继承人为空，符合原剧本规则。
5. `FamilyDoctrinePanelTests` 的 `sc024` 路线不再期待 pledge，改为雨夜来客；`FamilyDoctrineSessionTests` 的 SC-024 成员数更新为 7，并断言普通 SC-013 仍为 6。
6. `Assets/Tests/EditMode/SC024M01RegressionTests.cs` 是**已跟踪文件**（提交 `6958b3a` 新增，HEAD 中 404 行），不是未跟踪改动。本轮在其上新增 `Sc024_FamilyDataKeepsSc013RulesWhenTheRouteIsNotSc024`（复审第 4 点），该文件共 11 个用例。
7. 运行结果：`Logs/Editor.log` 记录 `*** Tundra build success` 且该区间无 `error CS`；聚焦 EditMode 76/76 通过，`TestResults.xml` 的 `test-run` 为 `testcasecount="76" total="76" passed="76" failed="0"`，起始时间 `2026-11-07 00:29:32Z`。分项：`SC024M01RegressionTests` 11、`FamilyDoctrineStateTests` 20、`FamilyDoctrineSessionTests` 14、`FamilyDoctrinePanelTests` 12、`FamilyDoctrineSaveTests` 12、`M2FixedScenarioTests` 3、`M2StoryGateTests` 4。
8. 场景接入、当前 Validator、Play Mode 与用户验收仍不在本轮范围；不属于本轮运行结果。

本轮代码改动文件：

- `Assets/Scripts/Application/M2FixedScenario.cs`（路线感知门禁、指定继承人、名册与代数；复审后收紧无路线回退）
- `Assets/Scripts/Application/SC024FamilyDoctrineScenario.cs`（SC-024 专属第四代子嗣与路线辅助）
- `Assets/Scripts/Application/GameSession.cs`（把活动路线传入共享门禁/名册/指定继承人调用）
- `Assets/Tests/EditMode/FamilyDoctrinePanelTests.cs`（`sc024` 路线 pledge 期待改为雨夜来客）
- `Assets/Tests/EditMode/FamilyDoctrineSessionTests.cs`（SC-024 成员数 7；普通 SC-013 仍 6）
- `Assets/Tests/EditMode/SC024M01RegressionTests.cs`（已跟踪回归文件；本轮新增路线边界用例，无新增 `.meta`）
- `SC024_DEMO.md`、`PROJECT_STATE.md`、本报告（状态与证据）

## 1. 当前结论

SC-024 的领域状态、家训议事事件、抱负推进、存档 v9、纯视图、uGUI 面板和事务式生成器已进入 `main`。M01 演示的原始缺口与当前状态：

1. **场景状态必须区分 HEAD 与工作区：** `git show HEAD:Assets/Scenes/FoundationScene.unity | grep -c "SC024\|FamilyDoctrine"` 为 `0`，即已提交场景不含 SC-024；当前**未提交工作区**场景有 17 处匹配，含 `Canvas/FamilyDoctrineRoot`、`Canvas/FamilyDoctrineOpenButton`（`FamilyDoctrinePanel`）与 `M6TitleRoot/SC024FamilyDoctrineDemoButton`。工作区接入不是 `origin/main` 内容，也不是本轮编辑产物。SC-024 场景生成结果从未进入提交：`3d81e58` 的文件清单不含 `FoundationScene.unity`；`main` 的合并 `5885315` 又把该文件列为冲突，合并结果不含 SC-024。
2. **当前 Validator 证据缺失：** 仓库中的 `Logs/SceneValidationReport.txt` 曾记录 SC-024 PASS，但该报告早于当前未提交工作区场景接入；本报告不把该历史报告当作当前 Validator 结果，本轮也没有运行 Validator。
3. **继承门禁阻塞（已由第 0 节修正）：** 原始实现中 SC-024 使用 `sc024` 事件路线，但 UI 退休仍走 `M2FixedScenario.CanBeginRetirement`，该门禁要求 `event.sc013.inheritance-pledge`；该事件在目录中标记为 `scenario: "sc013"`，`EventEngine.ScenarioMatches` 要求场景标签完全一致，因此正常 UI 无法在 SC-024 中满足第一代退休条件。修正后 `sc024` 路线改用自己的家训议事事件，普通 SC-013 门禁不变。
4. **测试/合并不一致（已由第 0 节修正并运行）：** `main` 的 `FamilyDoctrinePanelTests.AdvanceToFirstSuccession` 第 384 行在 `sc024` 路线中期待 `SC013EventIds.PledgeEventId`；这与上述场景门禁静态不一致。历史 70/70 日志对应的是合并前使用 `SC024EventIds.PledgeEventId` 的测试版本。本轮把该路线改为雨夜来客，并在 Unity 中跑出聚焦 76/76；`SC024M01RegressionTests.cs` 自 `6958b3a` 起就是已跟踪文件。
5. **用户验收缺失：** `docs/reports/sc024-aid-ending-save-load-playtest-2026-11-06.md` 是既有的独立代理 Play Mode 补充记录（伊织援助结局与同实例存读档），它不等于用户 Play Test 或用户接受；`20/20`、`63/63`、`10/10`、`70/70` 都是历史 worker 日志。

截至上述代码修正与复审轮次，状态应表述为：**代码/内容已实现；HEAD 场景未接入，工作区有未提交接入；该轮编译与聚焦 EditMode 通过；该轮 Validator 与 Play Mode 尚未运行。** 后续验证结果见本文开头的“当前验证补充”；M01 最终验收仍待办。

`GDD_v4.0.md` §M01 要求从开局完成两次家主交接，并确认关键关系、家业、编年史和资源在继承及读档后正确。后续代理 Play Mode 已完成两次交接、家训关系变化及同实例存读档的关键路径，但未覆盖教育抱负路线，且名册视口仍有显示项待复核；工作区场景也尚未进入提交。因此这些结果是 M01 验证证据，不等于最终里程碑验收。最终验收仍需用户明确接受。

## 2. 已实现的规则

### 2.1 固定剧本与默认值

- `SC024FamilyDoctrineScenario.ScenarioId = "sc024"`。
- 复用 `M2FixedScenario` 的六人明智家、稳定人物 ID 与 `1560-04-01` 起始日期；不复制人物或亲属图。M01 修正后仅 SC-024 追加第四代子嗣 `person.sc024.nobumasa-heir`（明智 景继，1550 年生），普通 SC-013 仍为六人。
- `GameSession.InitFamilyDoctrineDemoScenario()` 初始化后：
  - 家训为 `FamilyCreedKind.RighteousnessFirst`（义理至上）；
  - 当前家主晴信自动拥有 `FamilyAmbitionKind.DevelopHousehold`（发展家业）`0/3`；
  - 活动事件路线为 `sc024`；
  - `LastFamilyDoctrineMessage = "家主已选定抱负：发展家业。"`。
- 普通 SC-013「新游戏」不触发 SC-024 事件；SC-024 事件只在 `scenario = sc024` 时排程。

### 2.2 家训议事事件

三个稳定事件，均 `Monthly`、`PerGame`、`priority: 1000`、`cooldownMonths: 0`：

| 当前家训 | 事件 ID | 事件标题 | 维持选项关系 | 改革效果 |
|---|---|---|---|---|
| 守秘为先 | `event.sc024.creed-council.secrecy` | 密议之夜 | 正綱 +5，信正 -5 | 改义理或唯才时两人各 -5 |
| 义理至上 | `event.sc024.creed-council.righteousness` | 各房议事 | 正綱 +5，信正 +5 | 改守秘或唯才时两人各 -5 |
| 唯才是举 | `event.sc024.creed-council.merit` | 按才分议 | 正綱 -5，信正 +5 | 改守秘或义理时两人各 -5 |

- 条件使用新 `FamilyCreedIs`，效果使用新 `FamilyRelationshipDelta` 与 `SetFamilyCreed`。
- 改革提交在同一事务中写入：事件记录 1 条 + 两名家族成员关系记录各 1 条；家训、关系和编年史同成同败。
- 非当前家训的事件不排程；改革后只有新家训版本可提交。
- 维持/改革的关系目标是既有 `person.sc013.masatsuna`（正綱）与 `person.sc013.nobumasa`（信正）。

### 2.3 个人抱负

| 抱负 | 稳定 ID | 目标 | 推进来源 | 不计入的情况 |
|---|---|---:|---|---|
| 发展家业 | `DevelopHousehold` | 3 个合格月 | 成功月结中至少一条产业 `Operated`，且三类资源净变化总和 > 0，且无短缺 | 空缺、短缺、未运营、净和 ≤ 0；同一月只计一次 |
| 传承家技 | `PassDownTechnique` | 1 项新技艺 | 选择抱负时记录已知技艺快照；此后新完成并记录的家族技艺 | 选择前已知技艺、同一技艺重复、未完成研究 |
| 培养继承人 | `EducateHeir` | 5 点正向成长 | 当前指定继承人的年度教育结果中五项能力正向增量之和 | 非指定继承人、无正向成长、自由成长回退、同一 `(heir, ageYear)` 重复 |

- 每位家主只有一项抱负，绑定稳定人物 ID；旧家主记录保留，不转移给新家主。
- 家训跨继承保留。
- 达到目标只追加一条完成编年史，不发放额外资源或能力。
- 研究仍由 `FamilyResearchSettlement` 唯一结算；教育仍在 12→1 月年结；抱负只消费既有报告/结果，不另跑月结。

### 2.4 存档

- `SaveData.saveFormatVersion = 9`；`rulesVersion = 5`；事件内容 `contentVersion = 3`。
- 新增 `familyDoctrine`：`creedId` + 按 head ID 排序的 `ambitions[]`（抱负 ID、进度、目标、完成态、选择时已知技艺快照）。
- 新增 `activeEventScenarioId`；SC-024 存档必须恢复 `sc024`，否则议事后的待处理事件/历史会被场景校验拒绝。
- v8→v9 迁移：家训初始化为 `RighteousnessFirst`、抱负为空、事件路线为空；保留既有 SC-018/019/020/021/022/023 数据，不补算历史。
- 校验拒绝未知/重复 head、未知家训/抱负 ID、目标不符、进度越界、完成态不一致、技艺基线不在当前家族知识中，以及旧版本携带 v9 专有块。

### 2.5 面板与场景生成器

- `FamilyDoctrineView`：纯只读投影，生成中文家训/议事效果/抱负进度和阻止原因。
- `FamilyDoctrinePanel`：绑定在 active 的 `FamilyDoctrineOpenButton` 上；面板根 `Canvas/FamilyDoctrineRoot` 保存为 inactive；只通过 `GameSession.TryStartCurrentHeadAmbition` 提交抱负，不在面板直接改家训。
- `AddSC024FamilyDoctrine`：一个 Undo 组、保存前 `Validate`、异常 `Undo.RevertAllDownToGroup`、不重载场景；固定根/控件/标题入口命名；检查 Canvas 内含、控件不重叠、HUD 不重叠、标题入口不压 SC-023 等既有控件。
- 标题入口标签：`SC-024 家训与抱负示范`；打开按钮标签：`家训与抱负`。

## 3. 文件清单（SC-024 已提交）

### 新增

- `Assets/Scripts/Domain/FamilyDoctrineState.cs` 与 `.meta`
- `Assets/Scripts/Application/FamilyDoctrineView.cs` 与 `.meta`
- `Assets/Scripts/Application/SC024FamilyDoctrineScenario.cs` 与 `.meta`
- `Assets/Scripts/Presentation/FamilyDoctrinePanel.cs` 与 `.meta`
- `Assets/Scripts/Editor/AddSC024FamilyDoctrine.cs` 与 `.meta`
- `Assets/Tests/EditMode/FamilyDoctrineStateTests.cs` 与 `.meta`
- `Assets/Tests/EditMode/FamilyDoctrineSessionTests.cs` 与 `.meta`
- `Assets/Tests/EditMode/FamilyDoctrineSaveTests.cs` 与 `.meta`
- `Assets/Tests/EditMode/FamilyDoctrinePanelTests.cs` 与 `.meta`
- `Assets/Tests/EditMode/AddSC024FamilyDoctrineTests.cs` 与 `.meta`
- `docs/superpowers/specs/2026-11-05-sc-024-family-creed-ambition-design.md`
- `docs/superpowers/plans/2026-11-05-sc-024-family-creed-ambition.md`
- `docs/reports/sc024-reuse-research.md`
- `docs/reports/sc024-github-reuse.md`

### 修改

- `Assets/Scripts/Events/EventCatalogData.cs`（家训/抱负 ID、`FamilyCreedIs`、`SetFamilyCreed`、`SC024EventIds`）
- `Assets/Scripts/Events/EventCatalogValidator.cs`
- `Assets/Scripts/Events/EventEngine.cs`
- `Assets/Scripts/Application/GameSession.cs`
- `Assets/Scripts/Persistence/SaveData.cs`
- `Assets/Scripts/Persistence/SaveDataValidator.cs`
- `Assets/Scripts/Persistence/SaveDataMigrator.cs`
- `Assets/Scripts/Domain/AssemblyInfo.cs`
- `Assets/Scripts/Presentation/TitleScreenController.cs`
- `Assets/Scripts/Editor/AddM6TitleScreen.cs`
- `Assets/Scripts/Editor/AddSC013TitleScenarioButtons.cs`
- `Assets/Scripts/Editor/FoundationSceneValidator.cs`
- `Assets/Resources/Events/event-catalog-v1.json`（3 个 SC-024 事件、9 个选项）
- `Assets/Resources/Localization/event-text-zh-v1.json`（21 条 SC-024 文本）

### 未提交到 `main` 的场景状态

- `Assets/Scenes/FoundationScene.unity` 在 `main` 中**没有** SC-024 对象；不是“已接入但隐藏”，而是对象不存在。
- `Logs/sc024-scene-integration-2026-11-05.txt` 记录的生成器成功、SHA-256（`2b584518…` → `be77a832…`）与 2149 行插入，是 2026-11-05 的本地历史，不是当前提交内容。
- 合并 `5885315` 的冲突文件列表包含 `Assets/Scenes/FoundationScene.unity`；合并后的场景没有 SC-024 行。该合并同时把 `FamilyDoctrinePanelTests` 中原本的 `SC024EventIds.PledgeEventId` 改成 `SC013EventIds.PledgeEventId`。

## 4. 证据台账

| 门 | 已记录的历史证据 | 当前状态 |
|---|---|---|
| Unity 编译（本轮） | 打开的 Unity `6000.6.6f1`（PID 90052）在本轮脚本刷新后记录 `*** Tundra build success`；新增/修改脚本的编译区间没有 `error CS`。历史 `Logs/Editor.log` 第 167825–167836 行的 `CS0117` 属于合并前版本。 | **本轮已运行并通过编译**；不是 EditMode/Play Mode 证据。 |
| EditMode（本轮聚焦） | `TestResults.xml`：`test-run testcasecount="76" total="76" passed="76" failed="0"`，起始 `2026-11-07 00:29:32Z`，Editor PID 90052。分项：SC024M01 11、State 20、Session 14、Panel 12、Save 12、M2FixedScenario 3、M2StoryGate 4。 | **本轮已运行并通过 76/76**；仅覆盖所列聚焦套件，不是全量 1184 项。 |
| EditMode（历史） | 按钮修复轮 20/20（`aac3679`）；合并前聚焦 63/63（`Logs/sc024-focused-editmode-tests.xml`，2026-11-06T04:35Z）；标题引用 10/10（04:44Z）；新家主反馈 70/70（`Logs/sc024-newhead-feedback-tests.json`，合并前测试版本）。 | **历史记录**；不作为本轮结果。 |
| Console | 历史记录：生成器+Validator 后 0 errors / 0 warnings / 1 log；标题引用 follow-up 后 0 / 1 / 7；新家主修复后 0 errors / 1 warning。 | **本轮未在测试结束前单独读取 Console 计数**；编译无 `error CS`，聚焦测试 0 failed，`Editor.log` 仅有 Unity AI Assistant `NoSubscription` 的既有追踪异常。不得把历史计数当作本轮 Console 证据。 |
| `FoundationSceneValidator` | `Logs/SceneValidationReport.txt` 第 39 行历史 `PASS SC-024 family-doctrine panel, title entry, bindings, containment, and layout.`；第 29 行既有 SC-009 反馈条错误；第 582 行 `RESULT: FAIL; Errors: 1`。 | **本轮未运行**。该报告早于当前未提交工作区场景接入；HEAD 场景无 SC-024 对象，工作区场景有接入，但两者都不能引用这份历史报告作为当前 PASS。 |
| Play Mode | 独立代理补充记录 `docs/reports/sc024-aid-ending-save-load-playtest-2026-11-06.md`：当前工作区未提交场景接入下，伊织援助结局与同实例存读档路径通过；未验证重启应用后的载入。 | **代理 Play Mode 已记录，但本轮未运行 Play Mode**；不等于用户 Play Test 或用户接受。 |
| 用户验收 | 无。 | **待用户 Play Test 与明确接受**。 |

## 5. 当前已知缺口与风险

### 5.1 HEAD 场景对象从未提交，当前工作区已有未提交接入

- `git show HEAD:Assets/Scenes/FoundationScene.unity | grep -c "SC024\|FamilyDoctrine"` 的结果为 `0`。
- 当前工作区 `Assets/Scenes/FoundationScene.unity` 有 17 处匹配，含 `FamilyDoctrineRoot`、`FamilyDoctrineOpenButton`、`SC024FamilyDoctrineDemoButton` 与 `FamilyDoctrinePanel` 组件；这是用户既有未提交改动，不是本轮编辑产物。
- `3d81e58`（SC-024 UI 提交）的文件清单不含 `Assets/Scenes/FoundationScene.unity`；`Logs/sc024-scene-integration-2026-11-05.txt` 的生成器成功只记录在本地历史。合并 `5885315` 也把该文件列为冲突，提交后的场景不含 SC-024。
- 因此场景接入若要进入 `main`，仍需单独的集成提交，并针对该提交后的场景重新取得 SC-024 Validator 行；`SC024_DEMO.md` 第 0 节已区分 HEAD 与当前工作区。

### 5.2 SC-024 UI 退休门禁（已由第 0 节修正并运行）

- `M2FixedScenario.IsActive` 只检查 family ID；SC-024 复用 `family.akechi.sc013`，所以 M2 门禁确实生效。
- 原始 `M2FixedScenario.CanBeginRetirement` 要求第一代已提交 `PledgeEventId`（`event.sc013.inheritance-pledge`）。
- 目录中该事件为 `"scenario": "sc013"`；`EventEngine.ScenarioMatches` 要求场景标签完全一致。
- `GameSession.InitFamilyDoctrineDemoScenario` 使用 `sc024`；因此该事件不会在 SC-024 中出现，原始 UI 退休按钮无法启用。
- **修正后：** 路线感知门禁在 `sc024` 下第一代只要求本路线家训议事事件已提交，第二代不再要求由雨夜来客派生的旧客回访；普通 SC-013 仍保留 pledge + 雨夜来客 + 旧客回访门禁。`SC024M01RegressionTests` 的两个门禁用例与 `M2StoryGateTests` 已在本轮 76/76 中通过。

### 5.3 当前合并后的测试与门禁不一致（已由第 0 节修正并运行）

- `Assets/Tests/EditMode/FamilyDoctrinePanelTests.cs` 原第 384 行在 `sc024` 路线中期待 `SC013EventIds.PledgeEventId`。
- 提交 `6f4190f` 原本使用 `SC024EventIds.PledgeEventId`；合并 `5885315` 的冲突解决改成了 SC013 ID。
- 当前 `Assets/Scripts/Events/EventCatalogData.cs` 的 `SC024EventIds` 中没有 `PledgeEventId`，目录中也没有 SC-024 的 pledge 事件。
- **修正后：** 该测试在 `sc024` 路线改为期待雨夜来客；本轮 `FamilyDoctrinePanelTests` 12/12、`FamilyDoctrineSessionTests` 14/14 通过。
- `Assets/Tests/EditMode/SC024M01RegressionTests.cs` 是已跟踪文件（`6958b3a` 新增），本轮新增路线边界用例后共 11 项，全部通过。

### 5.4 “培养继承人”与路线边界（已由第 0 节修正并运行）

- 原始 SC-024 复用 SC-013 六人；指定继承人依次为正綱（46）和信正（22），没有 0–15 岁教育对象。
- **修正后：** SC-024 追加第四代子嗣明智 景继（`person.sc024.nobumasa-heir`，1550 年生），第二次既有交接后由信正指定并成为教育对象；普通 SC-013 仍为六人。
- **复审边界：** 景继的指定、名册追加和“第四代”标签只在 `sc024` 路线生效。若家系数据带有景继但路线为 `sc013` 或空，`ExpectedHeir` 在第三代返回空、`SyncDesignation` 不指定景继、`BuildRoster` 仍输出六人；新增回归 `Sc024_FamilyDataKeepsSc013RulesWhenTheRouteIsNotSc024` 已通过。

### 5.5 既有 Validator 错误不属于 SC-024

- 历史报告的整体失败来自 `Feedback text must be a top-anchored banner clear of the map HUD controls.`，是既有 SC-009/SC-005 反馈条位置问题。
- 修复 SC-024 场景接入时，应把该错误单独列出；SC-024 专项 PASS 不等于整体 Validator PASS。

## 6. 本次文档与验证轮次

- `SC024_DEMO.md`
- `docs/reports/sc024-implementation.md`
- `PROJECT_STATE.md`

M01 修正轮次修改了第 0 节所列 C# 与测试文件，并在打开的 Unity Editor 中完成编译与聚焦 EditMode（76/76）；未修改 Unity 场景/预制体、`ProjectSettings`、`Library`、`Temp`，未运行 `FoundationSceneValidator` 或 Play Mode，未提交或推送，未处理工作区中的 `nul`。验证期间曾临时加入一个仅用于触发 `TestRunnerApi` 的 Editor 脚本；测试结束后已删除该脚本及其 `.meta`，未留在工作区。

## 7. 未声称的事项

- 未声称全量 1184 项 EditMode 通过；本轮只跑聚焦 76 项。
- 未声称本轮 Console 计数为零；本轮未在测试结束前单独读取 Console。
- 未声称 `FoundationSceneValidator` 或 Play Mode 本轮通过；代理 Play Mode 补充记录不等于用户接受。
- 未声称 SC-024 场景接入已进入 `main`；HEAD 场景不含 SC-024，工作区场景有未提交接入。
- 未声称历史 20/20、63/63、10/10、70/70 是本轮测试结果。
- 未声称用户已完成 SC-024 Play Test 或接受该切片。
- 未声称 M01 已完成；M01 仍需场景接入、当前 Validator、Play Mode 与用户明确接受。
