using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public class DemoSummaryPanelTests
    {
        private GameSession _session;
        private GameObject _panelObject;
        private DemoSummaryPanel _panel;
        private GameObject _textObject;
        private Text _summaryText;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();

            _panelObject = new GameObject("DemoSummaryPanel");
            _panel = _panelObject.AddComponent<DemoSummaryPanel>();

            _textObject = new GameObject("M6DemoSummaryText", typeof(RectTransform));
            _summaryText = _textObject.AddComponent<Text>();

            _panel.Bind(_session, _summaryText);
        }

        [TearDown]
        public void TearDown()
        {
            if (_panelObject != null)
                Object.DestroyImmediate(_panelObject);
            if (_textObject != null)
                Object.DestroyImmediate(_textObject);
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
        }

        private void ApplyStages(int supply, int letter, int tally)
        {
            var data = new SaveData
            {
                saveFormatVersion = SaveDataValidator.CurrentVersion,
                year = 1560,
                month = 4,
                day = 3,
                locationId = LocationId.Kiyosu.Value,
                supplyShortageStage = supply,
                sealedLetterStage = letter,
                marketTallyStage = tally,
                relationships = null
            };
            Assert.That(_session.TryApplySave(data).Succeeded, Is.True);
        }

        [Test]
        public void NewGame_DoesNotShowSummary()
        {
            Assert.That(_session.IsDemoComplete, Is.False);
            _panel.Refresh();
            Assert.That(_panel.IsShowing, Is.False);
        }

        [Test]
        public void PartiallyCompleted_DoesNotShowSummary()
        {
            ApplyStages((int)SupplyShortageStage.RewardClaimed, 0, 0);

            Assert.That(_session.IsDemoComplete, Is.False);
            _panel.Refresh();
            Assert.That(_panel.IsShowing, Is.False);
        }

        [Test]
        public void AllMissionsClaimed_ShowsSummaryWithTotalMerit()
        {
            ApplyStages(
                (int)SupplyShortageStage.RewardClaimed,
                (int)SealedLetterStage.RewardClaimed,
                (int)MarketTallyStage.RewardClaimed);

            Assert.That(_session.IsDemoComplete, Is.True);
            Assert.That(_session.Merit, Is.EqualTo(20));

            _panel.Refresh();
            Assert.That(_panel.IsShowing, Is.True);
            Assert.That(_summaryText.text, Does.Contain("Phase 1 演示完成"));
            Assert.That(_summaryText.text, Does.Contain("总功绩：20"));
        }

        [Test]
        public void Refresh_IsIdempotent()
        {
            ApplyStages(
                (int)SupplyShortageStage.RewardClaimed,
                (int)SealedLetterStage.RewardClaimed,
                (int)MarketTallyStage.RewardClaimed);

            _panel.Refresh();
            _panel.Refresh();

            Assert.That(_panel.IsShowing, Is.True);
        }

        [Test]
        public void MissingSession_HidesSummary()
        {
            var orphan = new GameObject("EmptyPanel");
            DemoSummaryPanel panel = orphan.AddComponent<DemoSummaryPanel>();
            panel.Bind(null, _summaryText);
            panel.Refresh();

            Assert.That(panel.IsShowing, Is.False);
            Object.DestroyImmediate(orphan);
        }
    }
}
