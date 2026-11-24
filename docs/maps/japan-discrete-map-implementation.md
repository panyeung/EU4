# 日本全境离散地图：实现、兼容与阶段边界

状态：Unity attempt16 已通过编译、EditMode 与 FoundationScene Validator，并报告 KaiTi uGUI 回退的文本可读；仍缺真实 EventSystem/鼠标命中验证和用户验收。主控会话中另有指定点 RaycastAll、ExecuteHierarchy 与合成滚轮观测，证据等级和边界见 §8；它们不等同真实设备输入或用户验收。本文记录代码差异所依据的决策、ID 方案、存档兼容做法、场景接入、字体与素材边界、测试夹具和未决项。

## 1. 范围与阶段门

- 本项目计划 §16 要求在三代原型验证前暂缓“完整日本战国地图”。本次落地的是**稀疏、地点选择式**的全日本旅行图，不是连续开放世界，也不是默认畅通的全国公共路网。
- `FoundationScene` 中 `GameSession` 所在 GameObject 挂有 `JapanMapSceneBootstrap` 组件；运行时其 `Start` 定位 `GameSession`/`WorldMapController` 所在 Canvas（不可用时回退到场景首个 Canvas），再调用 `EnsureFor` 在该 Canvas 下幂等创建“全国地图”入口和地图根节点；地图、目录和路线卡读取同一 `GameSession` 权威状态。
- 既有 M02 尾张玩法保持原样：清洲、热田、那古野三个既有地点 ID、两条既有 1 天路线和热田↔那古野不可直达的规则都未改变。新增枢纽通过显式路线接入，不重编号既有地点或路线。
- 没有新增位图、图集、字体或第三方美术素材；地图呈现优先使用运行时 uGUI + TextMeshPro（TMP），TMP Settings 缺失时文本改用 uGUI `Text`，批准字体也不可用时明确报告缺字。不复制第三方或商业游戏地图。

## 2. 地理目录与旅行图分层

### 2.1 目录（`Assets/Scripts/Domain/JapanMapCatalog.cs`）

- 五畿 + 七道共 8 个目录域：`kinai`、`tokaido`、`tosando`、`hokurikudo`、`sanyodo`、`sanindo`、`nankaido`、`saikaido`。
- 68 个旧国，稳定 ID 形如 `province.owari`；安房为 `province.awa.kanto`，阿波为 `province.awa`，避免 ID 冲突。
- `ezo`（虾夷地）与 `ryukyu`（琉球）是独立旅行区域，不进入 68 国目录，也不假设其与普通旧国有陆路。
- 目录层级、显示名称和省份归属只用于呈现；存档身份永远是旅行枢纽/地点 ID，不是目录 ID。没有旅行枢纽的旧国只显示为目录条目，旧国本身不是可访问地点。

### 2.2 稀疏旅行图（`Assets/Scripts/Domain/JapanTravelGraph.cs`）

- 37 个旅行枢纽：3 个既有 M02 地点（ID 直接复用 `owari.kiyosu`、`owari.atsuta`、`owari.nagoya`），34 个新增地图枢纽。
- 40 条无向路线：2 条既有 M02 路线 + 38 条新增路线。每条路线有稳定 `RouteId`（端点规范化排序生成，例如 `route.mikawa.okazaki--owari.kiyosu`），并带：
  - 通行时期：长期走廊或战国设计值；
  - 控制/通行说明；
  - 路线类别（陆/海）；
  - 游戏内估算天数（全部是设计值，不是史料天数）；
  - 证据状态：`DocumentedCorridor`（区域走廊有史料）或 `DesignInference`（游戏设计推演）；
  - 来源说明。
