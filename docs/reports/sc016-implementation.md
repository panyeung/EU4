# SC-016 M2 委托旧约实现报告

日期：2026-11-03  
分支：`feature/sc013-fixed-three-generation`  
工作区：`C:\Users\panye\My project`  
设计稿：`docs/superpowers/specs/2026-11-03-sc-016-m2-commission-legacy-design.md`  
实现计划：`docs/superpowers/plans/2026-11-03-sc-016-m2-commission-legacy.md`  
试玩手册：`SC016_DEMO.md`

## 结果

SC-016 已按批准的设计与计划完成数据/文档切片：在 SC-013 固定 M2 剧本中追加 `chain.sc013.commission-legacy`，包含七个事件节点、十个选项和二十七条中文文本。所有节点使用 `scenario: "sc013"`、`Monthly` 触发器、`PerGame` 一次性范围和现有允许列表条件；三个决策事件共六个选项使用 `RelationshipDelta` 指向既有 NPC `npc.sakai.jirobei`，四个第三代结尾选项的 `effects` 为空。`contentVersion` 保持 `2`。

数据实现阶段没有修改场景、预制体、`ProjectSettings`、存档 schema、package、测试、代码、`Library` 或 `Temp`；没有变更 SC-011、SC-013、SC-014、SC-015 的既有事件、选项或文本。后续验证发现 SC-013 标题入口既有 EditMode 夹具未匹配 Preview Scene 的 Canvas 几何，OpenCode2 仅调整 `Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs`，没有改生产代码或场景。实现与验证期间未提交、推送、合并或打标签。

## 变更文件

- `Assets/Resources/Events/event-catalog-v1.json` — 追加 `chain.sc013.commission-legacy` 和七个 `event.sc013.commission-legacy.*` 事件（十个选项）。
- `Assets/Resources/Localization/event-text-zh-v1.json` — 追加七个事件、十个选项、十个摘要，共二十七条中文文本。
- `PROJECT_STATE.md` — 追加 SC-016 状态行与实现小节。
- `ARCHITECTURE.md` — 追加 SC-016 事件链、关系效果与优先级说明。
- `DECISIONS.md` — 追加 SC-016 以既有关系差值与事件历史承载后果的决策。
- `SC016_DEMO.md` — 新建纯文本交付说明、四条分支试玩步骤、失败判据和结果清单。
- `docs/reports/sc016-implementation.md` — 本报告。
- 后续验证夹具修正：`Assets/Tests/EditMode/AddSC013TitleScenarioButtonsTests.cs` — 将测试 Canvas 固定到参考分辨率并断言 M6TitleRoot 的实际几何，确保 Unity 使用的 EditMode 测试程序集匹配当前源码。

## 分支结构

| 世代 | 事件 | 来源条件 | 选项/结局 |
|---|---|---|---|
| 第一代 | `event.sc013.commission-legacy.first` | `SuccessionCountEquals 0` | 接受 `+5`；婉拒 `-5` |
| 第二代（守约） | `event.sc013.commission-legacy.second-kept` | 第一代接受 + `SettledSuccessionCountAtLeast 1` | 继续守密 `+5`；公开真相 `-5` |
| 第二代（失约） | `event.sc013.commission-legacy.second-broken` | 第一代拒绝 + `SettledSuccessionCountAtLeast 1` | 主动补偿 `+5`；拒绝再牵连 `-5` |
| 第三代（守约） | `event.sc013.commission-legacy.third-safe` | 第二代继续守密 + `SettledSuccessionCountAtLeast 2` | 单一接收选项，无效果 |
| 第三代（公开） | `event.sc013.commission-legacy.third-public` | 第二代公开真相 + `SettledSuccessionCountAtLeast 2` | 单一接收选项，无效果 |
| 第三代（补偿） | `event.sc013.commission-legacy.third-cleared` | 第二代主动补偿 + `SettledSuccessionCountAtLeast 2` | 单一接收选项，无效果 |
| 第三代（决裂） | `event.sc013.commission-legacy.third-severed` | 第二代拒绝再牵连 + `SettledSuccessionCountAtLeast 2` | 单一接收选项，无效果 |

