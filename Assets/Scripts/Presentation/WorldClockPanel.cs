using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 最小 uGUI 面板：显示当前日期，并提供「推进一天」按钮。
    /// M1 范围：仅负责呈现与输入绑定，不承载日期规则。
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

        /// <summary>Editor 脚本注入依赖（M1：仅 Editor 场景创建器使用）。</summary>
        public void SetDependencies(GameSession session, Button button, Text text)
        {
            gameSession = session;
            advanceButton = button;
            dateText = text;
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
            Refresh();
        }

        private void OnValidate()
        {
            Refresh();
        }

        private void OnAdvanceClicked()
        {
            if (gameSession == null)
            {
                Debug.LogWarning($"{nameof(WorldClockPanel)}: gameSession 为空，无法推进。");
                return;
            }

            bool ok = gameSession.AdvanceOneDay();
            if (ok)
                Refresh();
            else
                Debug.Log($"{nameof(WorldClockPanel)}: 日期已达支持上限，无法再推进。");
        }

        public void Refresh()
        {
            if (dateText == null || gameSession == null || gameSession.WorldClock == null)
                return;

            dateText.text = gameSession.WorldClock.CurrentDate.ToString();
        }

        private void OnEnable()
        {
            if (advanceButton != null)
                advanceButton.onClick.AddListener(OnAdvanceClicked);
        }

        private void OnDisable()
        {
            if (advanceButton != null)
                advanceButton.onClick.RemoveListener(OnAdvanceClicked);
        }
    }
}
