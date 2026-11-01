using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class TownNpcDialoguePanelTests
    {
        private GameSession _session;
        private GameObject _fixture;
        private GameObject _townRoot;
        private GameObject _rosterRoot;
        private GameObject _dialogueRoot;
        private Transform _rosterContent;
        private Transform _optionsContent;
        private Text _rosterStatus;
        private Text _dialogueText;
        private Button _rowPrefab;
        private Button _optionPrefab;
        private Button _backToRoster;
        private Button _rosterBackToTown;
        private Button _dialogueBackToTown;
        private Button _openRoster;
        private TownNpcDialoguePanel _panel;
        private WorldMapController _map;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.NewGame();
            _fixture = new GameObject("TownNpcDialoguePanelTests");

            _townRoot = CreateRoot("TownMenu", _fixture.transform, true);
            _rosterRoot = CreateRoot("Roster", _fixture.transform, false);
            _dialogueRoot = CreateRoot("Dialogue", _fixture.transform, false);

            _rosterContent = CreateRoot(
                "RosterContent", _rosterRoot.transform, true).transform;
            _optionsContent = CreateRoot(
                "OptionsContent", _dialogueRoot.transform, true).transform;

            _rosterStatus = CreateText("RosterStatus", _rosterRoot.transform);
            _dialogueText = CreateText("DialogueText", _dialogueRoot.transform);

            _rowPrefab = CreateButtonPrefab("NpcRowPrefab", withText: true);
            _optionPrefab = CreateButtonPrefab("OptionPrefab", withText: true);
            _backToRoster = CreateButton("BackToRoster", _dialogueRoot.transform);
            _rosterBackToTown = CreateButton("RosterBackToTown", _rosterRoot.transform);
            _dialogueBackToTown = CreateButton("DialogueBackToTown", _dialogueRoot.transform);

            GameObject panelObject = new GameObject("TownNpcDialoguePanel");
            panelObject.transform.SetParent(_fixture.transform, false);
            _panel = panelObject.AddComponent<TownNpcDialoguePanel>();
            _panel.BindReferences(
                _townRoot,
                _rosterRoot,
                _rosterContent,
                _rowPrefab,
                _rosterStatus,
                _dialogueRoot,
                _dialogueText,
                _optionsContent,
                _optionPrefab,
                _backToRoster,
                _rosterBackToTown,
                _dialogueBackToTown);

            GameObject mapObject = new GameObject("WorldMapController");
            mapObject.transform.SetParent(_fixture.transform, false);
            _map = mapObject.AddComponent<WorldMapController>();
            _map.BindReferences(
                _session, null,
                null, null, null, null, null, null,
                null, null, null,
                _townRoot, null, new Text[0]);

            _openRoster = CreateButton("OpenRoster", _townRoot.transform);
            _map.BindTownNpcRoster(_panel, _openRoster);
        }

        [TearDown]
        public void TearDown()
        {
            if (_fixture != null)
                Object.DestroyImmediate(_fixture);
            if (_session != null)
                Object.DestroyImmediate(_session.gameObject);
            if (_rowPrefab != null)
                Object.DestroyImmediate(_rowPrefab.gameObject);
            if (_optionPrefab != null)
                Object.DestroyImmediate(_optionPrefab.gameObject);
        }

        // 纯 Domain/Application API 验证，不依赖 UI 对象或 EventSystem。
        [Test]
        public void AssignmentCatalog_SeparatesNpcListsByTown()
        {
            var kiyosu = TownNpcAssignments.GetNpcs(LocationId.Kiyosu.Value);
            var atsuta = TownNpcAssignments.GetNpcs(LocationId.Atsuta.Value);
            var nagoya = TownNpcAssignments.GetNpcs(LocationId.Nagoya.Value);

            Assert.That(kiyosu, Does.Contain(CharacterId.TachibanaSuzu));
            Assert.That(kiyosu, Does.Contain(CharacterId.MoriGenzaemon));
            CollectionAssert.DoesNotContain(kiyosu, CharacterId.KurodaIori);
            Assert.That(atsuta, Is.EqualTo(new[] { CharacterId.KurodaIori }));
            Assert.That(nagoya, Does.Contain(CharacterId.HoshinoAya));
            Assert.That(nagoya, Does.Contain(CharacterId.SakaiJirobei));
        }

        [Test]
        public void RosterUsesSessionCurrentTown()
        {
            TravelTo(LocationId.Atsuta);

            _panel.OpenRoster(_session);

            Assert.That(_rosterContent.childCount, Is.EqualTo(1));
            Assert.That(
                _rosterContent.GetChild(0)
                    .GetComponentInChildren<Text>(true).text,
                Does.Contain("黒田 伊織"));
        }

        [Test]
        public void RelationshipReadReflectsSessionValue()
        {
            _session.TryChangeRelationship(CharacterId.KurodaIori, 7);
            TravelTo(LocationId.Atsuta);

            _panel.OpenRoster(_session);

            Assert.That(
                _rosterContent.GetChild(0)
                    .GetComponentInChildren<Text>(true).text,
                Does.Contain("+7"));
        }

        [Test]
        public void NpcWithoutDialogueIsVisibleButDisabled()
        {
            _panel.OpenRoster(_session);

            Assert.That(_rosterContent.childCount, Is.EqualTo(2));
            for (int i = 0; i < _rosterContent.childCount; i++)
            {
                Button row = _rosterContent.GetChild(i).GetComponent<Button>();
                Assert.That(row.interactable, Is.False);
                Assert.That(
                    row.GetComponentInChildren<Text>(true).text,
                    Does.Contain("暂无对话"));
            }
        }

        [Test]
        public void AcceptChangesRelationshipByTenAndEndsAtTerminalNode()
        {
            TravelTo(LocationId.Atsuta);
            _panel.OpenRoster(_session);
            _rosterContent.GetChild(0).GetComponent<Button>().onClick.Invoke();

            Assert.That(_optionsContent.childCount, Is.EqualTo(2));
            _optionsContent.GetChild(0).GetComponent<Button>().onClick.Invoke();

            Assert.That(
                _session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                Is.EqualTo(10));
            Assert.That(_dialogueText.text, Does.Contain("Trust is earned."));
            Assert.That(_dialogueText.text, Does.Contain("+10"));
            Assert.That(ActiveChildCount(_optionsContent), Is.Zero);
        }

        [Test]
        public void DeclineLeavesRelationshipUnchangedAndEndsAtTerminalNode()
        {
            TravelTo(LocationId.Atsuta);
            _panel.OpenRoster(_session);
            _rosterContent.GetChild(0).GetComponent<Button>().onClick.Invoke();

            _optionsContent.GetChild(1).GetComponent<Button>().onClick.Invoke();

            Assert.That(
                _session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                Is.Zero);
            Assert.That(_dialogueText.text, Does.Contain("Farewell."));
            Assert.That(ActiveChildCount(_optionsContent), Is.Zero);
        }

        [Test]
        public void InvalidOptionDoesNotChangeRelationship()
        {
            DialogueStartResult started =
                _session.TryStartDialogue(CharacterId.KurodaIori);

            DialogueSelectionResult result =
                _session.TrySelectDialogueOption(
                    started.DialogueInstanceId,
                    started.CurrentNodeId,
                    new DialogueOptionId("invalid-option"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.CurrentNodeId, Is.EqualTo(started.CurrentNodeId));
            Assert.That(
                _session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                Is.Zero);
        }

        [Test]
        public void InvalidNodeAndUnknownNpcFailWithoutChangingRelationship()
        {
            DialogueStartResult started =
                _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult invalidNode =
                _session.TrySelectDialogueOption(
                    started.DialogueInstanceId,
                    "not-a-catalog-node",
                    BranchingDialogueCatalog.KurodaOfferOption);
            DialogueStartResult unknownNpc =
                _session.TryStartDialogue(new CharacterId("npc.unknown"));

            Assert.That(invalidNode.Succeeded, Is.False);
            Assert.That(invalidNode.CurrentNodeId, Is.EqualTo(started.CurrentNodeId));
            Assert.That(unknownNpc.Succeeded, Is.False);
            Assert.That(unknownNpc.DialogueInstanceId, Is.EqualTo(System.Guid.Empty));
            Assert.That(
                _session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                Is.Zero);
        }

        // EditMode 只验证一次 Accept 后的 UI 更新与动态选项清理；
        // 同帧快速连点需要真实指针输入，留给后续 Play Mode 验证。
        [Test]
        public void AcceptUpdatesUiAndDestroysDynamicOptions()
        {
            TravelTo(LocationId.Atsuta);
            _panel.OpenRoster(_session);
            _rosterContent.GetChild(0).GetComponent<Button>().onClick.Invoke();

            Button accept = _optionsContent.GetChild(0).GetComponent<Button>();
            accept.onClick.Invoke();

            Assert.That(
                _session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                Is.EqualTo(10));
            Assert.That(_dialogueText.text, Does.Contain("Trust is earned."));
            Assert.That(_dialogueText.text, Does.Contain("+10"));
            Assert.That(_optionsContent.childCount, Is.Zero);
            Assert.That(ActiveChildCount(_optionsContent), Is.Zero);
            // DestroyImmediate 后只检查 Unity 的销毁态比较，不访问实例成员。
            Assert.That(accept == null, Is.True);
        }

        // 以下测试使用真实 Unity UI GameObject，但不验证 Canvas 绘制或指针输入。
        [Test]
        public void RosterShowsOnlyNpcsAssignedToCurrentTown()
        {
            _panel.OpenRoster(_session);

            Assert.That(_rosterContent.childCount, Is.EqualTo(2));
            string first = _rosterContent.GetChild(0)
                .GetComponentInChildren<Text>(true).text;
            string second = _rosterContent.GetChild(1)
                .GetComponentInChildren<Text>(true).text;
            Assert.That(first + second, Does.Contain("橘 すず"));
            Assert.That(first + second, Does.Contain("森 源左衛門"));
            Assert.That(first + second, Does.Not.Contain("黒田 伊織"));
        }

        [Test]
        public void OpeningRosterDoesNotDestroyOrMoveButtonTemplate()
        {
            Transform originalParent = _rowPrefab.transform.parent;
            _panel.OpenRoster(_session);

            Assert.That(_rowPrefab != null, Is.True);
            Assert.That(_rowPrefab.gameObject.activeSelf, Is.True);
            Assert.That(_rowPrefab.transform.parent, Is.EqualTo(originalParent));
        }

        [Test]
        public void ClosingRosterRemovesOwnedRowsButPreservesStaticContent()
        {
            Text staticText = CreateText("StaticRosterContent", _rosterContent);
            _panel.OpenRoster(_session);
            Button oldRow = _rosterContent.GetChild(0).GetComponent<Button>();

            _panel.Close();

            Assert.That(oldRow == null, Is.True);
            Assert.That(_rosterContent.childCount, Is.EqualTo(1));
            Assert.That(_rosterContent.GetChild(0).gameObject, Is.EqualTo(staticText.gameObject));
        }

        [Test]
        public void MissingTextTemplateCloneIsDestroyedWithoutTouchingTemplate()
        {
            Button badPrefab = CreateButtonPrefab("BadRowPrefab", withText: false);
            Transform originalParent = badPrefab.transform.parent;
            _panel.BindReferences(
                _townRoot,
                _rosterRoot,
                _rosterContent,
                badPrefab,
                _rosterStatus,
                _dialogueRoot,
                _dialogueText,
                _optionsContent,
                _optionPrefab,
                _backToRoster,
                _rosterBackToTown,
                _dialogueBackToTown);

            _panel.OpenRoster(_session);

            Assert.That(_rosterContent.childCount, Is.Zero);
            Assert.That(badPrefab != null, Is.True);
            Assert.That(badPrefab.gameObject.activeSelf, Is.True);
            Assert.That(badPrefab.transform.parent, Is.EqualTo(originalParent));
            Object.DestroyImmediate(badPrefab.gameObject);
        }

        [Test]
        public void RepeatedOpenDestroysOldRowsBeforeCreatingNewOnes()
        {
            _panel.OpenRoster(_session);
            Button firstGeneration = _rosterContent.GetChild(0).GetComponent<Button>();

            // 不同 session 会触发名单重建；旧克隆立即禁用并移除监听。
            GameSession otherSession = GameSession.NewGame();
            _panel.OpenRoster(otherSession);

            Assert.That(firstGeneration == null, Is.True);
            Assert.That(_rosterContent.childCount, Is.EqualTo(2));
            Object.DestroyImmediate(otherSession.gameObject);
        }

        [Test]
        public void ReEnableAfterPanelDisableRebuildsClearedRoster()
        {
            _panel.OpenRoster(_session);
            _panel.enabled = false;
            _panel.enabled = true;

            _panel.OpenRoster(_session);

            Assert.That(_rosterContent.childCount, Is.EqualTo(2));
        }

        [Test]
        public void GameSessionRejectsRepeatedAcceptForStaleNodeWithoutReapplyingEffect()
        {
            DialogueStartResult started =
                _session.TryStartDialogue(CharacterId.KurodaIori);
            DialogueSelectionResult accepted =
                _session.TrySelectDialogueOption(
                    started.DialogueInstanceId,
                    started.CurrentNodeId,
                    BranchingDialogueCatalog.KurodaOfferOption);
            DialogueSelectionResult duplicate =
                _session.TrySelectDialogueOption(
                    started.DialogueInstanceId,
                    started.CurrentNodeId,
                    BranchingDialogueCatalog.KurodaOfferOption);

            Assert.That(accepted.Succeeded, Is.True, accepted.Error);
            Assert.That(
                accepted.CurrentNodeId,
                Is.EqualTo(BranchingDialogueCatalog.KurodaTrustNode));
            Assert.That(accepted.IsEnded, Is.True);
            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(
                _session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue,
                Is.EqualTo(10));
        }

        [Test]
        public void BackToRosterAndTownPreserveExistingTownRootAndFacilityButton()
        {
            int clickCount = 0;
            Button facilityButton = CreateButton("ExistingFacility", _townRoot.transform);
            facilityButton.onClick.AddListener(() => clickCount++);

            TravelTo(LocationId.Atsuta);
            _map.OpenTownMenu();
            _panel.OpenRoster(_session);
            _rosterContent.GetChild(0).GetComponent<Button>().onClick.Invoke();
            _backToRoster.onClick.Invoke();
            _rosterBackToTown.onClick.Invoke();

            Assert.That(_townRoot.activeSelf, Is.True);
            Assert.That(_rosterRoot.activeSelf, Is.False);
            Assert.That(_dialogueRoot.activeSelf, Is.False);
            Assert.That(_map.InTownMenu, Is.True);
            Assert.That(facilityButton != null, Is.True);
            Assert.That(facilityButton.interactable, Is.True);

            facilityButton.onClick.Invoke();
            Assert.That(clickCount, Is.EqualTo(1));
        }

        [Test]
        public void DialogueBackToTownRestoresTownMenuAndClosesDialogue()
        {
            _map.OpenTownMenu();
            _panel.OpenRoster(_session);
            _rosterContent.GetChild(0).GetComponent<Button>().onClick.Invoke();

            _dialogueBackToTown.onClick.Invoke();

            Assert.That(_map.InTownMenu, Is.True);
            Assert.That(_townRoot.activeSelf, Is.True);
            Assert.That(_rosterRoot.activeSelf, Is.False);
            Assert.That(_dialogueRoot.activeSelf, Is.False);
        }

        [Test]
        public void RosterBackToTownWorksAfterRepeatedPanelEnableCycles()
        {
            _panel.enabled = false;
            _panel.enabled = true;
            _panel.enabled = false;
            _panel.enabled = true;
            _map.OpenTownMenu();
            _panel.OpenRoster(_session);

            _rosterBackToTown.onClick.Invoke();

            Assert.That(_map.InTownMenu, Is.True);
            Assert.That(_townRoot.activeSelf, Is.True);
            Assert.That(_rosterRoot.activeSelf, Is.False);
            Assert.That(_dialogueRoot.activeSelf, Is.False);
        }

        [Test]
        public void MapReturnClosesNpcPanelsWithoutDestroyingTownMenuObject()
        {
            _map.OpenTownMenu();
            _map.OpenNpcRoster();

            _map.ReturnToMap();

            Assert.That(_townRoot != null, Is.True);
            Assert.That(_townRoot.activeSelf, Is.False);
            Assert.That(_rosterRoot.activeSelf, Is.False);
            Assert.That(_dialogueRoot.activeSelf, Is.False);
            Assert.That(_map.InTownMenu, Is.False);
        }

        [Test]
        public void RepeatedEnableAndOpenDoesNotDuplicateRosterRows()
        {
            _map.enabled = false;
            _map.enabled = true;
            _map.enabled = false;
            _map.enabled = true;

            _map.OpenTownMenu();
            _openRoster.onClick.Invoke();
            _openRoster.onClick.Invoke();

            Assert.That(_rosterContent.childCount, Is.EqualTo(2));
        }

        private void TravelTo(LocationId destination)
        {
            TravelPreview? preview = _session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True);
            Assert.That(_session.ConfirmTravel(preview.Value), Is.True);
        }

        private static int ActiveChildCount(Transform parent)
        {
            int count = 0;
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).gameObject.activeSelf)
                    count++;
            return count;
        }

        private static GameObject CreateRoot(
            string name, Transform parent, bool active)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            root.SetActive(active);
            return root;
        }

        private static Text CreateText(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<Text>();
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            go.transform.SetParent(parent, false);
            return go.AddComponent<Button>();
        }

        private static Button CreateButtonPrefab(string name, bool withText)
        {
            var go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            Button button = go.AddComponent<Button>();

            if (withText)
            {
                var label = new GameObject(
                    "Label",
                    typeof(RectTransform),
                    typeof(CanvasRenderer));
                label.transform.SetParent(go.transform, false);
                label.AddComponent<Text>();
            }

            return button;
        }
    }
}
