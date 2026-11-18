using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Transactional updater for the single town overview under the existing town root.
    /// It keeps the city illustration and residents on the left, puts the local work that
    /// can be done here in the right column, and folds the existing commission and
    /// tactical entries into that column. One Undo group per run; the scene is validated
    /// before saving and the group is rolled back on failure. It never reloads the scene.
    /// </summary>
    public static class UpdateTownOverview
    {
        private const string MenuPath = "Tools/Sengoku Chronicle/Update Town Overview";
        private const string RootName = "TownOverviewRoot";
        private const string ActivityName = "LocalActions";
        private const string ActivityButtonsName = "TownActivityButtons";
        private const string ActivityTemplateName = "ActivityButtonTemplate";
        private const string LegacyActivityName = "Rumors";
        private const string CommissionEntryName = "M5CommissionOpenButton";
        private const string TacticalEntryName = "M6TacticalOpenButton";
        private const string UndoName = "Update Town Overview";

        private const float PageInset = 28f;
        private const float ColumnWidth = 300f;
        private const float ActionHeight = 40f;
        private const float FooterWidth = 240f;
        private const float FooterHeight = 56f;

        // The town page lives in a band owned by the scene (M2WorldMapRoot/TownMenuRoot), and
        // ReferenceGalleryNavigator restyles that band at runtime. Layout validation reads the
        // live band rect instead of restating a size here, so a scene whose band no longer fits
        // the page is refused instead of silently overflowing it.
        private static readonly Vector2 ArtSize = new Vector2(540f, 304f);
        private static readonly Vector2 ResidentsSize = new Vector2(300f, 96f);
        private static readonly Vector2 FooterSize = new Vector2(FooterWidth, FooterHeight);
        private static readonly Vector2 TitleSize = new Vector2(640f, 46f);
        private static readonly Vector2 FeatureSize = new Vector2(640f, 34f);
        private static readonly Vector2 ActivitySize = new Vector2(ColumnWidth, 34f);

        // Kiyosu can offer one activity per kind at once. The fixed activity area is sized
        // from the panel's own card metrics, and its capacity is the panel's kind count, so
        // the runtime cards and this area cannot drift apart.
        private static readonly Vector2 ActivityButtonsSize =
            new Vector2(ColumnWidth, TownOverviewPanel.ActivityAreaHeight);
        private static readonly Vector2 ColumnActionSize = new Vector2(ColumnWidth, ActionHeight);

        private const float ArtBottom = 200f;
        private const float ResidentsBottom = 96f;
        private const float FooterBottom = 28f;
        private const float TitleTop = -10f;
        private const float FeatureTop = -58f;
        private const float ActivityTop = -8f;
        private const float ActivityButtonsTop = -46f;
        private const float ColumnActionGap = 10f;
        private const float ColumnActionStep = 44f;

        // The column below the activity area follows the area's own height, so adding an
        // activity kind pushes the mission entry down instead of overlapping it.
        private static float ColumnActionTop =>
            ActivityButtonsTop - TownOverviewPanel.ActivityAreaHeight - ColumnActionGap;

        private static readonly Color PageBackground = new Color(0.88f, 0.83f, 0.70f, 0.98f);
        private static readonly Color Ink = new Color(0.13f, 0.16f, 0.19f, 1f);
        private static readonly Color ActionBackground = new Color(0.20f, 0.24f, 0.29f, 1f);
        private static readonly Color ActionLabel = new Color(0.92f, 0.86f, 0.69f, 1f);

        public enum UpdateResult { Success, WrongScene, DirtyScene, Playing, MissingBindings, AlreadyExists, Invalid, Failed }

        [MenuItem(MenuPath)]
        private static void RunMenu()
        {
            Scene scene = SceneManager.GetActiveScene();
            UpdateResult result = EditorApplication.isPlayingOrWillChangePlaymode
                ? UpdateResult.Playing
                : scene.path != "Assets/Scenes/FoundationScene.unity" ? UpdateResult.WrongScene
                : scene.isDirty ? UpdateResult.DirtyScene : Execute(scene, true, null);
            if (result == UpdateResult.Success)
                Debug.Log("Town overview was validated and saved under the existing TownMenuRoot.");
            else
                Debug.LogError($"Town overview update refused or rolled back: {result}.");
        }

        public static UpdateResult Update(Scene scene) => Execute(scene, false, null);

        /// <summary>
        /// Isolated-scene seam. The fault runs after the page is laid out, after the panel and
        /// the controller reference are bound, and before validation, so a test observes the
        /// same integration state a refused run has to roll back.
        /// </summary>
        public static UpdateResult Update(Scene scene, Action fault) => Execute(scene, false, fault);

        public static bool Validate(Scene scene, out string error)
        {
            error = null;
            if (!scene.IsValid()) { error = "Scene is invalid."; return false; }
            GameObject[] roots = scene.GetRootGameObjects();
            Transform town = roots.SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(x => x.name == "TownMenuRoot");
            Transform overview = town != null ? town.Find(RootName) : null;
            TownOverviewPanel panel = overview != null ? overview.GetComponent<TownOverviewPanel>() : null;
            if (town == null || overview == null || panel == null || overview.parent != town)
            {
                error = "Town overview root, panel, or parent town root is missing.";
                return false;
            }

            string[] required = { "TownArt", "TownName", "TownFeature", "Residents", ActivityName,
                ActivityButtonsName, "RosterButton", "MissionButton" };
            foreach (string name in required)
            {
                if (overview.Find(name) == null)
                {
                    error = $"Town overview is missing '{name}'.";
                    return false;
                }
            }
            if (overview.Find(ActivityButtonsName).Find(ActivityTemplateName)?.GetComponent<Button>() == null)
            {
                error = "Town activity button template is missing or unbound.";
                return false;
            }

            var controller = town.parent != null ? town.parent.GetComponent<WorldMapController>() : null;
            if (controller == null || !Bound(controller, "townOverviewPanel", panel))
            {
                error = "Town overview is not bound to its WorldMapController.";
                return false;
            }
            if (!PanelIsBound(panel))
            {
                error = "Town overview labels, illustration, or actions are not bound.";
                return false;
            }

            Transform commission = FindTownEntry(town, CommissionEntryName);
            Transform tactical = FindTownEntry(town, TacticalEntryName);
            if (commission == null || tactical == null)
            {
                error = "The escort commission or tactical town entry is missing.";
                return false;
            }
            if (!AnchoredToTownPage(commission, town) || !AnchoredToTownPage(tactical, town))
            {
                error = "The escort commission and tactical entries must be anchored to the town page.";
                return false;
            }
            if (overview.GetSiblingIndex() >= TownLevelRoot(commission, town).GetSiblingIndex() ||
                overview.GetSiblingIndex() >= TownLevelRoot(tactical, town).GetSiblingIndex())
            {
                error = "The town page must render behind the commission and tactical entries.";
                return false;
            }

            return LayoutIsClear(overview, commission, tactical, out error);
        }

        private static UpdateResult Execute(Scene scene, bool save, Action fault)
        {
            if (!scene.IsValid()) return UpdateResult.Invalid;
            Transform town = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(x => x.name == "TownMenuRoot");
            Transform map = town != null ? town.parent : null;
            WorldMapController controller = map != null ? map.GetComponent<WorldMapController>() : null;
            Button returnButton = town != null ? town.Find("ReturnToMapButton")?.GetComponent<Button>() : null;
            GameObject sessionObject = scene.GetRootGameObjects()
                .SelectMany(x => x.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(x => x.name == "GameSession")?.gameObject;
            if (town == null || controller == null || returnButton == null || sessionObject == null)
                return UpdateResult.MissingBindings;

            Transform commission = FindTownEntry(town, CommissionEntryName);
            Transform tactical = FindTownEntry(town, TacticalEntryName);
            if (commission == null || tactical == null)
            {
                Debug.LogWarning(
                    $"Town overview needs the existing {CommissionEntryName} and {TacticalEntryName} entries.");
                return UpdateResult.MissingBindings;
            }
            if (!AnchoredToTownPage(commission, town) || !AnchoredToTownPage(tactical, town))
            {
                Debug.LogWarning(
                    "Town overview can only place town entries that are anchored to the town page band.");
                return UpdateResult.Invalid;
            }

            Transform existing = town.Find(RootName);
            if (existing != null && existing.GetComponent<TownOverviewPanel>() == null)
                return UpdateResult.AlreadyExists;
            return Build(scene, town, controller, returnButton, existing, commission, tactical, save, fault);
        }

        private static UpdateResult Build(Scene scene, Transform town, WorldMapController controller,
            Button returnButton, Transform existing, Transform commissionEntry, Transform tacticalEntry,
            bool save, Action fault)
        {
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(UndoName);
            // Undo is the primary rollback path and restores everything this run recorded,
            // including the child layout of a regenerated page. The fallback below covers the
            // contexts where the Undo history is unavailable: every rect in an existing page
            // subtree (regeneration rewrites each descendant's scale), the commission and
            // tactical entries, the return control, and the controller's panel reference.
            var returnRect = new RectSnapshot((RectTransform)returnButton.transform);
            var commissionRect = new RectSnapshot((RectTransform)commissionEntry);
            var tacticalRect = new RectSnapshot((RectTransform)tacticalEntry);
            // The page itself is restretched and every descendant scale is normalized, so an
            // existing page keeps a snapshot of its whole subtree.
            RectSnapshot[] pageSnapshots = existing != null
                ? existing.GetComponentsInChildren<RectTransform>(true)
                    .Select(rect => new RectSnapshot(rect)).ToArray()
                : new RectSnapshot[0];
            TownOverviewPanel previousPanel =
                new SerializedObject(controller).FindProperty("townOverviewPanel")
                    ?.objectReferenceValue as TownOverviewPanel;
            int returnSibling = returnButton.transform.GetSiblingIndex();
            int rootSibling = existing != null ? existing.GetSiblingIndex() : -1;
            Text returnLabel = returnButton.GetComponentInChildren<Text>(true);
            string returnLabelText = returnLabel != null ? returnLabel.text : null;
            GameObject root = null;
            try
            {
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) throw new InvalidOperationException("Built-in UI font is unavailable.");
                root = existing != null
                    ? existing.gameObject
                    : Create(scene, town, RootName, typeof(Image), typeof(TownOverviewPanel));
                TownOverviewPanel panel = root.GetComponent<TownOverviewPanel>();
                if (panel == null) throw new InvalidOperationException("Town overview root has no panel.");
                Undo.RegisterFullObjectHierarchyUndo(root, UndoName);
                Undo.RecordObject((RectTransform)commissionEntry, UndoName);
                Undo.RecordObject((RectTransform)tacticalEntry, UndoName);
                // The return control keeps its own RectTransform under the town root, so its
                // move, its label, and the sibling order it is raised to are recorded here.
                // The town root itself is recorded because both the page and the return
                // control are re-ordered among its children.
                Undo.RecordObject((RectTransform)returnButton.transform, UndoName);
                Undo.RecordObject(root.transform.parent, UndoName);
                if (returnLabel != null) Undo.RecordObject(returnLabel, UndoName);

                Image background = EnsureComponent<Image>(root);
                Undo.RecordObject(background, UndoName);
                background.color = PageBackground;
                background.raycastTarget = true;
                SengokuUiArt.ApplyParchmentSurface(background);

                Image art = EnsureImage(scene, root.transform, "TownArt");
                Text title = EnsureText(scene, root.transform, "TownName", font, 34, TextAnchor.MiddleLeft);
                Text feature = EnsureText(scene, root.transform, "TownFeature", font, 20, TextAnchor.MiddleLeft);
                Text residents = EnsureText(scene, root.transform, "Residents", font, 20, TextAnchor.UpperLeft);
                Text activity = EnsureActivity(scene, root.transform, font);
                RectTransform activityButtons = EnsureUiObject(scene, root.transform,
                    ActivityButtonsName, typeof(RectTransform)).GetComponent<RectTransform>();
                Button activityTemplate = EnsureButton(scene, activityButtons, ActivityTemplateName,
                    string.Empty, font);
                PlaceActivityTemplate((RectTransform)activityTemplate.transform);
                activityTemplate.gameObject.SetActive(false);
                Button roster = EnsureButton(scene, root.transform, "RosterButton", "当地人物", font);
                Button mission = EnsureButton(scene, root.transform, "MissionButton", "查看全部委托", font);

                ApplyLayout(root, art, title, feature, residents, activity, activityButtons,
                    roster, mission, returnButton, commissionEntry, tacticalEntry);

                panel.BindReferences(art, title, feature, residents, activity, roster, mission,
                    returnButton, activityButtons, activityTemplate);
                Undo.RecordObject(controller, UndoName);
                controller.BindTownOverviewPanel(panel);
                EditorUtility.SetDirty(controller);
                EditorUtility.SetDirty(panel);
                // The seam sits after both bindings and before validation, so a test that fails
                // here proves the rollback also clears the controller reference it just set.
                fault?.Invoke();
                if (!Validate(scene, out string error)) throw new InvalidOperationException(error);
                EditorSceneManager.MarkSceneDirty(scene);
                if (save && !EditorSceneManager.SaveScene(scene))
                    throw new InvalidOperationException("Unity refused to save FoundationScene.");
                Undo.CollapseUndoOperations(group);
                return UpdateResult.Success;
            }
            catch (Exception exception)
            {
                // Runtime cards are instantiated outside the Undo history. Release them before
                // the revert so it cannot report dangling children, then release again after it
                // because the page's hierarchy snapshot can hand back the cards that already
                // existed when the run started.
                TownOverviewPanel rolledBackPanel =
                    root != null ? root.GetComponent<TownOverviewPanel>() : null;
                if (rolledBackPanel != null)
                    rolledBackPanel.ReleaseRuntimeActivityCards();
                if (existing == null && root != null)
                    UnityEngine.Object.DestroyImmediate(root);
                Undo.FlushUndoRecordObjects();
                Undo.RevertAllDownToGroup(group);
                commissionRect.Restore();
                tacticalRect.Restore();
                returnRect.Restore();
                foreach (RectSnapshot snapshot in pageSnapshots)
                    snapshot.Restore();
                if (rolledBackPanel != null)
                    rolledBackPanel.ReleaseRuntimeActivityCards();
                RestoreReference(controller, "townOverviewPanel", previousPanel);
                if (returnLabel != null && returnLabelText != null) returnLabel.text = returnLabelText;
                returnButton.transform.SetSiblingIndex(returnSibling);
                if (root != null && existing != null)
                    root.transform.SetSiblingIndex(rootSibling);
                Debug.LogWarning($"Town overview update rolled back: {exception.Message}");
                return UpdateResult.Failed;
            }
        }

        private static void ApplyLayout(GameObject root, Image art, Text title, Text feature,
            Text residents, Text activity, RectTransform activityButtons, Button roster, Button mission, Button returnButton,
            Transform commissionEntry, Transform tacticalEntry)
        {
            var rootRect = (RectTransform)root.transform;
            Stretch(rootRect, 0f, 0f, 0f, 0f);
            // Parenting through Undo keeps the world scale, which would shrink the page under
            // a scaled town band. Reset the local scale so the page inherits the band's UI scale.
            rootRect.localScale = Vector3.one;

            PlaceTopLeft((RectTransform)title.transform, PageInset, TitleTop, TitleSize);
            PlaceTopLeft((RectTransform)feature.transform, PageInset, FeatureTop, FeatureSize);
            PlaceBottomLeft((RectTransform)art.transform, PageInset, ArtBottom, ArtSize);
            art.preserveAspect = true;
            art.transform.SetAsFirstSibling();
            PlaceBottomLeft((RectTransform)residents.transform, PageInset, ResidentsBottom, ResidentsSize);
            PlaceTopRight((RectTransform)activity.transform, -PageInset, ActivityTop, ActivitySize);
            PlaceTopRight(activityButtons, -PageInset, ActivityButtonsTop, ActivityButtonsSize);
            PlaceTopRight((RectTransform)mission.transform, -PageInset, ColumnActionTop, ColumnActionSize);
            PlaceTopRight((RectTransform)commissionEntry, -PageInset, ColumnActionTop - ColumnActionStep,
                ColumnActionSize);
            PlaceTopRight((RectTransform)tacticalEntry, -PageInset, ColumnActionTop - 2f * ColumnActionStep,
                ColumnActionSize);
            PlaceBottomLeft((RectTransform)roster.transform, PageInset, FooterBottom, FooterSize);
            PlaceBottomRight((RectTransform)returnButton.transform, -PageInset, FooterBottom, FooterSize);

            StyleAction(roster, 20);
            StyleAction(mission, 16);
            StyleAction(activityButtons.Find(ActivityTemplateName).GetComponent<Button>(), 16);
            StyleAction(returnButton, 20);
            StyleAction(commissionEntry.GetComponent<Button>(), 16);
            StyleAction(tacticalEntry.GetComponent<Button>(), 16);
            Text returnLabel = returnButton.GetComponentInChildren<Text>(true);
            if (returnLabel != null) returnLabel.text = "返回地图";

            // A saved page can carry a stale scale on any descendant: the last scene
            // integration saved the activity root at 0.5 while its siblings stayed at 1.
            // Normalize the whole subtree so every element renders at the band's UI scale and
            // a later scene save stores unit scales instead of the half-size copy.
            foreach (RectTransform child in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (child.localScale == Vector3.one) continue;
                Undo.RecordObject(child, UndoName);
                child.localScale = Vector3.one;
            }

            // The page background is almost opaque, so it must render behind the two town
            // entry roots that own the commission and tactical buttons. Moving to the lower
            // of the two indexes keeps a second run stable instead of hopping past an entry.
            int entryIndex = Mathf.Min(
                TownLevelRoot(commissionEntry, root.transform.parent).GetSiblingIndex(),
                TownLevelRoot(tacticalEntry, root.transform.parent).GetSiblingIndex());
            if (root.transform.GetSiblingIndex() > entryIndex)
                root.transform.SetSiblingIndex(entryIndex);
            returnButton.transform.SetAsLastSibling();
        }

        private static void PlaceTopLeft(RectTransform rect, float x, float top, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, top);
            rect.sizeDelta = size;
        }

        private static void PlaceTopRight(RectTransform rect, float x, float top, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(x, top);
            rect.sizeDelta = size;
        }

        private static void PlaceBottomLeft(RectTransform rect, float x, float bottom, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(x, bottom);
            rect.sizeDelta = size;
        }

        private static void PlaceBottomRight(RectTransform rect, float x, float bottom, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(x, bottom);
            rect.sizeDelta = size;
        }

        private static void PlaceActivityTemplate(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, TownOverviewPanel.ActivityCardHeight);
        }

        private static void StyleAction(Button button, int fontSize)
        {
            if (button == null) return;
            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null) return;
            Undo.RecordObject(label, UndoName);
            label.fontSize = fontSize;
            label.color = ActionLabel;
            label.raycastTarget = false;
        }

        private static bool LayoutIsClear(Transform overview, Transform commissionEntry,
            Transform tacticalEntry, out string error)
        {
            error = null;
            foreach (RectTransform child in ((RectTransform)overview).GetComponentsInChildren<RectTransform>(true))
            {
                if (child.localScale == Vector3.one) continue;
                error = $"'{child.name}' must use unit local scale, but the town page stores " +
                        $"{child.localScale.ToString("0.###")}.";
                return false;
            }
            var root = (RectTransform)overview;
            if (root.anchorMin != Vector2.zero || root.anchorMax != Vector2.one ||
                root.offsetMin != Vector2.zero || root.offsetMax != Vector2.zero)
            {
                error = "The town overview root must fill the town page band.";
                return false;
            }
            // The band is the rect the scene gives the town page; the live size is authoritative
            // because ReferenceGalleryNavigator restyles the same band at runtime.
            Vector2 bandSize = root.rect.size;
            if (!BandCanHoldPage(bandSize, out error)) return false;
            if (!TryRectInBand((RectTransform)overview.Find("TownArt"), bandSize, out Rect art, out error) ||
                !TryRectInBand((RectTransform)overview.Find("TownName"), bandSize, out Rect title, out error) ||
                !TryRectInBand((RectTransform)overview.Find("TownFeature"), bandSize, out Rect feature, out error) ||
                !TryRectInBand((RectTransform)overview.Find("Residents"), bandSize, out Rect residents, out error) ||
                !TryRectInBand((RectTransform)overview.Find("RosterButton"), bandSize, out Rect roster, out error) ||
                !TryRectInBand((RectTransform)overview.Find(ActivityName), bandSize, out Rect activity, out error) ||
                !TryRectInBand((RectTransform)overview.Find(ActivityButtonsName), bandSize, out Rect activityButtons, out error) ||
                !TryRectInBand((RectTransform)overview.Find("MissionButton"), bandSize, out Rect mission, out error) ||
                !TryRectInBand((RectTransform)commissionEntry, bandSize, out Rect commission, out error) ||
                !TryRectInBand((RectTransform)tacticalEntry, bandSize, out Rect tactical, out error))
                return false;

            var band = new Rect(Vector2.zero, bandSize);
            if (!Inside(art, band) || !Inside(title, band) || !Inside(feature, band) ||
                !Inside(residents, band) || !Inside(roster, band) ||
                !Inside(activity, band) || !Inside(activityButtons, band) ||
                !Inside(mission, band) || !Inside(commission, band) ||
                !Inside(tactical, band))
            {
                error = "A town page element extends outside the town page band.";
                return false;
            }
            if (art.xMax > activity.xMin)
            {
                error = "The town illustration must not reach the local-action column.";
                return false;
            }
            if (art.yMax > feature.yMin)
            {
                error = "The town illustration must stay clear of the town name and feature lines.";
                return false;
            }
            if (residents.yMax > art.yMin)
            {
                error = "The resident list must not cover the town illustration.";
                return false;
            }
            if (roster.yMax > residents.yMin)
            {
                error = "The roster action must not cover the resident list.";
                return false;
            }
            if (activity.yMin < activityButtons.yMax || activityButtons.yMin < mission.yMax ||
                mission.yMin < commission.yMax ||
                commission.yMin < tactical.yMax)
            {
                error = "The local-action summary and its entries must not overlap.";
                return false;
            }
            Text activityLabel = overview.Find(ActivityName).GetComponent<Text>();
            if (activityLabel == null || activityLabel.fontSize < 17)
            {
                error = "The local-action summary needs a text size of at least 17.";
                return false;
            }
            RectTransform buttonsRoot = (RectTransform)overview.Find(ActivityButtonsName);
            if (activityButtons.height + 0.5f < TownOverviewPanel.ActivityAreaHeight)
            {
                error = $"The local activity area must fit {TownLocalActions.KindCount} activity cards.";
                return false;
            }
            Button[] buttons = buttonsRoot.GetComponentsInChildren<Button>(true);
            foreach (Button button in buttons)
            {
                RectTransform rect = button.transform as RectTransform;
                if (rect == null || !RectFits(rect, buttonsRoot))
                {
                    error = $"Town activity button '{button.name}' extends outside its activity area.";
                    return false;
                }
            }

            Button template = overview.Find(ActivityButtonsName).Find(ActivityTemplateName)
                .GetComponent<Button>();
            return ActionIsReadable(overview.Find("MissionButton"), out error) &&
                   ActionIsReadable(template.transform, out error) &&
                   ActionIsReadable(commissionEntry, out error) &&
                   ActionIsReadable(tacticalEntry, out error);
        }

        private static bool ActionIsReadable(Transform action, out string error)
        {
            error = null;
            Text label = action != null ? action.GetComponentInChildren<Text>(true) : null;
            if (label != null && label.fontSize >= 15) return true;
            error = $"'{action?.name}' needs a label of at least 15px.";
            return false;
        }

        private static bool TryRectInBand(RectTransform rect, Vector2 bandSize, out Rect result,
            out string error)
        {
            result = default;
            error = null;
            if (rect.anchorMin != rect.anchorMax)
            {
                error = $"'{rect.name}' must use a fixed anchor so the layout holds at every resolution.";
                return false;
            }
            Vector2 anchor = new Vector2(rect.anchorMin.x * bandSize.x, rect.anchorMin.y * bandSize.y);
            Vector2 size = rect.sizeDelta;
            Vector2 min = anchor + rect.anchoredPosition -
                          new Vector2(rect.pivot.x * size.x, rect.pivot.y * size.y);
            result = new Rect(min, size);
            return true;
        }

        /// <summary>
        /// The live band has to be able to hold the page's own metrics: the illustration's top
        /// edge must clear the feature line, and the activity column must clear the illustration.
        /// </summary>
        private static bool BandCanHoldPage(Vector2 band, out string error)
        {
            error = null;
            float requiredHeight = ArtBottom + ArtSize.y + Mathf.Abs(FeatureTop) + FeatureSize.y;
            float requiredWidth = PageInset + ArtSize.x + PageInset + ColumnWidth + PageInset;
            if (band.x + 0.5f >= requiredWidth && band.y + 0.5f >= requiredHeight) return true;
            error = $"The town page band is {band.x:0}x{band.y:0}, but the page needs at least " +
                    $"{requiredWidth:0}x{requiredHeight:0} for its illustration, feature line, and " +
                    "activity column.";
            return false;
        }

        private static bool Inside(Rect rect, Rect band)
        {
            const float tolerance = 0.5f;
            return rect.xMin >= band.xMin - tolerance && rect.yMin >= band.yMin - tolerance &&
                   rect.xMax <= band.xMax + tolerance && rect.yMax <= band.yMax + tolerance;
        }

        private static bool RectFits(RectTransform rect, RectTransform parent)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
            {
                Vector3 point = parent.InverseTransformPoint(corner);
                const float tolerance = 0.5f;
                if (point.x < parent.rect.xMin - tolerance || point.x > parent.rect.xMax + tolerance ||
                    point.y < parent.rect.yMin - tolerance || point.y > parent.rect.yMax + tolerance)
                    return false;
            }
            return true;
        }

        private static bool PanelIsBound(TownOverviewPanel panel)
        {
            var serialized = new SerializedObject(panel);
            string[] fields =
            {
                "townIllustration", "townNameText", "townFeatureText", "residentText",
                "localActionsText", "activityButtonsRoot", "activityButtonTemplate",
                "rosterButton", "missionButton", "returnToMapButton"
            };
            foreach (string field in fields)
            {
                SerializedProperty property = serialized.FindProperty(field);
                if (property == null || property.objectReferenceValue == null) return false;
            }
            return true;
        }

        private static Transform FindTownEntry(Transform town, string name) =>
            town.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);

        /// <summary>The town child that owns an entry button: the entry itself or its root.</summary>
        private static Transform TownLevelRoot(Transform entry, Transform town) =>
            entry.parent == town ? entry : entry.parent;

        private static bool AnchoredToTownPage(Transform entry, Transform town)
        {
            RectTransform parent = entry.parent as RectTransform;
            if (parent == null) return false;
            if (parent == town) return true;
            return parent.parent == town &&
                   parent.anchorMin == Vector2.zero && parent.anchorMax == Vector2.one &&
                   parent.offsetMin == Vector2.zero && parent.offsetMax == Vector2.zero;
        }

        private static GameObject Create(Scene scene, Transform parent, string name, params Type[] components)
        {
            var obj = new GameObject(name, components);
            SceneManager.MoveGameObjectToScene(obj, scene);
            Undo.RegisterCreatedObjectUndo(obj, UndoName);
            Undo.SetTransformParent(obj.transform, parent, UndoName);
            return obj;
        }

        private static GameObject EnsureUiObject(Scene scene, Transform parent, string name,
            params Type[] components)
        {
            Transform existing = parent.Find(name);
            if (existing == null) return Create(scene, parent, name, components);
            Undo.RegisterFullObjectHierarchyUndo(existing.gameObject, UndoName);
            foreach (Type type in components)
            {
                if (existing.GetComponent(type) == null)
                    Undo.AddComponent(existing.gameObject, type);
            }
            return existing.gameObject;
        }

        private static T EnsureComponent<T>(GameObject owner) where T : Component
        {
            T component = owner.GetComponent<T>();
            return component != null ? component : Undo.AddComponent<T>(owner);
        }

        private static Image EnsureImage(Scene scene, Transform parent, string name)
        {
            GameObject obj = EnsureUiObject(scene, parent, name, typeof(Image));
            Image image = EnsureComponent<Image>(obj);
            Undo.RecordObject(image, UndoName);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static Text EnsureText(Scene scene, Transform parent, string name, Font font,
            int size, TextAnchor alignment)
        {
            GameObject obj = EnsureUiObject(scene, parent, name, typeof(Text));
            Text text = EnsureComponent<Text>(obj);
            Undo.RecordObject(text, UndoName);
            text.font = font;
            text.fontSize = size;
            text.color = Ink;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Text EnsureActivity(Scene scene, Transform root, Font font)
        {
            if (root.Find(ActivityName) == null)
            {
                Transform legacy = root.Find(LegacyActivityName);
                if (legacy != null)
                {
                    Undo.RegisterCompleteObjectUndo(legacy.gameObject, UndoName);
                    legacy.gameObject.name = ActivityName;
                }
            }
            return EnsureText(scene, root, ActivityName, font, 17, TextAnchor.UpperLeft);
        }

        private static Button EnsureButton(Scene scene, Transform parent, string name, string label, Font font)
        {
            GameObject obj = EnsureUiObject(scene, parent, name, typeof(Image), typeof(Button));
            Image image = EnsureComponent<Image>(obj);
            Undo.RecordObject(image, UndoName);
            image.color = ActionBackground;
            image.raycastTarget = true;
            Button button = EnsureComponent<Button>(obj);
            Undo.RecordObject(button, UndoName);
            button.targetGraphic = image;
            Text text = EnsureText(scene, obj.transform, "Label", font, 20, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = ActionLabel;
            return button;
        }

        private static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }

        private static bool Bound(UnityEngine.Object owner, string fieldName, UnityEngine.Object expected)
        {
            var property = new SerializedObject(owner).FindProperty(fieldName);
            return property != null && property.objectReferenceValue == expected;
        }

        /// <summary>Fallback for the controller reference when Undo history cannot restore it.</summary>
        private static void RestoreReference(UnityEngine.Object owner, string fieldName,
            UnityEngine.Object value)
        {
            var serialized = new SerializedObject(owner);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null || property.objectReferenceValue == value) return;
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Rect state captured before layout so a refused run can restore it.</summary>
        private readonly struct RectSnapshot
        {
            private readonly RectTransform _rect;
            private readonly Vector2 _anchorMin;
            private readonly Vector2 _anchorMax;
            private readonly Vector2 _pivot;
            private readonly Vector2 _anchoredPosition;
            private readonly Vector2 _sizeDelta;
            private readonly Vector3 _localScale;

            public RectSnapshot(RectTransform rect)
            {
                _rect = rect;
                _anchorMin = rect.anchorMin;
                _anchorMax = rect.anchorMax;
                _pivot = rect.pivot;
                _anchoredPosition = rect.anchoredPosition;
                _sizeDelta = rect.sizeDelta;
                _localScale = rect.localScale;
            }

            public void Restore()
            {
                if (_rect == null) return;
                _rect.anchorMin = _anchorMin;
                _rect.anchorMax = _anchorMax;
                _rect.pivot = _pivot;
                _rect.anchoredPosition = _anchoredPosition;
                _rect.sizeDelta = _sizeDelta;
                _rect.localScale = _localScale;
            }
        }
    }
}
