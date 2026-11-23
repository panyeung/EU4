using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// National map data, catalog separation, sparse graph and save compatibility.
    /// The M02 Owari graph is frozen; new content is additive and save-stable.
    /// </summary>
    public sealed class JapanMapTests
    {
        [Test]
        public void Catalog_HasEightProvinceDomainsAndSixtyEightProvinces()
        {
            int provinceDomains = 0;
            for (int i = 0; i < JapanMapCatalog.Domains.Count; i++)
                if (JapanMapCatalog.Domains[i].IsProvinceDomain)
                    provinceDomains++;

            Assert.That(provinceDomains, Is.EqualTo(8));
            Assert.That(JapanMapCatalog.Provinces.Count, Is.EqualTo(68));
        }

        [Test]
        public void Catalog_IdsAreUniqueAndOwariOwnsTheThreeLegacyHubs()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < JapanMapCatalog.Domains.Count; i++)
                Assert.That(ids.Add(JapanMapCatalog.Domains[i].Id), Is.True,
                    "duplicate domain id " + JapanMapCatalog.Domains[i].Id);
            for (int i = 0; i < JapanMapCatalog.Provinces.Count; i++)
                Assert.That(ids.Add(JapanMapCatalog.Provinces[i].Id), Is.True,
                    "duplicate province id " + JapanMapCatalog.Provinces[i].Id);

            IReadOnlyList<JapanMapHub> owari =
                JapanTravelGraph.GetHubsForProvince("province.owari");
            Assert.That(owari.Count, Is.EqualTo(3));
            var legacyIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < owari.Count; i++)
            {
                Assert.That(owari[i].IsLegacyLocation, Is.True);
                legacyIds.Add(owari[i].Id);
            }

            Assert.That(legacyIds.Contains(LocationId.Kiyosu.Value), Is.True);
            Assert.That(legacyIds.Contains(LocationId.Atsuta.Value), Is.True);
            Assert.That(legacyIds.Contains(LocationId.Nagoya.Value), Is.True);
        }

        [Test]
        public void EzoAndRyukyu_AreSeparateTravelDomainsNotProvinces()
        {
            Assert.That(JapanMapCatalog.TryGetProvince("province.ezo", out _), Is.False);
            Assert.That(JapanMapCatalog.TryGetProvince("province.ryukyu", out _), Is.False);

            Assert.That(JapanMapCatalog.TryGetDomain("ezo", out JapanMapDomain ezo), Is.True);
            Assert.That(ezo.IsProvinceDomain, Is.False);
            Assert.That(JapanMapCatalog.TryGetDomain("ryukyu", out JapanMapDomain ryukyu), Is.True);
            Assert.That(ryukyu.IsProvinceDomain, Is.False);

            Assert.That(JapanTravelGraph.TryGetHub("ezo.matsumae", out JapanMapHub ezoHub), Is.True);
            Assert.That(ezoHub.ProvinceId, Is.Empty);
            Assert.That(JapanTravelGraph.TryGetHub("ryukyu.shuri", out JapanMapHub ryukyuHub), Is.True);
            Assert.That(ryukyuHub.ProvinceId, Is.Empty);

            var explorer = new JapanMapExplorer(LocationId.Kiyosu);
            IReadOnlyList<JapanMapCatalogRow> regions = explorer.BuildTravelRegionRows();
            Assert.That(regions.Count, Is.EqualTo(2));
            Assert.That(regions[0].Kind, Is.EqualTo(JapanMapRowKind.TravelRegion));
            Assert.That(regions[0].Id, Is.EqualTo("ezo"));
            Assert.That(regions[0].HubNames, Does.Contain("松前"));
            Assert.That(regions[1].Id, Is.EqualTo("ryukyu"));
            Assert.That(regions[1].HubNames, Does.Contain("首里"));
            string catalog = JapanMapText.BuildCatalog(explorer);
            Assert.That(catalog, Does.Contain("独立旅行区域"));
            Assert.That(catalog, Does.Contain("虾夷地"));
            Assert.That(catalog, Does.Contain("琉球"));
        }

        [Test]
        public void LegacyRoutes_KeepTheirIdsAndOneDayCost()
        {
            Assert.That(JapanTravelGraph.TryGetRoute(LocationId.Kiyosu.Value,
                LocationId.Atsuta.Value, out JapanMapRoute atsuta), Is.True);
            Assert.That(atsuta.Days, Is.EqualTo(1));
            Assert.That(atsuta.IsLegacyRoute, Is.True);
            Assert.That(JapanTravelGraph.TryGetRoute(LocationId.Kiyosu.Value,
                LocationId.Nagoya.Value, out JapanMapRoute nagoya), Is.True);
            Assert.That(nagoya.Days, Is.EqualTo(1));
            Assert.That(nagoya.IsLegacyRoute, Is.True);

            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Kiyosu, LocationId.Atsuta,
                out int worldDays), Is.True);
            Assert.That(worldDays, Is.EqualTo(1));
            Assert.That(JapanTravelGraph.TryGetRoute(LocationId.Atsuta.Value,
                LocationId.Nagoya.Value, out _), Is.False,
                "热田与那古野不得新增直达边，旧存档和旧测试依赖这一事实。");
        }

        [Test]
        public void LegacyRouteDayValidation_RejectsAChangedDayCount()
        {
            Assert.That(JapanTravelGraph.TryGetRoute(LocationId.Kiyosu.Value,
                LocationId.Atsuta.Value, out JapanMapRoute actual), Is.True);
            Assert.That(JapanTravelGraph.LegacyRouteIsIntact(actual, 1), Is.True);

            var changed = new JapanMapRoute(actual.FromHubId, actual.ToHubId, 2,
                actual.Kind, actual.Evidence, actual.Period, actual.PeriodNote,
                actual.ControlNote, actual.EvidenceNote, actual.SourceNote, true);
            Assert.That(JapanTravelGraph.LegacyRouteIsIntact(changed, 1), Is.False,
                "a changed legacy day count must fail the invariant instead of reading the same graph value back");
            Assert.That(JapanTravelGraph.LegacyRouteIsIntact(changed, 2), Is.True,
                "the helper must accept only the explicitly expected day count");
        }

        [Test]
        public void Graph_ValidateReportsNoIssues()
        {
            IReadOnlyList<string> issues = JapanTravelGraph.Validate();
            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [Test]
        public void RemoteRegions_UsePortHubsAndExplicitSeaAccess()
        {
            IReadOnlyList<JapanMapRoute> ezoRoutes =
                JapanTravelGraph.GetRoutesFrom("ezo.matsumae");
            Assert.That(ezoRoutes.Count, Is.EqualTo(1));
            Assert.That(ezoRoutes[0].Kind, Is.EqualTo(JapanRouteKind.Sea));
            Assert.That(ezoRoutes[0].OtherHubId("ezo.matsumae"), Is.EqualTo("mutsu.tosaminato"));

            IReadOnlyList<JapanMapRoute> shuriRoutes =
                JapanTravelGraph.GetRoutesFrom("ryukyu.shuri");
            Assert.That(shuriRoutes.Count, Is.EqualTo(1));
            Assert.That(shuriRoutes[0].Kind, Is.EqualTo(JapanRouteKind.Land));
            Assert.That(shuriRoutes[0].OtherHubId("ryukyu.shuri"), Is.EqualTo("ryukyu.naha"));

            IReadOnlyList<JapanMapRoute> nahaRoutes =
                JapanTravelGraph.GetRoutesFrom("ryukyu.naha");
            Assert.That(nahaRoutes.Count, Is.EqualTo(2));
            bool sawSeaToKagoshima = false;
            bool sawLandToShuri = false;
            for (int i = 0; i < nahaRoutes.Count; i++)
            {
                string other = nahaRoutes[i].OtherHubId("ryukyu.naha");
                if (other == "satsuma.kagoshima")
                    sawSeaToKagoshima = nahaRoutes[i].Kind == JapanRouteKind.Sea;
                if (other == "ryukyu.shuri")
                    sawLandToShuri = nahaRoutes[i].Kind == JapanRouteKind.Land;
            }
            Assert.That(sawSeaToKagoshima, Is.True);
            Assert.That(sawLandToShuri, Is.True);
        }

        [Test]
        public void Explorer_ExpandsAndCollapsesTheOwariDomain()
        {
            var explorer = new JapanMapExplorer(LocationId.Kiyosu);
            Assert.That(explorer.SelectedHubId, Is.EqualTo(LocationId.Kiyosu.Value));
            Assert.That(explorer.IsDomainExpanded("tokaido"), Is.True);

            IReadOnlyList<JapanMapCatalogRow> expanded = explorer.BuildCatalogRows();
            int domainRows = 0;
            int provinceRows = 0;
            bool sawOwari = false;
            for (int i = 0; i < expanded.Count; i++)
            {
                if (expanded[i].Kind == JapanMapRowKind.Domain)
                    domainRows++;
                else
                {
                    provinceRows++;
                    if (expanded[i].Id == "province.owari")
                    {
                        sawOwari = true;
                        Assert.That(expanded[i].HubCount, Is.EqualTo(3));
                        Assert.That(expanded[i].HubNames.Count, Is.EqualTo(3));
                    }
                }
            }

            Assert.That(domainRows, Is.EqualTo(8));
            Assert.That(provinceRows, Is.EqualTo(15), "tokaido must expose its 15 provinces");
            Assert.That(sawOwari, Is.True);

            Assert.That(explorer.ToggleDomain("tokaido"), Is.True);
            IReadOnlyList<JapanMapCatalogRow> collapsed = explorer.BuildCatalogRows();
            Assert.That(collapsed.Count, Is.EqualTo(8));
            for (int i = 0; i < collapsed.Count; i++)
                Assert.That(collapsed[i].Kind, Is.EqualTo(JapanMapRowKind.Domain));
        }

        [Test]
        public void Explorer_ProvinceWithoutHubStaysCatalogOnly()
        {
            JapanProvince emptyProvince = default;
            bool found = false;
            for (int i = 0; i < JapanMapCatalog.Provinces.Count; i++)
            {
                JapanProvince province = JapanMapCatalog.Provinces[i];
                if (JapanTravelGraph.GetHubsForProvince(province.Id).Count != 0)
                    continue;
                emptyProvince = province;
                found = true;
                break;
            }

            Assert.That(found, Is.True, "the catalog must contain provinces without hubs");
            var explorer = new JapanMapExplorer(LocationId.Kiyosu);
            Assert.That(explorer.SelectProvince(emptyProvince.Id), Is.True);
            Assert.That(explorer.SelectedProvinceId, Is.EqualTo(emptyProvince.Id));
            Assert.That(explorer.SelectedHubId, Is.Empty);
            Assert.That(explorer.BuildSelectedHubDetail(), Is.Null);
            string detail = JapanMapText.BuildDetail(explorer);
            Assert.That(detail, Does.Contain("没有旅行枢纽"));
            Assert.That(detail, Does.Contain("旧国本身不是可访问地点"));
        }

        [Test]
        public void Explorer_FiltersSeaRoutesWithoutChangingTheGraph()
        {
            var explorer = new JapanMapExplorer(LocationId.Kiyosu);
            Assert.That(explorer.SetRouteFilter(JapanRouteKind.Sea), Is.True);
            Assert.That(explorer.BuildRouteCards(), Is.Empty,
                "清洲 has no direct sea edge in the sparse graph");
            Assert.That(JapanMapText.FilterLabel(explorer.RouteFilter), Is.EqualTo("仅海路"));

            Assert.That(explorer.SelectHub("satsuma.kagoshima"), Is.True);
            IReadOnlyList<JapanMapRouteCard> cards = explorer.BuildRouteCards();
            Assert.That(cards.Count, Is.EqualTo(1));
            Assert.That(cards[0].OtherHubId, Is.EqualTo("ryukyu.naha"));
            Assert.That(cards[0].Route.Kind, Is.EqualTo(JapanRouteKind.Sea));

            Assert.That(explorer.SetRouteFilter(null), Is.True);
            Assert.That(explorer.BuildRouteCards().Count, Is.EqualTo(2),
                "鹿儿岛 keeps one sea and one land edge");
        }

        [Test]
        public void Text_LabelsLegacyRoutesAndDesignInference()
        {
            var explorer = new JapanMapExplorer(LocationId.Kiyosu);
            string routes = JapanMapText.BuildRoutes(explorer);

            Assert.That(routes, Does.Contain("既有 M2 路线"));
            Assert.That(routes, Does.Contain("游戏设计推演"));
            Assert.That(routes, Does.Contain("来源："));
            Assert.That(JapanMapText.EvidenceLabel(JapanRouteEvidence.DocumentedCorridor),
                Is.EqualTo("史料走廊支持"));
            Assert.That(JapanMapText.KindLabel(JapanRouteKind.Sea), Is.EqualTo("海路"));

            Assert.That(explorer.SelectHub("yamashiro.kyoto"), Is.True);
            string detail = JapanMapText.BuildDetail(explorer);
            Assert.That(detail, Does.Contain("旅行枢纽"));
            Assert.That(detail, Does.Contain("没有城镇活动"));
            Assert.That(detail, Does.Contain("旧国条目只作目录"));
        }

        [Test]
        public void Audit_AcceptsCapturedSave_AndRejectsUnknownLocation()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData data = session.CaptureSave();
                Assert.That(JapanMapSaveAudit.Validate(data).Succeeded, Is.True,
                    JapanMapSaveAudit.Validate(data).Message);

                data.locationId = "owari.unknown";
                SaveOperationResult result = JapanMapSaveAudit.Validate(data);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Message, Does.Contain("当前位置"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void Audit_RejectsTravelDayMismatch()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData data = session.CaptureSave();
                SetInFlightTravel(data, LocationId.Kiyosu.Value, LocationId.Atsuta.Value, 2);

                SaveOperationResult result = JapanMapSaveAudit.Validate(data);

                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Message, Does.Contain("天数不一致"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void Migration_V16_PreservesCurrentLocationAndInFlightTravel()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData legacy = session.CaptureSave();
                SetInFlightTravel(legacy, LocationId.Kiyosu.Value, LocationId.Atsuta.Value, 1);
                string expectedLocation = legacy.locationId;
                legacy.saveFormatVersion = 16;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);
                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.locationId, Is.EqualTo(expectedLocation));

                DailyActionPersonSaveEntry entry = current.dailyActions.persons[0];
                Assert.That(entry.hasAction, Is.True);
                Assert.That((DailyActionKind)entry.actionKind, Is.EqualTo(DailyActionKind.Travel));
                Assert.That(entry.travelOriginId, Is.EqualTo(LocationId.Kiyosu.Value));
                Assert.That(entry.travelDestinationId, Is.EqualTo(LocationId.Atsuta.Value));
                Assert.That(entry.actionDurationDays, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        private static void SetInFlightTravel(SaveData save, string origin, string destination,
            int days)
        {
            DailyActionPersonSaveEntry entry = save.dailyActions.persons[0];
            entry.hasLocation = false;
            entry.locationId = string.Empty;
            entry.hasAction = true;
            entry.actionKind = (int)DailyActionKind.Travel;
            entry.actionDurationDays = days;
            entry.actionElapsedDays = 0;
            entry.actionStartYear = save.year;
            entry.actionStartMonth = save.month;
            entry.actionStartDay = save.day;
            entry.travelOriginId = origin;
            entry.travelDestinationId = destination;
            entry.actionReason = "地图兼容测试";
        }
    }
}
