# 日本地图区域锚点：GeoNames 来源、许可与逐点追溯

状态：37 个旅行枢纽的区域锚点已接入领域数据，并由 `JapanMapGeoLayout` 按经纬度投影决定地图节点落点。底图美术、最终视觉布局与用户验收仍未完成。本文只登记数据来源、许可与逐点追溯，不表示底图或完整地图已交付。

## 1. 坐标口径

- 全部坐标是**全日本地图的区域近似代表锚点**，用于给地图节点一个稳定的、可追溯的地理偏好位置。
- 不称城市中心、遗址中心、港口中心，也不称精确历史地点。清洲、热田、那古野等相距仅数公里的地点，锚点只能区分区域，不能复现战国期的实际街区或建筑位置。
- 国家/区域 ID 与 GeoNames 坐标分开存放：`Assets/Scripts/Domain/JapanGeoAnchors.cs` 只按旅行枢纽稳定 ID 索引，不参与存档身份。
- `JapanMapHub.MapX` / `MapY` 仍是既有 `JapanMapLayout` 的兼容设计坐标，不当作经纬度，也不由经纬度反算。
- 旅行天数、路线与日期规则不来自地图几何；锚点只影响节点落点。

## 2. 来源

- GeoNames 官方导出说明：<https://download.geonames.org/export/dump/readme.txt>
  - 数据为 WGS84 十进制度；JP dump 为 `JP.zip`，坐标字段为 `latitude`、`longitude`。
  - 官方 readme 未对单条记录的精度作声明；本项目按原样接入，不保证精度，也不做坐标修正。
- GeoNames 导出与使用条款：<https://www.geonames.org/export/>
  - 官方 export 条款允许商业使用，并要求署名 GeoNames（Creative Commons Attribution 4.0）。
- 许可全文：<https://creativecommons.org/licenses/by/4.0/>

本轮接入依据是官方 export 说明与任务给定的 37 条候选行；**没有逐条访问 GeoNames 单条记录网页**，因此本文不声称单条网页已被直接核验。每条记录的 `geonameId` 可逐点追溯。

## 3. 应用内署名

地图详情面板固定附上来源与许可（`JapanMapText.GeoAttributionNote`）：

> 区域锚点：GeoNames JP dump（WGS84 十进制度，CC BY 4.0） https://www.geonames.org/export/ ；坐标为区域近似代表点，非城市中心或精确历史地点。

## 4. 37 个锚点

稳定枢纽 ID | GeoNames ID | 纬度 | 经度
---|---|---|---
owari.kiyosu | 1859210 | 35.21667 | 136.83333
owari.atsuta | 1864911 | 35.12901 | 136.89993
owari.nagoya | 1856053 | 35.18147 | 136.90641
yamashiro.kyoto | 1857910 | 35.02107 | 135.75385
settsu.osaka | 1853909 | 34.69379 | 135.50107
ise.yamada | 1861450 | 34.48333 | 136.70000
mikawa.okazaki | 1854376 | 34.95000 | 137.16667
suruga.sunpu | 1851717 | 34.98333 | 138.38333
kai.kofu | 1859100 | 35.66667 | 138.56667
sagami.odawara | 1854747 | 35.25556 | 139.15972
musashi.edo | 1850147 | 35.68950 | 139.69171
mino.inokuchi | 1863641 | 35.42291 | 136.76039
shinano.komoro | 1858858 | 36.31667 | 138.43333
mutsu.aizu | 6822148 | 37.49142 | 139.94546
mutsu.tosaminato | 12908995 | 41.02262 | 140.32009
dewa.yamagata | 2110556 | 38.23333 | 140.36667
echizen.ichijodani | 1863985 | 36.06443 | 136.22257
kaga.kanazawa | 1860243 | 36.60000 | 136.61667
etchu.toyama | 1849870 | 36.55146 | 137.29872
echigo.kasugayama | 11811984 | 37.14434 | 138.22968
harima.himeji | 1862627 | 34.81667 | 134.70000
bizen.okayama | 1854380 | 34.71251 | 133.92329
aki.koriyama | 11612331 | 34.67416 | 132.68188
nagato.yamaguchi | 1848689 | 34.18333 | 131.46667
nagato.akama | 1852225 | 33.95548 | 130.93713
izumo.izumo | 1861084 | 35.36667 | 132.76667
kii.wakayama | 1926004 | 34.23333 | 135.16667
sanuki.takamatsu | 1851100 | 34.33333 | 134.05000
iyo.matsuyama | 1926099 | 33.83916 | 132.76574
iyo.masaki | 1926101 | 33.78757 | 132.71124
tosa.kochi | 1859146 | 33.55000 | 133.53333
chikuzen.hakata | 1863967 | 33.60000 | 130.41667
bungo.funai | 1854487 | 33.23333 | 131.60000
satsuma.kagoshima | 1860827 | 31.56667 | 130.55000
ezo.matsumae | 2129282 | 41.42732 | 140.10298
ryukyu.naha | 1856035 | 26.21300 | 127.67851
ryukyu.shuri | 1861368 | 26.22478 | 127.73172

