# M6 实施计划：标题界面与演示封装

设计：`docs/superpowers/specs/2026-11-01-m6-title-demo-packaging-design.md`
分支：`feature/m5-save-slot`（用户指定沿用同一分支与工作树）

## 任务

1. **纯逻辑先行**
   - `GameSession.IsDemoComplete` 属性（三任务均 RewardClaimed）。
   - 测试：完成前 false、完成 true（经 `TryApplySave` 构造完成态）。
2. **Presentation 脚本**
   - `TitleScreenController.cs`：Bind/SetStore 注入口、Start 显示标题、RefreshTitleState（缺档/坏档/有效三态）、StartNewGame、TryContinue、幂等按钮监听（OnEnable/OnDisable）。
   - `DemoSummaryPanel.cs`：幂等 `Refresh()` + Update 轮询；文案「Phase 1 演示完成。总功绩：N。」
   - `TitleScreenControllerTests.cs`、`DemoSummaryPanelTests.cs`。
3. **Editor 更新器**
   - `AddM6TitleScreen.cs`：M5 同构事务式更新器；创建 `Canvas/M6TitleRoot`（全屏不透明 Image，默认隐藏，最后兄弟）与 `map/M6DemoSummaryText`（TownMenuRoot 之前，默认隐藏，位于地图矩形内且不压 M5 控件）；在 GameSession 对象挂两个新组件并绑定；菜单 `Tools/Sengoku Chronicle/Add M6 Title Screen`；`RunBatchIntegration()`；故障注入缝。
   - `FoundationSceneValidator.cs`：新增 M6 FOUND/绑定检查并调用 `AddM6TitleScreen.Validate`。
   - `AddM6TitleScreenTests.cs`（preview-scene 结构、绑定、幂等、回滚）。
4. **Unity 验证**（受 Editor 占用约束）
   - 用户 Editor 正打开本工作树：不得 batchmode。二选一：
     a) 用户关闭 Editor → 我跑 batchmode：编译、EditMode 全量、`RunBatchIntegration`、Validator 报告；用户重开做 Play Mode 验收。
     b) 用户保持 Editor：由用户在 Test Runner 执行 Run All 并运行菜单更新器 + Validator，回报结果。
   - 记录：编译诊断、测试总数、场景 diff、Validator 报告分别留档。
5. **文档与收尾**
   - `M6_DEMO.md`（验收路径+证据）、`PROJECT_STATE.md`、`PHASE1_ROADMAP.md`、`ARCHITECTURE.md` 更新。
   - 同任务 QA 复审（QATester 流程按 AGENTS；kanban 工具不可用时由 lead 记录复审结论）。
   - 用户 Play Mode 接受后：最终 checkpoint → 合并 `main` → tag `demo-M6` → 推送 GitHub（AGENTS 新规：checkpoint 后立即推送）。

## 约束提醒

- 单写者：仅 lead/集成工作者改场景；本计划内由集成流程执行。
- `.meta`/GUID 保留；`Library`/`Temp` 不动；不装包。
- 不宣称未真实执行的编译/测试/Play Mode。
