using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class FamilyTreeLayoutTests
    {
        private static readonly GameDate Date = new GameDate(1560, 4, 1);

        [Test]
        public void Calculate_LaysOutMoreThanSixMembersAndThreeGenerations()
        {
            FamilyTreeGraphSnapshot graph = BuildLargeGraph(out _);

            FamilyTreeLayoutResult layout = FamilyTreeLayout.Calculate(graph);

            Assert.That(graph.Count, Is.GreaterThan(6));
            Assert.That(layout.Count, Is.EqualTo(graph.Count));
            Assert.That(layout.HasSection(FamilyTreeLayoutSection.Generation), Is.True);
            Assert.That(layout.HasSection(FamilyTreeLayoutSection.UnconnectedKin), Is.True);
            Assert.That(layout.HasSection(FamilyTreeLayoutSection.Household), Is.True);

            int maximumGeneration = 0;
            bool sawUnconnected = false;
            bool sawHousehold = false;
            float kinBottom = 0f;
            float householdTop = float.MaxValue;
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                FamilyTreeNodePlacement node = layout.Nodes[i];
                Assert.That(float.IsNaN(node.Position.X), Is.False);
                Assert.That(float.IsNaN(node.Position.Y), Is.False);
                if (node.Section == FamilyTreeLayoutSection.Generation)
                {
                    maximumGeneration = System.Math.Max(maximumGeneration, node.Generation);
                    kinBottom = System.Math.Max(kinBottom,
                        node.Position.Y + node.Size.Height);
                }
                else if (node.Section == FamilyTreeLayoutSection.UnconnectedKin)
                {
                    sawUnconnected = true;
                }
                else
                {
                    sawHousehold = true;
                    householdTop = System.Math.Min(householdTop, node.Position.Y);
                }
                Assert.That(node.Position.X, Is.GreaterThanOrEqualTo(0f));
                Assert.That(node.Position.Y, Is.GreaterThanOrEqualTo(0f));
            }

            Assert.That(maximumGeneration, Is.GreaterThanOrEqualTo(4),
                "layout must derive more than three generations from kinship data");
            Assert.That(sawUnconnected, Is.True);
            Assert.That(sawHousehold, Is.True);
            Assert.That(householdTop, Is.GreaterThan(kinBottom),
                "household members must sit in their own band below all kin");
            Assert.That(layout.ContentSize.Height, Is.GreaterThan(0f));
            Assert.That(layout.ContentSize.Width, Is.GreaterThanOrEqualTo(
                FamilyTreeLayout.MinimumContentWidth));
            Assert.That(layout.ContentSize.Height,
                Is.GreaterThanOrEqualTo(householdTop));
        }

        [Test]
        public void Calculate_IsDeterministicWhenInputOrderChanges()
        {
            FamilyTreeGraphSnapshot graph = BuildLargeGraph(out _);
            var reversedPersons = new List<FamilyTreePersonNode>(graph.Persons);
            reversedPersons.Reverse();
            var reversedRelations = new List<FamilyTreeRelationEdge>(graph.Relations);
            reversedRelations.Reverse();
            var reversed = new FamilyTreeGraphSnapshot(graph.FamilyName, reversedPersons,
                reversedRelations);

            FamilyTreeLayoutResult first = FamilyTreeLayout.Calculate(graph);
            FamilyTreeLayoutResult second = FamilyTreeLayout.Calculate(reversed);

            Assert.That(second.Count, Is.EqualTo(first.Count));
            Assert.That(second.ContentSize.Width, Is.EqualTo(first.ContentSize.Width));
            Assert.That(second.ContentSize.Height, Is.EqualTo(first.ContentSize.Height));
            for (int i = 0; i < first.Nodes.Count; i++)
            {
                Assert.That(second.TryGetNode(first.Nodes[i].PersonId, out var reordered), Is.True);
                Assert.That(reordered.Position.X, Is.EqualTo(first.Nodes[i].Position.X));
                Assert.That(reordered.Position.Y, Is.EqualTo(first.Nodes[i].Position.Y));
                Assert.That(reordered.Generation, Is.EqualTo(first.Nodes[i].Generation));
                Assert.That(reordered.Section, Is.EqualTo(first.Nodes[i].Section));
            }
        }

        [Test]
        public void Calculate_PreservesCollateralSpouseAndAdoptionEdges()
        {
            FamilyTreeGraphSnapshot graph = BuildLargeGraph(out LargeFamily ids);

            Assert.That(HasEdge(graph, ids.CollateralId, ids.CollateralChildId,
                KinshipType.ParentChild), Is.True,
                "collateral branch parent/child edge must remain in the graph");
            Assert.That(HasEdge(graph, ids.ChildAId, ids.ChildASpouseId,
                KinshipType.Spouse), Is.True);
            Assert.That(HasEdge(graph, ids.HeadId, ids.AdoptedId,
                KinshipType.Adoption), Is.True);
            Assert.That(HasEdge(graph, ids.Grandchild1Id, ids.Grandchild1SiblingId,
                KinshipType.Sibling), Is.True);

            FamilyTreeLayoutResult layout = FamilyTreeLayout.Calculate(graph);
            Assert.That(layout.TryGetNode(ids.CollateralId, out _), Is.True);
            Assert.That(layout.TryGetNode(ids.CollateralChildId, out _), Is.True);
            Assert.That(layout.TryGetNode(ids.AdoptedId, out _), Is.True);
            Assert.That(layout.TryGetNode(ids.ChildASpouseId, out _), Is.True);
            Assert.That(layout.TryGetNode(ids.Grandchild1SiblingId, out _), Is.True);
        }

        [Test]
        public void Calculate_ReturnsUniquePositionsAndFiniteBounds()
        {
            FamilyTreeGraphSnapshot graph = BuildLargeGraph(out _);

            FamilyTreeLayoutResult layout = FamilyTreeLayout.Calculate(graph);

            var positions = new HashSet<string>();
            float maximumRight = 0f;
            float maximumBottom = 0f;
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                FamilyTreeNodePlacement node = layout.Nodes[i];
                string key = node.Position.X.ToString("R") + ":" + node.Position.Y.ToString("R");
                Assert.That(positions.Add(key), Is.True,
                    "two cards must not share the same position: " + key);
                Assert.That(node.Size.Width, Is.GreaterThan(0f));
                Assert.That(node.Size.Height, Is.GreaterThan(0f));
                Assert.That(float.IsInfinity(node.Position.X), Is.False);
                Assert.That(float.IsInfinity(node.Position.Y), Is.False);
                maximumRight = System.Math.Max(maximumRight,
                    node.Position.X + node.Size.Width);
                maximumBottom = System.Math.Max(maximumBottom,
                    node.Position.Y + node.Size.Height);
            }

            Assert.That(layout.ContentSize.Width, Is.GreaterThanOrEqualTo(maximumRight));
            Assert.That(layout.ContentSize.Height, Is.GreaterThanOrEqualTo(maximumBottom));
            Assert.That(float.IsInfinity(layout.ContentSize.Width), Is.False);
            Assert.That(float.IsInfinity(layout.ContentSize.Height), Is.False);
            for (int i = 0; i < layout.Sections.Count; i++)
            {
                Assert.That(layout.Sections[i].Position.Y, Is.GreaterThanOrEqualTo(0f));
                Assert.That(layout.Sections[i].Size.Width, Is.GreaterThan(0f));
            }
        }

        [Test]
        public void Calculate_EmptyGraphReturnsFiniteEmptyLayout()
        {
            var empty = new FamilyTreeGraphSnapshot("空家", null, null);

            FamilyTreeLayoutResult layout = FamilyTreeLayout.Calculate(empty);

            Assert.That(layout.Count, Is.Zero);
            Assert.That(layout.Sections.Count, Is.Zero);
            Assert.That(layout.ContentSize.Width, Is.EqualTo(FamilyTreeLayout.MinimumContentWidth));
            Assert.That(layout.ContentSize.Height, Is.EqualTo(0f));
            Assert.That(FamilyTreeLayout.Calculate(null).Count, Is.Zero);
        }

        [Test]
        public void SpouseRoute_AvoidsInterveningPersonCard()
        {
            PersonId leftId = new PersonId("person.route.left");
            PersonId middleId = new PersonId("person.route.middle");
            PersonId rightId = new PersonId("person.route.right");
            const float width = FamilyTreeLayout.NodeWidth;
            const float height = FamilyTreeLayout.NodeHeight;
            const float gap = FamilyTreeLayout.HorizontalGap;
            var left = new FamilyTreeNodePlacement(leftId, new FamilyTreePoint(0f, 0f),
                new FamilyTreeSize(width, height), 1, FamilyTreeLayoutSection.Generation);
            var middle = new FamilyTreeNodePlacement(middleId,
                new FamilyTreePoint(width + gap, 0f), new FamilyTreeSize(width, height), 1,
                FamilyTreeLayoutSection.Generation);
            var right = new FamilyTreeNodePlacement(rightId,
                new FamilyTreePoint((width + gap) * 2f, 0f),
                new FamilyTreeSize(width, height), 1, FamilyTreeLayoutSection.Generation);
            var placements = new[] { left, middle, right };

            IReadOnlyList<FamilyTreePoint> path = FamilyTreeRouting.BuildPath(
                KinshipType.Spouse, left, right, placements);

            Assert.That(path.Count, Is.GreaterThan(2),
                "a clear direct line would only have two points");
            for (int i = 0; i + 1 < path.Count; i++)
            {
                Assert.That(FamilyTreeRouting.SegmentIntersectsCard(path[i], path[i + 1], middle),
                    Is.False, "spouse connector must not cross the intervening person card");
                Assert.That(FamilyTreeRouting.SegmentIntersectsCard(path[i], path[i + 1], left),
                    Is.False, "spouse connector must not cross its own card");
                Assert.That(FamilyTreeRouting.SegmentIntersectsCard(path[i], path[i + 1], right),
                    Is.False, "spouse connector must not cross its own card");
            }
        }

        [Test]
        public void SpouseRoute_KeepsDirectLineWhenNoCardIntervenes()
        {
            PersonId leftId = new PersonId("person.route.left");
            PersonId rightId = new PersonId("person.route.right");
            const float width = FamilyTreeLayout.NodeWidth;
            const float height = FamilyTreeLayout.NodeHeight;
            const float gap = FamilyTreeLayout.HorizontalGap;
            var left = new FamilyTreeNodePlacement(leftId, new FamilyTreePoint(0f, 0f),
                new FamilyTreeSize(width, height), 1, FamilyTreeLayoutSection.Generation);
            var right = new FamilyTreeNodePlacement(rightId,
                new FamilyTreePoint(width + gap, 0f), new FamilyTreeSize(width, height), 1,
                FamilyTreeLayoutSection.Generation);
            var placements = new[] { left, right };

            IReadOnlyList<FamilyTreePoint> path = FamilyTreeRouting.BuildPath(
                KinshipType.Spouse, left, right, placements);

            Assert.That(path.Count, Is.EqualTo(2));
            Assert.That(path[0].X, Is.EqualTo(width));
            Assert.That(path[1].X, Is.EqualTo(right.Position.X));
        }

        [Test]
        public void SpouseRoute_GuaranteedOuterRouteWhenAllStandardCandidatesAreBlocked()
        {
            PersonId leftId = new PersonId("person.route.crowded-left");
            PersonId rightId = new PersonId("person.route.crowded-right");
            PersonId middleId = new PersonId("person.route.crowded-middle");
            PersonId aboveId = new PersonId("person.route.crowded-above");
            PersonId belowId = new PersonId("person.route.crowded-below");
            const float width = FamilyTreeLayout.NodeWidth;
            const float height = FamilyTreeLayout.NodeHeight;
            const float gap = FamilyTreeLayout.HorizontalGap;
            var left = new FamilyTreeNodePlacement(leftId, new FamilyTreePoint(0f, 0f),
                new FamilyTreeSize(width, height), 1, FamilyTreeLayoutSection.Generation);
            var right = new FamilyTreeNodePlacement(rightId,
                new FamilyTreePoint((width + gap) * 2f, 0f),
                new FamilyTreeSize(width, height), 1, FamilyTreeLayoutSection.Generation);
            var middle = new FamilyTreeNodePlacement(middleId,
                new FamilyTreePoint(width + gap, 0f), new FamilyTreeSize(width, height), 1,
                FamilyTreeLayoutSection.Generation);
            var above = new FamilyTreeNodePlacement(aboveId,
                new FamilyTreePoint(width + gap, -321f),
                new FamilyTreeSize(width, 320f), 1, FamilyTreeLayoutSection.Generation);
            var below = new FamilyTreeNodePlacement(belowId,
                new FamilyTreePoint(width + gap, height + 1f),
                new FamilyTreeSize(width, 320f), 1, FamilyTreeLayoutSection.Generation);
            var placements = new[] { left, middle, right, above, below };

            List<List<FamilyTreePoint>> standard = StandardSpouseCandidates(left, right);
            Assert.That(standard.Count, Is.EqualTo(6),
                "the regression must exercise all six existing standard lane candidates");
            for (int i = 0; i < standard.Count; i++)
            {
                bool blocked = false;
                for (int card = 0; card < placements.Length && !blocked; card++)
                {
                    FamilyTreeNodePlacement placement = placements[card];
                    if (placement.PersonId == leftId || placement.PersonId == rightId)
                        continue;
                    blocked = PathIntersectsCard(standard[i], placement);
                }
                Assert.That(blocked, Is.True,
                    "standard spouse candidate " + i + " must be blocked by a card");
            }

            IReadOnlyList<FamilyTreePoint> path = FamilyTreeRouting.BuildPath(
                KinshipType.Spouse, left, right, placements);

            Assert.That(path.Count, Is.GreaterThan(2),
                "the crowded fallback must not be the old two-point direct line");
            Assert.That(path[0].X, Is.EqualTo(left.Position.X + left.Size.Width));
            Assert.That(path[0].Y, Is.EqualTo(left.Position.Y + left.Size.Height / 2f));
            Assert.That(path[path.Count - 1].X, Is.EqualTo(right.Position.X));
            Assert.That(path[path.Count - 1].Y,
                Is.EqualTo(right.Position.Y + right.Size.Height / 2f));

            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < placements.Length; i++)
            {
                minY = System.Math.Min(minY, placements[i].Position.Y);
                maxY = System.Math.Max(maxY,
                    placements[i].Position.Y + placements[i].Size.Height);
            }
            bool usesOuterCorridor = false;
            for (int i = 0; i < path.Count; i++)
                if (path[i].Y < minY || path[i].Y > maxY)
                    usesOuterCorridor = true;
            Assert.That(usesOuterCorridor, Is.True,
                "the guaranteed route must leave the complete card bounds");

            for (int card = 0; card < placements.Length; card++)
            {
                for (int i = 0; i + 1 < path.Count; i++)
                {
                    Assert.That(FamilyTreeRouting.SegmentIntersectsCard(path[i],
                            path[i + 1], placements[card]), Is.False,
                        "guaranteed spouse connector must not cross card " +
                        placements[card].PersonId.Value);
                }
            }
        }

        private static List<List<FamilyTreePoint>> StandardSpouseCandidates(
            FamilyTreeNodePlacement from, FamilyTreeNodePlacement to)
        {
            bool fromIsLeft = from.Position.X + from.Size.Width / 2f <=
                              to.Position.X + to.Size.Width / 2f;
            var start = new FamilyTreePoint(
                fromIsLeft ? from.Position.X + from.Size.Width : from.Position.X,
                from.Position.Y + from.Size.Height / 2f);
            var end = new FamilyTreePoint(
                fromIsLeft ? to.Position.X : to.Position.X + to.Size.Width,
                to.Position.Y + to.Size.Height / 2f);
            float below = System.Math.Max(from.Position.Y + from.Size.Height,
                to.Position.Y + to.Size.Height) + FamilyTreeRouting.CardClearance;
            float above = System.Math.Min(from.Position.Y, to.Position.Y) -
                          FamilyTreeRouting.CardClearance;
            float step = FamilyTreeLayout.NodeHeight + FamilyTreeLayout.VerticalGap;
            var lanes = new[]
            {
                below, above,
                below + step, above - step,
                below + step * 2f, above - step * 2f
            };
            float stub = FamilyTreeRouting.CardClearance / 2f;
            float fromLaneX = fromIsLeft
                ? from.Position.X + from.Size.Width + stub
                : from.Position.X - stub;
            float toLaneX = fromIsLeft
                ? to.Position.X - stub
                : to.Position.X + to.Size.Width + stub;
            var candidates = new List<List<FamilyTreePoint>>(lanes.Length);
            for (int i = 0; i < lanes.Length; i++)
            {
                candidates.Add(new List<FamilyTreePoint>(6)
                {
                    start,
                    new FamilyTreePoint(fromLaneX, start.Y),
                    new FamilyTreePoint(fromLaneX, lanes[i]),
                    new FamilyTreePoint(toLaneX, lanes[i]),
                    new FamilyTreePoint(toLaneX, end.Y),
                    end
                });
            }
            return candidates;
        }

        private static bool PathIntersectsCard(IReadOnlyList<FamilyTreePoint> path,
            FamilyTreeNodePlacement card)
        {
            for (int i = 0; i + 1 < path.Count; i++)
                if (FamilyTreeRouting.SegmentIntersectsCard(path[i], path[i + 1], card))
                    return true;
            return false;
        }

        private static bool HasEdge(FamilyTreeGraphSnapshot graph, PersonId from, PersonId to,
            KinshipType type)
        {
            for (int i = 0; i < graph.Relations.Count; i++)
            {
                FamilyTreeRelationEdge edge = graph.Relations[i];
                if (edge.From == from && edge.To == to && edge.Type == type)
                    return true;
            }
            return false;
        }

        private sealed class LargeFamily
        {
            public PersonId HeadId;
            public PersonId SpouseId;
            public PersonId CollateralId;
            public PersonId CollateralChildId;
            public PersonId ChildAId;
            public PersonId ChildASpouseId;
            public PersonId AdoptedId;
            public PersonId Grandchild1Id;
            public PersonId Grandchild1SiblingId;
            public PersonId GreatGrandchildId;
            public PersonId UnconnectedId;
            public PersonId RetainerId;
            public PersonId DiscipleId;
            public PersonId GuestId;
        }

        private static FamilyTreeGraphSnapshot BuildLargeGraph(out LargeFamily ids)
        {
            ids = new LargeFamily();
            var world = new SuccessionWorld(new FamilyId("family.layout"), "布局测试家");
            var state = new SuccessionState(world);

            ids.HeadId = Add(world, "person.l.head", "初代当主", 1480, FamilyMemberRole.Head);
            ids.SpouseId = Add(world, "person.l.spouse", "初代配偶", 1482,
                FamilyMemberRole.Spouse);
            ids.CollateralId = Add(world, "person.l.collateral", "旁系叔父", 1484,
                FamilyMemberRole.CollateralKin);
            ids.CollateralChildId = Add(world, "person.l.collateral-child", "旁系堂弟", 1510,
                FamilyMemberRole.CollateralKin);
            ids.ChildAId = Add(world, "person.l.child-a", "嫡子", 1508,
                FamilyMemberRole.DirectKin);
            ids.ChildASpouseId = Add(world, "person.l.child-a-spouse", "嫡子配偶", 1510,
                FamilyMemberRole.Spouse);
            ids.AdoptedId = Add(world, "person.l.adopted", "养子", 1512,
                FamilyMemberRole.AdoptedChild);
            ids.Grandchild1Id = Add(world, "person.l.grandchild-1", "长孙", 1534,
                FamilyMemberRole.DirectKin);
            ids.Grandchild1SiblingId = Add(world, "person.l.grandchild-1-sibling", "次孙", 1538,
                FamilyMemberRole.DirectKin);
            ids.GreatGrandchildId = Add(world, "person.l.great-grandchild", "曾孙", 1558,
                FamilyMemberRole.DirectKin);
            ids.UnconnectedId = Add(world, "person.l.unconnected", "无亲缘族亲", 1544,
                FamilyMemberRole.DirectKin);
            ids.RetainerId = Add(world, "person.l.retainer", "家臣", 1516,
                FamilyMemberRole.Retainer);
            ids.DiscipleId = Add(world, "person.l.disciple", "弟子", 1540,
                FamilyMemberRole.Disciple);
            ids.GuestId = Add(world, "person.l.guest", "客将", 1522, FamilyMemberRole.Guest);

            Link(world, ids.HeadId, ids.SpouseId, KinshipType.Spouse);
            Link(world, ids.HeadId, ids.CollateralId, KinshipType.Sibling);
            Link(world, ids.CollateralId, ids.CollateralChildId, KinshipType.ParentChild);
            Link(world, ids.HeadId, ids.ChildAId, KinshipType.ParentChild);
            Link(world, ids.SpouseId, ids.ChildAId, KinshipType.ParentChild);
            Link(world, ids.HeadId, ids.AdoptedId, KinshipType.Adoption);
            Link(world, ids.ChildAId, ids.ChildASpouseId, KinshipType.Spouse);
            Link(world, ids.ChildAId, ids.Grandchild1Id, KinshipType.ParentChild);
            Link(world, ids.ChildASpouseId, ids.Grandchild1Id, KinshipType.ParentChild);
            Link(world, ids.ChildAId, ids.Grandchild1SiblingId, KinshipType.ParentChild);
            Link(world, ids.Grandchild1Id, ids.Grandchild1SiblingId, KinshipType.Sibling);
            Link(world, ids.Grandchild1Id, ids.GreatGrandchildId, KinshipType.ParentChild);

            state.HeadPersonId = ids.HeadId;
            state.DesignatedHeirId = ids.ChildAId;
            return FamilyTreeGraphBuilder.Build(state, Date);
        }

        private static PersonId Add(SuccessionWorld world, string id, string name, int birthYear,
            FamilyMemberRole role)
        {
            var personId = new PersonId(id);
            Assert.That(world.TryAddPerson(new PersonState(personId, name),
                new SuccessionPersonLife(birthYear, true, false), out string personError), Is.True,
                personError);
            Assert.That(world.TryAddMember(personId, role, out string memberError), Is.True,
                memberError);
            return personId;
        }

        private static void Link(SuccessionWorld world, PersonId from, PersonId to,
            KinshipType type)
        {
            Assert.That(KinshipLink.TryCreate(from, to, type, out KinshipLink link,
                out string linkError), Is.True, linkError);
            Assert.That(world.TryAddKinship(link, out string addError), Is.True, addError);
        }
    }
}
