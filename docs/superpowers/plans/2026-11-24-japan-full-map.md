# 日本全境地理地图实施计划

> **执行要求：** 所有 C#、测试与 Unity 实现由 OpenCode2 在 Orca 分配的隔离工作树完成。每个任务先写失败测试，再做最小实现；Unity 测试串行执行并立即复制结果。

**目标：** 把现有网格旅行图改成以日本全境地理底图为主、可缩放和拖动的旅行地图，同时保留现有地点选择、路线筛选、旅行预览和确认。

**架构：** 保留 `JapanTravelGraph`、`Assets/Scripts/Application/JapanMapExplorer.cs` 与 `GameSession` 作为旅行事实来源。Presentation 层新增有来源的枢纽经纬度和投影，底图、路线、节点置于同一受边界约束的地图视口中。缩放与平移只改变视口显示，不改旅行或存档状态。

**技术栈：** Unity 6、C#、uGUI、现有 TextMeshPro/批准字体回退、Unity Input System 与项目现有测试程序集；不新增包。

**设计依据：** `docs/maps/japan-geographic-map-design.md`，用户已批准全日本底图、首次显示全境、缩放与拖动。

**当前进度：** Natural Earth 原创轮廓底图已生成并登记。视口平移几何提交 `e7a89e3`、输入与裁切接线提交 `074ba2e`、拖动 PlayMode 提交 `375fb92`、滚轮锚点 PlayMode 提交 `c8e7b10` 均已推送并获独立复审 ACCEPT。EditMode：视口几何 11/11、屏幕接线 7/7、覆盖层 5/5、可玩旅行 68/68。`JapanGeographicMapPlayModeTests` 3/3 覆盖拖动阈值和平移、短按选择与预览、偏心指针滚轮锚定；证据见 `Logs/verify/20261124-scroll-playmode-verify/README.md` 与其中的 `TestResults.xml`。虚拟鼠标不证明实体鼠标或 Windows 输入采集，也不等于用户验收。37 个枢纽的代表对象、坐标、来源精度与许可尚未全部通过入库门槛；地理投影和全日本标记、路线尚未实现，不得把 `MapX` / `MapY` 当作地理坐标。

## 全局约束

- 保留 `JapanTravelGraph` 的 37 个稳定枢纽 ID、40 条路线 ID、路线天数、类型、证据和时段；不把 `MapX` / `MapY` 当经纬度。
- 保留 Application 层 `JapanMapExplorer` 与 `GameSession` 的选择、预览、确认路径；不改存档格式、地点 ID 或权威游戏状态。
- 地图首次打开显示完整全境；鼠标滚轮缩放、左键拖动平移；缩放和平移受地图边界限制，且不推进日期。
- 底图范围包含用户批准设计中的日本列岛与现有游戏覆盖岛屿；地图纹理与节点投影共用同一组经纬度边界和方向，不能各自拉伸。
- 目录、陆海筛选、当前位置、详情、预览、确认、取消和关闭入口继续可用；地图占据页面主要显示区域。
- 坐标表每项须有明确代表对象、来源直链、坐标基准/精度说明。缺乏直接证据的点不得猜填，也不得将城市中心说成遗址中心。
- 仅使用许可已核实的底图来源和原创绘制；本计划拟用 Natural Earth 1:10m 海岸/陆地轮廓及原创非测绘阴影，不使用许可未确认的 GSI、SRTM 或 OSM 数据。
- 坐标数据单独记录许可与署名。若实现采用 CODH/Geoshape 坐标，在 `ui设计规划.md` 第 10 节登记 CC BY 4.0 与指定数据集署名/DOI；逐项核对的其他坐标来源遵守各自许可。
- 保留 Unity `.meta` 与 GUID，不改 `Library`、`Temp`、用户的 `AGENTS.md` 或无关脏文件。不得并行改同一工作树或并行运行测试。
- 不直接改场景或预制体。入口继续由 `JapanMapSceneBootstrap` 运行时创建；如果验证发现必须改场景，先停下交主控处理。
- 完成预验收后更新关联 PR，写明用户逐步试玩操作、每步预期效果、通过/失败判据与未闭合项。用户验收前不合并 `main`、不打 tag。

## 复审重点

