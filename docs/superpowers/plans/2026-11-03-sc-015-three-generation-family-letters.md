# SC-015 三代家书实现计划

> **给 OpenCode2：** 仅按下列文件清单编辑。当前工作区包含多项既有 SC-013/014 与用户 WIP；不要重置、整理、格式化或覆盖它们。

**目标：** 为现有 SC-013 固定 M2 剧本增加一条由家族历史选择塑造的三代家书事件链。

**架构：** 只追加数据驱动事件和中文文本，依靠现有事件选择历史与两次继承条件连接三代。选择不更改关系或任务数值；不新增任何代码或存档状态。

**技术栈：** 现有 Unity Resources JSON 事件目录与中文文本目录；无新依赖。

**设计稿：** [SC-015 三代家书设计稿](../specs/2026-11-03-sc-015-three-generation-family-letters-design.md)

## 全局约束

- 既有 Unity 项目 `C:\Users\panye\My project`；不新建项目。
- 事件 ID 使用 `event.sc013.*`、`option.sc013.*`、`chain.sc013.*`，事件 `scenario` 为 `sc013`，`contentVersion` 保持 `2`。
- 只使用 `EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`；所有新增效果数组为空。
- 不改既有事件/文本，不改场景、预制体、ProjectSettings、测试、存档 schema、包、`Library` 或 `Temp`。
- 不新增或运行测试，不操作 Unity，不提交/推送/合并/打标签。

## 文件所有权

实现 worker 可编辑：

- `Assets/Resources/Events/event-catalog-v1.json`：只追加 SC-015 的链声明和 7 个事件。
- `Assets/Resources/Localization/event-text-zh-v1.json`：只追加 SC-015 的事件、选项和摘要文本。
- `PROJECT_STATE.md`：只追加 SC-015 状态行/小节，不改写旧记录。
- `ARCHITECTURE.md`：仅追加现有事件数据说明中的 SC-015 分支与零数值效果。
- `DECISIONS.md`：仅追加本链选择历史作为后果载体、无关系/存档变化的决策。
- `SC015_DEMO.md`：交付说明、明确验证边界及之后的文本试玩步骤；不得把未执行项目写成通过。

实现 worker 不得编辑其他文件。设计稿和本计划由协调方维护。

## 步骤

1. 阅读 SC-015 设计稿、事件目录现有 schema、SC-013/014 节点与 SC-013/014 文本；确认工作区中其他差异保持不动。
2. 向事件目录追加 `chain.sc013.family-letters` 和以下七个事件。事件优先级按现有 `EventEngine` 的**从高到低**顺序设置，使 SC-015 先于同代 SC-014、但后于 SC-013 必须事件：第一代 `6`、第二代 `60`、第三代 `600`。
   - `event.sc013.family-letters.first`：首次交接前，两个选项分别公开家书与封存家书。
   - `event.sc013.family-letters.second-open`：公开路径，至少一次已结算交接；两个选项分别保留完整细节与整理简史。
   - `event.sc013.family-letters.second-sealed`：封存路径，至少一次已结算交接；两个选项分别由正綱拆阅并完整记录、继续封存。
   - `event.sc013.family-letters.third-full-public`、`...third-short-history`、`...third-opened-by-heir`、`...third-sealed`：均要求至少两次已结算交接，各自唯一引用产生该结尾的第二代事件与选项。
3. 所有事件设 `scenario: "sc013"`、`trigger: "Monthly"`、`onceScope: "PerGame"`、`cooldownMonths: 0`；沿用目录现有合法权重/结构。每个分支条件为相应 `EventOptionChosen` 加世代门槛，入口另加 `SuccessionCountEquals 0`。所有 options 使用现有 `Always` requirement、摘要文本引用和空 `effects`。
4. 为每个新增 `textId` 编写符合家书/亲情主题的简体中文标题、描述与结果摘要。文本应准确说明由晴信、千恵、正綱、信正何人作出/承接决定；不声称数值或人物状态变化。
5. 仅更新本计划所列文档。试玩手册包含入口、各分支逐步点击顺序、预期事件、失败条件、结尾核对与纯文本结果清单；注明当前尚未在 Unity 执行。
6. 做一次性静态检查：两个 JSON 可解析；所有 ID 唯一；每个新增 `textId`、`optionId`、摘要引用均存在；链成员完整；所有源事件/选项引用确实存在；三个世代门槛正确；每个事件的 effects 为空；SC-011/SC-013/SC-014 原事件/文本与实现前完全相同。记录检查方法与结果。不要运行 Unity 或测试。
7. 报告仅限 SC-015 的改动、静态检查结果、未执行验证项及任何差异；不提交/推送/合并/打标签。

## 审阅任务

实现完成后，顺序派发一名独立 OpenCode2 worker 只读审阅本计划文件清单内的 SC-015 diff，并将结果写入 `docs/reports/sc015-review.md`。复核七个节点的 ID、所有文本引用、分支可达性、源选项唯一性、一次性结算、继承门槛、空效果声明及 SC-011/013/014 的原内容未改。不得运行 Unity/测试或编辑功能文件。输出 `ACCEPT` 或 `RETURN`；若修正则另派同一或新的只读复审并保留报告。

## 验收条件

- 七个节点构成完整且互斥的三代家史分支；第一代入口、第二代回收、第三代终点的门槛准确。
- 仅修改“文件所有权”列出的功能文件，外加独立审阅报告；不影响 Unity 场景与项目配置。
- 两个资源可解析、引用与唯一性静态检查无错，独立审阅为 `ACCEPT`。
- 对 Unity 编译、EditMode、Console、场景验证、Play Mode、存读档及用户 Play Test 均保持“未执行/未验证”。