每个第二代选项只被一个第三代节点引用；分支互斥由 `EventOptionChosen` 的唯一来源和世代门槛共同表达。设计稿内容卡列出三行决策节点与四个第三代结尾；本实现以该内容卡与“7 个事件节点”的明确计数为准：三个决策事件、六个 ±5 选项，四个第三代结尾无效果。此前摘要中的旧措辞已按内容卡更正。这一对应关系已在静态检查中逐项断言。

## 优先级语义

`EventEngine.Schedule` 的候选排序为 priority 降序（`EventEngine.cs` 中 `right.priority.CompareTo(left.priority)`），因此**数值越大越早**。本链使用：

- 第一代 `8`：低于 SC-013 `event.sc013.inheritance-pledge` 的 `1000` 与 SC-011 `event.rainy-arrival` 的 `10`，高于 SC-015 `event.sc013.family-letters.first` 的 `6` 和 SC-014 `event.sc013.refugee-arrival` 的 `5`。
- 第二代 `80`：低于 SC-011 回访的 `100`，高于 SC-015 第二代的 `60` 和 SC-014 `event.sc013.refugee-loyalty` 的 `50`。
- 第三代 `800`：低于 SC-013 `event.sc013.finale` 的 `1000`，高于 SC-015 第三代的 `600` 和 SC-014 援助/竞争节点的 `500`。

这满足“同世代先于 SC-015/SC-014 可选事件、低于 SC-013 必须事件、不抢占既有 SC-011 顺序”的计划要求。M2 退休门禁未改变，完整链仍需要在每次交接前额外结束月份；第一次交接前未触发入口时，本链不会迟到启动。

## 静态检查证据

实现侧在 Windows 主机上使用 Python 3 执行一次性 JSON 检查（未运行 Unity 或测试）：

1. 两个 JSON 文件均可解析。
2. 两个 `contentVersion` 均为 `2`，且彼此相等。
3. 纯追加比较：以实现前工作区文件快照为基线（路径与 SHA-256 见下文），4 条既有链、16 个既有事件（含 31 个选项）、77 条既有文本逐项 JSON 相等；新增 1 条链、7 个事件、10 个选项、27 条文本。
4. 链 `chain.sc013.commission-legacy` 的 7 个成员存在、顺序正确、无重复、无未链接事件。
5. 所有事件/文本/选项/摘要 ID 唯一；27 个新增 `textId` 全部存在；SC-016 文本行与引用集合完全一致，无孤立文本。
6. 所有 `EventOptionChosen` 的源事件存在、源选项存在、同一链；每个源事件/选项组合只被一个事件引用。
7. 第一代门槛为 `SuccessionCountEquals 0`；第二代为第一代接受/拒绝选项加 `SettledSuccessionCountAtLeast 1`；第三代四个节点分别为对应第二代唯一选项加 `SettledSuccessionCountAtLeast 2`。
8. 所有新增事件 `scenario` 为 `sc013`、`trigger` 为 `Monthly`、`onceScope` 为 `PerGame`、`cooldownMonths` 为 `0`、`weight` 为正。
9. 三个决策事件各有两个选项，六个选项各含且仅含一个 `RelationshipDelta`，目标均为 `npc.sakai.jirobei`，数值按设计为 `+5`/`-5`；四个第三代结尾各有一个选项，`effects` 为空，要求仅 `Always`。
10. 四个第三代结尾的来源选项互不相同；优先级关系满足上文约束。

结果：10 组检查、0 错误。此为源文件静态比较，不是 Unity Test Runner、编译或运行时证据。

### 实现前快照

实现前工作区快照保存在项目之外：

```text
%LOCALAPPDATA%\Temp\opencode\sc016-pre\event-catalog-v1.json
%LOCALAPPDATA%\Temp\opencode\sc016-pre\event-text-zh-v1.json
```

快照 SHA-256（实现前基线）：

```text
catalog snapshot = afea1fadb46b75cb5df28c33c54372faab8c01c12a11d029971bd2b4c5321e83
text snapshot    = fb3b93dc63d06f8d1f4cb27d491c724b09efce9e642976348f39419511c73851
```

实现后文件 SHA-256（供后续审阅复核）：

```text
catalog result   = e481964a6ec3ed3927db000ec8d4c9db1a59c40e316bcc9526ff53310ceb773b
text result      = d1d1d103a83fdd8c7ab08b9a64ac75ca37f5fe9c50e7b737c8498baaf1338e79
```

