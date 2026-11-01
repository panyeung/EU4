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
    public class RankPanelTests
    {
        private GameSession _session;
        private GameObject _panelObject;
        private RankPanel _panel;
        private GameObject _textObject;
        private Text _rankText;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();

            _panelObject = new GameObject("RankPanel");
            _panel = _panelObject.AddComponent<RankPanel>();

            _textObject = new GameObject("M7RankText", typeof(RectTransform));
            _rankText = _textObject.AddComponent<Text>();

            _panel.Bind(_session, _rankText);
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
        public void NewGame_ShowsFirstRankAndNextThreshold()
        {
            Assert.That(_rankText.text, Does.Contain("功名：0"));
            Assert.That(_rankText.text, Does.Contain("足轻"));
            Assert.That(_rankText.text, Does.Contain("下一阶 番头 5"));
        }

        [Test]
        public void MidProgress_ShowsMiddleRank()
        {
            ApplyStages((int)SupplyShortageStage.RewardClaimed, 0, 0);
            _panel.Refresh();

            Assert.That(_session.Rank, Is.EqualTo(PromotionRank.Kumigashira));
            Assert.That(_rankText.text, Does.Contain("功名：10"));
            Assert.That(_rankText.text, Does.Contain("组头"));
            Assert.That(_rankText.text, Does.Contain("下一阶 武士头 20"));
        }

        [Test]
        public void CompletedDemo_ShowsTopRankMarker()
        {
            ApplyStages(
                (int)SupplyShortageStage.RewardClaimed,
                (int)SealedLetterStage.RewardClaimed,
                (int)MarketTallyStage.RewardClaimed);
            _panel.Refresh();

            Assert.That(_rankText.text, Does.Contain("功名：20"));
            Assert.That(_rankText.text, Does.Contain("武士头"));
            Assert.That(_rankText.text, Does.Contain("最高"));
        }

        [Test]
        public void MissingSession_LeavesTextUnchanged()
        {
            var orphan = new GameObject("EmptyRankPanel");
            RankPanel panel = orphan.AddComponent<RankPanel>();
            string before = _rankText.text;
            panel.Bind(null, _rankText);

            Assert.That(_rankText.text, Is.EqualTo(before));
            Assert.That(panel.CurrentText, Is.EqualTo(before));
            Object.DestroyImmediate(orphan);
        }

        [Test]
        public void Refresh_IsIdempotent()
        {
            string first = _rankText.text;
            _panel.Refresh();
            _panel.Refresh();
            Assert.That(_rankText.text, Is.EqualTo(first));
        }
    }
}
