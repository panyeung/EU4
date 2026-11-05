using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-022 Task 1: direct-line and adoption-cycle graph rules.</summary>
    public sealed class FamilyFormationRulesTests
    {
        private static readonly PersonId A = new PersonId("person.test.a");
        private static readonly PersonId B = new PersonId("person.test.b");
        private static readonly PersonId C = new PersonId("person.test.c");
        private static readonly PersonId D = new PersonId("person.test.d");

        [Test]
        public void DirectLine_DetectsParentAndChildInBothDirections()
        {
            KinshipGraph graph = Graph();
            Add(graph, A, B, KinshipType.ParentChild);

            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, A, B), Is.True);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, B, A), Is.True);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, A, C), Is.False);
        }

        [Test]
        public void DirectLine_DetectsAdoptionAndMultiHopChains()
        {
            KinshipGraph graph = Graph();
            Add(graph, A, B, KinshipType.Adoption);
            Add(graph, B, C, KinshipType.ParentChild);

            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, A, B), Is.True);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, A, C), Is.True);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, C, A), Is.True);
        }

        [Test]
        public void DirectLine_DoesNotTreatSiblingsAsAncestors()
        {
            KinshipGraph graph = Graph();
            Add(graph, A, B, KinshipType.Sibling);

            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, A, B), Is.False);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, B, A), Is.False);
        }

        [Test]
        public void DirectLine_IsSafeForEmptyGraphsAndIds()
        {
            Assert.That(FamilyFormationRules.IsDirectLineRelation(null, A, B), Is.False);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(Graph(), default, B), Is.False);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(Graph(), A, default), Is.False);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(Graph(), A, A), Is.False);
        }

        [Test]
        public void AdoptionCycle_RejectsAnAncestorCandidateAndAllowsUnrelated()
        {
            KinshipGraph graph = Graph();
            Add(graph, B, A, KinshipType.ParentChild);

            Assert.That(FamilyFormationRules.WouldCreateAdoptionCycle(graph, A, B), Is.True);
            Assert.That(FamilyFormationRules.WouldCreateAdoptionCycle(graph, A, D), Is.False);
        }

        [Test]
        public void AdoptionCycle_RejectsMultiHopAdoptiveAncestor()
        {
            KinshipGraph graph = Graph();
            Add(graph, C, B, KinshipType.Adoption);
            Add(graph, B, A, KinshipType.ParentChild);

            Assert.That(FamilyFormationRules.WouldCreateAdoptionCycle(graph, A, C), Is.True);
            Assert.That(FamilyFormationRules.WouldCreateAdoptionCycle(graph, C, A), Is.False);
        }

        [Test]
        public void AdoptionCycle_DoesNotTreatSiblingsAsAncestors()
        {
            KinshipGraph graph = Graph();
            Add(graph, A, B, KinshipType.Sibling);

            Assert.That(FamilyFormationRules.WouldCreateAdoptionCycle(graph, A, B), Is.False);
        }

        [Test]
        [Timeout(2000)]
        public void ExistingMalformedCycle_TerminatesWithoutHanging()
        {
            KinshipGraph graph = Graph();
            Add(graph, A, B, KinshipType.ParentChild);
            Add(graph, B, C, KinshipType.ParentChild);
            Add(graph, C, A, KinshipType.Adoption);

            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, A, C), Is.True);
            Assert.That(FamilyFormationRules.IsDirectLineRelation(graph, A, D), Is.False);
            Assert.That(FamilyFormationRules.WouldCreateAdoptionCycle(graph, A, D), Is.False);
        }

        private static KinshipGraph Graph() => new KinshipGraph();

        private static void Add(KinshipGraph graph, PersonId from, PersonId to, KinshipType type)
        {
            Assert.That(KinshipLink.TryCreate(from, to, type, out KinshipLink link, out string error),
                Is.True, error);
            Assert.That(graph.TryAdd(link, out error), Is.True, error);
        }
    }
}