- 东海道四段与和歌山—高松一段标为“史料走廊支持”，但具体路段、通行权和天数仍标设计推演。其余边标“游戏设计推演”。
- t10/t12 修正：删除不连贯的内陆海路 `aki.koriyama ↔ iyo.matsuyama`；虾夷地不再从内陆的会津直接跨海，而是 `mutsu.aizu --陆路 2 天(设计推演)--> mutsu.tosaminato（十三凑） --海路 6 天(设计推演)--> ezo.matsumae`。十三凑是新增的津轻半岛口岸枢纽；来源只证明区域贸易背景，不证明具体 1560 航线、天数或控制权。
- 琉球只通过 `satsuma.kagoshima --海路 8 天(设计推演)--> ryukyu.naha（那霸） --陆路 1 天--> ryukyu.shuri（首里）` 连接；海段明确不推定 1560 年控制权、通行权或定期班次，那霸—首里是琉球域内的短陆路接驳。
- t16 消除了把内陆城市直接当作海路端点：`sanuki.takamatsu --海路 2 天(设计推演)--> iyo.masaki（伊予松前） --陆路 1 天--> iyo.matsuyama`；`nagato.yamaguchi --陆路 1 天--> nagato.akama（赤间关） --海路 2 天(设计推演)--> chikuzen.hakata`；`satsuma.kagoshima --海路 8 天(设计推演)--> ryukyu.naha --陆路 1 天--> ryukyu.shuri`。新增港口枢纽只证明“中世/区域港口或贸易背景”，不证明具体 1560 港口对、控制、班次或天数；这些边和天数都保留为游戏设计推演。
- `JapanTravelGraph.Validate()` 检查：ID 唯一、坐标范围、地域/旧国一致、端点存在、无自环、无重复、天数正数、`RouteId` 非空且唯一、跨独立区域（虾夷地/琉球）必须为海路且同域内允许陆路接驳、说明非空、既有路线恰为两条且天数等于 `WorldMapRules`、热田↔那古野无直达、全图可达。

### 2.3 可玩接入与呈现