- 中部密集枢纽在全境视图与放大后仍能找到并选择；由 `JapanMapGeographyTests` 与 Play Mode 地图交互用例覆盖，目录仍可访问全部 37 个节点。
- 滚轮缩放围绕指针位置进行，未触及边缘时指针下地图点保持不动；由 `JapanMapViewportTests.ZoomAtPointerKeepsMapPoint` 覆盖。
- 平移和缩放不露出地图内容边界以外的空白；缩放回全境时自动居中；由 `JapanMapViewportTests.PanAndZoomStayWithinBounds` 覆盖。
- 地图尺寸或画布比例变化后，底图、路线与节点继续重合；由 `JapanMapGeographyTests.ProjectionKeepsAllHubsAndRoutesAlignedAtViewportSizes` 覆盖。
- 点击节点仍只选择并预览，只有确认才走现有旅行命令；由 Play Mode 真实 EventSystem/Input System 输入用例和既有 `JapanMapOverlayRefreshTests` 覆盖。

---

## 开始编码前的输入门

**负责人：主控。OpenCode2 不得用猜测坐标或临时底图替代此门。**

- 完成 `docs/maps/japan-hub-coordinate-register.md`：逐项列出 `JapanTravelGraph` 的 37 个稳定 ID、地图代表对象、纬度、经度、来源直链、来源精度/基准与歧义。未能核实的点明确标记，交主控决定如何补证；不得开始以完整地理定位为目标的代码实现，直到 37 项都获得已确认的地图代表点。
- [x] 主控生成日本全境原创底图资源 `Assets/Resources/Generated/japan_geographic_basemap.png`，以 Natural Earth 1:10m 许可范围核对后的轮廓作地理边界，写清经纬度边界并使用原创海色和装饰等高线；等高线明确不代表真实测绘地形。
- [x] 主控把底图来源、许可、尺寸、用途、输入版本与哈希登记到 `ui设计规划.md` 第 10 节。OpenCode2 仍负责接入资源与导入设置。

## 文件职责

- 新建 `Assets/Scripts/Presentation/JapanMapGeography.cs`：保存 37 个稳定 ID 的代表点坐标及可测试的投影函数。
- 修改 `Assets/Scripts/Presentation/JapanMapLayout.cs`：让地图屏幕改用地理投影。保留现有 `ComputeLayout` / `Compute`，直到 `JapanMapScreen` 与 `JapanMapPlayableTravelTests` 的调用和布局断言迁移完毕；只有搜索确认无调用后才可删除旧接口，并保留等价的尺寸、边界与节点分布回归覆盖。
- 新建 `Assets/Scripts/Presentation/JapanMapViewport.cs`：处理地图视口的拖动、指针锚点缩放、适配和边界约束，不拥有旅行状态。
- 修改 `Assets/Scripts/Presentation/JapanMapScreen.cs`：接入底图、视口、投影节点与路线；保持目录、筛选、详情和旅行操作的现有入口。
- 新建 `Assets/Tests/EditMode/JapanMapGeographyTests.cs`、`Assets/Tests/EditMode/JapanMapViewportTests.cs` 及 `.meta`：锁定数据完整性、投影、缩放和平移边界。
- 新建 `Assets/Tests/PlayMode/JapanGeographicMapPlayModeTests.cs` 及 `.meta`，加入现有 `Assets/Tests/PlayMode/SengokuChronicle.PlayModeTests.asmdef` 程序集，不新增 asmdef：在 `FoundationScene` 中通过真实 EventSystem、GraphicRaycaster 与 Input System 输入链验证地图交互和旅行状态边界。
- 修改 `ui设计规划.md` 第 10 节：登记主控交付的底图资产，不登记未用或许可未确认的数据。

## 任务 1：枢纽地理数据与投影

**依赖：** “开始编码前的输入门”中的 37 点坐标登记完成并由主控核对。

**文件：**

- 新建：`Assets/Scripts/Presentation/JapanMapGeography.cs` 及 `.meta`
- 修改：`Assets/Scripts/Presentation/JapanMapLayout.cs`
- 测试：`Assets/Tests/EditMode/JapanMapGeographyTests.cs` 及 `.meta`

**接口：**

- `JapanMapGeographicPoint(double latitude, double longitude)`：只保存一个已核实的地理点。
- `JapanMapGeographicExtent(double westLongitude, double eastLongitude, double southLatitude, double northLatitude)`：定义节点投影和底图纹理共同使用的边界。
- `JapanMapGeography.TryGetPoint(string hubId, out JapanMapGeographicPoint point)`：按原稳定 ID 查点。
- `JapanMapGeography.FullExtent`：返回与主控底图资源一致的固定全境范围。
- `JapanMapLayout.ComputeGeographicLayout(IReadOnlyList<JapanMapHub> hubs, Vector2 mapContentSize)`：按全境范围将所有点投影到地图内容的共同局部坐标空间，返回既有布局结果。路线绘制从稳定枢纽 ID 读取同一结果中的端点位置，不另算第二套投影。

