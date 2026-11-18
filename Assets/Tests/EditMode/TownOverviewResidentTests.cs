using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class TownOverviewResidentTests
    {
        private GameSession _session;
        private WorldMapController _controller;
        private TownOverviewPanel _panel;
        private GameObject _townRoot;
        private Text _residents;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _controller = new GameObject("WorldMapController_Test").AddComponent<WorldMapController>();
            _townRoot = new GameObject("TownMenuRoot", typeof(RectTransform));
            _residents = new GameObject("Residents", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text))
                .GetComponent<Text>();
            _residents.transform.SetParent(_townRoot.transform, false);
            _panel = _townRoot.AddComponent<TownOverviewPanel>();
            _panel.BindReferences(null, null, null, _residents, null, null, null, null);
            _controller.BindReferences(_session, null, null, null, null, null, null, null,
                null, null, null, _townRoot, null, null);
            _controller.BindTownOverviewPanel(_panel);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controller != null) Object.DestroyImmediate(_controller.gameObject);
            if (_townRoot != null) Object.DestroyImmediate(_townRoot);
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void ResidentSummary_TracksTheCurrentTownWithoutMixingAssignments()
        {
            Assert.That(_residents.text, Does.Contain("橘 すず"));
            Assert.That(_residents.text, Does.Contain("森 源左衛門"));
            Assert.That(_residents.text, Does.Not.Contain("黒田 伊織"));

            TravelTo(LocationId.Atsuta);
            _panel.Refresh();
            Assert.That(_residents.text, Is.EqualTo("当地人物\n黒田 伊織"));

            TravelTo(LocationId.Nagoya);
            _panel.Refresh();
            Assert.That(_residents.text, Does.Contain("星野 彩"));
            Assert.That(_residents.text, Does.Contain("酒井 次郎兵衛"));
            Assert.That(_residents.text, Does.Not.Contain("黒田 伊織"));
            Assert.That(_residents.text, Does.Not.Contain("橘 すず"));
        }

        private void TravelTo(LocationId destination)
        {
            if (_session.CurrentLocation == destination) return;
            if (_session.CurrentLocation != LocationId.Kiyosu && destination != LocationId.Kiyosu)
                TravelTo(LocationId.Kiyosu);
            TravelPreview? preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }
    }
}
