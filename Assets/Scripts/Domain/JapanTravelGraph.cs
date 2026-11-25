using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    public enum JapanRouteKind
    {
        Land = 0,
        Sea = 1,
    }

    /// <summary>How strongly the route is supported by the Phase 1 historical research.</summary>
    public enum JapanRouteEvidence
    {
        /// <summary>A regional corridor is documented; this exact segment and day count are not.</summary>
        DocumentedCorridor = 0,

        /// <summary>No direct evidence for this link; the edge exists only as game connectivity.</summary>
        DesignInference = 1,
    }

    public enum JapanRoutePeriod
    {
        /// <summary>A corridor that already existed before the Sengoku period.</summary>
        LongLivedCorridor = 0,

        /// <summary>A 1560-era link whose exact status is a design assumption.</summary>
        SengokuDesign = 1,
    }

    /// <summary>A visitable place on the sparse national travel graph.</summary>
    public sealed class JapanMapHub
    {
        public JapanMapHub(string id, string name, string provinceId, string domainId,
            float mapX, float mapY, bool isLegacyLocation)
        {
            Id = id;
            Name = name;
            ProvinceId = provinceId ?? string.Empty;
            DomainId = domainId;
            MapX = mapX;
            MapY = mapY;
            IsLegacyLocation = isLegacyLocation;
        }

        /// <summary>Stable hub ID. The three M02 hubs reuse their save location IDs verbatim.</summary>
        public string Id { get; }

        public string Name { get; }

        /// <summary>Empty only for the separate Ezo and Ryukyu domains.</summary>
        public string ProvinceId { get; }

        public string DomainId { get; }

        /// <summary>Normalized design coordinate, 0..1 from west to east.</summary>
        public float MapX { get; }

        /// <summary>Normalized design coordinate, 0..1 from north to south.</summary>
        public float MapY { get; }

        /// <summary>True for a location that already existed in the M02 save schema.</summary>
        public bool IsLegacyLocation { get; }
    }

    /// <summary>
    /// One undirected edge of the sparse travel graph. Days are always a game value:
    /// even a documented corridor does not establish a 1560 travel time.
    /// </summary>
    public sealed class JapanMapRoute
    {
        public JapanMapRoute(string fromHubId, string toHubId, int days,
            JapanRouteKind kind, JapanRouteEvidence evidence, JapanRoutePeriod period,
            string periodNote, string controlNote, string evidenceNote, string sourceNote,
            bool isLegacyRoute, string routeId = null)
        {
            RouteId = string.IsNullOrWhiteSpace(routeId)
                ? BuildRouteId(fromHubId, toHubId)
                : routeId;
            FromHubId = fromHubId;
            ToHubId = toHubId;
            Days = days;
            Kind = kind;
            Evidence = evidence;
            Period = period;
            PeriodNote = periodNote;
            ControlNote = controlNote;
            EvidenceNote = evidenceNote;
            SourceNote = sourceNote;
            IsLegacyRoute = isLegacyRoute;
        }

        /// <summary>
        /// Stable, display-independent route identity derived from the two hub IDs.
        /// Save data keeps referencing endpoints, so this ID can be audited without
        /// renumbering old locations or routes.
        /// </summary>
        public string RouteId { get; }

        public string FromHubId { get; }
        public string ToHubId { get; }
        public int Days { get; }
        public JapanRouteKind Kind { get; }
        public JapanRouteEvidence Evidence { get; }
        public JapanRoutePeriod Period { get; }
        public string PeriodNote { get; }
        public string ControlNote { get; }
        public string EvidenceNote { get; }
        public string SourceNote { get; }

        /// <summary>True only for the two M02 routes that must keep their identity and days.</summary>
        public bool IsLegacyRoute { get; }

        public bool Connects(string hubId) =>
            string.Equals(FromHubId, hubId, StringComparison.Ordinal) ||
            string.Equals(ToHubId, hubId, StringComparison.Ordinal);

        public string OtherHubId(string hubId) =>
            string.Equals(FromHubId, hubId, StringComparison.Ordinal) ? ToHubId : FromHubId;

        private static string BuildRouteId(string fromHubId, string toHubId)
        {
            string first = fromHubId ?? string.Empty;
            string second = toHubId ?? string.Empty;
            if (string.CompareOrdinal(first, second) > 0)
            {
                string swap = first;
                first = second;
                second = swap;
            }
            return "route." + first + "--" + second;
        }
    }

    /// <summary>
    /// Sparse nationwide travel graph. It is deliberately not a continuous open
    /// world: most provinces have no hub, and only explicit edges are traversable.
    /// The three M02 locations keep their IDs and the two M02 routes keep one day.
    /// Ezo and Ryukyu are separate domains reachable only by explicit sea edges.
    /// </summary>
    public static class JapanTravelGraph
    {
        public const string OwariKiyosuId = "owari.kiyosu";
        public const string OwariAtsutaId = "owari.atsuta";
        public const string OwariNagoyaId = "owari.nagoya";

        private static readonly JapanMapHub[] HubList =
        {
            // M02 legacy hubs. Their IDs are the existing save location IDs.
            // Coordinates are spread apart so the 1280x720 node grid does not
            // collapse the three Owari hubs into one cell.
            new JapanMapHub(OwariKiyosuId, "清洲", "province.owari", "tokaido", 0.47f, 0.55f, true),
            new JapanMapHub(OwariAtsutaId, "热田", "province.owari", "tokaido", 0.62f, 0.55f, true),
            new JapanMapHub(OwariNagoyaId, "那古野", "province.owari", "tokaido", 0.55f, 0.66f, true),

            new JapanMapHub("yamashiro.kyoto", "京都", "province.yamashiro", "kinai", 0.38f, 0.60f, false),
            new JapanMapHub("settsu.osaka", "大坂", "province.settsu", "kinai", 0.36f, 0.64f, false),
            new JapanMapHub("ise.yamada", "山田", "province.ise", "tokaido", 0.48f, 0.63f, false),
            new JapanMapHub("mikawa.okazaki", "冈崎", "province.mikawa", "tokaido", 0.60f, 0.56f, false),
            new JapanMapHub("suruga.sunpu", "骏府", "province.suruga", "tokaido", 0.68f, 0.54f, false),
            new JapanMapHub("kai.kofu", "甲府", "province.kai", "tokaido", 0.70f, 0.48f, false),
            new JapanMapHub("sagami.odawara", "小田原", "province.sagami", "tokaido", 0.74f, 0.52f, false),
            new JapanMapHub("musashi.edo", "江户", "province.musashi", "tokaido", 0.79f, 0.50f, false),
            new JapanMapHub("mino.inokuchi", "稻叶山", "province.mino", "tosando", 0.49f, 0.52f, false),
            new JapanMapHub("shinano.komoro", "小诸", "province.shinano", "tosando", 0.61f, 0.43f, false),
            new JapanMapHub("mutsu.aizu", "会津", "province.mutsu", "tosando", 0.73f, 0.32f, false),
            new JapanMapHub("mutsu.tosaminato", "十三凑", "province.mutsu", "tosando", 0.80f, 0.18f, false),
            new JapanMapHub("dewa.yamagata", "山形", "province.dewa", "tosando", 0.76f, 0.24f, false),
            new JapanMapHub("echizen.ichijodani", "一乘谷", "province.echizen", "hokurikudo", 0.44f, 0.47f, false),
            new JapanMapHub("kaga.kanazawa", "金泽", "province.kaga", "hokurikudo", 0.48f, 0.42f, false),
            new JapanMapHub("etchu.toyama", "富山", "province.etchu", "hokurikudo", 0.52f, 0.40f, false),
            new JapanMapHub("echigo.kasugayama", "春日山", "province.echigo", "hokurikudo", 0.57f, 0.35f, false),
            new JapanMapHub("harima.himeji", "姬路", "province.harima", "sanyodo", 0.33f, 0.68f, false),
            new JapanMapHub("bizen.okayama", "冈山", "province.bizen", "sanyodo", 0.30f, 0.74f, false),
            new JapanMapHub("aki.koriyama", "郡山", "province.aki", "sanyodo", 0.24f, 0.79f, false),
            new JapanMapHub("nagato.yamaguchi", "山口", "province.nagato", "sanyodo", 0.17f, 0.76f, false),
            new JapanMapHub("nagato.akama", "赤间关", "province.nagato", "sanyodo", 0.13f, 0.75f, false),
            new JapanMapHub("izumo.izumo", "出云", "province.izumo", "sanindo", 0.23f, 0.64f, false),
            new JapanMapHub("kii.wakayama", "和歌山", "province.kii", "nankaido", 0.40f, 0.70f, false),
            new JapanMapHub("sanuki.takamatsu", "高松", "province.sanuki", "nankaido", 0.38f, 0.78f, false),
            new JapanMapHub("iyo.matsuyama", "松山", "province.iyo", "nankaido", 0.30f, 0.81f, false),
            new JapanMapHub("iyo.masaki", "伊予松前", "province.iyo", "nankaido", 0.26f, 0.82f, false),
            new JapanMapHub("tosa.kochi", "高知", "province.tosa", "nankaido", 0.33f, 0.87f, false),
            new JapanMapHub("chikuzen.hakata", "博多", "province.chikuzen", "saikaido", 0.14f, 0.82f, false),
            new JapanMapHub("bungo.funai", "府内", "province.bungo", "saikaido", 0.17f, 0.89f, false),
            new JapanMapHub("satsuma.kagoshima", "鹿儿岛", "province.satsuma", "saikaido", 0.13f, 0.95f, false),

            // Separate travel domains. They are not part of the sixty-eight provinces.
            new JapanMapHub("ezo.matsumae", "松前", string.Empty, "ezo", 0.86f, 0.10f, false),
            new JapanMapHub("ryukyu.naha", "那霸", string.Empty, "ryukyu", 0.02f, 0.93f, false),
            new JapanMapHub("ryukyu.shuri", "首里", string.Empty, "ryukyu", 0.03f, 0.97f, false),
        };

        private static readonly JapanMapRoute[] RouteList =
        {
            // M02 legacy edges: ID and days are frozen.
            Legacy(OwariKiyosuId, OwariAtsutaId, 1),
            Legacy(OwariKiyosuId, OwariNagoyaId, 1),

            // Tokaido corridor. The corridor is documented; each segment and day count is a design value.
            Land(OwariKiyosuId, "mikawa.okazaki", 2, true),
            Land("mikawa.okazaki", "suruga.sunpu", 3, true),
            Land("suruga.sunpu", "sagami.odawara", 2, true),
            Land("sagami.odawara", "musashi.edo", 2, true),
            Land("suruga.sunpu", "kai.kofu", 2, false),
            Land(OwariKiyosuId, "ise.yamada", 2, false),
            Land(OwariKiyosuId, "mino.inokuchi", 2, false),
            Land("mino.inokuchi", "yamashiro.kyoto", 3, false),
            Land("mino.inokuchi", "shinano.komoro", 3, false),
            Land("shinano.komoro", "musashi.edo", 4, false),
            Land("shinano.komoro", "echigo.kasugayama", 3, false),

            // Hokuriku and Tohoku regional links.
            Land("mino.inokuchi", "echizen.ichijodani", 3, false),
            Land("echizen.ichijodani", "kaga.kanazawa", 2, false),
            Land("kaga.kanazawa", "etchu.toyama", 2, false),
            Land("etchu.toyama", "echigo.kasugayama", 2, false),
            Land("echigo.kasugayama", "dewa.yamagata", 4, false),
            Land("dewa.yamagata", "mutsu.aizu", 4, false),
            Land("mutsu.aizu", "mutsu.tosaminato", 2, false),
            Land("musashi.edo", "mutsu.aizu", 5, false),

            // Kinai, Sanyo and Sanin links.
            Land("yamashiro.kyoto", "settsu.osaka", 1, false),
            Land("yamashiro.kyoto", "harima.himeji", 3, false),
            Land("harima.himeji", "bizen.okayama", 2, false),
            Land("bizen.okayama", "aki.koriyama", 3, false),
            Land("aki.koriyama", "nagato.yamaguchi", 3, false),
            Land("yamashiro.kyoto", "izumo.izumo", 5, false),
            Land("izumo.izumo", "bizen.okayama", 4, false),
            Land("yamashiro.kyoto", "kii.wakayama", 2, false),

            // Seto Inland Sea corridor: region documented, pairs/schedules design values.
            // Takamatsu and Masaki are the explicit port hubs; Matsuyama is reached
            // by a short land connector, so the inland city is not shown as a port.
            Sea("kii.wakayama", "sanuki.takamatsu", 2, true),
            Sea("sanuki.takamatsu", "iyo.masaki", 2, false,
                "伊予松前港有中世至近世良港的町史背景（https://www.town.masaki.ehime.jp/soshiki/16/tenpo.html）；不证明1560年高松—松前航线、控制、班次或2天，属游戏设计推演。"),
            Land("iyo.masaki", "iyo.matsuyama", 1, false),
            Land("iyo.matsuyama", "tosa.kochi", 2, false),

            // Kyushu links. Yamaguchi is inland; Akama is the explicit port hub.
            Land("nagato.yamaguchi", "nagato.akama", 1, false),
            Sea("nagato.akama", "chikuzen.hakata", 2, false,
                "赤间关属大内氏、16世纪中期后由毛利氏直辖并参与对外交易（https://www.shimohaku.jp/o/contents.html?node=0102&contnum=5）；不证明1560年赤间关—博多航线、控制、班次或2天，属游戏设计推演。"),
            Land("chikuzen.hakata", "bungo.funai", 3, false),
            Land("bungo.funai", "satsuma.kagoshima", 4, false),

            // Remote domains: only explicit sea edges, no assumed regular service or control.
            // Aizu is inland; Tosaminato is the explicit port connector. Both hops remain design inference.
            RemoteSea("mutsu.tosaminato", "ezo.matsumae", 6,
                "https://hokkaido-digital-museum.jp/zh-cn/hokkaido/kaitaku/k-01/ ; https://www.town.kaminokuni.lg.jp/hotnews/detail/00000294.html（区域背景；不证明具体1560航线、天数或控制）"),
            // Ryukyu is reached at Naha; Shuri is an in-domain land connector.
            Sea("satsuma.kagoshima", "ryukyu.naha", 8, false,
                "琉球1609年前为独立王国（https://en.wikipedia.org/wiki/Ryukyu_Kingdom）；不证明1560年鹿儿岛—那霸航线、控制、班次或8天，属游戏设计推演。"),
            Land("ryukyu.naha", "ryukyu.shuri", 1, false),
        };

        private static readonly Dictionary<string, JapanMapHub> HubsById = BuildHubIndex();

        public static IReadOnlyList<JapanMapHub> Hubs => HubList;
        public static IReadOnlyList<JapanMapRoute> Routes => RouteList;

        public static bool TryGetHub(string hubId, out JapanMapHub hub)
        {
            if (string.IsNullOrEmpty(hubId))
            {
                hub = null;
                return false;
            }

            return HubsById.TryGetValue(hubId, out hub);
        }

        /// <summary>
        /// Maps any legal save location to its hub. Legacy IDs and new hub IDs share
        /// one resolver, so GameSession and save auditing keep a single travel path.
        /// </summary>
        public static bool TryGetHubForLocation(LocationId location, out JapanMapHub hub)
        {
            return TryGetHub(location.Value, out hub);
        }

        public static bool TryGetRouteById(string routeId, out JapanMapRoute route)
        {
            route = null;
            if (string.IsNullOrEmpty(routeId))
                return false;

            for (int i = 0; i < RouteList.Length; i++)
            {
                JapanMapRoute candidate = RouteList[i];
                if (string.Equals(candidate.RouteId, routeId, StringComparison.Ordinal))
                {
                    route = candidate;
                    return true;
                }
            }

            return false;
        }

        public static bool TryGetRoute(string fromHubId, string toHubId, out JapanMapRoute route)
        {
            route = null;
            if (string.IsNullOrEmpty(fromHubId) || string.IsNullOrEmpty(toHubId) ||
                string.Equals(fromHubId, toHubId, StringComparison.Ordinal))
                return false;

            for (int i = 0; i < RouteList.Length; i++)
            {
                JapanMapRoute candidate = RouteList[i];
                if (candidate.Connects(fromHubId) && candidate.Connects(toHubId))
                {
                    route = candidate;
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyList<JapanMapRoute> GetRoutesFrom(string hubId)
        {
            var result = new List<JapanMapRoute>();
            if (string.IsNullOrEmpty(hubId))
                return result;

            for (int i = 0; i < RouteList.Length; i++)
                if (RouteList[i].Connects(hubId))
                    result.Add(RouteList[i]);
            return result;
        }

        public static IReadOnlyList<JapanMapHub> GetHubsForProvince(string provinceId)
        {
            var result = new List<JapanMapHub>();
            if (string.IsNullOrEmpty(provinceId))
                return result;

            for (int i = 0; i < HubList.Length; i++)
                if (string.Equals(HubList[i].ProvinceId, provinceId, StringComparison.Ordinal))
                    result.Add(HubList[i]);
            return result;
        }

        public static IReadOnlyList<JapanMapHub> GetHubsForDomain(string domainId)
        {
            var result = new List<JapanMapHub>();
            if (string.IsNullOrEmpty(domainId))
                return result;

            for (int i = 0; i < HubList.Length; i++)
                if (string.Equals(HubList[i].DomainId, domainId, StringComparison.Ordinal))
                    result.Add(HubList[i]);
            return result;
        }

        /// <summary>
        /// Returns every structural problem found in the hard-coded graph. An empty
        /// list is the contract used by the EditMode tests and future editor checks.
        /// </summary>
        public static IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();
            var hubIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < HubList.Length; i++)
            {
                JapanMapHub hub = HubList[i];
                if (string.IsNullOrEmpty(hub.Id))
                    issues.Add("旅行枢纽缺少 ID。");
                else if (!hubIds.Add(hub.Id))
                    issues.Add("旅行枢纽 ID 重复：" + hub.Id);
                if (string.IsNullOrEmpty(hub.Name))
                    issues.Add("旅行枢纽缺少名称：" + hub.Id);
                if (hub.MapX < 0f || hub.MapX > 1f || hub.MapY < 0f || hub.MapY > 1f)
                    issues.Add("旅行枢纽坐标越界：" + hub.Id);
                if (!JapanMapCatalog.TryGetDomain(hub.DomainId, out JapanMapDomain domain))
                {
                    issues.Add("旅行枢纽引用了未知地域：" + hub.Id);
                }
                else if (domain.IsProvinceDomain)
                {
                    if (!JapanMapCatalog.TryGetProvince(hub.ProvinceId, out JapanProvince province))
                        issues.Add("旅行枢纽引用了未知旧国：" + hub.Id);
                    else if (!string.Equals(province.DomainId, hub.DomainId, StringComparison.Ordinal))
                        issues.Add("旅行枢纽与旧国所属地域不一致：" + hub.Id);
                }
                else if (!string.IsNullOrEmpty(hub.ProvinceId))
                {
                    issues.Add("独立区域枢纽不应属于旧国：" + hub.Id);
                }

                if (hub.IsLegacyLocation)
                {
                    if (!LocationId.TryParse(hub.Id, out LocationId legacy) || !legacy.IsValid)
                        issues.Add("既有旅行枢纽 ID 不是合法存档地点：" + hub.Id);
                }
            }

            var routeKeys = new HashSet<string>(StringComparer.Ordinal);
            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            int legacyRouteCount = 0;
            for (int i = 0; i < RouteList.Length; i++)
            {
                JapanMapRoute route = RouteList[i];
                if (string.IsNullOrWhiteSpace(route.RouteId))
                    issues.Add("路线缺少稳定 RouteId：" + route.FromHubId + " ↔ " + route.ToHubId);
                else if (!routeIds.Add(route.RouteId))
                    issues.Add("路线 RouteId 重复：" + route.RouteId);
                if (!hubIds.Contains(route.FromHubId) || !hubIds.Contains(route.ToHubId))
                    issues.Add("路线端点不存在：" + route.FromHubId + " ↔ " + route.ToHubId);
                if (string.Equals(route.FromHubId, route.ToHubId, StringComparison.Ordinal))
                    issues.Add("路线不能连接自身：" + route.FromHubId);
                if (route.Days <= 0)
                    issues.Add("路线天数必须为正：" + route.FromHubId + " ↔ " + route.ToHubId);

                string key = string.CompareOrdinal(route.FromHubId, route.ToHubId) < 0
                    ? route.FromHubId + "|" + route.ToHubId
                    : route.ToHubId + "|" + route.FromHubId;
                if (!routeKeys.Add(key))
                    issues.Add("路线重复：" + key);

                if (route.IsLegacyRoute)
                    legacyRouteCount++;

                bool fromRemote = IsRemoteHub(route.FromHubId, out string fromRemoteDomain);
                bool toRemote = IsRemoteHub(route.ToHubId, out string toRemoteDomain);
                bool sameRemoteDomain = fromRemote && toRemote &&
                    string.Equals(fromRemoteDomain, toRemoteDomain, StringComparison.Ordinal);
                if ((fromRemote || toRemote) && !sameRemoteDomain &&
                    route.Kind != JapanRouteKind.Sea)
                    issues.Add("跨独立区域必须通过海路连接：" + key);

                if (string.IsNullOrEmpty(route.PeriodNote) ||
                    string.IsNullOrEmpty(route.ControlNote) ||
                    string.IsNullOrEmpty(route.EvidenceNote) ||
                    string.IsNullOrEmpty(route.SourceNote))
                    issues.Add("路线缺少时期、控制、证据或来源说明：" + key);
            }

            if (legacyRouteCount != 2)
                issues.Add("既有 M02 路线数量必须保持为 2。");
            CheckLegacyRoute(LocationId.Kiyosu, LocationId.Atsuta, 1, issues);
            CheckLegacyRoute(LocationId.Kiyosu, LocationId.Nagoya, 1, issues);
            if (TryGetRoute(OwariAtsutaId, OwariNagoyaId, out _))
                issues.Add("热田与那古野不得新增直达路线。");

            CheckReachability(hubIds, issues);
            return issues;
        }

        private static void CheckLegacyRoute(LocationId from, LocationId to, int expectedDays,
            List<string> issues)
        {
            if (!TryGetHubForLocation(from, out JapanMapHub fromHub) ||
                !TryGetHubForLocation(to, out JapanMapHub toHub))
            {
                issues.Add("既有地点缺少旅行枢纽：" + from.Value + " 或 " + to.Value);
                return;
            }

            if (!TryGetRoute(fromHub.Id, toHub.Id, out JapanMapRoute route))
            {
                issues.Add("既有路线丢失：" + from.Value + " ↔ " + to.Value);
                return;
            }

            if (!LegacyRouteIsIntact(route, expectedDays))
                issues.Add("既有路线身份或天数被改变：" + from.Value + " ↔ " + to.Value);
        }

        /// <summary>
        /// The frozen M02 identity and a caller-supplied expected day count. The expected
        /// days are passed literally by Validate instead of being read back from
        /// WorldMapRules.GetTravelDays, which resolves the same route and would make the
        /// comparison tautologically true.
        /// </summary>
        internal static bool LegacyRouteIsIntact(JapanMapRoute route, int expectedDays) =>
            route != null &&
            route.IsLegacyRoute &&
            route.Kind == JapanRouteKind.Land &&
            route.Days == expectedDays;

        private static void CheckReachability(HashSet<string> hubIds, List<string> issues)
        {
            if (hubIds.Count == 0)
                return;

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            string start = HubList[0].Id;
            visited.Add(start);
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                string current = queue.Dequeue();
                for (int i = 0; i < RouteList.Length; i++)
                {
                    JapanMapRoute route = RouteList[i];
                    if (!route.Connects(current))
                        continue;
                    string next = route.OtherHubId(current);
                    if (visited.Add(next))
                        queue.Enqueue(next);
                }
            }

            foreach (string hubId in hubIds)
                if (!visited.Contains(hubId))
                    issues.Add("旅行枢纽不可达：" + hubId);
        }

        private static bool IsRemoteHub(string hubId, out string domainId)
        {
            domainId = string.Empty;
            if (!TryGetHub(hubId, out JapanMapHub hub))
                return false;
            if (string.Equals(hub.DomainId, "ezo", StringComparison.Ordinal) ||
                string.Equals(hub.DomainId, "ryukyu", StringComparison.Ordinal))
            {
                domainId = hub.DomainId;
                return true;
            }

            return false;
        }

        private static JapanMapRoute Land(string from, string to, int days, bool documented)
        {
            return new JapanMapRoute(from, to, days, JapanRouteKind.Land,
                documented ? JapanRouteEvidence.DocumentedCorridor : JapanRouteEvidence.DesignInference,
                documented ? JapanRoutePeriod.LongLivedCorridor : JapanRoutePeriod.SengokuDesign,
                documented ? "长期走廊（室町以前至战国）" : "战国期（设计值）",
                documented
                    ? "受庄园、守护与私关分割（史料）；具体通行权未核实。"
                    : "地方势力控制；通行需交涉（设计推演）。",
                documented
                    ? "区域走廊有史料；本段具体走向、通行权与天数为设计推演。"
                    : "无1560年直达路线史料；仅作游戏连通设计。",
                documented
                    ? "https://www.mlit.go.jp/road/michi-re/2-1.htm ; https://www.mlit.go.jp/road/michi-re/2-2.htm"
                    : "无直接史料；游戏设计推演",
                false);
        }

        private static JapanMapRoute Sea(string from, string to, int days, bool documented,
            string sourceNote = null)
        {
            return new JapanMapRoute(from, to, days, JapanRouteKind.Sea,
                documented ? JapanRouteEvidence.DocumentedCorridor : JapanRouteEvidence.DesignInference,
                documented ? JapanRoutePeriod.LongLivedCorridor : JapanRoutePeriod.SengokuDesign,
                documented ? "长期海运走廊（室町以前至战国）" : "战国期（设计值）",
                documented
                    ? "濑户内海海运长期存在（史料）；班次、港口关系与通行权未核实。"
                    : "不推定定期班次、港口关系或通行权（设计推演）。",
                documented
                    ? "区域海运走廊有史料；本段航线与天数为设计推演。"
                    : "无1560年航线史料；仅作游戏连通设计。",
                documented
                    ? "https://www.pref.hiroshima.lg.jp/site/rekishih/tushi.html"
                    : sourceNote ?? "无直接史料；游戏设计推演",
                false);
        }

        private static JapanMapRoute RemoteSea(string from, string to, int days, string sourceNote)
        {
            return new JapanMapRoute(from, to, days, JapanRouteKind.Sea,
                JapanRouteEvidence.DesignInference, JapanRoutePeriod.SengokuDesign,
                "1560年控制与航路未核实（设计推演）",
                "不推定1560年控制权、通行权或定期班次。",
                "仅表示游戏内可沿此海路进入该独立区域；不是史实航线。",
                sourceNote, false);
        }

        private static JapanMapRoute Legacy(string from, string to, int days)
        {
            return new JapanMapRoute(from, to, days, JapanRouteKind.Land,
                JapanRouteEvidence.DesignInference, JapanRoutePeriod.SengokuDesign,
                "M2 既有游戏规则（非史实天数）",
                "既有局内规则；不表示1560年通行权。",
                "既有 M2 直达路线，保持 ID 与天数不变；天数为设计值。",
                "项目既有 WorldMapRules", true);
        }

        private static Dictionary<string, JapanMapHub> BuildHubIndex()
        {
            var index = new Dictionary<string, JapanMapHub>(StringComparer.Ordinal);
            for (int i = 0; i < HubList.Length; i++)
                index.Add(HubList[i].Id, HubList[i]);
            return index;
        }
    }
}
