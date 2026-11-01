using UnityEngine;
using UnityEngine.UI;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 功名行：常驻显示总功绩、当前军阶与下一阶门槛（顶级显示「最高」）。
    /// 军阶规则全部来自 PromotionRules / GameSession 的纯 C# 派生属性；
    /// 本组件只做展示，与 DemoSummaryPanel 相同采用幂等 Refresh + Update 轮询。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RankPanel : MonoBehaviour
    {
        [SerializeField]
        private GameSession gameSession;

        [SerializeField]
        private Text rankText;

        /// <summary>当前显示的文本（测试与诊断用）。</summary>
        public string CurrentText => rankText != null ? rankText.text : string.Empty;

        public void Bind(GameSession session, Text text)
        {
            gameSession = session;
            rankText = text;
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

        /// <summary>幂等刷新；会话缺失时保持文本不变。</summary>
        public void Refresh()
        {
            if (rankText == null || gameSession == null)
                return;

            string message = "功名：" + gameSession.Merit + " · " + gameSession.RankName;
            if (PromotionRules.TryNextRank(gameSession.Rank, out PromotionRank next, out int needed))
                message += "（下一阶 " + PromotionRules.ChineseName(next) + " " + needed + "）";
            else
                message += "（最高）";

            rankText.text = message;
        }
    }
}
