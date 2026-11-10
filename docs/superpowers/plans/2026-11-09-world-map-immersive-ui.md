# 沉浸式世界地图实施计划

> **For agentic workers:** Use superpowers:executing-plans to implement this plan task-by-task. One independent read-only review follows implementation.

**Goal:** 落实用户选定的全屏地图与可收起侧栏，保留现有旅行和时间操作。

**Architecture:** 在现有地图页上增加一个只负责布局的组件，复用原按钮、文本和地图插画。导航统一控制页面显隐；不改旅行结算或存档契约。

**Tech Stack:** Unity 6、现有 uGUI、现有原创素材；无新依赖。

**Spec:** ../specs/2026-11-09-world-map-immersive-ui-design.md

## Global Constraints

- 当前 main 内开发，不创建分支、worktree 或项目；保留所有既有 WIP。
- 使用 PID 126040 的现有 Editor；不运行 batchmode。
- 不修改场景、存档或玩法规则；截图查看后立即删除。
- 既有导航、名册、编年史、结束月份均必须可达。

## Review Focus

- 反复进出地图与同页点击：不增加节点或监听、不清除有效预览。
- 侧栏收起：原文本状态保留，确认与取消始终可达。
- 城镇覆盖层：不被新增地图控件遮住，返回时正确恢复。
- 低分辨率：文字不缩小到不可读，栏宽与地图空间明确。
- 地图装饰不接收射线，背景不遮挡导航。

### Task 1: 地图布局与导航集成

**Files:** 新增 `Assets/Scripts/Presentation/ImmersiveWorldMapView.cs`；修改 `ReferenceGalleryNavigator.cs`；新增对应 EditMode 测试，调整旧地图布局断言。

**Interfaces:** `Configure()` 在组件所在地图根上幂等绑定既有子节点；`SetTravelExpanded(bool)` / `SetInformationExpanded(bool)` 只修改显示状态。

- [x] 添加折叠、重复配置、动作栏边界与状态不变回归测试并观察失败。
- [x] 复用既有地图插画，重新排版为地图主体、左右侧栏、底部固定操作。
- [x] 集成到地图页；保留原旅行按钮的监听和原生引用。
- [x] 运行聚焦 EditMode，然后独立只读 QA；修复缺陷。

### Task 2: 当前 Editor 验证与交付

- [x] 运行适当的完整 EditMode、编译与场景 Validator，分别记录结果。
- [x] Play Mode 检查侧栏开合、导航、名册/编年史、预览/取消/确认及时间按钮。
- [x] 校验用户存档未变；退出 Play Mode；检查 Console 并清理截图。
- [x] 更新项目状态和报告，只提交本轮验证文件并推送当前 main；最终里程碑验收仍由用户决定。

## Decisions

- 用户已选择方案 3 并要求直接继续开发，按该范围实施，不增加新的玩法选择。
- 沿用既有原创插画和 uGUI，无需重复外部依赖研究或新增美术素材。
- 侧栏直接开合，不添加非必要动画；没有会话数据的传闻/委托不显示。
- 窄屏实机已在 960×720 与 1280×720 Game View 预设验证，并恢复 4K UHD。详见后续验收附录。

## 2026-11-09 窄窗实机跟进

已通过 1280×720 的实际 GUI 保存/推进/读回；已将用户 active 与备份恢复到原哈希。960×720 暴露真实缺陷：地图只在进入页面时读取 Canvas 尺寸，分辨率变化后保留旧尺寸；共享顶栏也保留固定宽度，使资源和地图右侧操作裁切。

修复范围仅布局：`ReferenceGalleryNavigator` 观察真实父 Canvas 尺寸变化，在不调用 ReturnToMap 的情况下重排地图；共享顶栏在窄窗将资源移到第二排，不缩小文字，侧栏保留可用高度。`ImmersiveWorldMapView.Configure()` 识别配置后的实际尺寸变化，自动收起信息栏。沿用同一工作区、既有按钮/监听/存档契约，不调整玩法。

实机追加决定：窄窗经地图快捷入口打开家谱后，原760宽卡片仍压在导航下。该入口的验收需要家谱可读，故同一修复收窄卡片和既有 ScrollRect 视口，保留图节点字号/坐标/双轴滚动，不增加编辑功能；相应 HUD 下移到两行顶栏下方。宽窗恢复既有尺寸。新增原家族布局测试的缩窄断言并已观察明确红测。

- [x] 添加父 Canvas 动态缩窄测试，观察真实失败；保留地图返回计数/选择，检查顶栏与页面边界。
- [x] 最小修复，聚焦测试通过后安排独立只读 QA。
- [x] 当前 Editor 验证 960×720 与 1280×720，旅行预览、侧栏、名册/编年史、时间按钮；既有 GUI 存读档验证与本轮存档哈希复核有效。
- [x] 完成全量/编译/Validator，恢复用户存档和原 4K UHD 视图预设，清理临时脚本与截图，更新报告并仅提交本轮文件推送 main。

## 2026-11-10 家谱窄屏验收补充

地图入口打开家谱时，960×720 下原固定宽度卡片与导航栏/HUD 冲突。保留原 ScrollRect 与图内容，只缩窄视口并添加独立横向滚动条，让较宽家谱可以通过标准点击访问右侧成员。继承人说明移到滚动条下方；重复页面布局不得重置 Scrollbar 管理的 handle 几何。1280×720 下卡片恢复原宽度。

- [x] 横向滚动条缺失回归先红后绿；重复配置后的 handle 宽度断言通过。
- [x] 完整 EditMode `1386/1386`，独立只读 QA 第五轮 ACCEPT。
- [x] 960×720 与 1280×720 真实 Play Mode 检查；点击横向滚动条后家谱内容移至右侧成员。
- [x] FoundationScene Validator、存档哈希、4K UHD 恢复、Play Mode 退出及临时文件清理见验证报告。
