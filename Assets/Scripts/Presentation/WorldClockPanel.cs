using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 最小 uGUI 面板：显示当前日期，并提供「推进一天」与「结束月份」两个按钮。
    /// 推进一天保持 M1/M2 既有语义；结束月份为 SC-004 月度回合入口，
    /// 成功时同步刷新本面板与地图侧的两处日期显示。
    /// SC-010：家族灭亡（Extinct）时两个时间按钮都禁用，加载较早存档后恢复。
    /// 范围：仅负责呈现与输入绑定，不承载日期规则。
    /// </summary>
    public sealed class WorldClockPanel : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private GameSession gameSession;

        [Header("UI")]
        [SerializeField]
        private Text dateText;

        [SerializeField]
        private Button advanceButton;

        [Header("Month Turn (SC-004, optional)")]
        [SerializeField]
        private Button endMonthButton;

        /// <summary>可空：成功结束月份后同步刷新其「日期：」文本；缺省时仅刷新本面板。</summary>
        [SerializeField]
        private WorldMapController worldMapController;

        /// <summary>最近一次「结束月份」的结果说明；成功时为空。</summary>
        public string LastMonthTurnMessage { get; private set; } = string.Empty;

        /// <summary>
        /// 最近一次「推进一天」被拒绝的原因（继承暂停、事件待选或日期上限）；
        /// 成功时为空。SC-010：继承暂停时不得再误报为日期上限。
        /// </summary>
        public string LastAdvanceMessage { get; private set; } = string.Empty;

        private bool _sessionSubscribed;

        /// <summary>Editor 脚本注入依赖（M1：仅 Editor 场景创建器使用）。</summary>
        public void SetDependencies(GameSession session, Button button, Text text)
        {
            UnsubscribeSession();
            gameSession = session;
            advanceButton = button;
            dateText = text;
            SubscribeSession();
            Refresh();
        }

        /// <summary>
        /// SC-004：注入月末按钮与地图控制器引用。幂等：重复绑定不会叠加监听，
        /// 更换按钮时旧按钮的监听会被移除。
        /// </summary>
        public void BindMonthTurn(Button monthButton, WorldMapController mapController)
        {
            if (endMonthButton != null)
                endMonthButton.onClick.RemoveListener(OnEndMonthClicked);

            endMonthButton = monthButton;
            worldMapController = mapController;

            if (endMonthButton != null)
            {
                endMonthButton.onClick.RemoveListener(OnEndMonthClicked);
                endMonthButton.onClick.AddListener(OnEndMonthClicked);
            }
        }

        private void Awake()
        {
            if (gameSession == null)
                Debug.LogWarning($"{nameof(WorldClockPanel)}: gameSession 未在 Inspector 中赋值，将使用当前场景中的 GameSession。");
        }

        private void Start()
        {
            if (gameSession == null)
                gameSession = GetComponentInParent<GameSession>() ?? FindAnyObjectByType<GameSession>();
            SubscribeSession();
            Refresh();
        }

        private void OnValidate()
        {
            Refresh();
        }

        private void OnAdvanceClicked()
        {
            TryAdvanceOneDay();
        }

        /// <summary>
        /// 推进一天。被拒绝时把真实原因写入 <see cref="LastAdvanceMessage"/> 并记录到
        /// Console；继承暂停时显示继承原因，而不是「日期已达支持上限」。
        /// </summary>
        public bool TryAdvanceOneDay()
        {
            if (gameSession == null)
            {
                LastAdvanceMessage = "会话未就绪，无法推进。";
                Debug.LogWarning($"{nameof(WorldClockPanel)}: gameSession 为空，无法推进。");
                return false;
            }

            if (gameSession.AdvanceOneDay())
            {
                LastAdvanceMessage = string.Empty;
                Refresh();
                return true;
            }

            LastAdvanceMessage = DescribeAdvanceRefusal();
            Debug.Log($"{nameof(WorldClockPanel)}: {LastAdvanceMessage}");
            return false;
        }

        private string DescribeAdvanceRefusal()
        {
            switch (gameSession.LastMonthTurnOutcome)
            {
                case MonthTurnOutcome.PendingEvent:
                    return "尚有事件等待选择，请先处理事件后再推进时间。";
                case MonthTurnOutcome.PendingSuccession:
                    return string.IsNullOrWhiteSpace(gameSession.LastEventMessage)
                        ? "尚有继承决策待处理，请先完成继承或成年交权后再推进时间。"
                        : gameSession.LastEventMessage;
                case MonthTurnOutcome.DateOutOfRange:
                    return "日期已达支持上限，无法再推进。";
                default:
                    return "无法推进时间，日期保持不变。";
            }
        }

        private void OnEndMonthClicked()
        {
            if (gameSession == null)
            {
                Debug.LogWarning($"{nameof(WorldClockPanel)}: gameSession 为空，无法结束月份。");
                return;
            }

            if (gameSession.TryAdvanceToNextMonth())
            {
                LastMonthTurnMessage = string.Empty;
                Refresh();
                RefreshMapDateDisplay();
                return;
            }

            switch (gameSession.LastMonthTurnOutcome)
            {
                case MonthTurnOutcome.PendingEvent:
                    LastMonthTurnMessage = "尚有事件等待选择，请先处理事件后再结束月份。";
                    break;
                case MonthTurnOutcome.PendingSuccession:
                    LastMonthTurnMessage = "尚有继承决策待处理，请先完成继承（或成年交权）后再结束月份。";
                    break;
                case MonthTurnOutcome.DateOutOfRange:
                    LastMonthTurnMessage = "下一月超出支持范围，日期保持不变。";
                    break;
                case MonthTurnOutcome.ContentUnavailable:
                    LastMonthTurnMessage = string.IsNullOrWhiteSpace(gameSession.LastEventMessage)
                        ? "事件目录不可用，无法结束月份。"
                        : gameSession.LastEventMessage;
                    break;
                case MonthTurnOutcome.SchedulingFailed:
                    LastMonthTurnMessage = string.IsNullOrWhiteSpace(gameSession.LastEventMessage)
                        ? "本月事件无法排定，日期保持不变。"
                        : gameSession.LastEventMessage;
                    break;
                default:
                    LastMonthTurnMessage = "无法结束月份，日期保持不变。";
                    break;
            }

            Debug.Log($"{nameof(WorldClockPanel)}: {LastMonthTurnMessage}");
        }

        public void Refresh()
        {
            if (gameSession == null)
                return;

            bool extinct = gameSession.IsFamilyExtinct;
            if (advanceButton != null)
                advanceButton.interactable = !extinct;
            if (endMonthButton != null)
                endMonthButton.interactable = !extinct;

            if (dateText == null || gameSession.WorldClock == null)
                return;

            dateText.text = gameSession.WorldClock.CurrentDate.ToString();
        }

        /// <summary>刷新地图侧日期显示；场景中无地图控制器时静默跳过。</summary>
        private void RefreshMapDateDisplay()
        {
            if (worldMapController == null)
                worldMapController = FindAnyObjectByType<WorldMapController>();
            if (worldMapController != null)
                worldMapController.RefreshDateDisplays();
        }

        private void OnEnable()
        {
            if (advanceButton != null)
                advanceButton.onClick.AddListener(OnAdvanceClicked);
            if (endMonthButton != null)
            {
                endMonthButton.onClick.RemoveListener(OnEndMonthClicked);
                endMonthButton.onClick.AddListener(OnEndMonthClicked);
            }
            SubscribeSession();
        }

        private void OnDisable()
        {
            if (advanceButton != null)
                advanceButton.onClick.RemoveListener(OnAdvanceClicked);
            if (endMonthButton != null)
                endMonthButton.onClick.RemoveListener(OnEndMonthClicked);
            UnsubscribeSession();
        }

        private void SubscribeSession()
        {
            if (_sessionSubscribed || gameSession == null)
                return;
            gameSession.SessionStateChanged += Refresh;
            _sessionSubscribed = true;
        }

        private void UnsubscribeSession()
        {
            if (!_sessionSubscribed || gameSession == null)
            {
                _sessionSubscribed = false;
                return;
            }
            gameSession.SessionStateChanged -= Refresh;
            _sessionSubscribed = false;
        }
    }
}
