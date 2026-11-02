using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class KinshipInterpersonalTests
    {
        private static readonly PersonId Parent = new PersonId("p.parent");
        private static readonly PersonId Child = new PersonId("p.child");
        private static readonly PersonId Other = new PersonId("p.other");

        [Test]
        public void KinshipLink_EndpointAndTypeValidation()
        {
            Assert.That(KinshipLink.TryCreate(Parent, Child, KinshipType.ParentChild, out KinshipLink link, out string error), Is.True, error);
            Assert.That(link.From, Is.EqualTo(Parent));
            Assert.That(link.To, Is.EqualTo(Child));
            Assert.That(link.Type, Is.EqualTo(KinshipType.ParentChild));

            Assert.That(KinshipLink.TryCreate(Parent, Parent, KinshipType.Sibling, out _, out error), Is.False, "Self-kinship must be rejected");
            Assert.That(error, Is.Not.Empty);
            Assert.That(KinshipLink.TryCreate(default, Child, KinshipType.Spouse, out _, out error), Is.False, "Empty endpoint must be rejected");
            Assert.That(KinshipLink.TryCreate(Parent, default, KinshipType.Spouse, out _, out error), Is.False);
            Assert.That(KinshipLink.TryCreate(Parent, Child, (KinshipType)99, out _, out error), Is.False, "Undefined kinship type must be rejected");
        }

        [Test]
        public void SymmetricAndDirectedDuplicateHandling()
        {
            var graph = new KinshipGraph();
            Assert.That(graph.TryAdd(new KinshipLink(Parent, Child, KinshipType.ParentChild), out string error), Is.True, error);
            Assert.That(graph.TryAdd(new KinshipLink(Parent, Child, KinshipType.ParentChild), out error), Is.False, "Exact duplicate rejected");
            Assert.That(graph.TryAdd(new KinshipLink(Child, Parent, KinshipType.ParentChild), out error), Is.True, error,
                "Directed kind: reversed endpoints is a separate claim; cycle policy belongs to later milestones");

            Assert.That(graph.TryAdd(new KinshipLink(Parent, Other, KinshipType.Spouse), out error), Is.True, error);
            Assert.That(graph.TryAdd(new KinshipLink(Other, Parent, KinshipType.Spouse), out error), Is.False, "Symmetric mirror is the same fact");
            Assert.That(graph.Contains(new KinshipLink(Other, Parent, KinshipType.Spouse)), Is.True);

            Assert.That(graph.TryAdd(new KinshipLink(Parent, Other, KinshipType.Sibling), out error), Is.True, error, "Same endpoints, different kind is a distinct fact");
            Assert.That(graph.Count, Is.EqualTo(4));
        }

        [Test]
        public void Graph_RejectsInvalidLinkWithoutMutation()
        {
            var graph = new KinshipGraph();
            Assert.That(graph.TryAdd(new KinshipLink(Parent, Parent, KinshipType.Adoption), out string error), Is.False);
            Assert.That(graph.TryAdd(new KinshipLink(default, Child, KinshipType.ParentChild), out error), Is.False);
            Assert.That(graph.Count, Is.Zero);
        }

        [Test]
        public void Graph_LinksForReturnsBothDirections()
        {
            var graph = new KinshipGraph();
            Assert.That(graph.TryAdd(new KinshipLink(Parent, Child, KinshipType.ParentChild), out string error), Is.True, error);
            Assert.That(graph.TryAdd(new KinshipLink(Parent, Other, KinshipType.Spouse), out error), Is.True, error);
            Assert.That(graph.TryAdd(new KinshipLink(Other, Child, KinshipType.Adoption), out error), Is.True, error);

            IReadOnlyList<KinshipLink> childLinks = graph.LinksFor(Child);
            Assert.That(childLinks.Count, Is.EqualTo(2));
            Assert.That(childLinks, Has.Some.Property("Type").EqualTo(KinshipType.ParentChild));
            Assert.That(childLinks, Has.Some.Property("Type").EqualTo(KinshipType.Adoption));
            Assert.That(graph.LinksFor(new PersonId("p.stranger")), Is.Empty);
        }

        [Test]
        public void KinshipGraph_HasNoRemovalOrMutationApi_StructuralFactsAreFixed()
        {
            string[] forbidden = { "Remove", "Clear", "Change", "Mutate", "Update" };
            IEnumerable<string> offending = typeof(KinshipGraph)
                .GetMethods()
                .Select(m => m.Name)
                .Where(name => forbidden.Any(f => name.Contains(f)));
            Assert.That(string.Join(",", offending), Is.Empty, "Structural kinship must be append-only (GDD 3.3 血缘关系属于结构性事实)");
        }

        [Test]
        public void InterpersonalAxes_AreIndependentValuesAndNotASingleNumber()
        {
            var state = new InterpersonalState();
            Assert.That(state.TryChange(Parent, Child, InterpersonalAxis.Affection, 30).Succeeded, Is.True);
            Assert.That(state.TryChange(Parent, Child, InterpersonalAxis.Trust, 20).Succeeded, Is.True);
            Assert.That(state.TryChange(Parent, Child, InterpersonalAxis.Obligation, -40).Succeeded, Is.True);

            Assert.That(state.TryGet(Parent, Child, InterpersonalAxis.Affection).CurrentValue, Is.EqualTo(30));
            Assert.That(state.TryGet(Parent, Child, InterpersonalAxis.Trust).CurrentValue, Is.EqualTo(20));
            Assert.That(state.TryGet(Parent, Child, InterpersonalAxis.Obligation).CurrentValue, Is.EqualTo(-40));

            InterpersonalResult undo = state.TryChange(Parent, Child, InterpersonalAxis.Affection, -30);
            Assert.That(undo.Succeeded, Is.True);
            Assert.That(undo.PreviousValue, Is.EqualTo(30));
            Assert.That(undo.CurrentValue, Is.Zero, "Axis changes must not leak into each other");
            Assert.That(state.TryGet(Parent, Child, InterpersonalAxis.Trust).CurrentValue, Is.EqualTo(20));
        }

        [Test]
        public void InterpersonalValues_ClampAndDefaultToZero()
        {
            var state = new InterpersonalState();
            InterpersonalResult absent = state.TryGet(Parent, Other, InterpersonalAxis.Affection);
            Assert.That(absent.Succeeded, Is.True, "Absent pairs are readable");
            Assert.That(absent.CurrentValue, Is.Zero);

            Assert.That(state.TryChange(Parent, Other, InterpersonalAxis.Trust, 90).Succeeded, Is.True);
            InterpersonalResult clamped = state.TryChange(Parent, Other, InterpersonalAxis.Trust, 50);
            Assert.That(clamped.PreviousValue, Is.EqualTo(90));
            Assert.That(clamped.CurrentValue, Is.EqualTo(InterpersonalState.MaximumValue));
            Assert.That(state.TryChange(Parent, Other, InterpersonalAxis.Trust, int.MinValue).CurrentValue, Is.EqualTo(InterpersonalState.MinimumValue));

            Assert.That(state.TryChange(Parent, Other, (InterpersonalAxis)77, 10).Succeeded, Is.False, "Undefined axis rejected");
            Assert.That(state.TryChange(Parent, Parent, InterpersonalAxis.Affection, 10).Succeeded, Is.False, "Self-state rejected");
            Assert.That(state.TryChange(default, Other, InterpersonalAxis.Affection, 10).Succeeded, Is.False, "Empty endpoint rejected");
        }

        [Test]
        public void InterpersonalState_IsDirected_AndSeparateFromKinship()
        {
            var graph = new KinshipGraph();
            Assert.That(graph.TryAdd(new KinshipLink(Parent, Child, KinshipType.ParentChild), out string error), Is.True, error);

            var state = new InterpersonalState();
            Assert.That(state.TryChange(Parent, Child, InterpersonalAxis.Affection, 40).Succeeded, Is.True);
            Assert.That(state.TryChange(Child, Parent, InterpersonalAxis.Affection, -10).Succeeded, Is.True);
            Assert.That(state.TryGet(Parent, Child, InterpersonalAxis.Affection).CurrentValue, Is.EqualTo(40));
            Assert.That(state.TryGet(Child, Parent, InterpersonalAxis.Affection).CurrentValue, Is.EqualTo(-10));

            IReadOnlyList<KinshipLink> parentLinks = graph.LinksFor(Parent);
            Assert.That(parentLinks.Count, Is.EqualTo(1), "Mutable sentiment must not alter structural facts");
            Assert.That(parentLinks[0].Type, Is.EqualTo(KinshipType.ParentChild));
        }

        [Test]
        public void Betrayal_IsOneShotMutableFlag_WithoutRewritingNumbersOrDirection()
        {
            var state = new InterpersonalState();
            Assert.That(state.TryChange(Parent, Child, InterpersonalAxis.Affection, 40).Succeeded, Is.True);

            Assert.That(state.TryRecordBetrayal(Child, Parent, out string error), Is.True, error);
            Assert.That(state.TryRecordBetrayal(Child, Parent, out error), Is.False, "One-shot per directed pair");
            Assert.That(state.IsBetrayalRecorded(Child, Parent), Is.True);
            Assert.That(state.IsBetrayalRecorded(Parent, Child), Is.False, "Betrayal direction is preserved");

            Assert.That(state.TryGet(Child, Parent, InterpersonalAxis.Obligation).CurrentValue, Is.Zero, "Flag does not rewrite numbers");
            Assert.That(state.TryGet(Parent, Child, InterpersonalAxis.Affection).CurrentValue, Is.EqualTo(40), "B→A betrayal does not alter A→B values");

            Assert.That(state.TryRecordBetrayal(Parent, Parent, out error), Is.False);
            Assert.That(state.TryRecordBetrayal(default, Child, out error), Is.False);

            InterpersonalResult changedAfterBetrayal = state.TryChange(Child, Parent, InterpersonalAxis.Obligation, -60);
            Assert.That(changedAfterBetrayal.Succeeded, Is.True);
            Assert.That(state.IsBetrayalRecorded(Child, Parent), Is.True, "Numeric change does not clear the betrayal flag");
        }

        [Test]
        public void InterpersonalState_HasNoKinshipMutationApi()
        {
            string[] forbidden = { "Kinship", "Parent", "Sibling", "Spouse", "Adopt" };
            IEnumerable<string> offending = typeof(InterpersonalState)
                .GetMethods()
                .Select(m => m.Name)
                .Where(name => forbidden.Any(f => name.Contains(f)));
            Assert.That(string.Join(",", offending), Is.Empty, "Mutable state must not touch structural kinds (GDD 3.3)");
        }

        [Test]
        public void Records_SnapshotExposesEveryDirectedPair()
        {
            var state = new InterpersonalState();
            Assert.That(state.TryChange(Parent, Child, InterpersonalAxis.Affection, 5).Succeeded, Is.True);
            Assert.That(state.TryChange(Child, Parent, InterpersonalAxis.Trust, 7).Succeeded, Is.True);
            Assert.That(state.RecordedPairCount, Is.EqualTo(2));

            IReadOnlyList<InterpersonalRecord> records = state.Records;
            Assert.That(records.Count, Is.EqualTo(2));
            Assert.That(records[0].From, Is.EqualTo(Child));
            Assert.That(records[0].To, Is.EqualTo(Parent));
            Assert.That(records[0].Trust, Is.EqualTo(7));
            Assert.That(records[1].From, Is.EqualTo(Parent));
            Assert.That(records[1].Obligation, Is.Zero);
        }
    }
}
