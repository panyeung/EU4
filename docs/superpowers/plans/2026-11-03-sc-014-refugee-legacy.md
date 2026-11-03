# SC-014 实现计划：流亡少年与家系遗产

> **给 OpenCode2：** 按顺序执行本计划。每个实现任务只拥有下列明确文件；不得重置、格式化或顺手清理其他脏文件。

**目标：** 在已有 SC-013 M2 固定剧本中加入 GDD 7.4 第二条跨代事件链，记录第一代救助选择在第二、三代的关系后果，并更新项目状态文档。本轮不 Play Test、不碰 Unity 场景。

**设计：** [SC-014 设计稿](../specs/2026-11-03-sc-014-refugee-legacy-design.md)

**范围依据：** GDD v4.0 §7.4、§12.2/12.3、§19.6；复用 SC-013 的 M2 事件门控和 SC-012 的编年史，不引入新的存档状态。

## 文件所有权

实现 worker 可编辑：

- `Assets/Resources/Events/event-catalog-v1.json`
- `Assets/Resources/Localization/event-text-zh-v1.json`
- `PROJECT_STATE.md`（只在当前 SC-013 状态记录后附加 SC-014 状态，不重排或重写其他历史）
- `ARCHITECTURE.md`（仅在已有事件目录/跨代链说明处追加 SC-014 数据链信息）
- `DECISIONS.md`（只追加本设计选择与兼容性记录）
- `SC014_DEMO.md`（仅记录实现状态、验证边界和之后用户可执行的文本步骤；不标记任何测试已通过）

实现 worker 不得编辑：场景、预制体、`ProjectSettings`、SC-011/SC-013 已有事件行、既有测试文件、其他 WIP 文档、`Library`、`Temp`。本轮不新增或运行测试；不启动 Unity，不使用截图或 Computer Use。

## 步骤

1. 精读现有事件目录、SC-013 场景 ID 规则、事件历史条件和本设计稿；不修改工作区中其它 SC-013/WIP 内容。
2. 只向两个 JSON 资源追加 `chain.sc013.refugee-legacy` 与四个事件节点：第一代入口、第二代忠诚选择、第三代盟友结局、第三代竞争结局。复用现有 `EventOptionChosen`、`SuccessionCountEquals`、`SettledSuccessionCountAtLeast`、`RelationshipAtLeast` 与 `RelationshipDelta` 规则；保留版本 `2` 和其他事件定义的字节/语义内容。
3. 按设计稿填写每个选项的中文标题、描述、结果摘要与关系数值。所有关系目标使用 `npc.kuroda.iori`；选项的效果与可用性清楚，不依赖隐式标记或新存档字段。
4. 仅追加本功能的状态、架构、决策记录及 `SC014_DEMO.md`。Demo 指南要明确这是交给用户稍后执行的文本手册，当前没有 Play Test 或 Unity 证据。
5. 交付变更摘要、修改文件、实现约束处理情况，以及明确未运行的验证项；不得自行提交、推送、合并、标记验收。

## 审阅任务（实现完成后顺序派发给独立 OpenCode2 worker）

- 只读检查允许范围内 diff；确认 ID 唯一、所有文本 ID 存在、链成员与 `EventOptionChosen` 源引用吻合，分支互斥，第二/第三代交接门槛正确，关系影响真实且可解释。
- 对比 `HEAD` 中 SC-011 与 SC-013 的既有 JSON 事件和文本条目，确认保持不变。
- 不运行 Unity、测试、Play Mode，不编辑文件。输出 `ACCEPT` 或 `RETURN`、问题位置及其证据；审阅报告写入 `docs/reports/sc014-review.md`。
- 如修正审阅发现后需要复审，保留首轮报告，并将第二轮结果另存为 `docs/reports/sc014-review-2.md`。

## 验收条件

- 四个新增节点组成一条连贯的跨三代事件链；前置条件精确地延续前代选项，第三代两条结果互斥。
- 只修改文件所有权段列出的文件；没有 Unity 场景、设置、存档 schema 或 package 改动。
- SC-011/SC-013 已有内容不改写。
- 独立 OpenCode2 静态审阅为 `ACCEPT`，或修正并复审后为 `ACCEPT`。
- Play Test、Unity 编译/EditMode/Console/Play Mode 均未声称通过；保留用户后续验证与验收权。