- `Assets/Scripts/Domain/LocationId.cs`：保留三个既有静态地点；`All` 从旅行图枢纽惰性生成，旧三 ID 仍是前三项；`TryParse` 与 `IsValid` 由图解析。
- `Assets/Scripts/Domain/WorldMapRules.cs`：改为无状态委派旅行图，旅行天数只来自图数据；同地点返回不可旅行，无路线返回 -1。
- `Assets/Scripts/Domain/LocationCapabilities.cs`：只有三个既有地点提供城镇服务、居民、当地行动、对话、委托和任务；新增枢纽是旅行枢纽，默认没有这些能力；只有清洲可开始战术任务；虾夷地/琉球标记为独立旅行区域。
- `Assets/Scripts/Application/GameSession.cs`：旅行预览/确认仍走 `PreviewTravel`/`ConfirmTravel`；非城镇枢纽在**后端**拒绝 NPC 对话开始、对话选项、三类地图任务、委托接取/指派，避免只靠隐藏 UI 菜单。
- `Assets/Scripts/Application/JapanMapExplorer.cs`：只读视图模型，负责五畿七道折叠、旧国选择、枢纽选择、陆/海筛选和路线卡生成；虾夷地与琉球单独列出，不混入 8 个省份目录组或 68 国计数。
- `Assets/Scripts/Application/JapanMapText.cs`：把目录、枢纽详情、路线卡渲染为中文文本，显式标注“史料走廊支持”/“游戏设计推演”和“既有 M2 路线”；对新增枢纽显示“没有城镇活动、居民或委托”，对空旧国显示“旧国本身不是可访问地点”。
- `Assets/Scripts/Domain/JapanGeoAnchors.cs`：37 个旅行枢纽的 GeoNames 区域锚点（`geonameId`、WGS84 纬度、经度），按稳定枢纽 ID 索引；声明顺序不影响按 ID 查找。坐标是区域近似代表点，不称城市中心、遗址中心、港口中心或精确历史地点；`MapX`/`MapY` 不被当作经纬度。`Validate()` 检查数量、ID/geonameId 唯一、经纬度有限且合法、落在日本投影范围内、锚点与枢纽一一对应，并由 `JapanTravelGraph.Validate()` 一并执行。来源与许可登记见 `docs/maps/geonames-attribution.md`。
- `Assets/Scripts/Presentation/JapanMapGeoLayout.cs`：`JapanGeoProjection` 把经纬度线性归一化为 x 东、y 北的 0..1 坐标（投影范围北纬 24°–46°、东经 122°–146°），非法坐标抛出 `ArgumentOutOfRangeException`。`JapanMapGeoLayout.ComputeLayout` 用该归一化坐标作为网格偏好格位，复用 `JapanMapLayout` 的网格容量、74×28 节点尺寸与最近空格规则；分配顺序按枢纽 ID 排序，因此改变枢纽输入顺序不改变任一 ID 的落点。真实锚点过密，落点只做可点击性所需的就近微调，不改变 ID、路线、天数或存档。
- `Assets/Scripts/Presentation/JapanMapLayout.cs`：纯静态布局，把归一化坐标映射到自适应网格并按最近空格确定节点位置；优先保留 74×28 点击目标。在 1280×720 的地图区（864×324）使用 10×10 网格；在审核报告的 720×720 窗口/逻辑 Canvas 960×960/地图区 648×432 下自动改为 8×10 网格，节点仍是 74×28，不会横向重叠。更窄的画布只有在本机确实放不下 74×28 时才按比例缩小节点，且始终保持在界内。若连缩小后的 74×28 网格也放不下全部枢纽，则按视口宽高比选择容量不少于枢纽数的列×行网格，再以列宽和行高为上限分别限制节点宽高；这样 100×100、100×1000、1000×100、1600×40 等极小或极端宽高比视口仍为每个枢纽分配唯一格位，不会回退到重复坐标。1×1 及以下视为退化视口，返回零位置且不绘制。布局不改动任何枢纽/路线 ID。`ComputeLayout` 的 `MapX`/`MapY` 入口保留为兼容路径，网格容量与摆放规则由地理布局共用。
- `Assets/Scripts/Presentation/JapanMapScreen.cs`：运行时 uGUI 界面，TMP Settings 可用时文本使用 TMP，缺失时由 `JapanMapFonts` 改用 uGUI `Text`；左栏 68 旧国/区域目录，右栏节点与陆/海连线，底部可滚动详情和“确认出发/取消”。节点和连线共用 `JapanMapGeoLayout.ComputeLayout` 的位置与节点尺寸，落点由经纬度投影决定，不再由 `MapX`/`MapY` 决定；`LateUpdate` 记录上次地图区尺寸，宽度或高度变化超过 0.5 像素时重建节点与连线，因此窗口 resize 后不会沿用旧格宽造成重叠。每个节点仍是铺满 74×28（窄屏为布局返回值）的 uGUI `Button`，标签不拦截点击。选中任意枢纽时，即使当前不可旅行，详情仍列出每条相邻路线的类型、时期、控制、证据、来源和“游戏设计推演”标注；选择可直达枢纽时调用 `GameSession.PreviewTravel`，确认调用 `GameSession.ConfirmTravel`，不另建旅行状态。详情区使用 `ScrollRect` + `ContentSizeFitter`，不再以 `Truncate` 裁切长文本。
- `Assets/Scripts/Presentation/JapanMapFonts.cs`：只从 `ui设计规划.md` 批准的字体列表（KaiTi、STKaiti、FangSong、SimSun、Noto Serif CJK SC、Microsoft YaHei）选择系统字体；不新增或分发字体文件。TMP Settings 存在且批准字体能生成 TMP 字体时走 TMP；TMP Settings 缺失或 TMP 创建失败时走 uGUI `Text`，仍只用同一批准字体。批准字体不可用时，代码状态/警告字符串与 `Debug.LogWarning` 记录“字体缺口”，文本使用不渲染的占位 `Graphic`；无字体时这些 UI 字符串本身也可能不可见，因此以 Console 警告为可审计提示。不遍历其他系统字体，不使用 TMP 默认字体或内置 `LegacyRuntime`/Arial 替代。实际系统字体覆盖、字形完整性和许可仍需目标机验收。
- `Assets/Scripts/Presentation/JapanMapPanel.cs`：仍保留为可选文本面板，支持既有 uGUI `Text` 与 `TMP_Text` 两种绑定；它不是场景必需入口。
- `Assets/Scripts/Presentation/JapanMapSceneBootstrap.cs`：`FoundationScene` 中的一个场景组件；运行时幂等创建“全国地图”按钮和地图根，入口标签经 `JapanMapFonts`（TMP 优先；TMP Settings 缺失时 uGUI `Text`；无批准字体时缺字占位），已存在则不重复创建，无 Canvas 时静默跳过。

