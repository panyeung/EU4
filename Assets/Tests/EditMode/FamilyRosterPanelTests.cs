using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class FamilyRosterPanelTests
    {
        private GameSession _session;
        private GameObject _root;
        private GameObject _panelObject;
        private GameObject _openObject;
        private GameObject _closeObject;
        private GameObject _titleObject;
        private GameObject _legendObject;
        private GameObject _scrollObject;
        private GameObject _contentObject;
        private GameObject _edgeLayerObject;
        private GameObject _nodeLayerObject;
        private GameObject _nodeTemplateObject;
        private GameObject _edgeTemplateObject;
        private GameObject _sectionTemplateObject;
        private GameObject _heirObject;
        private FamilyRosterPanel _panel;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _root = new GameObject("FamilyRosterRoot", typeof(RectTransform));
            _panelObject = Child("FamilyRosterPanel", _root.transform, typeof(Image));
            _openObject = Child("FamilyRosterOpenButton", _root.transform, typeof(Image),
                typeof(Button));
            _closeObject = Child("FamilyRosterCloseButton", _root.transform, typeof(Image),
                typeof(Button));
            _titleObject = Child("FamilyRosterTitleText", _root.transform, typeof(Text));
            _legendObject = Child("FamilyRosterLegendText", _root.transform, typeof(Text));
            _scrollObject = Child("FamilyRosterScroll", _root.transform, typeof(Image),
                typeof(ScrollRect), typeof(RectMask2D));
            _contentObject = Child("FamilyRosterContent", _scrollObject.transform);
            _edgeLayerObject = Child("FamilyRosterEdgeLayer", _contentObject.transform);
            _nodeLayerObject = Child("FamilyRosterNodeLayer", _contentObject.transform);
            _nodeTemplateObject = Child("FamilyRosterNodeTemplate", _nodeLayerObject.transform,
                typeof(Image));
            Child("Label", _nodeTemplateObject.transform, typeof(Text));
            _edgeTemplateObject = Child("FamilyRosterEdgeTemplate", _edgeLayerObject.transform,
                typeof(Image));
            _sectionTemplateObject = Child("FamilyRosterSectionTemplate", _contentObject.transform,
                typeof(Text));
            _heirObject = Child("FamilyRosterHeirText", _root.transform, typeof(Text));

            RectTransform scrollRect = (RectTransform)_scrollObject.transform;
            scrollRect.sizeDelta = new Vector2(700f, 410f);
            RectTransform contentRect = (RectTransform)_contentObject.transform;
            contentRect.anchorMin = contentRect.anchorMax = new Vector2(0f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.sizeDelta = new Vector2(700f, 600f);
            Stretch((RectTransform)_edgeLayerObject.transform);
            Stretch((RectTransform)_nodeLayerObject.transform);
            ScrollRect scroll = _scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = scrollRect;
            scroll.content = contentRect;
            scroll.horizontal = true;
            scroll.vertical = true;

            _nodeTemplateObject.SetActive(false);
            _edgeTemplateObject.SetActive(false);
            _sectionTemplateObject.SetActive(false);
            _panelObject.SetActive(false);

            _panel = _root.AddComponent<FamilyRosterPanel>();
            _panel.BindReferences(_session, _panelObject, _openObject.GetComponent<Button>(),
                _closeObject.GetComponent<Button>(), _titleObject.GetComponent<Text>(),
                _legendObject.GetComponent<Text>(), scroll, contentRect,
                (RectTransform)_edgeLayerObject.transform, (RectTransform)_nodeLayerObject.transform,
                _nodeTemplateObject, _edgeTemplateObject, _sectionTemplateObject.GetComponent<Text>(),
                _heirObject.GetComponent<Text>());
        }

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void Refresh_RendersSc013KinshipEdgesAndSeparateRetainerSection()
        {
            _panel.Open();

            Assert.That(_panel.VisibleNodeCount, Is.EqualTo(6));
            Assert.That(_panel.KinNodeCount, Is.EqualTo(5));
            Assert.That(_panel.HouseholdNodeCount, Is.EqualTo(1));
            Assert.That(_panel.HasHousehold, Is.True);
            Assert.That(_panel.HasSectionContaining("家臣"), Is.True);
            Assert.That(_panel.VisibleRelationCount, Is.EqualTo(5));
            Assert.That(_panel.VisibleEdgeSegmentCount, Is.GreaterThanOrEqualTo(5));

            Assert.That(_panel.NodeTextFor(M2FixedScenario.HarunobuId), Does.Contain("明智 晴信"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.HarunobuId), Does.Contain("家主"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.HarunobuId), Does.Contain("70岁"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.HarunobuId), Does.Contain("第1代"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.MasatsunaId),
                Does.Contain("指定继承人"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.MasatsunaId), Does.Contain("第2代"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.NobumasaId), Does.Contain("第3代"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.SakonId), Does.Contain("佐近"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.SakonId), Does.Contain("家臣"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.SakonId), Does.Not.Contain("第1代"));

            var types = new HashSet<KinshipType>();
            for (int i = 0; i < _panel.VisibleRelationCount; i++)
                types.Add(_panel.RelationType(i));
            Assert.That(types.Contains(KinshipType.ParentChild), Is.True);
            Assert.That(types.Contains(KinshipType.Spouse), Is.True);
            Assert.That(types.Contains(KinshipType.Sibling), Is.True);
            Assert.That(types.Contains(KinshipType.Adoption), Is.False);

            Color parent = FamilyRosterPanel.RelationColor(KinshipType.ParentChild);
            Color adoption = FamilyRosterPanel.RelationColor(KinshipType.Adoption);
            Color sibling = FamilyRosterPanel.RelationColor(KinshipType.Sibling);
            Color spouse = FamilyRosterPanel.RelationColor(KinshipType.Spouse);
            Assert.That(new[] { parent, adoption, sibling, spouse }, Is.Unique);
            Assert.That(_panel.LegendSummary, Does.Contain("亲子"));
            Assert.That(_panel.LegendSummary, Does.Contain("收养"));
            Assert.That(_panel.LegendSummary, Does.Contain("手足"));
            Assert.That(_panel.LegendSummary, Does.Contain("配偶"));
            Assert.That(_panel.HeirSummary, Does.Contain("固定继承人：明智 正綱"));
            Assert.That(_panel.HeirSummary, Does.Contain("46岁"));
        }

        [Test]
        public void Refresh_ShowsUnconnectedKinAndCurrentHeadAndHeir()
        {
            PersonId lonely = AddPerson("person.test.lonely", "无亲缘族亲", 1540,
                FamilyMemberRole.DirectKin);

            _panel.Open();

            Assert.That(_panel.VisibleNodeCount, Is.EqualTo(7));
            Assert.That(_panel.UnconnectedKinNodeCount, Is.EqualTo(1));
            Assert.That(_panel.HasUnconnectedKin, Is.True);
            Assert.That(_panel.HasSectionContaining("未连接"), Is.True);
            Assert.That(_panel.NodeTextFor(lonely), Does.Contain("无亲缘族亲"));
            Assert.That(_panel.NodeTextFor(lonely), Does.Contain("未连接亲族"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.HarunobuId), Does.Contain("家主"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.MasatsunaId),
                Does.Contain("指定继承人"));
        }

        [Test]
        public void Refresh_RendersLargeRosterInsideScrollableContent()
        {
            PersonId greatGrandchild = AddPerson("person.test.g4", "第四代", 1552,
                FamilyMemberRole.DirectKin);
            PersonId fifth = AddPerson("person.test.g5", "第五代", 1556,
                FamilyMemberRole.DirectKin);
            PersonId sixth = AddPerson("person.test.g6", "第六代", 1558,
                FamilyMemberRole.DirectKin);
            PersonId spouse = AddPerson("person.test.spouse", "第四代配偶", 1554,
                FamilyMemberRole.Spouse);
            PersonId sibling = AddPerson("person.test.sibling", "第四代旁系", 1553,
                FamilyMemberRole.CollateralKin);
            PersonId adopted = AddPerson("person.test.adopted", "第四代养子", 1550,
                FamilyMemberRole.AdoptedChild);
            Link(M2FixedScenario.NobumasaId, greatGrandchild, KinshipType.ParentChild);
            Link(greatGrandchild, fifth, KinshipType.ParentChild);
            Link(fifth, sixth, KinshipType.ParentChild);
            Link(greatGrandchild, spouse, KinshipType.Spouse);
            Link(greatGrandchild, sibling, KinshipType.Sibling);
            Link(M2FixedScenario.NobumasaId, adopted, KinshipType.Adoption);

            _panel.Open();

            Assert.That(_panel.VisibleNodeCount, Is.EqualTo(12));
            Assert.That(_panel.KinNodeCount, Is.EqualTo(11));
            Assert.That(_panel.ContentSize.x, Is.GreaterThan(0f));
            Assert.That(_panel.ContentSize.y, Is.GreaterThan(410f),
                "a taller graph must extend beyond the viewport so it can scroll");
            Assert.That(_panel.ContentSize.y,
                Is.EqualTo(_panel.LastLayoutContentSize.Height).Within(0.01f));
            Assert.That(_panel.ScrollRect.vertical, Is.True);
            Assert.That(_panel.NodeTextFor(sixth), Does.Contain("第6代"));
            Assert.That(_panel.NodeTextFor(sibling), Does.Contain("第4代"));
            Assert.That(_panel.NodeTextFor(adopted), Does.Contain("养子/养女"));

            RectTransform deepest = FindNode(sixth);
            Assert.That(deepest, Is.Not.Null);
            Assert.That(deepest.anchoredPosition.y, Is.LessThan(0f));
            Assert.That(-deepest.anchoredPosition.y,
                Is.LessThanOrEqualTo(_panel.ContentSize.y));
        }

        [Test]
        public void CloseAndReopen_DoesNotDuplicateNodesOrEdges()
        {
            _panel.Open();
            int nodes = _panel.VisibleNodeCount;
            int relations = _panel.VisibleRelationCount;

            _panel.Close();
            _panel.Open();
            _panel.Refresh();

            Assert.That(_panel.IsOpen, Is.True);
            Assert.That(_panel.VisibleNodeCount, Is.EqualTo(nodes));
            Assert.That(_panel.VisibleRelationCount, Is.EqualTo(relations));
            Assert.That(_panel.NodeInstanceCount(M2FixedScenario.HarunobuId), Is.EqualTo(1));
            Assert.That(_panel.NodeInstanceCount(M2FixedScenario.SakonId), Is.EqualTo(1));
            Assert.That(_nodeLayerObject.transform.childCount,
                Is.EqualTo(nodes + _panel.VisibleSectionCount + 1),
                "only live cards, section labels and the inactive node template may exist");
            int edgeChildren = _edgeLayerObject.transform.childCount;
            Assert.That(edgeChildren, Is.EqualTo(_panel.VisibleEdgeSegmentCount + 1),
                "only live line segments plus the inactive edge template may exist");
        }

        [Test]
        public void OpenScrollRefreshAndClose_DoNotMutateSuccessionOrKinship()
        {
            SuccessionState state = _session.Succession;
            int successionCount = state.SuccessionCount;
            int linkCount = state.World.Kinship.Count;
            int memberCount = state.World.Family.MemberCount;
            int personCount = state.World.PersonCount;
            PersonId head = state.HeadPersonId;
            PersonId heir = state.DesignatedHeirId;

            _panel.Open();
            _panel.ScrollRect.horizontalNormalizedPosition = 1f;
            _panel.ScrollRect.verticalNormalizedPosition = 0.25f;
            _panel.Refresh();
            _panel.Close();

            Assert.That(state.SuccessionCount, Is.EqualTo(successionCount));
            Assert.That(state.World.Kinship.Count, Is.EqualTo(linkCount));
            Assert.That(state.World.Family.MemberCount, Is.EqualTo(memberCount));
            Assert.That(state.World.PersonCount, Is.EqualTo(personCount));
            Assert.That(state.HeadPersonId, Is.EqualTo(head));
            Assert.That(state.DesignatedHeirId, Is.EqualTo(heir));
        }

        [Test]
        public void SessionStateChanged_RefreshesHeadAndHeirMarkers()
        {
            _panel.Open();
            Assert.That(_panel.NodeTextFor(M2FixedScenario.HarunobuId), Does.Contain("家主"));

            _session.InitStandardDemo();

            Assert.That(_panel.NodeTextFor(SuccessionSample.StandardHeadId), Does.Contain("家主"));
            Assert.That(_panel.NodeTextFor(M2FixedScenario.HarunobuId), Is.Empty);
            Assert.That(_panel.HeirSummary, Is.EqualTo("当前模式没有固定继承人。"));
        }

        [Test]
        public void Refresh_AfterLawfulSuccessionMovesTheHeadMarker()
        {
            _session.InitStandardDemo();
            _panel.Open();

            Assert.That(_session.BeginSuccessionRetirement().Succeeded, Is.True);
            var newHead = new PersonId("person.akechi.masatsuna");
            Assert.That(_session.SelectSuccessionSuccessor(newHead).Succeeded, Is.True);
            Assert.That(_session.ConfirmSuccession().Succeeded, Is.True);

            Assert.That(_session.Succession.HeadPersonId, Is.EqualTo(newHead));
            Assert.That(_panel.NodeTextFor(newHead), Does.Contain("家主"));
            Assert.That(_panel.NodeTextFor(SuccessionSample.StandardHeadId),
                Does.Not.Contain("家主"));
        }

        [Test]
        public void SpouseConnectorRoutesAroundInterveningPersonCard()
        {
            PersonId left = AddPerson("person.route.a", "配偶甲", 1530,
                FamilyMemberRole.DirectKin);
            PersonId middle = AddPerson("person.route.b", "中间人", 1531,
                FamilyMemberRole.DirectKin);
            PersonId right = AddPerson("person.route.c", "配偶乙", 1532,
                FamilyMemberRole.Spouse);
            Link(left, middle, KinshipType.Sibling);
            Link(left, right, KinshipType.Spouse);

            _panel.Open();

            RectTransform leftRect = FindNode(left);
            RectTransform middleRect = FindNode(middle);
            RectTransform rightRect = FindNode(right);
            Assert.That(leftRect, Is.Not.Null);
            Assert.That(middleRect, Is.Not.Null);
            Assert.That(rightRect, Is.Not.Null);
            Assert.That(middleRect.anchoredPosition.y,
                Is.EqualTo(leftRect.anchoredPosition.y),
                "the regression requires all three cards in one generation row");
            Assert.That(middleRect.anchoredPosition.y,
                Is.EqualTo(rightRect.anchoredPosition.y));
            Assert.That(middleRect.anchoredPosition.x,
                Is.GreaterThan(leftRect.anchoredPosition.x));
            Assert.That(middleRect.anchoredPosition.x,
                Is.LessThan(rightRect.anchoredPosition.x));

            IReadOnlyList<FamilyTreePoint> path =
                _panel.RelationPathFor(left, right, KinshipType.Spouse);
            Assert.That(path.Count, Is.GreaterThan(2),
                "a rendered spouse connector must detour around the intervening card");

            FamilyTreeNodePlacement middlePlacement = Placement(middle, middleRect);
            for (int i = 0; i + 1 < path.Count; i++)
                Assert.That(FamilyTreeRouting.SegmentIntersectsCard(path[i], path[i + 1],
                        middlePlacement), Is.False,
                    "rendered spouse connector must not cross the intervening person card");
        }

        [Test]
        public void CardTextShowsKnownDeathAndRetirementWithoutAge()
        {
            var deceased = new FamilyTreePersonNode(new PersonId("person.card.dead"),
                "已故者", FamilyMemberRole.DirectKin, 0, false, true, false, false, false,
                false, false, FamilyTreeMemberSection.Kinship);
            var retired = new FamilyTreePersonNode(new PersonId("person.card.retired"),
                "隐居者", FamilyMemberRole.Head, 0, false, true, true, true, true, false,
                false, FamilyTreeMemberSection.Kinship);
            var unknown = new FamilyTreePersonNode(new PersonId("person.card.unknown"),
                "资料不明者", FamilyMemberRole.DirectKin, 0, false, false, false, false,
                false, false, false, FamilyTreeMemberSection.Kinship);
            var placement = new FamilyTreeNodePlacement(deceased.PersonId,
                new FamilyTreePoint(0f, 0f),
                new FamilyTreeSize(FamilyTreeLayout.NodeWidth, FamilyTreeLayout.NodeHeight), 2,
                FamilyTreeLayoutSection.Generation);

            string deceasedText = FamilyTreeCardText.Format(deceased, placement);
            Assert.That(deceasedText, Does.Contain("年龄不详"));
            Assert.That(deceasedText, Does.Contain("已故"));
            Assert.That(deceasedText, Does.Not.Contain("已隐居"));

            string retiredText = FamilyTreeCardText.Format(retired, placement);
            Assert.That(retiredText, Does.Contain("年龄不详"));
            Assert.That(retiredText, Does.Contain("已隐居"));
            Assert.That(retiredText, Does.Not.Contain("已故"));

            string unknownText = FamilyTreeCardText.Format(unknown, placement);
            Assert.That(unknownText, Does.Contain("年龄不详"));
            Assert.That(unknownText, Does.Not.Contain("已故"),
                "missing life data must not be presented as a known death");
            Assert.That(unknownText, Does.Not.Contain("已隐居"));
        }

        private PersonId AddPerson(string id, string name, int birthYear, FamilyMemberRole role)
        {
            var personId = new PersonId(id);
            SuccessionWorld world = _session.Succession.World;
            Assert.That(world.TryAddPerson(new PersonState(personId, name),
                new SuccessionPersonLife(birthYear, true, false), out string personError), Is.True,
                personError);
            Assert.That(world.TryAddMember(personId, role, out string memberError), Is.True,
                memberError);
            return personId;
        }

        private void Link(PersonId from, PersonId to, KinshipType type)
        {
            Assert.That(KinshipLink.TryCreate(from, to, type, out KinshipLink link,
                out string linkError), Is.True, linkError);
            Assert.That(_session.Succession.World.TryAddKinship(link, out string addError), Is.True,
                addError);
        }

        private RectTransform FindNode(PersonId personId) =>
            _nodeLayerObject.transform.Find(
                "FamilyRosterNode_" + (personId.Value ?? string.Empty)) as RectTransform;

        private static FamilyTreeNodePlacement Placement(PersonId personId, RectTransform rect) =>
            new FamilyTreeNodePlacement(personId,
                new FamilyTreePoint(rect.anchoredPosition.x, -rect.anchoredPosition.y),
                new FamilyTreeSize(rect.sizeDelta.x, rect.sizeDelta.y), 1,
                FamilyTreeLayoutSection.Generation);

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject Child(string name, Transform parent,
            params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            foreach (System.Type component in components)
                go.AddComponent(component);
            return go;
        }
    }
}
