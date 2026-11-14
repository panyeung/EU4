using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using SengokuChronicle.Presentation;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// M08A Task 4: chronicle projection, campaign review, family tree and v16 save
    /// compatibility for natural deaths and generated spouses.
    /// </summary>
    public sealed class FamilyLifeCycleChronicleTests
    {
        private static readonly GameDate Date = new GameDate(1561, 1, 1);
        private static readonly PersonId Candidate = new PersonId("person.test.candidate");
        private static readonly PersonId Victim = new PersonId("person.test.victim");
        private static readonly PersonId Head = new PersonId("person.test.head");

        [Test]
        public void Ledger_KeepsDeathAndMarriageInAppendOrderWithReadableRows()
        {
            HistoryLedger ledger = HistoryLedger.CreateEmpty();
            Assert.That(ledger.TryAppendNaturalDeath(1561, 1, Victim.Value, 27, "家人离世",
                "「测试人物」于 1561 年 1 月自然离世，享年 27 岁。", out FamilyChronicleEntry death,
                out string error), Is.True, error);
            Assert.That(ledger.TryAppendFamilyFormation(1561, 1, Candidate.Value,
                FamilyChronicleEntry.MarriageSourceId, "自主婚姻",
                "「测试候选」与「测试配偶」结为配偶。", out FamilyChronicleEntry marriage,
                out error), Is.True, error);

            Assert.That(ledger.Entries, Has.Count.EqualTo(2));
            Assert.That(ledger.Entries[0].EntryId, Is.EqualTo(death.EntryId));
            Assert.That(ledger.Entries[1].EntryId, Is.EqualTo(marriage.EntryId));
            Assert.That(death.Category, Is.EqualTo(ChronicleCategory.NaturalDeath));
            Assert.That(death.SourceId, Is.EqualTo(FamilyChronicleEntry.NaturalDeathSourceId));
            Assert.That(death.Year, Is.EqualTo(1561));
            Assert.That(death.Month, Is.EqualTo(1));

            string deathRow = FamilyChroniclePanel.FormatRow(death);
            Assert.That(deathRow, Does.Contain("1561年1月"));
            Assert.That(deathRow, Does.Contain("人物离世"));
            Assert.That(deathRow, Does.Contain("享年 27 岁"));

            string marriageRow = FamilyChroniclePanel.FormatRow(marriage);
            Assert.That(marriageRow, Does.Contain("家族关系"));
            Assert.That(marriageRow, Does.Contain("婚姻登记"));
            Assert.That(marriageRow, Does.Contain("结为配偶"));
        }

        [Test]
        public void CampaignReview_PeopleSectionReadsOnlyRealDeathAndMarriageHistory()
        {
            using (var fixture = new MarriageSessionFixture())
            {
                fixture.AdvanceToStageReview();
                CampaignReviewView review = fixture.Session.CampaignReview;
                Assert.That(review.Timeline, Is.Not.Empty);

                CampaignHistoryRow deathRow = review.Timeline.FirstOrDefault(row =>
                    row.Label == "自然死亡");
                Assert.That(deathRow, Is.Not.Null,
                    "a real natural death appears as a People-section fact");
                Assert.That(deathRow.Category, Is.EqualTo(CampaignHistoryCategory.People));
                Assert.That(deathRow.Text, Does.Contain("自然离世"));

                CampaignHistoryRow marriageRow = review.Timeline.FirstOrDefault(row =>
                    row.Text.Contains("自主婚姻"));
                Assert.That(marriageRow, Is.Not.Null,
                    "the committed marriage appears as a People-section fact");
                Assert.That(marriageRow.Category, Is.EqualTo(CampaignHistoryCategory.People));
            }
        }

        [Test]
        public void FamilyTree_ShowsTheGeneratedSpouseAndTheDeadPerson()
        {
            SuccessionState state = BuildFamily();
            CharacterDevelopmentState development = CharacterDevelopmentState.CreateEmpty();
            foreach (PersonState person in state.World.Persons)
                development.TryAddPerson(person.Id,
                    CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0), out _);

            var proposals = new[]
            {
                new NpcMarriageProposal(Candidate, PersonGender.Male, 30, Date.Year - 30)
            };
            FamilyMarriagePlan plan = FamilyMarriageSettlement.Plan(state, proposals, Date);
            Assert.That(FamilyMarriageSettlement.TryApply(state, plan, ref development,
                out string error), Is.True, error);
            state.World.TrySetLife(Victim, new SuccessionPersonLife(Date.Year - 27, false,
                false), out error);

            FamilyTreeGraphSnapshot graph = FamilyTreeGraphBuilder.Build(state, Date);
            PersonId spouseId = plan.Marriages[0].SpouseId;
            Assert.That(graph.TryGetPerson(spouseId, out FamilyTreePersonNode spouse), Is.True);
            Assert.That(spouse.Role, Is.EqualTo(FamilyMemberRole.Spouse));
            Assert.That(spouse.Section, Is.EqualTo(FamilyTreeMemberSection.Kinship),
                "the generated spouse is displayed as formal same-generation kin");
            Assert.That(spouse.IsAlive, Is.True);

            Assert.That(graph.TryGetPerson(Victim, out FamilyTreePersonNode dead), Is.True);
            Assert.That(dead.IsAlive, Is.False);
            Assert.That(FamilyTreeCardText.Format(dead, new FamilyTreeNodePlacement(Victim,
                default, default, 1, FamilyTreeLayoutSection.Generation)), Does.Contain("已故"));

            Assert.That(graph.Relations.Any(edge =>
                edge.Type == KinshipType.Spouse &&
                ((edge.From == Candidate && edge.To == spouseId) ||
                 (edge.From == spouseId && edge.To == Candidate))), Is.True,
                "the formal marriage edge is drawn");
        }

        [Test]
        public void Version17Save_RoundTripsASynthesizedSpouseAndDeath()
        {
            using (var fixture = new MarriageSessionFixture())
            {
                fixture.AdvanceToSettlement();
                PersonId spouseId = fixture.SpouseId;
                Assert.That(spouseId.IsEmpty, Is.False);

                SaveData save = fixture.Session.CaptureSave();
                Assert.That(save.saveFormatVersion, Is.EqualTo(17),
                    "M08A reuses the existing person/life/kinship fields; M08B adds market");
                Assert.That(SaveDataValidator.CurrentVersion, Is.EqualTo(17));
                string rngBefore = RngSignature(fixture.Session);

                Assert.That(fixture.Session.TryApplySave(save).Succeeded, Is.True,
                    fixture.Session.LastEventMessage);

                Assert.That(fixture.Session.Succession.World.TryGetPerson(spouseId,
                    out PersonState spouse), Is.True);
                Assert.That(spouse.Gender, Is.EqualTo(PersonGender.Female));
                Assert.That(fixture.Session.Succession.World.IsLiving(spouseId), Is.True);
                Assert.That(fixture.Session.Succession.World.Family.IsMember(spouseId), Is.True);
                Assert.That(fixture.Session.Succession.World.Kinship.Contains(new KinshipLink(
                    M2FixedScenario.SakonId, spouseId, KinshipType.Spouse)), Is.True);
                Assert.That(fixture.Session.WorldClock.CurrentDate,
                    Is.EqualTo(new GameDate(1561, 1, 1)));
                Assert.That(RngSignature(fixture.Session), Is.EqualTo(rngBefore),
                    "the persisted random stream survives the round-trip");
            }
        }

        private static SuccessionState BuildFamily()
        {
            var world = new SuccessionWorld(new FamilyId("family.test.review"), "测试家");
            Add(world, Head, Date.Year - 45, PersonGender.Male, FamilyMemberRole.Head);
            Add(world, Candidate, Date.Year - 30, PersonGender.Male,
                FamilyMemberRole.DirectKin);
            Add(world, Victim, Date.Year - 27, PersonGender.Female,
                FamilyMemberRole.DirectKin);
            return new SuccessionState(world) { HeadPersonId = Head };
        }

        private static void Add(SuccessionWorld world, PersonId id, int birthYear,
            PersonGender gender, FamilyMemberRole role)
        {
            Assert.That(world.TryAddPerson(new PersonState(id, id.Value, gender),
                new SuccessionPersonLife(birthYear, true, false), out string error), Is.True,
                error);
            Assert.That(world.TryAddMember(id, role, out error), Is.True, error);
        }

        private static string RngSignature(GameSession session)
        {
            RandomStateSaveData state = session.RandomState.CaptureState();
            var builder = new StringBuilder(state.initialSeedHex);
            foreach (RandomStreamSaveEntry entry in state.streams)
                builder.Append('|').Append(entry.streamId).Append(':')
                    .Append(entry.state0Hex).Append(entry.state1Hex);
            return builder.ToString();
        }

        /// <summary>
        /// Reuses the Task 3 deterministic shape: Harunobu (20, head) and Sakon (30)
        /// survive the first eight months and the annual boundary then kills the third
        /// member while the 15% marriage roll succeeds and gives Sakon a spouse.
        /// </summary>
        private sealed class MarriageSessionFixture : IDisposable
        {
            public MarriageSessionFixture()
            {
                Session = GameSession.NewGame();
                Session.InitNewGame(FindSeed(stream =>
                    stream.TryNextInt(1000, out int first) && first >= 5 &&
                    stream.TryNextInt(1000, out int dying) && dying < 250 &&
                    stream.TryNextInt(1000, out int third) && third >= 5 &&
                    stream.TryNextInt(100, out int marriage) && marriage < 15));
                foreach (PersonState person in Session.Succession.World.Persons)
                {
                    if (person.Id == M2FixedScenario.HarunobuId ||
                        person.Id == M2FixedScenario.SakonId ||
                        person.Id == M2FixedScenario.MasatsunaId)
                        continue;
                    Session.Succession.World.TryGetLife(person.Id,
                        out SuccessionPersonLife life);
                    Session.Succession.World.TrySetLife(person.Id,
                        new SuccessionPersonLife(life.BirthYear, false, life.Retired), out _);
                }
                Session.Succession.World.TrySetLife(M2FixedScenario.HarunobuId,
                    new SuccessionPersonLife(1540, true, false), out _);
                Session.Succession.World.TrySetLife(M2FixedScenario.MasatsunaId,
                    new SuccessionPersonLife(1470, true, false), out _);
                Session.Succession.World.TrySetLife(M2FixedScenario.SakonId,
                    new SuccessionPersonLife(1530, true, false), out _);
            }

            public GameSession Session { get; }

            public PersonId SpouseId
            {
                get
                {
                    foreach (KinshipLink link in Session.Succession.World.Kinship.Links)
                    {
                        if (link.Type != KinshipType.Spouse)
                            continue;
                        if (link.From == M2FixedScenario.SakonId)
                            return link.To;
                        if (link.To == M2FixedScenario.SakonId)
                            return link.From;
                    }
                    return default;
                }
            }

            public void AdvanceToSettlement()
            {
                for (int i = 0; i < 9; i++)
                    AdvanceAndResolve();
            }

            public void AdvanceToStageReview()
            {
                AdvanceToSettlement();
                GameDate start = Session.CampaignStartDate;
                Assert.That(start.IsValid, Is.True);
                GameDate review = new GameDate(start.Year + 55, start.Month, start.Day);
                int days = 0;
                GameDate cursor = Session.WorldClock.CurrentDate;
                while (cursor < review && GameDate.TryNext(cursor, out GameDate next))
                {
                    cursor = next;
                    days++;
                }
                Assert.That(Session.AdvanceDaysForLegacyFixtureSetup(days), Is.True);
                Assert.That(Session.HasPendingCampaignReview, Is.True,
                    "the fixture reaches the 55-year preliminary review");
            }

            public void Dispose()
            {
                if (Session != null)
                    UnityEngine.Object.DestroyImmediate(Session.gameObject);
            }

            private void AdvanceAndResolve()
            {
                Assert.That(Session.TryAdvanceToNextMonth(), Is.True,
                    Session.LastEventMessage);
                if (string.IsNullOrEmpty(Session.PendingEventId))
                    return;
                string pending = Session.PendingEventId;
                foreach (SengokuChronicle.Events.EventOptionData option in Session.EventCatalog
                             .events.First(item => item.eventId == pending).options)
                {
                    if (!Session.TryGetEventOptionAvailability(pending, option.optionId,
                            out bool enabled, out _) || !enabled)
                        continue;
                    Assert.That(Session.TryChooseEventOption(pending, option.optionId,
                        Session.CreateEventResultId(pending, option.optionId)).Succeeded,
                        Is.True);
                    return;
                }
                Assert.Fail("no enabled option for " + pending);
            }

            private static ulong FindSeed(Func<DeterministicRandomStream, bool> predicate)
            {
                for (ulong seed = 1; seed <= 200000; seed++)
                {
                    DeterministicRandomStream stream = RandomStateManager.Create(seed)
                        .GetStream(RandomStreamKind.WorldSimulation);
                    if (predicate(stream))
                        return seed;
                }
                Assert.Fail("no deterministic seed matched the expected WorldSimulation rolls");
                return 0;
            }
        }
    }
}
