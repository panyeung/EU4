using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Transactional FoundationScene updater for the M02 character/daily-action page.
    /// Every generated object lives beneath one fixed root in a single Undo group; the
    /// existing scene is snapshotted, validation runs before saving, and any failure
    /// reverts the group without reloading the scene. The composition mirrors the supplied
    /// reference page (identity/resource header, nav rail, parchment profile, daily-action
    /// cards and a separated read-only AI rail) while all values remain live session data.
    /// </summary>
    public static class AddM02DailyActionPanel
    {
        public const string ScenePath = "Assets/Scenes/FoundationScene.unity";
        public const string RootName = "M02DailyActionRoot";
        public const string OpenButtonName = "M02DailyActionOpenButton";
        public const string PanelName = "M02DailyActionPanel";
        public const string TitleName = "M02DailyActionTitleText";
        public const string SummaryName = "M02DailyActionSummaryText";
        public const string ScrollName = "M02DailyActionScroll";
        public const string ContentName = "M02DailyActionContent";
        public const string RowTemplateName = "M02DailyActionRowTemplate";
        public const string EmptyName = "M02DailyActionEmptyText";
        public const string SelectedName = "M02DailyActionSelectedText";
        public const string DurationLabelName = "M02DailyActionDurationLabel";
        public const string DurationInputName = "M02DailyActionDurationInput";
        public const string RestButtonName = "M02DailyActionRestButton";
        public const string TrainingButtonName = "M02DailyActionTrainingButton";
        public const string WorkButtonName = "M02DailyActionWorkButton";
        public const string TravelKiyosuButtonName = "M02DailyActionTravelKiyosuButton";
        public const string TravelAtsutaButtonName = "M02DailyActionTravelAtsutaButton";
        public const string TravelNagoyaButtonName = "M02DailyActionTravelNagoyaButton";
        public const string InterruptButtonName = "M02DailyActionInterruptButton";
        public const string FeedbackName = "M02DailyActionFeedbackText";
        public const string CloseButtonName = "M02DailyActionCloseButton";

        public const string BackdropName = "M02PageBackdrop";
        public const string TopBarName = "M02TopBar";
        public const string HeaderDateName = "M02HeaderDateText";
        public const string HeaderLocationName = "M02HeaderLocationText";
        public const string HeaderMeritName = "M02HeaderMeritText";
        public const string HeaderPeopleName = "M02HeaderPeopleText";
        public const string HeaderFundsName = "M02HeaderFundsText";
        public const string HeaderSuppliesName = "M02HeaderSuppliesText";
        public const string HeaderIntelName = "M02HeaderIntelText";
        public const string NavRailName = "M02NavRail";
        public const string NavCharacterButtonName = "ReferenceNavCharacterButton";
        public const string NavFamilyButtonName = "ReferenceNavFamilyButton";
        public const string NavHouseholdButtonName = "ReferenceNavHouseholdButton";
        public const string NavMapButtonName = "ReferenceNavMapButton";
        public const string NavEventsButtonName = "ReferenceNavEventsButton";
        public const string RosterHeaderName = "M02RosterHeaderText";
        public const string ProfilePanelName = "M02ProfilePanel";
        public const string ProfilePortraitName = "M02ProfilePortrait";
        public const string ProfileNameName = "M02ProfileNameText";
        public const string ProfileAgeName = "M02ProfileAgeText";
        public const string ProfileIdentityName = "M02ProfileIdentityText";
        public const string ProfileRoleName = "M02ProfileRoleText";
        public const string ProfileLocationName = "M02ProfileLocationText";
        public const string ProfileStatusName = "M02ProfileStatusText";
        public const string CreedChipName = "M02ProfileCreedChip";
        public const string AmbitionChipName = "M02ProfileAmbitionChip";
        public const string TraitSummaryName = "M02ProfileTraitSummaryText";
        public const string NinjutsuValueName = "M02StatNinjutsuValueText";
        public const string MartialValueName = "M02StatMartialValueText";
        public const string ScholarshipValueName = "M02StatScholarshipValueText";
        public const string SocialValueName = "M02StatSocialValueText";
        public const string NinjutsuFillName = "M02StatNinjutsuFill";
        public const string MartialFillName = "M02StatMartialFill";
        public const string ScholarshipFillName = "M02StatScholarshipFill";
        public const string SocialFillName = "M02StatSocialFill";
        public const string RelationRootName = "M02RelationCard";
        public const string RelationNameName = "M02RelationNameText";
        public const string RelationDetailName = "M02RelationDetailText";
        public const string RelationsEmptyName = "M02RelationsEmptyText";
        public const string ProfileBodyName = "M02ProfileBody";
        public const string RelationsBodyName = "M02RelationsBody";
        public const string HistoryBodyName = "M02HistoryBody";
        public const string HistoryTextName = "M02HistoryText";
        public const string ProfileTabButtonName = "M02ProfileTabProfileButton";
        public const string RelationsTabButtonName = "M02ProfileTabRelationsButton";
        public const string HistoryTabButtonName = "M02ProfileTabHistoryButton";
        public const string ProfileTabUnderlineName = "M02ProfileTabProfileUnderline";
        public const string RelationsTabUnderlineName = "M02ProfileTabRelationsUnderline";
        public const string HistoryTabUnderlineName = "M02ProfileTabHistoryUnderline";
        public const string ActionPanelName = "M02ActionPanel";
        public const string ActionSubtitleName = "M02ActionSubtitleText";
        public const string ProgressLabelName = "M02ActionProgressLabel";
        public const string ProgressFillName = "M02ActionProgressFill";
        public const string ScheduleName = "M02ActionScheduleText";
        public const string TravelInfoName = "M02TravelInfoText";
        public const string TravelSceneName = "M02TravelSceneImage";
        public const string TravelCardName = "M02DailyActionTravelCardButton";
        public const string InspectorRailName = "M02InspectorRail";
        public const string InspectorTitleName = "M02InspectorTitleText";
        public const string InspectorBadgeName = "M02InspectorBadgeText";
        public const string InspectorSubjectName = "M02InspectorSubjectText";
        public const string InspectorHintName = "M02InspectorHintText";
        public const string InspectorOpenName = "M02InspectorOpenButton";

        public const string MenuPath = "Tools/Sengoku Chronicle/Add M02 Daily Action Panel";
        public const string DirtyMenuPath =
            "Tools/Sengoku Chronicle/Integrate M02 Daily Action Panel (dirty workspace)";

        private const string CanvasName = "Canvas";
        private const string MapRootName = "M2WorldMapRoot";
        private const string IndustryRootName = "IndustryRoot";
        private const string EducationRootName = "SC020EducationRoot";
        private const string ResearchRootName = "SC021ResearchRoot";
        private const string FormationRootName = "SC022FamilyFormationRoot";
        private const string DoctrineRootName = "FamilyDoctrineRoot";
        private const string DoctrineOpenButtonName = "FamilyDoctrineOpenButton";
        private const string TitleRootName = "M6TitleRoot";
        private const string EventRootName = "EventChoiceRoot";
        private const string TacticalOverlayName = AddM6TitleScreen.TacticalOverlayName;
        private const string InspectorRootName = "M03NpcAiInspectorRoot";
        private const string UndoName = "Add M02 Daily Action Panel";

        private const string PaperPath = "Assets/Art/UI/paper_grain.png";
        private const string PortraitPath = "Assets/Art/UI/ReferenceArt/portrait_main.png";
        private const string TravelScenePath = "Assets/Art/UI/ReferenceArt/travel_scene.png";
        private const string SealPath = "Assets/Art/UI/ReferenceArt/seal.png";

        // Original generated UI art (docs/ART_ASSET_WORKFLOW.md). The atlases are
        // sliced in the importer metadata; sub-sprites are addressed by name.
        private const string IconsAtlasPath = "Assets/Resources/Generated/ui_icons_atlas.png";
        private const string ParchmentAtlasPath = "Assets/Resources/Generated/parchment_tile.png";
        private const string IndigoAtlasPath = "Assets/Resources/Generated/indigo_pattern_tile.png";
        private const string ParchmentSpriteName = "parchment_tile_0";
        private const string IndigoSpriteName = "indigo_pattern_tile_0";

        /// <summary>Loads one named sub-sprite of a generated atlas (null-safe).</summary>
        private static Sprite LoadGeneratedSprite(string assetPath, string spriteName)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
                if (asset is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            return null;
        }

        private static readonly Vector2 OpenButtonPosition = new Vector2(20f, 660f);
        private static readonly Vector2 OpenButtonSize = new Vector2(220f, 40f);
        private static readonly Vector2 PanelSize = new Vector2(1280f, 720f);

        private const float ContainmentTolerance = 0.5f;

        // Column geometry at the 1280x720 reference canvas.
        private const float NavWidth = 150f;
        private const float ContentLeft = 158f;
        private const float ContentTop = 84f;
        // The reference page is composed at 1280x720, but the live Canvas scales
        // with the Game View; this host currently measures 1311.6x702.6. The
        // content columns must therefore end at 702 units from the top so the
        // inspector rail (and every other top-anchored column) stays inside both
        // the current Canvas height and the 1280x720 design height.
        private const float ContentHeight = 618f;
        private const float ProfileWidth = 440f;
        private const float ActionLeft = 606f;
        private const float ActionWidth = 400f;
        private const float InspectorLeft = 1016f;
        private const float InspectorWidth = 256f;

        public enum UpdateResult
        {
            Success, SceneMismatch, SceneDirty, EditorPlaying, InvalidScene,
            MissingOrDuplicateObjects, ExistingPartialControl, ValidationFailed,
            OriginalObjectsChanged, SaveFailed, Failed
        }

        [MenuItem(MenuPath)]
        public static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = CheckScenePreflight(scene.path, scene.isDirty,
                EditorApplication.isPlayingOrWillChangePlaymode);
            if (result == UpdateResult.Success)
                result = Execute(scene, true, null);
            Report(result, "M02 character/daily-action page");
        }

        /// <summary>
        /// Explicit integration entry for the authorized dirty shared workspace. It does
        /// not relax any structural or preservation validation; it only skips the
        /// "scene must be clean" convenience check because the existing workspace WIP is
        /// the reason the scene is dirty and must remain in place.
        /// </summary>
        [MenuItem(DirtyMenuPath)]
        public static void IntegrateDirtyWorkspace()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            {
                Report(EditorApplication.isPlayingOrWillChangePlaymode
                    ? UpdateResult.EditorPlaying
                    : UpdateResult.SceneMismatch, "M02 character/daily-action page");
                return;
            }
            Report(Execute(scene, true, null), "M02 character/daily-action page (dirty workspace)");
        }

        private static void Report(UpdateResult result, string label)
        {
            if (result == UpdateResult.Success)
                Debug.Log($"{label} validated and saved into FoundationScene.");
            else
                Debug.LogError($"{label} updater refused or rolled back: {result}.");
        }

        public static UpdateResult CheckScenePreflight(string path, bool dirty) =>
            CheckScenePreflight(path, dirty, false);

        public static UpdateResult CheckScenePreflight(string path, bool dirty, bool editorPlaying)
        {
            if (editorPlaying) return UpdateResult.EditorPlaying;
            if (path != ScenePath) return UpdateResult.SceneMismatch;
            return dirty ? UpdateResult.SceneDirty : UpdateResult.Success;
        }

        /// <summary>Structural test seam. It never saves, opens, or reloads the supplied scene.</summary>
        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>Fault-injection seam inside the same Undo transaction as construction.</summary>
        public static UpdateResult Update(Scene scene, Action<GameObject> beforeValidation) =>
            Execute(scene, false, beforeValidation);

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            if (!scene.IsValid() || !scene.isLoaded)
                return Invalid(out error, "Scene is invalid or not loaded.");

            Transform[] canvases = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            DailyActionPanel[] panels = Components<DailyActionPanel>(scene);
            Transform[] roots = Named(scene, RootName).ToArray();
            Transform[] opens = Named(scene, OpenButtonName).ToArray();
            if (canvases.Length != 1 || sessions.Length != 1 || panels.Length != 1 ||
                roots.Length != 1 || opens.Length != 1)
                return Invalid(out error,
                    "Expected exactly one Canvas, GameSession, DailyActionPanel, root, and open button.");

            Transform canvas = canvases[0];
            Transform root = roots[0];
            Transform open = opens[0];
            DailyActionPanel panel = panels[0];
            if (root.parent != canvas || !root.gameObject.activeSelf)
                return Invalid(out error,
                    "M02DailyActionRoot must be an active direct child of Canvas.");
            if (panel.transform != root || panel.gameObject != root.gameObject)
                return Invalid(out error, "DailyActionPanel must live on M02DailyActionRoot.");
            if (open.parent != root || !open.gameObject.activeSelf)
                return Invalid(out error,
                    "M02DailyActionOpenButton must be an active direct child of the root.");
            if (!HasButtonVisual(open.GetComponent<Button>()) ||
                !HasRaycastTarget(open.GetComponent<Button>()))
                return Invalid(out error, "The daily-action open button is invalid.");

            Transform panelRoot = Require(scene, PanelName);
            Transform backdrop = Require(scene, BackdropName);
            Transform topBar = Require(scene, TopBarName);
            Transform headerDate = Require(scene, HeaderDateName);
            Transform headerLocation = Require(scene, HeaderLocationName);
            Transform headerMerit = Require(scene, HeaderMeritName);
            Transform headerPeople = Require(scene, HeaderPeopleName);
            Transform headerFunds = Require(scene, HeaderFundsName);
            Transform headerSupplies = Require(scene, HeaderSuppliesName);
            Transform headerIntel = Require(scene, HeaderIntelName);
            Transform navRail = Require(scene, NavRailName);
            ReferenceGalleryNavigator navigator = root.GetComponent<ReferenceGalleryNavigator>();
            Transform rosterHeader = Require(scene, RosterHeaderName);
            Transform title = Require(scene, TitleName);
            Transform summary = Require(scene, SummaryName);
            Transform scroll = Require(scene, ScrollName);
            Transform content = Require(scene, ContentName);
            Transform template = Require(scene, RowTemplateName);
            Transform empty = Require(scene, EmptyName);
            Transform selected = Require(scene, SelectedName);
            Transform durationLabel = Require(scene, DurationLabelName);
            Transform durationInputTransform = Require(scene, DurationInputName);
            Transform rest = Require(scene, RestButtonName);
            Transform training = Require(scene, TrainingButtonName);
            Transform work = Require(scene, WorkButtonName);
            Transform travelKiyosu = Require(scene, TravelKiyosuButtonName);
            Transform travelAtsuta = Require(scene, TravelAtsutaButtonName);
            Transform travelNagoya = Require(scene, TravelNagoyaButtonName);
            Transform interrupt = Require(scene, InterruptButtonName);
            Transform feedback = Require(scene, FeedbackName);
            Transform close = Require(scene, CloseButtonName);
            Transform profilePanel = Require(scene, ProfilePanelName);
            Transform profilePaperGrain = Require(scene, "M02ProfilePaperGrain");
            Transform actionPanel = Require(scene, ActionPanelName);
            Transform actionPaperGrain = Require(scene, "M02ActionPaperGrain");
            Transform inspectorRail = Require(scene, InspectorRailName);
            Transform profileBody = Require(scene, ProfileBodyName);
            Transform relationsBody = Require(scene, RelationsBodyName);
            Transform relationsEmpty = Require(scene, RelationsEmptyName);
            Transform historyBody = Require(scene, HistoryBodyName);
            Transform portrait = Require(scene, ProfilePortraitName);
            Transform profileName = Require(scene, ProfileNameName);
            Transform profileAge = Require(scene, ProfileAgeName);
            Transform profileIdentity = Require(scene, ProfileIdentityName);
            Transform profileRole = Require(scene, ProfileRoleName);
            Transform profileLocation = Require(scene, ProfileLocationName);
            Transform profileStatus = Require(scene, ProfileStatusName);
            Transform creedChip = Require(scene, CreedChipName);
            Transform ambitionChip = Require(scene, AmbitionChipName);
            Transform traitSummary = Require(scene, TraitSummaryName);
            Transform ninjutsu = Require(scene, NinjutsuValueName);
            Transform martial = Require(scene, MartialValueName);
            Transform scholarship = Require(scene, ScholarshipValueName);
            Transform social = Require(scene, SocialValueName);
            Transform ninjutsuFill = Require(scene, NinjutsuFillName);
            Transform martialFill = Require(scene, MartialFillName);
            Transform scholarshipFill = Require(scene, ScholarshipFillName);
            Transform socialFill = Require(scene, SocialFillName);
            Transform actionSubtitle = Require(scene, ActionSubtitleName);
            Transform progressLabel = Require(scene, ProgressLabelName);
            Transform progressFill = Require(scene, ProgressFillName);
            Transform schedule = Require(scene, ScheduleName);
            Transform travelInfo = Require(scene, TravelInfoName);
            Transform travelScene = Require(scene, TravelSceneName);
            Transform travelCard = Require(scene, TravelCardName);
            Transform inspectorTitle = Require(scene, InspectorTitleName);
            Transform inspectorBadge = Require(scene, InspectorBadgeName);
            Transform inspectorSubject = Require(scene, InspectorSubjectName);
            Transform inspectorHint = Require(scene, InspectorHintName);
            Transform inspectorOpen = Require(scene, InspectorOpenName);
            Transform historyText = Require(scene, HistoryTextName);
            Transform profileTab = Require(scene, ProfileTabButtonName);
            Transform relationsTab = Require(scene, RelationsTabButtonName);
            Transform historyTab = Require(scene, HistoryTabButtonName);
            Transform[] relationRoots =
            {
                Require(scene, RelationRootName + "_1"), Require(scene, RelationRootName + "_2"),
                Require(scene, RelationRootName + "_3")
            };
            if (panelRoot == null || backdrop == null || topBar == null || headerDate == null ||
                headerLocation == null || headerMerit == null || headerPeople == null ||
                headerFunds == null || headerSupplies == null || headerIntel == null ||
                navRail == null || rosterHeader == null || navigator == null ||
                !navigator.IsBound || title == null || summary == null ||
                scroll == null || content == null || template == null || empty == null ||
                selected == null || durationLabel == null || durationInputTransform == null ||
                rest == null || training == null || work == null || travelKiyosu == null ||
                travelAtsuta == null || travelNagoya == null || interrupt == null ||
                feedback == null || close == null || profilePanel == null || actionPanel == null ||
                inspectorRail == null || profileBody == null || relationsBody == null ||
                historyBody == null || portrait == null || profileName == null ||
                profileAge == null || profileIdentity == null || profileRole == null ||
                profileLocation == null || profileStatus == null || creedChip == null ||
                ambitionChip == null || traitSummary == null || ninjutsu == null ||
                martial == null || scholarship == null || social == null ||
                ninjutsuFill == null || martialFill == null || scholarshipFill == null ||
                socialFill == null || actionSubtitle == null || progressLabel == null ||
                progressFill == null || schedule == null || travelInfo == null ||
                travelScene == null || travelCard == null || inspectorTitle == null ||
                inspectorBadge == null || inspectorSubject == null || inspectorHint == null ||
                inspectorOpen == null || historyText == null || profileTab == null ||
                relationsTab == null || historyTab == null ||
                relationRoots.Any(item => item == null))
                return Invalid(out error,
                    "M02 character/daily-action hierarchy is incomplete or ambiguous.");

            if (!PageRootsMatchCanvas(canvas, root, navigator))
                return Invalid(out error,
                    "The gallery page-root registration is stale; re-run the M02 updater.");
            if (!FeatureLaunchersMatchCanvas(canvas, navigator))
                return Invalid(out error,
                    "The feature launcher registration is stale; re-run the M02 updater.");

            if (panelRoot.parent != root || panelRoot.gameObject.activeSelf)
                return Invalid(out error,
                    "M02DailyActionPanel must be a hidden direct child of the root.");

            Transform[] directChildren =
            {
                backdrop, title, summary, scroll, empty, profilePanel,
                actionPanel, inspectorRail
            };
            foreach (Transform child in directChildren)
                if (child.parent != panelRoot)
                    return Invalid(out error,
                        "Top-level M02 page regions must be direct children of the panel.");
            if (navRail.parent != root || topBar.parent != root)
                return Invalid(out error,
                    "Shared navigation and header must be direct children of the gallery root.");
            if (selected.parent != relationsBody || historyText.parent != historyBody ||
                profileBody.parent != profilePanel || relationsBody.parent != profilePanel ||
                historyBody.parent != profilePanel)
                return Invalid(out error,
                    "M02 profile tab bodies must nest beneath the profile panel.");

            if (content.parent != scroll || template.parent != content ||
                template.gameObject.activeSelf)
                return Invalid(out error,
                    "The row template must be inactive inside the scroll content.");

            ScrollRect scrollRect = scroll.GetComponent<ScrollRect>();
            RectTransform contentRect = content as RectTransform;
            RectTransform scrollRectTransform = scroll as RectTransform;
            if (scrollRect == null || contentRect == null || scrollRectTransform == null ||
                scrollRect.content != contentRect || scrollRect.viewport != scrollRectTransform ||
                !scrollRect.vertical || scrollRect.horizontal)
                return Invalid(out error,
                    "M02DailyActionScroll must own a vertical ScrollRect over the content.");
            if (content.GetComponent<VerticalLayoutGroup>() == null ||
                content.GetComponent<ContentSizeFitter>() == null)
                return Invalid(out error,
                    "M02 scroll content needs a vertical layout and size fitter.");
            RectTransform templateRect = template as RectTransform;
            if (templateRect == null ||
                templateRect.rect.width > scrollRectTransform.rect.width)
                return Invalid(out error,
                    "The M02 roster row template must fit inside the roster scroll area.");

            Text titleText = title.GetComponent<Text>();
            Text summaryText = summary.GetComponent<Text>();
            Text emptyText = empty.GetComponent<Text>();
            Text selectedText = selected.GetComponent<Text>();
            Text durationValueText = durationInputTransform.Find("Text")?.GetComponent<Text>();
            Text durationLabelText = durationLabel.GetComponent<Text>();
            Text feedbackText = feedback.GetComponent<Text>();
            InputField durationInput = durationInputTransform.GetComponent<InputField>();
            Button restButton = rest.GetComponent<Button>();
            Button trainingButton = training.GetComponent<Button>();
            Button workButton = work.GetComponent<Button>();
            Button travelKiyosuButton = travelKiyosu.GetComponent<Button>();
            Button travelAtsutaButton = travelAtsuta.GetComponent<Button>();
            Button travelNagoyaButton = travelNagoya.GetComponent<Button>();
            Button interruptButton = interrupt.GetComponent<Button>();
            Button closeButton = close.GetComponent<Button>();
            Button travelCardButton = travelCard.GetComponent<Button>();
            Button inspectorButton = inspectorOpen.GetComponent<Button>();
            if (titleText == null || summaryText == null || emptyText == null ||
                selectedText == null || durationLabelText == null || feedbackText == null ||
                durationInput == null || durationValueText == null || restButton == null || trainingButton == null ||
                workButton == null || travelKiyosuButton == null || travelAtsutaButton == null ||
                travelNagoyaButton == null || interruptButton == null || closeButton == null ||
                travelCardButton == null || inspectorButton == null ||
                titleText.font == null || summaryText.font == null || emptyText.font == null ||
                selectedText.font == null || durationLabelText.font == null ||
                feedbackText.font == null || durationValueText.font == null || durationInput.placeholder == null ||
                !HasButtonVisual(restButton) || !HasButtonVisual(trainingButton) ||
                !HasButtonVisual(workButton) || !HasButtonVisual(travelKiyosuButton) ||
                !HasButtonVisual(travelAtsutaButton) || !HasButtonVisual(travelNagoyaButton) ||
                !HasButtonVisual(interruptButton) || !HasButtonVisual(closeButton) ||
                !HasButtonVisual(travelCardButton) || !HasButtonVisual(inspectorButton) ||
                !HasRaycastTarget(durationInput) || !HasRaycastTarget(restButton) ||
                !HasRaycastTarget(trainingButton) || !HasRaycastTarget(workButton) ||
                !HasRaycastTarget(travelKiyosuButton) ||
                !HasRaycastTarget(travelAtsutaButton) ||
                !HasRaycastTarget(travelNagoyaButton) || !HasRaycastTarget(interruptButton) ||
                !HasRaycastTarget(closeButton) || !HasRaycastTarget(travelCardButton) ||
                !HasRaycastTarget(inspectorButton) ||
                panelRoot.GetComponent<Image>() == null ||
                !panelRoot.GetComponent<Image>().raycastTarget ||
                scroll.GetComponent<Image>() == null || !scroll.GetComponent<Image>().raycastTarget ||
                scroll.GetComponent<RectMask2D>() == null)
                return Invalid(out error,
                    "M02 text, input, button, background, mask, or raycast components are invalid.");
            if (durationInput.contentType != InputField.ContentType.IntegerNumber ||
                durationInput.text != "1")
                return Invalid(out error,
                    "The M02 duration input must be an integer field starting at 1.");
            if (durationInput.placeholder.enabled != string.IsNullOrEmpty(durationInput.text))
                return Invalid(out error,
                    "The M02 duration placeholder visibility must match the input value.");
            if (!titleText.text.Contains("每日行动"))
                return Invalid(out error, "The M02 title must contain 每日行动.");
            if (feedbackText.verticalOverflow != VerticalWrapMode.Truncate)
                return Invalid(out error,
                    "The M02 feedback text must truncate instead of overflowing its area.");
            if (!selectedText.text.Contains("人物档案"))
                return Invalid(out error,
                    "The selected-member area must start as the character profile block.");
            if (selectedText.alignment != TextAnchor.UpperLeft ||
                selectedText.verticalOverflow != VerticalWrapMode.Truncate ||
                !selectedText.supportRichText || selectedText.fontSize < 16 ||
                selectedText.lineSpacing < 1f)
                return Invalid(out error,
                    "The character profile text must be a wrapping, truncating rich-text block.");
            if (empty.gameObject.activeSelf)
                return Invalid(out error, "The empty-state label must start hidden for a live roster.");

            Button[] pageButtons =
            {
                Find<Button>(scene, NavHouseholdButtonName),
                Find<Button>(scene, NavCharacterButtonName),
                Find<Button>(scene, NavFamilyButtonName),
                Find<Button>(scene, NavMapButtonName),
                Find<Button>(scene, NavEventsButtonName)
            };
            if (pageButtons.Any(button => button == null || !HasButtonVisual(button) ||
                                           !HasRaycastTarget(button)))
                return Invalid(out error,
                    "The shared navigation must contain five working reference-page buttons.");
            // Roster rail containment in the nav column, measured from the panel top edge.
            RectTransform navRect2 = navRail as RectTransform;
            RectTransform rosterRect = scroll as RectTransform;
            float navTop = -navRect2.anchoredPosition.y;
            float navBottom = navTop + navRect2.rect.height;
            float rosterTop = -rosterRect.anchoredPosition.y;
            float rosterBottom = rosterTop + rosterRect.rect.height;
            if (rosterTop < navTop - 1f || rosterBottom > navBottom + 1f ||
                rosterRect.anchoredPosition.x < 0f ||
                rosterRect.anchoredPosition.x + rosterRect.rect.width >
                navRect2.rect.width + 1f)
                return Invalid(out error,
                    "The roster rail must fit inside the nav column.");

            Sprite paper = AssetDatabase.LoadAssetAtPath<Sprite>(PaperPath);
            Sprite portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);
            Sprite travelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TravelScenePath);
            Sprite parchment = LoadGeneratedSprite(ParchmentAtlasPath, ParchmentSpriteName);
            Sprite indigo = LoadGeneratedSprite(IndigoAtlasPath, IndigoSpriteName);
            if (paper == null || portraitSprite == null || travelSprite == null)
                return Invalid(out error,
                    "The M02 reference-derived paper, portrait or travel art is missing.");
            if (parchment == null || indigo == null)
                return Invalid(out error,
                    "The generated parchment or indigo UI tile is missing.");
            if (backdrop.GetComponent<Image>() == null ||
                backdrop.GetComponent<Image>().sprite != null ||
                profilePanel.GetComponent<Image>() == null ||
                profilePanel.GetComponent<Image>().sprite != parchment ||
                profilePaperGrain == null || profilePaperGrain.parent != profilePanel ||
                profilePaperGrain.GetComponent<Image>() == null ||
                profilePaperGrain.GetComponent<Image>().sprite != paper ||
                actionPanel.GetComponent<Image>() == null ||
                actionPanel.GetComponent<Image>().sprite != parchment ||
                actionPaperGrain == null || actionPaperGrain.parent != actionPanel ||
                actionPaperGrain.GetComponent<Image>() == null ||
                actionPaperGrain.GetComponent<Image>().sprite != paper ||
                portrait.GetComponent<Image>() == null ||
                portrait.GetComponent<Image>().sprite != portraitSprite ||
                travelScene.GetComponent<Image>() == null ||
                travelScene.GetComponent<Image>().sprite != travelSprite)
                return Invalid(out error,
                    "The M02 parchment or portrait surfaces are not bound to the reference art.");
            if (topBar.GetComponent<Image>() == null || navRail.GetComponent<Image>() == null ||
                inspectorRail.GetComponent<Image>() == null ||
                !topBar.GetComponent<Image>().raycastTarget ||
                !inspectorRail.GetComponent<Image>().raycastTarget)
                return Invalid(out error,
                    "The M02 top bar, nav rail or inspector rail surface is invalid.");
            // Generated indigo textile on the dark chrome and atlas icons on the
            // five page buttons must be bound, non-interactive, and behind content.
            if (!HasPatternChild(topBar, "ReferenceTopBarPattern", indigo) ||
                !HasPatternChild(navRail, ReferenceGalleryNavigator.NavPatternChildName, indigo) ||
                !HasPatternChild(navRail.Find("ReferenceNavChrome"),
                    "ReferenceNavChromePattern", indigo))
                return Invalid(out error,
                    "The M02 dark chrome is not bound to the generated indigo pattern.");
            for (int i = 0; i < pageButtons.Length; i++)
            {
                Transform iconChild = pageButtons[i] != null
                    ? pageButtons[i].transform.Find(
                        ReferenceGalleryNavigator.NavIconChildName)
                    : null;
                Image iconImage = iconChild != null ? iconChild.GetComponent<Image>() : null;
                if (iconImage == null || iconImage.sprite == null ||
                    iconImage.sprite != LoadGeneratedIcon(
                        ReferenceGalleryNavigator.PageIconIndices[i]) ||
                    iconImage.raycastTarget)
                    return Invalid(out error,
                        "Every page button must carry its generated atlas icon.");
            }
            // The four action cards show their atlas icon inside the icon tile.
            string[] cardNames = { RestButtonName, TrainingButtonName, WorkButtonName, TravelCardName };
            int[] cardIcons =
            {
                SengokuUiArt.IconRest, SengokuUiArt.IconTraining,
                SengokuUiArt.IconWork, SengokuUiArt.IconHorseTravel
            };
            for (int i = 0; i < cardNames.Length; i++)
            {
                Transform tile = Require(scene, cardNames[i] + "Icon");
                Image tileImage = tile != null ? tile.GetComponent<Image>() : null;
                if (tileImage == null || tileImage.sprite != LoadGeneratedIcon(cardIcons[i]))
                    return Invalid(out error,
                        "The action cards must show the generated atlas icons.");
            }
            if (InspectorWidth < 220f ||
                !Contained((RectTransform)inspectorRail, (RectTransform)panelRoot))
                return Invalid(out error,
                    "The read-only inspector rail must stay a distinct right column.");

            CharacterDailyPageView pageView = Components<CharacterDailyPageView>(scene)
                .FirstOrDefault();
            if (pageView == null || !pageView.IsBound)
                return Invalid(out error,
                    "The character/daily-action page view is missing or incompletely bound.");
            if (relationsEmpty == null || relationsEmpty.parent != relationsBody)
                return Invalid(out error,
                    "The M02 relations empty-state text must be a direct child of M02RelationsBody.");
            Text relationsEmptyText = relationsEmpty.GetComponent<Text>();
            if (relationsEmptyText == null)
                return Invalid(out error,
                    "The M02 relations empty-state text is missing.");
            if (!Bound(pageView, "relationsEmptyText", relationsEmptyText))
                return Invalid(out error,
                    "The CharacterDailyPageView relationsEmptyText reference is not bound.");

            if (!Bound(panel, "gameSession", sessions[0]) ||
                !Bound(panel, "panelRoot", panelRoot.gameObject) ||
                !Bound(panel, "openButton", open.GetComponent<Button>()) ||
                !Bound(panel, "closeButton", closeButton) ||
                !Bound(panel, "titleText", titleText) ||
                !Bound(panel, "summaryText", summaryText) ||
                !Bound(panel, "scrollRect", scrollRect) ||
                !Bound(panel, "content", content) ||
                !Bound(panel, "rowTemplate", template.gameObject) ||
                !Bound(panel, "emptyText", emptyText) ||
                !Bound(panel, "selectedText", selectedText) ||
                !Bound(panel, "durationInput", durationInput) ||
                !Bound(panel, "restButton", restButton) ||
                !Bound(panel, "trainingButton", trainingButton) ||
                !Bound(panel, "workButton", workButton) ||
                !Bound(panel, "travelKiyosuButton", travelKiyosuButton) ||
                !Bound(panel, "travelAtsutaButton", travelAtsutaButton) ||
                !Bound(panel, "travelNagoyaButton", travelNagoyaButton) ||
                !Bound(panel, "interruptButton", interruptButton) ||
                !Bound(panel, "feedbackText", feedbackText) ||
                !Bound(panel, "pageView", pageView))
                return Invalid(out error,
                    "DailyActionPanel serialized references are not bound.");

            WorldClockPanel[] clocks = Components<WorldClockPanel>(scene);
            if (clocks.Length > 1)
                return Invalid(out error, "More than one WorldClockPanel is present.");
            if (clocks.Length == 1 && !Bound(clocks[0], "dailyActionPanel", panel))
                return Invalid(out error,
                    "WorldClockPanel.dailyActionPanel is not bound to the M02 panel.");

            RectTransform panelRect = panelRoot as RectTransform;
            RectTransform canvasRect = canvas as RectTransform;
            RectTransform backdropRect = backdrop as RectTransform;
            RectTransform topRect = topBar as RectTransform;
            RectTransform navRect = navRail as RectTransform;
            RectTransform openRect = open as RectTransform;
            RectTransform profileRect = profilePanel as RectTransform;
            RectTransform actionRect = actionPanel as RectTransform;
            RectTransform inspectorRect = inspectorRail as RectTransform;
            RectTransform scrollAreaRect = scroll as RectTransform;
            RectTransform portraitRect = portrait as RectTransform;
            RectTransform selectedRect = selected as RectTransform;
            RectTransform durationLabelRect = durationLabel as RectTransform;
            RectTransform durationInputRect = durationInputTransform as RectTransform;
            RectTransform restRect = rest as RectTransform;
            RectTransform trainingRect = training as RectTransform;
            RectTransform workRect = work as RectTransform;
            RectTransform interruptRect = interrupt as RectTransform;
            RectTransform feedbackRect = feedback as RectTransform;
            RectTransform closeRect = close as RectTransform;
            RectTransform progressRect = progressFill as RectTransform;
            if (panelRect == null || canvasRect == null || backdropRect == null ||
                topRect == null || navRect == null || openRect == null || profileRect == null ||
                actionRect == null || inspectorRect == null || scrollAreaRect == null ||
                portraitRect == null || selectedRect == null || durationLabelRect == null ||
                durationInputRect == null || restRect == null || trainingRect == null ||
                workRect == null || interruptRect == null || feedbackRect == null ||
                closeRect == null || progressRect == null ||
                !Contained(panelRect, canvasRect) || !Contained(openRect, canvasRect) ||
                !Contained(backdropRect, panelRect) || !Contained(topRect, canvasRect) ||
                !Contained(navRect, canvasRect) || !Contained(profileRect, panelRect) ||
                !Contained(actionRect, panelRect) || !Contained(inspectorRect, panelRect))
                return Invalid(out error,
                    "The M02 page regions must stay inside the 1280x720 canvas.");

            if (selectedRect.rect.width < 360f || selectedRect.rect.height < 300f)
                return Invalid(out error,
                    "The character profile area must be a substantial body block.");
            if (portraitRect.rect.width < 120f || portraitRect.rect.height < 160f)
                return Invalid(out error, "The character portrait must be a visible focal block.");
            // Top-left anchored layout: x is the distance from the panel's left edge.
            float rosterRight = scrollAreaRect.anchoredPosition.x + scrollAreaRect.rect.width;
            float profileLeftEdge = profileRect.anchoredPosition.x;
            if (rosterRight > profileLeftEdge - 10f)
                return Invalid(out error,
                    "The roster rail and character profile must stay in separate columns.");
            float profileRightEdge = profileRect.anchoredPosition.x + profileRect.rect.width;
            if (profileRightEdge + 6f > actionRect.anchoredPosition.x)
                return Invalid(out error,
                    "The profile and action panels must stay in separate columns.");
            float actionRightEdge = actionRect.anchoredPosition.x + actionRect.rect.width;
            if (actionRightEdge + 6f > inspectorRect.anchoredPosition.x)
                return Invalid(out error,
                    "The inspector must be a separate read-only column to the right.");

            RectTransform[] actionChildren =
            {
                durationLabelRect, durationInputRect, restRect, trainingRect, workRect,
                interruptRect, feedbackRect, closeRect, progressRect
            };
            foreach (RectTransform child in actionChildren)
                if (child != null && !Contained(child, actionRect))
                    return Invalid(out error,
                        "An M02 action control lies outside the action panel.");
            RectTransform[] profileChildren =
            {
                portraitRect, profileName as RectTransform, ninjutsuFill as RectTransform,
                martialFill as RectTransform, scholarshipFill as RectTransform,
                socialFill as RectTransform
            };
            foreach (RectTransform child in profileChildren)
                if (child != null && !Contained(child, profileRect))
                    return Invalid(out error,
                        "An M02 profile control lies outside the profile panel.");

            if (Overlaps((RectTransform)title, (RectTransform)summary, panelRect))
                return Invalid(out error, "M02 overlap: title/summary.");
            if (Overlaps((RectTransform)profileName, (RectTransform)portrait, profileRect))
                return Invalid(out error, "M02 overlap: profile name/portrait.");
            if (Overlaps(restRect, trainingRect, actionRect) ||
                Overlaps(workRect, interruptRect, actionRect) ||
                Overlaps(feedbackRect, closeRect, actionRect) ||
                Overlaps(durationLabelRect, durationInputRect, actionRect))
                return Invalid(out error, "M02 overlap inside the action panel.");

            if (!ContainedInRenderBand(canvas, root))
                return Invalid(out error,
                    "M02DailyActionRoot must render above the map HUD and below the title/event overlays.");

            // The read-only developer inspector is a separate surface and must open over
            // the page, never beneath its backdrop.
            Transform inspectorRoot = canvas.Find(InspectorRootName);
            if (inspectorRoot != null && inspectorRoot.GetSiblingIndex() < root.GetSiblingIndex())
                return Invalid(out error,
                    "M03NpcAiInspectorRoot must render above the M02 page.");
            return true;
        }

        private static UpdateResult Execute(Scene scene, bool save,
            Action<GameObject> beforeValidation)
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return UpdateResult.InvalidScene;

            Transform[] canvases = scene.GetRootGameObjects()
                .Where(go => go.name == CanvasName && go.GetComponent<Canvas>() != null)
                .Select(go => go.transform).ToArray();
            GameSession[] sessions = Components<GameSession>(scene);
            Transform canvas = canvases.Length == 1 ? canvases[0] : null;
            GameSession session = sessions.Length == 1 ? sessions[0] : null;
            if (canvas == null || session == null || Components<DailyActionPanel>(scene).Length > 1 ||
                Components<WorldClockPanel>(scene).Length > 1 ||
                Components<CharacterDailyPageView>(scene).Length > 1)
                return UpdateResult.MissingOrDuplicateObjects;

            if (Validate(scene, out _)) return UpdateResult.Success;

            Transform[] existingRoots = Named(scene, RootName).ToArray();
            if (existingRoots.Length > 1)
                return UpdateResult.ExistingPartialControl;
            Transform existingRoot = existingRoots.Length == 1 ? existingRoots[0] : null;
            if (existingRoot != null && !IsReplaceableGeneratedRoot(scene, existingRoot))
                return UpdateResult.ExistingPartialControl;
            if (existingRoot == null && HasAnyM02Object(scene))
                return UpdateResult.ExistingPartialControl;

            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            UpdateResult failure = UpdateResult.Failed;
            try
            {
                Sprite paper = AssetDatabase.LoadAssetAtPath<Sprite>(PaperPath);
                Sprite portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitPath);
                Sprite travelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TravelScenePath);
                Sprite sealSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SealPath);
                Sprite parchment = LoadGeneratedSprite(ParchmentAtlasPath, ParchmentSpriteName);
                Sprite indigo = LoadGeneratedSprite(IndigoAtlasPath, IndigoSpriteName);
                Sprite[] pageIcons = new Sprite[ReferenceGalleryNavigator.PageCount];
                for (int i = 0; i < pageIcons.Length; i++)
                    pageIcons[i] = LoadGeneratedIcon(
                        ReferenceGalleryNavigator.PageIconIndices[i]);
                if (paper == null || portraitSprite == null || travelSprite == null ||
                    sealSprite == null)
                    throw new InvalidOperationException(
                        "Missing M02 reference-derived art. Refresh the project first.");
                if (parchment == null || indigo == null ||
                    pageIcons.Any(icon => icon == null))
                    throw new InvalidOperationException(
                        "Missing generated UI art under Assets/Resources/Generated. " +
                        "Refresh the project first.");

                if (existingRoot != null)
                    Undo.DestroyObjectImmediate(existingRoot.gameObject);
                ObjectSnapshot[] originals = Snapshot(scene);

                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Missing built-in UI font.");

                GameObject rootObject = Create(scene, canvas, RootName);
                RectTransform rootRect = rootObject.GetComponent<RectTransform>();
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.offsetMin = Vector2.zero;
                rootRect.offsetMax = Vector2.zero;

                Button open = CreateButton(scene, rootRect, OpenButtonName, font, "人物档案 · 每日行动",
                    OpenButtonSize.x, OpenButtonSize.y, 13,
                    SengokuUiTheme.IndigoDeep, SengokuUiTheme.Gold);
                PlaceTopLeft(open.transform, OpenButtonPosition, OpenButtonSize);

                GameObject panelObject = Create(scene, rootRect, PanelName, typeof(Image));
                RectTransform panelRect = panelObject.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;
                Image panelImage = panelObject.GetComponent<Image>();
                panelImage.sprite = null;
                panelImage.type = Image.Type.Simple;
                panelImage.color = SengokuUiTheme.IndigoBackdrop;
                panelImage.raycastTarget = true;
                panelObject.SetActive(false);

                Image backdrop = CreateImage(scene, panelRect, BackdropName,
                    SengokuUiTheme.IndigoDeep);
                backdrop.sprite = null;
                backdrop.type = Image.Type.Simple;
                backdrop.raycastTarget = true;
                Stretch(backdrop.rectTransform, 0f, 0f, 0f, 0f);

                Image topBar = CreateImage(scene, rootRect, TopBarName,
                    SengokuUiTheme.IndigoDeep);
                topBar.raycastTarget = true;
                PlaceTopLeft(topBar.transform, new Vector2(0f, 0f), new Vector2(1280f, 52f));
                topBar.gameObject.SetActive(false);
                AddPatternOverlay(scene, topBar.rectTransform, indigo, "ReferenceTopBarPattern");

                Image navRail = CreateImage(scene, rootRect, NavRailName,
                    SengokuUiTheme.IndigoRail);
                navRail.raycastTarget = false;
                PlaceTopLeft(navRail.transform, new Vector2(0f, 52f), new Vector2(NavWidth, 650f));
                AddBorder(scene, navRail.rectTransform, "M02NavEdge", SengokuUiTheme.GoldDim,
                    0f, 1f, left: false, right: true);
                navRail.gameObject.SetActive(false);
                AddPatternOverlay(scene, navRail.rectTransform, indigo,
                    ReferenceGalleryNavigator.NavPatternChildName);

                Image navChrome = CreateImage(scene, navRail.transform, "ReferenceNavChrome",
                    SengokuUiTheme.IndigoRail);
                navChrome.raycastTarget = false;
                PlaceTopLeft(navChrome.transform, Vector2.zero, new Vector2(NavWidth, 270f));
                AddPatternOverlay(scene, navChrome.rectTransform, indigo,
                    "ReferenceNavChromePattern");

                CreateHeader(scene, rootRect, topBar.rectTransform, font, sealSprite);

                Button[] pageButtons = new Button[ReferenceGalleryNavigator.PageCount];
                pageButtons[(int)ReferenceGalleryPage.Household] = CreateButton(scene,
                    navRail.transform, NavHouseholdButtonName, font, "家族内务", 130f, 40f, 12,
                    SengokuUiTheme.IndigoPanel, SengokuUiTheme.ParchmentLight);
                pageButtons[(int)ReferenceGalleryPage.Character] = CreateButton(scene,
                    navRail.transform, NavCharacterButtonName, font, "人物档案", 130f, 40f, 12,
                    SengokuUiTheme.IndigoPanel, SengokuUiTheme.ParchmentLight);
                pageButtons[(int)ReferenceGalleryPage.Family] = CreateButton(scene,
                    navRail.transform, NavFamilyButtonName, font, "家族与继承", 130f, 40f, 12,
                    SengokuUiTheme.IndigoPanel, SengokuUiTheme.ParchmentLight);
                pageButtons[(int)ReferenceGalleryPage.Map] = CreateButton(scene,
                    navRail.transform, NavMapButtonName, font, "地图与出行", 130f, 40f, 12,
                    SengokuUiTheme.IndigoPanel, SengokuUiTheme.ParchmentLight);
                pageButtons[(int)ReferenceGalleryPage.Events] = CreateButton(scene,
                    navRail.transform, NavEventsButtonName, font, "事件抉择", 130f, 40f, 12,
                    SengokuUiTheme.IndigoPanel, SengokuUiTheme.ParchmentLight);
                for (int i = 0; i < pageButtons.Length; i++)
                    PlaceTopLeft(pageButtons[i].transform, new Vector2(10f, 12f + i * 48f),
                        new Vector2(130f, 40f));
                for (int i = 0; i < pageButtons.Length; i++)
                    AddNavIcon(scene, pageButtons[i].transform, pageIcons[i]);

                Text rosterHeader = CreateText(scene, navRail.transform, RosterHeaderName, font,
                    "家 族 名 册", 12f, 300f, 132f, 20f, 10, TextAnchor.MiddleLeft);
                rosterHeader.color = SengokuUiTheme.Gold;
                Image rosterRule = CreateImage(scene, navRail.transform, "M02RosterRule",
                    SengokuUiTheme.GoldDim);
                PlaceTopLeft(rosterRule.transform, new Vector2(12f, 322f),
                    new Vector2(126f, 1f));

                GameObject scrollObject = Create(scene, panelRect, ScrollName,
                    typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
                RectTransform scrollAreaRect = scrollObject.GetComponent<RectTransform>();
                PlaceTopLeft(scrollObject.transform, new Vector2(8f, 332f),
                    new Vector2(134f, 320f));
                Image scrollImage = scrollObject.GetComponent<Image>();
                scrollImage.color = new Color(0.09f, 0.11f, 0.15f, 0.85f);
                scrollImage.raycastTarget = true;
                ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;
                scrollRect.viewport = scrollAreaRect;

                GameObject contentObject = Create(scene, scrollAreaRect, ContentName,
                    typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
                RectTransform contentRect = contentObject.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.sizeDelta = Vector2.zero;
                ConfigureVerticalLayout(contentObject.GetComponent<VerticalLayoutGroup>(), 3);
                ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                scrollRect.content = contentRect;

                Button template = CreateButton(scene, contentRect, RowTemplateName, font,
                    "家族人物", 118f, 46f, 9, new Color(0.13f, 0.16f, 0.21f, 0.95f),
                    new Color(0.85f, 0.82f, 0.72f, 1f));
                Text templateLabel = template.GetComponentInChildren<Text>(true);
                if (templateLabel != null)
                {
                    templateLabel.alignment = TextAnchor.UpperLeft;
                    templateLabel.lineSpacing = 0.95f;
                    // The rail is narrow; the full session line stays in the text but is
                    // clipped to the row instead of spilling into the next one.
                    templateLabel.verticalOverflow = VerticalWrapMode.Truncate;
                }
                LayoutElement templateLayout = template.GetComponent<LayoutElement>();
                templateLayout.preferredWidth = 118f;
                templateLayout.preferredHeight = 46f;
                templateLayout.flexibleHeight = 0f;
                template.gameObject.SetActive(false);

                Text empty = CreateText(scene, panelRect, EmptyName, font,
                    "尚无已登记的家族人物。", 8f, 332f, 134f, 50f, 9,
                    TextAnchor.UpperCenter);
                empty.color = new Color(0.7f, 0.72f, 0.7f, 1f);
                empty.gameObject.SetActive(false);

                Image profilePanel = CreateImage(scene, panelRect, ProfilePanelName, Color.white);
                profilePanel.sprite = parchment;
                profilePanel.color = SengokuUiTheme.ParchmentLight;
                profilePanel.type = Image.Type.Tiled;
                profilePanel.raycastTarget = true;
                PlaceTopLeft(profilePanel.transform, new Vector2(ContentLeft, ContentTop),
                    new Vector2(ProfileWidth, ContentHeight));
                AddPaperOverlay(scene, profilePanel.rectTransform, paper, "M02ProfilePaperGrain");
                AddBorder(scene, profilePanel.rectTransform, "M02ProfileBorder",
                    SengokuUiTheme.PaperLine, 0f, 1f);
                CreateProfile(scene, profilePanel.rectTransform, font, portraitSprite, sealSprite);

                Image actionPanel = CreateImage(scene, panelRect, ActionPanelName, Color.white);
                actionPanel.sprite = parchment;
                actionPanel.color = SengokuUiTheme.ParchmentLight;
                actionPanel.type = Image.Type.Tiled;
                actionPanel.raycastTarget = true;
                PlaceTopLeft(actionPanel.transform, new Vector2(ActionLeft, ContentTop),
                    new Vector2(ActionWidth, ContentHeight));
                AddPaperOverlay(scene, actionPanel.rectTransform, paper, "M02ActionPaperGrain");
                AddBorder(scene, actionPanel.rectTransform, "M02ActionBorder",
                    SengokuUiTheme.PaperLine, 0f, 1f);
                CreateActionPage(scene, panelRect, actionPanel.rectTransform, font);

                Image inspectorRail = CreateImage(scene, panelRect, InspectorRailName,
                    SengokuUiTheme.IndigoPanel);
                inspectorRail.raycastTarget = true;
                PlaceTopLeft(inspectorRail.transform, new Vector2(InspectorLeft, ContentTop),
                    new Vector2(InspectorWidth, ContentHeight));
                AddBorder(scene, inspectorRail.rectTransform, "M02InspectorBorder",
                    SengokuUiTheme.GoldDim, 0f, 1f);
                CreateInspectorRail(scene, inspectorRail.rectTransform, font);

                // Instantiate functional controls with their long-standing names. The layout
                // differs from the first slice, but the bindings and command semantics do not.
                Text title = CreateText(scene, panelRect, TitleName, font,
                    "人物管理 › 人物档案 · 每日行动", 158f, 56f, 520f, 24f, 15,
                    TextAnchor.MiddleLeft);
                title.color = SengokuUiTheme.Parchment;
                Text summary = CreateText(scene, panelRect, SummaryName, font,
                    "当前日期：—　｜　家族登记 0 人", 690f, 58f, 582f, 20f, 9,
                    TextAnchor.MiddleRight);
                summary.color = new Color32(0xA9, 0xA2, 0x90, 0xFF);

                // The old profile text block is retained as the "关系" tab body so the
                // verifiable facts still come from one live projection. It lives inside the
                // profile panel and is hidden until the relations tab is chosen.
                Transform profileContentParent = profilePanel.rectTransform;
                Text selected = CreateText(scene, profileContentParent, SelectedName, font,
                    "<color=#A5392B>人物档案</color>\n当前选择：未选择人物。",
                    14f, 40f, 412f, 560f, 16, TextAnchor.UpperLeft);
                selected.color = SengokuUiTheme.Ink;
                selected.verticalOverflow = VerticalWrapMode.Truncate;
                selected.lineSpacing = 1.15f;
                GameObject relationsBody = WrapBody(scene, profileContentParent,
                    RelationsBodyName, selected, active: false);
                Text relationsEmpty = CreateText(scene, relationsBody.transform,
                    RelationsEmptyName, font, "该人物暂无其他可展示的关系。", 0f, 520f,
                    412f, 40f, 12, TextAnchor.MiddleCenter);
                relationsEmpty.color = SengokuUiTheme.InkSoft;
                relationsEmpty.verticalOverflow = VerticalWrapMode.Truncate;
                relationsEmpty.gameObject.SetActive(false);

                CharacterDailyPageView view = Undo.AddComponent<CharacterDailyPageView>(rootObject);

                Text historyText = CreateText(scene, profileContentParent, HistoryTextName, font,
                    "家族编年史尚无记录。", 14f, 40f, 412f, 560f, 16,
                    TextAnchor.UpperLeft);
                historyText.color = SengokuUiTheme.InkSoft;
                historyText.verticalOverflow = VerticalWrapMode.Truncate;
                historyText.lineSpacing = 1.1f;
                GameObject historyBody = WrapBody(scene, profileContentParent,
                    HistoryBodyName, historyText, active: false);

                PlaceAboveGameHud(canvas, rootObject.transform);

                DailyActionPanel panel = Undo.AddComponent<DailyActionPanel>(rootObject);
                Undo.RecordObject(panel, UndoName);
                BindPanel(panel, session, panelObject, open,
                    Find<Button>(scene, CloseButtonName), title, summary, scrollRect,
                    contentRect, template.gameObject, empty, selected,
                    Find<InputField>(scene, DurationInputName),
                    Find<Button>(scene, RestButtonName), Find<Button>(scene, TrainingButtonName),
                    Find<Button>(scene, WorkButtonName),
                    Find<Button>(scene, TravelKiyosuButtonName),
                    Find<Button>(scene, TravelAtsutaButtonName),
                    Find<Button>(scene, TravelNagoyaButtonName),
                    Find<Button>(scene, InterruptButtonName),
                    Find<Text>(scene, FeedbackName), view);
                BindView(view, session, panel,
                    Components<NpcAiInspectorPanel>(scene).FirstOrDefault(), scene);
                ReferenceGalleryNavigator navigator =
                    Undo.AddComponent<ReferenceGalleryNavigator>(rootObject);
                var pageRoots = new List<ReferenceGalleryPageRoot>();
                foreach (Transform pageRoot in canvas)
                    if (pageRoot != rootObject.transform && !IsExcludedFromPageRoots(pageRoot))
                        pageRoots.Add(new ReferenceGalleryPageRoot(pageRoot.gameObject,
                            PageMaskForRoot(pageRoot.name)));
                navigator.BindReferences(pageRoots.ToArray(), new[]
                {
                    pageButtons[(int)ReferenceGalleryPage.Household],
                    pageButtons[(int)ReferenceGalleryPage.Character],
                    pageButtons[(int)ReferenceGalleryPage.Family],
                    pageButtons[(int)ReferenceGalleryPage.Map],
                    pageButtons[(int)ReferenceGalleryPage.Events]
                }, panelObject, FindGameObject(scene, AddSC013FamilyRosterPanel.PanelName),
                    FindGameObject(scene, AddSC019IndustryPanel.PanelName),
                    FindGameObject(scene, MapRootName), open,
                    navRail.gameObject, topBar.gameObject);
                navigator.BindGameSession(session);
                navigator.BindFeatureLaunchers(FeatureLaunchers(canvas));
                EditorUtility.SetDirty(panel);
                EditorUtility.SetDirty(view);
                EditorUtility.SetDirty(navigator);

                WorldClockPanel[] clocks = Components<WorldClockPanel>(scene);
                if (clocks.Length == 1)
                {
                    Undo.RecordObject(clocks[0], UndoName);
                    clocks[0].BindDailyActionPanel(panel);
                    var clockBindings = new SerializedObject(clocks[0]);
                    SerializedProperty clockPanel = clockBindings.FindProperty("dailyActionPanel");
                    if (clockPanel != null)
                    {
                        clockPanel.objectReferenceValue = panel;
                        clockBindings.ApplyModifiedPropertiesWithoutUndo();
                    }
                    EditorUtility.SetDirty(clocks[0]);
                }

                beforeValidation?.Invoke(rootObject);
                failure = UpdateResult.ValidationFailed;
                Canvas.ForceUpdateCanvases();
                if (!Validate(scene, out string error))
                    throw new InvalidOperationException(error);

                failure = UpdateResult.OriginalObjectsChanged;
                if (!Preserved(scene, originals))
                    throw new InvalidOperationException(
                        "An original scene object was removed or changed.");

                Undo.FlushUndoRecordObjects();
                EditorSceneManager.MarkSceneDirty(scene);
                failure = UpdateResult.SaveFailed;
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity refused to save FoundationScene.");

                Undo.CollapseUndoOperations(group);
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                Debug.LogWarning($"M02 character page updater rolled back: {exception.Message} " +
                                 "Scene may remain dirty; inspect it before saving.");
                return failure;
            }
        }

        // ------------------------------------------------------------------ header

        private static void CreateHeader(Scene scene, Transform panel, Transform topBar,
            Font font, Sprite sealSprite)
        {
            Image seal = CreateImage(scene, topBar, "M02TitleSeal", Color.white);
            seal.sprite = sealSprite;
            seal.raycastTarget = false;
            PlaceTopLeft(seal.transform, new Vector2(196f, 12f), new Vector2(28f, 28f));

            Text brand = CreateText(scene, topBar, "M02BrandText", font, "影 之 一 族",
                18f, 4f, 190f, 36f, 25, TextAnchor.MiddleLeft);
            brand.color = SengokuUiTheme.ParchmentLight;
            brand.fontStyle = FontStyle.Bold;
            Text brandEn = CreateText(scene, topBar, "M02BrandSubText", font,
                "S E N G O K U   C H R O N I C L E", 20f, 36f, 220f, 12f, 7,
                TextAnchor.MiddleLeft);
            brandEn.color = SengokuUiTheme.GoldDim;

            Text date = CreateText(scene, topBar, HeaderDateName, font, "日期未初始化",
                238f, 14f, 220f, 24f, 12, TextAnchor.MiddleLeft);
            date.color = SengokuUiTheme.Parchment;
            Text location = CreateText(scene, topBar, HeaderLocationName, font, "所在　未知",
                470f, 14f, 170f, 24f, 11, TextAnchor.MiddleLeft);
            location.color = new Color32(0xBF, 0xB3, 0x99, 0xFF);

            Image divider = CreateImage(scene, topBar, "M02HeaderDivider",
                new Color32(0x4A, 0x42, 0x34, 0xFF));
            PlaceTopLeft(divider.transform, new Vector2(648f, 8f), new Vector2(1f, 36f));

            CreateResourceChip(scene, topBar, font, "功名", "兵", HeaderMeritName, 662f);
            CreateResourceChip(scene, topBar, font, "人口", "人", HeaderPeopleName, 778f);
            CreateResourceChip(scene, topBar, font, "资金", "贯", HeaderFundsName, 894f);
            CreateResourceChip(scene, topBar, font, "物资", "石", HeaderSuppliesName, 1010f);
            CreateResourceChip(scene, topBar, font, "情报", "卷", HeaderIntelName, 1126f);

            Image goldLine = CreateImage(scene, topBar, "M02HeaderGoldLine",
                SengokuUiTheme.GoldDim);
            PlaceTopLeft(goldLine.transform, new Vector2(0f, 50f), new Vector2(1280f, 2f));
        }

        private static void CreateResourceChip(Scene scene, Transform parent, Font font,
            string label, string glyph, string valueName, float x)
        {
            Image tile = CreateImage(scene, parent, "M02Chip_" + label,
                new Color(0.12f, 0.145f, 0.19f, 0.75f));
            tile.raycastTarget = false;
            PlaceTopLeft(tile.transform, new Vector2(x, 8f), new Vector2(108f, 36f));
            AddBorder(scene, tile.rectTransform, "M02ChipBorder_" + label,
                new Color32(0x4A, 0x42, 0x34, 0xFF), 0f, 1f);

            Text icon = CreateText(scene, tile.transform, "M02ChipIcon_" + label, font,
                glyph, 6f, 4f, 20f, 26f, 13, TextAnchor.MiddleCenter);
            icon.color = SengokuUiTheme.Gold;
            Text caption = CreateText(scene, tile.transform, "M02ChipLabel_" + label, font,
                label, 26f, 2f, 42f, 14f, 8, TextAnchor.MiddleLeft);
            caption.color = new Color32(0x9C, 0x93, 0x7F, 0xFF);
            Text value = CreateText(scene, tile.transform, valueName, font, "—",
                26f, 16f, 74f, 18f, 12, TextAnchor.MiddleLeft);
            value.color = SengokuUiTheme.ParchmentLight;
        }

        // ------------------------------------------------------------------ profile

        private static void CreateProfile(Scene scene, RectTransform parent, Font font,
            Sprite portraitSprite, Sprite sealSprite)
        {
            GameObject profileBody = new GameObject(ProfileBodyName, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(profileBody, parent.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(profileBody, UndoName);
            Undo.SetTransformParent(profileBody.transform, parent, UndoName);
            RectTransform bodyRect = profileBody.GetComponent<RectTransform>();
            Stretch(bodyRect, 0f, 0f, 0f, 0f);

            CreateTab(scene, parent, font, ProfileTabButtonName, ProfileTabUnderlineName,
                "人物档案", 8f, 6f, 146f);
            CreateTab(scene, parent, font, RelationsTabButtonName, RelationsTabUnderlineName,
                "关系", 158f, 6f, 116f);
            CreateTab(scene, parent, font, HistoryTabButtonName, HistoryTabUnderlineName,
                "经历", 278f, 6f, 116f);

            Image frame = CreateImage(scene, bodyRect, "M02ProfilePortraitFrame",
                SengokuUiTheme.PaperLine);
            frame.raycastTarget = false;
            PlaceTopLeft(frame.transform, new Vector2(14f, 46f), new Vector2(158f, 200f));
            Image portrait = CreateImage(scene, frame.transform, ProfilePortraitName, Color.white);
            portrait.sprite = portraitSprite;
            portrait.type = Image.Type.Simple;
            portrait.raycastTarget = false;
            Stretch(portrait.rectTransform, 2f, 2f, 2f, 2f);

            Text name = CreateText(scene, bodyRect, ProfileNameName, font, "未选择人物",
                184f, 46f, 212f, 36f, 24, TextAnchor.MiddleLeft);
            name.color = SengokuUiTheme.Ink;
            name.fontStyle = FontStyle.Bold;

            Image seal = CreateImage(scene, bodyRect, "M02ProfileSeal", Color.white);
            seal.sprite = sealSprite;
            seal.raycastTarget = false;
            PlaceTopLeft(seal.transform, new Vector2(402f, 50f), new Vector2(28f, 28f));

            CreateInfoRow(scene, bodyRect, font, "年龄", ProfileAgeName, 92f);
            CreateInfoRow(scene, bodyRect, font, "身份", ProfileIdentityName, 118f);
            CreateInfoRow(scene, bodyRect, font, "职务", ProfileRoleName, 144f);
            CreateInfoRow(scene, bodyRect, font, "所在", ProfileLocationName, 170f);
            CreateInfoRow(scene, bodyRect, font, "状态", ProfileStatusName, 196f);

            CreateSectionHeader(scene, bodyRect, font, "M02TraitHeader", "性格特质", 250f);
            CreateChip(scene, bodyRect, font, CreedChipName, "家训未记录", 14f, 274f, 168f);
            CreateChip(scene, bodyRect, font, AmbitionChipName, "未立抱负", 190f, 274f, 236f);
            Text traitSummary = CreateText(scene, bodyRect, TraitSummaryName, font,
                "家训与抱负将在此显示。", 14f, 302f, 412f, 46f, 9, TextAnchor.UpperLeft);
            traitSummary.color = SengokuUiTheme.InkSoft;
            traitSummary.verticalOverflow = VerticalWrapMode.Truncate;

            CreateSectionHeader(scene, bodyRect, font, "M02StatsHeader", "能力属性", 350f);
            CreateStat(scene, bodyRect, font, "忍术", NinjutsuValueName, NinjutsuFillName,
                "忍", 12f, 374f);
            CreateStat(scene, bodyRect, font, "武艺", MartialValueName, MartialFillName,
                "武", 114f, 374f);
            CreateStat(scene, bodyRect, font, "学问", ScholarshipValueName,
                ScholarshipFillName, "学", 216f, 374f);
            CreateStat(scene, bodyRect, font, "社交", SocialValueName, SocialFillName,
                "交", 318f, 374f);

            CreateSectionHeader(scene, bodyRect, font, "M02RelationHeader", "关系网（主要关系）",
                440f);
            for (int i = 0; i < 3; i++)
                CreateRelationCard(scene, bodyRect, font, i, 14f + i * 138f, 464f);
        }

        private static void CreateTab(Scene scene, Transform parent, Font font, string buttonName,
            string underlineName, string label, float x, float y, float width)
        {
            Button button = CreateButton(scene, parent, buttonName, font, label, width, 26f, 10,
                (Color)SengokuUiTheme.ParchmentDeep, (Color)SengokuUiTheme.Ink, false);
            PlaceTopLeft(button.transform, new Vector2(x, y), new Vector2(width, 26f));
            Image underline = CreateImage(scene, parent, underlineName,
                buttonName == ProfileTabButtonName
                    ? SengokuUiTheme.Vermilion
                    : new Color(0f, 0f, 0f, 0f));
            underline.raycastTarget = false;
            PlaceTopLeft(underline.transform, new Vector2(x, y + 26f), new Vector2(width, 2f));
        }

        private static void CreateInfoRow(Scene scene, Transform parent, Font font,
            string label, string valueName, float y)
        {
            Image chip = CreateImage(scene, parent, "M02ProfileInfoLabel_" + label,
                new Color32(0xDD, 0xCC, 0xAD, 0xFF));
            chip.raycastTarget = false;
            PlaceTopLeft(chip.transform, new Vector2(184f, y), new Vector2(48f, 20f));
            Text labelText = CreateText(scene, chip.transform, "Text", font, label, 0f, 0f,
                48f, 20f, 9, TextAnchor.MiddleCenter);
            labelText.color = SengokuUiTheme.InkSoft;
            Text value = CreateText(scene, parent, valueName, font, "—", 240f, y, 186f, 20f, 10,
                TextAnchor.MiddleLeft);
            value.color = SengokuUiTheme.Ink;
            value.verticalOverflow = VerticalWrapMode.Truncate;
        }

        private static void CreateSectionHeader(Scene scene, Transform parent, Font font,
            string name, string label, float y)
        {
            Image bar = CreateImage(scene, parent, name + "Bar", SengokuUiTheme.Vermilion);
            bar.raycastTarget = false;
            PlaceTopLeft(bar.transform, new Vector2(14f, y + 5f), new Vector2(3f, 15f));
            Text text = CreateText(scene, parent, name, font, label, 22f, y, 260f, 24f, 12,
                TextAnchor.MiddleLeft);
            text.color = SengokuUiTheme.Ink;
            text.fontStyle = FontStyle.Bold;
        }

        private static void CreateChip(Scene scene, Transform parent, Font font, string name,
            string label, float x, float y, float width)
        {
            Image chip = CreateImage(scene, parent, name, new Color32(0xDD, 0xCC, 0xAD, 0xFF));
            chip.raycastTarget = false;
            PlaceTopLeft(chip.transform, new Vector2(x, y), new Vector2(width, 22f));
            Text text = CreateText(scene, chip.transform, name + "_Text", font, label, 6f, 0f,
                width - 12f, 22f, 10, TextAnchor.MiddleLeft);
            text.color = SengokuUiTheme.Ink;
        }

        private static void CreateStat(Scene scene, Transform parent, Font font, string label,
            string valueName, string fillName, string glyph, float x, float y)
        {
            Image cell = CreateImage(scene, parent, "M02StatCell_" + label,
                SengokuUiTheme.ParchmentLight);
            cell.raycastTarget = false;
            PlaceTopLeft(cell.transform, new Vector2(x, y), new Vector2(98f, 58f));
            AddBorder(scene, cell.rectTransform, "M02StatBorder_" + label,
                SengokuUiTheme.PaperLine, 0f, 1f);
            Text icon = CreateText(scene, cell.transform, "M02StatIcon_" + label, font, glyph,
                6f, 4f, 16f, 18f, 11, TextAnchor.MiddleCenter);
            icon.color = SengokuUiTheme.Vermilion;
            Text caption = CreateText(scene, cell.transform, "M02StatLabel_" + label, font,
                label, 22f, 4f, 44f, 18f, 10, TextAnchor.MiddleLeft);
            caption.color = SengokuUiTheme.InkSoft;
            Text value = CreateText(scene, cell.transform, valueName, font, "—",
                66f, 4f, 26f, 18f, 12, TextAnchor.MiddleRight);
            value.color = SengokuUiTheme.Ink;
            value.fontStyle = FontStyle.Bold;
            Image bar = CreateImage(scene, cell.transform, "M02StatBar_" + label,
                new Color32(0xD3, 0xC2, 0xA4, 0xFF));
            bar.raycastTarget = false;
            PlaceTopLeft(bar.transform, new Vector2(6f, 42f), new Vector2(86f, 6f));
            Image fill = CreateImage(scene, bar.transform, fillName, SengokuUiTheme.Jade);
            fill.raycastTarget = false;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            Stretch(fill.rectTransform, 1f, 1f, 1f, 1f);
        }

        private static void CreateRelationCard(Scene scene, Transform parent, Font font,
            int index, float x, float y)
        {
            string suffix = "_" + (index + 1);
            Image card = CreateImage(scene, parent, RelationRootName + suffix,
                new Color(1f, 1f, 1f, 0.34f));
            card.raycastTarget = false;
            PlaceTopLeft(card.transform, new Vector2(x, y), new Vector2(128f, 116f));
            AddBorder(scene, card.rectTransform, "M02RelationBorder" + suffix,
                SengokuUiTheme.PaperLine, 0f, 1f);

            Image avatar = CreateImage(scene, card.transform, "M02RelationAvatar" + suffix,
                SengokuUiTheme.IndigoPanel);
            avatar.raycastTarget = false;
            PlaceTopLeft(avatar.transform, new Vector2(47f, 8f), new Vector2(34f, 34f));
            Text glyph = CreateText(scene, avatar.transform, "Text", font,
                index == 0 ? "家" : index == 1 ? "亲" : "友", 0f, 0f, 34f, 34f, 12,
                TextAnchor.MiddleCenter);
            glyph.color = SengokuUiTheme.Gold;

            Text name = CreateText(scene, card.transform, RelationNameName + suffix, font,
                "—", 4f, 46f, 120f, 18f, 10, TextAnchor.MiddleCenter);
            name.color = SengokuUiTheme.Ink;
            name.fontStyle = FontStyle.Bold;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            Text detail = CreateText(scene, card.transform, RelationDetailName + suffix, font,
                "—", 4f, 64f, 120f, 44f, 8, TextAnchor.UpperCenter);
            detail.color = SengokuUiTheme.InkSoft;
            detail.verticalOverflow = VerticalWrapMode.Truncate;
        }

        // ------------------------------------------------------------------ action page

        private static void CreateActionPage(Scene scene, Transform panel, RectTransform parent,
            Font font)
        {
            CreateSectionHeader(scene, parent, font, "M02ActionHeader", "日常行动安排", 8f);
            Text subtitle = CreateText(scene, parent, ActionSubtitleName, font,
                "为当前人物安排本回合的行动计划", 22f, 34f, 366f, 18f, 9,
                TextAnchor.MiddleLeft);
            subtitle.color = SengokuUiTheme.InkSoft;

            Text daysLabel = CreateText(scene, parent, DurationLabelName, font, "行动天数",
                14f, 62f, 54f, 22f, 10, TextAnchor.MiddleLeft);
            daysLabel.color = SengokuUiTheme.Ink;
            InputField durationInput = CreateIntegerInput(scene, parent, DurationInputName, font,
                "1", 70f, 58f, 46f, 24f);
            _ = durationInput;
            Text suffix = CreateText(scene, parent, "M02DurationSuffixText", font, "天",
                120f, 62f, 20f, 22f, 9, TextAnchor.MiddleLeft);
            suffix.color = SengokuUiTheme.InkSoft;

            Text progress = CreateText(scene, parent, ProgressLabelName, font, "行动天数　尚未安排",
                150f, 62f, 236f, 22f, 8, TextAnchor.MiddleRight);
            progress.color = SengokuUiTheme.InkSoft;
            Image progressBar = CreateImage(scene, parent, "M02ActionProgressBar",
                new Color32(0xD3, 0xC2, 0xA4, 0xFF));
            progressBar.raycastTarget = false;
            PlaceTopLeft(progressBar.transform, new Vector2(14f, 88f), new Vector2(372f, 9f));
            Image progressFill = CreateImage(scene, progressBar.transform, ProgressFillName,
                SengokuUiTheme.Jade);
            progressFill.raycastTarget = false;
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            progressFill.fillOrigin = 0;
            progressFill.fillAmount = 0f;
            Stretch(progressFill.rectTransform, 1f, 1f, 1f, 1f);

            Text cardsHeader = CreateText(scene, parent, "M02ActionCardsHeader", font,
                "可选行动（点击加入日程）", 14f, 104f, 300f, 20f, 10, TextAnchor.MiddleLeft);
            cardsHeader.color = SengokuUiTheme.Ink;
            cardsHeader.fontStyle = FontStyle.Bold;

            Button rest = CreateActionCard(scene, parent, font, RestButtonName, "休息", "休",
                "恢复人物状态", LoadGeneratedIcon(SengokuUiArt.IconRest), 14f, 126f);
            Button training = CreateActionCard(scene, parent, font, TrainingButtonName, "训练",
                "训", "安排训练日程", LoadGeneratedIcon(SengokuUiArt.IconTraining), 208f, 126f);
            Button work = CreateActionCard(scene, parent, font, WorkButtonName, "工作", "工",
                "执行家族任务", LoadGeneratedIcon(SengokuUiArt.IconWork), 14f, 196f);
            Button travelCard = CreateActionCard(scene, parent, font, TravelCardName, "旅行",
                "旅", "前往他地联络", LoadGeneratedIcon(SengokuUiArt.IconHorseTravel), 208f, 196f);
            travelCard.interactable = false;
            travelCard.transition = Selectable.Transition.None;
            _ = rest;
            _ = training;
            _ = work;
            _ = travelCard;

            Text scheduleHeader = CreateText(scene, parent, "M02ScheduleHeaderText", font,
                "本回合日程", 14f, 268f, 300f, 20f, 10, TextAnchor.MiddleLeft);
            scheduleHeader.color = SengokuUiTheme.Ink;
            scheduleHeader.fontStyle = FontStyle.Bold;
            Image scheduleRule = CreateImage(scene, parent, "M02ScheduleRule",
                SengokuUiTheme.PaperLine);
            scheduleRule.raycastTarget = false;
            PlaceTopLeft(scheduleRule.transform, new Vector2(14f, 288f), new Vector2(372f, 1f));
            Text schedule = CreateText(scene, parent, ScheduleName, font,
                "本回合尚无日程。", 14f, 294f, 372f, 46f, 9, TextAnchor.UpperLeft);
            schedule.color = SengokuUiTheme.InkSoft;
            schedule.verticalOverflow = VerticalWrapMode.Truncate;

            Text travelHeader = CreateText(scene, parent, "M02TravelHeaderText", font,
                "旅行目的地", 14f, 342f, 300f, 20f, 10, TextAnchor.MiddleLeft);
            travelHeader.color = SengokuUiTheme.Ink;
            travelHeader.fontStyle = FontStyle.Bold;
            Image travelScene = CreateImage(scene, parent, TravelSceneName, Color.white);
            travelScene.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TravelScenePath);
            travelScene.type = Image.Type.Simple;
            travelScene.raycastTarget = false;
            PlaceTopLeft(travelScene.transform, new Vector2(288f, 362f), new Vector2(98f, 72f));
            AddBorder(scene, travelScene.rectTransform, "M02TravelSceneBorder",
                SengokuUiTheme.PaperLine, 0f, 1f);
            Text travelInfo = CreateText(scene, parent, TravelInfoName, font,
                "旅途中无法再次启程。", 14f, 362f, 266f, 72f, 8.5f,
                TextAnchor.UpperLeft);
            travelInfo.color = SengokuUiTheme.InkSoft;
            travelInfo.verticalOverflow = VerticalWrapMode.Truncate;

            Button kiyosu = CreateButton(scene, parent, TravelKiyosuButtonName, font, "前往清洲",
                120f, 26f, 9, (Color)SengokuUiTheme.ParchmentDeep, (Color)SengokuUiTheme.Ink);
            PlaceTopLeft(kiyosu.transform, new Vector2(14f, 442f), new Vector2(120f, 26f));
            Button atsuta = CreateButton(scene, parent, TravelAtsutaButtonName, font, "前往热田",
                120f, 26f, 9, (Color)SengokuUiTheme.ParchmentDeep, (Color)SengokuUiTheme.Ink);
            PlaceTopLeft(atsuta.transform, new Vector2(142f, 442f), new Vector2(120f, 26f));
            Button nagoya = CreateButton(scene, parent, TravelNagoyaButtonName, font, "前往那古野",
                120f, 26f, 9, (Color)SengokuUiTheme.ParchmentDeep, (Color)SengokuUiTheme.Ink);
            PlaceTopLeft(nagoya.transform, new Vector2(270f, 442f), new Vector2(120f, 26f));

            Text feedback = CreateText(scene, parent, FeedbackName, font, string.Empty,
                14f, 476f, 372f, 48f, 8.5f, TextAnchor.UpperLeft);
            feedback.color = SengokuUiTheme.VermilionDark;
            feedback.verticalOverflow = VerticalWrapMode.Truncate;
            feedback.gameObject.SetActive(false);

            Button interrupt = CreateButton(scene, parent, InterruptButtonName, font,
                "中断当前行动", 210f, 32f, 10, (Color)SengokuUiTheme.Vermilion,
                (Color)SengokuUiTheme.ParchmentLight);
            PlaceTopLeft(interrupt.transform, new Vector2(14f, 530f), new Vector2(210f, 32f));
            Button close = CreateButton(scene, parent, CloseButtonName, font, "返回",
                90f, 32f, 10, (Color)SengokuUiTheme.IndigoDeep,
                (Color)SengokuUiTheme.Parchment);
            PlaceTopLeft(close.transform, new Vector2(232f, 530f), new Vector2(90f, 32f));
        }

        /// <summary>Loads one generated icon atlas sprite by index (null-safe).</summary>
        private static Sprite LoadGeneratedIcon(int index) =>
            LoadGeneratedSprite(IconsAtlasPath, "ui_icons_atlas_" + index);

        private static Button CreateActionCard(Scene scene, Transform parent, Font font,
            string name, string title, string glyph, string subtitle, Sprite icon, float x, float y)
        {
            Button button = CreateButton(scene, parent, name, font, string.Empty,
                182f, 62f, 10, new Color32(0xF4, 0xEB, 0xD8, 0xFF),
                (Color)SengokuUiTheme.Ink, false);
            PlaceTopLeft(button.transform, new Vector2(x, y), new Vector2(182f, 62f));
            AddBorder(scene, (RectTransform)button.transform, name + "Border",
                SengokuUiTheme.PaperLine, 0f, 1f);
            Image iconTile = CreateImage(scene, button.transform, name + "Icon",
                new Color32(0xE4, 0xD6, 0xBC, 0xFF));
            iconTile.raycastTarget = false;
            PlaceTopLeft(iconTile.transform, new Vector2(8f, 12f), new Vector2(36f, 38f));
            Text glyphText = CreateText(scene, iconTile.transform, "Text", font, glyph, 0f, 0f,
                36f, 38f, 16, TextAnchor.MiddleCenter);
            glyphText.color = SengokuUiTheme.VermilionDark;
            if (icon != null)
            {
                iconTile.sprite = icon;
                iconTile.type = Image.Type.Simple;
                iconTile.color = Color.white;
                iconTile.preserveAspect = true;
                glyphText.gameObject.SetActive(false);
            }
            Text heading = CreateText(scene, button.transform, name + "Title", font, title,
                52f, 8f, 120f, 22f, 12, TextAnchor.MiddleLeft);
            heading.color = SengokuUiTheme.Ink;
            heading.fontStyle = FontStyle.Bold;
            Text caption = CreateText(scene, button.transform, name + "Subtitle", font,
                subtitle, 52f, 30f, 120f, 26f, 8, TextAnchor.UpperLeft);
            caption.color = SengokuUiTheme.InkSoft;
            caption.verticalOverflow = VerticalWrapMode.Truncate;
            return button;
        }

        private static InputField CreateIntegerInput(Scene scene, Transform parent, string name,
            Font font, string value, float x, float y, float width, float height)
        {
            Image image = CreateImage(scene, parent, name, new Color32(0xE7, 0xD8, 0xBB, 0xFF));
            image.raycastTarget = true;
            PlaceTopLeft(image.transform, new Vector2(x, y), new Vector2(width, height));
            GameObject go = image.gameObject;
            InputField input = Undo.AddComponent<InputField>(go);
            input.targetGraphic = image;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.characterLimit = 3;

            Text text = CreateText(scene, (RectTransform)go.transform, "Text", font, value,
                0f, 0f, width - 10f, height - 4f, 11, TextAnchor.MiddleCenter);
            text.supportRichText = false;
            text.raycastTarget = false;
            text.color = SengokuUiTheme.Ink;
            Text placeholder = CreateText(scene, (RectTransform)go.transform, "Placeholder",
                font, "天数", 0f, 0f, width - 10f, height - 4f, 10, TextAnchor.MiddleCenter);
            placeholder.color = SengokuUiTheme.InkFaint;
            placeholder.raycastTarget = false;
            placeholder.enabled = string.IsNullOrEmpty(value);
            var serializedInput = new SerializedObject(input);
            SerializedProperty placeholderValue = serializedInput.FindProperty("m_Placeholder");
            SerializedProperty textValue = serializedInput.FindProperty("m_Text");
            if (placeholderValue == null || textValue == null)
                throw new InvalidOperationException("InputField is missing serialized UI fields.");
            placeholderValue.objectReferenceValue = placeholder;
            textValue.stringValue = value;
            serializedInput.ApplyModifiedPropertiesWithoutUndo();
            return input;
        }

        // ------------------------------------------------------------------ inspector rail

        private static void CreateInspectorRail(Scene scene, RectTransform parent, Font font)
        {
            Image topLine = CreateImage(scene, parent, "M02InspectorTopLine",
                SengokuUiTheme.Gold);
            topLine.raycastTarget = false;
            PlaceTopLeft(topLine.transform, new Vector2(0f, 0f), new Vector2(InspectorWidth, 2f));

            Text title = CreateText(scene, parent, InspectorTitleName, font,
                "AI 自治调试（开发者）", 10f, 10f, 190f, 22f, 11, TextAnchor.MiddleLeft);
            title.color = SengokuUiTheme.Gold;
            title.fontStyle = FontStyle.Bold;

            Image badge = CreateImage(scene, parent, "M02InspectorBadge", SengokuUiTheme.Vermilion);
            badge.raycastTarget = false;
            PlaceTopLeft(badge.transform, new Vector2(208f, 12f), new Vector2(38f, 18f));
            Text badgeText = CreateText(scene, badge.transform, InspectorBadgeName, font,
                "只读", 0f, 0f, 38f, 18f, 8, TextAnchor.MiddleCenter);
            badgeText.color = SengokuUiTheme.ParchmentLight;

            Text subject = CreateText(scene, parent, InspectorSubjectName, font, "对象：未选择",
                10f, 38f, 236f, 18f, 9, TextAnchor.MiddleLeft);
            subject.color = new Color32(0xBF, 0xB3, 0x99, 0xFF);

            Text hint = CreateText(scene, parent, InspectorHintName, font,
                "NPC 自主性只读投影。本页不在此栏提供排程、中断或重新规划操作。",
                12f, 60f, 236f, 190f, 8, TextAnchor.UpperLeft);
            hint.color = new Color32(0xBF, 0xB3, 0x99, 0xFF);
            hint.verticalOverflow = VerticalWrapMode.Truncate;
            hint.lineSpacing = 1.05f;

            Button open = CreateButton(scene, parent, InspectorOpenName, font,
                "打开完整调试面板", 236f, 34f, 9,
                new Color32(0x2C, 0x36, 0x4A, 0xFF), (Color)SengokuUiTheme.Gold, false);
            PlaceTopLeft(open.transform, new Vector2(10f, 262f), new Vector2(236f, 34f));
            AddBorder(scene, (RectTransform)open.transform, "M02InspectorOpenBorder",
                SengokuUiTheme.GoldDim, 0f, 1f);

            Image rule = CreateImage(scene, parent, "M02InspectorRule", SengokuUiTheme.GoldDim);
            rule.raycastTarget = false;
            PlaceTopLeft(rule.transform, new Vector2(10f, 314f), new Vector2(236f, 1f));
            Text footer = CreateText(scene, parent, "M02InspectorFooter", font,
                "需求 · 候选 · 当前计划 · 决策记录", 10f, 324f, 236f, 40f, 8,
                TextAnchor.UpperLeft);
            footer.color = new Color32(0x78, 0x7D, 0x86, 0xFF);
        }

        // ------------------------------------------------------------------ tab bodies

        /// <summary>
        /// Moves a tab's text block into its own toggleable body container beneath the
        /// profile panel, so exactly one tab body is visible at a time.
        /// </summary>
        private static GameObject WrapBody(Scene scene, Transform parent, string bodyName,
            Text content, bool active)
        {
            GameObject body = new GameObject(bodyName, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(body, parent.gameObject.scene);
            Undo.RegisterCreatedObjectUndo(body, UndoName);
            Undo.SetTransformParent(body.transform, parent, UndoName);
            RectTransform rect = body.GetComponent<RectTransform>();
            PlaceTopLeft(rect, new Vector2(14f, 40f), new Vector2(412f, 560f));
            Undo.SetTransformParent(content.transform, rect, UndoName);
            PlaceTopLeft(content.transform, new Vector2(0f, 0f), new Vector2(412f, 560f));
            body.SetActive(active);
            return body;
        }

        // ------------------------------------------------------------------ binding

        private static void BindPanel(DailyActionPanel panel, GameSession session,
            GameObject panelRoot, Button open, Button close, Text title, Text summary,
            ScrollRect scroll, Transform content, GameObject template, Text empty,
            Text selected, InputField duration, Button rest, Button training, Button work,
            Button travelKiyosu, Button travelAtsuta, Button travelNagoya, Button interrupt,
            Text feedback, CharacterDailyPageView view)
        {
            panel.BindReferences(session, panelRoot, open, close, title, summary, scroll, content,
                template, empty, selected, duration, rest, training, work, travelKiyosu,
                travelAtsuta, travelNagoya, interrupt, feedback, view);
            var serialized = new SerializedObject(panel);
            Set(serialized, "gameSession", session);
            Set(serialized, "panelRoot", panelRoot);
            Set(serialized, "openButton", open);
            Set(serialized, "closeButton", close);
            Set(serialized, "titleText", title);
            Set(serialized, "summaryText", summary);
            Set(serialized, "scrollRect", scroll);
            Set(serialized, "content", content);
            Set(serialized, "rowTemplate", template);
            Set(serialized, "emptyText", empty);
            Set(serialized, "selectedText", selected);
            Set(serialized, "durationInput", duration);
            Set(serialized, "restButton", rest);
            Set(serialized, "trainingButton", training);
            Set(serialized, "workButton", work);
            Set(serialized, "travelKiyosuButton", travelKiyosu);
            Set(serialized, "travelAtsutaButton", travelAtsuta);
            Set(serialized, "travelNagoyaButton", travelNagoya);
            Set(serialized, "interruptButton", interrupt);
            Set(serialized, "feedbackText", feedback);
            Set(serialized, "pageView", view);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BindView(CharacterDailyPageView view, GameSession session,
            DailyActionPanel panel, NpcAiInspectorPanel inspector, Scene scene)
        {
            var serialized = new SerializedObject(view);
            Set(serialized, "gameSession", session);
            Set(serialized, "dailyActionPanel", panel);
            Set(serialized, "inspectorPanel", inspector);
            Set(serialized, "headerDateText", Find<Text>(scene, HeaderDateName));
            Set(serialized, "headerLocationText", Find<Text>(scene, HeaderLocationName));
            Set(serialized, "headerMeritText", Find<Text>(scene, HeaderMeritName));
            Set(serialized, "headerPeopleText", Find<Text>(scene, HeaderPeopleName));
            Set(serialized, "headerFundsText", Find<Text>(scene, HeaderFundsName));
            Set(serialized, "headerSuppliesText", Find<Text>(scene, HeaderSuppliesName));
            Set(serialized, "headerIntelText", Find<Text>(scene, HeaderIntelName));
            Set(serialized, "portraitImage", Find<Image>(scene, ProfilePortraitName));
            Set(serialized, "profileNameText", Find<Text>(scene, ProfileNameName));
            Set(serialized, "profileAgeText", Find<Text>(scene, ProfileAgeName));
            Set(serialized, "profileIdentityText", Find<Text>(scene, ProfileIdentityName));
            Set(serialized, "profileRoleText", Find<Text>(scene, ProfileRoleName));
            Set(serialized, "profileLocationText", Find<Text>(scene, ProfileLocationName));
            Set(serialized, "profileStatusText", Find<Text>(scene, ProfileStatusName));
            Set(serialized, "creedChipText", Find<Text>(scene, CreedChipName + "_Text"));
            Set(serialized, "ambitionChipText", Find<Text>(scene, AmbitionChipName + "_Text"));
            Set(serialized, "traitSummaryText", Find<Text>(scene, TraitSummaryName));
            Set(serialized, "ninjutsuValueText", Find<Text>(scene, NinjutsuValueName));
            Set(serialized, "martialValueText", Find<Text>(scene, MartialValueName));
            Set(serialized, "scholarshipValueText", Find<Text>(scene, ScholarshipValueName));
            Set(serialized, "socialValueText", Find<Text>(scene, SocialValueName));
            Set(serialized, "ninjutsuFill", Find<Image>(scene, NinjutsuFillName));
            Set(serialized, "martialFill", Find<Image>(scene, MartialFillName));
            Set(serialized, "scholarshipFill", Find<Image>(scene, ScholarshipFillName));
            Set(serialized, "socialFill", Find<Image>(scene, SocialFillName));

            var relationRoots = new UnityEngine.Object[3];
            var relationNames = new UnityEngine.Object[3];
            var relationDetails = new UnityEngine.Object[3];
            for (int i = 0; i < 3; i++)
            {
                string suffix = "_" + (i + 1);
                relationRoots[i] = FindGameObject(scene, RelationRootName + suffix);
                relationNames[i] = Find<Text>(scene, RelationNameName + suffix);
                relationDetails[i] = Find<Text>(scene, RelationDetailName + suffix);
            }
            SetArray(serialized, "relationRoots", relationRoots);
            SetArray(serialized, "relationNameTexts", relationNames);
            SetArray(serialized, "relationDetailTexts", relationDetails);
            Set(serialized, "relationsEmptyText", Find<Text>(scene, RelationsEmptyName));

            Set(serialized, "profileBody", FindGameObject(scene, ProfileBodyName));
            Set(serialized, "relationsBody", FindGameObject(scene, RelationsBodyName));
            Set(serialized, "historyBody", FindGameObject(scene, HistoryBodyName));
            Set(serialized, "profileTabButton", Find<Button>(scene, ProfileTabButtonName));
            Set(serialized, "relationsTabButton", Find<Button>(scene, RelationsTabButtonName));
            Set(serialized, "historyTabButton", Find<Button>(scene, HistoryTabButtonName));
            Set(serialized, "profileTabImage", Find<Image>(scene, ProfileTabUnderlineName));
            Set(serialized, "relationsTabImage", Find<Image>(scene, RelationsTabUnderlineName));
            Set(serialized, "historyTabImage", Find<Image>(scene, HistoryTabUnderlineName));
            Set(serialized, "historyText", Find<Text>(scene, HistoryTextName));
            Set(serialized, "actionSubtitleText", Find<Text>(scene, ActionSubtitleName));
            Set(serialized, "progressLabelText", Find<Text>(scene, ProgressLabelName));
            Set(serialized, "progressFill", Find<Image>(scene, ProgressFillName));
            Set(serialized, "scheduleText", Find<Text>(scene, ScheduleName));
            Set(serialized, "travelInfoText", Find<Text>(scene, TravelInfoName));
            Set(serialized, "travelSceneImage", Find<Image>(scene, TravelSceneName));
            Set(serialized, "travelCardButton", Find<Button>(scene, TravelCardName));
            Set(serialized, "restCardImage", TargetImage(scene, RestButtonName));
            Set(serialized, "trainingCardImage", TargetImage(scene, TrainingButtonName));
            Set(serialized, "workCardImage", TargetImage(scene, WorkButtonName));
            Set(serialized, "travelCardImage", TargetImage(scene, TravelCardName));
            Set(serialized, "inspectorSubjectText", Find<Text>(scene, InspectorSubjectName));
            Set(serialized, "inspectorHintText", Find<Text>(scene, InspectorHintName));
            Set(serialized, "inspectorOpenButton", Find<Button>(scene, InspectorOpenName));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            view.BindSources(session, panel, inspector);
        }

        private static void Set(SerializedObject serialized, string field,
            UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject serialized, string field,
            UnityEngine.Object[] values)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
                return;
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static T Find<T>(Scene scene, string name) where T : UnityEngine.Object
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T[] found = root.GetComponentsInChildren<T>(true);
                for (int i = 0; i < found.Length; i++)
                {
                    string objectName = found[i] is Component component
                        ? component.gameObject.name
                        : found[i] is GameObject gameObject ? gameObject.name : null;
                    if (string.Equals(objectName, name, StringComparison.Ordinal))
                        return found[i];
                }
            }
            return null;
        }

        private static GameObject FindGameObject(Scene scene, string name)
        {
            Transform found = Find<Transform>(scene, name);
            return found != null ? found.gameObject : null;
        }

        private static Image TargetImage(Scene scene, string name)
        {
            Button button = Find<Button>(scene, name);
            return button != null ? button.targetGraphic as Image : null;
        }

        // ------------------------------------------------------------------ helpers

        private static void ConfigureVerticalLayout(VerticalLayoutGroup layout, int spacing)
        {
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(4, 4, 4, 4);
        }

        private static bool ContainedInRenderBand(Transform canvas, Transform target)
        {
            int index = target.GetSiblingIndex();
            string[] above =
            {
                MapRootName, IndustryRootName, EducationRootName, ResearchRootName,
                FormationRootName
            };
            string[] below = { TitleRootName, EventRootName, TacticalOverlayName };
            foreach (string name in above)
            {
                Transform other = canvas.Find(name);
                if (other != null && index <= other.GetSiblingIndex())
                    return false;
            }
            foreach (string name in below)
            {
                Transform other = canvas.Find(name);
                if (other != null && index >= other.GetSiblingIndex())
                    return false;
            }
            return true;
        }

        private static void PlaceAboveGameHud(Transform canvas, Transform target)
        {
            // The page is a full-screen view: every game HUD root must render beneath it,
            // while the title/event overlays, the M06 tactical sheet, and the read-only M03
            // inspector stay above.
            var children = new List<Transform>();
            for (int i = 0; i < canvas.childCount; i++)
                children.Add(canvas.GetChild(i));

            int next = 0;
            foreach (Transform child in children)
            {
                if (child == target || child.name == RootName ||
                    child.name == InspectorRootName || child.name == TitleRootName ||
                    child.name == EventRootName || child.name == TacticalOverlayName)
                    continue;
                child.SetSiblingIndex(next++);
            }
            target.SetSiblingIndex(next++);
            Transform inspector = canvas.Find(InspectorRootName);
            if (inspector != null)
                inspector.SetSiblingIndex(next++);
            Transform title = canvas.Find(TitleRootName);
            if (title != null)
                title.SetSiblingIndex(next++);
            Transform eventRoot = canvas.Find(EventRootName);
            if (eventRoot != null)
                eventRoot.SetSiblingIndex(next++);
            Transform tacticalOverlay = canvas.Find(TacticalOverlayName);
            if (tacticalOverlay != null)
                tacticalOverlay.SetSiblingIndex(next++);
        }

        private static bool IsReplaceableGeneratedRoot(Scene scene, Transform root)
        {
            if (root == null || root.name != RootName)
                return false;
            // Every M02 object lives under this root and the tool owns them, so a previous
            // generated layout (including the simpler first slice without the page view)
            // can be replaced inside the same Undo transaction. Anything else is refused.
            if (root.GetComponent<DailyActionPanel>() == null)
                return false;
            Transform open = root.Find(OpenButtonName);
            if (open == null || open.GetComponent<Button>() == null)
                return false;
            Transform panel = root.Find(PanelName);
            return panel != null && panel.GetComponent<Image>() != null;
        }

        private static bool HasAnyM02Object(Scene scene) =>
            Named(scene, RootName).Any() || Named(scene, PanelName).Any() ||
            Named(scene, OpenButtonName).Any() || Components<DailyActionPanel>(scene).Length != 0 ||
            Components<CharacterDailyPageView>(scene).Length != 0;

        private static bool Preserved(Scene scene, ObjectSnapshot[] originals)
        {
            foreach (ObjectSnapshot original in originals)
            {
                Transform transform = original.Transform;
                if (transform == null)
                    return false;
                if (!string.Equals(transform.name, original.Name, StringComparison.Ordinal))
                    return false;
                if (transform.gameObject.activeSelf != original.Active)
                    return false;
                if (transform.parent != original.Parent)
                    return false;
            }
            return true;
        }

        private static ObjectSnapshot[] Snapshot(Scene scene)
        {
            Transform[] all = AllTransforms(scene);
            var snapshots = new ObjectSnapshot[all.Length];
            for (int i = 0; i < all.Length; i++)
            {
                Transform transform = all[i];
                snapshots[i] = new ObjectSnapshot
                {
                    Transform = transform,
                    Name = transform.name,
                    Parent = transform.parent,
                    Active = transform.gameObject.activeSelf
                };
            }
            return snapshots;
        }

        private struct ObjectSnapshot
        {
            public Transform Transform;
            public string Name;
            public Transform Parent;
            public bool Active;
        }

        private static GameObject Create(Scene scene, Transform parent, string name,
            params Type[] components)
        {
            var go = new GameObject(name, typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, UndoName);
            Undo.SetTransformParent(go.transform, parent, UndoName);
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
            go.layer = parent.gameObject.layer;
            foreach (Type component in components)
                Undo.AddComponent(go, component);
            return go;
        }

        private static Image CreateImage(Scene scene, Transform parent, string name, Color color)
        {
            GameObject go = Create(scene, parent, name, typeof(Image));
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void AddBorder(Scene scene, RectTransform parent, string name,
            Color color, float inset, float thickness, bool left = true, bool right = true)
        {
            float width = parent.rect.width;
            float height = parent.rect.height;
            if (width <= 1f || height <= 1f)
            {
                width = parent.sizeDelta.x;
                height = parent.sizeDelta.y;
            }
            Image top = CreateImage(scene, parent, name + "_Top", color);
            PlaceTopLeft(top.transform, new Vector2(inset, inset),
                new Vector2(width - inset * 2f, thickness));
            Image bottom = CreateImage(scene, parent, name + "_Bottom", color);
            PlaceTopLeft(bottom.transform, new Vector2(inset, height - inset - thickness),
                new Vector2(width - inset * 2f, thickness));
            if (left)
            {
                Image leftLine = CreateImage(scene, parent, name + "_Left", color);
                PlaceTopLeft(leftLine.transform, new Vector2(inset, inset),
                    new Vector2(thickness, height - inset * 2f));
            }
            if (right)
            {
                Image rightLine = CreateImage(scene, parent, name + "_Right", color);
                PlaceTopLeft(rightLine.transform,
                    new Vector2(width - inset - thickness, inset),
                    new Vector2(thickness, height - inset * 2f));
            }
        }

        private static Text CreateText(Scene scene, Transform parent, string name, Font font,
            string value, float x, float y, float width, float height, float fontSize,
            TextAnchor alignment)
        {
            GameObject go = Create(scene, parent, name, typeof(Text));
            Text text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = Mathf.RoundToInt(fontSize);
            text.text = value;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            PlaceTopLeft(text.transform, new Vector2(x, y), new Vector2(width, height));
            return text;
        }

        private static Button CreateButton(Scene scene, Transform parent, string name, Font font,
            string label, float width, float height, int fontSize, Color background,
            Color foreground, bool addLayoutElement = true)
        {
            GameObject go = Create(scene, parent, name, typeof(Image), typeof(Button));
            if (addLayoutElement)
            {
                LayoutElement layout = Undo.AddComponent<LayoutElement>(go);
                layout.preferredWidth = width;
                layout.preferredHeight = height;
                layout.flexibleHeight = 0f;
            }
            Image image = go.GetComponent<Image>();
            image.color = background;
            image.raycastTarget = true;
            Button button = go.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f);
            button.colors = colors;
            Text text = CreateText(scene, (RectTransform)go.transform, "Label", font, label, 0f, 0f,
                width, height, fontSize, TextAnchor.MiddleCenter);
            text.color = foreground;
            return button;
        }

        private static void PlaceTopLeft(Transform target, Vector2 position, Vector2 size)
        {
            if (!(target is RectTransform rect))
                return;
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(position.x, -position.y);
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect, float left, float top, float right,
            float bottom)
        {
            Undo.RecordObject(rect, UndoName);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void AddPaperOverlay(Scene scene, RectTransform parent, Sprite paper,
            string name)
        {
            Image overlay = CreateImage(scene, parent, name, new Color(1f, 1f, 1f, 0.11f));
            overlay.sprite = paper;
            overlay.type = Image.Type.Simple;
            overlay.raycastTarget = false;
            Stretch(overlay.rectTransform, 0f, 0f, 0f, 0f);
            overlay.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// Stretches the generated indigo textile over a dark rail or bar as a
        /// non-interactive first-sibling overlay, so the pattern reads while the
        /// surface keeps its flat fallback colour underneath.
        /// </summary>
        private static void AddPatternOverlay(Scene scene, RectTransform parent, Sprite indigo,
            string name)
        {
            Image overlay = CreateImage(scene, parent, name, Color.white);
            overlay.sprite = indigo;
            overlay.type = Image.Type.Tiled;
            overlay.raycastTarget = false;
            Stretch(overlay.rectTransform, 0f, 0f, 0f, 0f);
            overlay.transform.SetAsFirstSibling();
        }

        /// <summary>
        /// Places one generated atlas icon at the left edge of a page button. The
        /// label shifts right so text and picture never overlap.
        /// </summary>
        private static void AddNavIcon(Scene scene, Transform button, Sprite icon)
        {
            GameObject iconObject = Create(scene, button,
                ReferenceGalleryNavigator.NavIconChildName, typeof(Image));
            RectTransform rect = iconObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchoredPosition = new Vector2(6f, 0f);
            rect.sizeDelta = new Vector2(28f, 28f);
            Image image = iconObject.GetComponent<Image>();
            image.sprite = icon;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = false;
            Transform label = button.Find("Label");
            if (label is RectTransform labelRect)
            {
                labelRect.anchorMin = new Vector2(0f, 0f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.offsetMin = new Vector2(36f, 0f);
                labelRect.offsetMax = new Vector2(-6f, 0f);
            }
        }

        private static bool HasButtonVisual(Button button) =>
            button != null && button.targetGraphic != null &&
            button.GetComponentInChildren<Text>(true) != null;

        /// <summary>True when the named stretched child carries the indigo tile and
        /// never blocks raycasts.</summary>
        private static bool HasPatternChild(Transform parent, string name, Sprite indigo)
        {
            Transform child = parent != null ? parent.Find(name) : null;
            Image image = child != null ? child.GetComponent<Image>() : null;
            return image != null && image.sprite == indigo && !image.raycastTarget;
        }

        private static bool HasRaycastTarget(Button button) =>
            button != null && button.targetGraphic != null && button.targetGraphic.raycastTarget;

        private static ReferenceGalleryPageMask PageMaskForRoot(string name)
        {
            switch (name)
            {
                case "FamilyRosterRoot":
                case "SuccessionRoot":
                case "FamilyChronicleRoot":
                case "SC022FamilyFormationRoot":
                case "SC021ResearchRoot":
                case "FamilyDoctrineOpenButton":
                    return ReferenceGalleryPageMask.Family;
                case "SC020EducationRoot":
                    return ReferenceGalleryPageMask.Character;
                case "IndustryRoot":
                    return ReferenceGalleryPageMask.Household;
                case "EventChoiceRoot":
                    return ReferenceGalleryPageMask.Household | ReferenceGalleryPageMask.Events;
                case "M2WorldMapRoot":
                case "WorldClockPanel":
                case "TownNpcButtonTemplates":
                case "TownNpcDialoguePanelHost":
                case "TownNpcDialogueRoot":
                case "TownNpcRosterRoot":
                case "M4SupplyShortageMissionRoot":
                    return ReferenceGalleryPageMask.Map;
                case "M03NpcAiInspectorRoot":
                    return ReferenceGalleryPageMask.Character;
                default:
                    return ReferenceGalleryPageMask.None;
            }
        }

        /// <summary>
        /// Canvas roots the gallery must never own. The campaign review, the title screen and
        /// the tactical sheet are full-screen overlays driven by their own controllers, so
        /// deactivating them on a page switch would drop the review's live subscriptions or
        /// fight the title lifecycle. The family-doctrine body is a panel its launcher opens,
        /// so navigation must not force it visible either. Excluded roots are neither
        /// registered nor counted as expected, which keeps them untouched on every page.
        /// </summary>
        private static bool IsExcludedFromPageRoots(Transform candidate) =>
            candidate != null &&
            (candidate.name == CampaignReviewPanel.RootObjectName ||
             candidate.name == TacticalMissionPanel.OverlayObjectName ||
             candidate.name == TitleRootName ||
             candidate.name == DoctrineRootName);

        /// <summary>
        /// True when the navigator's serialized page-root registration still matches what
        /// the current mapping would produce for this canvas. A stale registration keeps a
        /// generated root hidden on every page, so validation must reject it instead of
        /// letting <see cref="Update(Scene)"/> report success without repairing the scene.
        /// Every registered entry must be a distinct Canvas child that is not an overlay and
        /// carries its mapped mask; together with the count that covers omissions too.
        /// </summary>
        private static bool PageRootsMatchCanvas(Transform canvas, Transform pageRoot,
            ReferenceGalleryNavigator navigator)
        {
            int expected = 0;
            for (int i = 0; i < canvas.childCount; i++)
                if (canvas.GetChild(i) != pageRoot && !IsExcludedFromPageRoots(canvas.GetChild(i)))
                    expected++;

            SerializedProperty roots = new SerializedObject(navigator).FindProperty("pageRoots");
            if (roots == null || !roots.isArray || roots.arraySize != expected)
                return false;
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < roots.arraySize; i++)
            {
                SerializedProperty entry = roots.GetArrayElementAtIndex(i);
                SerializedProperty root = entry.FindPropertyRelative("root");
                SerializedProperty visibleOn = entry.FindPropertyRelative("visibleOn");
                var registered = root != null
                    ? root.objectReferenceValue as GameObject
                    : null;
                if (registered == null || visibleOn == null ||
                    registered == pageRoot.gameObject ||
                    IsExcludedFromPageRoots(registered.transform) ||
                    registered.transform.parent != canvas ||
                    !seen.Add(registered) ||
                    (ReferenceGalleryPageMask)visibleOn.intValue !=
                    PageMaskForRoot(registered.name))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// The feature entries whose roots open an SC-020/021/022/024 panel from a page tab.
        /// Their launcher control is the root itself (doctrine) or its first direct button
        /// child (education, research, formation).
        /// </summary>
        private static readonly string[] FeatureLauncherRootNames =
        {
            EducationRootName, ResearchRootName, FormationRootName, DoctrineOpenButtonName
        };

        private static bool IsFeatureLauncherRoot(Transform candidate) =>
            candidate != null && FeatureLauncherRootNames.Contains(candidate.name);

        private static ReferenceGalleryPageRoot[] FeatureLaunchers(Transform canvas)
        {
            var launchers = new List<ReferenceGalleryPageRoot>();
            foreach (string name in FeatureLauncherRootNames)
            {
                Transform launcher = canvas.Find(name);
                if (launcher != null)
                    launchers.Add(new ReferenceGalleryPageRoot(launcher.gameObject,
                        PageMaskForRoot(name)));
            }
            return launchers.ToArray();
        }

        /// <summary>
        /// True when the navigator's launcher list names exactly the feature roots present on
        /// this Canvas, each as its own direct child with the mapped page mask and a working
        /// launcher button. The shelf placement is runtime presentation, but a launcher that
        /// is not registered here would stay beneath the opaque page and be unclickable.
        /// </summary>
        private static bool FeatureLaunchersMatchCanvas(Transform canvas,
            ReferenceGalleryNavigator navigator)
        {
            int expected = 0;
            for (int i = 0; i < canvas.childCount; i++)
                if (IsFeatureLauncherRoot(canvas.GetChild(i)))
                    expected++;

            SerializedProperty launchers =
                new SerializedObject(navigator).FindProperty("featureLaunchers");
            if (launchers == null || !launchers.isArray || launchers.arraySize != expected)
                return false;
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < launchers.arraySize; i++)
            {
                SerializedProperty entry = launchers.GetArrayElementAtIndex(i);
                SerializedProperty root = entry.FindPropertyRelative("root");
                SerializedProperty visibleOn = entry.FindPropertyRelative("visibleOn");
                var registered = root != null
                    ? root.objectReferenceValue as GameObject
                    : null;
                if (registered == null || visibleOn == null ||
                    registered.transform.parent != canvas ||
                    !IsFeatureLauncherRoot(registered.transform) ||
                    !seen.Add(registered) ||
                    (ReferenceGalleryPageMask)visibleOn.intValue !=
                    PageMaskForRoot(registered.name) ||
                    !HasLauncherButton(registered.transform))
                    return false;
            }
            return true;
        }

        private static bool HasLauncherButton(Transform root)
        {
            Button button = root != null ? root.GetComponent<Button>() : null;
            for (int i = 0; root != null && button == null && i < root.childCount; i++)
                button = root.GetChild(i).GetComponent<Button>();
            return button != null && HasButtonVisual(button) && HasRaycastTarget(button);
        }

        private static bool HasRaycastTarget(InputField input) =>
            input != null && input.targetGraphic != null && input.targetGraphic.raycastTarget;

        private static bool Bound(UnityEngine.Object target, string field,
            UnityEngine.Object expected)
        {
            SerializedProperty property = new SerializedObject(target).FindProperty(field);
            return property != null && expected != null && property.objectReferenceValue == expected;
        }

        private static bool HasRaycastTarget(Image image) =>
            image != null && image.raycastTarget;

        private static bool Contained(RectTransform child, RectTransform parent)
        {
            if (child == null || parent == null)
                return false;
            var corners = new Vector3[4];
            child.GetWorldCorners(corners);
            Rect bounds = parent.rect;
            foreach (Vector3 corner in corners)
            {
                Vector3 point = parent.InverseTransformPoint(corner);
                if (point.x < bounds.xMin - ContainmentTolerance ||
                    point.x > bounds.xMax + ContainmentTolerance ||
                    point.y < bounds.yMin - ContainmentTolerance ||
                    point.y > bounds.yMax + ContainmentTolerance)
                    return false;
            }
            return true;
        }

        private static bool Overlaps(RectTransform first, RectTransform second,
            RectTransform space)
        {
            if (first == null || second == null || space == null)
                return false;
            Rect a = WorldRect(first, space);
            Rect b = WorldRect(second, space);
            const float tolerance = 0.75f;
            return a.xMin < b.xMax - tolerance && a.xMax > b.xMin + tolerance &&
                   a.yMin < b.yMax - tolerance && a.yMax > b.yMin + tolerance;
        }

        private static Rect WorldRect(RectTransform rect, RectTransform space)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 min = space.InverseTransformPoint(corners[0]);
            Vector3 max = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static bool Invalid(out string error, string message)
        {
            error = message;
            return false;
        }

        private static Transform Require(Scene scene, string name)
        {
            Transform[] found = Named(scene, name).ToArray();
            return found.Length == 1 ? found[0] : null;
        }

        private static IEnumerable<Transform> Named(Scene scene, string name) =>
            AllTransforms(scene).Where(transform => transform.name == name);

        private static Transform[] AllTransforms(Scene scene) =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();

        private static T[] Components<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
    }
}
