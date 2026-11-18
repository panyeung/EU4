using System;
using System.Collections.Generic;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// Canvas 直接子级的唯一层序定义。每个生成器都向这张表要层，不再各自算插入位置：
    /// 地图/城镇 HUD &lt; 页面（家族名册等） &lt; 弹窗（继承、危机） &lt; 标题覆盖层 &lt; 事件选择覆盖层。
    /// 层号只在表内比较，外部只应使用 <see cref="LayerOf"/>、<see cref="InsertIndex"/> 与
    /// <see cref="TryFindViolation"/>，不要自己写 sibling index。
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
            EventChoiceOverlay = 40
        }

        private static readonly Dictionary<string, CanvasLayer> Layers =
            new Dictionary<string, CanvasLayer>(StringComparer.Ordinal)
            {
                // 地图与城镇 HUD。
                { "WorldClockPanel", CanvasLayer.MapHud },
                { "M2WorldMapRoot", CanvasLayer.MapHud },
                { "TownNpcDialoguePanelHost", CanvasLayer.MapHud },
                { "TownNpcRosterRoot", CanvasLayer.MapHud },
                { "TownNpcDialogueRoot", CanvasLayer.MapHud },
                { "TownNpcButtonTemplates", CanvasLayer.MapHud },

                // 页面。
                { "FamilyRosterRoot", CanvasLayer.Page },
                { "FamilyChronicleRoot", CanvasLayer.Page },
                { "IndustryRoot", CanvasLayer.Page },
                { "SC020EducationRoot", CanvasLayer.Page },
                { "SC021ResearchRoot", CanvasLayer.Page },
                { "SC022FamilyFormationRoot", CanvasLayer.Page },
                { "FamilyDoctrineRoot", CanvasLayer.Page },
                { "FamilyDoctrineOpenButton", CanvasLayer.Page },
                { "M02DailyActionRoot", CanvasLayer.Page },
                { "M03NpcAiInspectorRoot", CanvasLayer.Page },
                { "M08CampaignReviewRoot", CanvasLayer.Page },

                // 弹窗。
                { "SuccessionRoot", CanvasLayer.Modal },

                // 全屏覆盖层。
                { "M6TitleRoot", CanvasLayer.TitleOverlay },
                { "EventChoiceRoot", CanvasLayer.EventChoiceOverlay }
            };

        /// <summary>
        /// 查层序。表里没有的名字（例如运行时才出现的覆盖层）返回 null，代表不参与座次约束。
        /// </summary>
        public static CanvasLayer? LayerOf(string childName)
        {
            if (string.IsNullOrEmpty(childName))
                return null;
            return Layers.TryGetValue(childName, out CanvasLayer layer) ? layer : (CanvasLayer?)null;
        }

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

            for (int i = 0; i < siblingNames.Count; i++)
            {
                CanvasLayer? other = LayerOf(siblingNames[i]);
                if (other.HasValue && other.Value > layer)
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

            for (int i = 0; i < siblingNames.Count; i++)
            {
                if (i == subjectIndex)
                    continue;
                CanvasLayer? other = LayerOf(siblingNames[i]);
                if (!other.HasValue)
                    continue;
                if (other.Value < layer && i > subjectIndex)
                {
                    offenderName = siblingNames[i];
                    offenderLayer = other.Value;
                    mustRenderAbove = true;
                    return true;
                }
                if (other.Value > layer && i < subjectIndex)
                {
                    offenderName = siblingNames[i];
                    offenderLayer = other.Value;
                    mustRenderAbove = false;
                    return true;
                }
            }
            return false;
        }

        /// <summary>层的说明片段，只用于拼断言消息；与既有 Validator 英文消息保持一致。</summary>
        public static string Describe(CanvasLayer layer)
        {
            switch (layer)
            {
                case CanvasLayer.MapHud: return "map/town HUD layer";
                case CanvasLayer.Page: return "opaque page layer";
                case CanvasLayer.Modal: return "decision modal layer";
                case CanvasLayer.TitleOverlay: return "title overlay layer";
                case CanvasLayer.EventChoiceOverlay: return "event-choice overlay layer";
                default: throw new ArgumentOutOfRangeException(nameof(layer), layer, null);
            }
        }
    }
}
