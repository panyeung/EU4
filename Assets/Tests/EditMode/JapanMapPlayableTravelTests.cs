using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// t9: the sparse national travel graph is playable through the existing
    /// GameSession preview/confirm path, while the three M02 locations and the
    /// two M02 routes keep their saved identity.
    /// </summary>
    public class JapanMapPlayableTravelTests
    {
        private const string OkazakiId = "mikawa.okazaki";

        private static GameSession NewSession()
        {
            return GameSession.NewGame();
        }

        [Test]
        public void Graph_IsValidAndKeepsLegacyEdges()
        {
            IReadOnlyList<string> issues = JapanTravelGraph.Validate();
            Assert.That(issues, Is.Empty, string.Join("; ", issues));
            Assert.That(JapanTravelGraph.Hubs.Count, Is.EqualTo(37));
            Assert.That(JapanTravelGraph.Routes.Count, Is.EqualTo(40));

            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (JapanMapRoute route in JapanTravelGraph.Routes)
            {
                Assert.That(string.IsNullOrEmpty(route.RouteId), Is.False, route.FromHubId);
                Assert.That(routeIds.Add(route.RouteId), Is.True, route.RouteId);
                Assert.That(route.Days, Is.GreaterThan(0));
                Assert.That(JapanTravelGraph.TryGetHub(route.FromHubId, out _), Is.True);
                Assert.That(JapanTravelGraph.TryGetHub(route.ToHubId, out _), Is.True);
                Assert.That(JapanTravelGraph.TryGetRouteById(route.RouteId, out JapanMapRoute byId), Is.True);
                Assert.That(byId.RouteId, Is.EqualTo(route.RouteId));
            }

            Assert.That(JapanTravelGraph.TryGetRoute(LocationId.Kiyosu.Value,
                LocationId.Atsuta.Value, out JapanMapRoute kiyosuAtsuta), Is.True);
            Assert.That(kiyosuAtsuta.Days, Is.EqualTo(1));
            Assert.That(kiyosuAtsuta.IsLegacyRoute, Is.True);
            Assert.That(JapanTravelGraph.TryGetRoute(LocationId.Kiyosu.Value,
                LocationId.Nagoya.Value, out JapanMapRoute kiyosuNagoya), Is.True);
            Assert.That(kiyosuNagoya.Days, Is.EqualTo(1));
            Assert.That(kiyosuNagoya.IsLegacyRoute, Is.True);

            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Atsuta, LocationId.Nagoya,
                out _), Is.False, "Atsuta-Nagoya stays the deliberate missing edge");

            // t12/t10: the inland Aki-Koriyama to Iyo-Matsuyama sea hop was removed,
            // and Ezo is now reached through the port hub at Tosaminato instead of Aizu.
            Assert.That(JapanTravelGraph.TryGetRoute("aki.koriyama", "iyo.matsuyama",
                out _), Is.False);
            Assert.That(JapanTravelGraph.TryGetRoute("mutsu.aizu", "ezo.matsumae",
                out _), Is.False);
            Assert.That(JapanTravelGraph.TryGetRoute("mutsu.aizu", "mutsu.tosaminato",
                out JapanMapRoute aizuTosaminato), Is.True);
            Assert.That(aizuTosaminato.Kind, Is.EqualTo(JapanRouteKind.Land));
            Assert.That(JapanTravelGraph.TryGetRoute("mutsu.tosaminato", "ezo.matsumae",
                out JapanMapRoute tosaminatoEzo), Is.True);
            Assert.That(tosaminatoEzo.Kind, Is.EqualTo(JapanRouteKind.Sea));
            Assert.That(tosaminatoEzo.Days, Is.EqualTo(6));
            Assert.That(tosaminatoEzo.Evidence, Is.EqualTo(JapanRouteEvidence.DesignInference));
            Assert.That(tosaminatoEzo.SourceNote, Does.Contain("不证明"),
                "Tosaminato-Matsumae must stay regional background, not a documented 1560 route");

            // t16: inland cities are no longer sea endpoints. Each former inland
            // sea hop now uses an explicit port hub plus a short land connector.
            Assert.That(JapanTravelGraph.TryGetRoute("sanuki.takamatsu", "iyo.matsuyama",
                out _), Is.False);
            Assert.That(JapanTravelGraph.TryGetRoute("sanuki.takamatsu", "iyo.masaki",
                out JapanMapRoute takamatsuMasaki), Is.True);
            Assert.That(takamatsuMasaki.Kind, Is.EqualTo(JapanRouteKind.Sea));
            Assert.That(takamatsuMasaki.Days, Is.EqualTo(2));
            Assert.That(takamatsuMasaki.Evidence, Is.EqualTo(JapanRouteEvidence.DesignInference));
            Assert.That(JapanTravelGraph.TryGetRoute("iyo.masaki", "iyo.matsuyama",
                out JapanMapRoute masakiMatsuyama), Is.True);
            Assert.That(masakiMatsuyama.Kind, Is.EqualTo(JapanRouteKind.Land));
            Assert.That(masakiMatsuyama.Days, Is.EqualTo(1));

            Assert.That(JapanTravelGraph.TryGetRoute("nagato.yamaguchi", "chikuzen.hakata",
                out _), Is.False);
            Assert.That(JapanTravelGraph.TryGetRoute("nagato.yamaguchi", "nagato.akama",
                out JapanMapRoute yamaguchiAkama), Is.True);
            Assert.That(yamaguchiAkama.Kind, Is.EqualTo(JapanRouteKind.Land));
            Assert.That(yamaguchiAkama.Days, Is.EqualTo(1));
            Assert.That(JapanTravelGraph.TryGetRoute("nagato.akama", "chikuzen.hakata",
                out JapanMapRoute akamaHakata), Is.True);
            Assert.That(akamaHakata.Kind, Is.EqualTo(JapanRouteKind.Sea));
            Assert.That(akamaHakata.Days, Is.EqualTo(2));
            Assert.That(akamaHakata.Evidence, Is.EqualTo(JapanRouteEvidence.DesignInference));

            Assert.That(JapanTravelGraph.TryGetRoute("satsuma.kagoshima", "ryukyu.shuri",
                out _), Is.False);
            Assert.That(JapanTravelGraph.TryGetRoute("satsuma.kagoshima", "ryukyu.naha",
                out JapanMapRoute kagoshimaNaha), Is.True);
            Assert.That(kagoshimaNaha.Kind, Is.EqualTo(JapanRouteKind.Sea));
            Assert.That(kagoshimaNaha.Days, Is.EqualTo(8));
            Assert.That(kagoshimaNaha.Evidence, Is.EqualTo(JapanRouteEvidence.DesignInference));
            Assert.That(JapanTravelGraph.TryGetRoute("ryukyu.naha", "ryukyu.shuri",
                out JapanMapRoute nahaShuri), Is.True);
            Assert.That(nahaShuri.Kind, Is.EqualTo(JapanRouteKind.Land));
            Assert.That(nahaShuri.Days, Is.EqualTo(1));
        }

        [TestCase(864f, 324f)]
        [TestCase(648f, 432f)]
        [TestCase(1280f, 320f)]
        public void Layout_AtRepresentativeSizes_KeepsFullClickTargetWithoutOverlap(
            float width, float height)
        {
            JapanMapLayoutResult layout = JapanMapLayout.ComputeLayout(JapanTravelGraph.Hubs,
                new Vector2(width, height));

            Assert.That(layout.Positions.Length,
                Is.EqualTo(JapanTravelGraph.Hubs.Count));
            Assert.That(layout.NodeSize.x, Is.EqualTo(JapanMapLayout.NodeWidth).Within(0.01f));
            Assert.That(layout.NodeSize.y, Is.EqualTo(JapanMapLayout.NodeHeight).Within(0.01f));
            AssertLayoutInBounds(layout, width, height);

            AssertNoOverlap(layout.Positions, LocationId.Kiyosu.Value,
                LocationId.Atsuta.Value, layout.NodeSize);
            AssertNoOverlap(layout.Positions, LocationId.Kiyosu.Value,
                LocationId.Nagoya.Value, layout.NodeSize);
            AssertNoOverlap(layout.Positions, LocationId.Atsuta.Value,
                LocationId.Nagoya.Value, layout.NodeSize);
        }

        [Test]
        public void Layout_NarrowPortrait_StaysInBoundsWithoutOverlap()
        {
            const float width = 253f;
            const float height = 300f;
            JapanMapLayoutResult layout = JapanMapLayout.ComputeLayout(JapanTravelGraph.Hubs,
                new Vector2(width, height));

            Assert.That(layout.Positions.Length,
                Is.EqualTo(JapanTravelGraph.Hubs.Count));
            Assert.That(layout.NodeSize.x, Is.GreaterThan(0f));
            Assert.That(layout.NodeSize.y, Is.GreaterThan(0f));
            Assert.That(layout.NodeSize.x, Is.LessThanOrEqualTo(JapanMapLayout.NodeWidth));
            Assert.That(layout.NodeSize.y, Is.LessThanOrEqualTo(JapanMapLayout.NodeHeight));
            AssertLayoutInBounds(layout, width, height);
        }

        [TestCase(100f, 100f)]
        [TestCase(100f, 1000f)]
        [TestCase(1000f, 100f)]
        [TestCase(1600f, 40f)]
        public void Layout_TinyOrExtremeViewport_KeepsDistinctPositions(
            float width, float height)
        {
            JapanMapLayoutResult layout = JapanMapLayout.ComputeLayout(JapanTravelGraph.Hubs,
                new Vector2(width, height));

            Assert.That(layout.Positions.Length,
                Is.EqualTo(JapanTravelGraph.Hubs.Count));
            Assert.That(layout.NodeSize.x, Is.GreaterThan(0f));
            Assert.That(layout.NodeSize.y, Is.GreaterThan(0f));
            Assert.That(layout.NodeSize.x, Is.LessThanOrEqualTo(JapanMapLayout.NodeWidth));
            Assert.That(layout.NodeSize.y, Is.LessThanOrEqualTo(JapanMapLayout.NodeHeight));
            AssertLayoutInBounds(layout, width, height);

            var distinct = new HashSet<Vector2>();
            foreach (Vector2 position in layout.Positions)
                distinct.Add(position);
            Assert.That(distinct.Count, Is.EqualTo(layout.Positions.Length),
                "small/extreme viewport reused a node position");
        }

        [TestCase(1f, 1f)]
        [TestCase(1f, 100f)]
        [TestCase(100f, 1f)]
        public void Layout_AtDeclaredMinimumSize_IsExplicitlyDegenerate(
            float width, float height)
        {
            // ComputeLayout documents <= MinSupportedSize on either axis as the only
            // zero-position case. This is a contract test, not a renderable layout.
            JapanMapLayoutResult layout = JapanMapLayout.ComputeLayout(
                JapanTravelGraph.Hubs, new Vector2(width, height));

            Assert.That(layout.Positions.Length,
                Is.EqualTo(JapanTravelGraph.Hubs.Count));
            foreach (Vector2 position in layout.Positions)
                Assert.That(position, Is.EqualTo(Vector2.zero));
        }

        [TestCase(1.01f, 1.01f)]
        [TestCase(1.01f, 100f)]
        [TestCase(100f, 1.01f)]
        public void Layout_JustAboveDeclaredMinimumSize_KeepsDistinctNodes(
            float width, float height)
        {
            // One hundredth of a pixel above the documented minimum must already
            // enter the normal capacity path and give every hub its own position.
            JapanMapLayoutResult layout = JapanMapLayout.ComputeLayout(
                JapanTravelGraph.Hubs, new Vector2(width, height));

            Assert.That(layout.Positions.Length,
                Is.EqualTo(JapanTravelGraph.Hubs.Count));
            Assert.That(layout.NodeSize.x, Is.GreaterThan(0f));
            Assert.That(layout.NodeSize.y, Is.GreaterThan(0f));
            Assert.That(layout.NodeSize.x, Is.LessThanOrEqualTo(JapanMapLayout.NodeWidth));
            Assert.That(layout.NodeSize.y, Is.LessThanOrEqualTo(JapanMapLayout.NodeHeight));
            AssertLayoutInBounds(layout, width, height);

            var distinct = new HashSet<Vector2>();
            foreach (Vector2 position in layout.Positions)
                distinct.Add(position);
            Assert.That(distinct.Count, Is.EqualTo(layout.Positions.Length),
                "just above MinSupportedSize reused a node position");
        }

        private static void AssertLayoutInBounds(JapanMapLayoutResult layout,
            float width, float height)
        {
            Vector2 nodeSize = layout.NodeSize;
            for (int i = 0; i < layout.Positions.Length; i++)
            {
                Assert.That(layout.Positions[i].x - nodeSize.x * 0.5f,
                    Is.GreaterThanOrEqualTo(-width * 0.5f - 0.01f));
                Assert.That(layout.Positions[i].x + nodeSize.x * 0.5f,
                    Is.LessThanOrEqualTo(width * 0.5f + 0.01f));
                Assert.That(layout.Positions[i].y - nodeSize.y * 0.5f,
                    Is.GreaterThanOrEqualTo(-height * 0.5f - 0.01f));
                Assert.That(layout.Positions[i].y + nodeSize.y * 0.5f,
                    Is.LessThanOrEqualTo(height * 0.5f + 0.01f));
            }

            for (int i = 0; i < layout.Positions.Length; i++)
            {
                for (int j = i + 1; j < layout.Positions.Length; j++)
                {
                    float dx = Math.Abs(layout.Positions[j].x - layout.Positions[i].x);
                    float dy = Math.Abs(layout.Positions[j].y - layout.Positions[i].y);
                    // Cell centres are exact multiples of the cell size, but the
                    // float round-trip can differ by a few 1e-5; allow 0.01 px so
                    // touching edges are not reported as an overlap.
                    const float overlapTolerance = 0.01f;
                    Assert.That(dx + overlapTolerance >= nodeSize.x ||
                                dy + overlapTolerance >= nodeSize.y, Is.True,
                        "overlap between " + JapanTravelGraph.Hubs[i].Id + " and " +
                        JapanTravelGraph.Hubs[j].Id);
                }
            }
        }

        private static void AssertNoOverlap(Vector2[] positions, string firstId,
            string secondId, Vector2 nodeSize)
        {
            int first = -1;
            int second = -1;
            for (int i = 0; i < JapanTravelGraph.Hubs.Count; i++)
            {
                if (JapanTravelGraph.Hubs[i].Id == firstId)
                    first = i;
                if (JapanTravelGraph.Hubs[i].Id == secondId)
                    second = i;
            }

            Assert.That(first, Is.GreaterThanOrEqualTo(0), firstId);
            Assert.That(second, Is.GreaterThanOrEqualTo(0), secondId);
            float dx = Math.Abs(positions[second].x - positions[first].x);
            float dy = Math.Abs(positions[second].y - positions[first].y);
            Assert.That(dx >= nodeSize.x || dy >= nodeSize.y,
                Is.True, firstId + " and " + secondId + " are too close");
        }

        [Test]
        public void PreviewNewHub_DoesNotMovePlayer()
        {
            GameSession session = NewSession();
            try
            {
                TravelPreview? preview = session.PreviewTravel(new LocationId(OkazakiId));
                Assert.That(preview.HasValue, Is.True);
                Assert.That(preview.Value.Destination.Value, Is.EqualTo(OkazakiId));
                Assert.That(preview.Value.DestinationName, Is.EqualTo("冈崎"));
                Assert.That(preview.Value.TravelDays, Is.EqualTo(2));
                Assert.That(preview.Value.EstimatedArrivalDate, Is.EqualTo(new GameDate(1560, 4, 3)));
                Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 1)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void ConfirmNewHub_MovesPlayerAndAdvancesDate()
        {
            GameSession session = NewSession();
            try
            {
                TravelPreview preview = session.PreviewTravel(new LocationId(OkazakiId)).Value;
                Assert.That(session.ConfirmTravel(preview), Is.True,
                    session.LastTravelBlockReason);
                Assert.That(session.CurrentLocation.Value, Is.EqualTo(OkazakiId));
                Assert.That(session.WorldClock.CurrentDate,
                    Is.EqualTo(preview.EstimatedArrivalDate));
                Assert.That(WorldMapRules.IsSameLocation(session.CurrentLocation,
                    new LocationId(OkazakiId)), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void DiscardedPreview_LeavesTravelStateUnchanged()
        {
            GameSession session = NewSession();
            try
            {
                TravelPreview? discarded = session.PreviewTravel(new LocationId(OkazakiId));
                Assert.That(discarded.HasValue, Is.True);
                Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 1)));

                TravelPreview? again = session.PreviewTravel(new LocationId(OkazakiId));
                Assert.That(again.HasValue, Is.True);
                Assert.That(again.Value, Is.EqualTo(discarded.Value));
                Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void PreviewNewHub_AcrossMonthEnd_IsRejectedWithoutMoving()
        {
            GameSession session = NewSession();
            try
            {
                Assert.That(session.AdvanceDaysForLegacyFixtureSetup(29), Is.True);
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 30)));

                TravelPreview? preview = session.PreviewTravel(new LocationId(OkazakiId));

                Assert.That(preview.HasValue, Is.False);
                Assert.That(session.LastTravelBlockReason, Does.Contain("跨越月末"));
                Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
                Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 30)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void NonTownHub_RejectsBackendMissions()
        {
            GameSession session = NewSession();
            try
            {
                Assert.That(session.ConfirmTravel(
                    session.PreviewTravel(new LocationId(OkazakiId)).Value), Is.True);

                SealedLetterActionResult letter = session.TryAcceptSealedLetterMission();
                Assert.That(letter.Succeeded, Is.False);
                Assert.That(letter.Stage, Is.EqualTo(SealedLetterStage.NotAccepted));
                Assert.That(letter.Feedback, Is.EqualTo(LocationCapabilities.NonTownHubMessage));

                MarketTallyActionResult market = session.TryAcceptMarketTallyMission();
                Assert.That(market.Succeeded, Is.False);
                Assert.That(market.Stage, Is.EqualTo(MarketTallyStage.NotAccepted));
                Assert.That(market.Feedback, Is.EqualTo(LocationCapabilities.NonTownHubMessage));

                SupplyShortageActionResult supply = session.TryAcceptSupplyShortageMission();
                Assert.That(supply.Succeeded, Is.False);
                Assert.That(supply.Stage, Is.EqualTo(SupplyShortageStage.NotAccepted));
                Assert.That(supply.Feedback, Is.EqualTo(LocationCapabilities.NonTownHubMessage));

                ShadowEscortActionResult commission = session.TryAcceptCommission();
                Assert.That(commission.Succeeded, Is.False);
                Assert.That(commission.Feedback, Is.EqualTo(LocationCapabilities.NonTownHubMessage));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void SaveAudit_AcceptsNewHubLocationAndRoute()
        {
            GameSession session = NewSession();
            try
            {
                SaveData data = session.CaptureSave();
                data.locationId = OkazakiId;
                SetInFlightTravel(data, OkazakiId, "suruga.sunpu", 3);

                SaveOperationResult result = JapanMapSaveAudit.Validate(data);

                Assert.That(result.Succeeded, Is.True, result.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void Migration_V16_PreservesNewHubAndInFlightTravel()
        {
            GameSession session = NewSession();
            try
            {
                SaveData legacy = session.CaptureSave();
                legacy.locationId = OkazakiId;
                SetInFlightTravel(legacy, OkazakiId, "suruga.sunpu", 3);
                legacy.saveFormatVersion = 16;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);
                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.locationId, Is.EqualTo(OkazakiId));

                DailyActionPersonSaveEntry entry = HeadDailyAction(current);
                Assert.That(entry.hasAction, Is.True);
                Assert.That((DailyActionKind)entry.actionKind, Is.EqualTo(DailyActionKind.Travel));
                Assert.That(entry.travelOriginId, Is.EqualTo(OkazakiId));
                Assert.That(entry.travelDestinationId, Is.EqualTo("suruga.sunpu"));
                Assert.That(entry.actionDurationDays, Is.EqualTo(3));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void CurrentV17Save_KeepsLegacyLocationAndRouteAuditable()
        {
            GameSession session = NewSession();
            try
            {
                SaveData current = session.CaptureSave();
                Assert.That(current.saveFormatVersion, Is.EqualTo(17));
                Assert.That(current.rulesVersion, Is.EqualTo(8));

                current.locationId = LocationId.Atsuta.Value;
                SetInFlightTravel(current, LocationId.Kiyosu.Value, LocationId.Atsuta.Value, 1);
                SaveOperationResult audit = JapanMapSaveAudit.Validate(current);
                Assert.That(audit.Succeeded, Is.True, audit.Message);

                Assert.That(SaveDataMigrator.TryMigrate(current, null, out SaveData migrated,
                    out SaveOperationResult result), Is.True, result.Message);
                Assert.That(migrated.locationId, Is.EqualTo(LocationId.Atsuta.Value));
                DailyActionPersonSaveEntry migratedEntry = HeadDailyAction(migrated);
                Assert.That(migratedEntry.travelOriginId,
                    Is.EqualTo(LocationId.Kiyosu.Value));
                Assert.That(migratedEntry.travelDestinationId,
                    Is.EqualTo(LocationId.Atsuta.Value));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }


        [Test]
        public void NonTownHub_RejectsNpcDialogueBeforeAndAfterTravel()
        {
            GameSession session = NewSession();
            try
            {
                DialogueStartResult started = session.TryStartDialogue(CharacterId.KurodaIori);
                Assert.That(started.Succeeded, Is.True, started.Error);

                TravelPreview preview = session.PreviewTravel(new LocationId(OkazakiId)).Value;
                Assert.That(session.ConfirmTravel(preview), Is.True, session.LastTravelBlockReason);
                int relationshipBeforeSelection =
                    session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue;

                DialogueStartResult blockedStart = session.TryStartDialogue(CharacterId.KurodaIori);
                Assert.That(blockedStart.Succeeded, Is.False);
                Assert.That(blockedStart.DialogueInstanceId, Is.EqualTo(Guid.Empty));
                Assert.That(blockedStart.Error, Is.EqualTo(LocationCapabilities.NonTownHubMessage));

                // The dialogue was opened at Kiyosu, then the player travelled to a
                // non-town hub. Selecting an old option must not change relationships.
                DialogueSelectionResult blockedSelect = session.TrySelectDialogueOption(
                    started.DialogueInstanceId, started.CurrentNodeId,
                    BranchingDialogueCatalog.KurodaOfferOption);
                Assert.That(blockedSelect.Succeeded, Is.False);
                Assert.That(blockedSelect.Error, Is.EqualTo(LocationCapabilities.NonTownHubMessage));
                Assert.That(blockedSelect.RelationshipEffectApplied, Is.False);
                Assert.That(blockedSelect.RelationshipResult, Is.EqualTo(default(RelationshipResult)));
                Assert.That(session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                    Is.EqualTo(relationshipBeforeSelection));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void LegacyOneDayRoutes_RoundTripThroughSyntheticV17Json()
        {
            // There is no real pre-t2 save in the repository. This fixture is a
            // synthetic v17 payload produced by the current serializer, used only to
            // prove that the legacy one-day edges survive JSON and a fresh load.
            foreach (string destinationId in new[]
                     {
                         LocationId.Atsuta.Value,
                         LocationId.Nagoya.Value,
                     })
            {
                GameSession session = NewSession();
                try
                {
                    TravelPreview? preview = session.PreviewTravel(new LocationId(destinationId));
                    Assert.That(preview.HasValue, Is.True, session.LastTravelBlockReason);
                    Assert.That(preview.Value.TravelDays, Is.EqualTo(1));
                    Assert.That(session.ConfirmTravel(preview.Value), Is.True,
                        session.LastTravelBlockReason);

                    SaveData loaded = RoundTrip(session.CaptureSave());
                    Assert.That(loaded.saveFormatVersion, Is.EqualTo(17));
                    Assert.That(loaded.locationId, Is.EqualTo(destinationId));

                    GameSession restored = NewSession();
                    try
                    {
                        SaveOperationResult applied = restored.TryApplySave(loaded);
                        Assert.That(applied.Succeeded, Is.True, applied.Message);
                        Assert.That(restored.CurrentLocation.Value, Is.EqualTo(destinationId));
                        Assert.That(restored.WorldClock.CurrentDate,
                            Is.EqualTo(new GameDate(1560, 4, 2)));
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(restored.gameObject);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(session.gameObject);
                }
            }
        }

        [Test]
        public void SyntheticV17Json_LoadsAndContinuesLegacyInFlightTravel()
        {
            GameSession session = NewSession();
            try
            {
                SaveData source = session.CaptureSave();
                Assert.That(source.saveFormatVersion, Is.EqualTo(17));
                source.locationId = LocationId.Kiyosu.Value;
                SetInFlightTravel(source, LocationId.Kiyosu.Value, LocationId.Atsuta.Value, 1);

                SaveData loaded = RoundTrip(source);
                GameSession restored = NewSession();
                try
                {
                    SaveOperationResult applied = restored.TryApplySave(loaded);
                    Assert.That(applied.Succeeded, Is.True, applied.Message);
                    Assert.That(restored.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));

                    DailyActionPersonSaveEntry entry = HeadDailyAction(restored.CaptureSave());
                    Assert.That(entry.hasAction, Is.True);
                    Assert.That((DailyActionKind)entry.actionKind, Is.EqualTo(DailyActionKind.Travel));
                    Assert.That(entry.travelOriginId, Is.EqualTo(LocationId.Kiyosu.Value));
                    Assert.That(entry.travelDestinationId, Is.EqualTo(LocationId.Atsuta.Value));
                    Assert.That(entry.actionDurationDays, Is.EqualTo(1));
                    Assert.That(entry.actionElapsedDays, Is.Zero);

                    Assert.That(restored.AdvanceDays(1), Is.True, restored.LastDailyActionMessage);
                    Assert.That(restored.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
                    Assert.That(restored.WorldClock.CurrentDate,
                        Is.EqualTo(new GameDate(1560, 4, 2)));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(restored.gameObject);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void MultiDayInFlightTravel_RoundTripsElapsedDaysAndContinues()
        {
            GameSession session = NewSession();
            GameSession restored = null;
            try
            {
                PersonId head = session.Succession.HeadPersonId;
                Assert.That(session.TryPlanTravelAction(head, new LocationId(OkazakiId),
                    out string reason), Is.True, reason);
                Assert.That(session.AdvanceDays(1), Is.True, session.LastDailyActionMessage);

                SaveData source = session.CaptureSave();
                DailyActionPersonSaveEntry sourceEntry = HeadDailyAction(source);
                Assert.That(sourceEntry.hasAction, Is.True);
                Assert.That((DailyActionKind)sourceEntry.actionKind,
                    Is.EqualTo(DailyActionKind.Travel));
                Assert.That(sourceEntry.hasLocation, Is.False);
                Assert.That(sourceEntry.travelOriginId, Is.EqualTo(LocationId.Kiyosu.Value));
                Assert.That(sourceEntry.travelDestinationId, Is.EqualTo(OkazakiId));
                Assert.That(sourceEntry.actionDurationDays, Is.EqualTo(2));
                Assert.That(sourceEntry.actionElapsedDays, Is.EqualTo(1));

                SaveData loaded = RoundTrip(source);
                restored = NewSession();
                SaveOperationResult applied = restored.TryApplySave(loaded);
                Assert.That(applied.Succeeded, Is.True, applied.Message);
                Assert.That(restored.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));

                DailyActionPersonSaveEntry restoredEntry = HeadDailyAction(restored.CaptureSave());
                Assert.That(restoredEntry.actionElapsedDays, Is.EqualTo(1));
                Assert.That(restoredEntry.actionDurationDays, Is.EqualTo(2));

                Assert.That(restored.AdvanceDays(1), Is.True, restored.LastDailyActionMessage);
                Assert.That(restored.CurrentLocation.Value, Is.EqualTo(OkazakiId));
                Assert.That(restored.WorldClock.CurrentDate,
                    Is.EqualTo(new GameDate(1560, 4, 3)));
            }
            finally
            {
                if (restored != null)
                    UnityEngine.Object.DestroyImmediate(restored.gameObject);
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        // Reverse cases pin direction-independent compatibility: an in-flight save
        // may have either endpoint as its current location.
        [TestCase("sanuki.takamatsu", "iyo.matsuyama", 2)]
        [TestCase("iyo.matsuyama", "sanuki.takamatsu", 2)]
        [TestCase("nagato.yamaguchi", "chikuzen.hakata", 2)]
        [TestCase("chikuzen.hakata", "nagato.yamaguchi", 2)]
        [TestCase("satsuma.kagoshima", "ryukyu.shuri", 8)]
        [TestCase("ryukyu.shuri", "satsuma.kagoshima", 8)]
        public void SyntheticV17_RetiredRouteInFlight_LoadsCompletesAndStaysUnselectable(
            string originId, string destinationId, int durationDays)
        {
            // The three direct edges below were replaced by port/land segments in the
            // unreleased t16 working tree. A development v17/rules-8 save can still
            // contain one as an in-flight action; restore must accept that exact
            // duration without adding the edge back to the playable graph.
            var origin = new LocationId(originId);
            var destination = new LocationId(destinationId);
            Assert.That(JapanTravelGraph.TryGetRoute(originId, destinationId, out _), Is.False);
            Assert.That(WorldMapRules.TryGetTravelDays(origin, destination, out _), Is.False);
            Assert.That(WorldMapRules.GetTravelDays(origin, destination), Is.EqualTo(-1));
            Assert.That(WorldMapRules.TryGetInFlightTravelDays(origin, destination,
                out int compatDays), Is.True);
            Assert.That(compatDays, Is.EqualTo(durationDays));
            Assert.That(WorldMapRules.TryGetInFlightTravelDays(destination, origin,
                out int reverseDays), Is.True);
            Assert.That(reverseDays, Is.EqualTo(durationDays));

            GameSession session = NewSession();
            GameSession restored = null;
            try
            {
                SaveData source = session.CaptureSave();
                Assert.That(source.saveFormatVersion, Is.EqualTo(17));
                Assert.That(source.rulesVersion, Is.EqualTo(8));
                source.locationId = originId;
                SetInFlightTravel(source, originId, destinationId, durationDays);
                DailyActionPersonSaveEntry sourceEntry = HeadDailyAction(source);
                sourceEntry.actionElapsedDays = durationDays - 1;

                SaveData loaded = RoundTrip(source);
                restored = NewSession();
                SaveOperationResult applied = restored.TryApplySave(loaded);
                Assert.That(applied.Succeeded, Is.True, applied.Message);

                DailyActionPersonSaveEntry restoredEntry = HeadDailyAction(restored.CaptureSave());
                Assert.That(restoredEntry.hasAction, Is.True);
                Assert.That((DailyActionKind)restoredEntry.actionKind,
                    Is.EqualTo(DailyActionKind.Travel));
                Assert.That(restoredEntry.travelOriginId, Is.EqualTo(originId));
                Assert.That(restoredEntry.travelDestinationId, Is.EqualTo(destinationId));
                Assert.That(restoredEntry.actionDurationDays, Is.EqualTo(durationDays));
                Assert.That(restoredEntry.actionElapsedDays, Is.EqualTo(durationDays - 1));

                Assert.That(restored.AdvanceDays(1), Is.True,
                    restored.LastDailyActionMessage);
                Assert.That(restored.CurrentLocation.Value, Is.EqualTo(destinationId));
                Assert.That(restored.WorldClock.CurrentDate,
                    Is.EqualTo(new GameDate(1560, 4, 2)));

                // Compatibility is read-only: the old edge never becomes a new route.
                Assert.That(JapanTravelGraph.TryGetRoute(originId, destinationId, out _),
                    Is.False);
                Assert.That(WorldMapRules.TryGetTravelDays(origin, destination, out _),
                    Is.False);
            }
            finally
            {
                if (restored != null)
                    UnityEngine.Object.DestroyImmediate(restored.gameObject);
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [TestCase("sanuki.takamatsu", "iyo.matsuyama", 2)]
        [TestCase("iyo.matsuyama", "sanuki.takamatsu", 2)]
        [TestCase("nagato.yamaguchi", "chikuzen.hakata", 2)]
        [TestCase("chikuzen.hakata", "nagato.yamaguchi", 2)]
        [TestCase("satsuma.kagoshima", "ryukyu.shuri", 8)]
        [TestCase("ryukyu.shuri", "satsuma.kagoshima", 8)]
        public void Migration_V17_RetiredRouteInFlight_PreservesDurationAndElapsed(
            string originId, string destinationId, int durationDays)
        {
            GameSession session = NewSession();
            try
            {
                SaveData source = session.CaptureSave();
                source.locationId = originId;
                SetInFlightTravel(source, originId, destinationId, durationDays);
                HeadDailyAction(source).actionElapsedDays = durationDays - 1;

                Assert.That(SaveDataMigrator.TryMigrate(source, null, out SaveData migrated,
                    out SaveOperationResult result), Is.True, result.Message);
                Assert.That(migrated.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(migrated.rulesVersion, Is.EqualTo(8));

                DailyActionPersonSaveEntry entry = HeadDailyAction(migrated);
                Assert.That(entry.hasAction, Is.True);
                Assert.That(entry.travelOriginId, Is.EqualTo(originId));
                Assert.That(entry.travelDestinationId, Is.EqualTo(destinationId));
                Assert.That(entry.actionDurationDays, Is.EqualTo(durationDays));
                Assert.That(entry.actionElapsedDays, Is.EqualTo(durationDays - 1));

                SaveOperationResult audit = JapanMapSaveAudit.Validate(migrated, 17, 8);
                Assert.That(audit.Succeeded, Is.True, audit.Message);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        // Coverage boundary: the fixtures in this file are synthetic SaveData
        // objects created from a fresh v17 game and then shaped like a historical
        // in-flight action. They pin the migration/restore contract, not a
        // byte-for-byte JSON file emitted by a released build. RoundTrip() below
        // covers the JsonUtility serialization shape; no released historical save
        // fixture exists in this repository.
        [TestCase(16, "sanuki.takamatsu", "iyo.matsuyama", 2)]
        [TestCase(16, "iyo.matsuyama", "sanuki.takamatsu", 2)]
        [TestCase(16, "nagato.yamaguchi", "chikuzen.hakata", 2)]
        [TestCase(16, "chikuzen.hakata", "nagato.yamaguchi", 2)]
        [TestCase(16, "satsuma.kagoshima", "ryukyu.shuri", 8)]
        [TestCase(16, "ryukyu.shuri", "satsuma.kagoshima", 8)]
        [TestCase(15, "sanuki.takamatsu", "iyo.matsuyama", 2)]
        [TestCase(15, "iyo.matsuyama", "sanuki.takamatsu", 2)]
        [TestCase(15, "nagato.yamaguchi", "chikuzen.hakata", 2)]
        [TestCase(15, "chikuzen.hakata", "nagato.yamaguchi", 2)]
        [TestCase(15, "satsuma.kagoshima", "ryukyu.shuri", 8)]
        [TestCase(15, "ryukyu.shuri", "satsuma.kagoshima", 8)]
        public void Migration_PreV17_RetiredRouteInFlight_IsRejected(int sourceVersion,
            string originId, string destinationId, int durationDays)
        {
            // The migrator rewrites the payload to v17/rules 8 before the map audit.
            // Passing the pre-migration version is what keeps a v16/v15 save from
            // inheriting the v17-only retired-route compatibility window. Every
            // retired pair is exercised in both directions.
            GameSession session = NewSession();
            try
            {
                SaveData source = session.CaptureSave();
                source.saveFormatVersion = sourceVersion;
                source.locationId = originId;
                SetInFlightTravel(source, originId, destinationId, durationDays);

                bool migrated = SaveDataMigrator.TryMigrate(source, null, out SaveData current,
                    out SaveOperationResult result);

                Assert.That(migrated, Is.False);
                Assert.That(current, Is.Null);
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.Message, Does.Contain("地图兼容审计"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [TestCase("sanuki.takamatsu", "iyo.matsuyama", 3, 0,
            "存档旅行行动不符合世界地图直达路线规则")]
        [TestCase("nagato.yamaguchi", "chikuzen.hakata", 3, 0,
            "存档旅行行动不符合世界地图直达路线规则")]
        [TestCase("satsuma.kagoshima", "ryukyu.shuri", 9, 0,
            "存档旅行行动不符合世界地图直达路线规则")]
        [TestCase("sanuki.takamatsu", "iyo.matsuyama", 2, 2,
            "存档每日行动进度必须在 0..持续时间-1 之间")]
        [TestCase("sanuki.takamatsu", "iyo.matsuyama", 2, -1,
            "存档每日行动进度必须在 0..持续时间-1 之间")]
        public void SyntheticV17_RetiredRouteWithInvalidDurationOrElapsed_IsRejected(
            string originId, string destinationId, int durationDays, int elapsedDays,
            string expectedMessage)
        {
            GameSession session = NewSession();
            try
            {
                SaveData source = session.CaptureSave();
                source.locationId = originId;
                SetInFlightTravel(source, originId, destinationId, durationDays);
                HeadDailyAction(source).actionElapsedDays = elapsedDays;

                SaveOperationResult validation = SaveDataValidator.Validate(source);

                Assert.That(validation.Succeeded, Is.False,
                    "invalid duration/elapsed must not pass the load validator");
                Assert.That(validation.Message, Does.Contain(expectedMessage));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void Audit_V17RetiredRouteWithWrongRulesVersion_IsRejected()
        {
            // Only a 17/rules-8 source may unlock the retired routes. A v17 payload
            // carrying rules 7 must not inherit that window even if its map fields
            // otherwise look like the accepted synthetic fixture.
            GameSession session = NewSession();
            try
            {
                SaveData source = session.CaptureSave();
                source.rulesVersion = 7;
                source.locationId = "sanuki.takamatsu";
                SetInFlightTravel(source, "sanuki.takamatsu", "iyo.matsuyama", 2);

                SaveOperationResult audit =
                    JapanMapSaveAudit.Validate(source, 17, source.rulesVersion);

                Assert.That(audit.Succeeded, Is.False);
                Assert.That(audit.Message, Does.Contain("地图兼容审计"));
                Assert.That(audit.Message, Does.Contain("天数不一致"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void Bootstrap_CreatesMapRootAndLauncherBelowProtectedOverlays()
        {
            GameSession session = NewSession();
            GameObject canvasObject = null;
            try
            {
                Canvas canvas = CreateCanvas(out canvasObject);
                Transform ordinary = CreateCanvasChild(canvas.transform, "OrdinaryPanel");
                Transform title = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.TitleOverlayName);
                Transform eventChoice = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.EventChoiceOverlayName);
                Transform tactical = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.TacticalOverlayName);

                JapanMapScreen screen = JapanMapSceneBootstrap.EnsureFor(session, canvas);

                Assert.That(screen, Is.Not.Null);
                Transform root = canvas.transform.Find(JapanMapSceneBootstrap.RootName);
                Transform launcher = canvas.transform.Find(JapanMapSceneBootstrap.LauncherName);
                Assert.That(root, Is.Not.Null, "the bootstrap must create the map root");
                Assert.That(launcher, Is.Not.Null, "the bootstrap must create the entry button");
                Assert.That(root.gameObject.activeSelf, Is.False,
                    "the map root starts closed");
                Assert.That(launcher.gameObject.activeInHierarchy, Is.True,
                    "the entry button must be visible through the canvas");
                Assert.That(ordinary.GetSiblingIndex(), Is.LessThan(root.GetSiblingIndex()));
                Assert.That(root.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()));
                Assert.That(launcher.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()));
                Assert.That(title.GetSiblingIndex(), Is.LessThan(eventChoice.GetSiblingIndex()));
                Assert.That(eventChoice.GetSiblingIndex(), Is.LessThan(tactical.GetSiblingIndex()));
            }
            finally
            {
                DestroyCanvas(canvasObject, session);
            }
        }

        [Test]
        public void BootstrapTargetCanvas_PrefersGameSessionCanvasWhenItIsNotTheSceneFirstCanvas()
        {
            GameSession session = NewSession();
            GameObject firstCanvasObject = null;
            GameObject sessionCanvasObject = null;
            try
            {
                // 先建的 Canvas 代表场景首个 Canvas，GameSession 放在后建的 Canvas 下。
                Canvas firstCanvas = CreateCanvas(out firstCanvasObject);
                Canvas sessionCanvas = CreateCanvas(out sessionCanvasObject);
                Assert.That(firstCanvas, Is.Not.SameAs(sessionCanvas));

                session.gameObject.transform.SetParent(sessionCanvas.transform, false);

                Canvas resolved = JapanMapSceneBootstrap.ResolveTargetCanvas(session);

                Assert.That(resolved, Is.SameAs(sessionCanvas),
                    "入口必须挂到 GameSession 所在的 Canvas");
                Assert.That(resolved, Is.Not.SameAs(firstCanvas),
                    "场景首个 Canvas 不是 GameSession 所在 Canvas 时不得被选中");
            }
            finally
            {
                DestroyCanvas(sessionCanvasObject, session);
                if (firstCanvasObject != null)
                    UnityEngine.Object.DestroyImmediate(firstCanvasObject);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StartupEntries_LeaveOneMapEntryOnTheGameSessionCanvasWhateverTheOrder(
            bool controllerStartFirst)
        {
            GameSession session = NewSession();
            GameObject firstCanvasObject = null;
            GameObject sessionCanvasObject = null;
            GameObject controllerObject = null;
            try
            {
                // 两个 Canvas：GameSession 挂在后建的 Canvas 下，WorldMapController 挂在
                // 先建的 Canvas 下，两个启动入口的 Canvas 不同。
                Canvas controllerCanvas = CreateCanvas(out firstCanvasObject);
                Canvas sessionCanvas = CreateCanvas(out sessionCanvasObject);
                Assert.That(controllerCanvas, Is.Not.SameAs(sessionCanvas));

                session.gameObject.transform.SetParent(sessionCanvas.transform, false);
                var bootstrap = session.gameObject.AddComponent<JapanMapSceneBootstrap>();

                controllerObject = new GameObject("M2WorldMapRoot", typeof(RectTransform));
                controllerObject.transform.SetParent(controllerCanvas.transform, false);
                controllerObject.SetActive(false);
                var controller = controllerObject.AddComponent<WorldMapController>();
                SetPrivateField(controller, "gameSession", session);
                controllerObject.SetActive(true);

                MethodInfo bootstrapStart = typeof(JapanMapSceneBootstrap).GetMethod(
                    "Start", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo controllerStart = typeof(WorldMapController).GetMethod(
                    "Start", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(bootstrapStart, Is.Not.Null, "JapanMapSceneBootstrap.Start 必须存在");
                Assert.That(controllerStart, Is.Not.Null, "WorldMapController.Start 必须存在");

                if (controllerStartFirst)
                {
                    controllerStart.Invoke(controller, null);
                    bootstrapStart.Invoke(bootstrap, null);
                }
                else
                {
                    bootstrapStart.Invoke(bootstrap, null);
                    controllerStart.Invoke(controller, null);
                }

                AssertSingleMapEntry(sessionCanvas);
                Assert.That(controllerCanvas.transform.Find(JapanMapSceneBootstrap.RootName), Is.Null,
                    "WorldMapController 所在 Canvas 不得出现第二套地图根");
                Assert.That(controllerCanvas.transform.Find(JapanMapSceneBootstrap.LauncherName),
                    Is.Null, "WorldMapController 所在 Canvas 不得出现第二个入口按钮");
            }
            finally
            {
                if (controllerObject != null)
                    UnityEngine.Object.DestroyImmediate(controllerObject);
                DestroyCanvas(sessionCanvasObject, session);
                if (firstCanvasObject != null)
                    UnityEngine.Object.DestroyImmediate(firstCanvasObject);
            }
        }

        [Test]
        public void Bootstrap_LauncherKeepsFixedTopRightGeometryAndClearsTheHeaderRow()
        {
            GameSession session = NewSession();
            GameObject canvasObject = null;
            try
            {
                Canvas canvas = CreateCanvas(out canvasObject);
                Assert.That(JapanMapSceneBootstrap.EnsureFor(session, canvas), Is.Not.Null);

                var launcher = (RectTransform)canvas.transform.Find(
                    JapanMapSceneBootstrap.LauncherName);
                Assert.That(launcher.anchorMin, Is.EqualTo(Vector2.one));
                Assert.That(launcher.anchorMax, Is.EqualTo(Vector2.one));
                Assert.That(launcher.pivot, Is.EqualTo(Vector2.one));
                Assert.That(launcher.sizeDelta, Is.EqualTo(new Vector2(
                    JapanMapSceneBootstrap.LauncherWidth, JapanMapSceneBootstrap.LauncherHeight)));
                Assert.That(launcher.anchoredPosition, Is.EqualTo(new Vector2(
                    -JapanMapSceneBootstrap.LauncherRightMargin,
                    -JapanMapSceneBootstrap.LauncherTopMargin)));

                foreach (float width in new[] { 1108f, 1249f, 1280f, 1920f })
                {
                    bool compact = width < ReferenceGalleryNavigator.HeaderCompactWidth;
                    float origin = ReferenceGalleryNavigator.ResolveHeaderChipOriginX(width, compact
                        ? ReferenceGalleryNavigator.HeaderChipCompactOriginX
                        : ReferenceGalleryNavigator.HeaderChipWideOriginX);
                    float rowRight = origin + ReferenceGalleryNavigator.HeaderChipRowWidth;
                    float launcherLeft = width - JapanMapSceneBootstrap.LauncherReservedWidth;
                    Assert.That(rowRight, Is.LessThanOrEqualTo(launcherLeft + 0.01f),
                        "the entry button must not cover the resource row at width " + width);
                }
            }
            finally
            {
                DestroyCanvas(canvasObject, session);
            }
        }

        [Test]
        public void MapOpenAndClose_KeepProtectedOverlaysAboveAndStopBlockingInput()
        {
            GameSession session = NewSession();
            GameObject canvasObject = null;
            try
            {
                Canvas canvas = CreateCanvas(out canvasObject);
                Transform title = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.TitleOverlayName);
                Transform eventChoice = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.EventChoiceOverlayName);
                Transform tactical = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.TacticalOverlayName);

                JapanMapScreen screen = JapanMapSceneBootstrap.EnsureFor(session, canvas);
                Assert.That(screen, Is.Not.Null);
                Transform root = canvas.transform.Find(JapanMapSceneBootstrap.RootName);
                Assert.That(root, Is.Not.Null);

                screen.Open();

                Assert.That(screen.IsOpen, Is.True);
                Assert.That(root.gameObject.activeSelf, Is.True);
                Assert.That(root.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()),
                    "opening the map must not raise it above the title overlay");
                Assert.That(root.GetSiblingIndex(), Is.LessThan(eventChoice.GetSiblingIndex()),
                    "opening the map must not raise it above the event overlay");
                Assert.That(root.GetSiblingIndex(), Is.LessThan(tactical.GetSiblingIndex()),
                    "opening the map must not raise it above the tactical overlay");

                screen.Close();

                Assert.That(screen.IsOpen, Is.False);
                Assert.That(root.gameObject.activeSelf, Is.False,
                    "a closed map must not intercept input");
                Assert.That(root.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()));
                Assert.That(root.GetSiblingIndex(), Is.LessThan(eventChoice.GetSiblingIndex()));
                Assert.That(root.GetSiblingIndex(), Is.LessThan(tactical.GetSiblingIndex()));
            }
            finally
            {
                DestroyCanvas(canvasObject, session);
            }
        }

        [Test]
        public void PlaceBelowProtectedOverlays_MovesTailTargetAndIsIdempotent()
        {
            GameObject canvasObject = null;
            try
            {
                Canvas canvas = CreateCanvas(out canvasObject);
                CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.TitleOverlayName);
                Transform eventChoice = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.EventChoiceOverlayName);
                Transform tactical = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.TacticalOverlayName);
                Transform root = CreateCanvasChild(canvas.transform,
                    JapanMapSceneBootstrap.RootName);
                var rootRect = (RectTransform)root.transform;

                Assert.That(root.GetSiblingIndex(), Is.GreaterThan(eventChoice.GetSiblingIndex()));

                int moved = JapanMapSceneBootstrap.PlaceBelowProtectedOverlays(
                    rootRect, canvas.transform);
                int again = JapanMapSceneBootstrap.PlaceBelowProtectedOverlays(
                    rootRect, canvas.transform);

                Assert.That(moved, Is.GreaterThanOrEqualTo(0));
                Assert.That(again, Is.EqualTo(moved));
                Assert.That(root.GetSiblingIndex(), Is.LessThan(eventChoice.GetSiblingIndex()));
                Assert.That(root.GetSiblingIndex(), Is.LessThan(tactical.GetSiblingIndex()));
            }
            finally
            {
                if (canvasObject != null)
                    UnityEngine.Object.DestroyImmediate(canvasObject);
            }
        }

        // NUnit 3.5 (com.unity.ext.nunit 2.1.0) has no dedicated non-parallel test
        // attribute; ParallelScope.None is the supported opt-out and keeps these
        // static-cache tests out of parallel execution.
        [Test]
        [Parallelizable(ParallelScope.None)]
        public void FontCache_PlayModeLifecycleReset_ClearsCachedResolution()
        {
            try
            {
                SetFontCacheField("_resolved", true);
                SetFontCacheField("_fontMissing", true);
                SetFontCacheField("_status", "stale-status");
                SetFontCacheField("_warning", "stale-warning");

                ResetFontCacheForPlayMode();

                Assert.That(GetFontCacheField<bool>("_resolved"), Is.False,
                    "a new Play Mode must not inherit the previous resolution flag");
                Assert.That(GetFontCacheField<bool>("_fontMissing"), Is.False,
                    "a new Play Mode must not inherit the previous font state");
                Assert.That(GetFontCacheField<string>("_status"), Is.Empty);
                Assert.That(GetFontCacheField<string>("_warning"), Is.Empty);
            }
            finally
            {
                ResetFontCacheForPlayMode();
            }
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        [TestCase(false)]
        [TestCase(true)]
        public void FontCache_ResolveRebuildsWhenCachedUnityFontIsFakeNull(bool useTmp)
        {
            try
            {
                ResetFontCacheForPlayMode();
                SetFontCacheField("_resolved", true);
                SetFontCacheField("_fontMissing", false);
                SetFontCacheField("_useTmp", useTmp);
                SetFontCacheField("_font", null);
                SetFontCacheField("_legacyFont", null);
                SetFontCacheField("_status", "stale-status");
                SetFontCacheField("_warning", "stale-warning");

                // A destroyed UnityEngine.Object compares equal to null. Setting
                // the cached reference to null models that fake-null state
                // deterministically without requiring a particular OS font.
                string status = JapanMapFonts.Status;

                Assert.That(status, Is.Not.EqualTo("stale-status"),
                    "Resolve must rebuild instead of returning the stale cache");
                Assert.That(status, Does.StartWith("字体"),
                    "the rebuild must end in a font status or the explicit missing-font status");
            }
            finally
            {
                ResetFontCacheForPlayMode();
            }
        }

        [Test]
        [Parallelizable(ParallelScope.None)]
        public void FontCache_MissingFontStatus_IsPreservedWithoutRetry()
        {
            const string missingStatus =
                "字体缺口：未找到批准列表中的 CJK 系统字体；文本未渲染";
            const string missingWarning = "font-gap-warning";
            try
            {
                ResetFontCacheForPlayMode();
                SetFontCacheField("_resolved", true);
                SetFontCacheField("_fontMissing", true);
                SetFontCacheField("_status", missingStatus);
                SetFontCacheField("_warning", missingWarning);

                Assert.That(JapanMapFonts.Status, Is.EqualTo(missingStatus),
                    "an explicit missing-font result must stay explicit");
                Assert.That(JapanMapFonts.Warning, Is.EqualTo(missingWarning));
            }
            finally
            {
                ResetFontCacheForPlayMode();
            }
        }

        private static void ResetFontCacheForPlayMode()
        {
            MethodInfo reset = typeof(JapanMapFonts).GetMethod(
                "ResetCacheOnPlayModeEnter", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(reset, Is.Not.Null,
                "JapanMapFonts must keep the Play Mode cache-reset hook.");
            Assert.That(reset.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>(),
                Is.Not.Null,
                "the cache reset must be registered for every Play Mode start.");
            reset.Invoke(null, null);
        }

        private static void SetFontCacheField(string name, object value)
        {
            FieldInfo field = typeof(JapanMapFonts).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, "Missing JapanMapFonts field: " + name);
            field.SetValue(null, value);
        }

        private static T GetFontCacheField<T>(string name)
        {
            FieldInfo field = typeof(JapanMapFonts).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, "Missing JapanMapFonts field: " + name);
            return (T)field.GetValue(null);
        }

        private static void AssertSingleMapEntry(Canvas canvas)
        {
            Assert.That(canvas, Is.Not.Null, "目标 Canvas 不能为空");
            Assert.That(ChildCount(canvas.transform, JapanMapSceneBootstrap.RootName), Is.EqualTo(1),
                "目标 Canvas 必须且只能有一套地图根");
            Assert.That(ChildCount(canvas.transform, JapanMapSceneBootstrap.LauncherName),
                Is.EqualTo(1), "目标 Canvas 必须且只能有一个入口按钮");
            Transform root = canvas.transform.Find(JapanMapSceneBootstrap.RootName);
            Assert.That(root.GetComponent<JapanMapScreen>(), Is.Not.Null);
            Assert.That(canvas.GetComponentsInChildren<JapanMapScreen>(true).Length,
                Is.EqualTo(1), "目标 Canvas 只能有一套地图界面");
        }

        private static int ChildCount(Transform parent, string childName)
        {
            int count = 0;
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    count++;
            }

            return count;
        }

        private static void SetPrivateField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Missing field: " + name);
            field.SetValue(target, value);
        }

        private static Canvas CreateCanvas(out GameObject canvasObject)
        {
            canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        private static Transform CreateCanvasChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void DestroyCanvas(GameObject canvasObject, GameSession session)
        {
            if (canvasObject != null)
                UnityEngine.Object.DestroyImmediate(canvasObject);
            if (session != null)
                UnityEngine.Object.DestroyImmediate(session.gameObject);
        }

        private static SaveData RoundTrip(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            SaveDataPresenceProbe probe =
                JsonUtility.FromJson<SaveDataPresenceProbe>(json);
            SaveOperationResult presence = SaveDataValidator.ValidatePresence(probe, json);
            Assert.That(presence.Succeeded, Is.True, presence.Message);

            SaveData parsed = JsonUtility.FromJson<SaveData>(json);
            SaveOperationResult validation = SaveDataValidator.Validate(parsed);
            Assert.That(validation.Succeeded, Is.True, validation.Message);
            return parsed;
        }

        private static DailyActionPersonSaveEntry HeadDailyAction(SaveData save)
        {
            string headId = save.succession != null ? save.succession.headPersonId : null;
            Assert.That(string.IsNullOrEmpty(headId), Is.False, "存档缺少家主 ID。");
            foreach (DailyActionPersonSaveEntry entry in save.dailyActions.persons)
            {
                if (entry != null && entry.personId == headId)
                    return entry;
            }

            Assert.Fail("存档每日行动缺少家主记录。");
            return null;
        }

        private static void SetInFlightTravel(SaveData save, string origin, string destination,
            int days)
        {
            DailyActionPersonSaveEntry entry = HeadDailyAction(save);
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
            entry.actionReason = "地图可玩性测试";
        }
    }
}
