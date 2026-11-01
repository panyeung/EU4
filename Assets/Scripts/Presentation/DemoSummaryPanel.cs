using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Phase 1 演示收尾摘要：当会话中三项任务全部结算时，在地图上显示一行完成文案
    /// 与总功绩。完成判定本身是 GameSession.IsDemoComplete 的纯规则；本组件只做展示。
    /// 挂在始终活跃的 GameSession 对象上，Update 每帧调用幂等 Refresh（仅枚举比较）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DemoSummaryPanel : MonoBehaviour
    {
        public const string SummaryFormat = "Phase 1 演示完成。总功绩：{0}。";

        [SerializeField]
        private GameSession gameSession;

        [SerializeField]
        private Text summaryText;

        /// <summary>当前是否处于演示完成状态。</summary>
        public bool IsShowing => summaryText != null && summaryText.gameObject.activeSelf;

        public void Bind(GameSession session, Text text)
        {
            gameSession = session;
            summaryText = text;
            Refresh();
        }

        private void Start()
        {
            if (gameSession == null)
                gameSession = GetComponent<GameSession>() ?? FindAnyObjectByType<GameSession>();
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        /// <summary>幂等刷新：完成时显示摘要，未完成时保持隐藏。</summary>
        public void Refresh()
        {
            if (summaryText == null)
                return;

            bool complete = gameSession != null && gameSession.IsDemoComplete;
            summaryText.gameObject.SetActive(complete);
            if (complete)
                summaryText.text = string.Format(SummaryFormat, gameSession.Merit);
        }
    }
}
