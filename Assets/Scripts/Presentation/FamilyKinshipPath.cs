using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// 两个稳定人物 ID 之间的只读最短亲属路径。家族组建页用它
    /// 在玩家确认婚姻或收养之前显示已记录的关系路径。它只走
    /// <see cref="KinshipGraph"/> 中已有的事实，绝不新增、删除或推断关系。
    /// 亲子与收养事实带方向，因此渲染出的每一步都按实际记录的方向
    /// 标注，而不会暗示关系对称。
    /// </summary>
    public static class FamilyKinshipPath
    {
        /// <summary>更长的亲属链报告为未展开，而不是整段打印。</summary>
        public const int MaximumSteps = 6;
        public const string NoRecordedPathText = "未记录亲属关系";
        public const string SelfText = "本人";
        public const string TooFarText = "亲属链过长（超过 6 步），未展开";

        public static IReadOnlyList<KinshipLink> FindShortestPath(KinshipGraph graph, PersonId from,
            PersonId to)
        {
            if (graph == null || from.IsEmpty || to.IsEmpty || from == to)
                return Array.Empty<KinshipLink>();

            var visited = new HashSet<PersonId> { from };
            var previous = new Dictionary<PersonId, KinshipLink>();
            var queue = new Queue<PersonId>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                PersonId current = queue.Dequeue();
                foreach (KinshipLink link in SortedLinksFor(graph, current))
                {
                    PersonId next = link.From == current ? link.To : link.From;
                    if (next.IsEmpty || !visited.Add(next))
                        continue;
                    previous[next] = link;
                    if (next == to)
                        return BuildPath(previous, from, to);
                    queue.Enqueue(next);
                }
            }
            return Array.Empty<KinshipLink>();
        }

        /// <summary>
        /// 面向玩家的路径，形如 <c>晴信 —亲子·子→ 正綱 —亲子·子→ 信正</c>。
        /// 每一步标注下一个人被记录的角色，因此逆着有向事实走出的路径
        /// 读作 <c>信正 —亲子·父母→ 正綱</c>，而不会暗示 信正 是父母。
        /// 姓名来自调用方传入的解析器，显示命名仍由调用方决定。
        /// </summary>
        public static string Describe(KinshipGraph graph, PersonId from, PersonId to,
            Func<PersonId, string> nameOf)
        {
            if (from == to)
                return SelfText;
            IReadOnlyList<KinshipLink> path = FindShortestPath(graph, from, to);
            if (path.Count == 0)
                return NoRecordedPathText;
            if (path.Count > MaximumSteps)
                return TooFarText;

            var builder = new StringBuilder(128);
            builder.Append(NameOf(nameOf, from));
            PersonId current = from;
            for (int i = 0; i < path.Count; i++)
            {
                KinshipLink link = path[i];
                PersonId next = link.From == current ? link.To : link.From;
                builder.Append(" —").Append(KindName(link.Type)).Append('·')
                    .Append(StepRoleName(link, current)).Append("→ ")
                    .Append(NameOf(nameOf, next));
                current = next;
            }
            return builder.ToString();
        }

        /// <summary>已记录的亲属种类，不暗示方向。</summary>
        public static string KindName(KinshipType type)
        {
            switch (type)
            {
                case KinshipType.ParentChild: return "亲子";
                case KinshipType.Adoption: return "收养";
                case KinshipType.Sibling: return "手足";
                case KinshipType.Spouse: return "配偶";
                default: return type.ToString();
            }
        }

        /// <summary>
        /// 人物的一条事实行：主体是记录中的父母/收养人时为 <c>亲子（子女）：正綱</c>，
        /// 主体是记录中的子女时为 <c>亲子（父母）：晴信</c>。
        /// 手足与配偶事实不带方向。
        /// </summary>
        public static string FactLabel(KinshipLink link, PersonId subject)
        {
            string role = StepRoleName(link, subject);
            return string.IsNullOrEmpty(role)
                ? KindName(link.Type)
                : KindName(link.Type) + "（" + role + "）";
        }

        /// <summary>
        /// 另一个端点相对 <paramref name="subject"/> 是什么身份，取自该事实
        /// 记录的方向。对称种类返回空串。
        /// </summary>
        public static string StepRoleName(KinshipLink link, PersonId subject)
        {
            switch (link.Type)
            {
                case KinshipType.ParentChild:
                    return link.From == subject ? "子女" : "父母";
                case KinshipType.Adoption:
                    return link.From == subject ? "养子/养女" : "养父母";
                default:
                    return string.Empty;
            }
        }

        private static List<KinshipLink> SortedLinksFor(KinshipGraph graph, PersonId personId)
        {
            var links = new List<KinshipLink>();
            foreach (KinshipLink link in graph.LinksFor(personId))
                links.Add(link);
            links.Sort((left, right) =>
            {
                int type = ((int)left.Type).CompareTo((int)right.Type);
                if (type != 0)
                    return type;
                string leftOther = (left.From == personId ? left.To : left.From).Value ?? string.Empty;
                string rightOther = (right.From == personId ? right.To : right.From).Value ??
                                    string.Empty;
                return string.Compare(leftOther, rightOther, StringComparison.Ordinal);
            });
            return links;
        }

        private static IReadOnlyList<KinshipLink> BuildPath(
            Dictionary<PersonId, KinshipLink> previous, PersonId from, PersonId to)
        {
            var reversed = new List<KinshipLink>();
            PersonId current = to;
            while (current != from && previous.TryGetValue(current, out KinshipLink link))
            {
                reversed.Add(link);
                current = link.From == current ? link.To : link.From;
                if (reversed.Count > MaximumSteps + 1)
                    break;
            }
            reversed.Reverse();
            return reversed;
        }

        private static string NameOf(Func<PersonId, string> nameOf, PersonId personId)
        {
            string name = nameOf != null ? nameOf(personId) : null;
            return string.IsNullOrWhiteSpace(name) ? personId.Value ?? string.Empty : name;
        }
    }
}