## 3. 存档与迁移兼容

- 不升存档 schema 版本。`SaveDataValidator.CurrentVersion` 保持 17，`SaveData.CurrentRulesVersion` 保持 8；`SaveData` 字段与序列化形状未改，旧存档可原样读取。t18 没有新增 v18/rules 9，也没有把旧边写回图数据。
- 不重编号任何既有地点或路线；3 个既有地点 ID、2 条既有路线及 1 天耗时保持不变。
- 新增 `Assets/Scripts/Persistence/JapanMapSaveAudit.cs`，在 `SaveDataMigrator.FinishMigration` 中先执行原有 `SaveDataValidator.Validate`，只有原校验通过后才执行地图审计：
  1. `locationId` 必须能解析为旅行图中的枢纽；
  2. `dailyActions.persons` 中每个进行中的 Travel 行动，其起点/终点必须都在旅行图中；
  3. 当前路线天数，或**真正的 v17/rules 8 来源档**明确保留的三条退役在途边天数，必须等于存档的 `actionDurationDays`。
- **来源版本隔离**：迁移会先把工作副本改写成 v17/rules 8，因此审计不能读取改写后的 `saveFormatVersion`/`rulesVersion` 来判断兼容窗口。`FinishMigration` 把迁移前的 `legacy.saveFormatVersion` 与 `legacy.rulesVersion` 传给 `JapanMapSaveAudit.Validate(data, sourceFormatVersion, sourceRulesVersion)`；只有二者恰为 17/8 时，退役边兼容才生效。v16/v15 等旧源即使载荷已被改写成 17/8，途中旅行仍只接受当前旅行图的显式路线，带退役边的旧源会被地图审计拒绝。
- **在途旧路线兼容边界**：t16 之前开发期工作树中曾有 `sanuki.takamatsu ↔ iyo.matsuyama`（2 天）、`nagato.yamaguchi ↔ chikuzen.hakata`（2 天）、`satsuma.kagoshima ↔ ryukyu.shuri`（8 天）三条直达边；它们后来被港口/陆路接驳替换。仓库 HEAD 0bb2f4f 不包含日本地图文件（当前均未提交），所以这些边只可能出现在本机开发期生成的 v17/rules 8 存档中，不能假定不存在。
  - 支持的兼容（仅限来源版本为 v17/rules 8 的合成/开发档）：存档已有 Travel 行动且端点为上述某一对、方向任意、`actionDurationDays` 精确等于 2/2/8、`actionElapsedDays` 在 0..duration-1 时，`SaveData.ValidateForSave`、`DailyActionState.TryRestoreActive` 和 `JapanMapSaveAudit` 通过 `WorldMapRules.TryGetInFlightTravelDays` 接受并保留原 duration/elapsed；恢复后可继续逐日推进到原目的地。旧 v10–v16 来源不会获得该兼容窗口。
  - 不支持的兼容：旧边不加入 `JapanTravelGraph.Routes`，`TryGetTravelDays`/`GetTravelDays` 仍返回无路线，旅行预览、确认、按钮和地图连线不会把旧边重新暴露为新可选海路；不把旧边的两段新接驳当作等价替代，也不自动重算天数。天数不符、端点不在图中或人物状态无效仍拒绝。
  - 兼容查询只供存档恢复/迁移审计使用；新增规划仍只能使用当前图的显式边。
