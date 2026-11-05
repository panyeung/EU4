using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// SC-022 pure kinship checks for marriage and ordinary adoption. Both helpers
    /// follow only directed parent/child and adoption edges and use a visited set so
    /// an existing malformed cycle in a loaded save can never hang a command.
    /// Sibling and spouse facts are deliberately ignored here: siblings may marry,
    /// and a spouse is rejected by the separate duplicate/spouse rule.
    /// </summary>
    public static class FamilyFormationRules
    {
        /// <summary>
        /// True when either person is an ancestor of the other through any number of
        /// biological or adoptive parent→child steps. A sibling relationship is not a
        /// direct-line relationship and therefore returns false.
        /// </summary>
        public static bool IsDirectLineRelation(KinshipGraph graph, PersonId left, PersonId right)
        {
            if (graph == null || left.IsEmpty || right.IsEmpty || left == right)
                return false;
            return ReachesThroughDirectedParentEdge(graph, left, right) ||
                   ReachesThroughDirectedParentEdge(graph, right, left);
        }

        /// <summary>
        /// True when appending adopter→candidate as an adoption edge would close a
        /// directed parent/child chain, i.e. the candidate is already an ancestor of
        /// the adopter. Empty or identical IDs are rejected by the caller, so this
        /// helper stays a safe negative for them.
        /// </summary>
        public static bool WouldCreateAdoptionCycle(KinshipGraph graph, PersonId adopter,
            PersonId candidate)
        {
            if (graph == null || adopter.IsEmpty || candidate.IsEmpty || adopter == candidate)
                return false;
            return ReachesThroughDirectedParentEdge(graph, candidate, adopter);
        }

        private static bool ReachesThroughDirectedParentEdge(KinshipGraph graph, PersonId from,
            PersonId target)
        {
            var visited = new HashSet<PersonId> { from };
            var queue = new Queue<PersonId>();
            queue.Enqueue(from);
            while (queue.Count > 0)
            {
                PersonId current = queue.Dequeue();
                foreach (KinshipLink link in graph.LinksFor(current))
                {
                    if (!IsDirectedParentKind(link.Type) || link.From != current)
                        continue;
                    if (link.To == target)
                        return true;
                    if (visited.Add(link.To))
                        queue.Enqueue(link.To);
                }
            }
            return false;
        }

        private static bool IsDirectedParentKind(KinshipType type) =>
            type == KinshipType.ParentChild || type == KinshipType.Adoption;
    }
}
