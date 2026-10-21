using UnityEngine;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// 游戏会话 MonoBehaviour：M1 范围只初始化并持有纯 C# WorldClock 状态。
    /// 挂载在场景中的 GameObject 上，新局固定从 1560-04-01 开始。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameSession : MonoBehaviour
    {
        public WorldClock WorldClock { get; private set; }

        private void Awake()
        {
            if (WorldClock == null)
                InitNewGame();
        }

        /// <summary>以新局状态重置/初始化时钟（1560-04-01）。</summary>
        public void InitNewGame()
        {
            WorldClock = new WorldClock();
        }

        /// <summary>创建新游戏会话，日期固定为 1560-04-01。返回一个挂载在临时 GameObject 上的实例。</summary>
        public static GameSession NewGame()
        {
            var go = new GameObject("GameSession_NewGame");
            var session = go.AddComponent<GameSession>();
            session.InitNewGame();
            return session;
        }

        /// <summary>推进一天。成功返回 true；失败返回 false 且状态不变。</summary>
        public bool AdvanceOneDay()
        {
            if (WorldClock == null)
                InitNewGame();
            return WorldClock.AdvanceOneDay();
        }

        /// <summary>推进多天。count 必须为正整数；失败返回 false 且状态不变。</summary>
        public bool AdvanceDays(int count)
        {
            if (WorldClock == null)
                InitNewGame();
            return WorldClock.AdvanceDays(count);
        }
    }
}