- [ ] **步骤 1：写失败测试** `GeographyContainsEveryHubExactlyOnce`，断言图中恰有 37 个唯一 ID、每个 ID 都能取到有效经纬度，坐标处于登记范围内。
- [ ] **步骤 2：写失败测试** `ProjectionKeepsAllHubsAndRoutesAlignedAtViewportSizes`，使用 16:9、方形、窄竖屏三种非退化地图内容尺寸，断言每个位置有限且位于全境投影范围；对每条路线用其稳定起终点 ID 查回节点投影位置，断言路线端点与相应节点位置一致。
- [ ] **步骤 3：实现** 在 `JapanMapGeography` 记录已批准的 37 点和 `FullExtent`；使用等距圆柱投影并按全境中心纬度修正经度尺度，底图纹理使用同一边界和纵横比，不修改 `JapanMapHub.MapX` / `MapY`。
- [ ] **步骤 4：实现** `ComputeGeographicLayout`，让图幅保持底图宽高比并留出边距；同一个枢纽坐标在任意尺寸下仍与其路线共用同一投影位置。
- [ ] **步骤 5：运行 EditMode 用例。** 通过条件为测试总数大于 0、两个指定测试均在 XML 中且全通过；过滤器匹配零项、失败或缺少 XML 均算失败。每轮串行运行后立即复制 `TestResults.xml`。

## 任务 2：全境底图视口与缩放拖动

**依赖：** 主控交付并登记底图资源。复用 Unity uGUI，不添加包。

**文件：**

- 新建：`Assets/Scripts/Presentation/JapanMapViewport.cs` 及 `.meta`
- 修改：`Assets/Scripts/Presentation/JapanMapScreen.cs`
- 测试：`Assets/Tests/EditMode/JapanMapViewportTests.cs` 及 `.meta`

**接口：**

- `JapanMapViewport.Bind(RectTransform viewport, RectTransform mapContent)`：绑定遮罩视口与底图内容。底图、路线和节点都以同一个 `mapContent` 为父级，投影使用该 RectTransform 的局部坐标；底图按同一 `FullExtent` 和纵横比铺设，不单独拉伸或另算边界。
- `JapanMapViewport.ResetToFullExtent()`：把地图缩放复位到全境适配并居中。
- `JapanMapViewport.ZoomAt(Vector2 screenPosition, float scrollDelta)`：按指针锚点缩放并约束边界。
- `JapanMapViewport.PanBy(Vector2 screenStart, Vector2 screenEnd)`：分别把拖动起点与终点映射到视口局部平面，按两点差值平移内容并约束边界，适用于透视且倾斜的视口。

- [x] **步骤 1：写失败测试** `ZoomAtPointerKeepsMapPoint`，缩放前后检查指针下同一地图点的局部坐标一致，边界触发时单独检查夹取行为。
- [x] **步骤 2：写失败测试** `PanAndZoomStayWithinBounds`，断言内容大于视口时任一边缘不露空白、内容小于视口时对应轴居中、复位后缩放为全境适配；另有 `TiltedPerspectiveViewportMapsPanFromRealDragPoints` 验证非屏幕原点拖动在透视倾斜视口中的起终点映射。
- [x] **步骤 3：实现** 在地图视口输入层处理拖动与滚轮；地图内容缩放不影响目录、详情、按钮或全屏界面。真实 EventSystem/InputSystemUIInputModule 虚拟鼠标测试覆盖超过拖动阈值后平移且不误选、短按仍选择并预览。
- [x] **步骤 4：实现** `BuildStaticUi` 将地图区域构造成单一 `RectMask2D` 视口和直接子级 `MapContent`；路线与节点共享内容变换，目录、筛选与详情留在视口外。实现与测试见 `074ba2e`，EditMode 接线 7/7。
- [x] **步骤 5：实现** 首次打开和视口尺寸变化时适配全境并回到 1x，倍率限制为 `1x` 至 `6x`。地理投影接入前，此处“全境”仍指现有地图内容范围，不代表地理底图上的 37 个地点。
- [x] **步骤 6：运行 EditMode 用例。** `JapanMapViewportTests` 11/11，`JapanMapScreenViewportTests` 7/7，`JapanMapOverlayRefreshTests` 5/5，`JapanMapPlayableTravelTests` 68/68。几何 XML `Logs/verify/20261124-pan-drag-start/green/TestResults.xml` SHA-256 `6affa2ed5ec57113d3112227a0f5627b61222118d668dd9d24883005152eb6b4`；屏幕接线及回归副本见 `Logs/verify/20261124-viewport-input/`。旧平移算法在 `TiltedPerspectiveViewportMapsPanFromRealDragPoints` 红灯 0/1，证实新测试能区分旧算法。PlayMode 另记于任务 3。