## 5. 投影与落点

- 投影范围与 `ui设计规划.md` §10 登记的 `japan_geographic_basemap.png` 一致：东经 122.5–147.0、北纬 23.5–46.0（登记像素 1824×2048），覆盖全部 37 点且留有余量；`JapanGeoAnchors.Validate()` 会拒绝有限性、经纬度范围或投影范围外的锚点。
- `JapanGeoProjection.ToNormalized` 把经纬度线性归一化为 x 东为正、y 北为正的 0..1 坐标，因此北在上、东在右；非法坐标抛出 `ArgumentOutOfRangeException`，不静默夹取。
- 地图矩形按登记底图的宽高比 1824/2048（约 0.8906）在视口内拟合，节点与底图共用这同一个矩形，经度、纬度不会被分别拉伸到不同纵横比的 RectTransform。较宽视口的左右留白不属于地图内容。
- 计划记载等距圆柱并按全境中心纬度（34.75°）修正经度尺度，理论宽高比为 24.5×cos(34.75°)/22.5 ≈ 0.8941，与登记 PNG 的实际 0.8906 相差约 0.4%。登记资料没有写明底图生成器使用的精确投影，因此保留这一差异，不声称像素级地理配准。
- `JapanMapGeoLayout` 复用 `JapanMapLayout` 的网格容量、74×28 节点尺寸与最近空格规则：真实锚点在通讯距离上过密，精确投影点会完全重叠，因此经纬度只决定偏好格位，落点只做可点击性所需的就近微调；落点是离散格位，不是底图像素级投影。
- 分配顺序按枢纽 ID 排序，不按输入列表顺序；改变枢纽声明顺序不改变任一 ID 的落点。

## 6. 验证入口

- `Assets/Tests/EditMode/JapanGeoAnchorTests.cs`：37 条锚点覆盖与唯一性、合法性、逐点 GeoName ID、按 ID 查找与声明顺序无关、投影南北/东西保序、非法坐标拒绝、应用内署名。
- `Assets/Tests/EditMode/JapanMapGeoLayoutTests.cs`：多种视口下有限、界内、互不重叠、退化视口、缺锚点拒绝、落点由锚点而非 `MapX`/`MapY` 决定、输入顺序无关。
- `JapanTravelGraph.Validate()` 追加锚点校验，既有 37 枢纽/40 路线/2 条既有路线断言不变。

## 7. 不在本文件范围

- 底图美术与底图许可：尚未接入；不复制或修改根主工作树未跟踪的底图资源。
- 单条 GeoNames 网页核对、坐标精度、与现代行政区划的对应关系。
- 用户验收、合并 `main`、打 tag。
