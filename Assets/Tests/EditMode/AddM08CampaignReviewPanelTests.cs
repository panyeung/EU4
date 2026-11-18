using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Events;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Structural contract tests for the M08 campaign-review generator and validator.
    /// Every fixture is an isolated preview scene, so the user's FoundationScene is never
    /// saved, replaced or reloaded.
    /// </summary>
    public sealed class AddM08CampaignReviewPanelTests
    {
        private Scene scene;
        private Scene previousScene;
        private bool previousDirty;
        private RectTransform canvas;
        private GameSession session;
        private EventSystem manuallyRegisteredEventSystem;

        [SetUp]
        public void SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            previousDirty = previousScene.isDirty;
            scene = EditorSceneManager.NewPreviewScene();

            GameObject canvasObject = Create("Canvas", null, typeof(Canvas), typeof(CanvasScaler));
            canvas = canvasObject.GetComponent<RectTransform>();
            canvas.sizeDelta = new Vector2(1280f, 720f);
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Create("M2WorldMapRoot", canvas, typeof(WorldMapController));
            session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            session.InitNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            if (manuallyRegisteredEventSystem != null &&
                EventSystem.current == manuallyRegisteredEventSystem)
                typeof(EventSystem).GetMethod("OnDisable",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(manuallyRegisteredEventSystem, null);
            if (scene.IsValid())
                EditorSceneManager.ClosePreviewScene(scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(previousScene));
            Assert.That(previousScene.isDirty, Is.EqualTo(previousDirty),
                "Tests must preserve the user's active scene.");
        }

        [Test]
        public void MenuCommand_IsRegistered()
        {
            MethodInfo run = typeof(AddM08CampaignReviewPanel).GetMethod("RunMenu",
                BindingFlags.Public | BindingFlags.Static);
            Assert.That(run, Is.Not.Null);
            Assert.That(run.GetCustomAttributes(typeof(MenuItem), false), Has.Length.EqualTo(1));
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            int count = ObjectCount();
            Assert.That(AddM08CampaignReviewPanel.CheckScenePreflight(
                    "Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.SceneMismatch));
            Assert.That(AddM08CampaignReviewPanel.CheckScenePreflight(
                    AddM08CampaignReviewPanel.ScenePath, true),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.SceneDirty));
            Assert.That(AddM08CampaignReviewPanel.CheckScenePreflight(
                    AddM08CampaignReviewPanel.ScenePath, false),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [TestCase("Canvas", false)]
        [TestCase("Canvas", true)]
        [TestCase("M2WorldMapRoot", false)]
        [TestCase("M2WorldMapRoot", true)]
        [TestCase("GameSession", false)]
        [TestCase("GameSession", true)]
        public void Updater_RefusesMissingOrDuplicateRoots(string name, bool duplicate)
        {
            Transform target = All().Single(t => t.name == name);
            if (duplicate)
            {
                if (name == "GameSession")
                    Create(name, null, typeof(GameSession));
                else
                    Create(name, null);
            }
            else
                Object.DestroyImmediate(target.gameObject);
            int count = ObjectCount();
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.MissingOrDuplicateObjects));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [Test]
        public void Updater_BuildsBindsAndUndoesTheWholeOverlay()
        {
            int count = ObjectCount();
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out _), Is.False);

            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));

            Transform root = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(canvas));
            CampaignReviewPanel panel = root.GetComponent<CampaignReviewPanel>();
            Assert.That(panel, Is.Not.Null);
            Transform page = root.Find(AddM08CampaignReviewPanel.PageName);
            Assert.That(page, Is.Not.Null);
            Assert.That(page.gameObject.activeSelf, Is.False, "The overlay must start hidden.");
            Assert.That(panel.IsBound, Is.True);
            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(1));
            Assert.That(ObjectCount(), Is.EqualTo(count + 15));
            Assert.That(GetReference(panel, "gameSession"), Is.EqualTo(session));
            Assert.That(GetReference(panel, "panelRoot"), Is.EqualTo(page.gameObject));
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string error), Is.True,
                error);
            Assert.That(scene.path, Is.Empty, "The public updater must never save the test scene.");

            int builtCount = ObjectCount();
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount), "A repeat update must be a no-op.");

            Undo.PerformUndo();
            Assert.That(ObjectCount(), Is.EqualTo(count),
                "One Undo must remove the entire generated hierarchy.");
            AssertNoPanel();
        }

        [TestCase("reference")]
        [TestCase("exception")]
        [TestCase("geometry")]
        public void Updater_RollsBackOnValidationFailure(string fault)
        {
            int count = ObjectCount();
            var result = AddM08CampaignReviewPanel.Update(scene, root =>
            {
                if (fault == "exception")
                    throw new InvalidOperationException("Injected construction failure");
                if (fault == "geometry")
                    root.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
                else
                    SetReference(root.GetComponent<CampaignReviewPanel>(), "titleText", null);
            });

            Assert.That(result, Is.EqualTo(fault == "exception"
                ? AddM08CampaignReviewPanel.UpdateResult.Failed
                : AddM08CampaignReviewPanel.UpdateResult.ValidationFailed));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
            Assert.That(scene.path, Is.Empty,
                "Rollback must not save an unvalidated result to clear the dirty flag.");
        }

        [Test]
        public void Updater_RefusesExistingCampaignObjects()
        {
            GameObject existing = Create("OtherCampaignHost", canvas);
            existing.AddComponent<CampaignReviewPanel>();
            int count = ObjectCount();
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.ExistingCampaignPanel));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Object.DestroyImmediate(existing);

            Create(AddM08CampaignReviewPanel.RootName, canvas);
            count = ObjectCount();
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.ExistingCampaignPanel));
            Assert.That(ObjectCount(), Is.EqualTo(count));
        }

        [Test]
        public void Updater_RefusesInsufficientCanvasSpace()
        {
            canvas.sizeDelta = new Vector2(800f, 600f);
            int count = ObjectCount();
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.InsufficientSpace));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            AssertNoPanel();
        }

        [Test]
        public void Validator_RejectsMissingDuplicateAndExternalReferences()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Transform root = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Transform page = root.Find(AddM08CampaignReviewPanel.PageName);
            CampaignReviewPanel panel = root.GetComponent<CampaignReviewPanel>();

            SetReference(panel, "titleText",
                Create("ExternalText", null, typeof(Text)).GetComponent<Text>());
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out _), Is.False,
                "An external title reference must be rejected.");
            SetReference(panel, "titleText",
                FindDeep(page, AddM08CampaignReviewPanel.TitleTextName).GetComponent<Text>());
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string restored), Is.True,
                restored);

            Text label = FindDeep(page, AddM08CampaignReviewPanel.ContinueLabelName)
                .GetComponent<Text>();
            string original = label.text;
            label.text = string.Empty;
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out _), Is.False,
                "An empty continue label must be rejected.");
            label.text = original;
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out _), Is.True);

            Create(AddM08CampaignReviewPanel.RootName, canvas);
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out _), Is.False,
                "A duplicate root must be rejected.");
        }

        [Test]
        public void GeneratedOverlay_IsReadableAndScrollableAtReferenceSize()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Transform root = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Transform page = root.Find(AddM08CampaignReviewPanel.PageName);
            Transform card = page.Find(AddM08CampaignReviewPanel.CardName);
            Text title = FindDeep(page, AddM08CampaignReviewPanel.TitleTextName)
                .GetComponent<Text>();
            Text body = FindDeep(page, AddM08CampaignReviewPanel.BodyTextName)
                .GetComponent<Text>();
            Button cont = FindDeep(page, AddM08CampaignReviewPanel.ContinueButtonName)
                .GetComponent<Button>();
            ScrollRect scroll = FindDeep(page, AddM08CampaignReviewPanel.BodyScrollName)
                .GetComponent<ScrollRect>();

            Assert.That(Contained((RectTransform)card, (RectTransform)root), Is.True);
            Assert.That(title.fontSize, Is.GreaterThanOrEqualTo(24));
            Assert.That(body.fontSize, Is.GreaterThanOrEqualTo(15));
            Assert.That(body.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow));
            Assert.That(scroll.vertical, Is.True);
            Assert.That(scroll.horizontal, Is.False);
            Assert.That(((RectTransform)card).rect.width, Is.LessThanOrEqualTo(1280f));
            Assert.That(cont.interactable, Is.True);
        }

        [Test]
        public void GeneratedOverlay_HasStartNewScenarioEntry()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Transform page = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .Find(AddM08CampaignReviewPanel.PageName);
            Button startNew = FindDeep(page, "M08CampaignReviewNewScenarioButton")
                .GetComponent<Button>();

            Assert.That(startNew, Is.Not.Null);
            Assert.That(startNew.interactable, Is.True);
            Assert.That(startNew.GetComponentInChildren<Text>(true).text, Is.EqualTo("开始新剧本"));
        }

        [Test]
        public void GeneratedOverlay_StatusTextMeetsNormalTextContrast()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Transform page = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .Find(AddM08CampaignReviewPanel.PageName);
            Image card = page.Find(AddM08CampaignReviewPanel.CardName).GetComponent<Image>();
            Text status = FindDeep(page, AddM08CampaignReviewPanel.StatusTextName)
                .GetComponent<Text>();

            Assert.That(ContrastRatio(status.color, card.color), Is.GreaterThanOrEqualTo(4.5f));
        }

        [Test]
        public void GeneratedOverlay_FitsANarrowerCanvas()
        {
            canvas.sizeDelta = new Vector2(1024f, 576f);
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Transform root = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Transform card = root.Find(AddM08CampaignReviewPanel.PageName)
                .Find(AddM08CampaignReviewPanel.CardName);
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string error), Is.True, error);
            Assert.That(((RectTransform)card).rect.width, Is.LessThanOrEqualTo(1024f));
            Assert.That(((RectTransform)card).rect.height, Is.LessThanOrEqualTo(576f));
        }

        [Test]
        public void Panel_RefitsTheOpenCardAfterTheCanvasResizes()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            CampaignReviewPanel panel = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .GetComponent<CampaignReviewPanel>();
            RectTransform page = (RectTransform)canvas.Find(AddM08CampaignReviewPanel.RootName)
                .Find(AddM08CampaignReviewPanel.PageName);
            RectTransform card = (RectTransform)page.Find(AddM08CampaignReviewPanel.CardName);
            int days = DaysBetween(session.WorldClock.CurrentDate, GameDate.Parse(1615, 4, 1));
            Assert.That(session.AdvanceDaysForLegacyFixtureSetup(days), Is.True);
            panel.Refresh();
            Assert.That(panel.IsVisible, Is.True);

            canvas.sizeDelta = new Vector2(1024f, 576f);
            Canvas.ForceUpdateCanvases();
            InvokeLateUpdate(panel);
            Canvas.ForceUpdateCanvases();

            Assert.That(panel.CardScale, Is.LessThan(1f));
            Assert.That(card.rect.width * panel.CardScale,
                Is.LessThanOrEqualTo(page.rect.width - CampaignReviewPanel.PageSafeMargin * 2f + 1f));
            Assert.That(card.rect.height * panel.CardScale,
                Is.LessThanOrEqualTo(page.rect.height - CampaignReviewPanel.PageSafeMargin * 2f + 1f));
        }

        [Test]
        public void Panel_ContinueFocusIsVisibleAndRestoredWhenTheReviewCloses()
        {
            EventSystem eventSystem = CreateTestEventSystem();
            Assert.That(eventSystem, Is.Not.Null,
                "the focus test needs the EventSystem Unity routes UI selection through");
            GameObject selectionBeforeTest = eventSystem.currentSelectedGameObject;
            Button previousSelection = Create("PreviousSelection", canvas,
                typeof(Image), typeof(Button)).GetComponent<Button>();
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            CampaignReviewPanel panel = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .GetComponent<CampaignReviewPanel>();
            Button continueButton = FindDeep(canvas, AddM08CampaignReviewPanel.ContinueButtonName)
                .GetComponent<Button>();
            Image continueImage = continueButton.targetGraphic as Image;
            Text continueLabel = continueButton.GetComponentInChildren<Text>(true);
            Color authoredBackground = continueImage.color;
            Color authoredForeground = continueLabel.color;
            Navigation authoredNavigation = continueButton.navigation;
            try
            {
                eventSystem.SetSelectedGameObject(previousSelection.gameObject);
                int days = DaysBetween(session.WorldClock.CurrentDate,
                    GameDate.Parse(1615, 4, 1));
                Assert.That(session.AdvanceDaysForLegacyFixtureSetup(days), Is.True);
                panel.Refresh();

                Assert.That(eventSystem.currentSelectedGameObject,
                    Is.EqualTo(continueButton.gameObject));
                Assert.That(continueImage.color,
                    Is.EqualTo(CampaignReviewPanel.FocusedButtonBackground));
                Assert.That(continueLabel.color,
                    Is.EqualTo(CampaignReviewPanel.FocusedButtonForeground));
                Assert.That(continueButton.navigation.mode, Is.EqualTo(Navigation.Mode.None));

                // Reapplying focus presentation must work even when Unity already holds this
                // button as the selected object.
                continueImage.color = authoredBackground;
                continueLabel.color = authoredForeground;
                continueButton.navigation = authoredNavigation;
                panel.Refresh();
                Assert.That(continueImage.color,
                    Is.EqualTo(CampaignReviewPanel.FocusedButtonBackground));
                Assert.That(continueLabel.color,
                    Is.EqualTo(CampaignReviewPanel.FocusedButtonForeground));
                Assert.That(continueButton.navigation.mode, Is.EqualTo(Navigation.Mode.None));

                panel.Continue();

                Assert.That(eventSystem.currentSelectedGameObject,
                    Is.EqualTo(previousSelection.gameObject));
                Assert.That(continueImage.color, Is.EqualTo(authoredBackground));
                Assert.That(continueLabel.color, Is.EqualTo(authoredForeground));
                Assert.That(continueButton.navigation, Is.EqualTo(authoredNavigation));
            }
            finally
            {
                eventSystem.SetSelectedGameObject(selectionBeforeTest);
            }
        }

        [Test]
        public void Panel_RecapturesClearedFocusAndYieldsToPendingEventOverlay()
        {
            EventSystem eventSystem = CreateTestEventSystem();
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(EventSystem.current, Is.SameAs(eventSystem),
                "the focus assertions must target the active EventSystem");
            GameObject selectionBeforeTest = eventSystem.currentSelectedGameObject;
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            CampaignReviewPanel panel = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .GetComponent<CampaignReviewPanel>();
            Button continueButton = FindDeep(canvas, AddM08CampaignReviewPanel.ContinueButtonName)
                .GetComponent<Button>();
            Image continueImage = continueButton.targetGraphic as Image;
            Text continueLabel = continueButton.GetComponentInChildren<Text>(true);
            Color authoredBackground = continueImage.color;
            Color authoredForeground = continueLabel.color;
            Navigation authoredNavigation = continueButton.navigation;
            var eventField = typeof(GameSession).GetField("_eventRuntimeState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var pendingIdsField = typeof(EventRuntimeState).GetField("_pendingEventIds",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(eventField, Is.Not.Null);
            Assert.That(pendingIdsField, Is.Not.Null);
            var runtime = (EventRuntimeState)eventField.GetValue(session);
            var pendingIds = (System.Collections.Generic.List<string>)pendingIdsField.GetValue(runtime);

            try
            {
                int days = DaysBetween(session.WorldClock.CurrentDate,
                    GameDate.Parse(1615, 4, 1));
                Assert.That(session.AdvanceDaysForLegacyFixtureSetup(days), Is.True);
                panel.Refresh();
                Assert.That(eventSystem.currentSelectedGameObject,
                    Is.EqualTo(continueButton.gameObject));

                eventSystem.SetSelectedGameObject(null);
                InvokeLateUpdate(panel);
                Assert.That(eventSystem.currentSelectedGameObject,
                    Is.EqualTo(continueButton.gameObject),
                    "the open review must reclaim cleared keyboard focus");

                pendingIds.Add("test.pending-event");
                InvokeLateUpdate(panel);
                Assert.That(eventSystem.currentSelectedGameObject, Is.Null,
                    "a pending event choice owns focus ahead of the campaign review");
                Assert.That(continueImage.color, Is.EqualTo(authoredBackground));
                Assert.That(continueLabel.color, Is.EqualTo(authoredForeground));
                Assert.That(continueButton.navigation, Is.EqualTo(authoredNavigation));

                pendingIds.Clear();
                InvokeLateUpdate(panel);
                SuccessionPhase[] urgentSuccessionPhases =
                {
                    SuccessionPhase.AwaitingSuccessionChoice,
                    SuccessionPhase.AwaitingRegentChoice,
                    SuccessionPhase.CrisisDecision
                };
                foreach (SuccessionPhase phase in urgentSuccessionPhases)
                {
                    session.Succession.Phase = phase;
                    eventSystem.SetSelectedGameObject(continueButton.gameObject);
                    InvokeLateUpdate(panel);
                    Assert.That(eventSystem.currentSelectedGameObject, Is.Null,
                        phase + " owns focus ahead of the campaign review");
                    Assert.That(continueImage.color, Is.EqualTo(authoredBackground),
                        phase + " clears the review's focused presentation");
                    Assert.That(continueLabel.color, Is.EqualTo(authoredForeground),
                        phase + " restores the authored label presentation");
                    Assert.That(continueButton.navigation, Is.EqualTo(authoredNavigation),
                        phase + " releases the review's navigation trap");
                }

                session.Succession.Phase = SuccessionPhase.None;
                InvokeLateUpdate(panel);
                Assert.That(eventSystem.currentSelectedGameObject,
                    Is.EqualTo(continueButton.gameObject));
                Assert.That(continueImage.color,
                    Is.EqualTo(CampaignReviewPanel.FocusedButtonBackground));
                Assert.That(continueLabel.color,
                    Is.EqualTo(CampaignReviewPanel.FocusedButtonForeground));
            }
            finally
            {
                pendingIds.Clear();
                session.Succession.Phase = SuccessionPhase.None;
                eventSystem.SetSelectedGameObject(selectionBeforeTest);
            }
        }

        [Test]
        public void Updater_KeepsTheContinueButtonInsideTheCardWhenTheCanvasOriginIsOutsideIt()
        {
            // FoundationScene's Canvas has pivot (0,0) at the world origin, so the centred
            // card's rect starts at (80,40) instead of covering the origin. A child created at
            // the world origin must not keep that point as its local position, because the
            // continue button is validated before any canvas layout pass runs.
            canvas.pivot = Vector2.zero;
            canvas.anchorMin = canvas.anchorMax = Vector2.zero;
            canvas.anchoredPosition = Vector2.zero;

            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string error), Is.True, error);

            Transform page = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .Find(AddM08CampaignReviewPanel.PageName);
            var card = (RectTransform)page.Find(AddM08CampaignReviewPanel.CardName);
            var cont = (RectTransform)FindDeep(page,
                AddM08CampaignReviewPanel.ContinueButtonName);
            Assert.That(Contained(cont, card), Is.True,
                "the continue button must sit inside the card before a layout pass");
        }

        [Test]
        public void Panel_RevealsAtMilestoneAndContinueAcknowledgesOnce()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Transform root = canvas.Find(AddM08CampaignReviewPanel.RootName);
            CampaignReviewPanel panel = root.GetComponent<CampaignReviewPanel>();
            panel.Refresh();
            Assert.That(panel.IsVisible, Is.False);

            int days = DaysBetween(session.WorldClock.CurrentDate,
                GameDate.Parse(1615, 4, 1));
            Assert.That(days, Is.GreaterThan(0));
            Assert.That(session.AdvanceDaysForLegacyFixtureSetup(days), Is.True);
            panel.Refresh();
            Assert.That(panel.IsVisible, Is.True);
            Assert.That(panel.Title, Does.Contain("阶段评价"));
            string categoryHeader = "【" + CampaignReviewView.CategoryName(
                CampaignHistoryCategory.Rulers) + "】";
            Assert.That(panel.Body, Does.Contain(categoryHeader),
                "the body presents the GDD categories directly");
            Assert.That(panel.Body.Split(new[] { categoryHeader }, StringSplitOptions.None),
                Has.Length.EqualTo(2), "each category is presented exactly once");
            Assert.That(panel.Body, Does.Contain("家族最终地位"));
            Assert.That(panel.Body, Does.Contain("五项发展维度"));
            Assert.That(panel.ContinueLabel, Does.Contain("继续家族"));

            panel.Continue();
            Assert.That(panel.IsVisible, Is.False);
            Assert.That(session.PendingCampaignReview,
                Is.EqualTo(CampaignReviewMilestone.None));
            Assert.That(session.CampaignState.StageReviewAcknowledged, Is.True);
            Assert.That(panel.StaticButtonBindingCount, Is.EqualTo(1),
                "Acknowledging must not duplicate the continue listener.");
        }

        [Test]
        public void Updater_InsertsReviewRootBelowTitleAndTacticalOverlayKeepsLast()
        {
            Transform dailyAction = Create(CanvasOverlayOrder.DailyActionRootName, canvas).transform;
            Transform inspector = Create(CanvasOverlayOrder.InspectorRootName, canvas).transform;
            BuildM6TitleAndTacticalLayers();
            Assert.That(AddM6TitleScreen.Update(scene),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(AddM6TitleScreen.Validate(scene, out string titleBefore), Is.True,
                titleBefore);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string tacticalBefore),
                Is.True, tacticalBefore);

            int eventIndex = canvas.Find(AddM08CampaignReviewPanel.EventChoiceRootName)
                .GetSiblingIndex();
            int titleIndex = canvas.Find(AddM08CampaignReviewPanel.TitleRootName).GetSiblingIndex();
            Assert.That(titleIndex, Is.EqualTo(eventIndex - 1),
                "The fixture must start with the M6 title immediately below EventChoiceRoot.");

            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));

            Transform root = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Transform title = canvas.Find(AddM08CampaignReviewPanel.TitleRootName);
            Transform eventRoot = canvas.Find(AddM08CampaignReviewPanel.EventChoiceRootName);
            Transform tactical = canvas.Find(AddM08CampaignReviewPanel.TacticalOverlayName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(dailyAction.GetSiblingIndex()));
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(inspector.GetSiblingIndex()));
            Assert.That(root.GetSiblingIndex(), Is.LessThan(title.GetSiblingIndex()),
                "M08CampaignReviewRoot must render below the full-screen M6 title overlay.");
            Assert.That(title.GetSiblingIndex(), Is.EqualTo(eventRoot.GetSiblingIndex() - 1),
                "The M6 title must stay immediately below EventChoiceRoot.");
            Assert.That(tactical, Is.Not.Null);
            Assert.That(tactical.GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1),
                "The M06 tactical overlay must stay the last Canvas child.");
            Assert.That(root.GetSiblingIndex(), Is.LessThan(tactical.GetSiblingIndex()));

            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string error), Is.True, error);
            Assert.That(AddM6TitleScreen.Validate(scene, out string titleAfter), Is.True, titleAfter);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string tacticalAfter),
                Is.True, tacticalAfter);

            // A root appended back above the title must be rejected, not silently accepted.
            int generatedIndex = root.GetSiblingIndex();
            root.SetSiblingIndex(title.GetSiblingIndex() + 1);
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string defect), Is.False);
            Assert.That(defect, Does.Contain("M6 title overlay"));
            root.SetSiblingIndex(generatedIndex);
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string restored), Is.True,
                restored);

            Undo.PerformUndo();
            Assert.That(canvas.Find(AddM08CampaignReviewPanel.RootName), Is.Null);
            Assert.That(canvas.Find(AddM08CampaignReviewPanel.TitleRootName).GetSiblingIndex(),
                Is.EqualTo(titleIndex), "Undo must restore the title's original sibling order.");
        }

        [Test]
        public void CanvasOverlayOrder_OrdersScrambledRootsAndPreservesUnrankedSlots()
        {
            RectTransform testCanvas = Create("CanvasOverlayOrderTestCanvas", null, typeof(Canvas))
                .GetComponent<RectTransform>();
            string[] initialOrder =
            {
                CanvasOverlayOrder.TacticalOverlayName,
                "UnrankedA",
                CanvasOverlayOrder.CampaignReviewRootName,
                AddSC010SuccessionPanel.RootName,
                CanvasOverlayOrder.TitleRootName,
                "UnrankedB",
                CanvasOverlayOrder.DailyActionRootName,
                CanvasOverlayOrder.EventChoiceRootName,
                CanvasOverlayOrder.InspectorRootName,
                "UnrankedC"
            };
            foreach (string name in initialOrder)
                Create(name, testCanvas);

            Transform[] nonRankedAtStart = testCanvas.Cast<Transform>()
                .Where(child => !CanvasOverlayOrder.IsRanked(child)).ToArray();
            int[] nonRankedSlots = Enumerable.Range(0, testCanvas.childCount)
                .Where(index => !CanvasOverlayOrder.IsRanked(testCanvas.GetChild(index)))
                .ToArray();

            CanvasOverlayOrder.OrderRankedBand(testCanvas);

            Transform[] ranked = testCanvas.Cast<Transform>()
                .Where(CanvasOverlayOrder.IsRanked).ToArray();
            Assert.That(ranked.Select(child => CanvasOverlayOrder.RankOf(child.name)),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4, 5, 6 }));
            Assert.That(CanvasOverlayOrder.RankOf(AddSC010SuccessionPanel.RootName), Is.EqualTo(3),
                "a pending succession decision must render above the campaign review");
            for (int i = 0; i < nonRankedSlots.Length; i++)
                Assert.That(testCanvas.GetChild(nonRankedSlots[i]), Is.SameAs(nonRankedAtStart[i]),
                    "normalizing the band must leave every unranked root in its slot");
            Assert.That(CanvasOverlayOrder.ValidateBand(testCanvas, out string validError),
                Is.True, validError);

            Transform[] normalized = testCanvas.Cast<Transform>().ToArray();
            CanvasOverlayOrder.OrderRankedBand(testCanvas);
            Assert.That(testCanvas.Cast<Transform>().ToArray(), Is.EqualTo(normalized),
                "normalizing a valid band a second time is idempotent");

            Transform daily = testCanvas.Find(CanvasOverlayOrder.DailyActionRootName);
            Transform inspector = testCanvas.Find(CanvasOverlayOrder.InspectorRootName);
            daily.SetSiblingIndex(inspector.GetSiblingIndex());
            Assert.That(CanvasOverlayOrder.ValidateBand(testCanvas, out string invalidError),
                Is.False);
            Assert.That(invalidError, Does.StartWith(CanvasOverlayOrder.RankOrderError));
            Assert.That(invalidError, Does.Contain(daily.name));
            Assert.That(invalidError, Does.Contain(inspector.name));
        }

        [Test]
        public void CanvasOverlayOrder_PlaceAtRankMovesOnlyTheTarget()
        {
            RectTransform testCanvas = Create("CanvasOverlayPlaceTestCanvas", null, typeof(Canvas))
                .GetComponent<RectTransform>();
            Create("UnrankedBefore", testCanvas);
            Create(CanvasOverlayOrder.DailyActionRootName, testCanvas);
            Create(CanvasOverlayOrder.InspectorRootName, testCanvas);
            Create(CanvasOverlayOrder.TitleRootName, testCanvas);
            Create(CanvasOverlayOrder.EventChoiceRootName, testCanvas);
            Create(CanvasOverlayOrder.TacticalOverlayName, testCanvas);
            Create("UnrankedAfter", testCanvas);
            Transform review = Create(CanvasOverlayOrder.CampaignReviewRootName, testCanvas).transform;
            Transform[] originalOthers = testCanvas.Cast<Transform>()
                .Where(child => child != review).ToArray();

            CanvasOverlayOrder.PlaceAtRank(testCanvas, review);

            Assert.That(review.GetSiblingIndex(), Is.EqualTo(
                testCanvas.Find(CanvasOverlayOrder.TitleRootName).GetSiblingIndex() - 1));
            Assert.That(testCanvas.Cast<Transform>().Where(child => child != review).ToArray(),
                Is.EqualTo(originalOthers),
                "placing one root must preserve the relative order of every other Canvas child");
            Assert.That(CanvasOverlayOrder.ValidateBand(testCanvas, out string error), Is.True, error);
        }

        [Test]
        public void Updater_RepairsOnlyTheCanvasOrderOfAnExistingPanel()
        {
            BuildM6TitleAndTacticalLayers();
            Assert.That(AddM6TitleScreen.Update(scene),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));

            Transform root = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Transform title = canvas.Find(AddM08CampaignReviewPanel.TitleRootName);
            Transform tactical = canvas.Find(AddM08CampaignReviewPanel.TacticalOverlayName);
            int count = ObjectCount();
            int titleIndex = title.GetSiblingIndex();

            tactical.SetSiblingIndex(title.GetSiblingIndex());
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string tacticalLastError),
                Is.False);
            Assert.That(tacticalLastError, Does.Contain("M06 tactical overlay"));
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Assert.That(tactical.GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1));
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string repairedError),
                Is.True, repairedError);

            // The defect the older revision saved: the review root sits last, above both the
            // full-screen title overlay and the tactical modal.
            root.SetAsLastSibling();
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out _), Is.False);

            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(count),
                "A repair must move the page, not rebuild it.");
            Assert.That(root.GetSiblingIndex(), Is.EqualTo(titleIndex - 1),
                "Repairing must put the review root directly below the title.");
            Assert.That(title.GetSiblingIndex(), Is.EqualTo(titleIndex),
                "Repairing must return the title to its original index.");
            Assert.That(canvas.Find(AddM08CampaignReviewPanel.TacticalOverlayName)
                    .GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1),
                "The tactical overlay must stay last after a repair.");
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string error), Is.True, error);
            Assert.That(AddM6TitleScreen.Validate(scene, out string titleError), Is.True, titleError);
            Assert.That(AddM06TacticalMissionPanel.Validate(scene, out string tacticalError),
                Is.True, tacticalError);

            // A structurally broken page with a wrong sibling index is still refused
            // untouched: the repair rolls back and leaves the order it found.
            Transform viewport = FindDeep(root, AddM08CampaignReviewPanel.BodyViewportName);
            Object.DestroyImmediate(viewport.GetComponent<RectMask2D>());
            root.SetAsLastSibling();
            int brokenTitleIndex = title.GetSiblingIndex();
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.ExistingCampaignPanel));
            Assert.That(root.GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1),
                "A failed repair must leave the sibling order it found.");
            Assert.That(title.GetSiblingIndex(), Is.EqualTo(brokenTitleIndex),
                "A failed repair must not shift the title overlay.");

            // A defect reported before the ordering check is refused without any move.
            ((RectTransform)root).anchorMin = new Vector2(0.5f, 0.5f);
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.ExistingCampaignPanel));
            Assert.That(root.GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1),
                "A non-order defect must not move the review root.");
        }

        [Test]
        public void Updater_RepairsReviewBelowTheDailyActionPageAndInspector()
        {
            Transform dailyAction = Create(CanvasOverlayOrder.DailyActionRootName, canvas).transform;
            Transform inspector = Create(CanvasOverlayOrder.InspectorRootName, canvas).transform;
            Transform succession = Create(AddSC010SuccessionPanel.RootName, canvas).transform;
            BuildM6TitleAndTacticalLayers();
            Assert.That(AddM6TitleScreen.Update(scene),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Assert.That(AddM06TacticalMissionPanel.Update(scene),
                Is.EqualTo(AddM06TacticalMissionPanel.UpdateResult.Success));
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));

            Transform review = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Assert.That(succession.GetSiblingIndex(), Is.GreaterThan(review.GetSiblingIndex()),
                "pending succession controls must render above the full-screen campaign review");
            Assert.That(CanvasOverlayOrder.ValidateBand(canvas, out string initialBandError),
                Is.True, initialBandError);
            review.SetSiblingIndex(dailyAction.GetSiblingIndex());
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string belowDailyAction),
                Is.False);
            Assert.That(belowDailyAction,
                Does.Contain(CanvasOverlayOrder.CampaignReviewAboveDailyActionError));
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Assert.That(review.GetSiblingIndex(), Is.GreaterThan(dailyAction.GetSiblingIndex()));

            review.SetSiblingIndex(inspector.GetSiblingIndex());
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string belowInspector),
                Is.False);
            Assert.That(belowInspector,
                Does.Contain(CanvasOverlayOrder.CampaignReviewAboveInspectorError));
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            Assert.That(review.GetSiblingIndex(), Is.GreaterThan(inspector.GetSiblingIndex()));
            Assert.That(succession.GetSiblingIndex(), Is.GreaterThan(review.GetSiblingIndex()),
                "repairing the review must preserve succession priority");
            Assert.That(CanvasOverlayOrder.ValidateBand(canvas, out string bandError), Is.True,
                bandError);
        }

        [Test]
        public void Updater_MigratesTheLegacyGeneratedPageWithoutTouchingUnrelatedObjects()
        {
            GameObject decoy = Create("UnrelatedHudNote", canvas, typeof(Image));
            GameObject legacyRoot = BuildLegacyPanel(canvas);
            GameObject hudAfterPanel = Create("UnrelatedHudPanel", canvas, typeof(Image));
            int count = ObjectCount();
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string legacyError), Is.False);
            Assert.That(legacyError, Does.Contain("new-scenario"),
                "the fixture must reproduce the page saved before the new-scenario action");

            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));

            Transform root = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.gameObject, Is.Not.SameAs(legacyRoot),
                "the legacy page is replaced by one complete generated page");
            Assert.That(ObjectCount(), Is.EqualTo(count + 2),
                "the upgrade adds only the new-scenario button and its label");
            Transform startNew = FindDeep(root, AddM08CampaignReviewPanel.NewScenarioButtonName);
            Assert.That(startNew, Is.Not.Null);
            Assert.That(startNew.GetComponent<Button>().interactable, Is.True);
            Assert.That(startNew.GetComponentInChildren<Text>(true).text,
                Is.EqualTo(CampaignReviewPanel.NewScenarioLabel));
            Text status = FindDeep(root, AddM08CampaignReviewPanel.StatusTextName)
                .GetComponent<Text>();
            Assert.That(status.color, Is.EqualTo(CampaignReviewPanel.StatusTextColor),
                "the migrated status line must use the readable ink");
            Assert.That(decoy.transform.parent, Is.EqualTo(canvas));
            Assert.That(All(), Does.Contain(decoy.transform),
                "an unrelated Canvas object must survive the migration");
            Assert.That(All(), Does.Contain(hudAfterPanel.transform));
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(hudAfterPanel.transform.GetSiblingIndex()),
                "the migrated overlay must render above a HUD root saved after it");
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(decoy.transform.GetSiblingIndex()));
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out string error), Is.True,
                error);

            Undo.PerformUndo();

            Transform restored = canvas.Find(AddM08CampaignReviewPanel.RootName);
            Assert.That(restored, Is.Not.Null, "one Undo must restore the replaced legacy page");
            Assert.That(restored.GetComponent<CampaignReviewPanel>(), Is.Not.Null);
            Assert.That(FindDeep(canvas, AddM08CampaignReviewPanel.NewScenarioButtonName), Is.Null);
            Assert.That(FindDeep(canvas, AddM08CampaignReviewPanel.ContinueButtonName), Is.Not.Null,
                "the restored legacy page keeps its own hierarchy");
            Assert.That(ObjectCount(), Is.EqualTo(count),
                "Undo must remove every object the migration created");
            Assert.That(AddM08CampaignReviewPanel.Validate(scene, out _), Is.False,
                "the restored page is the legacy one again");
        }

        [Test]
        public void Updater_RefusesToMigrateALegacyPageThatOwnsUserObjects()
        {
            GameObject legacyRoot = BuildLegacyPanel(canvas);
            Transform card = FindDeep(legacyRoot.transform,
                AddM08CampaignReviewPanel.CardName);
            Create("UserNote", card);
            int count = ObjectCount();

            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.ExistingCampaignPanel));
            Assert.That(canvas.Find(AddM08CampaignReviewPanel.RootName),
                Is.SameAs(legacyRoot.transform),
                "a page a user extended must not be deleted by the migration");
            Assert.That(FindDeep(canvas, AddM08CampaignReviewPanel.NewScenarioButtonName), Is.Null);
            Assert.That(ObjectCount(), Is.EqualTo(count));
        }

        /// <summary>
        /// Rebuilds the page the first generator revision saved: the same names and hierarchy
        /// without the new-scenario action, with the old GoldDim status ink.
        /// </summary>
        private GameObject BuildLegacyPanel(Transform parent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject root = Create(AddM08CampaignReviewPanel.RootName, parent);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            GameObject page = Create(AddM08CampaignReviewPanel.PageName, root.transform,
                typeof(Image));
            var pageRect = (RectTransform)page.transform;
            pageRect.anchorMin = Vector2.zero;
            pageRect.anchorMax = Vector2.one;
            pageRect.offsetMin = Vector2.zero;
            pageRect.offsetMax = Vector2.zero;
            GameObject card = Create(AddM08CampaignReviewPanel.CardName, page.transform,
                typeof(Image), typeof(VerticalLayoutGroup));
            var cardRect = (RectTransform)card.transform;
            cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(1120f, 640f);
            CreateLegacyText(card.transform, AddM08CampaignReviewPanel.TitleTextName, font, "家族评价");
            CreateLegacyText(card.transform, AddM08CampaignReviewPanel.ContextTextName, font,
                "剧本来源与评价日期");

            GameObject scroll = Create(AddM08CampaignReviewPanel.BodyScrollName, card.transform,
                typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            GameObject viewport = Create(AddM08CampaignReviewPanel.BodyViewportName,
                scroll.transform, typeof(Image), typeof(RectMask2D));
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(8f, 6f);
            viewportRect.offsetMax = new Vector2(-8f, -6f);
            GameObject content = Create(AddM08CampaignReviewPanel.BodyContentName,
                viewport.transform, typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            Text body = CreateLegacyText(content.transform,
                AddM08CampaignReviewPanel.BodyTextName, font, string.Empty);
            body.verticalOverflow = VerticalWrapMode.Overflow;
            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = viewportRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            Text status = CreateLegacyText(card.transform,
                AddM08CampaignReviewPanel.StatusTextName, font, string.Empty);
            status.color = SengokuUiTheme.GoldDim;
            GameObject row = Create(AddM08CampaignReviewPanel.ButtonRowName, card.transform,
                typeof(LayoutElement));
            GameObject cont = Create(AddM08CampaignReviewPanel.ContinueButtonName,
                row.transform, typeof(Image), typeof(Button));
            var contRect = (RectTransform)cont.transform;
            contRect.sizeDelta = new Vector2(380f, 42f);
            Button contButton = cont.GetComponent<Button>();
            contButton.targetGraphic = cont.GetComponent<Image>();
            Text label = CreateLegacyText(cont.transform,
                AddM08CampaignReviewPanel.ContinueLabelName, font,
                AddM08CampaignReviewPanel.ContinueCaption);

            CampaignReviewPanel panel = root.AddComponent<CampaignReviewPanel>();
            SetReference(panel, "gameSession", session);
            SetReference(panel, "panelRoot", page);
            SetReference(panel, "titleText", FindDeep(root.transform,
                AddM08CampaignReviewPanel.TitleTextName).GetComponent<Text>());
            SetReference(panel, "contextText", FindDeep(root.transform,
                AddM08CampaignReviewPanel.ContextTextName).GetComponent<Text>());
            SetReference(panel, "bodyText", body);
            SetReference(panel, "continueLabelText", label);
            SetReference(panel, "statusText", status);
            SetReference(panel, "continueButton", contButton);
            return root;
        }

        private Text CreateLegacyText(Transform parent, string name, Font font, string value)
        {
            GameObject go = Create(name, parent, typeof(Text));
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1000f, 24f);
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = 14;
            text.text = value;
            text.color = SengokuUiTheme.ParchmentLight;
            text.raycastTarget = false;
            return text;
        }

        [Test]
        public void Panel_TabMovesFocusBetweenTheTwoActionsAndSubmitInvokesTheSelectedOne()
        {
            EventSystem eventSystem = CreateTestEventSystem();
            Assert.That(eventSystem, Is.Not.Null);
            Assert.That(EventSystem.current, Is.SameAs(eventSystem),
                "the focus assertions must target the active EventSystem");
            GameObject selectionBeforeTest = eventSystem.currentSelectedGameObject;
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            CampaignReviewPanel panel = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .GetComponent<CampaignReviewPanel>();
            Button continueButton = FindDeep(canvas, AddM08CampaignReviewPanel.ContinueButtonName)
                .GetComponent<Button>();
            Button startNew = FindDeep(canvas, AddM08CampaignReviewPanel.NewScenarioButtonName)
                .GetComponent<Button>();
            Text status = FindDeep(canvas, AddM08CampaignReviewPanel.StatusTextName)
                .GetComponent<Text>();
            try
            {
                int days = DaysBetween(session.WorldClock.CurrentDate,
                    GameDate.Parse(1615, 4, 1));
                Assert.That(session.AdvanceDaysForLegacyFixtureSetup(days), Is.True);
                panel.Refresh();
                Assert.That(panel.IsContinueFocused, Is.True);

                panel.FocusNextReviewAction();
                Assert.That(eventSystem.currentSelectedGameObject,
                    Is.EqualTo(startNew.gameObject),
                    "Tab must reach the second action while the navigation trap is on");
                Assert.That(panel.IsNewScenarioFocused, Is.True);
                Assert.That(panel.IsContinueFocused, Is.False);

                panel.FocusNextReviewAction();
                Assert.That(eventSystem.currentSelectedGameObject,
                    Is.EqualTo(continueButton.gameObject));
                Assert.That(panel.IsContinueFocused, Is.True);

                // Submitting the selected control is the exact handler the Input System's
                // Submit binding invokes. This fixture binds no title controller, so the
                // action reports the missing title without changing campaign state.
                ExecuteEvents.Execute<ISubmitHandler>(startNew.gameObject,
                    new PointerEventData(eventSystem), ExecuteEvents.submitHandler);
                Assert.That(status.text, Does.Contain("未找到标题界面"));
                Assert.That(session.PendingCampaignReview,
                    Is.EqualTo(CampaignReviewMilestone.StageReview),
                    "opening the selector route must not acknowledge the review");
            }
            finally
            {
                eventSystem.SetSelectedGameObject(selectionBeforeTest);
            }
        }

        [Test]
        public void Panel_RendersARealisticLongReviewAsBoundedBlocksInOrder()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            CampaignReviewPanel panel = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .GetComponent<CampaignReviewPanel>();
            string body = BuildRealisticLongReviewBody();
            Assert.That(body.Length, Is.GreaterThanOrEqualTo(80000),
                "the fixture must reproduce the reported 55-year review size");
            Assert.That(body.Length, Is.GreaterThan(
                    CampaignReviewPanel.MaxBodyBlockCharacters),
                "one Text must not be able to hold this body");

            panel.RenderBody(body);

            Transform content = FindDeep(canvas, AddM08CampaignReviewPanel.BodyContentName);
            Text[] rendered = content.GetComponentsInChildren<Text>(true);
            Assert.That(rendered.Length, Is.GreaterThan(1),
                "a long review must be split across several Text meshes");
            Assert.That(panel.BodyBlockCount, Is.EqualTo(rendered.Length));
            Assert.That(panel.Body, Is.EqualTo(body),
                "the panel must keep the whole formatted review, not a truncated copy");

            var combined = new StringBuilder(body.Length);
            for (int i = 0; i < rendered.Length; i++)
            {
                Text block = rendered[i];
                Assert.That(block.text.Length,
                    Is.LessThanOrEqualTo(CampaignReviewPanel.MaxBodyBlockCharacters),
                    "block " + i + " exceeds the safe character bound");
                Assert.That(block.text.Length * 4,
                    Is.LessThanOrEqualTo(CampaignReviewPanel.MaxBodyBlockVertices),
                    "block " + i + " exceeds the safe vertex bound");
                Assert.That(block.font, Is.EqualTo(rendered[0].font));
                Assert.That(block.fontSize, Is.EqualTo(rendered[0].fontSize));
                Assert.That(block.verticalOverflow, Is.EqualTo(VerticalWrapMode.Overflow));
                Assert.That(block.raycastTarget, Is.False);
                combined.Append(block.text);
            }

            Assert.That(CampaignReviewPanel.MaxBodyBlockVertices, Is.LessThan(65535),
                "the block budget must stay below the uGUI CanvasRenderer mesh limit");
            Assert.That(combined.ToString(), Is.EqualTo(body),
                "every source character must remain available in order");
            Assert.That(content.GetChild(0).GetComponent<Text>(), Is.SameAs(rendered[0]),
                "blocks must be laid out in source order as direct content children");
            for (int i = 1; i < rendered.Length; i++)
                Assert.That(content.GetChild(i).GetComponent<Text>(), Is.SameAs(rendered[i]),
                    "block " + i + " is out of scroll order");
        }

        [Test]
        public void Panel_RebuildsBodyBlocksWithoutDuplicatesWhenTheBodyChanges()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            CampaignReviewPanel panel = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .GetComponent<CampaignReviewPanel>();
            Transform content = FindDeep(canvas, AddM08CampaignReviewPanel.BodyContentName);

            panel.RenderBody(BuildRealisticLongReviewBody());
            int longBlocks = panel.BodyBlockCount;
            Assert.That(longBlocks, Is.GreaterThan(1));

            panel.RenderBody(BuildRealisticLongReviewBody());
            Assert.That(panel.BodyBlockCount, Is.EqualTo(longBlocks),
                "rendering the same body twice must not duplicate blocks");
            Assert.That(content.GetComponentsInChildren<Text>(true).Length,
                Is.EqualTo(longBlocks));

            panel.RenderBody("短评价");
            Assert.That(panel.BodyBlockCount, Is.EqualTo(1));
            Assert.That(panel.Body, Is.EqualTo("短评价"));
            Assert.That(content.GetComponentsInChildren<Text>(true).Length, Is.EqualTo(1),
                "surplus blocks must be removed when the body shrinks");
        }

        [Test]
        public void Panel_ResetsTheBodyScrollToTheTopWhenTheReviewBodyChanges()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            CampaignReviewPanel panel = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .GetComponent<CampaignReviewPanel>();
            ScrollRect scroll = FindDeep(canvas, AddM08CampaignReviewPanel.BodyScrollName)
                .GetComponent<ScrollRect>();
            Assert.That(scroll.content.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(scroll.content.pivot, Is.EqualTo(new Vector2(0.5f, 1f)),
                "anchored Y zero is the top only for a top-anchored, top-pivoted content");

            string stageReview = BuildRealisticLongReviewBody();
            panel.RenderBody(stageReview);
            SetScrollOffset(scroll, -1234f);
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(-1234f));

            // A state notification that re-renders the same review must not yank the reader
            // back to the top.
            panel.RenderBody(stageReview);
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(-1234f));

            // The 100-year review replaces the 55-year body in the same scroll.
            string centuryReview = stageReview + "\n【百年评定】";
            panel.RenderBody(centuryReview);
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f),
                "a newly rendered review body must start at its first line");
            Assert.That(panel.Body, Is.EqualTo(centuryReview));
        }

        [Test]
        public void Panel_AddsOneVisibleScrollbarAndClampsBodyScrolling()
        {
            Assert.That(AddM08CampaignReviewPanel.Update(scene),
                Is.EqualTo(AddM08CampaignReviewPanel.UpdateResult.Success));
            CampaignReviewPanel panel = canvas.Find(AddM08CampaignReviewPanel.RootName)
                .GetComponent<CampaignReviewPanel>();
            ScrollRect scroll = FindDeep(canvas, AddM08CampaignReviewPanel.BodyScrollName)
                .GetComponent<ScrollRect>();
            canvas.Find(AddM08CampaignReviewPanel.RootName)
                .Find(AddM08CampaignReviewPanel.PageName).gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            panel.RenderBody(BuildRealisticLongReviewBody());
            Canvas.ForceUpdateCanvases();

            panel.EnsureBodyScrollbar();
            panel.EnsureBodyScrollbar();
            Canvas.ForceUpdateCanvases();

            Assert.That(panel.BodyScrollbar, Is.Not.Null);
            Assert.That(panel.BodyScrollbar.gameObject.name,
                Is.EqualTo(CampaignReviewPanel.ScrollbarObjectName));
            Assert.That(panel.BodyScrollbar.gameObject.scene, Is.EqualTo(scroll.gameObject.scene),
                "the generated scrollbar must stay in the scroll's preview scene");
            Assert.That(panel.BodyScrollbar.handleRect.gameObject.scene,
                Is.EqualTo(scroll.gameObject.scene),
                "the generated handle must stay in the scroll's preview scene");
            Assert.That(scroll.GetComponentsInChildren<Scrollbar>(true), Has.Length.EqualTo(1));
            RectTransform viewport = (RectTransform)scroll.viewport;
            RectTransform track = (RectTransform)panel.BodyScrollbar.transform;
            Assert.That(track.rect.width, Is.EqualTo(24f).Within(0.01f));
            Assert.That(viewport.offsetMax.x, Is.EqualTo(-34f).Within(0.01f),
                "the viewport reserves 2 px margin, a 24 px target and an 8 px gap");
            var viewportCorners = new Vector3[4];
            var trackCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);
            track.GetWorldCorners(trackCorners);
            float viewportRight = scroll.transform.InverseTransformPoint(viewportCorners[2]).x;
            float trackLeft = scroll.transform.InverseTransformPoint(trackCorners[0]).x;
            Assert.That(trackLeft - viewportRight, Is.EqualTo(8f).Within(1f));
            float maximum = Mathf.Max(0f, scroll.content.rect.height - scroll.viewport.rect.height);
            Assert.That(maximum, Is.GreaterThan(0f), "the realistic review body must overflow");

            panel.ScrollBodyBy(float.MaxValue);
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(maximum).Within(1f));
            panel.ScrollBodyBy(float.MaxValue);
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(maximum).Within(1f));
            panel.ScrollBodyBy(-float.MaxValue);
            Assert.That(scroll.content.anchoredPosition.y, Is.EqualTo(0f).Within(0.01f));
        }

        private static void SetScrollOffset(ScrollRect scroll, float y)
        {
            Vector2 position = scroll.content.anchoredPosition;
            position.y = y;
            scroll.content.anchoredPosition = position;
        }

        [Test]
        public void SplitBody_KeepsWholeLinesAndSurrogatePairsInOrder()
        {
            string body = BuildRealisticLongReviewBody();
            IReadOnlyList<string> blocks = CampaignReviewPanel.SplitBody(body,
                CampaignReviewPanel.MaxBodyBlockCharacters);

            Assert.That(blocks.Count, Is.GreaterThan(1));
            Assert.That(string.Concat(blocks), Is.EqualTo(body));
            for (int i = 0; i < blocks.Count; i++)
            {
                Assert.That(blocks[i], Is.Not.Empty);
                Assert.That(blocks[i].Length,
                    Is.LessThanOrEqualTo(CampaignReviewPanel.MaxBodyBlockCharacters));
                if (i < blocks.Count - 1)
                    Assert.That(blocks[i].EndsWith("\n", StringComparison.Ordinal), Is.True,
                        "a review row must not be cut in half");
            }

            // 甲 + a surrogate pair + 乙: a two-character window lands inside the pair.
            const string pair = "甲\U0001F600乙";
            IReadOnlyList<string> split = CampaignReviewPanel.SplitBody(pair, 2);
            Assert.That(string.Concat(split), Is.EqualTo(pair));
            for (int i = 0; i < split.Count; i++)
            {
                Assert.That(char.IsLowSurrogate(split[i][0]), Is.False,
                    "a block must not start with a lone low surrogate");
                Assert.That(char.IsHighSurrogate(split[i][split[i].Length - 1]), Is.False,
                    "a block must not end with a lone high surrogate");
            }

            Assert.That(CampaignReviewPanel.SplitBody(string.Empty, 10), Is.EquivalentTo(
                new[] { string.Empty }));
            Assert.That(string.Concat(CampaignReviewPanel.SplitBody(body, 97)),
                Is.EqualTo(body), "any budget must still reproduce the source exactly");
        }

        private static string BuildRealisticLongReviewBody()
        {
            // Mirrors CampaignReviewView.FormatBody: one section per GDD 10.3 category in
            // GDD order, each row dated, labelled and sourced, then the five sourced
            // dimensions. 55 years of monthly settlement rows reproduce the reported
            // 89,212-character live review.
            var builder = new StringBuilder(96 * 1024);
            builder.Append("剧本实际开局后第 55 年的初步评价（不终止游戏）\n\n");
            var categories = (CampaignHistoryCategory[])Enum.GetValues(
                typeof(CampaignHistoryCategory));
            for (int c = 0; c < categories.Length; c++)
            {
                builder.Append('【').Append(CampaignReviewView.CategoryName(categories[c]))
                    .Append('】');
                for (int i = 0; i < 220; i++)
                {
                    int year = 1560 + i % 55;
                    int month = 1 + i % 12;
                    builder.Append('\n')
                        .Append(year).Append('-').Append(month.ToString("00"))
                        .Append("・产业结算：资金 ").Append(1200 + i * 7)
                        .Append(" → ").Append(1260 + i * 7)
                        .Append("；月度产业结算已记入家族财富变化，町屋与运输队维持收支（来源：")
                        .Append(FamilyChronicleEntry.BuildEntryId(i + 1)).Append('）');
                }
                builder.Append('\n');
            }
            builder.Append("【五项发展维度】");
            for (int i = 0; i < 5; i++)
                builder.Append("\n・家族名望：").Append(i)
                    .Append("（来源：任务结算记录的名望值）");
            builder.Append("\n【说明】\n")
                .Append("五项发展维度分别列出来源事实，不生成综合评分；空项表示尚无权威记录。");
            return builder.ToString();
        }

        /// <summary>
        /// Reproduces FoundationScene's Canvas layers the two reviewed generators validate:
        /// the M6 title overlay and the M06 tactical modal, with EventChoiceRoot above the
        /// title and the tactical overlay last.
        /// </summary>
        private void BuildM6TitleAndTacticalLayers()
        {
            canvas.sizeDelta = new Vector2(1280f, 720f);
            Transform map = canvas.Find("M2WorldMapRoot");
            var mapRect = (RectTransform)map;
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(0.5f, 0.19722222f);
            mapRect.pivot = new Vector2(0.5f, 0.5f);
            mapRect.sizeDelta = new Vector2(1216f, 268f);

            GameObject town = Create("TownMenuRoot", map);
            var townRect = (RectTransform)town.transform;
            townRect.anchorMin = Vector2.zero;
            townRect.anchorMax = Vector2.one;
            townRect.pivot = new Vector2(0.5f, 0.5f);
            townRect.offsetMin = Vector2.zero;
            townRect.offsetMax = Vector2.zero;
            town.SetActive(false);

            var controller = map.GetComponent<WorldMapController>();
            SetReference(controller, "gameSession", session);
            SetReference(controller, "townMenuRoot", town);

            Create(AddM08CampaignReviewPanel.EventChoiceRootName, canvas);
        }

        private static int DaysBetween(GameDate from, GameDate to)
        {
            int days = 0;
            GameDate cursor = from;
            while (cursor < to && GameDate.TryNext(cursor, out GameDate next))
            {
                cursor = next;
                days++;
            }
            return days;
        }

        private GameObject Create(string name, Transform parent, params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (Type type in components)
                go.AddComponent(type);
            return go;
        }

        private EventSystem CreateTestEventSystem()
        {
            if (EventSystem.current != null)
                return EventSystem.current;

            EventSystem eventSystem = Create("TestEventSystem", null, typeof(EventSystem))
                .GetComponent<EventSystem>();
            typeof(EventSystem).GetMethod("OnEnable",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(eventSystem, null);
            manuallyRegisteredEventSystem = eventSystem;
            return eventSystem;
        }

        private static void InvokeLateUpdate(CampaignReviewPanel panel) =>
            typeof(CampaignReviewPanel).GetMethod("LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, null);

        private static Transform FindDeep(Transform parent, string name) =>
            parent.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == name);

        private Transform[] All() => scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();

        private int ObjectCount() => All().Length;

        private void AssertNoPanel()
        {
            Assert.That(All().Any(t =>
                t.name == AddM08CampaignReviewPanel.RootName ||
                t.GetComponent<CampaignReviewPanel>() != null), Is.False);
        }

        private static Object GetReference(Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            return corners.All(c =>
            {
                Vector3 point = parent.InverseTransformPoint(c);
                return point.x >= bounds.xMin - 0.1f && point.x <= bounds.xMax + 0.1f &&
                       point.y >= bounds.yMin - 0.1f && point.y <= bounds.yMax + 0.1f;
            });
        }

        private static float ContrastRatio(Color first, Color second)
        {
            float high = Mathf.Max(RelativeLuminance(first), RelativeLuminance(second));
            float low = Mathf.Min(RelativeLuminance(first), RelativeLuminance(second));
            return (high + 0.05f) / (low + 0.05f);
        }

        private static float RelativeLuminance(Color color) =>
            0.2126f * Linear(color.r) + 0.7152f * Linear(color.g) +
            0.0722f * Linear(color.b);

        private static float Linear(float channel) => channel <= 0.04045f
            ? channel / 12.92f
            : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);
    }
}
