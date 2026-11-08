using System;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Events;
using SengokuChronicle.Presentation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M02 focused presentation and scene-integration coverage: stable person ordering,
    /// location/in-transit and progress rendering, positive-duration commands, travel
    /// through WorldMapRules, interruption/replanning feedback, the one-day control's
    /// monthly boundary, the reference-page composition (header, nav rail, parchment
    /// profile/action columns and the separated read-only inspector rail), and the
    /// transactional generator's validation/rollback. No Play Mode behavior is asserted.
    /// </summary>
    public sealed class M02DailyActionPanelTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();
            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
            _session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            _session.InitNewGame();
            Create("WorldClockPanel", null, typeof(WorldClockPanel));
            GameObject familyRoot = Create("FamilyRosterRoot", null);
            Create("FamilyRosterPanel", familyRoot.transform);
            Create("SuccessionRoot", null);
            Create("FamilyChronicleRoot", null);
            Create("FamilyDoctrineRoot", null);
            Create("SC022FamilyFormationRoot", null);
            GameObject industryRoot = Create("IndustryRoot", null);
            Create("IndustryPanel", industryRoot.transform);
            Create("M2WorldMapRoot", null);
            GameObject eventRoot = Create("EventChoiceRoot", null);
            Create("EventChoiceModal", eventRoot.transform);
            Create("M03NpcAiInspectorRoot", null);
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        private DailyActionPanel Panel() =>
            Find(AddM02DailyActionPanel.RootName).GetComponent<DailyActionPanel>();

        private CharacterDailyPageView View() =>
            Find(AddM02DailyActionPanel.RootName).GetComponent<CharacterDailyPageView>();

        [Test]
        public void Navigator_DefaultCharacterPageOpensWithTheCurrentHeadSelected()
        {
            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();

            navigator.ShowPage(ReferenceGalleryPage.Character);

            Assert.That(Panel().IsOpen, Is.True);
            Assert.That(Panel().SelectedPersonId.IsEmpty, Is.False,
                "the page must render the live current head instead of an unselected placeholder");
            Assert.That(View().ProfileNameLine, Is.Not.Empty);
            Assert.That(View().ProfileNameLine, Does.Not.Contain("未选择"));
        }

        [Test]
        public void Generator_ProducesValidIdempotentSceneAndPreservesTheClockBindings()
        {
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(FindAll(AddM02DailyActionPanel.RootName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM02DailyActionPanel.OpenButtonName).Length, Is.EqualTo(1));

            DailyActionPanel panel = Panel();
            Assert.That(panel.IsBound, Is.True);
            Assert.That(panel.IsOpen, Is.False,
                "the panel must start hidden and be opened by the HUD button");
            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(9));

            var panelRect = (RectTransform)Find(AddM02DailyActionPanel.PanelName);
            var durationLabel =
                (RectTransform)Find(AddM02DailyActionPanel.DurationLabelName);
            Assert.That(durationLabel.anchoredPosition.x,
                Is.GreaterThanOrEqualTo(10f),
                "the duration label must keep a real margin from the panel edge");

            var profile = (RectTransform)Find(AddM02DailyActionPanel.SelectedName);
            var profileText = profile.GetComponent<Text>();
            Assert.That(profile.rect.width, Is.GreaterThanOrEqualTo(360f),
                "the selected profile must be a substantial information column");
            Assert.That(profile.rect.height, Is.GreaterThanOrEqualTo(300f));
            Assert.That(profileText.alignment, Is.EqualTo(TextAnchor.UpperLeft));
            Assert.That(profileText.fontSize, Is.GreaterThanOrEqualTo(16));
            Assert.That(profileText.verticalOverflow,
                Is.EqualTo(VerticalWrapMode.Truncate));
            Assert.That(profileText.supportRichText, Is.True);
            Assert.That(profileText.text, Does.Contain("人物档案"));

            var feedback = (RectTransform)Find(AddM02DailyActionPanel.FeedbackName);
            var feedbackText = feedback.GetComponent<Text>();
            var close = (RectTransform)Find(AddM02DailyActionPanel.CloseButtonName);
            Assert.That(feedback.rect.height, Is.GreaterThanOrEqualTo(48f),
                "feedback needs room for three wrapped lines");
            Assert.That(feedbackText.verticalOverflow,
                Is.EqualTo(VerticalWrapMode.Truncate),
                "feedback must clip instead of spilling over nearby controls");
            float feedbackBottom = feedback.anchoredPosition.y - feedback.rect.height;
            float closeTop = close.anchoredPosition.y;
            Assert.That(feedbackBottom, Is.GreaterThanOrEqualTo(closeTop + 2f),
                "feedback and the Close button must stay separated");
        }

        [Test]
        public void Generator_BuildsTheReferencePageComposition()
        {
            var panelRect = (RectTransform)Find(AddM02DailyActionPanel.PanelName);
            Assert.That(panelRect.rect.width, Is.EqualTo(1280f).Within(1f),
                "the page fills the 1280x720 reference canvas");
            Assert.That(panelRect.rect.height, Is.EqualTo(720f).Within(1f));

            var backdrop = (RectTransform)Find(AddM02DailyActionPanel.BackdropName);
            var topBar = (RectTransform)Find(AddM02DailyActionPanel.TopBarName);
            var navRail = (RectTransform)Find(AddM02DailyActionPanel.NavRailName);
            var profilePanel = (RectTransform)Find(AddM02DailyActionPanel.ProfilePanelName);
            var actionPanel = (RectTransform)Find(AddM02DailyActionPanel.ActionPanelName);
            var inspectorRail = (RectTransform)Find(AddM02DailyActionPanel.InspectorRailName);

            // Top-left anchored layout: the y in anchoredPosition is the distance below the
            // panel top, and x is the distance from the panel's left edge.
            Assert.That(topBar.rect.width, Is.EqualTo(1280f).Within(1f),
                "the identity/date/resource header spans the full width");
            Assert.That(navRail.rect.width, Is.EqualTo(150f).Within(1f),
                "the left navigation rail keeps the reference width");
            Assert.That(navRail.anchoredPosition.y, Is.LessThanOrEqualTo(-52f),
                "the nav rail starts under the header");

            Assert.That(profilePanel.anchoredPosition.x,
                Is.GreaterThan(navRail.rect.width),
                "the profile column sits right of the nav rail");
            Assert.That(actionPanel.anchoredPosition.x,
                Is.GreaterThan(profilePanel.anchoredPosition.x + profilePanel.rect.width),
                "the action column sits right of the profile column");
            Assert.That(inspectorRail.anchoredPosition.x,
                Is.GreaterThan(actionPanel.anchoredPosition.x + actionPanel.rect.width),
                "the read-only inspector is a separate column on the right");

            Assert.That(profilePanel.rect.height, Is.EqualTo(actionPanel.rect.height).Within(1f));
            Assert.That(inspectorRail.rect.height, Is.EqualTo(actionPanel.rect.height).Within(1f));

            // Parchment content, indigo chrome, and reference-derived portrait art.
            Sprite paper = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Art/UI/paper_grain.png");
            Assert.That((Color32)profilePanel.GetComponent<Image>().color,
                Is.EqualTo((Color32)SengokuUiTheme.ParchmentLight),
                "the profile card keeps a clean parchment base");
            Assert.That((Color32)actionPanel.GetComponent<Image>().color,
                Is.EqualTo((Color32)SengokuUiTheme.ParchmentLight),
                "the action card keeps a clean parchment base");
            Assert.That(Find("M02ProfilePaperGrain").GetComponent<Image>().sprite,
                Is.EqualTo(paper), "the profile card overlays the supplied paper grain");
            Assert.That(Find("M02ActionPaperGrain").GetComponent<Image>().sprite,
                Is.EqualTo(paper), "the action card overlays the supplied paper grain");
            Assert.That((Color32)backdrop.GetComponent<Image>().color,
                Is.EqualTo((Color32)SengokuUiTheme.IndigoDeep));
            Assert.That(((RectTransform)navRail).GetComponent<Image>().color.a,
                Is.EqualTo(1f), "the page navigation uses an opaque dark rail");
            Assert.That((Color32)topBar.GetComponent<Image>().color,
                Is.EqualTo((Color32)SengokuUiTheme.IndigoDeep));
            Assert.That((Color32)inspectorRail.GetComponent<Image>().color,
                Is.EqualTo((Color32)SengokuUiTheme.IndigoPanel),
                "the inspector rail stays a dark developer surface, not parchment");
            var portrait = Find(AddM02DailyActionPanel.ProfilePortraitName).GetComponent<Image>();
            Assert.That(portrait.sprite, Is.Not.Null,
                "the selected character uses the supplied reference portrait");

            // The shared rail exposes the five reference pages as real controls.
            string[] pageButtons =
            {
                AddM02DailyActionPanel.NavHouseholdButtonName,
                AddM02DailyActionPanel.NavCharacterButtonName,
                AddM02DailyActionPanel.NavFamilyButtonName,
                AddM02DailyActionPanel.NavMapButtonName,
                AddM02DailyActionPanel.NavEventsButtonName
            };
            foreach (string buttonName in pageButtons)
                Assert.That(Find(buttonName).GetComponent<Button>(), Is.Not.Null);
            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            Assert.That(navigator.IsBound, Is.True);
            Assert.That(navigator.CurrentPage, Is.EqualTo(ReferenceGalleryPage.Character));

            // Action cards and progress controls exist inside the action panel.
            Assert.That(FindAll(AddM02DailyActionPanel.TravelCardName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM02DailyActionPanel.ProgressFillName).Length, Is.EqualTo(1));
            Image progressFill = Find(AddM02DailyActionPanel.ProgressFillName)
                .GetComponent<Image>();
            Assert.That(progressFill.type, Is.EqualTo(Image.Type.Filled));

            // Tabs are real controls; the relations/history bodies start hidden.
            Assert.That(Find(AddM02DailyActionPanel.RelationsBodyName).gameObject.activeSelf,
                Is.False);
            Assert.That(Find(AddM02DailyActionPanel.HistoryBodyName).gameObject.activeSelf,
                Is.False);
            Assert.That(Find(AddM02DailyActionPanel.ProfileBodyName).gameObject.activeSelf,
                Is.True);
        }

        [Test]
        public void Validate_ToleratesSubpixelEdgeRoundingButRejectsRealOverflow()
        {
            // The action column is 618 tall; Close sits at y=-530 with height 32, so a
            // 0.25px edge rounding stays within tolerance but 1px of overflow is rejected.
            var close = (RectTransform)Find(AddM02DailyActionPanel.CloseButtonName);
            Vector2 start = close.anchoredPosition;

            close.anchoredPosition = new Vector2(start.x, start.y - 56.25f);
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string subpixel),
                Is.True, subpixel);

            close.anchoredPosition = new Vector2(start.x, start.y - 57f);
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string overflow),
                Is.False);
            Assert.That(overflow, Does.Contain("outside the action panel"));

            close.anchoredPosition = start;
        }

        [Test]
        public void Validate_KeepsEveryColumnInsideVariableCanvasHeights()
        {
            // The live Game View canvas on this host scales the reference page down
            // to 1311.6x702.6; the content columns previously ended at 704 and hung
            // 1.4px below the panel. Every column must stay inside both that canvas
            // and the 1280x720 design canvas.
            RectTransform canvas = (RectTransform)Find("Canvas");
            RectTransform panel = (RectTransform)Find(AddM02DailyActionPanel.PanelName);
            RectTransform inspector =
                (RectTransform)Find(AddM02DailyActionPanel.InspectorRailName);
            RectTransform profile = (RectTransform)Find(AddM02DailyActionPanel.ProfilePanelName);
            RectTransform action = (RectTransform)Find(AddM02DailyActionPanel.ActionPanelName);
            RectTransform nav = (RectTransform)Find(AddM02DailyActionPanel.NavRailName);

            canvas.sizeDelta = new Vector2(1311.6f, 702.6f);
            Canvas.ForceUpdateCanvases();
            Assert.That(panel.rect.height, Is.EqualTo(702.6f).Within(0.5f));
            foreach (RectTransform column in new[] { profile, action, inspector, nav })
            {
                float bottom = Mathf.Abs(column.anchoredPosition.y) + column.rect.height;
                Assert.That(bottom, Is.LessThanOrEqualTo(panel.rect.height),
                    column.name + " must not hang below the live canvas");
            }
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string live),
                Is.True, live);

            canvas.sizeDelta = new Vector2(1280f, 720f);
            Canvas.ForceUpdateCanvases();
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string design),
                Is.True, design);
        }

        [Test]
        public void Validate_RejectsFeedbackConfiguredToOverflow()
        {
            Text feedback = Find(AddM02DailyActionPanel.FeedbackName).GetComponent<Text>();
            feedback.verticalOverflow = VerticalWrapMode.Overflow;
            try
            {
                Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error),
                    Is.False);
                Assert.That(error, Does.Contain("feedback"));
            }
            finally
            {
                feedback.verticalOverflow = VerticalWrapMode.Truncate;
            }
        }

        [Test]
        public void Validate_RejectsMergingTheInspectorIntoTheActionColumn()
        {
            var inspector = (RectTransform)Find(AddM02DailyActionPanel.InspectorRailName);
            Vector2 start = inspector.anchoredPosition;

            inspector.anchoredPosition = new Vector2(start.x - 120f, start.y);
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.False);
            Assert.That(error, Does.Contain("separate read-only column"));

            inspector.anchoredPosition = start;
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string restored),
                Is.True, restored);
        }

        [Test]
        public void Generator_FaultInjectionRollsBackWithoutLeavingPartialControls()
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject canvas = CreateIn(scene, "Canvas", null, typeof(Canvas));
                canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(1280f, 720f);
                GameSession session = CreateIn(scene, "GameSession", null, typeof(GameSession))
                    .GetComponent<GameSession>();
                session.InitNewGame();

                AddM02DailyActionPanel.UpdateResult result = AddM02DailyActionPanel.Update(scene,
                    root =>
                    {
                        Transform open = root.transform.Find(
                            AddM02DailyActionPanel.OpenButtonName);
                        Assert.That(open, Is.Not.Null);
                        open.name = "BrokenM02OpenButton";
                    });

                Assert.That(result,
                    Is.EqualTo(AddM02DailyActionPanel.UpdateResult.ValidationFailed));
                Assert.That(FindAllIn(scene, AddM02DailyActionPanel.RootName), Is.Empty);
                Assert.That(FindAllIn(scene, AddM02DailyActionPanel.PanelName), Is.Empty);
                Assert.That(scene.GetRootGameObjects()
                        .SelectMany(root => root.GetComponentsInChildren<DailyActionPanel>(true))
                        .ToArray(),
                    Is.Empty);
            }
            finally
            {
                if (scene.IsValid())
                    EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void Generator_ReplacesThePreviousSliceLayoutInsideOneTransaction()
        {
            // The live FoundationScene carries the earlier simpler layout; re-running must
            // rebuild the page instead of refusing, and must stay idempotent afterwards.
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(FindAll(AddM02DailyActionPanel.RootName).Length, Is.EqualTo(1));
            Assert.That(FindAll(AddM02DailyActionPanel.BackdropName).Length, Is.EqualTo(1));
        }

        [Test]
        public void Panel_ListsRegisteredPeopleInStableOrderWithLocationAndActionProgress()
        {
            DailyActionPanel panel = Panel();
            panel.Open();

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.RowCount, Is.EqualTo(_session.DailyActions.Count));
            Assert.That(panel.TitleLine, Does.Contain("每日行动"));

            string previous = string.Empty;
            for (int i = 0; i < panel.RowCount; i++)
            {
                string personId = panel.RowPersonId(i).Value;
                Assert.That(string.CompareOrdinal(previous, personId), Is.LessThan(0),
                    "rows must follow ordinal person-ID order");
                previous = personId;
                Assert.That(panel.RowText(i), Does.Contain("清洲"));
                if (NpcAutonomyRules.IsPilot(panel.RowPersonId(i)))
                    Assert.That(panel.RowText(i), Does.Not.Contain("空闲"),
                        "M03 pilots start with an autonomous action");
                else
                    Assert.That(panel.RowText(i), Does.Contain("空闲"));
            }

            panel.SelectPerson(M2FixedScenario.HarunobuId);
            panel.SetDuration("2");
            Assert.That(panel.ClickRest(), Is.True, panel.FeedbackLine);

            Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                out DailyActionTask task), Is.True);
            Assert.That(task.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("休息 0/2"));
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("剩 2"));
            Assert.That(panel.SelectedLine, Does.Contain("休息 进度 0/2"));
            Assert.That(panel.FeedbackVisible, Is.True);
            Assert.That(panel.FeedbackLine, Does.Contain("已安排"));
        }

        [Test]
        public void Panel_ProfileUsesLiveRosterFactsAndFollowsSelection()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            M2RosterRow harunobu = _session.FamilyRoster.Single(
                row => row.PersonId == M2FixedScenario.HarunobuId);

            panel.SelectPerson(M2FixedScenario.HarunobuId);

            Assert.That(panel.ProfileLine, Does.Contain("人物档案"));
            Assert.That(panel.ProfileLine, Does.Contain(harunobu.DisplayName));
            Assert.That(panel.ProfileLine, Does.Contain(harunobu.Age + "岁"));
            Assert.That(panel.ProfileLine, Does.Contain(harunobu.RoleName));
            Assert.That(panel.ProfileLine, Does.Contain(harunobu.GenerationLabel));
            Assert.That(panel.ProfileLine, Does.Contain(harunobu.Kinship));
            Assert.That(panel.ProfileLine, Does.Contain("所在：清洲"));
            Assert.That(panel.ProfileLine, Does.Contain("行动：空闲"));

            M2RosterRow chie = _session.FamilyRoster.Single(
                row => row.PersonId == M2FixedScenario.ChieId);
            panel.SelectPerson(M2FixedScenario.ChieId);

            Assert.That(panel.SelectedPersonId, Is.EqualTo(M2FixedScenario.ChieId));
            Assert.That(panel.ProfileLine, Does.Contain(chie.DisplayName));
            Assert.That(panel.ProfileLine, Does.Contain(chie.RoleName));
            Assert.That(panel.ProfileLine, Does.Contain(chie.Kinship),
                "the profile must follow the selected family member");
            Assert.That(panel.ProfileLine, Does.Not.Contain(harunobu.Kinship),
                "the previous member's kinship must not leak into the new profile");
        }

        [Test]
        public void PageView_RendersHeaderAndProfileFromLiveSessionData()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            CharacterDailyPageView view = View();
            Assert.That(view.IsBound, Is.True);

            M2RosterRow head = _session.FamilyRoster.Single(
                row => row.IsHead);
            Assert.That(view.ProfileNameLine, Is.EqualTo(head.DisplayName),
                "the page shows the session's current head by default");
            Assert.That(view.HeaderDateLine, Does.Contain(
                _session.WorldClock.CurrentDate.Year.ToString()),
                "the header date is the live world clock value");
            Assert.That(view.HeaderLocationLine, Does.Contain("清洲"));
            Assert.That(view.HeaderLocationLine, Does.Not.Contain("未知"));
            Assert.That(view.TraitLine, Does.Contain("家训"));
            Assert.That(view.ProfileNameLine, Does.Not.Contain("未选择"),
                "the page must not fall back to placeholder text for a live roster");

            // Stats and resources come from the domain, never from concept-art numbers.
            Assert.That(view.StatLine.Split('/').Length, Is.EqualTo(4));
            IndustryResources resources = _session.Industry.Resources;
            string people = _session.FamilyRoster.Count.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            string merit = _session.Merit.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(Find(AddM02DailyActionPanel.HeaderPeopleName).GetComponent<Text>().text,
                Is.EqualTo(people));
            Assert.That(Find(AddM02DailyActionPanel.HeaderMeritName).GetComponent<Text>().text,
                Is.EqualTo(merit));
            Assert.That(Find(AddM02DailyActionPanel.HeaderFundsName).GetComponent<Text>().text,
                Is.EqualTo(resources.Funds.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)));

            panel.SelectPerson(M2FixedScenario.HarunobuId);
            Assert.That(view.ProfileNameLine,
                Is.EqualTo(_session.FamilyRoster.Single(
                    row => row.PersonId == M2FixedScenario.HarunobuId).DisplayName),
                "the page profile follows the roster selection");
            Assert.That(view.RelationLine, Is.Not.EqualTo(view.ProfileNameLine),
                "relationship cards list other family members, not the selected one");
        }

        [Test]
        public void PageView_SwitchesTabsAndKeepsTheInspectorReadOnlyAndSeparate()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            CharacterDailyPageView view = View();

            Assert.That(view.CurrentTab, Is.EqualTo(CharacterDailyPageView.TabProfile));
            var historyBody = Find(AddM02DailyActionPanel.HistoryBodyName).gameObject;
            var relationsBody = Find(AddM02DailyActionPanel.RelationsBodyName).gameObject;
            var profileBody = Find(AddM02DailyActionPanel.ProfileBodyName).gameObject;

            view.SetTab(CharacterDailyPageView.TabHistory);
            Assert.That(historyBody.activeSelf, Is.True);
            Assert.That(profileBody.activeSelf, Is.False);
            Assert.That(relationsBody.activeSelf, Is.False);
            Assert.That(view.HistoryTextLine, Is.Not.Empty,
                "the history tab renders real chronicle state");

            view.SetTab(CharacterDailyPageView.TabRelations);
            Assert.That(relationsBody.activeSelf, Is.True);
            Assert.That(historyBody.activeSelf, Is.False);

            view.SetTab(CharacterDailyPageView.TabProfile);
            Assert.That(profileBody.activeSelf, Is.True);

            // The rail is a read-only bridge to M03; no M03 panel exists in this fixture,
            // so the hint must say so instead of pretending the debug view is present.
            Assert.That(view.InspectorSubjectLine, Does.Contain(view.ProfileNameLine));
            Assert.That(view.InspectorHintLine, Does.Contain("只读"));
            Assert.That(panel.ActionSubmitCount, Is.EqualTo(0),
                "rendering the page never submits an action");
        }

        [Test]
        public void PageView_ProgressAndScheduleFollowTheScheduledAction()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            CharacterDailyPageView view = View();
            Assert.That(view.ProgressLine, Does.Contain("尚未安排"));
            Assert.That(view.ScheduleLine, Does.Contain("本回合尚无日程"));

            panel.SelectPerson(M2FixedScenario.HarunobuId);
            panel.SetDuration("3");
            Assert.That(panel.ClickWork(), Is.True, panel.FeedbackLine);

            Assert.That(view.ProgressLine, Does.Contain("已用 0 / 共 3 天"));
            Assert.That(view.ScheduleLine, Does.Contain("工作"));
            var fill = Find(AddM02DailyActionPanel.ProgressFillName).GetComponent<Image>();
            Assert.That(fill.fillAmount, Is.EqualTo(0f));

            Assert.That(_session.AdvanceOneDay(), Is.True);
            panel.Refresh();
            Assert.That(view.ProgressLine, Does.Contain("已用 1 / 共 3 天"));
            Assert.That(fill.fillAmount, Is.EqualTo(1f / 3f).Within(0.01f));

            Assert.That(panel.ClickInterrupt(), Is.True, panel.FeedbackLine);
            Assert.That(view.ProgressLine, Does.Contain("尚未安排"));
            Assert.That(fill.fillAmount, Is.EqualTo(0f));
        }

                [Test]
        public void PageView_TravelSummaryRequiresAnExplicitDestination()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);
            _ = View();

            Assert.That(_session.IsPersonInTransit(M2FixedScenario.HarunobuId), Is.False);
            Assert.That(panel.ClickTravel(LocationId.Atsuta), Is.True, panel.FeedbackLine);
            Assert.That(_session.IsPersonInTransit(M2FixedScenario.HarunobuId), Is.True);
        }

        [Test]
        public void Panel_ProfileTracksActionProgressDateAndTransit()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.ChieId);
            panel.SetDuration("2");
            Assert.That(panel.ClickRest(), Is.True, panel.FeedbackLine);

            Assert.That(panel.ProfileLine,
                Does.Contain("行动：休息 进度 0/2 天（剩 2）"));

            Assert.That(_session.AdvanceOneDay(), Is.True);
            panel.Refresh();

            Assert.That(panel.ProfileLine,
                Does.Contain("行动：休息 进度 1/2 天（剩 1）"));
            Assert.That(panel.SummaryLine,
                Does.Contain(_session.WorldClock.CurrentDate.ToString()));

            Assert.That(panel.ClickInterrupt(), Is.True, panel.FeedbackLine);
            Assert.That(panel.ProfileLine, Does.Contain("行动：空闲"));
            Assert.That(panel.ClickTravel(LocationId.Atsuta), Is.True, panel.FeedbackLine);
            Assert.That(panel.ProfileLine, Does.Contain("旅途中：清洲→热田"));
        }

        [Test]
        public void Layout_KeepsEveryM02ControlInsideThePanelAt1280x720()
        {
            var canvas = (RectTransform)Find("Canvas");
            var panel = (RectTransform)Find(AddM02DailyActionPanel.PanelName);
            var open = (RectTransform)Find(AddM02DailyActionPanel.OpenButtonName);
            var action = (RectTransform)Find(AddM02DailyActionPanel.ActionPanelName);
            var profile = (RectTransform)Find(AddM02DailyActionPanel.ProfilePanelName);
            Assert.That(Contained(panel, canvas), Is.True);
            Assert.That(Contained(open, canvas), Is.True);
            Assert.That(Contained(action, panel), Is.True);
            Assert.That(Contained(profile, panel), Is.True);

            string[] panelControls =
            {
                AddM02DailyActionPanel.TitleName, AddM02DailyActionPanel.SummaryName,
                AddM02DailyActionPanel.ScrollName, AddM02DailyActionPanel.EmptyName,
                AddM02DailyActionPanel.ProfilePanelName,
                AddM02DailyActionPanel.ActionPanelName,
                AddM02DailyActionPanel.InspectorRailName,
                AddM02DailyActionPanel.HeaderDateName,
                AddM02DailyActionPanel.HeaderMeritName,
                AddM02DailyActionPanel.NavRailName
            };
            foreach (string name in panelControls)
                Assert.That(Contained((RectTransform)Find(name), panel), Is.True, name);

            string[] actionControls =
            {
                AddM02DailyActionPanel.DurationLabelName,
                AddM02DailyActionPanel.DurationInputName,
                AddM02DailyActionPanel.RestButtonName,
                AddM02DailyActionPanel.TrainingButtonName,
                AddM02DailyActionPanel.WorkButtonName,
                AddM02DailyActionPanel.TravelKiyosuButtonName,
                AddM02DailyActionPanel.TravelAtsutaButtonName,
                AddM02DailyActionPanel.TravelNagoyaButtonName,
                AddM02DailyActionPanel.TravelCardName,
                AddM02DailyActionPanel.InterruptButtonName,
                AddM02DailyActionPanel.FeedbackName,
                AddM02DailyActionPanel.CloseButtonName,
                AddM02DailyActionPanel.ScheduleName,
                AddM02DailyActionPanel.TravelInfoName,
                AddM02DailyActionPanel.ProgressFillName
            };
            foreach (string name in actionControls)
                Assert.That(Contained((RectTransform)Find(name), action), Is.True, name);

            string[] profileControls =
            {
                AddM02DailyActionPanel.ProfilePortraitName,
                AddM02DailyActionPanel.ProfileNameName,
                AddM02DailyActionPanel.ProfileAgeName,
                AddM02DailyActionPanel.ProfileStatusName,
                AddM02DailyActionPanel.TraitSummaryName,
                AddM02DailyActionPanel.NinjutsuFillName,
                AddM02DailyActionPanel.SocialFillName,
                AddM02DailyActionPanel.SelectedName,
                AddM02DailyActionPanel.HistoryTextName,
                AddM02DailyActionPanel.RelationRootName + "_1",
                AddM02DailyActionPanel.RelationRootName + "_3"
            };
            foreach (string name in profileControls)
                Assert.That(Contained((RectTransform)Find(name), profile), Is.True, name);

            var scroll = (RectTransform)Find(AddM02DailyActionPanel.ScrollName);
            Assert.That(scroll.anchoredPosition.x + scroll.rect.width,
                Is.LessThanOrEqualTo(profile.anchoredPosition.x),
                "the roster rail and profile column must not overlap");
        }

        [Test]
        public void Panel_RejectsNonPositiveOrNonNumericDurationWithoutPlanning()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);

            panel.SetDuration("0");
            Assert.That(panel.ClickTraining(), Is.False);
            Assert.That(panel.FeedbackLine, Does.Contain("大于 0"));
            Assert.That(_session.HasActiveDailyAction(M2FixedScenario.HarunobuId), Is.False);

            panel.SetDuration("abc");
            Assert.That(panel.ClickWork(), Is.False);
            Assert.That(panel.FeedbackLine, Does.Contain("大于 0"));
            Assert.That(_session.HasActiveDailyAction(M2FixedScenario.HarunobuId), Is.False);
        }

        [Test]
        public void Panel_TravelUsesWorldMapRulesAndShowsInTransitState()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);

            Assert.That(panel.TravelInteractable(LocationId.Kiyosu), Is.False,
                "the current location is not a travel destination");
            Assert.That(panel.TravelInteractable(LocationId.Atsuta), Is.True);
            Assert.That(panel.TravelInteractable(LocationId.Nagoya), Is.True);

            Assert.That(panel.ClickTravel(LocationId.Atsuta), Is.True, panel.FeedbackLine);
            Assert.That(_session.IsPersonInTransit(M2FixedScenario.HarunobuId), Is.True);
            Assert.That(_session.TryGetPersonLocation(M2FixedScenario.HarunobuId, out _), Is.False);
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("旅途中"));
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("清洲→热田"));
            Assert.That(panel.SelectedLine, Does.Contain("旅途中"));
            Assert.That(panel.TravelInteractable(LocationId.Atsuta), Is.False);
            Assert.That(panel.TravelInteractable(LocationId.Nagoya), Is.False);

            Assert.That(_session.AdvanceOneDay(), Is.True);
            panel.Refresh();
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("热田"));
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("空闲"));
            Assert.That(panel.TravelInteractable(LocationId.Nagoya), Is.False,
                "Atsuta and Nagoya have no direct route");
            Assert.That(panel.ClickTravel(LocationId.Nagoya), Is.False);
            Assert.That(panel.FeedbackLine, Does.Contain("没有直达路线"));
        }

        [Test]
        public void Panel_InterruptAndReplanShowExplicitOutcomeFeedback()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);

            panel.SetDuration("3");
            Assert.That(panel.ClickWork(), Is.True, panel.FeedbackLine);
            Assert.That(panel.FeedbackLine, Does.Contain("已安排"));

            Assert.That(panel.ClickInterrupt(), Is.True, panel.FeedbackLine);
            Assert.That(panel.InterruptCount, Is.EqualTo(1));
            Assert.That(panel.FeedbackLine, Does.Contain("已中断"));
            Assert.That(_session.HasActiveDailyAction(M2FixedScenario.HarunobuId), Is.False);
            Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("空闲"));

            panel.SetDuration("2");
            Assert.That(panel.ClickTraining(), Is.True, panel.FeedbackLine);
            panel.SetDuration("1");
            Assert.That(panel.ClickRest(), Is.True, panel.FeedbackLine);
            Assert.That(panel.ReplanCount, Is.EqualTo(1));
            Assert.That(panel.FeedbackLine, Does.Contain("已中断"));
            Assert.That(panel.FeedbackLine, Does.Contain("已安排"));
            Assert.That(panel.FeedbackLine, Does.Contain("重新规划"));
            Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                out DailyActionTask task), Is.True);
            Assert.That(task.Kind, Is.EqualTo(DailyActionKind.Rest));
            Assert.That(task.RemainingDays, Is.EqualTo(1));

            panel.SetDuration("0");
            Assert.That(panel.ClickWork(), Is.False);
            Assert.That(panel.FeedbackLine, Does.Contain("大于 0"));
            Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                out DailyActionTask preserved), Is.True);
            Assert.That(preserved.Kind, Is.EqualTo(DailyActionKind.Rest),
                "a rejected replan must keep the original action");
        }

        [Test]
        public void ClockPanel_AdvanceUsesDailyActionDayAndMonthSettlementExactlyOnce()
        {
            Assert.That(_session.TryAssignIndustry(IndustryCatalog.FarmId,
                M2FixedScenario.HarunobuId).Succeeded, Is.True);
            Assert.That(_session.AdvanceDays(28), Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 29)));

            DailyActionPanel panel = Panel();
            panel.Open();
            panel.SelectPerson(M2FixedScenario.HarunobuId);
            panel.SetDuration("4");
            Assert.That(panel.ClickRest(), Is.True, panel.FeedbackLine);

            var clockObject = new GameObject("WorldClockPanel_M02Test");
            try
            {
                WorldClockPanel clock = clockObject.AddComponent<WorldClockPanel>();
                clock.SetDependencies(_session, null, null);
                clock.BindDailyActionPanel(panel);

                Assert.That(clock.TryAdvanceOneDay(), Is.True, clock.LastAdvanceMessage);
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 4, 30)));
                Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                    out DailyActionTask task), Is.True);
                Assert.That(task.RemainingDays, Is.EqualTo(3));
                Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("剩 3"));

                Assert.That(clock.TryAdvanceOneDay(), Is.True, clock.LastAdvanceMessage);
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 1)));
                Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                    out task), Is.True);
                Assert.That(task.RemainingDays, Is.EqualTo(2),
                    "the month boundary must consume exactly one action day");
                Assert.That(panel.RowTextFor(M2FixedScenario.HarunobuId), Does.Contain("剩 2"));
                Assert.That(_session.Industry.LastSettledMonthIndex,
                    Is.EqualTo(IndustryCalendar.MonthIndex(new GameDate(1560, 5, 1)) - 1));
                Assert.That(_session.LastIndustryReport, Is.Not.Null);
                Assert.That(_session.LastIndustryReport.Month, Is.EqualTo(4));
                int fundsAfterSettlement = _session.Industry.Resources.Funds;

                if (!string.IsNullOrEmpty(_session.PendingEventId))
                {
                    string pending = _session.PendingEventId;
                    EventChoiceResult choice = _session.TryChooseEventOption(pending,
                        M2FixedScenario.KeepPromiseOptionId,
                        _session.CreateEventResultId(pending,
                            M2FixedScenario.KeepPromiseOptionId));
                    Assert.That(choice.Succeeded, Is.True, choice.Message);
                }

                Assert.That(clock.TryAdvanceOneDay(), Is.True, clock.LastAdvanceMessage);
                Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(new GameDate(1560, 5, 2)));
                Assert.That(_session.TryGetDailyAction(M2FixedScenario.HarunobuId,
                    out task), Is.True);
                Assert.That(task.RemainingDays, Is.EqualTo(1));
                Assert.That(_session.Industry.Resources.Funds, Is.EqualTo(fundsAfterSettlement),
                    "a day inside the new month must not repeat the previous settlement");
                Assert.That(_session.LastIndustryReport.Month, Is.EqualTo(4));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clockObject);
            }
        }

        private GameObject Create(string name, Transform parent, params Type[] components) =>
            CreateIn(_scene, name, parent, components);

        private static GameObject CreateIn(Scene scene, string name, Transform parent,
            params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (Type component in components)
                go.AddComponent(component);
            return go;
        }

        private Transform Find(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .SingleOrDefault(transform => transform.name == name);

        private Transform[] FindAll(string name) =>
            _scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name).ToArray();

        private static Transform[] FindAllIn(Scene scene, string name) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Where(transform => transform.name == name).ToArray();

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(corner =>
            {
                Vector3 point = parent.InverseTransformPoint(corner);
                return point.x >= bounds.xMin - 0.5f &&
                       point.x <= bounds.xMax + 0.5f &&
                       point.y >= bounds.yMin - 0.5f &&
                       point.y <= bounds.yMax + 0.5f;
            });
        }
    }
}
