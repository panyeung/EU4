using System.Linq;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>M04B Task 2: validated, stably ordered pregnancy records.</summary>
    public sealed class FamilyPregnancyStateTests
    {
        private static readonly PersonId Mother = new PersonId("person.test.mother");
        private static readonly PersonId Father = new PersonId("person.test.father");
        private static readonly PersonId SecondMother = new PersonId("person.test.mother2");
        private static readonly GameDate Conception = new GameDate(1560, 4, 1);

        [Test]
        public void ValidRecord_DerivesDueDateAndProgress()
        {
            Assert.That(FamilyPregnancyRecord.TryCreate(Mother, Father, Conception,
                new GameDate(1561, 1, 1), out FamilyPregnancyRecord record,
                out string error), Is.True, error);

            Assert.That(record.DueDate, Is.EqualTo(new GameDate(1561, 1, 1)));
            Assert.That(record.ProgressMonth(Conception), Is.EqualTo(1));
            Assert.That(record.ProgressMonth(new GameDate(1560, 12, 1)), Is.EqualTo(9));
            Assert.That(record.IsDueAt(new GameDate(1560, 12, 1)), Is.False);
            Assert.That(record.IsDueAt(new GameDate(1561, 1, 1)), Is.True);
        }

        [TestCase(1560, 13, 1, 1561, 10, 1)]
        [TestCase(1560, 2, 30, 1560, 11, 30)]
        [TestCase(1560, 4, 1, 1561, 2, 1)]
        [TestCase(1560, 4, 1, 1560, 12, 1)]
        public void InvalidOrNonNineMonthRecord_IsRejected(int cy, int cm, int cd,
            int dy, int dm, int dd)
        {
            var conception = new GameDate(cy, cm, cd);
            var due = new GameDate(dy, dm, dd);

            bool created = FamilyPregnancyRecord.TryCreate(Mother, Father, conception, due,
                out _, out string error);

            Assert.That(created, Is.False);
            Assert.That(error, Is.Not.Empty);
        }

        [Test]
        public void Records_AreOrderedByGestatingPersonId()
        {
            var state = new FamilyPregnancyState();
            Assert.That(state.TryAdd(Record(SecondMother, Father), out string error),
                Is.True, error);
            Assert.That(state.TryAdd(Record(Mother, Father), out error), Is.True, error);

            Assert.That(state.Records.Select(record => record.GestatingPersonId),
                Is.EqualTo(new[] { Mother, SecondMother }));
        }

        [Test]
        public void DuplicateFemale_IsRejectedWithoutMutation()
        {
            var state = new FamilyPregnancyState();
            Assert.That(state.TryAdd(Record(Mother, Father), out _), Is.True);

            Assert.That(state.TryAdd(Record(Mother, SecondMother), out string error), Is.False);
            Assert.That(error, Does.Contain("女性"));
            Assert.That(state.Count, Is.EqualTo(1));
        }

        [Test]
        public void DuplicatePair_IsRejectedInEitherEndpointOrder()
        {
            var state = new FamilyPregnancyState();
            Assert.That(state.TryAdd(Record(Mother, Father), out _), Is.True);

            Assert.That(state.ContainsPair(Father, Mother), Is.True);
            Assert.That(state.TryAdd(Record(Father, Mother), out string error), Is.False);
            Assert.That(error, Does.Contain("配偶对"));
            Assert.That(state.TryAdd(Record(SecondMother, Father), out _), Is.True,
                "a male may have pregnancies with different spouses");
        }

        [Test]
        public void Remove_RequiresTheExactRecord()
        {
            var state = new FamilyPregnancyState();
            Assert.That(state.TryAdd(Record(Mother, Father), out _), Is.True);
            Assert.That(state.TryRemove(Record(Mother, SecondMother), out _), Is.False);
            Assert.That(state.Count, Is.EqualTo(1));

            Assert.That(state.TryRemove(Record(Mother, Father), out string error), Is.True,
                error);
            Assert.That(state.Count, Is.Zero);
        }

        private static FamilyPregnancyRecord Record(PersonId mother, PersonId father)
        {
            Assert.That(FamilyPregnancyRecord.TryCreate(mother, father, Conception,
                new GameDate(1561, 1, 1), out FamilyPregnancyRecord record,
                out string error), Is.True, error);
            return record;
        }
    }
}