- 审计不替换原有错误信息；旧存档在原校验阶段的行为不变。旧 v10–v16 迁移路径仍走原来的补块逻辑，迁移完成后追加同一来源门控审计；带退役边的 v16/v15 旧源会被拒绝，而不是因为迁移先把版本改写成 17/8 而放行。
- 迁移与存读档测试覆盖：v16 旧档携带新枢纽当前位置与在途旅行，迁移后位置、起点/终点、3 天时长保留；当前 v17/rules 8 档仍保留热田位置与清洲→热田 1 天在途；新枢纽的取消预览、确认移动、跨月拒绝和旧/新在途端点均纳入 `JapanMapPlayableTravelTests`。
- t18/t22 新增三条退役边的合成 v17/rules 8 在途测试：每条边分别恢复、保留 duration/elapsed、继续一天抵达原目的地，并断言 `TryGetRoute`/`TryGetTravelDays` 仍为 false；迁移测试覆盖三条边各自的 duration/elapsed 保留；负例覆盖 v16/v15 旧源带同类退役边时迁移失败、`current` 为 null 且返回地图审计错误。夹具是合成数据，不是从用户目录复制的真实存档。
- 新增 JSON 往返测试：合成 v17 的旧两条 1 天路线、清洲→热田 1 天在途、清洲→冈崎 2 天在途（保存/恢复 `actionElapsedDays=1` 后继续走到目的地）。仓库没有真实 t2 之前的旧档，这些夹具是**合成数据**，不是从用户目录复制的真实存档。
- 后端对话门控测试覆盖“在清洲先开对话、旅行到冈崎后再提交旧选项”：选项被拒绝且关系值不变。

## 4. 数据来源、许可与素材登记

本实现没有新增美术或字体素材，因此不产生新的图片/字体许可登记；`ui设计规划.md` §10 已登记“无新增素材”。TMP Settings 存在时运行时 TMP 字体来自玩家系统；TMP Settings 缺失时 uGUI `Text` 使用同一批准列表系统字体；没有批准字体时不选择其他字体或内置字体，也不随项目分发。

史料来源（与 t1/t10 研究一致，均为公开网页；具体边界见各路线 `SourceNote`）：

- 国土交通省“道路的历史”五畿七道/东海道/室町至战国/宿场页面：`https://www.mlit.go.jp/road/michi-re/1-3.htm`、`2-1.htm`、`2-2.htm`、`2-3.htm`、`3-1.htm`
- 广岛县立历史博物馆濑户内海历史：`https://www.pref.hiroshima.lg.jp/site/rekishih/tushi.html`
- Britannica 阿伊努条目（只证分布，不证 1560 控制权）：`https://www.britannica.com/topic/Ainu`
- Wikipedia 琉球王国条目（1609 年前独立王国；不作 1560 路网/控制权依据）：`https://en.wikipedia.org/wiki/Ryukyu_Kingdom`
- UNESCO 琉球王国城堡遗址：`https://whc.unesco.org/en/list/972/`
- 北海道数字博物馆与上之国町（十三凑/道南贸易区域背景；不证明具体 1560 航线、天数或会津接驳）：`https://hokkaido-digital-museum.jp/zh-cn/hokkaido/kaitaku/k-01/`、`https://www.town.kaminokuni.lg.jp/hotnews/detail/00000294.html`

许可：代码与文本数据为本项目原创；不复制第三方或商业游戏地图/素材；TMP 与系统字体遵循其适用许可。

地图区域锚点来源（逐点追溯与许可登记见 `docs/maps/geonames-attribution.md`）：

- GeoNames JP dump：`https://download.geonames.org/export/dump/readme.txt`（WGS84 十进制度；单条记录精度未声明；按原样提供）
- GeoNames 导出与使用条款：`https://www.geonames.org/export/`（官方 export 条款允许商业使用并要求署名）
- 许可全文：`https://creativecommons.org/licenses/by/4.0/`（CC BY 4.0）

