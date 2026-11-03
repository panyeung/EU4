using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class M2FixedScenarioTests
    {
        private static readonly GameDate Start = new GameDate(1560, 4, 1);

        [Test]
        public void CreateSuccession_HasApprovedSixMembersAndDirectLine()
        {
            SuccessionState state = M2FixedScenario.CreateSuccession();

            Assert.That(state.World.Family.Id, Is.EqualTo(new FamilyId("family.akechi.sc013")));
            Assert.That(state.World.Family.DisplayName, Is.EqualTo("明智家"));
            Assert.That(state.World.PersonCount, Is.EqualTo(6));
            Assert.That(state.World.Family.MemberCount, Is.EqualTo(6));
            Assert.That(state.HeadPersonId, Is.EqualTo(new PersonId("person.sc013.harunobu")));
            Assert.That(state.DesignatedHeirId, Is.EqualTo(new PersonId("person.sc013.masatsuna")));
            Assert.That(state.SuccessionCount, Is.Zero);
            Assert.That(state.Phase, Is.EqualTo(SuccessionPhase.None));

            AssertPerson(state, "person.sc013.harunobu", "明智 晴信", 70, FamilyMemberRole.Head);
            AssertPerson(state, "person.sc013.chie", "明智 千恵", 66, FamilyMemberRole.Spouse);
            AssertPerson(state, "person.sc013.masatsuna", "明智 正綱", 46, FamilyMemberRole.DirectKin);
            AssertPerson(state, "person.sc013.nobumasa", "明智 信正", 22, FamilyMemberRole.DirectKin);
            AssertPerson(state, "person.sc013.tokihime", "明智 時姫", 44, FamilyMemberRole.CollateralKin);
            AssertPerson(state, "person.sc013.sakon", "佐近", 40, FamilyMemberRole.Retainer);

            var harunobu = new PersonId("person.sc013.harunobu");
            var masatsuna = new PersonId("person.sc013.masatsuna");
            var nobumasa = new PersonId("person.sc013.nobumasa");
            Assert.That(state.World.Kinship.Contains(
                new KinshipLink(harunobu, masatsuna, KinshipType.ParentChild)), Is.True);
            Assert.That(state.World.Kinship.Contains(
                new KinshipLink(masatsuna, nobumasa, KinshipType.ParentChild)), Is.True);
        }

        [Test]
        public void CreateSuccession_IsM2OnlyAndExpectedHeirFollowsTheDirectLine()
        {
            SuccessionState m2 = M2FixedScenario.CreateSuccession();
            SuccessionState standard = SuccessionSample.CreateStandard();

            Assert.That(M2FixedScenario.IsActive(m2), Is.True);
            Assert.That(M2FixedScenario.IsActive(standard), Is.False);
            Assert.That(M2FixedScenario.ExpectedHeir(m2),
                Is.EqualTo(new PersonId("person.sc013.masatsuna")));

            m2.HeadPersonId = new PersonId("person.sc013.masatsuna");
            M2FixedScenario.SyncDesignation(m2);

            Assert.That(M2FixedScenario.ExpectedHeir(m2),
                Is.EqualTo(new PersonId("person.sc013.nobumasa")));
            Assert.That(m2.DesignatedHeirId,
                Is.EqualTo(new PersonId("person.sc013.nobumasa")));
        }

        [Test]
        public void BuildRoster_ShowsSixIdentitiesRolesAgesGenerationsAndKinship()
        {
            SuccessionState state = M2FixedScenario.CreateSuccession();

            IReadOnlyList<M2RosterRow> rows = M2FixedScenario.BuildRoster(state, Start,
                state.DesignatedHeirId);

            Assert.That(rows.Count, Is.EqualTo(6));
            AssertRow(rows[0], "明智 晴信", "家主", 70, "第一代", "配偶");
            AssertRow(rows[1], "明智 千恵", "配偶", 66, "第一代", "配偶");
            AssertRow(rows[2], "明智 正綱", "直系亲族", 46, "第二代", "父母");
            AssertRow(rows[3], "明智 信正", "直系亲族", 22, "第三代", "父亲");
            AssertRow(rows[4], "明智 時姫", "旁系亲族", 44, "第一代", "兄弟姐妹");
            AssertRow(rows[5], "佐近", "家臣", 40, "家臣", "无亲属记录");
            Assert.That(rows[2].IsDesignatedHeir, Is.True);
            Assert.That(rows[2].IsDesignatedHeirValid, Is.True);
            Assert.That(rows[0].Kinship, Does.Contain("配偶：明智 千恵"));
            Assert.That(rows[0].Kinship, Does.Contain("子女：明智 正綱"));
            Assert.That(rows[0].Kinship, Does.Contain("兄弟姐妹：明智 時姫"));
        }

        private static void AssertPerson(SuccessionState state, string id, string name, int age,
            FamilyMemberRole role)
        {
            var personId = new PersonId(id);
            Assert.That(state.World.TryGetPerson(personId, out PersonState person), Is.True, id);
            Assert.That(person.DisplayName, Is.EqualTo(name));
            Assert.That(state.World.TryGetLife(personId, out SuccessionPersonLife life), Is.True, id);
            Assert.That(life.AgeAt(Start), Is.EqualTo(age), id);
            Assert.That(state.World.Family.TryGetRole(personId, out FamilyMemberRole actual), Is.True, id);
            Assert.That(actual, Is.EqualTo(role), id);
        }

        private static void AssertRow(M2RosterRow row, string name, string roleName, int age,
            string generation, string kinshipFragment)
        {
            Assert.That(row.DisplayName, Is.EqualTo(name));
            Assert.That(row.RoleName, Is.EqualTo(roleName));
            Assert.That(row.Age, Is.EqualTo(age));
            Assert.That(row.GenerationLabel, Is.EqualTo(generation));
            Assert.That(row.Kinship, Does.Contain(kinshipFragment));
        }
    }
}
