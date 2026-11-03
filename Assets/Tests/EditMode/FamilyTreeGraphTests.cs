using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class FamilyTreeGraphTests
    {
        private static readonly GameDate Date = new GameDate(1560, 4, 1);

        [Test]
        public void Build_IncludesSc013MembersAndExactKinshipEdges()
        {
            SuccessionState state = M2FixedScenario.CreateSuccession();

            FamilyTreeGraphSnapshot graph = FamilyTreeGraphBuilder.Build(state, Date);

            Assert.That(graph.FamilyName, Is.EqualTo("明智家"));
            Assert.That(graph.Count, Is.EqualTo(6));
            Assert.That(graph.KinshipMembers.Count, Is.EqualTo(5));
            Assert.That(graph.HouseholdMembers.Count, Is.EqualTo(1));
            Assert.That(graph.UnconnectedKin.Count, Is.Zero);

            Assert.That(graph.TryGetPerson(M2FixedScenario.HarunobuId, out var harunobu), Is.True);
            Assert.That(harunobu.DisplayName, Is.EqualTo("明智 晴信"));
            Assert.That(harunobu.Age, Is.EqualTo(70));
            Assert.That(harunobu.HasAge, Is.True);
            Assert.That(harunobu.Role, Is.EqualTo(FamilyMemberRole.Head));
            Assert.That(harunobu.IsHead, Is.True);
            Assert.That(harunobu.IsKin, Is.True);

            Assert.That(graph.TryGetPerson(M2FixedScenario.MasatsunaId, out var masatsuna), Is.True);
            Assert.That(masatsuna.Age, Is.EqualTo(46));
            Assert.That(masatsuna.IsDesignatedHeir, Is.True);
            Assert.That(masatsuna.IsDesignatedHeirValid, Is.True);

            Assert.That(graph.Relations.Count, Is.EqualTo(5));
            Assert.That(HasEdge(graph, M2FixedScenario.HarunobuId, M2FixedScenario.ChieId,
                KinshipType.Spouse), Is.True);
            Assert.That(HasEdge(graph, M2FixedScenario.HarunobuId, M2FixedScenario.MasatsunaId,
                KinshipType.ParentChild), Is.True);
            Assert.That(HasEdge(graph, M2FixedScenario.ChieId, M2FixedScenario.MasatsunaId,
                KinshipType.ParentChild), Is.True);
            Assert.That(HasEdge(graph, M2FixedScenario.MasatsunaId, M2FixedScenario.NobumasaId,
                KinshipType.ParentChild), Is.True);
            Assert.That(HasEdge(graph, M2FixedScenario.HarunobuId, M2FixedScenario.TokihimeId,
                KinshipType.Sibling), Is.True);
        }

        [Test]
        public void Build_KeepsUnconnectedKinVisible()
        {
            SuccessionState state = CreateState(out SuccessionWorld world);
            PersonId head = Add(world, "person.head", "当主", 1500, FamilyMemberRole.Head);
            PersonId child = Add(world, "person.child", "嫡子", 1530, FamilyMemberRole.DirectKin);
            PersonId lonely = Add(world, "person.lonely", "无亲缘者", 1540, FamilyMemberRole.DirectKin);
            Link(world, head, child, KinshipType.ParentChild);
            state.HeadPersonId = head;
            state.DesignatedHeirId = child;

            FamilyTreeGraphSnapshot graph = FamilyTreeGraphBuilder.Build(state, Date);

            Assert.That(graph.Count, Is.EqualTo(3));
            Assert.That(graph.TryGetPerson(lonely, out var lonelyNode), Is.True);
            Assert.That(lonelyNode.Section, Is.EqualTo(FamilyTreeMemberSection.UnconnectedKin));
            Assert.That(lonelyNode.IsUnconnectedKin, Is.True);
            Assert.That(lonelyNode.IsHousehold, Is.False);
            Assert.That(graph.UnconnectedKin.Count, Is.EqualTo(1));
            Assert.That(graph.KinshipMembers.Count, Is.EqualTo(2));
            Assert.That(graph.Relations.Count, Is.EqualTo(1));
        }

        [Test]
        public void Build_SeparatesRetainersDisciplesAndGuestsFromKinshipEdges()
        {
            SuccessionState state = CreateState(out SuccessionWorld world);
            PersonId head = Add(world, "person.head", "当主", 1500, FamilyMemberRole.Head);
            PersonId child = Add(world, "person.child", "嫡子", 1530, FamilyMemberRole.DirectKin);
            PersonId retainer = Add(world, "person.retainer", "家臣", 1510, FamilyMemberRole.Retainer);
            PersonId disciple = Add(world, "person.disciple", "弟子", 1540, FamilyMemberRole.Disciple);
            PersonId guest = Add(world, "person.guest", "客将", 1520, FamilyMemberRole.Guest);
            Link(world, head, child, KinshipType.ParentChild);
            // Even if a caller records a structural fact to a household member, the
            // presentation layer must not draw it as a family link.
            Link(world, head, retainer, KinshipType.Sibling);
            Link(world, head, disciple, KinshipType.Adoption);
            Link(world, head, guest, KinshipType.Spouse);
            state.HeadPersonId = head;

            FamilyTreeGraphSnapshot graph = FamilyTreeGraphBuilder.Build(state, Date);

            Assert.That(graph.HouseholdMembers.Count, Is.EqualTo(3));
            Assert.That(graph.KinshipMembers.Count, Is.EqualTo(2));
            Assert.That(graph.Relations.Count, Is.EqualTo(1));
            Assert.That(graph.Relations[0].To, Is.EqualTo(child));
            Assert.That(graph.TryGetPerson(retainer, out var retainerNode), Is.True);
            Assert.That(retainerNode.Section, Is.EqualTo(FamilyTreeMemberSection.Household));
            Assert.That(retainerNode.RoleName, Is.EqualTo("家臣"));
            Assert.That(graph.TryGetPerson(disciple, out var discipleNode), Is.True);
            Assert.That(discipleNode.RoleName, Is.EqualTo("弟子"));
            Assert.That(graph.TryGetPerson(guest, out var guestNode), Is.True);
            Assert.That(guestNode.RoleName, Is.EqualTo("客将"));
        }

        [Test]
        public void Build_IgnoresRemovedEndpointButKeepsValidMember()
        {
            SuccessionState state = CreateState(out SuccessionWorld world);
            PersonId head = Add(world, "person.head", "当主", 1500, FamilyMemberRole.Head);
            PersonId removed = Add(world, "person.removed", "离族者", 1530,
                FamilyMemberRole.DirectKin);
            Link(world, head, removed, KinshipType.ParentChild);
            Assert.That(world.TryRemoveMember(removed, out string removeError), Is.True, removeError);
            state.HeadPersonId = head;

            FamilyTreeGraphSnapshot graph = FamilyTreeGraphBuilder.Build(state, Date);

            Assert.That(graph.Count, Is.EqualTo(1));
            Assert.That(graph.Contains(removed), Is.False);
            Assert.That(graph.TryGetPerson(head, out var headNode), Is.True);
            Assert.That(headNode.Section, Is.EqualTo(FamilyTreeMemberSection.UnconnectedKin),
                "a member whose only visible relation is gone must remain visible as unconnected kin");
            Assert.That(graph.Relations.Count, Is.Zero);
        }

        [Test]
        public void Build_ReportsKnownLifeStatusWhenAgeIsUnavailable()
        {
            SuccessionState state = CreateState(out SuccessionWorld world);
            var deceasedId = new PersonId("person.deceased");
            var retiredId = new PersonId("person.retired");
            var unknownId = new PersonId("person.unknown");
            Assert.That(world.TryAddPerson(new PersonState(deceasedId, "已故者"),
                new SuccessionPersonLife(1500, false, false), out string deceasedError), Is.True,
                deceasedError);
            Assert.That(world.TryAddMember(deceasedId, FamilyMemberRole.DirectKin,
                out string deceasedMemberError), Is.True, deceasedMemberError);
            Assert.That(world.TryAddPerson(new PersonState(retiredId, "隐居者"),
                new SuccessionPersonLife(1500, true, true), out string retiredError), Is.True,
                retiredError);
            Assert.That(world.TryAddMember(retiredId, FamilyMemberRole.Head,
                out string retiredMemberError), Is.True, retiredMemberError);
            Assert.That(world.TryAddPerson(new PersonState(unknownId, "资料不明者"),
                new SuccessionPersonLife(1500, true, false), out string unknownError), Is.True,
                unknownError);
            Assert.That(world.TryAddMember(unknownId, FamilyMemberRole.DirectKin,
                out string unknownMemberError), Is.True, unknownMemberError);

            // An invalid date makes age unavailable while the life record stays known.
            FamilyTreeGraphSnapshot graph = FamilyTreeGraphBuilder.Build(state, default);

            Assert.That(graph.TryGetPerson(deceasedId, out var deceased), Is.True);
            Assert.That(deceased.HasAge, Is.False);
            Assert.That(deceased.HasLife, Is.True);
            Assert.That(deceased.IsAlive, Is.False);

            Assert.That(graph.TryGetPerson(retiredId, out var retired), Is.True);
            Assert.That(retired.HasAge, Is.False);
            Assert.That(retired.HasLife, Is.True);
            Assert.That(retired.IsRetired, Is.True);

            Assert.That(graph.TryGetPerson(unknownId, out var unknown), Is.True);
            Assert.That(unknown.HasAge, Is.False);
            Assert.That(unknown.HasLife, Is.True,
                "a life record can exist without a computable age");
        }

        [Test]
        public void Build_DoesNotMutateSuccessionOrKinship()
        {
            SuccessionState state = M2FixedScenario.CreateSuccession();
            int linkCount = state.World.Kinship.Count;
            int memberCount = state.World.Family.MemberCount;
            int personCount = state.World.PersonCount;
            int successionCount = state.SuccessionCount;

            FamilyTreeGraphBuilder.Build(state, Date);
            FamilyTreeGraphBuilder.Build(state, Date);

            Assert.That(state.World.Kinship.Count, Is.EqualTo(linkCount));
            Assert.That(state.World.Family.MemberCount, Is.EqualTo(memberCount));
            Assert.That(state.World.PersonCount, Is.EqualTo(personCount));
            Assert.That(state.SuccessionCount, Is.EqualTo(successionCount));
            Assert.That(state.HeadPersonId, Is.EqualTo(M2FixedScenario.HarunobuId));
            Assert.That(state.DesignatedHeirId, Is.EqualTo(M2FixedScenario.MasatsunaId));
        }

        [Test]
        public void Build_NullStateReturnsEmptySnapshot()
        {
            FamilyTreeGraphSnapshot graph = FamilyTreeGraphBuilder.Build(null, Date);

            Assert.That(graph.Count, Is.Zero);
            Assert.That(graph.Relations.Count, Is.Zero);
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

        private static SuccessionState CreateState(out SuccessionWorld world)
        {
            world = new SuccessionWorld(new FamilyId("family.test"), "测试家");
            return new SuccessionState(world);
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