## 任务 3：旅行界面接线与 Play Mode 回归

**文件：**

- 修改：`Assets/Scripts/Presentation/JapanMapScreen.cs`
- 测试：`Assets/Tests/PlayMode/JapanGeographicMapPlayModeTests.cs` 及 `.meta`，加入现有 `SengokuChronicle.PlayModeTests.asmdef`
- 回归：`Assets/Tests/EditMode/JapanMapPlayableTravelTests.cs`、`Assets/Tests/EditMode/JapanMapOverlayRefreshTests.cs`

- [ ] **步骤 1：写失败测试** `FullMapShowsEveryHubAndRoute`，在 `FoundationScene` 运行时断言底图可用、37 个节点与 40 条路线都生成，筛选只改变路线可见性。
- [x] **步骤 2：写失败测试** 用项目真实 `EventSystem`、`GraphicRaycaster` 与 `InputSystemUIInputModule` 虚拟鼠标分别验证拖动、滚轮和短按选择；断言拖动/缩放/预览不改当前位置与日期，拖动不误选或产生预览。具体用例为 `VirtualMouse_DragFromHubPastThreshold_PansWithoutSelectingOrPreviewing`、`VirtualMouse_ScrollOverMap_ZoomsAtPointerWithoutChangingTravelFacts`、`VirtualMouse_ShortPressOnHub_SelectsAndPreviewsWithoutTraveling`。
- [ ] **步骤 3：实现** `RebuildMap` 使用地理投影生成路线和节点；筛选、目录选择、当前位置按钮、详情、旅行预览、确认、取消和关闭仍调用原有方法。
- [ ] **步骤 4：运行 Play Mode 指定用例** `FullMapShowsEveryHubAndRoute` 与 `VirtualMouseCanPanZoomAndSelectWithoutChangingTravelState`。通过条件为两例均出现在 XML、失败数为 0、结果总数大于 0；立即复制结果。
- [x] **步骤 5：串行运行** `JapanMapPlayableTravelTests` 68/68 与 `JapanMapOverlayRefreshTests` 5/5，确认既有旅行预览/确认、日期、当前位置和关闭刷新回归通过。对应副本位于 `Logs/verify/20261124-viewport-input/`；过滤器均命中用例。
- [ ] **步骤 6：使用现有项目 Editor 验证** 地理投影实现后，在集成工作树验证编译、Validator、全地图 Play Mode 与 EditMode；逐项记录 Console 增量、`FoundationScene isDirty` 和存档状态。当前视口分支已有独立编译和视口交互 PlayMode 证据，但它不验证尚未实现的 37 点投影、完整底图与地图集成，不得据此勾选本项。

## 任务 4：主控预验收与用户试玩 PR

- [ ] 主控检查编译和测试副本、底图资源登记、场景未变脏、Console 增量和现有存档兼容性；不保存或覆盖用户存档。
- [ ] 主控在 PR 正文列出用户逐步操作：打开全国地图、确认首次显示全日本、滚轮放大/缩小、拖动地图、放大后点不同枢纽、筛选路线、预览并取消一次旅行、再确认一次旅行、关闭地图并核对底层位置/日期。
- [ ] 每步写预期效果、通过/失败判据、发生缺陷时停止位置；明确自动化输入不等于实体鼠标/用户验收。
- [ ] 用户最终验收前不合并 `main`、不打 tag；若需用户决定坐标代表对象或素材许可，PR 明确列出且保持未闭合。

## 本计划的边界与未决风险

- 现有只读调查仍有未核实坐标和代表对象歧义；完整 37 点登记及主控核对是编码前置条件。
- 当前底图数据授权只支持计划使用 Natural Earth 1:10m 轮廓与原创纹理；不把未获确认的真实高程数据放入随游戏资源。
- 计划未授权修改路线、旅程时间、存档、地图覆盖层优先级或场景对象。发现这些改动成为必要条件时先停下汇报。