37 条锚点只保存稳定枢纽 ID、`geonameId`、纬度与经度；应用内署名位于地图详情面板（`JapanMapText.GeoAttributionNote`）。坐标是区域近似代表锚点，不是城市中心、遗址中心、港口中心或精确历史地点。

## 5. 场景接入

1. `FoundationScene` 的 `GameSession` 所在 GameObject 上挂有 `JapanMapSceneBootstrap` 组件；Canvas 不是该组件的宿主，而是运行时目标。`Start()` 找到 `GameSession` 和 `WorldMapController` 所在 Canvas（不可用时回退到场景首个 Canvas），再在该 Canvas 下幂等创建 `JapanMapLauncherButton`（“全国地图”）和 `JapanMapRoot`。
2. `WorldMapController.Start()` 也会在解析到 `GameSession` 后调用同一 `EnsureFor`，作为场景组件缺失时的兜底；两条路径都不会重复创建。
3. 入口按钮只开关地图；地图内的目录行、旧国行、区域行、节点按钮和筛选按钮都由 `JapanMapExplorer` 生成。
4. 确认旅行仍走 `GameSession.PreviewTravel/ConfirmTravel`；地图不直接改日期、位置或存档字段。
5. 地图优先使用运行时 uGUI + TMP；没有预置 TMP 字体资产，也没有分发字体。TMP Settings 缺失时文本改用 uGUI `Text`。若目标机器没有批准列表中的 CJK 系统字体，代码状态/警告字符串与 `Debug.LogWarning` 会记录字体缺口，文本使用不渲染占位；无字体时这些 UI 字符串本身也可能不可见，因此以 Console 警告为可审计提示。不会挑选其他系统字体或内置 `LegacyRuntime`/Arial 掩盖；这是当前已知的表现风险，不在本次新增未登记字体素材。实际字体覆盖、字形完整性和许可仍需实机验收。

## 6. 未决项与不在本次范围

- 1560 年逐条路线的通行权、关所、天数与港口关系；现有边只能作为游戏连通设计。
- 高松—伊予松前、赤间关—博多与鹿儿岛—那霸的具体 1560 航线、船期、控制权和天数仍无直接史料；新增港口枢纽只提供区域背景，相关边明确标为游戏设计推演。
- 虾夷地/琉球在具体年份的政治控制与可达性细节。
- 美术底图、图集、最终视觉布局和预置 TMP 字体资源；本次只接入 GeoNames 区域近似锚点与可验证的自适应节点网格，不新增素材。锚点精度未由 GeoNames 逐条声明，也不能还原战国期实际地点。
- 用户尚未选择的视觉/交互选项不在本实现中臆定。

## 7. 复核入口

