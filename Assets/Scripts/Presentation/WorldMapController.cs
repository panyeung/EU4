using System;
using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 世界地图控制器（M2-02）：目的地选择、旅行预览、确认/取消、
    /// 到达后的城镉菜单与「未开放」设施占位。
    /// 所有状态读写通过 GameSession；预览绝不修改日期/位置。
    /// 公开 BindReferences / 状态属性与访问器，供 Editor 生成器与 EditMode 测试注入。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldMapController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private GameSession gameSession;
        // M1 面板（可空）：确认旅行后同步刷新，避免改写 M1 面板逻辑。
        [SerializeField]
        private WorldClockPanel worldClockPanel;

        [Header("Location Marker")]
        [SerializeField]
        private Text locationMarkerText;

        [Header("Feedback")]
        [SerializeField]
        private Text feedbackText;

        [Header("Current Date (M2 显示)")]
        [SerializeField]
        private Text currentDateText;

        [Header("Destination")]
        [SerializeField]
        private Text destinationNameText;

        [SerializeField]
        private Text travelTimeText;

        [SerializeField]
        private Text arrivalDateText;

        [Header("Buttons")]
        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private Button cancelButton;

        [SerializeField]
        private Button returnToMapButton;

        [Header("Location Buttons (bound by generator; mark current location)")]
        [SerializeField]
        private Button kiyosuButton;
        [SerializeField]
        private Text kiyosuLabel;
        [SerializeField]
        private Button atsutaButton;
        [SerializeField]
        private Text atsutaLabel;
        [SerializeField]
        private Button nagoyaButton;
        [SerializeField]
        private Text nagoyaLabel;

        [Header("Town Menu")]
        [SerializeField]
        private GameObject townMenuRoot;

        [SerializeField]
        private Text townMenuTitleText;

        [SerializeField]
        private Text facility1Text;

        [SerializeField]
        private Text facility2Text;

        [SerializeField]
        private Text facility3Text;

        [SerializeField]
        private Text[] facilityTexts;

        [Header("Town NPCs")]
        [SerializeField]
        private TownNpcDialoguePanel townNpcDialoguePanel;

        [SerializeField]
        private Button openNpcRosterButton;

        [Header("Supply Shortage Mission")]
        [SerializeField] private SupplyShortageMissionPanel supplyShortageMissionPanel;

        // ---- 公开状态（测试断言用）----

        /// <summary>是否位于城镉菜单。</summary>
        public bool InTownMenu => _inTownMenu;

        /// <summary>当前选中的目的地；未选择为 null。</summary>
        public LocationId? SelectedDestination => _selectedDestination;

        /// <summary>当前旅行预览；未就绪为 null。</summary>
        public TravelPreview? CurrentPreview => _preview;

        /// <summary>最近一次反馈文本。</summary>
        public string Feedback => _feedback;

        /// <summary>当前所在的 GameSession。</summary>
        public GameSession Session => gameSession;

        /// <summary>城镉菜单根对象（初始隐藏）。</summary>
        public GameObject GetTownMenuRoot() => townMenuRoot;

        /// <summary>设施占位文本数组（均为「未开放」）。</summary>
        public Text[] GetFacilityTexts() => facilityTexts;

        private LocationId? _selectedDestination;
        private TravelPreview? _preview;
        private bool _inTownMenu;
        private string _feedback = "请选择目的地。";

        private void Awake()
        {
            if (gameSession == null)
                Debug.LogWarning($"{nameof(WorldMapController)}: gameSession 未在 Inspector 中赋值。");
        }

        private void Start()
        {
            if (gameSession == null)
                gameSession = GetComponentInParent<GameSession>() ?? FindAnyObjectByType<GameSession>();
            if (worldClockPanel == null)
                worldClockPanel = FindAnyObjectByType<WorldClockPanel>();
            BindButtonListeners(true);
            BindTownNpcButton(true);
            if (townNpcDialoguePanel != null)
                townNpcDialoguePanel.BindWorldMapController(this);
            BindSupplyShortageMissionPanel(supplyShortageMissionPanel);
            RefreshAll();
        }

        private void OnEnable()
        {
            BindButtonListeners(true);
            BindTownNpcButton(true);
        }

        private void OnDisable()
        {
            BindButtonListeners(false);
            BindTownNpcButton(false);
        }

        // ---- 公开 API（Editor 生成器与测试注入）----

        /// <summary>
        /// Editor 生成器 / 测试以反射外的类型安全方式注入依赖与 UI 引用。
        /// 仅执行赋值，不持久化、不修改 M1 面板行为。
        /// </summary>
        public void BindReferences(
            GameSession session,
            WorldClockPanel clock,
            Text locationMarker, Text feedback, Text currentDate,
            Text destinationName, Text travelTime, Text arrivalDate,
            Button confirm, Button back, Button returnToMap,
            GameObject townRoot, Text townTitle, Text[] facilities)
        {
            gameSession = session;
            worldClockPanel = clock;
            locationMarkerText = locationMarker;
            feedbackText = feedback;
            currentDateText = currentDate;
            destinationNameText = destinationName;
            travelTimeText = travelTime;
            arrivalDateText = arrivalDate;
            confirmButton = confirm;
            cancelButton = back;
            returnToMapButton = returnToMap;
            townMenuRoot = townRoot;
            townMenuTitleText = townTitle;
            facility1Text = facilities != null && facilities.Length > 0 ? facilities[0] : null;
            facility2Text = facilities != null && facilities.Length > 1 ? facilities[1] : null;
            facility3Text = facilities != null && facilities.Length > 2 ? facilities[2] : null;
            facilityTexts = facilities;
        }

        /// <summary>选择目的地并刷新预览；不修改日期/位置。</summary>
        public void SelectDestination(LocationId destination)
        {
            _selectedDestination = destination;
            _preview = gameSession != null ? gameSession.PreviewTravel(destination) : (TravelPreview?)null;
            RefreshDestinationPanel();

            if (gameSession == null)
                SetFeedback("缺少 GameSession，无法预览。");
            else if (!destination.IsValid)
                SetFeedback("未知地点，无法旅行。");
            else if (destination == gameSession.CurrentLocation)
                SetFeedback($"当前位置：{LocationId.GetChineseName(gameSession.CurrentLocation)}，无法向自身旅行。");
            else if (_preview == null)
                SetFeedback("无直达路线（热田 ↔ 那古野 不可直达）。");
            else
                SetFeedback($"已选择目的地：{LocationId.GetChineseName(destination)}，耗时 {_preview.Value.TravelDays} 天，预计 {_preview.Value.EstimatedArrivalDate} 到达。");

            RefreshFeedback();
            RefreshLocationButtons();
            RefreshButtons();
        }

        /// <summary>确认旅行：唯一执行旅行的动作。取消/返回不触发。</summary>
        public void ConfirmTravel()
        {
            if (gameSession == null)
            {
                SetFeedback("会话为空，无法旅行。");
                return;
            }

            if (_preview == null)
            {
                SetFeedback("请选择有效目的地。");
                RefreshButtons();
                return;
            }

            TravelPreview preview = _preview.Value;
            bool ok = gameSession.ConfirmTravel(preview);
            if (ok)
            {
                _preview = null;
                _selectedDestination = null;
                OpenTownMenu();
                RefreshAll();
                SetFeedback($"已抵达{preview.DestinationName}，当前日期{gameSession.WorldClock.CurrentDate}。");
            }
            else
            {
                _preview = null;
                _selectedDestination = null;
                SetFeedback("旅行失败：路线或日期无效，日期与位置保持不变。");
                RefreshDestinationPanel();
                RefreshButtons();
            }
        }

        /// <summary>取消：不修改日期/位置，仅关闭预览。</summary>
        public void CancelTravel()
        {
            _preview = null;
            _selectedDestination = null;
            SetFeedback("已取消，日期与位置保持不变。");
            RefreshDestinationPanel();
            RefreshFeedback();
            RefreshButtons();
        }

        /// <summary>从城镉菜单返回地图。</summary>
        public void ReturnToMap()
        {
            _inTownMenu = false;
            if (supplyShortageMissionPanel != null)
                supplyShortageMissionPanel.Close();
            if (townNpcDialoguePanel != null)
                townNpcDialoguePanel.Close();
            if (townMenuRoot != null)
                townMenuRoot.SetActive(false);
            SetMapContentVisible(true);
            SetFeedback($"返回地图，当前位置：{LocationId.GetChineseName(gameSession != null ? gameSession.CurrentLocation : default)}。请选择目的地。");
            RefreshAll();
        }

        /// <summary>全量刷新 UI（当前日期、位置标记、反馈、按钮），并同步刷新 M1 面板。</summary>
        public void RefreshAll()
        {
            RefreshCurrentDate();
            RefreshLocationMarker();
            RefreshLocationButtons();
            RefreshDestinationPanel();
            RefreshFeedback();
            RefreshButtons();
            if (worldClockPanel != null)
                worldClockPanel.Refresh();
            if (supplyShortageMissionPanel != null)
                supplyShortageMissionPanel.Refresh();
        }

        // ---- 内部 ----

        /// <summary>幂等注册/注销按钮监听。</summary>
        private void BindButtonListeners(bool add)
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmTravel);
                if (add) confirmButton.onClick.AddListener(ConfirmTravel);
            }
            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(CancelTravel);
                if (add) cancelButton.onClick.AddListener(CancelTravel);
            }
            if (returnToMapButton != null)
            {
                returnToMapButton.onClick.RemoveListener(ReturnToMap);
                if (add) returnToMapButton.onClick.AddListener(ReturnToMap);
            }
            if (kiyosuButton != null)
            {
                kiyosuButton.onClick.RemoveListener(OnKiyosuClicked);
                if (add) kiyosuButton.onClick.AddListener(OnKiyosuClicked);
            }
            if (atsutaButton != null)
            {
                atsutaButton.onClick.RemoveListener(OnAtsutaClicked);
                if (add) atsutaButton.onClick.AddListener(OnAtsutaClicked);
            }
            if (nagoyaButton != null)
            {
                nagoyaButton.onClick.RemoveListener(OnNagoyaClicked);
                if (add) nagoyaButton.onClick.AddListener(OnNagoyaClicked);
            }
        }

        /// <summary>绑定城镇 NPC 名单按钮；重复调用不会叠加监听。</summary>
        public void BindTownNpcRoster(
            TownNpcDialoguePanel panel, Button openRosterButton)
        {
            if (openNpcRosterButton != null)
                openNpcRosterButton.onClick.RemoveListener(OpenNpcRoster);

            townNpcDialoguePanel = panel;
            openNpcRosterButton = openRosterButton;

            if (townNpcDialoguePanel != null)
                townNpcDialoguePanel.BindWorldMapController(this);

            BindTownNpcButton(isActiveAndEnabled);
        }

        private void BindTownNpcButton(bool add)
        {
            if (openNpcRosterButton == null)
                return;

            openNpcRosterButton.onClick.RemoveListener(OpenNpcRoster);
            if (add)
                openNpcRosterButton.onClick.AddListener(OpenNpcRoster);
        }

        /// <summary>显示会话当前地点对应的 NPC 名单。</summary>
        public void OpenNpcRoster()
        {
            if (!_inTownMenu ||
                gameSession == null ||
                townNpcDialoguePanel == null)
                return;

            townNpcDialoguePanel.BindWorldMapController(this);
            townNpcDialoguePanel.OpenRoster(gameSession);
        }

        /// <summary>Editor generator only: bind the three fixed location buttons + labels.</summary>
        public void BindLocationButtons(Button kiyosu, Text kiyosuLabelRef,
                                       Button atsuta, Text atsutaLabelRef,
                                       Button nagoya, Text nagoyaLabelRef)
        {
            kiyosuButton = kiyosu; this.kiyosuLabel = kiyosuLabelRef;
            atsutaButton = atsuta; this.atsutaLabel = atsutaLabelRef;
            nagoyaButton = nagoya; this.nagoyaLabel = nagoyaLabelRef;
            BindButtonListeners(true);
            RefreshLocationButtons();
        }

        private void OnKiyosuClicked()  => SelectDestination(LocationId.Kiyosu);
        private void OnAtsutaClicked()  => SelectDestination(LocationId.Atsuta);
        private void OnNagoyaClicked()  => SelectDestination(LocationId.Nagoya);

        private void RefreshLocationButtons()
        {
            if (gameSession == null)
                return;
            SetLocationButton(kiyosuButton, kiyosuLabel, LocationId.Kiyosu);
            SetLocationButton(atsutaButton, atsutaLabel, LocationId.Atsuta);
            SetLocationButton(nagoyaButton, nagoyaLabel, LocationId.Nagoya);
        }

        private void SetLocationButton(Button button, Text label, LocationId location)
        {
            if (button == null)
                return;
            bool isCurrent = gameSession != null && location == gameSession.CurrentLocation;
            button.interactable = !isCurrent;
            if (label != null)
                label.text = $"{LocationId.GetChineseName(location)}{(isCurrent ? " 《当前》" : "")}";
        }

        private void RefreshCurrentDate()
        {
            if (currentDateText == null || gameSession == null || gameSession.WorldClock == null)
                return;
            currentDateText.text = $"日期：{gameSession.WorldClock.CurrentDate}";
        }

        private void RefreshLocationMarker()
        {
            if (locationMarkerText == null || gameSession == null)
                return;
            string name = LocationId.GetChineseName(gameSession.CurrentLocation);
            locationMarkerText.text = $"当前位置：{name}";
        }

        private void RefreshDestinationPanel()
        {
            if (destinationNameText != null)
                destinationNameText.text = _selectedDestination.HasValue
                    ? LocationId.GetChineseName(_selectedDestination.Value)
                    : "—";

            if (travelTimeText != null)
                travelTimeText.text = _preview.HasValue ? $"{_preview.Value.TravelDays} 天" : "—";

            if (arrivalDateText != null)
                arrivalDateText.text = _preview.HasValue ? _preview.Value.EstimatedArrivalDate.ToString() : "—";
        }

        /// <summary>镜像 _feedback 到 UI 文本。</summary>
        private void RefreshFeedback()
        {
            if (feedbackText != null)
                feedbackText.text = _feedback;
        }

        private void RefreshButtons()
        {
            if (confirmButton != null)
                confirmButton.interactable = _preview.HasValue && !_inTownMenu;
            if (cancelButton != null)
                cancelButton.interactable = _preview.HasValue && !_inTownMenu;
            if (returnToMapButton != null)
                returnToMapButton.interactable = _inTownMenu;
        }

        /// <summary>到达后打开城镉菜单；设施占位标记为「未开放」并禁用。</summary>
        public void OpenTownMenu()
        {
            _inTownMenu = true;
            if (supplyShortageMissionPanel != null)
            {
                supplyShortageMissionPanel.Bind(gameSession);
                supplyShortageMissionPanel.Open();
            }
            SetMapContentVisible(false);
            if (townNpcDialoguePanel != null)
                townNpcDialoguePanel.Close();
            if (townMenuRoot != null)
            {
                Image townBackground = townMenuRoot.GetComponent<Image>();
                if (townBackground != null)
                {
                    Color backgroundColor = townBackground.color;
                    backgroundColor.a = 1f;
                    townBackground.color = backgroundColor;
                }
                townMenuRoot.SetActive(true);
            }

            if (townMenuTitleText != null && gameSession != null)
                townMenuTitleText.text = $"{LocationId.GetChineseName(gameSession.CurrentLocation)} 镇";

            if (facilityTexts == null)
                return;
            string[] labels = { "设施一", "设施二", "设施三" };
            for (int i = 0; i < facilityTexts.Length; i++)
            {
                Text t = facilityTexts[i];
                if (t == null)
                    continue;
                string label = i < labels.Length ? labels[i] : $"设施{i + 1}";
                t.text = $"{label}：未开放";
                t.enabled = false; // 未开放：显式禁用
            }
        }

        public void BindSupplyShortageMissionPanel(SupplyShortageMissionPanel panel)
        {
            if (supplyShortageMissionPanel != null && supplyShortageMissionPanel != panel)
                supplyShortageMissionPanel.Close();
            supplyShortageMissionPanel = panel;
            if (panel == null)
                return;
            panel.Bind(gameSession);
            if (_inTownMenu) panel.Open();
            else panel.Close();
        }

        private void SetMapContentVisible(bool visible)
        {
            SetVisible(locationMarkerText, visible);
            SetVisible(feedbackText, visible);
            SetVisible(destinationNameText, visible);
            SetVisible(travelTimeText, visible);
            SetVisible(arrivalDateText, visible);
            SetVisible(confirmButton, visible);
            SetVisible(cancelButton, visible);
            SetVisible(kiyosuButton, visible);
            SetVisible(atsutaButton, visible);
            SetVisible(nagoyaButton, visible);
        }

        private static void SetVisible(Behaviour component, bool visible)
        {
            if (component != null)
                component.gameObject.SetActive(visible);
        }

        private void SetFeedback(string message)
        {
            _feedback = message;
            if (feedbackText != null)
                feedbackText.text = message;
        }
    }
}
