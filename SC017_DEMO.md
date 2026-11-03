# SC-017 通用家谱展示 — 交付说明与纯文本试玩手册

更新时间：2026-11-03

本文件是 SC-017 的交付说明、场景接入步骤与用户试玩清单。实现目标：把 SC-013 固定六行名册改为只读、数据驱动、可滚动的通用家谱；家谱从真实家族成员与 `KinshipGraph` 关系生成，不写死六人或三代，未连接亲族与家臣分区显示。

## 0. 当前状态与证据边界

- 已实现：图快照/构建器、确定性布局、可滚动面板、typed 关系线、未连接亲族区、家臣/弟子/客将区、家主/指定继承人标记、事务式场景 updater 与 validator 扩展，以及对应 EditMode 测试源。
- 实现与审查 worker 未操作 Unity；随后用户于 2026-11-03 明确接受本次演示。用户未回传 EditMode Passed/Failed/Skipped 数量、Console 计数或 Validator 报告原文。
- 已取得：Unity 随附 Roslyn 对八个程序集编译 0 错误；纯图/布局 harness 86/86 通过（既有 77/77 保持不变，新增 9 项拥挤配偶线回归）。**这两项都不是 Unity Editor 编译、EditMode、Play Mode 或用户验收。**
- 顺序只读复审：由 Codex fallback 完成（OpenCode reviewer launcher 不可用），结论 ACCEPT；无 Critical/Important。轻微备注：空路由失败分支没有 FamilyRosterPanel 专项测试；源码检查确认该分支记录空路径且不绘制线段。复审未运行 Unity 或测试。
- 当前仍未提交、未推送、未合并、未打标签。工作区累积有 SC-013–SC-016 等未验收改动；本次只记录 SC-017 接受，不把其他切片一并发布。

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
