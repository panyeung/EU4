using System;
using System.Collections.Generic;
using UnityEngine;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Canvas 直接子级的唯一层序定义。每个生成器都向这张表要层，不再各自算插入位置：
    /// 地图/城镇 HUD &lt; 页面 &lt; 决策弹窗 &lt; 标题覆盖层 &lt; 事件选择覆盖层 &lt; 战术覆盖层。
    /// 页面带内还有 m08 引入的细分次序：M02 页面 &lt; M03 只读查看器 &lt; M08 战役回顾。
    /// 层号只在表内比较，外部只应使用 <see cref="LayerOf"/>、<see cref="InsertIndex"/> 与
    /// <see cref="TryFindViolation"/>，不要自己写 sibling index。
    /// 覆盖层带（七项）共用 <see cref="RankOf"/> 与 <see cref="PlaceAtRank"/>，避免生成器
    /// 相互覆盖彼此保存的座次。
    /// </summary>
    public static class CanvasLayerOrder
    {
        public enum CanvasLayer
        {
            /// <summary>地图与城镇 HUD：底层可点击控件，任何页面都要盖住它。</summary>
            MapHud = 0,

            /// <summary>不透明整页：家族名册、编年、家业、教育、研究、家规等。</summary>
            Page = 10,

            /// <summary>决策弹窗：继承与无继承人危机，必须盖住页面才能被点到。</summary>
            Modal = 20,

            /// <summary>全屏标题覆盖层。</summary>
            TitleOverlay = 30,

            /// <summary>全屏事件选择覆盖层。</summary>
            EventChoiceOverlay = 40,

            /// <summary>M06 战术任务覆盖层：全屏覆盖层里最高的一层，必须是最后一个 Canvas 子级。</summary>
            TacticalOverlay = 50
        }

        public const string DailyActionRootName = AddM02DailyActionPanel.RootName;
        public const string InspectorRootName = AddM03NpcAiInspector.RootName;
        public const string CampaignReviewRootName = CampaignReviewPanel.RootObjectName;
        public const string SuccessionRootName = AddSC010SuccessionPanel.RootName;
        public const string TitleRootName = AddM6TitleScreen.RootName;
        public const string EventChoiceRootName = AddM6TitleScreen.EventChoiceRootName;
        public const string TacticalOverlayName = TacticalMissionPanel.OverlayObjectName;

        // 页面带内的细分次序：三者对外都是 Page 层，但彼此的先后必须稳定，
        // 否则后跑的生成器会把 M08 回顾面板埋到 M02 页面之下。
        private const int DailyActionRank = 11;
        private const int InspectorRank = 12;
        private const int CampaignReviewRank = 13;

        private static readonly Dictionary<string, int> Ranks =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                // 地图与城镇 HUD。
                { "WorldClockPanel", (int)CanvasLayer.MapHud },
                { "M2WorldMapRoot", (int)CanvasLayer.MapHud },
                { "TownNpcDialoguePanelHost", (int)CanvasLayer.MapHud },
                { "TownNpcRosterRoot", (int)CanvasLayer.MapHud },
                { "TownNpcDialogueRoot", (int)CanvasLayer.MapHud },
                { "TownNpcButtonTemplates", (int)CanvasLayer.MapHud },

                // 页面。
                { "FamilyRosterRoot", (int)CanvasLayer.Page },
                { "FamilyChronicleRoot", (int)CanvasLayer.Page },
                { "IndustryRoot", (int)CanvasLayer.Page },
                { "SC020EducationRoot", (int)CanvasLayer.Page },
                { "SC021ResearchRoot", (int)CanvasLayer.Page },
                { "SC022FamilyFormationRoot", (int)CanvasLayer.Page },
                { "FamilyDoctrineRoot", (int)CanvasLayer.Page },
                { "FamilyDoctrineOpenButton", (int)CanvasLayer.Page },
                { DailyActionRootName, DailyActionRank },
                { InspectorRootName, InspectorRank },
                { CampaignReviewRootName, CampaignReviewRank },

                // 弹窗。
                { SuccessionRootName, (int)CanvasLayer.Modal },

                // 全屏覆盖层。
                { TitleRootName, (int)CanvasLayer.TitleOverlay },
                { EventChoiceRootName, (int)CanvasLayer.EventChoiceOverlay },
                { TacticalOverlayName, (int)CanvasLayer.TacticalOverlay }
            };

        /// <summary>
        /// 覆盖层带的七项，按渲染次序从低到高。表中的层号必须同向递增；
        /// <see cref="RankOf"/> 返回这张表里的索引，供生成器在不移动其它根节点的前提下摆位。
        /// </summary>
        private static readonly string[] RankedRoots =
        {
            DailyActionRootName,
            InspectorRootName,
            CampaignReviewRootName,
            SuccessionRootName,
            TitleRootName,
            EventChoiceRootName,
            TacticalOverlayName
        };

        /// <summary>共享错误：两个带内根节点保存成了错误的深度次序。</summary>
        public const string RankOrderError =
            "Canvas 覆盖层根节点必须遵循共享深度次序：" + DailyActionRootName +
            "、" + InspectorRootName + "、" + CampaignReviewRootName + "、" +
            SuccessionRootName + "、" + TitleRootName + "、" + EventChoiceRootName + "、" +
            TacticalOverlayName + "。";

        /// <summary>共享错误：战役回顾与 M02 页面、M03 只读查看器的相对次序。</summary>
        public const string CampaignReviewAboveDailyActionError =
            "战役回顾面板必须渲染在 M02 日常行动页（" + DailyActionRootName + "）之上。";
        public const string CampaignReviewAboveInspectorError =
            "战役回顾面板必须渲染在 M03 只读查看器（" + InspectorRootName + "）之上。";

        /// <summary>
        /// 查层序。表里没有的名字（例如运行时才出现的覆盖层）返回 null，代表不参与座次约束。
        /// </summary>
        public static CanvasLayer? LayerOf(string childName)
        {
            if (string.IsNullOrEmpty(childName))
                return null;
            return Ranks.TryGetValue(childName, out int rank) ? BucketOf(rank) : (CanvasLayer?)null;
        }

        /// <summary>层号归入对外公开的五个层，页面带内的细分次序对外仍是 Page。</summary>
        private static CanvasLayer BucketOf(int rank)
        {
            if (rank >= (int)CanvasLayer.TacticalOverlay)
                return CanvasLayer.TacticalOverlay;
            if (rank >= (int)CanvasLayer.EventChoiceOverlay)
                return CanvasLayer.EventChoiceOverlay;
            if (rank >= (int)CanvasLayer.TitleOverlay)
                return CanvasLayer.TitleOverlay;
            if (rank >= (int)CanvasLayer.Modal)
                return CanvasLayer.Modal;
            if (rank >= (int)CanvasLayer.Page)
                return CanvasLayer.Page;
            return CanvasLayer.MapHud;
        }

        private static int RankInTable(string childName) =>
            Ranks.TryGetValue(childName, out int rank) ? rank : -1;

        /// <summary>
        /// 按层算插入索引。<paramref name="siblingNames"/> 是目标节点之外的 Canvas 直接子级名，
        /// 按当前座次排列；返回的索引直接交给 <c>SetSiblingIndex</c>，因为 Unity 先摘掉自身
        /// 再按新序列插入，所以索引必须按「其它兄弟」算。
        /// 规则：插到第一个更高层兄弟之前；没有更高层兄弟时放到末端（最高）。
        /// 输入本身层序自相矛盾时（更低层的兄弟排在更高层之后），以不盖住更高覆盖层为先，
        /// 仍然返回第一个更高层兄弟的索引。
        /// </summary>
        public static int InsertIndex(IReadOnlyList<string> siblingNames, CanvasLayer layer)
        {
            if (siblingNames == null)
                throw new ArgumentNullException(nameof(siblingNames));

            int rank = (int)layer;
            for (int i = 0; i < siblingNames.Count; i++)
            {
                int other = RankInTable(siblingNames[i]);
                if (other >= 0 && other > rank)
                    return i;
            }
            return siblingNames.Count;
        }

        /// <summary>
        /// 找出第一个破坏层序的兄弟。<paramref name="siblingNames"/> 含目标节点本身，
        /// <paramref name="subjectIndex"/> 是它的当前 sibling index。
        /// 目标层更低层的兄弟排在它之后，说明目标压在了更低层之上，必须上移；
        /// 更高层的兄弟排在它之前，说明目标盖住了覆盖层，必须下移。
        /// </summary>
        public static bool TryFindViolation(IReadOnlyList<string> siblingNames, int subjectIndex,
            CanvasLayer layer, out string offenderName, out CanvasLayer offenderLayer,
            out bool mustRenderAbove)
        {
            offenderName = null;
            offenderLayer = layer;
            mustRenderAbove = false;
            if (siblingNames == null)
                throw new ArgumentNullException(nameof(siblingNames));

            int rank = (int)layer;
            for (int i = 0; i < siblingNames.Count; i++)
            {
                if (i == subjectIndex)
                    continue;
                int other = RankInTable(siblingNames[i]);
                if (other < 0)
                    continue;
                if (other < rank && i > subjectIndex)
                {
                    offenderName = siblingNames[i];
                    offenderLayer = BucketOf(other);
                    mustRenderAbove = true;
                    return true;
                }
                if (other > rank && i < subjectIndex)
                {
                    offenderName = siblingNames[i];
                    offenderLayer = BucketOf(other);
                    mustRenderAbove = false;
                    return true;
                }
            }
            return false;
        }

        /// <summary>层的说明片段，只用于拼断言消息；与既有 Validator 中文消息保持一致。</summary>
        public static string Describe(CanvasLayer layer)
        {
            switch (layer)
            {
                case CanvasLayer.MapHud: return "地图/城镇 HUD 层";
                case CanvasLayer.Page: return "不透明页面层";
                case CanvasLayer.Modal: return "决策弹窗层";
                case CanvasLayer.TitleOverlay: return "标题覆盖层";
                case CanvasLayer.EventChoiceOverlay: return "事件选择覆盖层";
                case CanvasLayer.TacticalOverlay: return "战术覆盖层";
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }

        /// <summary>覆盖层带内的座次，-1 表示不在带内。</summary>
        public static int RankOf(string name)
        {
            for (int i = 0; i < RankedRoots.Length; i++)
                if (RankedRoots[i] == name)
                    return i;
            return -1;
        }

        public static bool IsRanked(Transform child) =>
            child != null && RankOf(child.name) >= 0;

        /// <summary>
        /// 把一个带内根节点放到它的座次上，其它子级保持原位。
        /// 这样移动战役回顾或 M03 查看器时不会挪动标题、战术覆盖层或周围根节点的 Undo 记录。
        /// </summary>
        public static void PlaceAtRank(Transform canvas, Transform target)
        {
            if (canvas == null || target == null || target.parent != canvas)
                return;
            int rank = RankOf(target.name);
            if (rank < 0)
                return;

            var others = new List<Transform>(canvas.childCount);
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (child != target)
                    others.Add(child);
            }

            // 插到第一个渲染在它之上的根节点之前；它是带内最高项时追加到末尾。
            // Transform.SetSiblingIndex 接收的是摘掉自身后的子级列表索引，正是这个位置。
            int slot = others.Count;
            for (int i = 0; i < others.Count; i++)
            {
                if (RankOf(others[i].name) > rank)
                {
                    slot = i;
                    break;
                }
            }
            target.SetSiblingIndex(slot);
        }

        /// <summary>
        /// 把带内根节点按座次排好，未入带的根节点保持当前槽位。
        /// M02 生成器把 HUD 挪到页面之下后调用它，M08 修复路径也用它修正老版本保存的次序。
        /// </summary>
        public static void OrderRankedBand(Transform canvas)
        {
            if (canvas == null)
                return;
            int count = canvas.childCount;
            if (count < 2)
                return;

            var desired = new List<Transform>(count);
            var ranked = new List<Transform>(RankedRoots.Length);
            for (int i = 0; i < count; i++)
            {
                Transform child = canvas.GetChild(i);
                desired.Add(child);
                if (IsRanked(child))
                    ranked.Add(child);
            }
            if (ranked.Count < 2)
                return;

            ranked.Sort((left, right) => RankOf(left.name).CompareTo(RankOf(right.name)));
            int next = 0;
            for (int i = 0; i < desired.Count; i++)
                if (IsRanked(desired[i]))
                    desired[i] = ranked[next++];
            for (int i = 0; i < desired.Count; i++)
            {
                if (desired[i].GetSiblingIndex() == i)
                    continue;
                desired[i].SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// 把带内根节点按座次放到所有未入带根节点之后，并保持两组内部的相对次序。
        /// 这是 <c>AddM02DailyActionPanel.PlaceAboveGameHud</c> 产生的布局，也是场景 Validator
        /// 期望的布局：游戏 HUD 渲染在全屏覆盖层带之下，M02 页面仍然盖住它拥有的 HUD。
        /// 生成器在新建或替换旧页面后调用它；最小修复路径用 <see cref="RepairBand"/>。
        /// </summary>
        public static void PlaceBandAboveUnranked(Transform canvas)
        {
            if (canvas == null)
                return;
            var unranked = new List<Transform>(canvas.childCount);
            var ranked = new List<Transform>(canvas.childCount);
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (IsRanked(child))
                    ranked.Add(child);
                else
                    unranked.Add(child);
            }
            ranked.Sort((left, right) => RankOf(left.name).CompareTo(RankOf(right.name)));

            int next = 0;
            for (int i = 0; i < unranked.Count; i++)
            {
                if (unranked[i].GetSiblingIndex() == next)
                {
                    next++;
                    continue;
                }
                unranked[i].SetSiblingIndex(next++);
            }
            for (int i = 0; i < ranked.Count; i++)
            {
                if (ranked[i].GetSiblingIndex() == next)
                {
                    next++;
                    continue;
                }
                ranked[i].SetSiblingIndex(next++);
            }
        }

        /// <summary>
        /// 只把真正乱序的带内根节点放回座次。一个场景可能只有一项错位（老版本更新器把回顾
        /// 面板存到标题之上，或者继承面板早于共享带），其余都对；全部重排会把 M02 页面埋到
        /// 它必须盖住的 HUD 之下，也会挪动用户有意摆放的根节点。这里保留最长的已升序序列，
        /// 其余按座次插入，因此移动的根节点最少；未入带根节点永远不动。
        /// </summary>
        public static void RepairBand(Transform canvas)
        {
            if (canvas == null)
                return;
            var ranked = new List<Transform>(canvas.childCount);
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (IsRanked(child))
                    ranked.Add(child);
            }
            if (ranked.Count < 2)
                return;

            bool[] keep = LongestAscendingRanks(ranked);
            var moving = new List<Transform>(ranked.Count);
            for (int i = 0; i < ranked.Count; i++)
                if (!keep[i])
                    moving.Add(ranked[i]);
            moving.Sort((left, right) => RankOf(left.name).CompareTo(RankOf(right.name)));
            for (int i = 0; i < moving.Count; i++)
                PlaceAtRank(canvas, moving[i]);
        }

        /// <summary>
        /// 标记带内子级里最长的一段已升序座次，让 <see cref="RepairBand"/> 原样保留它们。
        /// </summary>
        private static bool[] LongestAscendingRanks(List<Transform> ranked)
        {
            int count = ranked.Count;
            var length = new int[count];
            var previous = new int[count];
            int best = 0;
            for (int i = 0; i < count; i++)
            {
                length[i] = 1;
                previous[i] = -1;
                int rank = RankOf(ranked[i].name);
                for (int j = 0; j < i; j++)
                {
                    if (RankOf(ranked[j].name) < rank && length[j] + 1 > length[i])
                    {
                        length[i] = length[j] + 1;
                        previous[i] = j;
                    }
                }
                if (length[i] > length[best])
                    best = i;
            }

            var keep = new bool[count];
            for (int index = best; index >= 0; index = previous[index])
                keep[index] = true;
            return keep;
        }

        /// <summary>带内根节点是否已按共享次序渲染。</summary>
        public static bool IsBandOrdered(Transform canvas)
        {
            if (canvas == null)
                return true;
            int previous = -1;
            for (int i = 0; i < canvas.childCount; i++)
            {
                int rank = RankOf(canvas.GetChild(i).name);
                if (rank < 0)
                    continue;
                if (rank < previous)
                    return false;
                previous = rank;
            }
            return true;
        }

        /// <summary>
        /// 报告第一对保存成错误深度次序的带内根节点。消息共享，场景 Validator 与两个面板的
        /// Validator 因此遵循同一契约。
        /// </summary>
        public static bool ValidateBand(Transform canvas, out string error)
        {
            error = null;
            if (canvas == null)
                return true;
            int previous = -1;
            string previousName = null;
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                int rank = RankOf(child.name);
                if (rank < 0)
                    continue;
                if (rank < previous)
                {
                    error = RankOrderError + " " + child.name + " 渲染在 " +
                            previousName + " 之上。";
                    return false;
                }
                previous = rank;
                previousName = child.name;
            }
            return true;
        }
    }
}