快照位于临时目录，不纳入版本控制。

## 既有内容保护

- 与实现前快照比较，既有 16 个事件（SC-011 的 3 个、SC-013 的 2 个、SC-014 的 4 个、SC-015 的 7 个）及全部 31 个选项 JSON 完全相等。
- 既有 4 条链（`chain.rainy-arrival`、`chain.sc013.main`、`chain.sc013.refugee-legacy`、`chain.sc013.family-letters`）JSON 完全相等。
- 既有 77 条文本条目 JSON 完全相等；SC-016 只追加 27 条。
- 因 SC-013/SC-014/SC-015 尚未提交，本报告不使用 `HEAD` 作为比较基线，也不声称与 `HEAD` 逐字节相等。
- 工作区中既有的其他修改（`SampleScene.unity`、旧方向文档、`Assets/1.unity`、`ProjectSettings/SceneTemplateSettings.json`、`graphify-out/`、`nul` 等）未被触碰。

## 运行验证追加（2026-11-02，本机时间）

- Unity Editor：使用已打开的 Unity 6.6.6f1 与本项目，没有启动 batchmode。前两次全量结果仍指向旧版测试源码；检查到失败调用行号与当前文件不一致后，在 Editor 执行 Assets > Refresh，等待 `Compiling Scripts` 完成，再次运行全量 EditMode。
- EditMode：Test Runner 显示 778 项通过、0 失败；`TestResults.xml` 为 778 passed、0 failed、0 skipped/inconclusive。三个 `AddSC013TitleScenarioButtonsTests` 用例全部通过。
- 编译：Editor 执行脚本编译/重载后测试通过；本项目 `Logs/Editor.log` 中未检出 `error CS####`、`Compilation failed` 或 `Scripts have compiler errors`。不据此声称其他运行阶段通过。
- Console：套件完成后 Unity Console 会话累计计数为 0 errors、14 warnings、8 logs；因此记录实际计数，不把它表述为零警告。测试期间的场景 updater 负例会输出回滚 warning。
- Play Mode：助手在用户明确的临时授权下，通过现有 Editor 对 SC-013 M2 新会话逐条执行四个 SC-016 分支。A 接受→继续守密到达“夜路仍然安全”；B 接受→公开真相到达“账册见了天日”；C 拒绝→主动补偿到达“旧账已清”；D 拒绝→拒绝再牵连到达“商路不再相通”。编年史显示对应结尾；未保存测试进度。
- 场景保护：测试后从 Unity 最近场景菜单重新打开磁盘上的 `FoundationScene`，未保存临时状态。`Assets/Scenes/FoundationScene.unity` SHA-256 为 `2B448E0DB0E1DF544C95C9A3CF8608FE867471D9EA06C98E08748EE484580D05`，与测试前记录相同。
- 独立只读复审：SC-013 测试夹具复审为 ACCEPT，确认它匹配真实标题 Canvas/控件布局并保留隔离断言；复审者没有运行 Unity。OpenCode reviewer 启动器不可用，因此按已告知用户的回退方案由独立 Codex reviewer 完成源码审查。
- 尚未运行：`FoundationSceneValidator`、存档保存/加载验证；未取得用户正式验收。不得据助手 Play Mode 代替用户对 SC-016 的验收。
- 未创建 checkpoint，未推送、合并、打标签。

## 尚未执行的验证

以下项目全部**未运行/未验证**，不得据此声称通过：

- `FoundationSceneValidator` 与场景验证（本任务未改场景）。
- 存读档试玩（SC-016 未新增存档字段，但仍未执行）。
- 用户 Play Test 与正式验收。
- 提交、推送、合并、打标签。
- 静态检查、助手 Play Mode、Unity EditMode、场景验证和用户验收是不同证据；前几项均不替代用户正式验收。

## 后续步骤

1. 用户审阅 `SC016_DEMO.md` 中四条 Play Mode 结果，执行其本人验收流程并填写结果清单。
2. 如需场景验证或存读档验证，在现有 Unity Editor 中继续进行并单独记录证据。
3. 仅在用户明确验收后，由协调方办理 checkpoint、合并、标签与推送。
