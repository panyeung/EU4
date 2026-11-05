using System;
using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;
using SengokuChronicle.Application;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M2 WorldMap travel EditMode tests.
    /// </summary>
    public class WorldMapTravelTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);

        private static GameSession NewSession()
        {
            return GameSession.NewGame();
        }
        // 1. Exactly three location IDs with Chinese names; no fourth.
        [Test]
        public void LocationId_HasExactlyThreeLocations()
        {
            var all = LocationId.All;
            Assert.That(all.Count, Is.EqualTo(3), "exactly 3 locations required");
            var values = new List<string>();
            foreach (var loc in all) values.Add(loc.Value);
            Assert.That(values, Is.EquivalentTo(new[] { "owari.kiyosu", "owari.atsuta", "owari.nagoya" }),
                "location set must be exactly these three, no additions or omissions");
            Assert.That(LocationId.Kiyosu, Is.Not.EqualTo(LocationId.Atsuta), "Kiyosu and Atsuta must differ");
            Assert.That(LocationId.Atsuta, Is.Not.EqualTo(LocationId.Nagoya), "Atsuta and Nagoya must differ");
            Assert.That(LocationId.Kiyosu, Is.Not.EqualTo(LocationId.Nagoya), "Kiyosu and Nagoya must differ");
        }

        [Test]
        public void LocationId_ChineseNames()
        {
            Assert.That(LocationId.GetChineseName(LocationId.Kiyosu), Is.EqualTo("清洲"));
            Assert.That(LocationId.GetChineseName(LocationId.Atsuta), Is.EqualTo("热田"));
            Assert.That(LocationId.GetChineseName(LocationId.Nagoya), Is.EqualTo("那古野"));
            Assert.That(LocationId.GetChineseName(new LocationId("nope")), Is.Empty);
            Assert.That(LocationId.GetChineseName(new LocationId("owari.kiyosu")), Is.EqualTo("清洲"));
        }

        [Test]
        public void LocationId_ValueStrings()
        {
            Assert.That(LocationId.Kiyosu.Value, Is.EqualTo("owari.kiyosu"));
            Assert.That(LocationId.Atsuta.Value, Is.EqualTo("owari.atsuta"));
            Assert.That(LocationId.Nagoya.Value, Is.EqualTo("owari.nagoya"));
            Assert.That(LocationId.Kiyosu, Is.Not.EqualTo(LocationId.Atsuta));
            Assert.That(LocationId.TryParse("owari.kiyosu", out var k), Is.True);
            Assert.That(k, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(LocationId.TryParse("bogus", out var b), Is.False);
        }
        // 3. Routes: both directions 1 day; Atsuta<->Nagoya unreachable.
        [Test]
        public void Routes_KiyosuAtsuta_BidirectionalOneDay()
        {
            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Kiyosu, LocationId.Atsuta, out int d1), Is.True);
            Assert.That(d1, Is.EqualTo(1));
            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Atsuta, LocationId.Kiyosu, out int d2), Is.True);
            Assert.That(d2, Is.EqualTo(1));
        }

        [Test]
        public void Routes_KiyosuNagoya_BidirectionalOneDay()
        {
            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Kiyosu, LocationId.Nagoya, out int d1), Is.True);
            Assert.That(d1, Is.EqualTo(1));
            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Nagoya, LocationId.Kiyosu, out int d2), Is.True);
            Assert.That(d2, Is.EqualTo(1));
        }

        [Test]
        public void Routes_AtsutaNagoya_Unreachable()
        {
            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Atsuta, LocationId.Nagoya, out int d1), Is.False);
            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Nagoya, LocationId.Atsuta, out int d2), Is.False);
            Assert.That(d1, Is.EqualTo(0));
            Assert.That(d2, Is.EqualTo(0));
        }

        [Test]
        public void Routes_NoRouteToSelf()
        {
            Assert.That(WorldMapRules.TryGetTravelDays(LocationId.Kiyosu, LocationId.Kiyosu, out int d), Is.False);
            Assert.That(d, Is.EqualTo(0));
        }

        // 2. GameSession starts at Kiyosu with 1560-04-01.
        [Test]
        public void GameSession_NewGame_StartsAtKiyosu_1560April1()
        {
            var session = NewSession();
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(WorldClock.NewGameDate));
        }
        // 4. Preview returns name/cost/arrival without side effects; discardable.
        [Test]
        public void PreviewTravel_ReturnsNameCostArrival_WithoutMutating()
        {
            var session = NewSession();
            TravelPreview? p = session.PreviewTravel(LocationId.Atsuta);
            Assert.That(p.HasValue, Is.True);
            Assert.That(p.Value.Destination, Is.EqualTo(LocationId.Atsuta));
            Assert.That(p.Value.DestinationName, Is.EqualTo("热田"));
            Assert.That(p.Value.TravelDays, Is.EqualTo(1));
            Assert.That(p.Value.EstimatedArrivalDate, Is.EqualTo(new GameDate(1560, 4, 2)));
            // State unchanged after preview:
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }

        [Test]
        public void PreviewTravel_Nagoya_ArrivalDate()
        {
            var session = NewSession();
            TravelPreview? p = session.PreviewTravel(LocationId.Nagoya);
            Assert.That(p.HasValue, Is.True);
            Assert.That(p.Value.DestinationName, Is.EqualTo("那古野"));
            Assert.That(p.Value.TravelDays, Is.EqualTo(1));
            Assert.That(p.Value.EstimatedArrivalDate, Is.EqualTo(new GameDate(1560, 4, 2)));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start), "preview must not mutate clock");
        }

        [Test]
        public void PreviewTravel_PreviewDiscarded_LeavesStateUnchanged()
        {
            var session = NewSession();
            var p = session.PreviewTravel(LocationId.Atsuta);
            Assert.That(p.HasValue, Is.True);
            // Drop the preview, then re-preview: still valid, state unchanged
            var p2 = session.PreviewTravel(LocationId.Atsuta);
            Assert.That(p2.HasValue, Is.True);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }

        [Test]
        public void PreviewTravel_UnknownLocation_ReturnsNull()
        {
            var session = NewSession();
            Assert.That(session.PreviewTravel(new LocationId("owari.unknown")).HasValue, Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }

        [Test]
        public void PreviewTravel_CurrentLocation_ReturnsNull()
        {
            var session = NewSession();
            Assert.That(session.PreviewTravel(LocationId.Kiyosu).HasValue, Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }

        [Test]
        public void PreviewTravel_NoRoute_ReturnsNull()
        {
            var session = NewSession();
            session.ConfirmTravel(session.PreviewTravel(LocationId.Atsuta).Value);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            // Atsuta -> Nagoya has no direct route
            Assert.That(session.PreviewTravel(LocationId.Nagoya).HasValue, Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }
        [Test]
        public void PreviewTravel_DateOverflow_ReturnsNull()
        {
            var session = NewSession();
            // 1560-04-01 + 160981 days == 2000-12-31 (max supported date)
            Assert.That(session.AdvanceDaysForLegacyFixtureSetup(160981), Is.True);
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 31)));
            Assert.That(session.PreviewTravel(LocationId.Atsuta).HasValue, Is.False, "travel beyond max date must fail preview");
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 31)));
        }

        // 5+6. Confirm travel: atomic updates, boundary crossings.
        [Test]
        public void ConfirmTravel_ValidPreview_UpdatesDateAndLocation()
        {
            var session = NewSession();
            var p = session.PreviewTravel(LocationId.Atsuta).Value;
            Assert.That(session.ConfirmTravel(p), Is.True);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }

        [Test]
        public void ConfirmTravel_InvalidDestination_FailsAtomically()
        {
            var session = NewSession();
            var bad = new TravelPreview(new LocationId("bogus"), "X", 1, new GameDate(1560, 4, 2));
            Assert.That(session.ConfirmTravel(bad), Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }

        [Test]
        public void ConfirmTravel_ToCurrentLocation_FailsAtomically()
        {
            var session = NewSession();
            var bad = new TravelPreview(LocationId.Kiyosu, "清洲", 1, new GameDate(1560, 4, 2));
            Assert.That(session.ConfirmTravel(bad), Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }

        [Test]
        public void ConfirmTravel_ForgedDays_FailsAtomically()
        {
            var session = NewSession();
            // Real Kiyosu->Atsuta costs 1 day; forge 3
            var bad = new TravelPreview(LocationId.Atsuta, "热田", 3, new GameDate(1560, 4, 4));
            Assert.That(session.ConfirmTravel(bad), Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(Start));
        }

        [Test]
        public void ConfirmTravel_NoRoute_FailsAtomically()
        {
            var session = NewSession();
            // Forge a reachable-from-Atsuta preview while at Atsuta: Atsuta->Nagoya has no route
            session.ConfirmTravel(session.PreviewTravel(LocationId.Atsuta).Value);
            var bad = new TravelPreview(LocationId.Nagoya, "那古野", 1, new GameDate(1560, 4, 3));
            Assert.That(session.ConfirmTravel(bad), Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Atsuta));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }
        [Test]
        public void ConfirmTravel_ExpiredPreview_FailsAtomically()
        {
            var session = NewSession();
            var p = session.PreviewTravel(LocationId.Atsuta).Value;
            Assert.That(session.AdvanceOneDay(), Is.True); // now 1560-04-02, preview stale
            Assert.That(session.ConfirmTravel(p), Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 2)));
        }

        [Test]
        public void ConfirmTravel_DateOverflow_FailsAtomically()
        {
            var session = NewSession();
            Assert.That(session.AdvanceDaysForLegacyFixtureSetup(160981), Is.True); // -> 2000-12-31 (max date)
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            // Forge a preview claiming arrival at 2000-12-31 (+1 from 2000-12-30).
            // At 2000-12-31, +1 day would overflow past the supported range, so confirm must fail.
            var forged = new TravelPreview(LocationId.Atsuta, "热田", 1, new GameDate(2000, 12, 31));
            Assert.That(session.ConfirmTravel(forged), Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu), "position must not change on overflow");
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(2000, 12, 31)), "date must not change on overflow");
        }

        // 6. Month/year boundaries: SC-019 forbids travel across a month end.
        [Test]
        public void PreviewTravel_MonthBoundary_April30ToMay1_IsRefused()
        {
            var session = NewSession();
            Assert.That(session.AdvanceDays(29), Is.True); // -> 1560-04-30
            GameDate date = session.WorldClock.CurrentDate;
            LocationId location = session.CurrentLocation;

            Assert.That(session.PreviewTravel(LocationId.Nagoya).HasValue, Is.False);
            Assert.That(session.LastTravelBlockReason, Does.Contain("跨越月末"));
            Assert.That(session.CurrentLocation, Is.EqualTo(location));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(date));
        }

        [Test]
        public void ConfirmTravel_YearBoundary_Dec31ToJan1NextYear_IsRefused()
        {
            var session = NewSession();
            Assert.That(session.AdvanceDaysForLegacyFixtureSetup(273), Is.True); // -> 1560-12-30
            Assert.That(session.ConfirmTravel(session.PreviewTravel(LocationId.Nagoya).Value), Is.True); // -> 12-31
            GameDate date = session.WorldClock.CurrentDate;
            LocationId location = session.CurrentLocation;
            var forged = new TravelPreview(LocationId.Kiyosu, "清洲", 1, new GameDate(1561, 1, 1));

            Assert.That(session.PreviewTravel(LocationId.Kiyosu).HasValue, Is.False);
            Assert.That(session.ConfirmTravel(forged), Is.False);
            Assert.That(session.CurrentLocation, Is.EqualTo(location));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(date));
        }

        [Test]
        public void ConfirmTravel_RoundTrip_KiyosuAtsutaKiyosu()
        {
            var session = NewSession();
            session.ConfirmTravel(session.PreviewTravel(LocationId.Atsuta).Value);
            session.ConfirmTravel(session.PreviewTravel(LocationId.Kiyosu).Value);
            Assert.That(session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));
            Assert.That(session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 3)));
        }
    }
}
