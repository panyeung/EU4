using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Editor;
using SengokuChronicle.Events;
using SengokuChronicle.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
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
            GameObject formationRoot = Create("SC022FamilyFormationRoot", null);
            CreateLauncherButton(AddSC022FamilyFormation.OpenButtonName,
                formationRoot.transform, "婚姻与收养");
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
        public void Navigator_ReclickingTheCharacterTabReopensAClosedPageOnlyOnce()
        {
            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            navigator.ShowPage(ReferenceGalleryPage.Character);
            DailyActionPanel panel = Panel();
            Assert.That(panel.IsOpen, Is.True);
            int opens = panel.OpenCount;

            panel.Close();
            Assert.That(panel.IsOpen, Is.False);

            navigator.ShowPage(ReferenceGalleryPage.Character);
            Assert.That(panel.IsOpen, Is.True,
                "re-clicking the active Character tab restores its closed page");
            Assert.That(panel.OpenCount, Is.EqualTo(opens + 1),
                "a closed page is reopened exactly once");

            navigator.ShowPage(ReferenceGalleryPage.Character);
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.OpenCount, Is.EqualTo(opens + 1),
                "an already-visible Character page is not reopened again");
        }

        [Test]
        public void Navigator_ReturnButtonLeavesANonEmptyPageWithoutASecondClick()
        {
            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            Button characterTab = Find(AddM02DailyActionPanel.NavCharacterButtonName)
                .GetComponent<Button>();
            Button returnButton = Find(AddM02DailyActionPanel.CloseButtonName)
                .GetComponent<Button>();
            DailyActionPanel panel = Panel();

            // Enter the page through the bound tab listener, exactly as a player click does.
            characterTab.onClick.Invoke();
            Assert.That(navigator.CurrentPage, Is.EqualTo(ReferenceGalleryPage.Character));
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(Find(AddM02DailyActionPanel.PanelName).gameObject.activeSelf, Is.True,
                "the Character tab click populates the page");

            GameDate date = _session.WorldClock.CurrentDate;
            int actions = _session.DailyActions.Count;

            // "返回" is the bound Close listener; it must leave a real page behind.
            returnButton.onClick.Invoke();

            Assert.That(navigator.CurrentPage, Is.EqualTo(ReferenceGalleryPage.Household),
                "Return leaves the Character page instead of blanking the centre");
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(Find(AddM02DailyActionPanel.PanelName).gameObject.activeSelf, Is.False);
            Assert.That(Find(AddSC019IndustryPanel.PanelName).gameObject.activeSelf, Is.True,
                "the page that replaces the dossier is the populated household overview");
            Assert.That(Find(AddM02DailyActionPanel.NavCharacterButtonName)
                    .GetComponent<Button>().targetGraphic.color,
                Is.Not.EqualTo(ReferenceGalleryNavigator.PageSelectedBackground),
                "no tab keeps the selected accent for the page that just closed");
            Assert.That(Find(AddM02DailyActionPanel.NavHouseholdButtonName)
                    .GetComponent<Button>().targetGraphic.color,
                Is.EqualTo(ReferenceGalleryNavigator.PageSelectedBackground),
                "the selected accent follows the page that is actually shown");

            // The page stays navigable: one more tab click re-enters it, no extra step.
            characterTab.onClick.Invoke();
            Assert.That(navigator.CurrentPage, Is.EqualTo(ReferenceGalleryPage.Character));
            Assert.That(panel.IsOpen, Is.True);
            Assert.That(_session.WorldClock.CurrentDate, Is.EqualTo(date),
                "returning and re-entering never advances the clock");
            Assert.That(_session.DailyActions.Count, Is.EqualTo(actions),
                "returning never mutates the M02 action roster");
            Assert.That(panel.ActionSubmitCount, Is.Zero,
                "returning never submits a daily action");
        }

        [Test]
        public void CharacterRoster_RendersAboveRailAndHidesWhenLeavingCharacterPage()
        {
            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            navigator.ShowPage(ReferenceGalleryPage.Character);
            Transform rail = Find(AddM02DailyActionPanel.NavRailName);
            Transform roster = Find(AddM02DailyActionPanel.ScrollName);
            Transform originalRoster = roster;

            Assert.That(roster.parent, Is.SameAs(rail),
                "a roster inside the earlier page panel is covered by the later opaque rail");
            Assert.That(roster.GetSiblingIndex(), Is.GreaterThan(
                rail.Find(ReferenceGalleryNavigator.NavPatternChildName).GetSiblingIndex()));
            var rect = (RectTransform)roster;
            Assert.That(-rect.anchoredPosition.y, Is.GreaterThan(322f),
                "the list must start below the rail's roster header and rule");
            Assert.That(-rect.anchoredPosition.y + rect.rect.height,
                Is.LessThanOrEqualTo(((RectTransform)rail).rect.height));

            navigator.ShowPage(ReferenceGalleryPage.Map);
            Assert.That(rail.gameObject.activeSelf, Is.True);
            Assert.That(roster.gameObject.activeSelf, Is.False,
                "the shared rail stays visible, but character rows must leave other pages");
            navigator.ShowPage(ReferenceGalleryPage.Character);
            navigator.ShowPage(ReferenceGalleryPage.Character);
            Assert.That(Find(AddM02DailyActionPanel.ScrollName), Is.SameAs(originalRoster));
            Assert.That(roster.gameObject.activeInHierarchy, Is.True);
            Find("M02DailyActionRow_" + M2FixedScenario.MasatsunaId.Value)
                .GetComponent<Button>().onClick.Invoke();
            Assert.That(Panel().SelectedPersonId, Is.EqualTo(M2FixedScenario.MasatsunaId));
            Assert.That(View().ProfileNameLine, Is.EqualTo(_session.FamilyRoster.Single(
                person => person.PersonId == M2FixedScenario.MasatsunaId).DisplayName));
        }

        [Test]
        public void Navigator_RepairsTheFamilyFormationRegistrationAndOpensItsPanel()
        {
            // Replace the bare fixture root with the real SC-022 hierarchy so the test
            // exercises the launcher's actual panel-open binding, not a stand-in button.
            UnityEngine.Object.DestroyImmediate(Find("SC022FamilyFormationRoot").gameObject);
            Assert.That(AddSC022FamilyFormation.Update(_scene),
                Is.EqualTo(AddSC022FamilyFormation.UpdateResult.Success));

            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string stale), Is.False,
                "a root added after the page was generated must invalidate the registration");
            Assert.That(stale, Does.Contain("stale"));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success),
                "the stale page-root registration must be repaired, not reported as valid");
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string repaired), Is.True, repaired);

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            Transform formation = Find("SC022FamilyFormationRoot");
            Button launcher = Find(AddSC022FamilyFormation.OpenButtonName).GetComponent<Button>();
            FamilyFormationPanel panel = formation.GetComponent<FamilyFormationPanel>();
            Assert.That(panel.IsOpen, Is.False);

            navigator.ShowPage(ReferenceGalleryPage.Family);
            Assert.That(formation.gameObject.activeSelf, Is.True,
                "SC022FamilyFormationRoot must map to ReferenceGalleryPageMask.Family, not None");
            Assert.That(launcher.gameObject.activeInHierarchy, Is.True,
                "the SC-022 launcher must stay clickable on the Family page");
            Assert.That(launcher.targetGraphic, Is.Not.Null);

            launcher.onClick.Invoke();
            Assert.That(panel.IsOpen, Is.True,
                "the Family-page launcher must open the SC-022 panel through its bound listener");

            navigator.ShowPage(ReferenceGalleryPage.Household);
            Assert.That(formation.gameObject.activeSelf, Is.False,
                "the SC-022 entry belongs to the Family page only");
            Assert.That(launcher.gameObject.activeInHierarchy, Is.False,
                "the SC-022 launcher must be unreachable once the Family page is left");
            Assert.That(panel.gameObject.activeInHierarchy, Is.False,
                "the SC-022 panel must not stay on screen after the Family page closes it");
        }

        [Test]
        public void Navigator_LeavesTheFullScreenOverlaysUnregistered()
        {
            Transform canvas = Find("Canvas");
            GameObject review = Create(CampaignReviewPanel.RootObjectName, canvas);
            GameObject tactical = Create(TacticalMissionPanel.OverlayObjectName, canvas);
            GameObject title = Create("M6TitleRoot", canvas);
            GameObject doctrine = Create(AddSC024FamilyDoctrine.RootName, canvas);
            doctrine.SetActive(false);

            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            string[] registered = RegisteredPageRootNames(navigator);
            Assert.That(registered, Does.Not.Contain(CampaignReviewPanel.RootObjectName));
            Assert.That(registered, Does.Not.Contain(TacticalMissionPanel.OverlayObjectName));
            Assert.That(registered, Does.Not.Contain("M6TitleRoot"));
            Assert.That(registered, Does.Not.Contain(AddSC024FamilyDoctrine.RootName));

            foreach (ReferenceGalleryPage page in Enum.GetValues(typeof(ReferenceGalleryPage)))
            {
                navigator.ShowPage(page);
                Assert.That(review.activeSelf, Is.True,
                    CampaignReviewPanel.RootObjectName + " must stay active on " + page);
                Assert.That(tactical.activeSelf, Is.True,
                    TacticalMissionPanel.OverlayObjectName + " must stay active on " + page);
                Assert.That(title.activeSelf, Is.True,
                    "M6TitleRoot is a lifecycle overlay the navigator must not deactivate on " + page);
                Assert.That(doctrine.activeSelf, Is.False,
                    "the launcher-owned doctrine body must stay closed on " + page);
            }
        }

        [Test]
        public void Navigator_ShowsTheFeatureEntriesOnTheirTabsOnly()
        {
            Transform canvas = Find("Canvas");
            Button educationOpen = CreateFeatureLauncher(AddSC020CharacterEducation.RootName,
                AddSC020CharacterEducation.OpenButtonName, canvas, "子女培养");
            Button researchOpen = CreateFeatureLauncher(AddSC021FamilyResearch.RootName,
                AddSC021FamilyResearch.OpenButtonName, canvas, "家族研究");
            Button doctrineOpen = CreateLauncherButton(AddSC024FamilyDoctrine.OpenButtonName,
                canvas, "家训与抱负");

            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();

            navigator.ShowPage(ReferenceGalleryPage.Character);
            Assert.That(educationOpen.gameObject.activeInHierarchy, Is.True,
                "the SC-020 education entry belongs on the Character page");
            Assert.That(researchOpen.gameObject.activeInHierarchy, Is.False);
            Assert.That(doctrineOpen.gameObject.activeInHierarchy, Is.False);

            navigator.ShowPage(ReferenceGalleryPage.Family);
            Assert.That(educationOpen.gameObject.activeInHierarchy, Is.False,
                "the education entry must leave with the Character page");
            Assert.That(researchOpen.gameObject.activeInHierarchy, Is.True,
                "the SC-021 research entry belongs on the Family page");
            Assert.That(doctrineOpen.gameObject.activeInHierarchy, Is.True,
                "the SC-024 doctrine launcher belongs on the Family page");

            navigator.ShowPage(ReferenceGalleryPage.Household);
            Assert.That(educationOpen.gameObject.activeInHierarchy, Is.False);
            Assert.That(researchOpen.gameObject.activeInHierarchy, Is.False);
            Assert.That(doctrineOpen.gameObject.activeInHierarchy, Is.False);
        }

        [Test]
        public void Validator_RejectsAndRepairsStaleMasksAndDuplicateRegistrations()
        {
            Transform canvas = Find("Canvas");
            Find("SC022FamilyFormationRoot").SetParent(canvas, false);
            Find("FamilyRosterRoot").SetParent(canvas, false);
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            SerializedProperty roots = new SerializedObject(navigator).FindProperty("pageRoots");
            Assert.That(roots.arraySize, Is.EqualTo(2),
                "the fixture registers exactly the SC-022 and family-roster page roots");

            // A serialized mask that no longer matches the mapping is rejected, then repaired
            // so the SC-022 entry returns to the Family page.
            int formationIndex = RegisteredRootIndex(roots, "SC022FamilyFormationRoot");
            Assert.That(formationIndex, Is.GreaterThanOrEqualTo(0));
            SetRegisteredMask(roots, formationIndex, ReferenceGalleryPageMask.None);
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string masked), Is.False,
                "a root registered as None is never shown on any page");
            Assert.That(masked, Does.Contain("stale"));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string repaired), Is.True, repaired);

            // Two entries for one root keep the count right while hiding another page root.
            navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            roots = new SerializedObject(navigator).FindProperty("pageRoots");
            int rosterIndex = RegisteredRootIndex(roots, "FamilyRosterRoot");
            formationIndex = RegisteredRootIndex(roots, "SC022FamilyFormationRoot");
            Assert.That(rosterIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(formationIndex, Is.GreaterThanOrEqualTo(0));
            roots.GetArrayElementAtIndex(rosterIndex).FindPropertyRelative("root")
                .objectReferenceValue = Find("SC022FamilyFormationRoot").gameObject;
            roots.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string duplicated), Is.False,
                "a duplicated registration drops the family-roster root from every page");
            Assert.That(duplicated, Does.Contain("stale"));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string final), Is.True, final);

            navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            navigator.ShowPage(ReferenceGalleryPage.Family);
            Assert.That(Find("SC022FamilyFormationRoot").gameObject.activeSelf, Is.True,
                "the repaired mask must put the SC-022 entry back on the Family page");
        }

        [Test]
        public void Navigator_PlacesTheFeatureLaunchersAboveTheOpaquePageChrome()
        {
            Transform canvas = Find("Canvas");
            Button education = CreateFeatureLauncher(AddSC020CharacterEducation.RootName,
                AddSC020CharacterEducation.OpenButtonName, canvas, "子女培养");
            Button research = CreateFeatureLauncher(AddSC021FamilyResearch.RootName,
                AddSC021FamilyResearch.OpenButtonName, canvas, "家族研究");
            Transform formationRoot = Find(AddSC022FamilyFormation.RootName);
            formationRoot.SetParent(canvas, false);
            StretchToCanvas((RectTransform)formationRoot);
            Button formation = Find(AddSC022FamilyFormation.OpenButtonName)
                .GetComponent<Button>();
            Button doctrine = CreateLauncherButton(AddSC024FamilyDoctrine.OpenButtonName,
                canvas, "家训与抱负");

            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            Assert.That(navigator.FeatureLauncherCount, Is.EqualTo(4),
                "every feature entry present on the Canvas must be bound as a launcher");

            navigator.ShowPage(ReferenceGalleryPage.Character);
            Canvas.ForceUpdateCanvases();
            Assert.That(education.gameObject.activeInHierarchy, Is.True);
            AssertLauncherReachable(canvas, education);
            Assert.That(research.gameObject.activeInHierarchy, Is.False);
            Assert.That(formation.gameObject.activeInHierarchy, Is.False);
            Assert.That(doctrine.gameObject.activeInHierarchy, Is.False);

            navigator.ShowPage(ReferenceGalleryPage.Family);
            Canvas.ForceUpdateCanvases();
            Assert.That(education.gameObject.activeInHierarchy, Is.False);
            AssertLauncherReachable(canvas, research);
            AssertLauncherReachable(canvas, formation);
            AssertLauncherReachable(canvas, doctrine);

            // The three Family entries form one non-overlapping shelf; each keeps its
            // own click target while the whole shelf clears the page's primary columns.
            Rect researchRect = RectIn((RectTransform)research.transform, canvas);
            Rect formationRect = RectIn((RectTransform)formation.transform, canvas);
            Rect doctrineRect = RectIn((RectTransform)doctrine.transform, canvas);
            Assert.That(researchRect.Overlaps(formationRect, true), Is.False);
            Assert.That(formationRect.Overlaps(doctrineRect, true), Is.False);
        }

        [Test]
        public void Navigator_FirstPageVisitsKeepTheShelfBetweenThePageAndTheOverlays()
        {
            Transform canvas = Find("Canvas");
            // The scene updaters create these roots before M02DailyActionRoot. The first
            // Character and Family visits must still end with the shelf between the page
            // root and the title/event/tactical/inspector overlays.
            Button education = CreateFeatureLauncher(AddSC020CharacterEducation.RootName,
                AddSC020CharacterEducation.OpenButtonName, canvas, "子女培养");
            Button research = CreateFeatureLauncher(AddSC021FamilyResearch.RootName,
                AddSC021FamilyResearch.OpenButtonName, canvas, "家族研究");
            Transform formationRoot = Find(AddSC022FamilyFormation.RootName);
            formationRoot.SetParent(canvas, false);
            StretchToCanvas((RectTransform)formationRoot);
            Button formation = Find(AddSC022FamilyFormation.OpenButtonName)
                .GetComponent<Button>();
            Button doctrine = CreateLauncherButton(AddSC024FamilyDoctrine.OpenButtonName,
                canvas, "家训与抱负");
            Find("M03NpcAiInspectorRoot").SetParent(canvas, false);
            Find("EventChoiceRoot").SetParent(canvas, false);
            Create("M6TitleRoot", canvas);
            Create("M6TacticalMissionOverlay", canvas);

            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            Assert.That(navigator.FeatureLauncherCount, Is.EqualTo(4));

            navigator.ShowPage(ReferenceGalleryPage.Character);
            Canvas.ForceUpdateCanvases();
            AssertShelfBand(canvas, education);
            AssertLauncherReachable(canvas, education);

            navigator.ShowPage(ReferenceGalleryPage.Family);
            Canvas.ForceUpdateCanvases();
            AssertShelfBand(canvas, research, formation, doctrine);
            AssertLauncherReachable(canvas, research);
            AssertLauncherReachable(canvas, formation);
            AssertLauncherReachable(canvas, doctrine);
        }

        [Test]
        public void Navigator_KeepsAnOpenSuccessionModalAboveTheFamilyShelf()
        {
            Transform canvas = Find("Canvas");
            CreateFeatureLauncher(AddSC021FamilyResearch.RootName,
                AddSC021FamilyResearch.OpenButtonName, canvas, "家族研究");
            Find(AddSC022FamilyFormation.RootName).SetParent(canvas, false);
            StretchToCanvas((RectTransform)Find(AddSC022FamilyFormation.RootName));
            Button doctrine = CreateLauncherButton(AddSC024FamilyDoctrine.OpenButtonName,
                canvas, "家训与抱负");
            Transform successionRoot = Find("SuccessionRoot");
            successionRoot.SetParent(canvas, false);
            StretchToCanvas((RectTransform)successionRoot);
            Create("SuccessionHud", successionRoot, typeof(Image));
            GameObject modal = Create("SuccessionModal", successionRoot, typeof(Image));
            var modalRect = (RectTransform)modal.transform;
            modalRect.anchorMin = modalRect.anchorMax = modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(860f, 600f);
            GameObject confirm = Create("SuccessionConfirmButton", modal.transform,
                typeof(Image), typeof(Button));
            var confirmRect = (RectTransform)confirm.transform;
            confirmRect.anchorMin = confirmRect.anchorMax = confirmRect.pivot =
                new Vector2(0.5f, 0.5f);
            confirmRect.sizeDelta = ReferenceGalleryNavigator.FeatureLauncherSize;
            Image confirmImage = confirm.GetComponent<Image>();
            confirmImage.raycastTarget = true;
            confirm.GetComponent<Button>().targetGraphic = confirmImage;
            modal.SetActive(false);
            Find("M03NpcAiInspectorRoot").SetParent(canvas, false);
            Find("EventChoiceRoot").SetParent(canvas, false);
            Create("M6TitleRoot", canvas);
            Create("M6TacticalMissionOverlay", canvas);

            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            navigator.ShowPage(ReferenceGalleryPage.Family);
            Canvas.ForceUpdateCanvases();

            // A modal control sits exactly under the doctrine shelf slot.
            MoveOver(confirmRect, (RectTransform)doctrine.transform);
            Vector2 point = SharedShelfPoint(canvas, confirmRect);
            Assert.That(TopmostGraphic(canvas, point, true), Is.SameAs(doctrine.targetGraphic),
                "with the modal closed the shelf owns its own slot");

            // The succession HUD opens the modal while the page is already displayed.
            modal.SetActive(true);
            typeof(ReferenceGalleryNavigator).GetMethod("LateUpdate",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic).Invoke(navigator, null);

            // Read the live indices: lifting the modal shifts the shelf band by one slot.
            Assert.That(successionRoot.GetSiblingIndex(),
                Is.GreaterThan(doctrine.transform.GetSiblingIndex()),
                "an open modal must render above the Family shelf");
            Assert.That(successionRoot.GetSiblingIndex(),
                Is.LessThan(canvas.Find("M03NpcAiInspectorRoot").GetSiblingIndex()),
                "lifting the modal must keep it below the lifecycle overlays");
            Assert.That(TopmostGraphic(canvas, point, true), Is.SameAs(confirmImage),
                "the modal control, not the shelf launcher, receives the pointer");
        }

        [Test]
        public void Validator_RejectsMissingDuplicateAndForeignLauncherRegistrations()
        {
            Transform canvas = Find("Canvas");
            CreateFeatureLauncher(AddSC020CharacterEducation.RootName,
                AddSC020CharacterEducation.OpenButtonName, canvas, "子女培养");
            CreateFeatureLauncher(AddSC021FamilyResearch.RootName,
                AddSC021FamilyResearch.OpenButtonName, canvas, "家族研究");
            Find(AddSC022FamilyFormation.RootName).SetParent(canvas, false);
            StretchToCanvas((RectTransform)Find(AddSC022FamilyFormation.RootName));
            CreateLauncherButton(AddSC024FamilyDoctrine.OpenButtonName, canvas, "家训与抱负");

            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(RegisteredLauncherNames(), Is.EqualTo(new[]
            {
                AddSC020CharacterEducation.RootName, AddSC021FamilyResearch.RootName,
                AddSC022FamilyFormation.RootName, AddSC024FamilyDoctrine.OpenButtonName
            }), "the generator must bind all four feature entries");

            // A registration that lost its root makes that entry unreachable on its page.
            SerializedProperty launchers = LauncherEntries();
            launchers.GetArrayElementAtIndex(0).FindPropertyRelative("root")
                .objectReferenceValue = null;
            launchers.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string missing), Is.False,
                "a launcher entry without a root must invalidate the registration");
            Assert.That(missing, Does.Contain("launcher"));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string repaired), Is.True, repaired);

            // Two entries for one launcher leave another entry unreachable.
            launchers = LauncherEntries();
            launchers.GetArrayElementAtIndex(0).FindPropertyRelative("root")
                .objectReferenceValue = Find(AddSC021FamilyResearch.RootName).gameObject;
            launchers.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string duplicated), Is.False,
                "a duplicated launcher registration drops another entry");
            Assert.That(duplicated, Does.Contain("launcher"));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));

            // A launcher-shaped root nested inside another page root cannot be shelved on
            // the Canvas, so it must be rejected instead of silently left below the page.
            GameObject foreign = Create(AddSC021FamilyResearch.RootName,
                Find("M2WorldMapRoot"));
            launchers = LauncherEntries();
            launchers.GetArrayElementAtIndex(1).FindPropertyRelative("root")
                .objectReferenceValue = foreign;
            launchers.serializedObject.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string foreignParent),
                Is.False, "a launcher root nested under another root is not a Canvas child");
            Assert.That(foreignParent, Does.Contain("launcher"));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string final), Is.True, final);
            Assert.That(RegisteredLauncherNames(), Does.Contain(AddSC021FamilyResearch.RootName));

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            navigator.ShowPage(ReferenceGalleryPage.Family);
            Canvas.ForceUpdateCanvases();
            AssertLauncherReachable(canvas, Find(AddSC021FamilyResearch.OpenButtonName)
                .GetComponent<Button>());
        }

        private SerializedProperty LauncherEntries()
        {
            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            return new SerializedObject(navigator).FindProperty("featureLaunchers");
        }

        private string[] RegisteredLauncherNames()
        {
            SerializedProperty launchers = LauncherEntries();
            var names = new string[launchers != null && launchers.isArray ? launchers.arraySize : 0];
            for (int i = 0; i < names.Length; i++)
            {
                var registered = launchers.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("root").objectReferenceValue as GameObject;
                names[i] = registered != null ? registered.name : string.Empty;
            }
            return names;
        }

        private static void AssertShelfBand(Transform canvas, params Button[] shelf)
        {
            Transform page = canvas.Find(AddM02DailyActionPanel.RootName);
            int pageIndex = page.GetSiblingIndex();
            string[] overlayNames =
            {
                "M03NpcAiInspectorRoot", "M6TitleRoot", "EventChoiceRoot",
                "M6TacticalMissionOverlay"
            };
            int ceiling = canvas.childCount;
            foreach (string overlayName in overlayNames)
            {
                Transform overlay = canvas.Find(overlayName);
                Assert.That(overlay, Is.Not.Null, overlayName + " must be a Canvas child");
                Assert.That(overlay.GetSiblingIndex(), Is.GreaterThan(pageIndex),
                    overlayName + " must render above the page root");
                ceiling = Mathf.Min(ceiling, overlay.GetSiblingIndex());
            }
            int previous = pageIndex;
            foreach (Button button in shelf)
            {
                Transform root = button.name == AddSC024FamilyDoctrine.OpenButtonName
                    ? button.transform
                    : button.transform.parent;
                int index = root.GetSiblingIndex();
                Assert.That(index, Is.EqualTo(previous + 1),
                    root.name + " belongs in the shelf band directly above the page root");
                Assert.That(index, Is.LessThan(ceiling),
                    root.name + " must render below every lifecycle overlay");
                previous = index;
            }
        }

        /// <summary>Places one rect exactly over another in the shared canvas plane.</summary>
        private static void MoveOver(RectTransform moving, RectTransform over)
        {
            Vector3 world = over.TransformPoint(over.rect.center);
            moving.anchoredPosition = moving.parent.InverseTransformPoint(world);
        }

        /// <summary>
        /// A point inside both the overlay control and the shelf: the shelf column's centre
        /// is right of the 860x600 succession sheet, so the shared strip is its left edge.
        /// </summary>
        private static Vector2 SharedShelfPoint(Transform canvas, RectTransform control)
        {
            Vector2 centre = CanvasPoint(canvas, control);
            return centre + new Vector2(-control.rect.width * 0.45f, 0f);
        }

        [Test]
        public void Navigator_ClosesTheLauncherOwnedDoctrinePanelWhenTheFamilyPageIsLeft()
        {
            // Replace the bare fixture body with the real SC-024 launcher, whose panel
            // component lives on the active HUD button while the body root stays excluded
            // from page-root toggling.
            UnityEngine.Object.DestroyImmediate(
                Find(AddSC024FamilyDoctrine.RootName).gameObject);
            _session.gameObject.AddComponent<TitleScreenController>();
            Assert.That(AddSC024FamilyDoctrine.Update(_scene),
                Is.EqualTo(AddSC024FamilyDoctrine.UpdateResult.Success));

            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string stale), Is.False,
                "a launcher added after generation must invalidate the registration");
            Assert.That(stale, Does.Contain("stale"));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string repaired), Is.True,
                repaired);

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            Button launcher = Find(AddSC024FamilyDoctrine.OpenButtonName)
                .GetComponent<Button>();
            FamilyDoctrinePanel panel = launcher.GetComponent<FamilyDoctrinePanel>();
            Transform doctrineRoot = Find(AddSC024FamilyDoctrine.RootName);
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(doctrineRoot.gameObject.activeSelf, Is.False);

            navigator.ShowPage(ReferenceGalleryPage.Family);
            Canvas.ForceUpdateCanvases();
            AssertLauncherReachable(Find("Canvas"), launcher);

            launcher.onClick.Invoke();
            Assert.That(panel.IsOpen, Is.True,
                "the Family-page launcher opens the launcher-owned doctrine panel");
            Assert.That(doctrineRoot.gameObject.activeSelf, Is.True);

            navigator.ShowPage(ReferenceGalleryPage.Household);
            Assert.That(panel.IsOpen, Is.False,
                "the excluded doctrine body must be closed explicitly when Family is left");
            Assert.That(doctrineRoot.gameObject.activeSelf, Is.False);
            Assert.That(launcher.gameObject.activeInHierarchy, Is.False);
        }

        [Test]
        public void Validator_RejectsAForeignParentPageRootRegistration()
        {
            Transform canvas = Find("Canvas");
            Find(AddSC022FamilyFormation.RootName).SetParent(canvas, false);
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));

            ReferenceGalleryNavigator navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            SerializedProperty roots = new SerializedObject(navigator).FindProperty("pageRoots");
            int formationIndex = RegisteredRootIndex(roots,
                AddSC022FamilyFormation.RootName);
            Assert.That(formationIndex, Is.GreaterThanOrEqualTo(0));

            // A registered page root must be a direct Canvas child. Re-point the entry at
            // a root nested under another page root: toggling it would leave the page area
            // dark and its geometry would be laid out against the wrong parent.
            GameObject foreign = Create("ForeignPageRoot", Find("M2WorldMapRoot"));
            roots.GetArrayElementAtIndex(formationIndex).FindPropertyRelative("root")
                .objectReferenceValue = foreign;
            roots.serializedObject.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string foreignParent),
                Is.False);
            Assert.That(foreignParent, Does.Contain("stale"));
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string repaired), Is.True,
                repaired);

            navigator = Find(AddM02DailyActionPanel.RootName)
                .GetComponent<ReferenceGalleryNavigator>();
            Assert.That(RegisteredPageRootNames(navigator), Does.Not.Contain("ForeignPageRoot"));
            Assert.That(RegisteredPageRootNames(navigator),
                Does.Contain(AddSC022FamilyFormation.RootName),
                "the repair must restore the real Canvas child");
        }

        private static int RegisteredRootIndex(SerializedProperty roots, string name)
        {
            for (int i = 0; i < roots.arraySize; i++)
            {
                SerializedProperty root = roots.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("root");
                var registered = root != null
                    ? root.objectReferenceValue as GameObject
                    : null;
                if (registered != null && registered.name == name)
                    return i;
            }
            return -1;
        }

        private static void SetRegisteredMask(SerializedProperty roots, int index,
            ReferenceGalleryPageMask mask)
        {
            SerializedProperty visibleOn = roots.GetArrayElementAtIndex(index)
                .FindPropertyRelative("visibleOn");
            Assert.That(visibleOn, Is.Not.Null);
            visibleOn.intValue = (int)mask;
            roots.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string[] RegisteredPageRootNames(ReferenceGalleryNavigator navigator)
        {
            SerializedProperty roots = new SerializedObject(navigator).FindProperty("pageRoots");
            int count = roots != null && roots.isArray ? roots.arraySize : 0;
            var names = new string[count];
            for (int i = 0; i < count; i++)
            {
                SerializedProperty root = roots.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("root");
                var registered = root != null
                    ? root.objectReferenceValue as GameObject
                    : null;
                names[i] = registered != null ? registered.name : string.Empty;
            }
            return names;
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

            // Every tab is a real hit target inside the 150px rail, and the five
            // 40px rows never overlap one another at the 1280x720 design canvas.
            var navButtons = new RectTransform[pageButtons.Length];
            for (int i = 0; i < pageButtons.Length; i++)
            {
                navButtons[i] = (RectTransform)Find(pageButtons[i]);
                Assert.That(Contained(navButtons[i], (RectTransform)navRail), Is.True,
                    pageButtons[i] + " must sit inside the navigation rail");
                Assert.That(navButtons[i].rect.width, Is.GreaterThanOrEqualTo(120f),
                    pageButtons[i] + " must keep a wide click target");
                Assert.That(navButtons[i].rect.height, Is.GreaterThanOrEqualTo(40f),
                    pageButtons[i] + " must keep a tall click target");
            }
            for (int i = 0; i < navButtons.Length; i++)
                for (int j = i + 1; j < navButtons.Length; j++)
                    Assert.That(
                        Mathf.Abs(navButtons[i].anchoredPosition.y -
                                  navButtons[j].anchoredPosition.y),
                        Is.GreaterThanOrEqualTo(
                            Mathf.Max(navButtons[i].rect.height, navButtons[j].rect.height)),
                        "navigation tab hit areas must not overlap");

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

            // A live roster with no other members shows a truthful empty state
            // instead of leaving the relation area blank.
            Assert.That(FindAll(AddM02DailyActionPanel.RelationsEmptyName).Length,
                Is.EqualTo(1));
            Transform relationsEmpty = Find(AddM02DailyActionPanel.RelationsEmptyName);
            Assert.That(relationsEmpty.gameObject.activeSelf, Is.False,
                "the relation empty state stays hidden while live relation cards render");
            Assert.That(relationsEmpty.GetComponent<Text>().text,
                Does.Contain("暂无其他可展示的关系"));
            Assert.That(relationsEmpty.GetComponent<Text>().raycastTarget, Is.False);
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
        public void Generator_KeepsTheTacticalOverlayAboveTheTitleAndEventOverlays()
        {
            Transform canvas = Find("Canvas");
            Transform eventRoot = Find("EventChoiceRoot");
            eventRoot.SetParent(canvas, false);
            GameObject title = Create("M6TitleRoot", canvas);
            GameObject overlay = Create("M6TacticalMissionOverlay", canvas);

            // Force the transactional rebuild instead of the idempotent early return.
            Find(AddM02DailyActionPanel.RootName).gameObject.SetActive(false);
            Assert.That(AddM02DailyActionPanel.Update(_scene),
                Is.EqualTo(AddM02DailyActionPanel.UpdateResult.Success));

            Transform root = Find(AddM02DailyActionPanel.RootName);
            Assert.That(root.gameObject.activeSelf, Is.True);
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.True, error);
            Assert.That(root.GetSiblingIndex(), Is.LessThan(title.transform.GetSiblingIndex()),
                "the M02 page must render beneath the title overlay");
            Assert.That(title.transform.GetSiblingIndex(), Is.LessThan(eventRoot.GetSiblingIndex()),
                "the title overlay must render beneath EventChoiceRoot");
            Assert.That(eventRoot.GetSiblingIndex(), Is.LessThan(overlay.transform.GetSiblingIndex()),
                "the M06 tactical sheet is not a gameplay root and must stay above the overlays");
            Assert.That(overlay.transform.GetSiblingIndex(), Is.EqualTo(canvas.childCount - 1),
                "the tactical sheet must remain the last Canvas child");
        }

        [UnityTest]
        public IEnumerator Panel_BindsDurationInputAfterActivatingItsText()
        {
            DailyActionPanel panel = Panel();
            InputField durationInput =
                Find(AddM02DailyActionPanel.DurationInputName).GetComponent<InputField>();
            Assert.That(panel.IsOpen, Is.False);
            Assert.That(panel.DurationText, Is.EqualTo("1"));
            Assert.That(durationInput.isActiveAndEnabled, Is.False);

            panel.Open();
            yield return null;

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.DurationText, Is.EqualTo("1"));
            Assert.That(durationInput.isActiveAndEnabled, Is.True);
            Assert.That(durationInput.textComponent, Is.SameAs(durationInput.transform.Find("Text")
                .GetComponent<Text>()));
            Assert.That(durationInput.textComponent.text, Is.EqualTo("1"));
            Assert.That(durationInput.placeholder.enabled, Is.False);
            int openCount = panel.OpenCount;
            panel.Open();
            Assert.That(panel.OpenCount, Is.EqualTo(openCount));

            panel.SetDuration(string.Empty);
            Assert.That(durationInput.textComponent.text, Is.Empty);
            Assert.That(durationInput.placeholder.enabled, Is.True);

            panel.SetDuration("2");
            Assert.That(durationInput.textComponent.text, Is.EqualTo("2"));
            Assert.That(durationInput.placeholder.enabled, Is.False);
            panel.Close();
            Assert.That(panel.IsOpen, Is.False);
            panel.Open();
            yield return null;
            Assert.That(durationInput.textComponent.text, Is.EqualTo("2"));
            Assert.That(durationInput.placeholder.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator Panel_ReopensDurationInputAfterActivationWithoutEnableFailure()
        {
            DailyActionPanel panel = Panel();
            InputField durationInput =
                Find(AddM02DailyActionPanel.DurationInputName).GetComponent<InputField>();

            panel.Open();
            yield return null;
            Assert.That(durationInput.isActiveAndEnabled, Is.True);
            Assert.That(durationInput.textComponent, Is.Not.Null);

            // Represents the player activating the field before leaving the page.
            // EditMode PreviewScene cannot create InputField's caret CanvasRenderer
            // because InputField.UpdateGeometry returns early when !Application.isPlaying,
            // so the cached-renderer branch itself is only exercised by the Editor Play
            // Mode gate. The bound textComponent below is what keeps that branch safe.
            durationInput.ActivateInputField();
            durationInput.Select();
            yield return null;

            panel.SetDuration("3");
            panel.Close();
            Assert.That(panel.IsOpen, Is.False);

            panel.Open();
            yield return null;

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(durationInput.isActiveAndEnabled, Is.True,
                "the field is enabled again after the deferred binding frame");
            Assert.That(durationInput.textComponent,
                Is.SameAs(durationInput.transform.Find("Text").GetComponent<Text>()),
                "the reopen path must bind the Text before OnEnable reads it");
            Assert.That(durationInput.text, Is.EqualTo("3"));
            Assert.That(durationInput.textComponent.text, Is.EqualTo("3"));
            Assert.That(durationInput.placeholder.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator Panel_ReopeningDurationInputKeepsTextAndCaretConsistent()
        {
            DailyActionPanel panel = Panel();
            InputField durationInput =
                Find(AddM02DailyActionPanel.DurationInputName).GetComponent<InputField>();

            panel.Open();
            yield return null;
            panel.SetDuration("3");

            // InputField.caretPosition reports the caret plus Input.compositionString.
            // This Editor process carries a stale 14-character composition string while
            // the PreviewScene has no EventSystem to own or clear IME state, so the public
            // caret property is not a reliable observable here. Assert the validated text
            // and its rendered label instead; the close/reopen path stays fully covered.
            Assert.That(durationInput.text, Is.EqualTo("3"),
                "setting duration must apply the validated text");
            Assert.That(durationInput.textComponent.text, Is.EqualTo("3"),
                "setting duration must render the validated text");
            durationInput.ActivateInputField();
            durationInput.Select();
            yield return null;

            panel.Close();
            panel.Open();
            yield return null;

            Assert.That(panel.IsOpen, Is.True);
            Assert.That(panel.DurationText, Is.EqualTo("3"),
                "reopening must keep the player's duration value");
            Assert.That(durationInput.text, Is.EqualTo("3"),
                "reopening must keep the field's text, not just its rendered label");
            Assert.That(durationInput.textComponent.text, Is.EqualTo("3"));
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
        public void SharedHeader_RefreshesAfterLoadWhileCharacterPageIsClosed()
        {
            DailyActionPanel panel = Panel();
            panel.Open();
            var saved = _session.CaptureSave();
            Assert.That(_session.AdvanceOneDay(), Is.True);
            Assert.That(View().HeaderDateLine, Does.Contain("2 日"));
            panel.Close();

            Assert.That(_session.TryApplySave(saved).Succeeded, Is.True);

            Assert.That(panel.IsOpen, Is.False);
            Assert.That(View().HeaderDateLine, Is.EqualTo("1560 年 4 月 1 日"),
                "the shared header remains visible on other pages and must follow load");
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
            string people = MarketSettlement.CountLivingFamilyMembers(_session.Succession)
                .ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(people, Is.EqualTo("6"),
                "the shipped six-person family keeps the approved header value");
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
        public void PageView_HeaderPeopleCountsLivingMembersBeyondTheFixedSixRows()
        {
            Assert.That(_session.FamilyRoster.Count, Is.EqualTo(6),
                "the fixed SC-013 projection keeps its six rows");
            var spouseId = new PersonId("person.m08a.header.spouse");
            Assert.That(_session.Succession.World.TryAddPerson(
                new PersonState(spouseId, "阿绫", PersonGender.Female),
                new SuccessionPersonLife(1540, true, false), out string personError),
                Is.True, personError);
            Assert.That(_session.Succession.World.TryAddMember(spouseId,
                FamilyMemberRole.Spouse, out string memberError), Is.True, memberError);

            Assert.That(_session.FamilyRoster.Any(row => row.PersonId == spouseId), Is.False,
                "M08A additions are not part of the fixed six-row projection");
            Panel().Open();

            Assert.That(Find(AddM02DailyActionPanel.HeaderPeopleName).GetComponent<Text>().text,
                Is.EqualTo("7"),
                "the header reports the living family, not the projected row count");
        }

        [Test]
        public void PageView_HeaderPeopleExcludesADeadFamilyMember()
        {
            Assert.That(_session.Succession.World.TryGetLife(M2FixedScenario.SakonId,
                out SuccessionPersonLife life), Is.True);
            Assert.That(_session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                new SuccessionPersonLife(life.BirthYear, false, life.Retired),
                out string error), Is.True, error);
            Assert.That(_session.FamilyRoster.Count, Is.EqualTo(6),
                "a dead member keeps its recorded roster row");

            Panel().Open();

            Assert.That(Find(AddM02DailyActionPanel.HeaderPeopleName).GetComponent<Text>().text,
                Is.EqualTo("5"), "the header reports living members only");
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
        public void PageView_RelationsEmptyStateTracksRosterSizeAndSelection()
        {
            CharacterDailyPageView view = View();
            Text empty = Find(AddM02DailyActionPanel.RelationsEmptyName).GetComponent<Text>();
            GameObject[] cards =
            {
                Find(AddM02DailyActionPanel.RelationRootName + "_1").gameObject,
                Find(AddM02DailyActionPanel.RelationRootName + "_2").gameObject,
                Find(AddM02DailyActionPanel.RelationRootName + "_3").gameObject
            };

            M2RosterRow selected = RelationRow("test.person.selected", "选中人物", true);
            M2RosterRow first = RelationRow("test.person.first", "甲");
            M2RosterRow second = RelationRow("test.person.second", "乙");
            M2RosterRow third = RelationRow("test.person.third", "丙");

            // 0 other members: the roster has only the selected person.
            view.RenderRelations(new[] { selected }, selected.PersonId);
            Assert.That(empty.gameObject.activeSelf, Is.True);
            Assert.That(empty.text, Does.Contain("暂无其他可展示的关系"));
            foreach (GameObject card in cards)
                Assert.That(card.activeSelf, Is.False);

            // Truly empty roster (and no selection) uses the roster-specific copy.
            view.RenderRelations(new M2RosterRow[0], selected.PersonId);
            Assert.That(empty.gameObject.activeSelf, Is.True);
            Assert.That(empty.text, Does.Contain("家族名册为空"));

            // 1 other member: exactly one card, empty state hidden.
            view.RenderRelations(new[] { selected, first }, selected.PersonId);
            Assert.That(empty.gameObject.activeSelf, Is.False);
            Assert.That(cards[0].activeSelf, Is.True);
            Assert.That(cards[1].activeSelf, Is.False);
            Assert.That(cards[2].activeSelf, Is.False);
            Assert.That(Find(AddM02DailyActionPanel.RelationNameName + "_1")
                .GetComponent<Text>().text, Is.EqualTo("甲"));

            // 3 other members: all cards render from the supplied live projection.
            // Only the head-first rule is contractual; the remaining order is not.
            view.RenderRelations(new[] { selected, first, second, third }, selected.PersonId);
            Assert.That(empty.gameObject.activeSelf, Is.False);
            foreach (GameObject card in cards)
                Assert.That(card.activeSelf, Is.True);
            string[] shownNames =
            {
                Find(AddM02DailyActionPanel.RelationNameName + "_1").GetComponent<Text>().text,
                Find(AddM02DailyActionPanel.RelationNameName + "_2").GetComponent<Text>().text,
                Find(AddM02DailyActionPanel.RelationNameName + "_3").GetComponent<Text>().text
            };
            Assert.That(shownNames, Is.EquivalentTo(new[] { "甲", "乙", "丙" }));

            // Switching the selected person re-drives the same render path.
            view.RenderRelations(new[] { selected, first, second, third }, first.PersonId);
            Assert.That(empty.gameObject.activeSelf, Is.False);
            foreach (GameObject card in cards)
                Assert.That(card.activeSelf, Is.True);
            Assert.That(Find(AddM02DailyActionPanel.RelationNameName + "_1")
                .GetComponent<Text>().text, Is.EqualTo("选中人物"),
                "the head of household sorts first among the remaining relations");

            // Switching to a person with no other members shows the empty state.
            view.RenderRelations(new[] { first }, first.PersonId);
            Assert.That(empty.gameObject.activeSelf, Is.True);
            Assert.That(empty.text, Does.Contain("暂无其他可展示的关系"));
            foreach (GameObject card in cards)
                Assert.That(card.activeSelf, Is.False);

            // No selected person: no fabricated relation cards and an explicit hint.
            view.RenderRelations(new[] { selected }, default(PersonId));
            Assert.That(empty.gameObject.activeSelf, Is.True);
            Assert.That(empty.text, Does.Contain("尚未选择人物"));
            foreach (GameObject card in cards)
                Assert.That(card.activeSelf, Is.False);
        }

        [Test]
        public void Validate_RequiresTheBoundRelationsEmptyText()
        {
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string baseline),
                Is.True, baseline);
            CharacterDailyPageView view = View();
            Text empty = Find(AddM02DailyActionPanel.RelationsEmptyName).GetComponent<Text>();
            var serialized = new SerializedObject(view);
            SerializedProperty property = serialized.FindProperty("relationsEmptyText");
            Assert.That(property, Is.Not.Null);
            Assert.That(property.objectReferenceValue, Is.EqualTo(empty),
                "the generated page view must reference the generated empty-state Text");

            property.objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            try
            {
                Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.False);
                Assert.That(error, Does.Contain("relationsEmptyText"));
            }
            finally
            {
                property.objectReferenceValue = empty;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string restored),
                Is.True, restored);
        }

        [Test]
        public void Validate_RejectsRelationsEmptyTextOutsideRelationsBody()
        {
            Transform empty = Find(AddM02DailyActionPanel.RelationsEmptyName);
            Transform originalParent = empty.parent;
            Assert.That(originalParent.name, Is.EqualTo(AddM02DailyActionPanel.RelationsBodyName));

            empty.SetParent(Find(AddM02DailyActionPanel.ProfileBodyName), false);
            try
            {
                Assert.That(AddM02DailyActionPanel.Validate(_scene, out string error), Is.False);
                Assert.That(error, Does.Contain("direct child of M02RelationsBody"));
            }
            finally
            {
                empty.SetParent(originalParent, false);
            }
            Assert.That(AddM02DailyActionPanel.Validate(_scene, out string restored),
                Is.True, restored);
        }

        private static M2RosterRow RelationRow(string id, string name, bool isHead = false) =>
            new M2RosterRow(new PersonId(id), name,
                isHead ? FamilyMemberRole.Head : FamilyMemberRole.DirectKin, 30, "第二代",
                "亲族", isHead, false, false);

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

        private Button CreateFeatureLauncher(string rootName, string buttonName, Transform canvas,
            string label)
        {
            GameObject root = Create(rootName, canvas);
            StretchToCanvas((RectTransform)root.transform);
            return CreateLauncherButton(buttonName, root.transform, label);
        }

        private Button CreateLauncherButton(string name, Transform parent, string label)
        {
            GameObject go = Create(name, parent, typeof(Image), typeof(Button));
            Image image = go.GetComponent<Image>();
            image.color = new Color(0.14f, 0.18f, 0.24f, 1f);
            image.raycastTarget = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            GameObject labelObject = Create(name + "Label", go.transform, typeof(Text));
            Text text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(200f, 44f);
            rect.anchoredPosition = new Vector2(20f, -300f);
            return button;
        }

        private static void StretchToCanvas(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Proves a launcher is not only active but actually visible and hittable: its
        /// rect is inside the canvas and clear of the opaque chrome, its render order is
        /// above the opaque page root, the topmost drawn graphic at its centre belongs to
        /// it, and the topmost raycast target at that point is the launcher itself.
        /// </summary>
        private void AssertLauncherReachable(Transform canvas, Button button)
        {
            RectTransform rect = (RectTransform)button.transform;
            Assert.That(button.interactable, Is.True, button.name + " must stay clickable");
            Assert.That(button.targetGraphic, Is.Not.Null);
            Assert.That(button.targetGraphic.raycastTarget, Is.True,
                button.name + " needs a raycast target for pointer hits");
            Assert.That(rect.rect.size, Is.EqualTo(
                    ReferenceGalleryNavigator.FeatureLauncherSize),
                button.name + " uses the shared launcher shelf size");
            Assert.That(Contained(rect, (RectTransform)canvas), Is.True,
                button.name + " must stay inside the page canvas");

            Rect launcherRect = RectIn(rect, canvas);
            Assert.That(launcherRect.Overlaps(RectIn((RectTransform)Find(
                AddM02DailyActionPanel.NavRailName), canvas), true), Is.False,
                button.name + " must not sit under the opaque navigation rail");
            Assert.That(launcherRect.Overlaps(RectIn((RectTransform)Find(
                AddM02DailyActionPanel.TopBarName), canvas), true), Is.False,
                button.name + " must not sit under the opaque top bar");
            Assert.That(launcherRect.Overlaps(RectIn((RectTransform)Find(
                AddM02DailyActionPanel.ProfilePanelName), canvas), true), Is.False,
                button.name + " must not cover the character profile column");
            Assert.That(launcherRect.Overlaps(RectIn((RectTransform)Find(
                AddM02DailyActionPanel.ActionPanelName), canvas), true), Is.False,
                button.name + " must not cover the daily-action column");

            Transform page = canvas.Find(AddM02DailyActionPanel.RootName);
            Transform root = button.transform.parent == canvas
                ? button.transform
                : button.transform.parent;
            Assert.That(root.parent, Is.EqualTo(canvas));
            Assert.That(root.GetSiblingIndex(), Is.GreaterThan(page.GetSiblingIndex()),
                button.name + " must render above the opaque page root, not behind it");

            RectTransform backdrop = (RectTransform)Find(AddM02DailyActionPanel.BackdropName);
            Vector2 point = CanvasPoint(canvas, rect);
            if (Find(AddM02DailyActionPanel.PanelName).gameObject.activeInHierarchy)
            {
                Assert.That(backdrop.gameObject.activeInHierarchy, Is.True,
                    "the open Character page paints its opaque backdrop under the launcher");
                Assert.That(RectIn(backdrop, canvas).Contains(point), Is.True,
                    "the sample point must lie inside the opaque M02 page area");
            }

            // On every tab the shared gallery background is the opaque surface the launcher
            // must rise above; the M02 page backdrop only exists while the dossier is open.
            Transform shared = canvas.Find("ReferenceGalleryBackdrop");
            Assert.That(shared, Is.Not.Null, "the shared page background must exist");
            var sharedRect = (RectTransform)shared;
            Assert.That(sharedRect.gameObject.activeInHierarchy, Is.True);
            Assert.That(sharedRect.GetComponent<Image>().color.a, Is.GreaterThan(0.5f),
                "the shared page background is opaque");
            Assert.That(RectIn(sharedRect, canvas).Contains(point), Is.True,
                "the launcher must sit over the opaque shared background");
            Assert.That(shared.GetSiblingIndex(), Is.LessThan(root.GetSiblingIndex()),
                "the shared background must render behind the launcher shelf");

            Graphic opaque = TopmostGraphic(canvas, point, false);
            Assert.That(opaque, Is.Not.Null);
            Assert.That(opaque.transform == button.transform ||
                        opaque.transform.IsChildOf(button.transform), Is.True,
                button.name + " is visually covered by " + Describe(opaque));
            Graphic hit = TopmostGraphic(canvas, point, true);
            Assert.That(hit, Is.SameAs(button.targetGraphic),
                button.name + " must be the topmost raycast target, not " + Describe(hit));
        }

        private static Rect RectIn(RectTransform rect, Transform canvas)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 min = canvas.InverseTransformPoint(corners[0]);
            Vector3 max = canvas.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static Vector2 CanvasPoint(Transform canvas, RectTransform rect) =>
            canvas.InverseTransformPoint(rect.TransformPoint(rect.rect.center));

        /// <summary>
        /// Topmost graphic at a Canvas-local point in uGUI render order (later siblings and
        /// children draw over earlier ones). With raycastOnly it returns the graphic the
        /// EventSystem would hit, which is the clickability ground truth.
        /// </summary>
        private static Graphic TopmostGraphic(Transform canvas, Vector2 canvasPoint,
            bool raycastOnly)
        {
            Vector3 world = canvas.TransformPoint(canvasPoint);
            Graphic found = null;
            for (int i = 0; i < canvas.childCount; i++)
            {
                Graphic child = TopmostGraphicIn(canvas.GetChild(i), world, raycastOnly);
                if (child != null)
                    found = child;
            }
            return found;
        }

        private static Graphic TopmostGraphicIn(Transform node, Vector3 world,
            bool raycastOnly)
        {
            Graphic found = null;
            foreach (Graphic graphic in node.GetComponents<Graphic>())
            {
                if (!graphic.enabled || !graphic.gameObject.activeInHierarchy ||
                    graphic.color.a <= 0.01f || (raycastOnly && !graphic.raycastTarget))
                    continue;
                if (graphic is Text text && string.IsNullOrEmpty(text.text))
                    continue;
                RectTransform rect = graphic.rectTransform;
                if (rect.rect.Contains(rect.InverseTransformPoint(world)))
                    found = graphic;
            }
            for (int i = 0; i < node.childCount; i++)
            {
                Graphic child = TopmostGraphicIn(node.GetChild(i), world, raycastOnly);
                if (child != null)
                    found = child;
            }
            return found;
        }

        private static string Describe(Graphic graphic) =>
            graphic == null ? "nothing" : graphic.gameObject.name;

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
