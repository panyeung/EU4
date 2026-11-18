// Unity 6.6 deprecates the DEVELOPMENT_BUILD directive (UAC0009). The approved
// Debug Mode plan requires a compile-time boundary that exists only in the Editor
// and development builds, so the directive stays and its deprecation notice is
// silenced for this file only.
#pragma warning disable UAC0009
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SengokuChronicle.Presentation
{
    /// <summary>Stable tabs of the Debug Mode window.</summary>
    public enum DebugModeTab
    {
        Session = 0,
        People = 1,
        Date = 2,
        Resources = 3,
        Events = 4
    }

    /// <summary>
    /// Five-tab development-only test window. It never edits domain collections itself:
    /// every action is submitted to the isolated <see cref="DebugSession"/>, and this
    /// component only forwards input and displays the returned result.
    /// <para>
    /// The whole component is compiled only for the Editor and development builds, so a
    /// release player has no window, no entry point and no command surface.
    /// </para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DebugModePanel : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public const string RootName = "M09DebugModeRoot";
        public const string Watermark = "DEBUG / 不会保存至玩家存档";
        public const int TabCount = 5;

        // Modal overlays that must keep rendering above the debug window. Everything else
        // at Canvas level is an ordinary page root and must render below it.
        private const string EventOverlayName = "EventChoiceRoot";
        private const string TacticalOverlayName = "M6TacticalMissionOverlay";

        [Header("Dependencies")]
        [SerializeField] private GameSession gameSession;

        [Header("Window")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button foldButton;
        [SerializeField] private RectTransform dragHandle;

        [Header("Header")]
        [SerializeField] private Text watermarkText;
        [SerializeField] private Text messageText;

        [Header("Tabs")]
        [SerializeField] private Button[] tabButtons = new Button[TabCount];
        [SerializeField] private GameObject[] tabContents = new GameObject[TabCount];

        [Header("Session tab")]
        [SerializeField] private Text sessionSummaryText;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button exitButton;

        [Header("People tab")]
        [SerializeField] private RectTransform rosterRoot;
        [SerializeField] private Text selectedPersonText;
        [SerializeField] private InputField nameField;
        [SerializeField] private InputField birthYearField;
        [SerializeField] private InputField kinshipFromField;
        [SerializeField] private InputField kinshipToField;
        [SerializeField] private Text kinshipPreviewText;
        [SerializeField] private Text personResultText;
        [SerializeField] private Button maleButton;
        [SerializeField] private Button femaleButton;
        [SerializeField] private Button unspecifiedButton;
        [SerializeField] private Button aliveButton;
        [SerializeField] private Button memberButton;
        [SerializeField] private Button roleButton;
        [SerializeField] private Button applyEditButton;
        [SerializeField] private Button transferHeadButton;
        [SerializeField] private Button removePersonButton;
        [SerializeField] private Button createPersonButton;
        [SerializeField] private Button presetButton;
        [SerializeField] private Button kinshipTypeButton;
        [SerializeField] private Button kinshipPreviewButton;
        [SerializeField] private Button kinshipApplyButton;
        [SerializeField] private Button kinshipRemoveButton;
        [SerializeField] private Button setAsTargetButton;

        [Header("Date tab")]
        [SerializeField] private Text dateSummaryText;
        [SerializeField] private InputField targetYearField;
        [SerializeField] private InputField targetMonthField;
        [SerializeField] private InputField targetDayField;
        [SerializeField] private Text datePreviewText;
        [SerializeField] private Text dateResultText;
        [SerializeField] private Button previewDateButton;
        [SerializeField] private Button confirmDateButton;
        [SerializeField] private Button advanceMonthButton;

        [Header("Resources tab")]
        [SerializeField] private Text resourceSummaryText;
        [SerializeField] private InputField fundsField;
        [SerializeField] private InputField suppliesField;
        [SerializeField] private InputField intelligenceField;
        [SerializeField] private InputField foodField;
        [SerializeField] private InputField foodPriceField;
        [SerializeField] private InputField materialsPriceField;
        [SerializeField] private Text resourceResultText;
        [SerializeField] private Button applyResourcesButton;

        [Header("Events tab")]
        [SerializeField] private Text eventSummaryText;
        [SerializeField] private RectTransform eventListRoot;
        [SerializeField] private InputField eventIdField;
        [SerializeField] private Text eventResultText;
        [SerializeField] private Button injectEventButton;

        private readonly List<Button> _rosterButtons = new List<Button>();
        private readonly List<Text> _rosterLabels = new List<Text>();
        private readonly List<Button> _eventButtons = new List<Button>();
        private readonly List<Text> _eventLabels = new List<Text>();
        private DebugSession _debugSession;
        private PersonId _selectedPersonId;
        private PersonId _kinshipFrom;
        private PersonId _kinshipTo;
        private KinshipType _kinshipType = KinshipType.Spouse;
        private bool _kinshipAdd = true;
        private string _previewedDateKey = string.Empty;
        private string _previewedKinshipKey = string.Empty;
        private string _previewedEventId = string.Empty;
        private PersonGender _editGender = PersonGender.Unspecified;
        private bool _editAlive = true;
        private bool _editMember = true;
        private FamilyMemberRole _editRole = FamilyMemberRole.DirectKin;
        private Vector2 _dragOffset;
        private bool _built;
        private bool _subscribed;
        private bool _openListenerBound;
        private bool _fitSubscribed;

        public DebugModeTab ActiveTab { get; private set; } = DebugModeTab.Session;

        /// <summary>
        /// True only while the window is actually visible: a window left under a hidden
        /// title overlay is not reported as open.
        /// </summary>
        public bool IsOpen => panelRoot != null && panelRoot.activeInHierarchy;

        /// <summary>True when the window's own active flag is set, regardless of its host.</summary>
        public bool IsRequestedOpen => panelRoot != null && panelRoot.activeSelf;

        /// <summary>Number of listeners this panel currently holds on the entry button.</summary>
        public int OpenListenerBindingCount { get; private set; }

        /// <summary>Raised after a reset or exit discards the session, with the readable reason.</summary>
        public event Action<string> DebugSessionEnded;

        /// <summary>
        /// Raised whenever the window opens or closes, so the title page can keep the dev
        /// entry hidden while the window covers the screen and visible when it is closed.
        /// </summary>
        public event Action WindowVisibilityChanged;

        public bool IsFolded { get; private set; }
        public bool IsBound => gameSession != null && panelRoot != null && watermarkText != null;
        public PersonId SelectedPersonId => _selectedPersonId;
        public DebugSession Session => _debugSession;
        public string MessageLine => messageText != null ? messageText.text : string.Empty;
        public string DateResultLine => dateResultText != null ? dateResultText.text : string.Empty;
        public string KinshipPreviewLine =>
            kinshipPreviewText != null ? kinshipPreviewText.text : string.Empty;
        public string PersonResultLine =>
            personResultText != null ? personResultText.text : string.Empty;
        public string ResourceResultLine =>
            resourceResultText != null ? resourceResultText.text : string.Empty;
        public string EventResultLine =>
            eventResultText != null ? eventResultText.text : string.Empty;
        public string WatermarkLine => watermarkText != null ? watermarkText.text : string.Empty;
        public int RosterRowCount => _rosterButtons.Count;
        public int EventRowCount => _eventButtons.Count;

        /// <summary>
        /// Binds the title-page entry that opens this window. The listener belongs to the
        /// binding, not to the window's visibility, so hiding or closing the window never
        /// disarms the entry that reopens it. A reference already assigned in the Inspector
        /// still needs its first <c>AddListener</c>, so the attach step is stateful rather
        /// than a same-reference early return.
        /// </summary>
        public void BindOpenButton(Button button)
        {
            if (openButton != button)
            {
                DetachOpenListener();
                openButton = button;
            }
            AttachOpenListener();
        }

        private void AttachOpenListener()
        {
            if (openButton == null || _openListenerBound)
                return;
            openButton.onClick.AddListener(Open);
            _openListenerBound = true;
            OpenListenerBindingCount++;
        }

        private void DetachOpenListener()
        {
            if (openButton != null && _openListenerBound)
            {
                openButton.onClick.RemoveListener(Open);
                OpenListenerBindingCount--;
            }
            _openListenerBound = false;
        }

        private void Awake() => EnsureBuilt();

        private void Start()
        {
            EnsureBuilt();
            if (gameSession == null)
                gameSession = GetComponentInParent<GameSession>() ?? FindAnyObjectByType<GameSession>();
            BindOpenButton(openButton);
            if (panelRoot != null && panelRoot.activeSelf)
                Refresh();
        }

        private void OnEnable()
        {
            EnsureBuilt();
            BindOpenButton(openButton);
        }

        private void OnDestroy()
        {
            // The entry button outlives the window: opening it is what re-enables this
            // component, so the listener is only detached when the panel itself goes away.
            Unsubscribe();
            UnsubscribeFit();
            DetachOpenListener();
        }

        // ---------------------------------------------------------------- window lifecycle

        /// <summary>
        /// Opens the window. Entering creates the isolated debug session when the live
        /// session is not already a debug session; nothing is read from the save slot.
        /// </summary>
        public void Open()
        {
            EnsureBuilt();
            if (gameSession == null)
            {
                Report("缺少 GameSession，无法进入 Debug 模式。");
                return;
            }
            if (!gameSession.IsDebugSessionActive)
                gameSession.BeginDebugSession();
            _debugSession = gameSession.CurrentDebugSession;
            EnsureWindowHost();
            if (panelRoot != null)
                panelRoot.SetActive(true);
            FitWindowToHost();
            SubscribeFit();
            Subscribe();
            Refresh();
            WindowVisibilityChanged?.Invoke();
        }

        /// <summary>
        /// Hides the window; the in-memory debug session and its state stay alive, and the
        /// entry affordance stays reachable so the player can reopen the same session.
        /// </summary>
        public void Close()
        {
            bool wasVisible = IsOpen;
            Unsubscribe();
            UnsubscribeFit();
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (wasVisible)
                WindowVisibilityChanged?.Invoke();
        }

        private void Subscribe()
        {
            if (gameSession == null || _subscribed)
                return;
            gameSession.SessionStateChanged += OnSessionStateChanged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (gameSession == null || !_subscribed)
                return;
            gameSession.SessionStateChanged -= OnSessionStateChanged;
            _subscribed = false;
        }

        private void OnSessionStateChanged()
        {
            if (IsOpen)
                Refresh();
        }

        /// <summary>Folds or unfolds the tab body without leaving the debug session.</summary>
        public void ToggleFold()
        {
            IsFolded = !IsFolded;
            ApplyFold();
        }

        private void ApplyFold()
        {
            for (int i = 0; i < tabContents.Length; i++)
            {
                if (tabContents[i] == null)
                    continue;
                if (IsFolded)
                    tabContents[i].SetActive(false);
                else
                    tabContents[i].SetActive(i == (int)ActiveTab);
            }
            for (int i = 0; i < tabButtons.Length; i++)
                if (tabButtons[i] != null)
                    tabButtons[i].gameObject.SetActive(!IsFolded);
        }

        /// <summary>
        /// Leaves the debug session: the temporary session and every injected change are
        /// discarded, the ordinary new/sample flow is restored and the window closes so the
        /// title entry can take the player back to the start page.
        /// </summary>
        public void ExitDebugSession()
        {
            EndSessionAndReturnToStart(
                "已退出 Debug 模式：临时会话已丢弃，未触碰玩家存档。");
        }

        /// <summary>
        /// Resets the debug session to the start page exactly like an exit: per the approved
        /// plan the temporary session and its debug markers are dropped and the ordinary
        /// new/sample paths are restored, so a reset can never leave a debug session behind.
        /// </summary>
        public void ResetDebugSession()
        {
            EndSessionAndReturnToStart(
                "已重置调试会话：临时会话已丢弃，请重新开始或再次进入 Debug 模式。");
        }

        private void EndSessionAndReturnToStart(string message)
        {
            if (gameSession != null)
                gameSession.EndDebugSession();
            _debugSession = null;
            _selectedPersonId = default;
            _previewedDateKey = string.Empty;
            _previewedKinshipKey = string.Empty;
            _previewedEventId = string.Empty;
            Close();
            Report(message);
            DebugSessionEnded?.Invoke(message);
        }

        /// <summary>
        /// Gives the window its one host and stacking rule. A scene saves the root as a
        /// Canvas child just below the title overlay, which is right for the title page but
        /// leaves every page root authored after it (HUD, daily action, NPC inspector)
        /// rendering over the open window. Opening therefore re-hosts the window on the root
        /// Canvas when it is nested, then puts it immediately below the modal overlays so the
        /// forced-event and tactical screens still cover it.
        /// </summary>
        private void EnsureWindowHost()
        {
            if (panelRoot == null)
                return;
            Transform current = panelRoot.transform.parent;
            Canvas canvas = current != null ? current.GetComponentInParent<Canvas>(true) : null;
            if (canvas == null && gameSession != null)
                canvas = gameSession.GetComponentInParent<Canvas>(true);
            if (canvas == null)
                return;
            Transform host = canvas.rootCanvas.transform;
            if (current != host)
            {
                panelRoot.transform.SetParent(host, false);
                if (current != null && !current.gameObject.activeInHierarchy)
                    Debug.LogWarning("M09DebugModeRoot was re-parented to the root Canvas so the " +
                                     "debug window stays usable outside the title overlay.");
            }
            PlaceBelowModalOverlays(host);
        }

        /// <summary>
        /// Moves the window to the slot immediately before the lowest modal overlay, which is
        /// above every ordinary page root. With no modal overlay in the Canvas the window is
        /// simply the last child. Reopening with the order already correct changes nothing.
        /// </summary>
        private void PlaceBelowModalOverlays(Transform host)
        {
            int firstOverlay = host.childCount;
            firstOverlay = OverlayIndex(host, EventOverlayName, firstOverlay);
            firstOverlay = OverlayIndex(host, TacticalOverlayName, firstOverlay);
            if (firstOverlay >= host.childCount)
            {
                panelRoot.transform.SetAsLastSibling();
                return;
            }
            int index = panelRoot.transform.GetSiblingIndex();
            int target = index < firstOverlay ? firstOverlay - 1 : firstOverlay;
            if (index != target)
                panelRoot.transform.SetSiblingIndex(target);
        }

        private static int OverlayIndex(Transform host, string name, int current)
        {
            Transform overlay = host.Find(name);
            return overlay != null ? Mathf.Min(current, overlay.GetSiblingIndex()) : current;
        }

        // ---------------------------------------------------------------- responsive fit

        /// <summary>
        /// Keeps the authored 1200x680 window usable on a smaller canvas. With room for the
        /// design size the window renders at 1:1; below that the whole window scales down
        /// uniformly, so controls, text and hit areas shrink together instead of the bottom
        /// and right edges being clipped. A canvas resize also has to reach an already open
        /// window, so the fit follows the same <see cref="Canvas.willRenderCanvases"/> stage
        /// uGUI uses to rebuild itself.
        /// </summary>
        private void FitWindowToHost()
        {
            if (panelRoot == null)
                return;
            Vector2 room = HostSize();
            if (room.x <= 0f || room.y <= 0f)
                return;
            float scale = Mathf.Min(1f, room.x / WindowSize.x, room.y / WindowSize.y);
            if (scale <= 0f)
                return;
            Vector3 current = panelRoot.transform.localScale;
            if (!Mathf.Approximately(current.x, scale) || !Mathf.Approximately(current.y, scale))
                panelRoot.transform.localScale = new Vector3(scale, scale, 1f);
        }

        /// <summary>Size of the area the window is hosted in, in that host's own units.</summary>
        private Vector2 HostSize()
        {
            var hostRect = panelRoot.transform.parent as RectTransform;
            if (hostRect != null && hostRect.rect.width > 0f && hostRect.rect.height > 0f)
                return hostRect.rect.size;
            Canvas canvas = panelRoot.GetComponentInParent<Canvas>(true);
            if (canvas != null)
            {
                Rect pixel = canvas.rootCanvas.pixelRect;
                if (pixel.width > 0f && pixel.height > 0f)
                    return pixel.size;
            }
            return new Vector2(Screen.width, Screen.height);
        }

        private void SubscribeFit()
        {
            if (_fitSubscribed)
                return;
            Canvas.willRenderCanvases += FitWindowToHost;
            _fitSubscribed = true;
        }

        private void UnsubscribeFit()
        {
            if (!_fitSubscribed)
                return;
            Canvas.willRenderCanvases -= FitWindowToHost;
            _fitSubscribed = false;
        }

        /// <summary>Routes to one tab; exactly one content root stays active.</summary>
        public void SelectTab(DebugModeTab tab)
        {
            ActiveTab = tab;
            IsFolded = false;
            ApplyFold();
            Refresh();
        }

        // ---------------------------------------------------------------- refresh

        /// <summary>Rebuilds every visible projection from the live debug session.</summary>
        public void Refresh()
        {
            EnsureBuilt();
            if (_debugSession == null || !_debugSession.IsActive)
                _debugSession = gameSession != null ? gameSession.CurrentDebugSession : null;
            bool active = _debugSession != null && _debugSession.IsActive;
            if (!active && panelRoot != null && panelRoot.activeSelf)
            {
                // The session ended outside this window (a normal start, load or exit), so
                // hide rather than leaving a stale debug surface over the game.
                Close();
                return;
            }

            if (watermarkText != null)
                watermarkText.text = Watermark;
            if (sessionSummaryText != null)
                sessionSummaryText.text = active ? DescribeSession() : "未进入调试会话。";
            if (dateSummaryText != null)
                dateSummaryText.text = active ? "当前日期：" + _debugSession.Today : "未进入调试会话。";
            if (resourceSummaryText != null)
                resourceSummaryText.text = active ? DescribeResources() : "未进入调试会话。";
            if (eventSummaryText != null)
                eventSummaryText.text = active ? DescribeEvents() : "未进入调试会话。";

            RebuildRoster(active);
            RebuildEvents(active);
            RefreshSelection(active);
            RefreshEditFields(active);
            ApplyFold();
        }

        private string DescribeSession()
        {
            SuccessionState succession = gameSession.Succession;
            string headName = succession.TryGetHead(out PersonState head)
                ? head.DisplayName
                : "（无）";
            return Watermark + "\n日期：" + _debugSession.Today + " · 家族：" +
                   succession.World.Family.DisplayName + " · 家主：" + headName + "\n人口：" +
                   succession.World.PersonCount + " · 家族成员：" +
                   succession.World.Family.MemberCount + " · 亲属关系：" +
                   succession.World.Kinship.Count + " · 继承阶段：" + succession.Phase;
        }

        private string DescribeResources()
        {
            IndustryResources resources = _debugSession.Resources;
            MarketState market = _debugSession.Market;
            return "资金 " + resources.Funds + " · 物资 " + resources.Supplies + " · 情报 " +
                   resources.Intelligence + "\n粮食 " + market.FoodInventory + " · 粮食价格 " +
                   market.FoodPriceIndexPermille + "‰ · 物资价格 " +
                   market.MaterialsPriceIndexPermille + "‰";
        }

        private string DescribeEvents()
        {
            IReadOnlyList<string> pending = _debugSession.PendingEventIds;
            return pending.Count == 0
                ? "当前没有待处理事件。"
                : "待处理事件：" + string.Join("、", pending);
        }

        private void RebuildRoster(bool active)
        {
            if (rosterRoot == null)
                return;
            IReadOnlyList<DebugPersonRow> rows = active
                ? _debugSession.Roster
                : Array.Empty<DebugPersonRow>();
            EnsureRowCapacity(rows.Count, rosterRoot, _rosterButtons, _rosterLabels, 64, "Person");
            for (int i = 0; i < _rosterButtons.Count; i++)
            {
                bool visible = i < rows.Count;
                _rosterButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;
                DebugPersonRow row = rows[i];
                _rosterLabels[i].text = row.Label + " · " + row.PersonId.Value;
                PersonId captured = row.PersonId;
                _rosterButtons[i].onClick.RemoveAllListeners();
                _rosterButtons[i].onClick.AddListener(() => SelectPerson(captured));
            }
        }

        private void RebuildEvents(bool active)
        {
            if (eventListRoot == null)
                return;
            IReadOnlyList<DebugEventRow> rows = active
                ? _debugSession.EventCatalogRows
                : Array.Empty<DebugEventRow>();
            EnsureRowCapacity(rows.Count, eventListRoot, _eventButtons, _eventLabels, 64, "Event");
            for (int i = 0; i < _eventButtons.Count; i++)
            {
                bool visible = i < rows.Count;
                _eventButtons[i].gameObject.SetActive(visible);
                if (!visible)
                    continue;
                DebugEventRow row = rows[i];
                _eventLabels[i].text = row.Label;
                string captured = row.EventId;
                _eventButtons[i].onClick.RemoveAllListeners();
                _eventButtons[i].onClick.AddListener(() =>
                {
                    if (eventIdField != null)
                        eventIdField.text = captured;
                });
            }
        }

        private void RefreshSelection(bool active)
        {
            if (selectedPersonText == null)
                return;
            if (!active || _selectedPersonId.IsEmpty ||
                !_debugSession.TryGetRow(_selectedPersonId, out DebugPersonRow row))
            {
                selectedPersonText.text = active ? "尚未选择人物。" : "未进入调试会话。";
                return;
            }
            selectedPersonText.text = "已选：" + row.DisplayName + "（" + row.PersonId.Value + "）· " +
                                      row.GenderName + " · " + row.BirthYear + " 年生 · " +
                                      (row.Alive ? "在世" : "已故") + " · " +
                                      (row.IsMember ? row.RoleName : "未入谱");
        }

        private void RefreshEditFields(bool active)
        {
            _editGender = PersonGender.Unspecified;
            _editAlive = true;
            _editMember = false;
            if (active && _debugSession.TryGetRow(_selectedPersonId, out DebugPersonRow row))
            {
                _editGender = row.Gender;
                _editAlive = row.Alive;
                _editMember = row.IsMember;
                _editRole = row.IsMember ? row.Role : FamilyMemberRole.DirectKin;
                if (nameField != null)
                    nameField.text = row.DisplayName;
                if (birthYearField != null)
                    birthYearField.text = row.BirthYear.ToString();
            }
            else if (birthYearField != null && string.IsNullOrEmpty(birthYearField.text))
            {
                birthYearField.text = active ? _debugSession.Today.Year.ToString() : string.Empty;
            }

            SetGenderButtons();
        }

        private void SetGenderButtons()
        {
            Highlight(maleButton, _editGender == PersonGender.Male);
            Highlight(femaleButton, _editGender == PersonGender.Female);
            Highlight(unspecifiedButton, _editGender == PersonGender.Unspecified);
            SetLabel(aliveButton, _editAlive ? "存活：是" : "存活：否");
            SetLabel(memberButton, _editMember ? "在谱：是" : "在谱：否");
            SetLabel(roleButton, "身份：" + DebugSession.RoleNameOf(_editRole));
        }

        private static void Highlight(Button button, bool on)
        {
            if (button == null)
                return;
            Image image = button.targetGraphic as Image;
            if (image != null)
                image.color = on ? SengokuUiTheme.GoldDim : SengokuUiTheme.IndigoPanel;
        }

        private static void SetLabel(Button button, string label)
        {
            if (button == null)
                return;
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
                text.text = label;
        }

        private void Report(string message)
        {
            if (messageText != null)
                messageText.text = message ?? string.Empty;
        }

        // ---------------------------------------------------------------- people actions

        public void SelectPerson(PersonId personId)
        {
            _selectedPersonId = personId;
            _kinshipFrom = personId;
            if (kinshipFromField != null)
                kinshipFromField.text = personId.Value ?? string.Empty;
            _previewedKinshipKey = string.Empty;
            Refresh();
        }

        public void ChooseGender(PersonGender gender)
        {
            _editGender = gender;
            SetGenderButtons();
        }

        public void ToggleAlive()
        {
            _editAlive = !_editAlive;
            SetGenderButtons();
        }

        public void ToggleMembership()
        {
            _editMember = !_editMember;
            SetGenderButtons();
        }

        public void CycleRole()
        {
            _editRole = NextRole(_editRole);
            SetGenderButtons();
        }

        private static FamilyMemberRole NextRole(FamilyMemberRole role)
        {
            switch (role)
            {
                case FamilyMemberRole.DirectKin: return FamilyMemberRole.CollateralKin;
                case FamilyMemberRole.CollateralKin: return FamilyMemberRole.AdoptedChild;
                case FamilyMemberRole.AdoptedChild: return FamilyMemberRole.Spouse;
                case FamilyMemberRole.Spouse: return FamilyMemberRole.Retainer;
                case FamilyMemberRole.Retainer: return FamilyMemberRole.Disciple;
                case FamilyMemberRole.Disciple: return FamilyMemberRole.Guest;
                case FamilyMemberRole.Guest: return FamilyMemberRole.DirectKin;
                default: return FamilyMemberRole.DirectKin;
            }
        }

        public void ApplyPersonEdit()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportPerson("未进入调试会话。");
                return;
            }
            if (_selectedPersonId.IsEmpty)
            {
                ReportPerson("请先在左侧名册选择一个测试人物。");
                return;
            }
            var edit = new DebugPersonEdit
            {
                PersonId = _selectedPersonId,
                DisplayName = nameField != null ? nameField.text : string.Empty,
                Gender = _editGender,
                BirthYear = ParseInt(birthYearField, 0),
                Alive = _editAlive,
                IsFamilyMember = _editMember,
                Role = _editRole
            };
            DebugCommandResult result = _debugSession.EditPerson(edit);
            ReportPerson(result.Message);
            Refresh();
        }

        public void CreatePerson()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportPerson("未进入调试会话。");
                return;
            }
            var draft = new DebugPersonDraft
            {
                DisplayName = nameField != null ? nameField.text : string.Empty,
                Gender = _editGender,
                BirthYear = ParseInt(birthYearField, 0),
                Alive = _editAlive,
                JoinFamily = _editMember,
                Role = _editRole
            };
            DebugCommandResult result = _debugSession.CreatePerson(draft, out PersonId created);
            ReportPerson(result.Message);
            if (result.Succeeded)
            {
                _selectedPersonId = created;
                _kinshipFrom = created;
                if (kinshipFromField != null)
                    kinshipFromField.text = created.Value ?? string.Empty;
            }
            Refresh();
        }

        public void TransferHeadship()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportPerson("未进入调试会话。");
                return;
            }
            DebugCommandResult result = _debugSession.TransferHeadship(_selectedPersonId);
            ReportPerson(result.Message);
            Refresh();
        }

        public void RemovePerson()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportPerson("未进入调试会话。");
                return;
            }
            DebugCommandResult result = _debugSession.RemovePerson(_selectedPersonId);
            ReportPerson(result.Message);
            if (result.Succeeded)
                _selectedPersonId = default;
            Refresh();
        }

        public void ApplyComplexPreset()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportPerson("未进入调试会话。");
                return;
            }
            DebugCommandResult result = _debugSession.ApplyComplexFamilyPreset();
            ReportPerson(result.Message);
            Refresh();
        }

        public void SetAsTarget()
        {
            _kinshipTo = _selectedPersonId;
            if (kinshipToField != null)
                kinshipToField.text = _selectedPersonId.Value ?? string.Empty;
            _previewedKinshipKey = string.Empty;
        }

        public void PreviewKinship()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportKinship("未进入调试会话。");
                return;
            }
            ReadKinshipEndpoints();
            DebugKinshipPreview preview =
                _debugSession.PreviewKinship(_kinshipFrom, _kinshipTo, _kinshipType, _kinshipAdd);
            _previewedKinshipKey = preview.CanApply
                ? KinshipKey(_kinshipFrom, _kinshipTo, _kinshipType, _kinshipAdd)
                : string.Empty;
            var text = new StringBuilder();
            text.Append(preview.CanApply ? "可以提交：" : "不能提交：").Append(preview.Reason);
            if (!string.IsNullOrEmpty(preview.Path))
                text.Append("\n").Append(preview.Path);
            text.Append("\n操作类型：调试注入（不写入编年史，不重算关系值）。");
            ReportKinship(text.ToString());
        }

        public void ApplyKinship(bool add)
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportKinship("未进入调试会话。");
                return;
            }
            ReadKinshipEndpoints();
            _kinshipAdd = add;
            string key = KinshipKey(_kinshipFrom, _kinshipTo, _kinshipType, add);
            if (!string.Equals(key, _previewedKinshipKey, StringComparison.Ordinal))
            {
                ReportKinship("请先预览这组关系参数，再确认提交。");
                return;
            }
            DebugCommandResult result = add
                ? _debugSession.AddKinship(_kinshipFrom, _kinshipTo, _kinshipType)
                : _debugSession.RemoveKinship(_kinshipFrom, _kinshipTo, _kinshipType);
            _previewedKinshipKey = string.Empty;
            ReportKinship(result.Message);
            Refresh();
        }

        private void ReadKinshipEndpoints()
        {
            _kinshipFrom = ParsePersonId(kinshipFromField, _kinshipFrom);
            _kinshipTo = ParsePersonId(kinshipToField, _kinshipTo);
        }

        public void CycleKinshipType()
        {
            switch (_kinshipType)
            {
                case KinshipType.Spouse: _kinshipType = KinshipType.ParentChild; break;
                case KinshipType.ParentChild: _kinshipType = KinshipType.Sibling; break;
                case KinshipType.Sibling: _kinshipType = KinshipType.Adoption; break;
                default: _kinshipType = KinshipType.Spouse; break;
            }
            _previewedKinshipKey = string.Empty;
            SetLabel(kinshipTypeButton, "关系类型：" + DebugSession.TypeNameOf(_kinshipType));
        }

        private static string KinshipKey(PersonId from, PersonId to, KinshipType type, bool add) =>
            from.Value + "|" + to.Value + "|" + (int)type + "|" + (add ? "add" : "remove");

        /// <summary>Test and automation seam: fills the two kinship endpoint fields.</summary>
        public void SetKinshipEndpoints(PersonId from, PersonId to)
        {
            _kinshipFrom = from;
            _kinshipTo = to;
            if (kinshipFromField != null)
                kinshipFromField.text = from.Value ?? string.Empty;
            if (kinshipToField != null)
                kinshipToField.text = to.Value ?? string.Empty;
            _previewedKinshipKey = string.Empty;
        }

        /// <summary>Test and automation seam: fills the person editor fields.</summary>
        public void SetPersonInput(string displayName, int birthYear)
        {
            if (nameField != null)
                nameField.text = displayName ?? string.Empty;
            if (birthYearField != null)
                birthYearField.text = birthYear.ToString();
        }

        // ---------------------------------------------------------------- date actions

        public void PreviewDate()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportDate("未进入调试会话。");
                return;
            }
            ReadDateFields(out int year, out int month, out int day);
            DebugDatePreview preview = _debugSession.PreviewDateJump(year, month, day);
            _previewedDateKey = preview.CanJump ? DateKey(year, month, day) : string.Empty;
            if (datePreviewText != null)
                datePreviewText.text = preview.CanJump
                    ? preview.Summary
                    : "不能跳转：" + preview.Reason;
        }

        public void ConfirmDate()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportDate("未进入调试会话。");
                return;
            }
            ReadDateFields(out int year, out int month, out int day);
            if (!string.Equals(DateKey(year, month, day), _previewedDateKey,
                    StringComparison.Ordinal))
            {
                ReportDate("目标日期已改变或尚未预览：请重新预览跳转结果。");
                return;
            }
            DebugCommandResult result = _debugSession.JumpToDate(year, month, day);
            _previewedDateKey = string.Empty;
            ReportDate(result.Message);
            Refresh();
        }

        public void AdvanceMonth()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportDate("未进入调试会话。");
                return;
            }
            DebugCommandResult result = _debugSession.AdvanceOneMonth();
            ReportDate("操作类型：" + (result.IsDebugInjection ? "调试注入" : "正常规则") + "\n" +
                       result.Message);
            Refresh();
        }

        private void ReadDateFields(out int year, out int month, out int day)
        {
            GameDate today = _debugSession != null ? _debugSession.Today
                : SengokuChronicle.Domain.WorldClock.NewGameDate;
            year = ParseInt(targetYearField, today.Year);
            month = ParseInt(targetMonthField, today.Month);
            day = ParseInt(targetDayField, today.Day);
        }

        private static string DateKey(int year, int month, int day) =>
            year + "-" + month + "-" + day;

        /// <summary>Test and automation seam: fills the forward date target fields.</summary>
        public void SetDateTarget(int year, int month, int day)
        {
            if (targetYearField != null)
                targetYearField.text = year.ToString();
            if (targetMonthField != null)
                targetMonthField.text = month.ToString();
            if (targetDayField != null)
                targetDayField.text = day.ToString();
            _previewedDateKey = string.Empty;
        }

        // ---------------------------------------------------------------- resource actions

        public void ApplyResources()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportResource("未进入调试会话。");
                return;
            }
            IndustryResources resources = _debugSession.Resources;
            MarketState market = _debugSession.Market;
            var request = new DebugResourceRequest
            {
                Funds = ParseInt(fundsField, resources.Funds),
                Supplies = ParseInt(suppliesField, resources.Supplies),
                Intelligence = ParseInt(intelligenceField, resources.Intelligence),
                FoodInventory = ParseInt(foodField, market.FoodInventory),
                FoodPriceIndexPermille = ParseInt(foodPriceField,
                    market.FoodPriceIndexPermille),
                MaterialsPriceIndexPermille = ParseInt(materialsPriceField,
                    market.MaterialsPriceIndexPermille)
            };
            DebugCommandResult result = _debugSession.SetResourcesAndMarket(request);
            ReportResource("操作类型：" + (result.IsDebugInjection ? "调试注入" : "正常规则") + "\n" +
                           result.Message);
            Refresh();
            FillResourceFields();
        }

        /// <summary>Test and automation seam: fills all six resource and price fields.</summary>
        public void SetResourceInput(int funds, int supplies, int intelligence, int food,
            int foodPrice, int materialsPrice)
        {
            if (fundsField != null) fundsField.text = funds.ToString();
            if (suppliesField != null) suppliesField.text = supplies.ToString();
            if (intelligenceField != null) intelligenceField.text = intelligence.ToString();
            if (foodField != null) foodField.text = food.ToString();
            if (foodPriceField != null) foodPriceField.text = foodPrice.ToString();
            if (materialsPriceField != null) materialsPriceField.text = materialsPrice.ToString();
        }

        private void FillResourceFields()
        {
            if (_debugSession == null)
                return;
            IndustryResources resources = _debugSession.Resources;
            MarketState market = _debugSession.Market;
            if (fundsField != null) fundsField.text = resources.Funds.ToString();
            if (suppliesField != null) suppliesField.text = resources.Supplies.ToString();
            if (intelligenceField != null) intelligenceField.text = resources.Intelligence.ToString();
            if (foodField != null) foodField.text = market.FoodInventory.ToString();
            if (foodPriceField != null)
                foodPriceField.text = market.FoodPriceIndexPermille.ToString();
            if (materialsPriceField != null)
                materialsPriceField.text = market.MaterialsPriceIndexPermille.ToString();
        }
        // ---------------------------------------------------------------- event actions

        /// <summary>Test and automation seam: fills the event ID field.</summary>
        public void SetEventId(string eventId)
        {
            if (eventIdField != null)
                eventIdField.text = eventId ?? string.Empty;
        }

        public void InjectEvent()
        {
            if (_debugSession == null || !_debugSession.IsActive)
            {
                ReportEvent("未进入调试会话。");
                return;
            }
            string eventId = eventIdField != null ? eventIdField.text : string.Empty;
            if (!string.Equals(eventId, _previewedEventId, StringComparison.Ordinal))
            {
                // Bypassing the natural trigger is destructive enough to need a second,
                // explicit confirmation with the unchanged event ID.
                DebugCommandResult preview = _debugSession.PreviewEventInjection(eventId);
                _previewedEventId = preview.Succeeded ? eventId : string.Empty;
                ReportEvent("操作类型：调试注入\n" + preview.Message +
                            (preview.Succeeded ? "\n再次点击「确认注入」才会排入。" : string.Empty));
                return;
            }

            DebugCommandResult result = _debugSession.InjectEvent(eventId);
            _previewedEventId = string.Empty;
            ReportEvent("操作类型：" + (result.IsDebugInjection ? "调试注入" : "正常规则") + "\n" +
                        result.Message);
            Refresh();
        }

        // ---------------------------------------------------------------- result plumbings

        private void ReportPerson(string message)
        {
            if (personResultText != null)
                personResultText.text = message ?? string.Empty;
            Report(message);
        }

        private void ReportKinship(string message)
        {
            if (kinshipPreviewText != null)
                kinshipPreviewText.text = message ?? string.Empty;
            Report(message);
        }

        private void ReportDate(string message)
        {
            if (dateResultText != null)
                dateResultText.text = message ?? string.Empty;
            Report(message);
        }

        private void ReportResource(string message)
        {
            if (resourceResultText != null)
                resourceResultText.text = message ?? string.Empty;
            Report(message);
        }

        private void ReportEvent(string message)
        {
            if (eventResultText != null)
                eventResultText.text = message ?? string.Empty;
            Report(message);
        }

        private static int ParseInt(InputField field, int fallback)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text))
                return fallback;
            return int.TryParse(field.text.Trim(), out int value) ? value : fallback;
        }

        private static PersonId ParsePersonId(InputField field, PersonId fallback)
        {
            if (field == null || string.IsNullOrWhiteSpace(field.text))
                return fallback;
            return new PersonId(field.text.Trim());
        }

        // ---------------------------------------------------------------- drag

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (panelRoot == null)
                return;
            var rect = panelRoot.transform as RectTransform;
            if (rect != null)
                _dragOffset = (Vector2)rect.position - eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panelRoot == null)
                return;
            var rect = panelRoot.transform as RectTransform;
            if (rect != null)
                rect.position = eventData.position + _dragOffset;
        }

        // ---------------------------------------------------------------- hierarchy construction

        /// <summary>
        /// Builds the complete window under one fixed root and wires every control. The
        /// scene updater only needs this call; it never has to hand-wire 40 references.
        /// </summary>
        public static DebugModePanel CreateWindowUnder(Transform parent, GameSession session,
            Button openButton = null)
        {
            if (parent == null)
                throw new ArgumentNullException(nameof(parent));

            var root = new GameObject(RootName, typeof(RectTransform), typeof(Image));
            // Host the window above the title overlay: a debug window parented under the
            // title root would disappear with it and could never be used over the game.
            root.transform.SetParent(WindowHostFor(parent), false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.sizeDelta = WindowSize;
            rootRect.anchoredPosition = Vector2.zero;
            var background = root.GetComponent<Image>();
            background.color = SengokuUiTheme.IndigoDeep;
            background.raycastTarget = true;

            var panel = root.AddComponent<DebugModePanel>();
            panel.gameSession = session;
            panel.panelRoot = root;
            panel.BuildHierarchy();
            panel.BindOpenButton(openButton);
            panel.SelectTab(DebugModeTab.Session);
            root.SetActive(false);
            return panel;
        }

        /// <summary>
        /// Binds a scene-authored root the first time the guarded title-screen code turns
        /// it into the debug window. No window component or control is serialized in the
        /// scene, so a release player never sees a missing-script object.
        /// </summary>
        public void BindSceneRoot(GameSession session, GameObject root, Button openButton)
        {
            gameSession = session;
            panelRoot = root;
            BindOpenButton(openButton);
        }

        /// <summary>
        /// Builds every runtime control under <see cref="panelRoot"/>. The scene updater only
        /// authors this root plus the entry button, so the layout has one source of truth in
        /// code and cannot drift between the scene and the runtime factory. Idempotent.
        /// </summary>
        public static readonly Vector2 WindowSize = new Vector2(1200f, 680f);

        public void BuildHierarchy()
        {
            if (_built || panelRoot == null)
                return;
            _built = true;
            // The header bar is built before the watermark that sits inside it: a sibling
            // added later draws on top, so the opaque header image would otherwise hide the
            // watermark line completely.
            Transform headerRoot = panelRoot.transform;
            dragHandle = CreateHeader(panelRoot.transform, this);
            watermarkText = CreateText(panelRoot.transform, "DebugWatermark", Watermark, 20,
                SengokuUiTheme.Vermilion, TextAnchor.MiddleLeft, 20f, -14f, 520f, 26f);
            watermarkText.fontStyle = FontStyle.Bold;
            // The watermark covers the drag handle, so it must never take the pointer events
            // the header listens for.
            watermarkText.raycastTarget = false;
            messageText = CreateText(panelRoot.transform, "DebugMessage", string.Empty, 14,
                SengokuUiTheme.Parchment, TextAnchor.UpperLeft, 20f, -608f, 1160f, 56f);

            closeButton = CreateButton(headerRoot, "DebugClose", "关闭", 18, 1104f, -12f,
                72f, 32f, SengokuUiTheme.IndigoPanel, out _);
            foldButton = CreateButton(headerRoot, "DebugFold", "折叠", 18, 1020f, -12f,
                76f, 32f, SengokuUiTheme.IndigoPanel, out _);

            tabButtons = new Button[TabCount];
            tabContents = new GameObject[TabCount];
            string[] names = { "会话", "人物与家谱", "日期", "资源", "事件" };
            for (int i = 0; i < TabCount; i++)
            {
                tabButtons[i] = CreateButton(panelRoot.transform, "DebugTab" + i, names[i], 18,
                    20f + i * 140f, -70f, 132f, 32f, SengokuUiTheme.IndigoPanel, out _);
                DebugModeTab tab = (DebugModeTab)i;
                tabButtons[i].onClick.AddListener(() => SelectTab(tab));
            }

            tabContents[(int)DebugModeTab.Session] =
                CreateContent(panelRoot.transform, "DebugSessionContent");
            tabContents[(int)DebugModeTab.People] =
                CreateContent(panelRoot.transform, "DebugPeopleContent");
            tabContents[(int)DebugModeTab.Date] =
                CreateContent(panelRoot.transform, "DebugDateContent");
            tabContents[(int)DebugModeTab.Resources] =
                CreateContent(panelRoot.transform, "DebugResourcesContent");
            tabContents[(int)DebugModeTab.Events] =
                CreateContent(panelRoot.transform, "DebugEventsContent");

            BuildSessionTab(this, tabContents[(int)DebugModeTab.Session].transform);
            BuildPeopleTab(this, tabContents[(int)DebugModeTab.People].transform);
            BuildDateTab(this, tabContents[(int)DebugModeTab.Date].transform);
            BuildResourcesTab(this, tabContents[(int)DebugModeTab.Resources].transform);
            BuildEventsTab(this, tabContents[(int)DebugModeTab.Events].transform);

            closeButton.onClick.AddListener(Close);
            foldButton.onClick.AddListener(ToggleFold);
            ActiveTab = DebugModeTab.Session;
            ApplyFold();
        }

        /// <summary>
        /// Returns the root Canvas that owns the requested parent, so the debug window is a
        /// sibling of the title overlay instead of a child that hides with it. A parent
        /// outside any Canvas keeps the window exactly where the caller put it.
        /// </summary>
        private static Transform WindowHostFor(Transform parent)
        {
            if (parent == null)
                return null;
            Canvas canvas = parent.GetComponentInParent<Canvas>(true);
            return canvas != null ? canvas.rootCanvas.transform : parent;
        }

        private static RectTransform CreateHeader(Transform parent, DebugModePanel panel)
        {
            var header = new GameObject("DebugHeader", typeof(RectTransform), typeof(Image));
            header.transform.SetParent(parent, false);
            var rect = (RectTransform)header.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, 66f);
            rect.anchoredPosition = Vector2.zero;
            header.GetComponent<Image>().color = SengokuUiTheme.IndigoPanel;
            var trigger = header.AddComponent<EventTrigger>();
            var beginDrag = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginDrag.callback.AddListener(data => panel.OnBeginDrag((PointerEventData)data));
            var drag = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            drag.callback.AddListener(data => panel.OnDrag((PointerEventData)data));
            trigger.triggers.Add(beginDrag);
            trigger.triggers.Add(drag);
            return rect;
        }

        private static GameObject CreateContent(Transform parent, string name)
        {
            var content = new GameObject(name, typeof(RectTransform));
            content.transform.SetParent(parent, false);
            var rect = (RectTransform)content.transform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(20f, 60f);
            rect.offsetMax = new Vector2(-20f, -108f);
            return content;
        }

        private static void BuildSessionTab(DebugModePanel panel, Transform root)
        {
            panel.sessionSummaryText = CreateText(root, "SessionSummary", string.Empty, 20,
                SengokuUiTheme.ParchmentLight, TextAnchor.UpperLeft, 8f, -4f, 1120f, 220f);
            panel.resetButton = CreateButton(root, "DebugReset", "重置调试会话（丢弃注入）", 18,
                8f, -232f, 300f, 44f, SengokuUiTheme.IndigoPanel, out _);
            panel.resetButton.onClick.AddListener(panel.ResetDebugSession);
            panel.exitButton = CreateButton(root, "DebugExit", "退出调试并回到正常开局", 18, 320f,
                -232f, 320f, 44f, SengokuUiTheme.Vermilion, out _);
            panel.exitButton.onClick.AddListener(panel.ExitDebugSession);
        }

        private static void BuildPeopleTab(DebugModePanel panel, Transform root)
        {
            panel.rosterRoot = CreateScrollList(root, "DebugRoster", 0f, -4f, 420f, 516f);

            panel.selectedPersonText = CreateText(root, "SelectedPerson", "尚未选择人物。", 16,
                SengokuUiTheme.Gold, TextAnchor.UpperLeft, 432f, -4f, 720f, 36f);
            panel.nameField = CreateInput(root, "DebugNameField", "姓名", 432f, -46f, 300f);
            panel.birthYearField = CreateInput(root, "DebugBirthField", "出生年", 746f, -46f, 180f);

            panel.maleButton = CreateButton(root, "GenderMale", "男", 16, 432f, -90f, 84f, 32f,
                SengokuUiTheme.IndigoPanel, out _);
            panel.maleButton.onClick.AddListener(() => panel.ChooseGender(PersonGender.Male));
            panel.femaleButton = CreateButton(root, "GenderFemale", "女", 16, 524f, -90f, 84f, 32f,
                SengokuUiTheme.IndigoPanel, out _);
            panel.femaleButton.onClick.AddListener(() => panel.ChooseGender(PersonGender.Female));
            panel.unspecifiedButton = CreateButton(root, "GenderUnspecified", "未定", 16, 616f,
                -90f, 96f, 32f, SengokuUiTheme.IndigoPanel, out _);
            panel.unspecifiedButton.onClick.AddListener(() =>
                panel.ChooseGender(PersonGender.Unspecified));
            panel.aliveButton = CreateButton(root, "AliveToggle", "存活：是", 16, 720f, -90f, 96f,
                32f, SengokuUiTheme.IndigoPanel, out _);
            panel.aliveButton.onClick.AddListener(panel.ToggleAlive);
            panel.memberButton = CreateButton(root, "MemberToggle", "在谱：是", 16, 824f, -90f,
                96f, 32f, SengokuUiTheme.IndigoPanel, out _);
            panel.memberButton.onClick.AddListener(panel.ToggleMembership);
            panel.roleButton = CreateButton(root, "RoleCycle", "身份：直系亲族", 16, 928f, -90f,
                220f, 32f, SengokuUiTheme.IndigoPanel, out _);
            panel.roleButton.onClick.AddListener(panel.CycleRole);

            panel.applyEditButton = CreateButton(root, "ApplyPersonEdit", "应用人物编辑", 16, 432f,
                -130f, 190f, 34f, SengokuUiTheme.Vermilion, out _);
            panel.applyEditButton.onClick.AddListener(panel.ApplyPersonEdit);
            panel.transferHeadButton = CreateButton(root, "TransferHead", "转移家主", 16, 630f,
                -130f, 140f, 34f, SengokuUiTheme.IndigoPanel, out _);
            panel.transferHeadButton.onClick.AddListener(panel.TransferHeadship);
            panel.removePersonButton = CreateButton(root, "RemovePerson", "移除测试人物", 16, 778f,
                -130f, 160f, 34f, SengokuUiTheme.IndigoPanel, out _);
            panel.removePersonButton.onClick.AddListener(panel.RemovePerson);
            panel.createPersonButton = CreateButton(root, "CreatePerson", "按上方字段新建人物", 16,
                946f, -130f, 200f, 34f, SengokuUiTheme.Jade, out _);
            panel.createPersonButton.onClick.AddListener(panel.CreatePerson);
            panel.presetButton = CreateButton(root, "ComplexPreset", "生成复杂家谱预设（16 人 / 6 代）",
                16, 432f, -172f, 360f, 32f, SengokuUiTheme.Jade, out _);
            panel.presetButton.onClick.AddListener(panel.ApplyComplexPreset);

            panel.kinshipFromField = CreateInput(root, "KinshipFrom", "甲方人物 ID", 432f, -212f,
                240f);
            panel.kinshipToField = CreateInput(root, "KinshipTo", "乙方人物 ID", 680f, -212f, 240f);
            panel.setAsTargetButton = CreateButton(root, "SetAsTarget", "把选中人物设为乙方", 16,
                432f, -252f, 240f, 32f, SengokuUiTheme.IndigoPanel, out _);
            panel.setAsTargetButton.onClick.AddListener(panel.SetAsTarget);
            panel.kinshipTypeButton = CreateButton(root, "KinshipType", "关系类型：配偶", 16, 680f,
                -252f, 180f, 32f, SengokuUiTheme.IndigoPanel, out _);
            panel.kinshipTypeButton.onClick.AddListener(panel.CycleKinshipType);
            panel.kinshipPreviewButton = CreateButton(root, "PreviewKinship", "预览关系变更", 16, 868f,
                -252f, 130f, 32f, SengokuUiTheme.IndigoPanel, out _);
            panel.kinshipPreviewButton.onClick.AddListener(panel.PreviewKinship);
            panel.kinshipApplyButton = CreateButton(root, "ConfirmKinship", "确认添加", 16, 1006f,
                -252f, 140f, 32f, SengokuUiTheme.Vermilion, out _);
            panel.kinshipApplyButton.onClick.AddListener(() => panel.ApplyKinship(true));
            panel.kinshipRemoveButton = CreateButton(root, "RemoveKinship", "预览后解除", 16, 432f,
                -292f, 150f, 30f, SengokuUiTheme.IndigoPanel, out _);
            panel.kinshipRemoveButton.onClick.AddListener(() => panel.ApplyKinship(false));

            panel.kinshipPreviewText = CreateText(root, "KinshipPreview", string.Empty, 15,
                SengokuUiTheme.Parchment, TextAnchor.UpperLeft, 432f, -330f, 720f, 80f);
            panel.personResultText = CreateText(root, "PersonResult", string.Empty, 15,
                SengokuUiTheme.Gold, TextAnchor.UpperLeft, 432f, -414f, 720f, 80f);
        }

        private static void BuildDateTab(DebugModePanel panel, Transform root)
        {
            panel.dateSummaryText = CreateText(root, "DateSummary", string.Empty, 20,
                SengokuUiTheme.ParchmentLight, TextAnchor.UpperLeft, 8f, -4f, 1120f, 34f);
            panel.targetYearField = CreateInput(root, "TargetYear", "年", 8f, -46f, 140f);
            panel.targetMonthField = CreateInput(root, "TargetMonth", "月", 156f, -46f, 120f);
            panel.targetDayField = CreateInput(root, "TargetDay", "日", 284f, -46f, 120f);
            panel.previewDateButton = CreateButton(root, "PreviewDate", "预览跳转", 16, 412f, -46f,
                140f, 36f, SengokuUiTheme.IndigoPanel, out _);
            panel.previewDateButton.onClick.AddListener(panel.PreviewDate);
            panel.confirmDateButton = CreateButton(root, "ConfirmDate", "确认跳转（调试注入）", 16,
                560f, -46f, 220f, 36f, SengokuUiTheme.Vermilion, out _);
            panel.confirmDateButton.onClick.AddListener(panel.ConfirmDate);
            panel.advanceMonthButton = CreateButton(root, "AdvanceMonth", "正常推进一个月", 16, 788f,
                -46f, 200f, 36f, SengokuUiTheme.Jade, out _);
            panel.advanceMonthButton.onClick.AddListener(panel.AdvanceMonth);
            panel.datePreviewText = CreateText(root, "DatePreview", string.Empty, 15,
                SengokuUiTheme.Parchment, TextAnchor.UpperLeft, 8f, -92f, 1120f, 110f);
            panel.dateResultText = CreateText(root, "DateResult", string.Empty, 15,
                SengokuUiTheme.Gold, TextAnchor.UpperLeft, 8f, -208f, 1120f, 120f);
        }

        private static void BuildResourcesTab(DebugModePanel panel, Transform root)
        {
            panel.resourceSummaryText = CreateText(root, "ResourceSummary", string.Empty, 20,
                SengokuUiTheme.ParchmentLight, TextAnchor.UpperLeft, 8f, -4f, 1120f, 52f);
            panel.fundsField = CreateInput(root, "FundsField", "资金", 8f, -66f, 170f);
            panel.suppliesField = CreateInput(root, "SuppliesField", "物资", 186f, -66f, 170f);
            panel.intelligenceField = CreateInput(root, "IntelligenceField", "情报", 364f, -66f,
                170f);
            panel.foodField = CreateInput(root, "FoodField", "粮食", 542f, -66f, 170f);
            panel.foodPriceField = CreateInput(root, "FoodPriceField", "粮价‰", 720f, -66f, 150f);
            panel.materialsPriceField = CreateInput(root, "MaterialsPriceField", "物资价‰",
                878f, -66f, 150f);
            panel.applyResourcesButton = CreateButton(root, "ApplyResources",
                "原子应用全部资源与价格（调试注入）", 16, 8f, -114f, 380f, 38f,
                SengokuUiTheme.Vermilion, out _);
            panel.applyResourcesButton.onClick.AddListener(panel.ApplyResources);
            panel.resourceResultText = CreateText(root, "ResourceResult", string.Empty, 15,
                SengokuUiTheme.Gold, TextAnchor.UpperLeft, 8f, -162f, 1120f, 120f);
        }

        private static void BuildEventsTab(DebugModePanel panel, Transform root)
        {
            panel.eventSummaryText = CreateText(root, "EventSummary", string.Empty, 18,
                SengokuUiTheme.ParchmentLight, TextAnchor.UpperLeft, 8f, -4f, 1120f, 32f);
            panel.eventListRoot = CreateScrollList(root, "DebugEventList", 0f, -44f, 540f, 440f);

            panel.eventIdField = CreateInput(root, "EventIdField", "事件 ID", 556f, -44f, 460f);
            panel.injectEventButton = CreateButton(root, "InjectEvent", "确认注入（调试注入）", 16,
                556f, -86f, 240f, 36f, SengokuUiTheme.Vermilion, out _);
            panel.injectEventButton.onClick.AddListener(panel.InjectEvent);
            panel.eventResultText = CreateText(root, "EventResult", string.Empty, 15,
                SengokuUiTheme.Gold, TextAnchor.UpperLeft, 556f, -128f, 460f, 220f);
            CreateText(root, "EventNote",
                "调试注入只检查目录与重复，不检查自然触发条件；选项与效果仍走正式流程。", 14,
                SengokuUiTheme.InkFaint, TextAnchor.UpperLeft, 556f, -352f, 460f, 80f);
        }

        private static void EnsureRowCapacity(int needed, RectTransform root, List<Button> buttons,
            List<Text> labels, int maxRows, string prefix)
        {
            int target = Mathf.Min(needed, maxRows);
            while (buttons.Count < target)
            {
                int index = buttons.Count;
                Button button = CreateButton(root, prefix + index, string.Empty, 14,
                    0f, -index * RowHeight, 0f, RowHeight - 2f, SengokuUiTheme.IndigoPanel,
                    out Text label);
                var rect = (RectTransform)button.transform;
                // The row spans the scroll content instead of a fixed pixel width: the roster
                // list is 420 wide, so its viewport and content are 412 after the 4 px inset,
                // and a 420-wide row would have its right edge clipped by the mask.
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(0f, -index * RowHeight);
                label.alignment = TextAnchor.MiddleLeft;
                // Row labels carry the person/event id, which is longer than the fixed row.
                // Best fit keeps the whole line inside the row instead of dropping its tail,
                // and never grows past the row's own type size.
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 10;
                label.resizeTextMaxSize = 14;
                buttons.Add(button);
                labels.Add(label);
            }
            // The list is a ScrollRect content, so its height follows the visible rows.
            var contentRect = root;
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, target * RowHeight);
        }

        private const float RowHeight = 26f;

        /// <summary>Creates one clipped, vertically scrollable list and returns its content.</summary>
        private static RectTransform CreateScrollList(Transform parent, string name, float x,
            float y, float width, float height)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect),
                typeof(RectMask2D));
            root.transform.SetParent(parent, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.sizeDelta = new Vector2(width, height);
            rootRect.anchoredPosition = new Vector2(x, y);
            root.GetComponent<Image>().color = SengokuUiTheme.IndigoBackdrop;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(root.transform, false);
            var viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = new Vector2(4f, 4f);
            viewportRect.offsetMax = new Vector2(-4f, -4f);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0f, 1f);
            contentRect.sizeDelta = new Vector2(0f, height);
            contentRect.anchoredPosition = Vector2.zero;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return contentRect;
        }

        private static Text CreateText(Transform parent, string name, string value, int size,
            Color color, TextAnchor anchor, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            var text = go.AddComponent<Text>();
            text.font = SengokuUiTheme.Serif;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.text = value;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, int size,
            float x, float y, float width, float height, Color background, out Text labelText)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, y);
            var image = go.GetComponent<Image>();
            image.color = background;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            labelText = CreateText(go.transform, "Label", label, size,
                SengokuUiTheme.ParchmentLight, TextAnchor.MiddleCenter, 0f, 0f, width, height);
            labelText.rectTransform.anchorMin = Vector2.zero;
            labelText.rectTransform.anchorMax = Vector2.one;
            labelText.rectTransform.offsetMin = Vector2.zero;
            labelText.rectTransform.offsetMax = Vector2.zero;
            return button;
        }

        private static InputField CreateInput(Transform parent, string name, string placeholder,
            float x, float y, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, 36f);
            rect.anchoredPosition = new Vector2(x, y);
            go.GetComponent<Image>().color = SengokuUiTheme.Parchment;

            Text text = CreateText(go.transform, "Text", string.Empty, 15, SengokuUiTheme.Ink,
                TextAnchor.MiddleLeft, 8f, 0f, width - 16f, 36f);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = new Vector2(8f, 0f);
            text.rectTransform.offsetMax = new Vector2(-8f, 0f);
            text.supportRichText = false;

            Text hint = CreateText(go.transform, "Placeholder", placeholder, 15,
                SengokuUiTheme.InkFaint, TextAnchor.MiddleLeft, 8f, 0f, width - 16f, 36f);
            hint.rectTransform.anchorMin = Vector2.zero;
            hint.rectTransform.anchorMax = Vector2.one;
            hint.rectTransform.offsetMin = new Vector2(8f, 0f);
            hint.rectTransform.offsetMax = new Vector2(-8f, 0f);

            var field = go.GetComponent<InputField>();
            field.targetGraphic = go.GetComponent<Image>();
            field.textComponent = text;
            field.placeholder = hint;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        private void EnsureBuilt()
        {
            BuildHierarchy();
            if (watermarkText != null)
                watermarkText.text = Watermark;
        }
    }
}
#endif
#pragma warning restore UAC0009
