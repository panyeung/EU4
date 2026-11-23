using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Shows current-town identity, the local work that can be done here, and the entries
    /// into the existing roster and mission pages. Every label comes from session state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TownOverviewPanel : MonoBehaviour
    {
        [SerializeField] private Image townIllustration;
        [SerializeField] private Image townArtVeil;
        [SerializeField] private Text townNameText;
        [SerializeField] private Text townFeatureText;
        [SerializeField] private Text residentText;
        [FormerlySerializedAs("rumorText")]
        [SerializeField] private Text localActionsText;
        [SerializeField] private RectTransform activityButtonsRoot;
        [SerializeField] private Button activityButtonTemplate;
        [SerializeField] private Button rosterButton;
        [SerializeField] private Button missionButton;
        [SerializeField] private Button returnToMapButton;

        // Runtime clones stay under the activity root, which owns their lifetime: unloading
        // the scene or destroying the page takes the cards with it. DontSave only keeps them
        // out of the serialized scene, and deliberately leaves them unloadable so the scene
        // itself can reclaim them. Hide flags are per object, so every cloned child carries
        // them too; a serialized label under a runtime card would be worse than a serialized card.
        private const HideFlags ActivityButtonFlags =
            HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        /// <summary>Card height, shared with the editor updater's fixed activity area.</summary>
        public const float ActivityCardHeight = 36f;

        /// <summary>Distance between two stacked cards, shared with the editor updater.</summary>
        public const float ActivityCardStep = 40f;

        /// <summary>
        /// Opacity of the washi wash between the full-page town illustration and the ink
        /// labels. Kept here so the scene updater and runtime ordering share one value.
        /// </summary>
        public const float TownArtVeilAlpha = 0.55f;

        /// <summary>Warm paper tint used by the washi wash over the town illustration.</summary>
        public static readonly Color TownArtVeilColor = new Color(0.96f, 0.92f, 0.83f, TownArtVeilAlpha);

        /// <summary>
        /// Height of the area that holds one card per activity kind. The town page sizes its
        /// activity root from this value, so the runtime step and the updater cannot drift
        /// apart and let a card run into the mission entry below.
        /// </summary>
        public static float ActivityAreaHeight =>
            ActivityCardStep * (TownLocalActions.KindCount - 1) + ActivityCardHeight;

        private WorldMapController _controller;
        // Cards are reused across refreshes so the page never accumulates children. This list
        // holds no lifetime claim; the activity root is the owner.
        private readonly List<Button> _activityButtons = new List<Button>();
        private static readonly TownLocalActions.Entry[] NoActivities = new TownLocalActions.Entry[0];

        public void BindReferences(Image illustration, Text townName, Text feature,
            Text residents, Text localActions, Button roster, Button mission, Button returnToMap,
            RectTransform actionsRoot = null, Button actionTemplate = null, Image veil = null)
        {
            BindListeners(false);
            townIllustration = illustration;
            if (veil != null) townArtVeil = veil;
            townNameText = townName;
            townFeatureText = feature;
            residentText = residents;
            localActionsText = localActions;
            activityButtonsRoot = actionsRoot;
            activityButtonTemplate = actionTemplate;
            rosterButton = roster;
            missionButton = mission;
            returnToMapButton = returnToMap;
            BindListeners(true);
            Refresh();
        }

        public void Bind(WorldMapController controller)
        {
            _controller = controller;
            Refresh();
        }

        public void Refresh()
        {
            ApplyTownArtOrder();
            GameSession session = _controller != null ? _controller.Session : null;
            if (session == null)
            {
                Set(townNameText, "城镇");
                Set(townFeatureText, "当前地点不可用。");
                Set(residentText, "居民\n暂无登记居民");
                Set(localActionsText, ActivitySummary(null, NoActivities));
                RefreshActivityButtons(NoActivities);
                if (townIllustration != null)
                {
                    townIllustration.sprite = null;
                    townIllustration.color = new Color(0.2f, 0.24f, 0.29f, 0.12f);
                }
                if (rosterButton != null) rosterButton.interactable = false;
                if (missionButton != null) missionButton.interactable = false;
                return;
            }

            LocationId location = session.CurrentLocation;
            if (!LocationCapabilities.CanUseTownServices(location))
            {
                Set(townNameText, LocationId.GetChineseName(location));
                Set(townFeatureText, "旅行枢纽：没有城镇活动、居民或当地委托。");
                Set(residentText, "当地人物\n暂无登记居民");
                Set(localActionsText, ActivitySummary(null, NoActivities));
                RefreshActivityButtons(NoActivities);
                if (townIllustration != null)
                {
                    townIllustration.sprite = null;
                    townIllustration.color = new Color(0.2f, 0.24f, 0.29f, 0.12f);
                }
                if (rosterButton != null) rosterButton.interactable = false;
                if (missionButton != null) missionButton.interactable = false;
                if (returnToMapButton != null) returnToMapButton.interactable = _controller.InTownMenu;
                return;
            }

            Set(townNameText, LocationId.GetChineseName(location));
            Set(townFeatureText, FeatureFor(location));
            if (townIllustration != null)
            {
                townIllustration.sprite = SengokuUiArt.TownIllustration(location);
                townIllustration.color = townIllustration.sprite != null
                    ? Color.white : new Color(0.2f, 0.24f, 0.29f, 0.12f);
            }

            var residents = TownNpcAssignments.GetNpcs(location.Value);
            if (residents.Count == 0)
            {
                Set(residentText, "当地人物\n暂无登记居民");
            }
            else
            {
                var names = new StringBuilder("当地人物");
                for (int i = 0; i < residents.Count; i++)
                {
                    string name = CharacterCatalog.TryGet(residents[i], out CharacterDefinition definition)
                        ? definition.DisplayName : residents[i].ToString();
                    names.Append('\n').Append(name);
                }
                Set(residentText, names.ToString());
            }

            IReadOnlyList<TownLocalActions.Entry> actions = TownLocalActions.GetAvailable(session);
            Set(localActionsText, ActivitySummary(session, actions));
            RefreshActivityButtons(actions);
            if (rosterButton != null)
                rosterButton.interactable = _controller.InTownMenu && residents.Count > 0;
            if (missionButton != null)
            {
                missionButton.interactable = _controller.InTownMenu && _controller.HasSupplyShortageMissionPanel;
                Text label = missionButton.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = _controller.IsSupplyShortageMissionOpen ? "返回城镇总览" : "查看全部委托";
            }
            if (returnToMapButton != null)
                returnToMapButton.interactable = _controller.InTownMenu;
        }

        private void OnEnable()
        {
            ApplyTownArtOrder();
            if (returnToMapButton != null)
                returnToMapButton.transform.SetAsLastSibling();
            BindListeners(true);
        }

        /// <summary>
        /// Keeps the illustration below its washi wash and both below every label and control,
        /// including after a scene reload or a mission button reparented to the town page.
        /// </summary>
        private void ApplyTownArtOrder()
        {
            if (townIllustration == null) return;
            if (townArtVeil == null)
                townArtVeil = transform.Find("TownArtVeil")?.GetComponent<Image>();
            townIllustration.transform.SetAsFirstSibling();
            if (townArtVeil == null || townArtVeil.transform.parent != townIllustration.transform.parent)
                return;
            int index = Mathf.Min(1, townArtVeil.transform.parent.childCount - 1);
            townArtVeil.transform.SetSiblingIndex(index);
        }

        private void OnDisable() => BindListeners(false);

        private bool HasButtonArea => activityButtonsRoot != null && activityButtonTemplate != null;

        /// <summary>
        /// The generated page carries one clickable card per entry, so its label is only a
        /// heading. A page without cards has just the compact summary label; filling that
        /// with one line per entry overflows the legacy 80px area.
        /// </summary>
        private string ActivitySummary(GameSession session, IReadOnlyList<TownLocalActions.Entry> actions)
        {
            if (HasButtonArea)
                return actions.Count > 0 ? TownLocalActions.Header : TownLocalActions.Describe(session);
            return TownLocalActions.DescribeCompact(session);
        }

        private void BindListeners(bool add)
        {
            Bind(rosterButton, OpenRoster, add);
            Bind(missionButton, ToggleMission, add);
        }

        private void OpenRoster()
        {
            if (_controller != null) _controller.OpenNpcRoster();
        }

        private void RefreshActivityButtons(IReadOnlyList<TownLocalActions.Entry> actions)
        {
            if (activityButtonsRoot == null || activityButtonTemplate == null)
                return;

            activityButtonTemplate.gameObject.SetActive(false);
            // A card can be destroyed without this component being told, so drop dead
            // references before reusing the cache.
            _activityButtons.RemoveAll(button => button == null);
            for (int i = 0; i < actions.Count; i++)
            {
                while (_activityButtons.Count <= i)
                {
                    Button created = Instantiate(activityButtonTemplate, activityButtonsRoot, false);
                    created.gameObject.name = "TownActivityButton";
                    foreach (Transform part in created.GetComponentsInChildren<Transform>(true))
                        part.gameObject.hideFlags |= ActivityButtonFlags;
                    _activityButtons.Add(created);
                }

                Button button = _activityButtons[i];
                TownLocalActions.Entry entry = actions[i];
                button.gameObject.name = $"TownActivity_{entry.Kind}";
                button.gameObject.SetActive(true);
                RectTransform rect = button.transform as RectTransform;
                if (rect != null)
                {
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 1f);
                    rect.anchoredPosition = new Vector2(0f, -ActivityCardStep * i);
                    rect.sizeDelta = new Vector2(0f, ActivityCardHeight);
                }

                Text label = button.GetComponentInChildren<Text>(true);
                if (label != null)
                    label.text = entry.Label;
                button.onClick.RemoveAllListeners();
                TownLocalActions.Kind kind = entry.Kind;
                button.onClick.AddListener(() =>
                {
                    if (_controller != null)
                        _controller.OpenTownActivity(kind);
                });
                button.transform.SetSiblingIndex(i);
            }

            for (int i = actions.Count; i < _activityButtons.Count; i++)
            {
                _activityButtons[i].onClick.RemoveAllListeners();
                _activityButtons[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Drops the runtime activity cards this panel created. They are instantiated outside
        /// the scene's Undo history, so the editor updater calls this when it refuses an update
        /// and the underlying page has to survive the rollback without orphaned cards.
        /// </summary>
        public void ReleaseRuntimeActivityCards()
        {
            for (int i = 0; i < _activityButtons.Count; i++)
            {
                Button button = _activityButtons[i];
                if (button == null) continue;
                button.onClick.RemoveAllListeners();
                DestroyNow(button.gameObject);
            }
            _activityButtons.Clear();
            if (activityButtonsRoot == null) return;
            // A refused regeneration can hand cards back through the page's Undo snapshot after
            // the cache is cleared, so also drop every card that still carries the runtime-only
            // exclusion flags. The serialized template has no flags and is kept.
            const HideFlags runtimeFlags =
                HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            foreach (Transform part in activityButtonsRoot.GetComponentsInChildren<Transform>(true))
            {
                if (part == null || part == activityButtonsRoot) continue;
                if ((part.gameObject.hideFlags & runtimeFlags) != runtimeFlags) continue;
                DestroyNow(part.gameObject);
            }
        }

        private static void DestroyNow(GameObject target)
        {
            if (UnityEngine.Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void ToggleMission()
        {
            if (_controller == null) return;
            _controller.ToggleSupplyShortageMissionPanel();
            if (missionButton == null || transform.parent == null) return;

            bool missionOpen = _controller.IsSupplyShortageMissionOpen;
            missionButton.transform.SetParent(missionOpen ? transform.parent : transform, true);
            if (missionOpen)
            {
                missionButton.transform.SetAsLastSibling();
                if (returnToMapButton != null)
                    returnToMapButton.transform.SetAsLastSibling();
            }
        }

        private static string FeatureFor(LocationId location)
        {
            if (location == LocationId.Kiyosu) return "尾张旧都 · 城郭与城门之间的街町";
            if (location == LocationId.Atsuta) return "热田神宫 · 参道连接港湾水岸";
            if (location == LocationId.Nagoya) return "那古野 · 沿路市集与丘陵边缘";
            return "尚无此地的城镇资料。";
        }

        private static void Set(Text target, string value)
        {
            if (target != null) target.text = value;
        }

        private static void Bind(Button button, UnityAction action, bool add)
        {
            if (button == null) return;
            button.onClick.RemoveListener(action);
            if (add) button.onClick.AddListener(action);
        }
    }
}
