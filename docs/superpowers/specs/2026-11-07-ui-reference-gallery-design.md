# 五张 UI 参考图的一比一功能复刻

**状态：** 用户已确认五张参考图全部纳入范围，并授权在 OpenCode2 账户余额不足时由项目负责人直接完成实现。目标是复刻页面构图、视觉层级、纸本/墨色材质、色彩、控件外观与相对位置，同时保留当前项目的真实数据和游戏操作。

## 目标

将设计图库中的五个 16:9 页面实现为《影之一族》的可交互 uGUI 屏幕：人物档案与日常行动、家谱与继承、家族内务、地图与出行、事件抉择。五页共享一致的家纹、深靛色边框、金色细节、和纸底纹、朱印强调与传统衬线字形；每页的导航和主内容分区跟随对应参考图。

## 参考图和页面职责

参考图均由用户提供，原始尺寸为 1668×941；Unity 目标画布以 1280×720 为基准并按相同比例缩放。

1. **人物档案与日常行动**：顶栏资源/日期，左侧主导航，档案/关系/经历标签，中部人物卡与关系网，右侧行动安排，最右只读 AI 调试栏。映射 `DailyActionPanel`、`CharacterDailyPageView` 与 `NpcAiInspectorPanel`。
2. **家谱与继承**：顶栏、左侧家族导航、中央按世代连线的家谱、右侧继承/家族信息与决策。映射 `FamilyRosterPanel`、`SuccessionPanel`、`FamilyTreeGraphBuilder`。
3. **家族内务**：顶栏、家主/日期侧栏、中央成员与任命表及本月事件、右侧天下动向和待决事项。只组合已存在的家族、产业、事件与月结数据；没有领域数据的字段不显示虚构值。
4. **地图与出行**：顶栏、左侧队伍/路线计划、中央地图及路线、右侧地方传闻/人物/委托。映射 `WorldMapController`、`GameSession` 的出行命令及已有委托/事件视图。
5. **事件抉择**：顶栏和左侧家族导航，中央事件插画/叙事/选项，右侧待处理事项。映射 `EventChoicePanel` 与现有事件目录、选项命令及反馈。

## 视觉验收

- 五个页面必须使用各自参考图的主构图与区域比例，不再保留当前平面蓝色 HUD 样式作为最终页面。
- 同一游戏数据在对应区域中以真实会话/领域状态渲染；截图中的姓名、数值、人物和事件只是视觉示例，不覆盖当前游戏数据。
- 页面上的现有命令仍可执行：人物选择、休息/训练/工作/旅行、日历推进、继承/摄政、家谱浏览、地图目的地/出行、事件选择、保存/结算等。
- 每个页面的按钮、面板和可滚动内容均需适配 1280×720 基准，不重叠、不越界；更宽画布保持比例和对齐。
- AI Inspector 保持独立、只读、开发者用途。

## 约束与非目标

- 不修改领域规则、存档结构、事件效果或随机结果；不新增第三方包或 ProjectSettings。
- 优先复用现有面板、数据快照和生成器；只新增共享 UI 主题/页面导航等跨屏必需组件。
- 仅将用户提供的设计图裁切为 UI 插画/纹理；不得从商业游戏复制资产。`ASSETS.md` 记录来源为用户提供的 ChatGPT Library UI 设计图及其裁切文件。
- 保留所有 `.meta` 和 GUID，场景改动仅通过单一固定根、单一 Undo 组、保存前验证和异常回滚完成。
- 保留当前已有的 M02 未提交 WIP并在其上继续；保留无关的 M04A 文档、`graphify-out/` 和 `nul`。

## 方法

按功能复用项目现有 UI，而非把整张截图铺成静态假界面。共享主题提供纸纹、色板、边框、字体与按钮样式；每个现有页面逐一映射其数据和命令，使用参考图裁切的装饰艺术做真实 uGUI 组成。每页独立更新场景生成器/绑定验证和 EditMode 覆盖，再整体核对五页导航与 Play Mode。

## 风险

参考图是合成的 UI 稿，含有仓库当前不存在的立绘/纹理和示例文案。可从用户提供的图中提取的装饰素材会被复用；不适合裁切的区域用项目内 uGUI 重建。若某个示例字段在当前会话中没有真实数据，该字段保持缺省/省略，不伪造数值。

## Runtime Chinese font

Runtime-created uGUI Chinese labels use Unity's built-in `LegacyRuntime.ttf`. The system dynamic-font path left the event empty-state labels invisible in this Canvas. This fallback does not change page data or event commands.

The shared full-page surface uses a deep indigo backdrop and an opaque indigo navigation rail, matching the dark frame in the supplied reference pages. The backdrop is a non-interactive Canvas sibling behind every page, so switching pages does not reveal the legacy blue scene background. These colors are presentation-only and do not affect game state.

## Map artwork integration — 2026-11-08

The map page uses the owner-provided central map crop as a non-raycast Image behind existing controls. It occupies the central content rectangle, while the existing route/travel interactions remain live. The crop is catalogued in ASSETS.md; this integration does not claim exact recreation of the reference's side panels, labels, route overlays, or scale decorations.
