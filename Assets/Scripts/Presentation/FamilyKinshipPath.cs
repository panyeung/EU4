using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Presentation
{
    /// <summary>
    /// Read-only shortest kinship path between two stable person IDs. The family
    /// formation page uses it to show the recorded relationship path before the
    /// player confirms a marriage or an adoption. It only walks facts that already
    /// exist in <see cref="KinshipGraph"/> and never adds, removes or infers a link.
    /// Parent/child and adoption facts are directed, so every rendered step names the
    /// direction that was actually recorded instead of implying a symmetric relation.
    /// </summary>
    public static class FamilyKinshipPath
    {
        /// <summary>Longer chains are reported as not shown rather than printed in full.</summary>
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
        /// Player-facing path such as <c>晴信 —亲子·子→ 正綱 —亲子·子→ 信正</c>. Each step
        /// names the next person's recorded role, so a path walked against a directed
        /// fact reads <c>信正 —亲子·父母→ 正綱</c> instead of implying that 信正 is the
        /// parent. Names come from the supplied resolver so the caller keeps its own
        /// display naming.
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

        /// <summary>Recorded kinship kind, with no direction implied.</summary>
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
        /// One fact line for a person: <c>亲子（子女）：正綱</c> when the subject is the
        /// recorded parent/adopter, <c>亲子（父母）：晴信</c> when the subject is the
        /// recorded child. Sibling and spouse facts carry no direction.
        /// </summary>
        public static string FactLabel(KinshipLink link, PersonId subject)
        {
            string role = StepRoleName(link, subject);
            return string.IsNullOrEmpty(role)
                ? KindName(link.Type)
                : KindName(link.Type) + "（" + role + "）";
        }

        /// <summary>
        /// What the other endpoint is to <paramref name="subject"/>, read from the
        /// recorded direction of the fact. Empty for symmetric kinds.
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
