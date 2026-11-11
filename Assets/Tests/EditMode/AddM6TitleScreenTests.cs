using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class AddM6TitleScreenTests
    {
        private Scene _scene;
        private Scene _previousScene;
        private bool _previousDirty;
        private Transform _canvas;
        private Transform _map;
        private Transform _town;
        private GameObject _sessionObject;
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _previousScene = SceneManager.GetActiveScene();
            _previousDirty = _previousScene.isDirty;
            _scene = EditorSceneManager.NewPreviewScene();

            GameObject canvas = Create("Canvas", null, typeof(Canvas), typeof(CanvasScaler));
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.enabled = false;
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.anchorMin = canvasRect.anchorMax = Vector2.zero;
            canvasRect.pivot = new Vector2(0.5f, 0.5f);
            canvasRect.anchoredPosition = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            canvasRect.localScale = Vector3.one;
            _canvas = canvas.transform;

            GameObject map = Create("M2WorldMapRoot", _canvas, typeof(WorldMapController));
            map.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            _map = map.transform;

            GameObject town = Create("TownMenuRoot", _map);
            town.GetComponent<RectTransform>().sizeDelta = new Vector2(1216f, 268f);
            _town = town.transform;
            town.SetActive(false);
            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRect);

            _sessionObject = Create("GameSession", null, typeof(GameSession));
            _session = _sessionObject.GetComponent<GameSession>();
            _session.InitNewGame();
        }

        [TearDown]
        public void TearDown()
        {
            if (_scene.IsValid())
                EditorSceneManager.ClosePreviewScene(_scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(_previousScene));
            Assert.That(_previousScene.isDirty, Is.EqualTo(_previousDirty));
        }

        [Test]
        public void Updater_AddsAndBindsTwoPageTitleAndSummary()
        {
            int count = ObjectCount();

            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            Transform root = Find(AddM6TitleScreen.RootName);
            Assert.That(root, Is.Not.Null);
            Assert.That(root.parent, Is.EqualTo(_canvas));
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(_map.GetSiblingIndex()));
            Assert.That(root.childCount, Is.EqualTo(2));
            Assert.That(root.gameObject.activeSelf, Is.False, "Title must boot hidden and be shown by Play Mode.");
            Assert.That(ObjectCount(), Is.EqualTo(count + 33));

            Image background = root.GetComponent<Image>();
            Assert.That(background, Is.Not.Null);
            Assert.That(background.raycastTarget, Is.True);
            Assert.That(background.color.a, Is.EqualTo(1f));
            Assert.That(background.sprite, Is.EqualTo(LoadSprite(AddM6TitleScreen.BackgroundAssetPath)));
            RectTransform rootRect = (RectTransform)root;
            Assert.That(rootRect.anchorMin, Is.EqualTo(Vector2.zero));
            Assert.That(rootRect.anchorMax, Is.EqualTo(Vector2.one));

            Transform main = Find(AddM6TitleScreen.MainMenuRootName);
            Transform selector = Find(AddM6TitleScreen.ScenarioSelectRootName);
            Assert.That(main, Is.Not.Null);
            Assert.That(selector, Is.Not.Null);
            Assert.That(main.parent, Is.EqualTo(root));
            Assert.That(selector.parent, Is.EqualTo(root));
            Assert.That(main.gameObject.activeSelf, Is.True);
            Assert.That(selector.gameObject.activeSelf, Is.False);
            Assert.That(main.GetComponentsInChildren<Button>(true).Length, Is.EqualTo(2),
                "The main menu must expose only Continue and New Game.");
            Assert.That(main.childCount, Is.EqualTo(6));
            Assert.That(Find(AddM6TitleScreen.ContinueButtonName).GetComponent<RectTransform>().anchoredPosition.y,
                Is.GreaterThan(Find(AddM6TitleScreen.NewGameButtonName)
                    .GetComponent<RectTransform>().anchoredPosition.y),
                "The approved sketch lists Continue above New Game.");
            Assert.That(selector.childCount, Is.EqualTo(12));
            Assert.That(Find(AddM6TitleScreen.SealName).GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(main.GetComponent<Outline>(), Is.Not.Null);
            Assert.That(selector.GetComponent<Outline>(), Is.Not.Null);
            Assert.That(main.GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(selector.GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(Find(AddM6TitleScreen.NewGameButtonName).GetComponent<Image>().sprite, Is.Not.Null);
            Assert.That(Find(AddM6TitleScreen.NewGameButtonName).GetComponent<Outline>(), Is.Not.Null);
            Assert.That(Find(AddM6TitleScreen.MainlineSectionName).parent, Is.EqualTo(selector));
            Assert.That(Find(AddM6TitleScreen.TopicsSectionName).parent, Is.EqualTo(selector));
            Assert.That(root.GetSiblingIndex(), Is.EqualTo(_canvas.childCount - 1),
                "The full-screen title overlay must render above every Canvas child.");

            TitleScreenController controller = _sessionObject.GetComponent<TitleScreenController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(GetReference(controller, "gameSession"), Is.EqualTo(_session));
            Assert.That(GetReference(controller, "worldMapController"), Is.EqualTo(_map.GetComponent<WorldMapController>()));
            Assert.That(GetReference(controller, "titleRoot"), Is.EqualTo(root.gameObject));
            Assert.That(GetReference(controller, "mainMenuRoot"), Is.EqualTo(main.gameObject));
            Assert.That(GetReference(controller, "scenarioSelectionRoot"), Is.EqualTo(selector.gameObject));
            Assert.That(GetReference(controller, "newGameButton"),
                Is.EqualTo(Find(AddM6TitleScreen.NewGameButtonName).GetComponent<Button>()));
            Assert.That(GetReference(controller, "continueButton"),
                Is.EqualTo(Find(AddM6TitleScreen.ContinueButtonName).GetComponent<Button>()));
            Assert.That(GetReference(controller, "statusText"),
                Is.EqualTo(Find(AddM6TitleScreen.StatusTextName).GetComponent<Text>()));
            Assert.That(GetReference(controller, "fixedScenarioButton"),
                Is.EqualTo(Find(AddM6TitleScreen.FixedScenarioButtonName).GetComponent<Button>()));
            Assert.That(GetReference(controller, "scenarioBackButton"),
                Is.EqualTo(Find(AddM6TitleScreen.ScenarioBackButtonName).GetComponent<Button>()));
            foreach (string name in ScenarioButtonFieldNames.Keys)
            {
                Assert.That(GetReference(controller, ScenarioButtonFieldNames[name]),
                    Is.EqualTo(Find(name).GetComponent<Button>()), name);
            }

            Transform summary = Find(AddM6TitleScreen.DemoSummaryTextName);
            Assert.That(summary, Is.Not.Null);
            Assert.That(summary.parent, Is.EqualTo(_map));
            Assert.That(summary.GetSiblingIndex(), Is.LessThan(_town.GetSiblingIndex()));
            Assert.That(summary.gameObject.activeSelf, Is.False);

            DemoSummaryPanel panel = _sessionObject.GetComponent<DemoSummaryPanel>();
            Assert.That(panel, Is.Not.Null);
            Assert.That(GetReference(panel, "gameSession"), Is.EqualTo(_session));
            Assert.That(GetReference(panel, "summaryText"), Is.EqualTo(summary.GetComponent<Text>()));

            Assert.That(AddM6TitleScreen.Validate(_scene, out string error), Is.True, error);
            Assert.That(_scene.path, Is.Empty, "The test seam must never save its scene.");
        }

        [Test]
        public void Updater_MigrationRollbackRestoresRootTransformImageChildrenAndReferences()
        {
            GameObject eventRoot = Create(AddM6TitleScreen.EventChoiceRootName, _canvas);
            CreateLegacyTitle();
            Transform root = Find(AddM6TitleScreen.RootName);
            RectTransform rootRect = (RectTransform)root;
            rootRect.anchorMin = new Vector2(0.1f, 0.2f);
            rootRect.anchorMax = new Vector2(0.9f, 0.8f);
            rootRect.pivot = new Vector2(0.3f, 0.4f);
            rootRect.sizeDelta = new Vector2(1001f, 601f);
            rootRect.anchoredPosition = new Vector2(11f, 12f);
            rootRect.localScale = new Vector3(1f, 1f, 1f);
            Vector2 expectedAnchorMin = rootRect.anchorMin;
            Vector2 expectedAnchorMax = rootRect.anchorMax;
            Vector2 expectedPivot = rootRect.pivot;
            Vector2 expectedSizeDelta = rootRect.sizeDelta;
            Vector2 expectedAnchoredPosition = rootRect.anchoredPosition;
            Vector2 expectedOffsetMin = rootRect.offsetMin;
            Vector2 expectedOffsetMax = rootRect.offsetMax;
            Image background = root.GetComponent<Image>();
            Sprite legacySprite = LoadAnySprite(AddM6TitleScreen.SealAssetPath);
            Assert.That(legacySprite, Is.Not.Null);
            Color legacyColor = new Color(0.2f, 0.3f, 0.4f, 0.8f);
            background.sprite = legacySprite;
            background.color = legacyColor;
            background.raycastTarget = false;
            background.type = Image.Type.Tiled;
            background.preserveAspect = true;
            int originalSibling = root.GetSiblingIndex();
            Assert.That(originalSibling, Is.GreaterThan(eventRoot.transform.GetSiblingIndex()),
                "The fixture must start with EventChoiceRoot above M6TitleRoot so migration reorders it.");
            TitleScreenController controller = _sessionObject.GetComponent<TitleScreenController>();
            Transform retained = Find(AddM6TitleScreen.EducationDemoButtonName);
            int count = ObjectCount();

            AddM6TitleScreen.UpdateResult result = AddM6TitleScreen.Update(_scene, _ =>
                SetReference(controller, "titleRoot", null));

            Assert.That(result, Is.EqualTo(AddM6TitleScreen.UpdateResult.ValidationFailed));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(Find(AddM6TitleScreen.RootName), Is.SameAs(root));
            Assert.That(root.GetSiblingIndex(), Is.EqualTo(originalSibling));
            Assert.That(eventRoot.transform.GetSiblingIndex(), Is.LessThan(root.GetSiblingIndex()));
            Assert.That(rootRect.anchorMin, Is.EqualTo(expectedAnchorMin));
            Assert.That(rootRect.anchorMax, Is.EqualTo(expectedAnchorMax));
            Assert.That(rootRect.offsetMin, Is.EqualTo(expectedOffsetMin));
            Assert.That(rootRect.offsetMax, Is.EqualTo(expectedOffsetMax));
            Assert.That(rootRect.pivot, Is.EqualTo(expectedPivot));
            Assert.That(rootRect.anchoredPosition, Is.EqualTo(expectedAnchoredPosition));
            Assert.That(rootRect.sizeDelta, Is.EqualTo(expectedSizeDelta));
            Assert.That(background.sprite, Is.SameAs(legacySprite));
            Assert.That(background.color.r, Is.EqualTo(legacyColor.r).Within(0.001f));
            Assert.That(background.color.g, Is.EqualTo(legacyColor.g).Within(0.001f));
            Assert.That(background.color.b, Is.EqualTo(legacyColor.b).Within(0.001f));
            Assert.That(background.color.a, Is.EqualTo(legacyColor.a).Within(0.001f));
            Assert.That(background.raycastTarget, Is.False);
            Assert.That(background.type, Is.EqualTo(Image.Type.Tiled));
            Assert.That(background.preserveAspect, Is.True);
            Assert.That(root.childCount, Is.EqualTo(11));
            Assert.That(retained.parent, Is.EqualTo(root));
            Assert.That(_sessionObject.GetComponent<TitleScreenController>(), Is.SameAs(controller));
            Assert.That(GetReference(controller, "titleRoot"), Is.SameAs(root.gameObject));
            Assert.That(GetReference(controller, "mainMenuRoot"), Is.Null);
            Button restoredStandard = Find(AddM6TitleScreen.StandardSampleButtonName).GetComponent<Button>();
            Assert.That(restoredStandard, Is.Not.Null);
            Assert.That(restoredStandard.transform.parent, Is.EqualTo(root));
            Assert.That(GetReference(controller, "standardSampleButton"), Is.SameAs(restoredStandard));
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Updater_IsIdempotentWhenAlreadyValid()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            int builtCount = ObjectCount();
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Assert.That(ObjectCount(), Is.EqualTo(builtCount));
        }

        [Test]
        public void Updater_RollsBackWhenValidationFails()
        {
            int count = ObjectCount();

            AddM6TitleScreen.UpdateResult result = AddM6TitleScreen.Update(_scene, _ =>
                SetReference(_sessionObject.GetComponent<TitleScreenController>(), "titleRoot", null));

            Assert.That(result, Is.EqualTo(AddM6TitleScreen.UpdateResult.ValidationFailed));
            Assert.That(Find(AddM6TitleScreen.RootName), Is.Null);
            Assert.That(Find(AddM6TitleScreen.DemoSummaryTextName), Is.Null);
            Assert.That(_sessionObject.GetComponent<TitleScreenController>(), Is.Null);
            Assert.That(_sessionObject.GetComponent<DemoSummaryPanel>(), Is.Null);
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
            Assert.That(_scene.path, Is.Empty);
        }

        [Test]
        public void Updater_RefusesPartialExistingPanel()
        {
            GameObject root = Create(AddM6TitleScreen.RootName, _canvas, typeof(Image));
            root.SetActive(false);

            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.ExistingPanel));
        }

        [Test]
        public void Updater_RefusesHierarchyThatAlreadyContainsAPageRoot()
        {
            GameObject root = Create(AddM6TitleScreen.RootName, _canvas, typeof(Image));
            Create(AddM6TitleScreen.MainMenuRootName, root.transform);
            root.SetActive(false);

            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.ExistingPanel));
            Assert.That(Find(AddM6TitleScreen.RootName), Is.Not.Null);
            Assert.That(Find(AddM6TitleScreen.MainMenuRootName), Is.Not.Null);
            Assert.That(_sessionObject.GetComponent<TitleScreenController>(), Is.Null);
        }

        [Test]
        public void Updater_RefusesIncompleteLegacyLayoutWithoutDeletingIt()
        {
            CreateLegacyTitle();
            Transform retained = Find(AddM6TitleScreen.EducationDemoButtonName);
            Assert.That(retained, Is.Not.Null);
            Object.DestroyImmediate(Find(AddM6TitleScreen.SeclusionDemoButtonName).gameObject);
            int count = ObjectCount();

            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.ExistingPanel));
            Assert.That(ObjectCount(), Is.EqualTo(count));
            Assert.That(retained, Is.Not.Null);
            Assert.That(retained.parent, Is.EqualTo(Find(AddM6TitleScreen.RootName)));
            Assert.That(Find(AddM6TitleScreen.MainMenuRootName), Is.Null);
            Assert.That(_sessionObject.GetComponent<TitleScreenController>(), Is.Not.Null);
        }

        [Test]
        public void Updater_MigratesCompleteLegacyLayoutInPlace()
        {
            GameObject eventRoot = Create(AddM6TitleScreen.EventChoiceRootName, _canvas);
            CreateLegacyTitle();
            Transform preMigrationRoot = Find(AddM6TitleScreen.RootName);
            Assert.That(preMigrationRoot.GetSiblingIndex(),
                Is.GreaterThan(eventRoot.transform.GetSiblingIndex()),
                "The fixture must start with EventChoiceRoot above M6TitleRoot so migration reorders it.");
            TitleScreenController controller = _sessionObject.GetComponent<TitleScreenController>();
            DemoSummaryPanel panel = _sessionObject.GetComponent<DemoSummaryPanel>();
            Text summary = Find(AddM6TitleScreen.DemoSummaryTextName).GetComponent<Text>();

            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            Assert.That(_sessionObject.GetComponent<TitleScreenController>(), Is.SameAs(controller));
            Assert.That(_sessionObject.GetComponent<DemoSummaryPanel>(), Is.SameAs(panel));
            Assert.That(Find(AddM6TitleScreen.DemoSummaryTextName).GetComponent<Text>(), Is.SameAs(summary));
            Assert.That(eventRoot, Is.Not.Null, "Migration must not delete unrelated Canvas roots.");

            Transform root = Find(AddM6TitleScreen.RootName);
            Assert.That(root.childCount, Is.EqualTo(2));
            Assert.That(root.GetSiblingIndex(), Is.EqualTo(eventRoot.transform.GetSiblingIndex() - 1),
                "Migration must keep the title above gameplay HUD roots and immediately below the event overlay.");
            Assert.That(eventRoot.transform.GetSiblingIndex(), Is.EqualTo(root.GetSiblingIndex() + 1),
                "The successful migration must move M6TitleRoot below EventChoiceRoot.");
            Assert.That(root.Find(AddM6TitleScreen.LabelName), Is.Null,
                "Legacy controls living directly under the root must be replaced by the page roots.");
            Transform main = Find(AddM6TitleScreen.MainMenuRootName);
            Transform selector = Find(AddM6TitleScreen.ScenarioSelectRootName);
            Assert.That(main, Is.Not.Null);
            Assert.That(selector, Is.Not.Null);
            Assert.That(main.Find(AddM6TitleScreen.LabelName), Is.Not.Null);
            Assert.That(main.Find(AddM6TitleScreen.StandardSampleButtonName), Is.Null);
            Assert.That(selector.Find(AddM6TitleScreen.FixedScenarioButtonName), Is.Not.Null);
            Assert.That(selector.Find(AddM6TitleScreen.StandardSampleButtonName), Is.Not.Null);
            Assert.That(selector.Find(AddM6TitleScreen.FamilyDoctrineDemoButtonName), Is.Not.Null);
            Assert.That(GetReference(controller, "mainMenuRoot"), Is.EqualTo(main.gameObject));
            Assert.That(GetReference(controller, "scenarioSelectionRoot"), Is.EqualTo(selector.gameObject));
            Assert.That(AddM6TitleScreen.Validate(_scene, out string error), Is.True, error);

            int migratedCount = ObjectCount();
            Assert.That(AddSC013TitleScenarioButtons.Update(_scene),
                Is.EqualTo(AddSC013TitleScenarioButtons.UpdateResult.Success),
                "The SC-013 title updater must accept the migrated hierarchy without recreating flat children.");
            Assert.That(ObjectCount(), Is.EqualTo(migratedCount));
        }

        [Test]
        public void Validator_RejectsMissingPageRoot()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Object.DestroyImmediate(Find(AddM6TitleScreen.ScenarioSelectRootName).gameObject);

            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Validator_RejectsMissingFixedEntry()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Object.DestroyImmediate(Find(AddM6TitleScreen.FixedScenarioButtonName).gameObject);

            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Validator_RejectsDuplicateScenarioEntry()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            GameObject duplicate = Object.Instantiate(Find(AddM6TitleScreen.StandardSampleButtonName).gameObject,
                Find(AddM6TitleScreen.ScenarioSelectRootName));
            duplicate.name = AddM6TitleScreen.StandardSampleButtonName;

            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
            Object.DestroyImmediate(duplicate);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_RejectsStaleScenarioEntryOnMainMenu()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Transform selector = Find(AddM6TitleScreen.ScenarioSelectRootName);
            Transform stale = Find(AddM6TitleScreen.StandardSampleButtonName);
            stale.SetParent(Find(AddM6TitleScreen.MainMenuRootName), false);

            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
            stale.SetParent(selector, false);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_RejectsWrongBackgroundSprite()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Image background = Find(AddM6TitleScreen.RootName).GetComponent<Image>();
            Sprite correct = background.sprite;
            background.sprite = null;
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);

            Sprite wrong = LoadAnySprite(AddM6TitleScreen.SealAssetPath);
            Assert.That(wrong, Is.Not.Null, "The test needs a real non-null different Sprite.");
            Assert.That(wrong, Is.Not.SameAs(correct));
            background.sprite = wrong;
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);

            background.sprite = correct;
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_RejectsUnboundNavigation()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            TitleScreenController controller = _sessionObject.GetComponent<TitleScreenController>();
            SetReference(controller, "fixedScenarioButton", null);

            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Validator_RejectsActiveTitleRoot()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            Find(AddM6TitleScreen.RootName).gameObject.SetActive(true);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Validator_RejectsSelectorShownAtBoot()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            Find(AddM6TitleScreen.ScenarioSelectRootName).gameObject.SetActive(true);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
        }

        [Test]
        public void Validator_RejectsSummaryOverlappingSaveControls()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            GameObject blocker = Create("M5SaveButton", _map, typeof(Image), typeof(Button));
            RectTransform rect = blocker.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(460f, -110f);
            rect.sizeDelta = new Vector2(150f, 30f);

            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);
            Object.DestroyImmediate(blocker);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_RejectsMainControlOutsideTheLeftPanel()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            RectTransform newGame = (RectTransform)Find(AddM6TitleScreen.NewGameButtonName);
            Vector2 original = newGame.anchoredPosition;

            newGame.anchoredPosition = new Vector2(300f, original.y);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);

            newGame.anchoredPosition = original;
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_RejectsSelectorControlOutsideTheCanvas()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            RectTransform fixedScenario = (RectTransform)Find(AddM6TitleScreen.FixedScenarioButtonName);
            Vector2 original = fixedScenario.anchoredPosition;

            fixedScenario.anchoredPosition = new Vector2(700f, original.y);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);

            fixedScenario.anchoredPosition = original;
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_AcceptsTitleImmediatelyBelowEventChoiceRoot()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            GameObject eventRoot = Create(AddM6TitleScreen.EventChoiceRootName, _canvas);
            Transform root = Find(AddM6TitleScreen.RootName);

            Assert.That(root.GetSiblingIndex(), Is.EqualTo(eventRoot.transform.GetSiblingIndex() - 1));
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);

            root.SetAsLastSibling();
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);

            root.SetSiblingIndex(eventRoot.transform.GetSiblingIndex());
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_RejectsAGameplayRootAboveTheTitleWhenNoEventRootExists()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            GameObject hudRoot = Create("M02DailyActionRoot", _canvas);
            Transform root = Find(AddM6TitleScreen.RootName);

            Assert.That(root.GetSiblingIndex(), Is.LessThan(_canvas.childCount - 1));
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False);

            Object.DestroyImmediate(hudRoot);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_AcceptsTacticalOverlayAboveTheTitleAndStillRejectsOtherRoots()
        {
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            GameObject overlay = Create(AddM6TitleScreen.TacticalOverlayName, _canvas);
            Transform root = Find(AddM6TitleScreen.RootName);
            Assert.That(overlay.transform.GetSiblingIndex(), Is.GreaterThan(root.GetSiblingIndex()),
                "The tactical sheet must sit above the inactive title overlay.");
            Assert.That(AddM6TitleScreen.Validate(_scene, out string error), Is.True, error);

            GameObject hudRoot = Create("M02DailyActionRoot", _canvas);
            hudRoot.transform.SetSiblingIndex(overlay.transform.GetSiblingIndex());
            Assert.That(hudRoot.transform.GetSiblingIndex(), Is.GreaterThan(root.GetSiblingIndex()));
            Assert.That(overlay.transform.GetSiblingIndex(), Is.EqualTo(_canvas.childCount - 1),
                "Only the injected gameplay root may sit between the title and the tactical sheet.");
            Assert.That(AddM6TitleScreen.Validate(_scene, out error), Is.False,
                "An unrelated gameplay root above the title must still be refused.");
            Assert.That(error, Is.Not.Empty);

            Object.DestroyImmediate(hudRoot);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Validator_AcceptsTacticalOverlayWithEventChoiceRootAdjacency()
        {
            GameObject eventRoot = Create(AddM6TitleScreen.EventChoiceRootName, _canvas);
            GameObject overlay = Create(AddM6TitleScreen.TacticalOverlayName, _canvas);
            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
            Transform root = Find(AddM6TitleScreen.RootName);

            Assert.That(root.GetSiblingIndex(), Is.EqualTo(eventRoot.transform.GetSiblingIndex() - 1),
                "The EventChoiceRoot adjacency rule must survive the tactical overlay.");
            Assert.That(overlay.transform.GetSiblingIndex(), Is.EqualTo(_canvas.childCount - 1));
            Assert.That(AddM6TitleScreen.Validate(_scene, out string error), Is.True, error);

            GameObject hudRoot = Create("M02DailyActionRoot", _canvas);
            hudRoot.transform.SetSiblingIndex(overlay.transform.GetSiblingIndex());
            Assert.That(hudRoot.transform.GetSiblingIndex(), Is.GreaterThan(root.GetSiblingIndex()));
            Assert.That(overlay.transform.GetSiblingIndex(), Is.EqualTo(_canvas.childCount - 1));
            Assert.That(AddM6TitleScreen.Validate(_scene, out error), Is.False,
                "Only the exempt tactical sheet may render above the title.");
            Assert.That(error, Does.Contain("Only EventChoiceRoot"));
            Object.DestroyImmediate(hudRoot);

            root.SetSiblingIndex(_canvas.childCount - 1);
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False,
                "The title must not drift above EventChoiceRoot.");

            root.SetSiblingIndex(eventRoot.transform.GetSiblingIndex());
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.True);
        }

        [Test]
        public void Updater_PlacesTitleBeneathTacticalOverlayWhenEventChoiceRootIsAbsent()
        {
            GameObject overlay = Create(AddM6TitleScreen.TacticalOverlayName, _canvas);

            Assert.That(AddM6TitleScreen.Update(_scene), Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));

            Transform root = Find(AddM6TitleScreen.RootName);
            Assert.That(overlay.transform.GetSiblingIndex(), Is.EqualTo(_canvas.childCount - 1),
                "The updater must leave the tactical sheet as the last Canvas child.");
            Assert.That(root.GetSiblingIndex(), Is.EqualTo(overlay.transform.GetSiblingIndex() - 1),
                "The title must sit immediately beneath the tactical sheet.");
            Assert.That(AddM6TitleScreen.Validate(_scene, out string error), Is.True, error);

            root.SetAsLastSibling();
            Assert.That(AddM6TitleScreen.Validate(_scene, out _), Is.False,
                "The validator must refuse a title that outranks the tactical sheet.");
        }

        [Test]
        public void Updater_RefusesWrongOrDirtyScene()
        {
            Assert.That(AddM6TitleScreen.CheckScenePreflight("Assets/Scenes/Other.unity", false),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.SceneMismatch));
            Assert.That(AddM6TitleScreen.CheckScenePreflight(AddM6TitleScreen.ScenePath, true),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.SceneDirty));
            Assert.That(AddM6TitleScreen.CheckScenePreflight(AddM6TitleScreen.ScenePath, false),
                Is.EqualTo(AddM6TitleScreen.UpdateResult.Success));
        }

        /// <summary>
        /// Recreates the pre-redesign single-page title hierarchy: four base controls and the
        /// seven scenario entries directly under M6TitleRoot, with the existing controller,
        /// summary panel, and summary text.
        /// </summary>
        private void CreateLegacyTitle()
        {
            GameObject rootObject = Create(AddM6TitleScreen.RootName, _canvas, typeof(Image));
            Image background = rootObject.GetComponent<Image>();
            background.color = new Color(0.07f, 0.08f, 0.11f, 1f);
            background.raycastTarget = true;
            RectTransform rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            Text label = CreateText(rootObject.transform, AddM6TitleScreen.LabelName, "战国立志传");
            Text status = CreateText(rootObject.transform, AddM6TitleScreen.StatusTextName, "");
            Button newGame = CreateButton(rootObject.transform, AddM6TitleScreen.NewGameButtonName, "新游戏");
            Button continueButton = CreateButton(rootObject.transform, AddM6TitleScreen.ContinueButtonName, "继续");
            Button standard = CreateButton(rootObject.transform, AddM6TitleScreen.StandardSampleButtonName, "标准");
            Button crisis = CreateButton(rootObject.transform, AddM6TitleScreen.CrisisDemoButtonName, "危机");
            Button education = CreateButton(rootObject.transform, AddM6TitleScreen.EducationDemoButtonName, "教育");
            Button research = CreateButton(rootObject.transform, AddM6TitleScreen.ResearchDemoButtonName, "研究");
            Button familyFormation = CreateButton(rootObject.transform, AddM6TitleScreen.FamilyFormationDemoButtonName, "婚姻");
            Button seclusion = CreateButton(rootObject.transform, AddM6TitleScreen.SeclusionDemoButtonName, "隐居");
            Button doctrine = CreateButton(rootObject.transform, AddM6TitleScreen.FamilyDoctrineDemoButtonName, "家训");

            Text summary = CreateText(_map, AddM6TitleScreen.DemoSummaryTextName, "");
            summary.gameObject.SetActive(false);
            summary.transform.SetSiblingIndex(_town.GetSiblingIndex());

            TitleScreenController controller = _sessionObject.AddComponent<TitleScreenController>();
            controller.Bind(_session, null, _map.GetComponent<WorldMapController>(), rootObject,
                newGame, continueButton, status);
            controller.BindScenarioButtons(standard, crisis);
            controller.BindEducationScenarioButton(education);
            controller.BindResearchScenarioButton(research);
            controller.BindFamilyFormationScenarioButton(familyFormation);
            controller.BindSeclusionScenarioButton(seclusion);
            controller.BindFamilyDoctrineScenarioButton(doctrine);

            DemoSummaryPanel panel = _sessionObject.AddComponent<DemoSummaryPanel>();
            panel.Bind(_session, summary);

            rootObject.SetActive(false);
        }

        private Text CreateText(Transform parent, string name, string value)
        {
            GameObject go = Create(name, parent, typeof(Text));
            Text text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            return text;
        }

        private Button CreateButton(Transform parent, string name, string label)
        {
            GameObject go = Create(name, parent, typeof(Image), typeof(Button));
            Button button = go.GetComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            Text text = CreateText(go.transform, "Label", label);
            text.rectTransform.sizeDelta = new Vector2(200f, 40f);
            return button;
        }

        private Transform Find(string name) => _scene.GetRootGameObjects()
            .SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .Where(t => t.name == name)
            .SingleOrDefault();

        private int ObjectCount() =>
            _scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Transform>(true).Length);

        private GameObject Create(string name, Transform parent, params System.Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, _scene);
            if (parent != null)
                go.transform.SetParent(parent, false);
            foreach (System.Type component in components)
                go.AddComponent(component);
            return go;
        }

        private static Sprite LoadSprite(string path) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static Sprite LoadAnySprite(string path)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite)
                    return sprite;
            return null;
        }

        private static Object GetReference(Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> ScenarioButtonFieldNames =
            new System.Collections.Generic.Dictionary<string, string>
            {
                { AddM6TitleScreen.StandardSampleButtonName, "standardSampleButton" },
                { AddM6TitleScreen.CrisisDemoButtonName, "crisisDemoButton" },
                { AddM6TitleScreen.EducationDemoButtonName, "educationDemoButton" },
                { AddM6TitleScreen.ResearchDemoButtonName, "researchDemoButton" },
                { AddM6TitleScreen.FamilyFormationDemoButtonName, "familyFormationDemoButton" },
                { AddM6TitleScreen.SeclusionDemoButtonName, "seclusionDemoButton" },
                { AddM6TitleScreen.FamilyDoctrineDemoButtonName, "familyDoctrineDemoButton" }
            };
    }
}
