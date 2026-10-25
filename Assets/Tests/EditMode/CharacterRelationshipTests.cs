using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    public class CharacterRelationshipTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp() => _session = GameSession.NewGame();

        [TearDown]
        public void TearDown()
        {
            if (_session != null) Object.DestroyImmediate(_session.gameObject);
        }

        [Test]
        public void AllFiveNpcsInitializeAtZero()
        {
            CharacterId[] ids = { CharacterId.KurodaIori, CharacterId.TachibanaSuzu, CharacterId.MoriGenzaemon, CharacterId.HoshinoAya, CharacterId.SakaiJirobei };
            foreach (CharacterId id in ids)
            {
                RelationshipResult result = _session.TryGetRelationship(id);
                Assert.That(result.Succeeded, Is.True, result.Error);
                Assert.That(result.NpcId, Is.EqualTo(id));
                Assert.That(result.CurrentValue, Is.Zero);
            }
        }

        [Test]
        public void ChangeResultsCarryTruePreviousAndClampedCurrentValues()
        {
            RelationshipResult positive = _session.TryChangeRelationship(CharacterId.KurodaIori, 25);
            Assert.That(positive.Succeeded, Is.True, positive.Error);
            Assert.That(positive.NpcId, Is.EqualTo(CharacterId.KurodaIori));
            Assert.That(positive.PreviousValue, Is.Zero);
            Assert.That(positive.CurrentValue, Is.EqualTo(25));
            Assert.That(positive.Error, Is.Empty);

            RelationshipResult negative = _session.TryChangeRelationship(CharacterId.KurodaIori, -40);
            Assert.That(negative.Succeeded, Is.True, negative.Error);
            Assert.That(negative.PreviousValue, Is.EqualTo(25));
            Assert.That(negative.CurrentValue, Is.EqualTo(-15));
        }

        [TestCase(100, 100)]
        [TestCase(-100, -100)]
        [TestCase(101, 100)]
        [TestCase(-101, -100)]
        [TestCase(int.MaxValue, 100)]
        [TestCase(int.MinValue, -100)]
        public void ChangeClampsAtBothBoundsWithoutOverflow(int delta, int expected)
        {
            RelationshipResult result = _session.TryChangeRelationship(CharacterId.KurodaIori, delta);
            Assert.That(result.Succeeded, Is.True, result.Error);
            Assert.That(result.PreviousValue, Is.Zero);
            Assert.That(result.CurrentValue, Is.EqualTo(expected));
            RelationshipResult stored = _session.TryGetRelationship(CharacterId.KurodaIori);
            Assert.That(stored.Succeeded, Is.True, stored.Error);
            Assert.That(stored.CurrentValue, Is.EqualTo(expected));
        }

        [TestCase("npc.unknown")]
        [TestCase("")]
        [TestCase("player.samurai")]
        public void InvalidUnknownOrPlayerTargetFailsWithoutChangingAnyNpc(string rawId)
        {
            _session.TryChangeRelationship(CharacterId.KurodaIori, 30);
            _session.TryChangeRelationship(CharacterId.TachibanaSuzu, -20);
            CharacterId id = CharacterId.Parse(rawId);
            RelationshipResult read = _session.TryGetRelationship(id);
            Assert.That(read.Succeeded, Is.False);
            Assert.That(read.Error, Is.Not.Empty);
            RelationshipResult failed = _session.TryChangeRelationship(id, int.MaxValue);
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.NpcId, Is.EqualTo(id));
            Assert.That(failed.PreviousValue, Is.Zero);
            Assert.That(failed.CurrentValue, Is.Zero);
            Assert.That(failed.Error, Is.Not.Empty);
            Assert.That(_session.TryGetRelationship(CharacterId.KurodaIori).CurrentValue, Is.EqualTo(30));
            Assert.That(_session.TryGetRelationship(CharacterId.TachibanaSuzu).CurrentValue, Is.EqualTo(-20));
        }

        [Test]
        public void UpdatingOneNpcDoesNotAffectAnotherAndNewGameResetsAll()
        {
            _session.TryChangeRelationship(CharacterId.KurodaIori, 12);
            Assert.That(_session.TryGetRelationship(CharacterId.TachibanaSuzu).CurrentValue, Is.Zero);
            _session.InitNewGame();
            foreach (CharacterDefinition npc in CharacterCatalog.Npcs)
                Assert.That(_session.TryGetRelationship(npc.Id).CurrentValue, Is.Zero, npc.Id.ToString());
        }

        [Test]
        public void RuntimeSurfaceExposesOnlyPlayerToNpcTargeting()
        {
            Assert.That(typeof(CharacterRelationships).GetMethods(), Has.None.Property("Name").EqualTo("TryChangeNpcRelationship"));
        }
    }
}
