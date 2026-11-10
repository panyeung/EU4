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
- 实际 Game View 为 4K/16:9；窄屏由600宽自动化断言覆盖，GUI分辨率切换未可靠读回，窄窗实机保持待补。详见本轮报告。
