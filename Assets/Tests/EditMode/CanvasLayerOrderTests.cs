using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// 行为测试：给定一组 Canvas 直接子级名字与目标层，共享层序表算出的插入索引必须让
    /// SuccessionRoot 高于 FamilyRosterRoot、FamilyRosterRoot 高于地图/城镇 HUD，
    /// 且两者都低于标题与事件选择覆盖层。断言只看公开函数与真实 Transform 座次，不碰私有字段。
    /// </summary>
    public sealed class CanvasLayerOrderTests
    {
        // FoundationScene 的 Canvas 直接子级代表性座次：HUD、若干页面、继承弹窗、覆盖层。
        private static readonly string[] CanvasChildren =
        {
            "WorldClockPanel",
            "M2WorldMapRoot",
            "TownNpcRosterRoot",
            "FamilyChronicleRoot",
            "FamilyRosterRoot",
            "IndustryRoot",
            "M02DailyActionRoot",
            "M08CampaignReviewRoot",
            "SuccessionRoot",
            "M6TitleRoot",
            "EventChoiceRoot",
            "M6TacticalMissionOverlay"
        };

        private GameObject _canvas;

        [TearDown]
        public void TearDown()
        {
            if (_canvas != null)
            {
                Object.DestroyImmediate(_canvas);
                _canvas = null;
            }
        }

        [Test]
        public void LayerOf_MapsEveryGeneratedRootToItsSharedLayer()
        {
            Assert.That(CanvasLayerOrder.LayerOf("M2WorldMapRoot"),
                Is.EqualTo(CanvasLayerOrder.CanvasLayer.MapHud));
            Assert.That(CanvasLayerOrder.LayerOf("WorldClockPanel"),
                Is.EqualTo(CanvasLayerOrder.CanvasLayer.MapHud));
            Assert.That(CanvasLayerOrder.LayerOf("FamilyRosterRoot"),
                Is.EqualTo(CanvasLayerOrder.CanvasLayer.Page));
            Assert.That(CanvasLayerOrder.LayerOf("SuccessionRoot"),
                Is.EqualTo(CanvasLayerOrder.CanvasLayer.Modal));
            Assert.That(CanvasLayerOrder.LayerOf("M6TitleRoot"),
                Is.EqualTo(CanvasLayerOrder.CanvasLayer.TitleOverlay));
            Assert.That(CanvasLayerOrder.LayerOf("EventChoiceRoot"),
                Is.EqualTo(CanvasLayerOrder.CanvasLayer.EventChoiceOverlay));

            // 层序定义本身：HUD < 页面 < 弹窗 < 标题 < 事件选择。
            Assert.That(CanvasLayerOrder.CanvasLayer.MapHud,
                Is.LessThan(CanvasLayerOrder.CanvasLayer.Page));
            Assert.That(CanvasLayerOrder.CanvasLayer.Page,
                Is.LessThan(CanvasLayerOrder.CanvasLayer.Modal));
            Assert.That(CanvasLayerOrder.CanvasLayer.Modal,
                Is.LessThan(CanvasLayerOrder.CanvasLayer.TitleOverlay));
            Assert.That(CanvasLayerOrder.CanvasLayer.TitleOverlay,
                Is.LessThan(CanvasLayerOrder.CanvasLayer.EventChoiceOverlay));

            // 表里没有的名字不参与座次约束。
            Assert.That(CanvasLayerOrder.LayerOf("M6TacticalMissionOverlay"), Is.Null);
            Assert.That(CanvasLayerOrder.LayerOf(string.Empty), Is.Null);
            Assert.That(CanvasLayerOrder.LayerOf(null), Is.Null);
        }

        [Test]
        public void InsertIndex_PutsSuccessionAboveTheOpaquePageAndBelowBothOverlays()
        {
            List<string> final = Place("SuccessionRoot", CanvasLayerOrder.CanvasLayer.Modal);

            int succession = final.IndexOf("SuccessionRoot");
            Assert.That(succession, Is.GreaterThan(final.IndexOf("FamilyRosterRoot")),
                "SuccessionRoot must render above the opaque family page so its decision modal is not covered.");
            Assert.That(succession, Is.GreaterThan(final.IndexOf("M2WorldMapRoot")),
                "SuccessionRoot must render above the map/town HUD so its buttons receive raycasts.");
            Assert.That(succession, Is.LessThan(final.IndexOf("M6TitleRoot")),
                "SuccessionRoot must render below the title overlay.");
            Assert.That(succession, Is.LessThan(final.IndexOf("EventChoiceRoot")),
                "SuccessionRoot must render below the event-choice overlay.");
        }

        [Test]
        public void InsertIndex_PutsFamilyPageAboveMapHudAndBelowTheModalAndOverlays()
        {
            List<string> final = Place("FamilyRosterRoot", CanvasLayerOrder.CanvasLayer.Page);

            int roster = final.IndexOf("FamilyRosterRoot");
            Assert.That(roster, Is.GreaterThan(final.IndexOf("M2WorldMapRoot")),
                "FamilyRosterRoot must render above the map/town HUD so its buttons receive raycasts.");
            Assert.That(roster, Is.GreaterThan(final.IndexOf("WorldClockPanel")));
            Assert.That(roster, Is.LessThan(final.IndexOf("SuccessionRoot")),
                "FamilyRosterRoot must stay below the succession modal.");
            Assert.That(roster, Is.LessThan(final.IndexOf("M6TitleRoot")));
            Assert.That(roster, Is.LessThan(final.IndexOf("EventChoiceRoot")));
        }

        [Test]
        public void InsertIndex_AppendsLastWhenNoHigherLayerSiblingExists()
        {
            var pagesOnly = new List<string> { "M2WorldMapRoot", "FamilyChronicleRoot" };
            Assert.That(CanvasLayerOrder.InsertIndex(pagesOnly, CanvasLayerOrder.CanvasLayer.Page),
                Is.EqualTo(pagesOnly.Count));

            // 未分类的兄弟既不构成上界也不构成下界。
            Assert.That(CanvasLayerOrder.InsertIndex(new[] { "M6TacticalMissionOverlay" },
                CanvasLayerOrder.CanvasLayer.Page), Is.EqualTo(1));

            Assert.That(CanvasLayerOrder.InsertIndex(new string[0], CanvasLayerOrder.CanvasLayer.Page),
                Is.EqualTo(0));
        }

        [Test]
        public void InsertIndex_NeverCoversAHigherOverlayInAContradictoryOrder()
        {
            // 场景本身已经矛盾（标题排在 HUD 之前）时，优先保证不盖住更高覆盖层。
            var contradictory = new List<string> { "M6TitleRoot", "M2WorldMapRoot" };
            int index = CanvasLayerOrder.InsertIndex(contradictory, CanvasLayerOrder.CanvasLayer.Modal);

            var final = new List<string>(contradictory);
            final.Insert(index, "SuccessionRoot");
            Assert.That(final.IndexOf("SuccessionRoot"), Is.LessThan(final.IndexOf("M6TitleRoot")));
        }

        [Test]
        public void TryFindViolation_ReportsThePageCoveringTheModalAndTheOverlayCoveringThePage()
        {
            string[] broken =
            {
                "M2WorldMapRoot", "SuccessionRoot", "FamilyRosterRoot", "M6TitleRoot", "EventChoiceRoot"
            };

            Assert.That(CanvasLayerOrder.TryFindViolation(broken, 1, CanvasLayerOrder.CanvasLayer.Modal,
                out string modalOffender, out CanvasLayerOrder.CanvasLayer modalOffenderLayer,
                out bool modalMustRenderAbove), Is.True);
            Assert.That(modalOffender, Is.EqualTo("FamilyRosterRoot"));
            Assert.That(modalOffenderLayer, Is.EqualTo(CanvasLayerOrder.CanvasLayer.Page));
            Assert.That(modalMustRenderAbove, Is.True);

            Assert.That(CanvasLayerOrder.TryFindViolation(broken, 2, CanvasLayerOrder.CanvasLayer.Page,
                out string pageOffender, out CanvasLayerOrder.CanvasLayer pageOffenderLayer,
                out bool pageMustRenderAbove), Is.True);
            Assert.That(pageOffender, Is.EqualTo("SuccessionRoot"));
            Assert.That(pageOffenderLayer, Is.EqualTo(CanvasLayerOrder.CanvasLayer.Modal));
            Assert.That(pageMustRenderAbove, Is.False,
                "The page covers the modal, so the page is the one that has to move down.");
        }

        [Test]
        public void TryFindViolation_RejectsAPageDrawnOverTheTitleOverlay()
        {
            string[] covered = { "M2WorldMapRoot", "M6TitleRoot", "FamilyRosterRoot" };

            Assert.That(CanvasLayerOrder.TryFindViolation(covered, 2, CanvasLayerOrder.CanvasLayer.Page,
                out string offender, out CanvasLayerOrder.CanvasLayer offenderLayer,
                out bool mustRenderAbove), Is.True);
            Assert.That(offender, Is.EqualTo("M6TitleRoot"));
            Assert.That(offenderLayer, Is.EqualTo(CanvasLayerOrder.CanvasLayer.TitleOverlay));
            Assert.That(mustRenderAbove, Is.False);

            string[] repaired = { "M2WorldMapRoot", "FamilyRosterRoot", "M6TitleRoot" };
            Assert.That(CanvasLayerOrder.TryFindViolation(repaired, 1, CanvasLayerOrder.CanvasLayer.Page,
                out _, out _, out _), Is.False);
        }

        [Test]
        public void MovingTheBrokenRootIntoItsLayerSatisfiesEveryInvariantOnRealTransforms()
        {
            _canvas = new GameObject("Canvas", typeof(RectTransform));
            string[] broken =
            {
                "M2WorldMapRoot", "SuccessionRoot", "FamilyRosterRoot", "M6TitleRoot", "EventChoiceRoot"
            };
            foreach (string name in broken)
                CreateChild(_canvas.transform, name);

            Transform succession = _canvas.transform.Find("SuccessionRoot");
            Assert.That(CanvasLayerOrder.TryFindViolation(Names(_canvas.transform), succession.GetSiblingIndex(),
                CanvasLayerOrder.CanvasLayer.Modal, out _, out _, out _), Is.True,
                "The broken order must be reported before it is repaired.");

            string[] others = Names(_canvas.transform).Where(name => name != "SuccessionRoot").ToArray();
            succession.SetSiblingIndex(CanvasLayerOrder.InsertIndex(others, CanvasLayerOrder.CanvasLayer.Modal));

            int roster = _canvas.transform.Find("FamilyRosterRoot").GetSiblingIndex();
            int map = _canvas.transform.Find("M2WorldMapRoot").GetSiblingIndex();
            int title = _canvas.transform.Find("M6TitleRoot").GetSiblingIndex();
            int events = _canvas.transform.Find("EventChoiceRoot").GetSiblingIndex();
            Assert.That(succession.GetSiblingIndex(), Is.GreaterThan(roster));
            Assert.That(succession.GetSiblingIndex(), Is.GreaterThan(map));
            Assert.That(succession.GetSiblingIndex(), Is.LessThan(title));
            Assert.That(succession.GetSiblingIndex(), Is.LessThan(events));
            Assert.That(CanvasLayerOrder.TryFindViolation(Names(_canvas.transform), succession.GetSiblingIndex(),
                CanvasLayerOrder.CanvasLayer.Modal, out _, out _, out _), Is.False);
        }

        /// <summary>把目标节点按层插进代表性座次，返回插入后的完整名单。</summary>
        private static List<string> Place(string name, CanvasLayerOrder.CanvasLayer layer)
        {
            var others = CanvasChildren.Where(child => child != name).ToList();
            int index = CanvasLayerOrder.InsertIndex(others, layer);
            others.Insert(index, name);
            return others;
        }

        private static string[] Names(Transform canvas) =>
            canvas.Cast<Transform>().Select(child => child.name).ToArray();

        private static void CreateChild(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
        }
    }
}