- 数据：`JapanMapCatalog`、`JapanTravelGraph`、`JapanGeoAnchors`、`JapanTravelGraph.Validate()`
- 能力与规则：`LocationCapabilities`、`WorldMapRules`、`LocationId`
- 呈现与场景：`JapanMapExplorer`、`JapanMapText`、`JapanMapScreen`、`JapanMapGeoLayout`、`JapanMapLayout`、`JapanMapFonts`、`JapanMapSceneBootstrap`
- 存档：`JapanMapSaveAudit`、`SaveDataMigrator.FinishMigration`
- 测试：`Assets/Tests/EditMode/JapanMapTests.cs`、`Assets/Tests/EditMode/JapanMapPlayableTravelTests.cs`、`Assets/Tests/EditMode/JapanMapGeoLayoutTests.cs`、`Assets/Tests/EditMode/JapanGeoAnchorTests.cs`、`Assets/Tests/EditMode/WorldMapTravelTests.cs`
- t22 的验证边界：本任务按合同**未运行** Unity、Pipeline、Roslyn 编译或 EditMode/PlayMode 测试，因此不声称当前代码已编译或测试通过。静态检查覆盖 37 枢纽/40 路线、端点存在、`RouteId` 唯一、旧两条 1 天、无热田↔那古野、三组旧内陆海路直达已移除、十三凑—松前仍为设计推演、自适应布局在 864×324/648×432 与非同比 1280×320 下保持 74×28 且不重叠、窄画布 253×300 仍在界内、极小/极端视口 100×100/100×1000/1000×100/1600×40 每枢纽位置唯一，以及三条退役在途边的 v17/rules 8 恢复/迁移边界和 v16/v15 旧源拒绝。t12 曾用 Unity 自带 Roslyn 分层编译和临时探针得到 34 枢纽/37 路线；那是 t16 改动前的历史证据，不能替代本轮 Unity 验证。后续 Unity attempt16 已通过编译、EditMode 与 Validator，见第 3 行状态。
- t40 的验证边界：`TextAlignmentOptions.MidlineCenter` 已移除，映射只使用当前 TMP 包 `TMP_Text.cs` 中存在的 `Center`/`Midline` 等成员；字体选择只遍历批准列表，不再遍历任意系统字体，也不再使用 TMP 默认字体或内置 `LegacyRuntime`/Arial。静态检查仅验证源码/文档一致，不能替代 Unity 编译、EditMode/PlayMode 和真实机器字体覆盖；当前 Unity 编译/EditMode/Validator 与字体回退结果见第 3 行状态，真实机器字体覆盖仍待验收。

## 8. 交互证据等级与用户验收边界（2026-11-22 补记）

- 可据持久文件复核的自动证据：焦点测试 XML 为 total=64、passed=64、failed=0、result=Passed、duration=1.9745334s；全量测试 XML 为 total=2291、passed=2291、failed=0、result=Passed、duration=231.5402521s；`Logs/SceneValidationReport.txt:1257` 为 `RESULT: PASS; Errors: 0`。日志中原有的 7.92s 不是 XML 测试 duration，且其原始输出未持久化，不再作为测试耗时证据；以 1.9745334s 为准。
- 主控会话中报告的交互观测没有留下当时的原始命令返回、`Editor.log` 摘录或截图（截图已按规则目视检查后删除）。本文只如实转记这些会话记述，不声称本轮独立复现，也不把它们升级为真实设备输入或用户验收。
- 指定点射线（会话记述）：在有效的 SC-013 新游戏中，对 `CatalogViewport` 中心调用 `RaycastAll`，命中 `CatalogRow`，并沿层级找到祖先 `ScrollRect`。该记录显示该坐标的 UI 射线命中与层级关系，不能证明操作系统指针事件、Input System 设备事件或完整 EventSystem 指针派发链。
- 处理函数与合成滚动（会话记述）：通过 `ExecuteHierarchy` 调用滚动处理，使 `verticalNormalizedPosition` 从 1 变为 0.715；复位到 1 后注入合成滚轮坐标 `(400,650)`，读回 0。该记录显示指定处理函数被调用后的状态变化，不等同真实鼠标滚轮、触摸或键盘输入。
- 同一坐标开关图（会话记述）：在 `(30,1680)` 打开地图后，射线 top 为 `Canvas/JapanMapRoot` 且无 click handler；关闭地图后 top 与 handler 为 `ReferenceNavFamilyButton`。该结果只覆盖这一次指定坐标和当时层级，不能外推为所有分辨率、缩放或设备下的命中行为。
- 停止 Play 后未保存、`isDirty=false`（会话记述）：这只说明 Unity 场景没有未保存修改，不是 Git 工作树干净。当前仓库仍有未提交与未跟踪的地图代码、文档和用户改动；不得把 Editor clean 写成 Git clean。
- 仍未闭合：真实 EventSystem 指针/鼠标/触摸/键盘输入，真实鼠标滚轮，目标机字体覆盖，以及用户最终验收。未合并 `main`、未打 tag，也不提交或推送未完成用户验收的地图。