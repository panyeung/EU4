using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Presentation;
using Object = UnityEngine.Object;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Isolated-scene coverage for the town overview updater. One run builds the page, a
    /// second run converges without duplicating it, and an injected fault restores the page
    /// rect, the two town entries, and the return control. The user's FoundationScene is
    /// never opened or saved.
    /// </summary>
    public sealed class UpdateTownOverviewTests
    {
        private Scene scene;
        private Scene previousScene;
        private bool previousDirty;
        private Transform town;
        private WorldMapController controller;
        private Button returnButton;
        private Button commissionEntry;
        private Button tacticalEntry;
        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            previousScene = SceneManager.GetActiveScene();
            previousDirty = previousScene.isDirty;
            scene = EditorSceneManager.NewPreviewScene();
            BuildFixture(scene);
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid())
                EditorSceneManager.ClosePreviewScene(scene);
            Assert.That(SceneManager.GetActiveScene(), Is.EqualTo(previousScene));
            Assert.That(previousScene.isDirty, Is.EqualTo(previousDirty),
                "Tests must preserve the user's active scene.");
        }

        [Test]
        public void Update_BuildsTheLocalActionColumnAndPlacesTownEntries()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            Assert.That(UpdateTownOverview.Validate(scene, out string error), Is.True, error);

            Transform overview = town.Find("TownOverviewRoot");
            Assert.That(overview, Is.Not.Null);
            Text activity = overview.Find("LocalActions")?.GetComponent<Text>();
            Assert.That(activity, Is.Not.Null, "The right column must be the local-action summary.");
            Assert.That(activity.text, Does.Contain("当地委托"));
            Assert.That(overview.Find("Rumors"), Is.Null, "The rumor placeholder must be gone.");

            var commissionRect = (RectTransform)commissionEntry.transform;
            var tacticalRect = (RectTransform)tacticalEntry.transform;
            Assert.That(commissionRect.anchoredPosition, Is.EqualTo(new Vector2(-28f, -296f)));
            Assert.That(tacticalRect.anchoredPosition, Is.EqualTo(new Vector2(-28f, -340f)));
            Assert.That(commissionRect.sizeDelta, Is.EqualTo(new Vector2(300f, 40f)));
            Assert.That(tacticalRect.sizeDelta, Is.EqualTo(new Vector2(300f, 40f)));

            Assert.That(overview.GetSiblingIndex(),
                Is.LessThan(commissionEntry.transform.parent.GetSiblingIndex()),
                "The opaque page must draw behind the commission entry.");
            Assert.That(overview.GetSiblingIndex(),
                Is.LessThan(tacticalEntry.transform.parent.GetSiblingIndex()),
                "The opaque page must draw behind the tactical entry.");
            Assert.That(returnButton.transform.GetSiblingIndex(),
                Is.EqualTo(town.childCount - 1), "The return control must stay on top.");

            var panel = overview.GetComponent<TownOverviewPanel>();
            Assert.That(GetReference(controller, "townOverviewPanel"), Is.SameAs(panel));
        }

        [Test]
        public void Update_BuildsRefreshableActivityButtonsAndRoutesTheSelectedEntry()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));

            Transform overview = town.Find("TownOverviewRoot");
            Transform activityButtons = overview.Find("TownActivityButtons");
            Assert.That(activityButtons, Is.Not.Null,
                "The town page must provide one fixed root for its local activity buttons.");
            if (activityButtons == null) return;
            Button template = activityButtons.Find("ActivityButtonTemplate")?.GetComponent<Button>();
            Assert.That(template, Is.Not.Null, "The fixed root must provide a reusable button template.");
            if (template == null) return;
            Assert.That(template.gameObject.activeSelf, Is.False,
                "The template must not appear as a duplicate activity.");

            controller.OpenTownMenu();
            var missionPanel = Create("SupplyShortageMissionPanel", null, typeof(SupplyShortageMissionPanel))
                .GetComponent<SupplyShortageMissionPanel>();
            controller.BindSupplyShortageMissionPanel(missionPanel);

            TownOverviewPanel panel = overview.GetComponent<TownOverviewPanel>();
            panel.Refresh();
            Button[] buttons = activityButtons.GetComponentsInChildren<Button>()
                .Where(button => button != template && button.gameObject.activeInHierarchy).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "补给短缺：领取", "密信：领取", "护送委托：领取", "潜入密令：开始"
            }, buttons.Select(button => button.GetComponentInChildren<Text>(true).text).ToArray());
            if (buttons.Length == 0) return;

            int childCount = activityButtons.childCount;
            panel.Refresh();
            panel.Refresh();
            Assert.That(activityButtons.childCount, Is.EqualTo(childCount),
                "Refreshing must reuse the four activity controls instead of accumulating children.");
            Assert.That(buttons.All(button =>
                    (button.gameObject.hideFlags & HideFlags.DontUnloadUnusedAsset) == 0), Is.True,
                "Runtime activity controls must be unloadable with their town scene.");

            buttons[0].onClick.Invoke();
            Assert.That(missionPanel.IsOpen, Is.True,
                "The first visible activity must open its existing mission page.");
        }

        [Test]
        public void Update_RefreshDoesNotDuplicateMissionButtonToggle()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            controller.OpenTownMenu();
            var missionPanel = Create("SupplyShortageMissionPanel", null,
                typeof(SupplyShortageMissionPanel)).GetComponent<SupplyShortageMissionPanel>();
            controller.BindSupplyShortageMissionPanel(missionPanel);
            TownOverviewPanel panel = town.Find("TownOverviewRoot").GetComponent<TownOverviewPanel>();

            panel.Refresh();
            panel.Refresh();
            town.Find("TownOverviewRoot/MissionButton").GetComponent<Button>().onClick.Invoke();

            Assert.That(missionPanel.IsOpen, Is.True,
                "One click after repeated refreshes must toggle the mission panel only once.");
        }

        [Test]
        public void Update_RefreshHidesActivitiesThatAreUnavailableAfterTravel()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            controller.OpenTownMenu();

            Transform activityRoot = town.Find("TownOverviewRoot/TownActivityButtons");
            Button template = activityRoot.Find("ActivityButtonTemplate").GetComponent<Button>();
            TownOverviewPanel panel = town.Find("TownOverviewRoot").GetComponent<TownOverviewPanel>();
            panel.Refresh();
            Assert.That(Cards(activityRoot, template), Has.Length.EqualTo(4));

            Assert.That(session.TryAcceptSupplyShortageMission().Succeeded, Is.True);
            TravelTo(LocationId.Atsuta);
            panel.Refresh();

            Button[] visible = Cards(activityRoot, template);
            Assert.That(visible, Has.Length.EqualTo(1),
                "A location change must hide cached activities that are no longer available.");
            Assert.That(visible[0].GetComponentInChildren<Text>(true).text,
                Is.EqualTo("补给短缺：调查"));
            Assert.That(activityRoot.childCount, Is.EqualTo(5),
                "Refresh should reuse the four cached cards while hiding the three stale ones.");
        }

        [Test]
        public void Update_SecondRunConvergesWithoutDuplicatingThePage()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            int count = All().Length;
            int overviewIndex = town.Find("TownOverviewRoot").GetSiblingIndex();
            int commissionIndex = commissionEntry.transform.parent.GetSiblingIndex();

            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            Assert.That(All().Length, Is.EqualTo(count));
            Assert.That(All().Count(x => x.name == "TownOverviewRoot"), Is.EqualTo(1));
            Assert.That(town.Find("TownOverviewRoot").GetSiblingIndex(), Is.EqualTo(overviewIndex));
            Assert.That(commissionEntry.transform.parent.GetSiblingIndex(),
                Is.EqualTo(commissionIndex));
            Assert.That(UpdateTownOverview.Validate(scene, out string error), Is.True, error);
        }

        [Test]
        public void Update_NormalizesScaledExistingPageChildren()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            Transform page = town.Find("TownOverviewRoot");
            RectTransform[] pageRects = page.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rect in pageRects)
                rect.localScale = Vector3.one * 0.5f;

            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));

            foreach (RectTransform rect in page.GetComponentsInChildren<RectTransform>(true))
                Assert.That(rect.localScale, Is.EqualTo(Vector3.one),
                    $"Regeneration must normalize '{rect.name}' so saved UI scale is stable.");
            Assert.That(UpdateTownOverview.Validate(scene, out string error), Is.True, error);
        }

        [Test]
        public void Update_RollsBackThePageAndTheThreeTownLevelControlsOnFailure()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            Transform existingPage = town.Find("TownOverviewRoot");
            var pageRect = (RectTransform)existingPage;
            var activityButtonsRect = (RectTransform)existingPage.Find("TownActivityButtons");
            pageRect.localScale = Vector3.one * 1.5f;
            activityButtonsRect.localScale = Vector3.one * 0.5f;
            TownOverviewPanel existingPanel = existingPage.GetComponent<TownOverviewPanel>();
            Vector3 pageScale = pageRect.localScale;
            Vector3 activityButtonsScale = activityButtonsRect.localScale;
            Vector2 returnPosition = ((RectTransform)returnButton.transform).anchoredPosition;
            Vector2 returnSize = ((RectTransform)returnButton.transform).sizeDelta;
            int returnSibling = returnButton.transform.GetSiblingIndex();
            Vector2 commissionPosition = ((RectTransform)commissionEntry.transform).anchoredPosition;
            Vector2 tacticalPosition = ((RectTransform)tacticalEntry.transform).anchoredPosition;

            UpdateTownOverview.UpdateResult result = UpdateTownOverview.Update(scene,
                () => throw new InvalidOperationException("injected layout failure"));

            Assert.That(result, Is.EqualTo(UpdateTownOverview.UpdateResult.Failed));
            Assert.That(town.Find("TownOverviewRoot"), Is.SameAs(existingPage));
            Assert.That(pageRect.localScale, Is.EqualTo(pageScale),
                "A refused update must restore an existing page's local scale.");
            Assert.That(activityButtonsRect.localScale, Is.EqualTo(activityButtonsScale),
                "A refused update must restore normalized descendant scales.");
            Assert.That(((RectTransform)returnButton.transform).anchoredPosition,
                Is.EqualTo(returnPosition));
            Assert.That(((RectTransform)returnButton.transform).sizeDelta, Is.EqualTo(returnSize));
            Assert.That(returnButton.transform.GetSiblingIndex(), Is.EqualTo(returnSibling));
            Assert.That(((RectTransform)commissionEntry.transform).anchoredPosition,
                Is.EqualTo(commissionPosition));
            Assert.That(((RectTransform)tacticalEntry.transform).anchoredPosition,
                Is.EqualTo(tacticalPosition));
            Assert.That(GetReference(controller, "townOverviewPanel"), Is.SameAs(existingPanel));
        }

        [Test]
        public void Update_RollsBackAFirstPageCreationWhenLayoutFails()
        {
            Vector2 returnPosition = ((RectTransform)returnButton.transform).anchoredPosition;
            Vector2 commissionPosition = ((RectTransform)commissionEntry.transform).anchoredPosition;
            Vector2 tacticalPosition = ((RectTransform)tacticalEntry.transform).anchoredPosition;
            bool failureReachedBoundState = false;

            UpdateTownOverview.UpdateResult result = UpdateTownOverview.Update(scene,
                () =>
                {
                    TownOverviewPanel createdPanel = town.Find("TownOverviewRoot")
                        ?.GetComponent<TownOverviewPanel>();
                    failureReachedBoundState = createdPanel != null &&
                                               GetReference(controller, "townOverviewPanel") == createdPanel;
                    throw new InvalidOperationException("injected first-page integration failure");
                });

            Assert.That(result, Is.EqualTo(UpdateTownOverview.UpdateResult.Failed));
            Assert.That(failureReachedBoundState, Is.True,
                "The injected failure must occur after the new panel is bound to its controller.");
            Assert.That(town.Find("TownOverviewRoot"), Is.Null,
                "A failed first generation must remove the page it created.");
            Assert.That(GetReference(controller, "townOverviewPanel"), Is.Null);
            Assert.That(((RectTransform)returnButton.transform).anchoredPosition,
                Is.EqualTo(returnPosition));
            Assert.That(((RectTransform)commissionEntry.transform).anchoredPosition,
                Is.EqualTo(commissionPosition));
            Assert.That(((RectTransform)tacticalEntry.transform).anchoredPosition,
                Is.EqualTo(tacticalPosition));
        }

        [Test]
        public void Validate_RefusesAnActivityAreaThatCannotHoldEveryKind()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));

            // A saved scene carries no runtime cards, so the area height alone has to prove
            // that every kind still fits when the page is next opened.
            var activityArea = (RectTransform)town.Find("TownOverviewRoot/TownActivityButtons");
            Button template = activityArea.Find("ActivityButtonTemplate").GetComponent<Button>();
            foreach (Button card in Cards(activityArea, template))
                Object.DestroyImmediate(card.gameObject);
            Assert.That(UpdateTownOverview.Validate(scene, out string baselineError), Is.True,
                baselineError);

            activityArea.sizeDelta = new Vector2(activityArea.sizeDelta.x,
                TownOverviewPanel.ActivityAreaHeight - 20f);

            Assert.That(UpdateTownOverview.Validate(scene, out string error), Is.False,
                "The 176px area that clipped the fifth card must be refused.");
            Assert.That(error, Does.Contain("activity cards"));
        }

        [Test]
        public void Validate_RefusesNonUnitScaleWithinTheTownPage()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            var activityRoot = (RectTransform)town.Find("TownOverviewRoot/TownActivityButtons");
            activityRoot.localScale = Vector3.one * 0.5f;

            Assert.That(UpdateTownOverview.Validate(scene, out string error), Is.False,
                "The validator must reject a scaled card root even when its unscaled rect fits.");
            Assert.That(error, Does.Contain("scale"));
        }

        [Test]
        public void ActivityCards_AreOwnedByTheActivityRootForTheirWholeLifetime()
        {
            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            controller.OpenTownMenu();
            var missionPanel = Create("SupplyShortageMissionPanel", null,
                typeof(SupplyShortageMissionPanel)).GetComponent<SupplyShortageMissionPanel>();
            controller.BindSupplyShortageMissionPanel(missionPanel);

            Transform page = town.Find("TownOverviewRoot");
            Transform activityRoot = page.Find("TownActivityButtons");
            Button template = activityRoot.Find("ActivityButtonTemplate").GetComponent<Button>();
            TownOverviewPanel panel = page.GetComponent<TownOverviewPanel>();
            panel.Refresh();
            Button[] cards = Cards(activityRoot, template);
            Assert.That(cards.Length, Is.EqualTo(4));
            Assert.That(cards.All(card => card.transform.parent == activityRoot), Is.True,
                "Runtime cards must be children of the activity root, which owns their lifetime.");
            var saveExclusion = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            Assert.That(cards.SelectMany(card => card.GetComponentsInChildren<Transform>(true))
                    .All(child => (child.gameObject.hideFlags & saveExclusion) == saveExclusion), Is.True,
                "Every cloned card object, including labels, must stay out of scene serialization.");

            // A runtime card can be destroyed without the panel being told, so the next refresh
            // must replace a dead reference instead of reusing it.
            Object.DestroyImmediate(cards[0].gameObject);
            panel.Refresh();
            Button[] recreated = Cards(activityRoot, template);
            Assert.That(recreated.Length, Is.EqualTo(4),
                "A reclaimed activity card must be recreated on the next refresh.");
            Assert.That(recreated[0].gameObject.name, Is.EqualTo("TownActivity_SupplyShortage"));

            // No separate cleanup runs: destroying the page destroys the activity root and
            // every card under it.
            Object.DestroyImmediate(page.gameObject);
            Assert.That(activityRoot == null, Is.True,
                "The activity root belongs to the town page.");
            Assert.That(recreated.All(card => card == null), Is.True,
                "Every runtime card must be destroyed together with its activity root.");
        }

        [Test]
        public void CompactSummary_ReportsTheCountInsideTheLegacySummaryArea()
        {
            TravelTo(LocationId.Nagoya);
            Assert.That(session.TryAcceptMarketTallyMission().Succeeded, Is.True);
            TravelTo(LocationId.Kiyosu);

            Transform page = Create("LegacyTownOverviewRoot", town).transform;
            var labelRect = (RectTransform)Create("LocalActions", page, typeof(Text)).transform;
            labelRect.anchorMin = labelRect.anchorMax = labelRect.pivot = new Vector2(1f, 0f);
            labelRect.anchoredPosition = new Vector2(-28f, 80f);
            labelRect.sizeDelta = new Vector2(300f, 80f);
            Text summary = labelRect.GetComponent<Text>();
            summary.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            summary.fontSize = 17;
            summary.horizontalOverflow = HorizontalWrapMode.Wrap;
            summary.verticalOverflow = VerticalWrapMode.Overflow;

            // A page with no card root and no template: the legacy page the panel still
            // supports, whose only summary label is 80px tall.
            var panel = page.gameObject.AddComponent<TownOverviewPanel>();
            panel.BindReferences(null, null, null, null, summary, null, null, null);
            panel.Bind(controller);

            Assert.That(summary.text, Is.EqualTo("当地委托\n5 项可办理"),
                "A cardless page must not list five activity labels in its compact summary.");
            Assert.That(summary.preferredHeight, Is.LessThanOrEqualTo(labelRect.rect.height),
                "The compact summary must fit the legacy 80px area instead of overflowing it.");
        }

        [TestCase(1280f, 720f, 1.0f)]
        [TestCase(1920f, 1080f, 1.5f)]
        public void Update_KeepsTheColumnAndEntriesInsideTheTownBandAtEachTargetSize(
            float width, float height, float expectedScale)
        {
            TravelTo(LocationId.Nagoya);
            Assert.That(session.TryAcceptMarketTallyMission().Succeeded, Is.True);
            TravelTo(LocationId.Kiyosu);

            var canvasRect = (RectTransform)town.parent.parent;
            CanvasScaler scaler = canvasRect.GetComponent<CanvasScaler>();
            float canvasScale = Mathf.Pow(2f, 0.5f * Mathf.Log(width / 1280f, 2f) +
                                          0.5f * Mathf.Log(height / 720f, 2f));
            Assert.That(canvasScale, Is.EqualTo(expectedScale).Within(0.001f));
            Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
            Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1280f, 720f)));
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            canvasRect.localScale = Vector3.one * canvasScale;

            Assert.That(UpdateTownOverview.Update(scene),
                Is.EqualTo(UpdateTownOverview.UpdateResult.Success));
            Assert.That(UpdateTownOverview.Validate(scene, out string error), Is.True, error);
            controller.OpenTownMenu();

            var band = (RectTransform)town;
            Transform overview = town.Find("TownOverviewRoot");
            var art = (RectTransform)overview.Find("TownArt");
            var activity = (RectTransform)overview.Find("LocalActions");
            Assert.That(((RectTransform)overview).localScale, Is.EqualTo(Vector3.one),
                "The generated town page must inherit the town band's UI scale.");
            Transform activityRoot = overview.Find("TownActivityButtons");
            Assert.That(activityRoot, Is.Not.Null);
            if (activityRoot == null) return;
            Button template = activityRoot.Find("ActivityButtonTemplate")?.GetComponent<Button>();
            Button[] activityButtons = activityRoot.GetComponentsInChildren<Button>(true)
                .Where(button => button != template && button.gameObject.activeInHierarchy).ToArray();
            Assert.That(activityButtons.Length, Is.EqualTo(5),
                "Kiyosu's five available activities must all receive a visible control.");
            var activityArea = (RectTransform)activityRoot;
            Assert.That(activityArea.rect.height,
                Is.EqualTo(TownOverviewPanel.ActivityAreaHeight).Within(0.5f),
                "The fixed activity area must be sized from the panel's shared card metrics.");
            var mission = (RectTransform)overview.Find("MissionButton");
            var commission = (RectTransform)commissionEntry.transform;
            var tactical = (RectTransform)tacticalEntry.transform;

            foreach (RectTransform item in new[] { art, activity, mission, commission, tactical })
                Assert.That(Contained(item, band), Is.True,
                    $"{item.name} must stay inside the town band at {width}x{height}.");
            Assert.That(Overlaps(art, activity), Is.False);
            Assert.That(Overlaps(activity, mission), Is.False);
            Assert.That(Overlaps(mission, commission), Is.False);
            Assert.That(Overlaps(commission, tactical), Is.False);
            foreach (Button button in activityButtons)
            {
                var rect = (RectTransform)button.transform;
                Assert.That(Contained(rect, band), Is.True,
                    $"{button.name} must stay inside the town band at {width}x{height}.");
                Assert.That(Contained(rect, activityArea), Is.True,
                    $"{button.name} must stay inside the {activityArea.rect.height}px activity " +
                    "area, not merely inside the 636px town band; the fifth card overflowed " +
                    "the former 176px area.");
                Assert.That(Overlaps(rect, activity), Is.False,
                    $"{button.name} must not cover the activity heading.");
                Assert.That(Overlaps(rect, mission), Is.False,
                    $"{button.name} must not cover the mission-board entry.");
            }
            for (int i = 0; i < activityButtons.Length; i++)
            for (int j = i + 1; j < activityButtons.Length; j++)
                Assert.That(Overlaps((RectTransform)activityButtons[i].transform,
                    (RectTransform)activityButtons[j].transform), Is.False,
                    "Town activity buttons must not overlap one another.");
        }

        private static Button[] Cards(Transform activityRoot, Button template) =>
            activityRoot.GetComponentsInChildren<Button>(true)
                .Where(button => button != template && button.gameObject.activeSelf).ToArray();

        private static bool Contained(RectTransform child, RectTransform parent)        {
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            return corners.All(corner =>
            {
                Vector3 point = parent.InverseTransformPoint(corner);
                return point.x >= parent.rect.xMin - 0.1f && point.x <= parent.rect.xMax + 0.1f &&
                       point.y >= parent.rect.yMin - 0.1f && point.y <= parent.rect.yMax + 0.1f;
            });
        }

        private static bool Overlaps(RectTransform first, RectTransform second)
        {
            var firstCorners = new Vector3[4];
            var secondCorners = new Vector3[4];
            first.GetWorldCorners(firstCorners);
            second.GetWorldCorners(secondCorners);
            float minX = Mathf.Max(firstCorners.Min(c => c.x), secondCorners.Min(c => c.x));
            float maxX = Mathf.Min(firstCorners.Max(c => c.x), secondCorners.Max(c => c.x));
            float minY = Mathf.Max(firstCorners.Min(c => c.y), secondCorners.Min(c => c.y));
            float maxY = Mathf.Min(firstCorners.Max(c => c.y), secondCorners.Max(c => c.y));
            return maxX - minX > 0.5f && maxY - minY > 0.5f;
        }

        private void BuildFixture(Scene target)
        {
            scene = target;
            GameObject canvas = Create("Canvas", null, typeof(Canvas));
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1280f, 720f);
            CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            GameObject map = Create("M2WorldMapRoot", canvas.transform, typeof(WorldMapController));
            var mapRect = map.GetComponent<RectTransform>();
            mapRect.anchorMin = mapRect.anchorMax = new Vector2(0.5f, 0.5f);
            mapRect.sizeDelta = new Vector2(1060f, 636f);
            town = Create("TownMenuRoot", map.transform).transform;
            Stretch((RectTransform)town);
            town.gameObject.SetActive(false);
            controller = map.GetComponent<WorldMapController>();

            returnButton = Create("ReturnToMapButton", town, typeof(Image), typeof(Button))
                .GetComponent<Button>();
            var returnRect = (RectTransform)returnButton.transform;
            returnRect.anchorMin = returnRect.anchorMax = new Vector2(0.5f, 0f);
            returnRect.pivot = new Vector2(0.5f, 0f);
            returnRect.anchoredPosition = new Vector2(300f, 42f);
            returnRect.sizeDelta = new Vector2(260f, 60f);
            Create("Label", returnButton.transform, typeof(Text)).GetComponent<Text>().text = "返回地图";

            Transform commissionRoot = Create("M5ShadowEscortCommissionRoot", town).transform;
            Stretch((RectTransform)commissionRoot);
            commissionEntry = CreateEntry("M5CommissionOpenButton", commissionRoot, -16f);
            Transform tacticalRoot = Create("M6TacticalMissionRoot", town).transform;
            Stretch((RectTransform)tacticalRoot);
            tacticalEntry = CreateEntry("M6TacticalOpenButton", tacticalRoot, -100f);

            session = Create("GameSession", null, typeof(GameSession)).GetComponent<GameSession>();
            session.InitNewGame();
            SetReference(controller, "gameSession", session);
            SetReference(controller, "townMenuRoot", town.gameObject);
        }

        private Button CreateEntry(string name, Transform parent, float top)
        {
            Button entry = Create(name, parent, typeof(Image), typeof(Button)).GetComponent<Button>();
            Create("Label", entry.transform, typeof(Text)).GetComponent<Text>().text = name;
            var rect = (RectTransform)entry.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, top);
            rect.sizeDelta = new Vector2(220f, 40f);
            return entry;
        }

        private void TravelTo(LocationId destination)
        {
            TravelPreview? preview = session.PreviewTravel(destination);
            Assert.That(preview.HasValue, Is.True,
                $"Expected travel from {session.CurrentLocation} to {destination}.");
            Assert.That(session.ConfirmTravel(preview.Value), Is.True);
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

        private Transform[] All() => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Object GetReference(Object target, string field) =>
            new SerializedObject(target).FindProperty(field).objectReferenceValue;

        private static void SetReference(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
