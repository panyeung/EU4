using System.Linq;
using NUnit.Framework;
using UnityEngine;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class TownActivityEntryTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp() => _session = GameSession.NewGame();

        [TearDown]
        public void TearDown()
        {
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Kiyosu_ListsAvailableActivitiesInStableOrder()
        {
            Assert.That(_session.CurrentLocation, Is.EqualTo(LocationId.Kiyosu));

            AssertActivities(
                new[]
                {
                    TownLocalActions.Kind.SupplyShortage,
                    TownLocalActions.Kind.SealedLetter,
                    TownLocalActions.Kind.EscortCommission,
                    TownLocalActions.Kind.TacticalMission
                },
                new[]
                {
                    "补给短缺：领取",
                    "密信：领取",
                    "护送委托：领取",
                    "潜入密令：开始"
                });
        }

        [Test]
        public void Atsuta_ListsSupplyStepsOnlyWhileTheirStageIsAvailableHere()
        {
            Assert.That(_session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            TravelTo(LocationId.Atsuta);
            AssertActivities(new[] { TownLocalActions.Kind.SupplyShortage },
                new[] { "补给短缺：调查" });

            Assert.That(_session.TryInvestigateSupplyShortage().Succeeded, Is.True);
            AssertActivities(new[] { TownLocalActions.Kind.SupplyShortage },
                new[] { "补给短缺：交涉" });

            Assert.That(_session.TryNegotiateSupplyShortage(CharacterId.KurodaIori).Succeeded, Is.True);
            AssertActivities(new TownLocalActions.Kind[0], new string[0]);

            TravelTo(LocationId.Kiyosu);
            AssertActivities(
                new[]
                {
                    TownLocalActions.Kind.SupplyShortage,
                    TownLocalActions.Kind.SealedLetter,
                    TownLocalActions.Kind.EscortCommission,
                    TownLocalActions.Kind.TacticalMission
                },
                new[]
                {
                    "补给短缺：报告",
                    "密信：领取",
                    "护送委托：领取",
                    "潜入密令：开始"
                });
        }

        [Test]
        public void Nagoya_ListsLetterDeliveryAndMarketIntake()
        {
            Assert.That(_session.TryAcceptSealedLetterMission().Succeeded, Is.True);
            TravelTo(LocationId.Nagoya);

            AssertActivities(
                new[] { TownLocalActions.Kind.SealedLetter, TownLocalActions.Kind.MarketTally },
                new[] { "密信：交付", "市集账目：领取" });

            Assert.That(_session.TryDeliverSealedLetter().Succeeded, Is.True);
            AssertActivities(new[] { TownLocalActions.Kind.MarketTally },
                new[] { "市集账目：领取" });
        }

        [Test]
        public void MarketTally_IsListedOnlyAtItsCurrentActionLocation()
        {
            TravelTo(LocationId.Nagoya);
            Assert.That(_session.TryAcceptMarketTallyMission().Succeeded, Is.True);
            AssertActivities(new TownLocalActions.Kind[0], new string[0]);

            TravelTo(LocationId.Kiyosu);
            AssertActivities(
                new[]
                {
                    TownLocalActions.Kind.SupplyShortage,
                    TownLocalActions.Kind.SealedLetter,
                    TownLocalActions.Kind.MarketTally,
                    TownLocalActions.Kind.EscortCommission,
                    TownLocalActions.Kind.TacticalMission
                },
                new[]
                {
                    "补给短缺：领取",
                    "密信：领取",
                    "市集账目：调查",
                    "护送委托：领取",
                    "潜入密令：开始"
                });
            Assert.That(_session.TryInvestigateMarketTally().Succeeded, Is.True);
            AssertActivities(
                new[]
                {
                    TownLocalActions.Kind.SupplyShortage,
                    TownLocalActions.Kind.SealedLetter,
                    TownLocalActions.Kind.EscortCommission,
                    TownLocalActions.Kind.TacticalMission
                },
                new[]
                {
                    "补给短缺：领取",
                    "密信：领取",
                    "护送委托：领取",
                    "潜入密令：开始"
                });

            TravelTo(LocationId.Nagoya);
            AssertActivities(new[] { TownLocalActions.Kind.MarketTally },
                new[] { "市集账目：报告" });
        }

        [Test]
        public void ReadingActivitiesAndDescription_DoesNotChangeSessionState()
        {
            string saveBefore = JsonUtility.ToJson(_session.CaptureSave());
            GameDate date = _session.WorldClock.CurrentDate;
            LocationId location = _session.CurrentLocation;
            IndustryResources resources = _session.Industry.Resources;
            SupplyShortageStage supply = _session.SupplyShortageStage;
            SealedLetterStage letter = _session.SealedLetterStage;
            MarketTallyStage market = _session.MarketTallyStage;
            ShadowEscortStage escort = _session.CommissionStage;

            TownLocalActions.GetAvailable(_session);
            TownLocalActions.GetAvailable(_session);
            TownLocalActions.Describe(_session);

            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date));
            Assert.That(_session.CurrentLocation, Is.EqualTo(location));
            Assert.That(_session.Industry.Resources, Is.EqualTo(resources));
            Assert.That(_session.SupplyShortageStage, Is.EqualTo(supply));
            Assert.That(_session.SealedLetterStage, Is.EqualTo(letter));
            Assert.That(_session.MarketTallyStage, Is.EqualTo(market));
            Assert.That(_session.CommissionStage, Is.EqualTo(escort));
            Assert.That(JsonUtility.ToJson(_session.CaptureSave()), Is.EqualTo(saveBefore));
        }

        [Test]
        public void CompactSummary_ReportsTheCountForCardlessTownPages()
        {
            Assert.That(TownLocalActions.DescribeCompact(null), Is.EqualTo("当地委托\n当前地点不可用"));
            Assert.That(TownLocalActions.DescribeCompact(_session), Is.EqualTo("当地委托\n4 项可办理"));

            TravelTo(LocationId.Atsuta);
            Assert.That(TownLocalActions.DescribeCompact(_session),
                Is.EqualTo("当地委托\n当前暂无可办理的本地委托"));
        }

        [Test]
        public void MissingOrUninitializedSession_HasNoAvailableActivities()
        {
            Assert.That(TownLocalActions.GetAvailable(null), Is.Empty);

            var host = new GameObject("UninitializedTownActivitySession_Test");
            try
            {
                var session = host.AddComponent<GameSession>();
                Assert.That(session.WorldClock, Is.Null);
                Assert.That(TownLocalActions.GetAvailable(session), Is.Empty);
                Assert.That(TownLocalActions.Describe(session), Does.Contain(TownLocalActions.Empty));
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }

        private void AssertActivities(TownLocalActions.Kind[] expectedKinds, string[] expectedLabels)
        {
            TownLocalActions.Entry[] entries = TownLocalActions.GetAvailable(_session).ToArray();
            CollectionAssert.AreEqual(expectedKinds, entries.Select(entry => entry.Kind).ToArray());
            CollectionAssert.AreEqual(expectedLabels, entries.Select(entry => entry.Label).ToArray());
        }

        private void TravelTo(LocationId destination)
        {
            TravelPreview? preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True, $"Expected travel from {_session.CurrentLocation} to {destination}.");
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }
    }
}
