using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public sealed class DeterministicRandomTests
    {
        [Test]
        public void SameSeedAndCallsProduceSameSequence()
        {
            RandomStateManager first = RandomStateManager.Create(123456789UL);
            RandomStateManager second = RandomStateManager.Create(123456789UL);
            DeterministicRandomStream firstEvents = first.GetStream(RandomStreamKind.Events);
            DeterministicRandomStream secondEvents = second.GetStream(RandomStreamKind.Events);

            for (int i = 0; i < 100; i++)
                Assert.That(firstEvents.NextUInt64(), Is.EqualTo(secondEvents.NextUInt64()));
        }

        [Test]
        public void AdditionalCallsOnOtherStreamDoNotChangeEventSequence()
        {
            RandomStateManager first = RandomStateManager.Create(0UL);
            RandomStateManager second = RandomStateManager.Create(0UL);
            DeterministicRandomStream firstEvents = first.GetStream(RandomStreamKind.Events);
            DeterministicRandomStream secondEvents = second.GetStream(RandomStreamKind.Events);
            DeterministicRandomStream other = first.GetStream(RandomStreamKind.CharacterGrowth);

            for (int i = 0; i < 50; i++)
            {
                other.NextUInt64();
                Assert.That(firstEvents.NextUInt64(), Is.EqualTo(secondEvents.NextUInt64()));
            }
        }

        [Test]
        public void CaptureRestoreContinuesExactSequence()
        {
            RandomStateManager original = RandomStateManager.Create(42UL);
            DeterministicRandomStream events = original.GetStream(RandomStreamKind.Events);
            for (int i = 0; i < 7; i++) events.NextUInt64();

            RandomStateSaveData captured = original.CaptureState();
            Assert.That(RandomStateManager.TryRestore(captured, out RandomStateManager restored), Is.True);

            for (int i = 0; i < 30; i++)
                Assert.That(restored.GetStream(RandomStreamKind.Events).NextUInt64(),
                    Is.EqualTo(events.NextUInt64()));
        }

        [Test]
        public void InvalidBoundsDoNotConsumeState()
        {
            DeterministicRandomStream tested = RandomStateManager.Create(7UL).GetStream(RandomStreamKind.Events);
            DeterministicRandomStream expected = RandomStateManager.Create(7UL).GetStream(RandomStreamKind.Events);

            Assert.That(tested.TryNextInt(0, out _), Is.False);
            Assert.That(tested.TryNextInt(-1, out _), Is.False);
            Assert.That(tested.NextUInt64(), Is.EqualTo(expected.NextUInt64()));
        }

        [Test]
        public void InvalidStateIsRejected()
        {
            RandomStateSaveData invalid = RandomStateManager.Create(1UL).CaptureState();
            invalid.streams[0].state0Hex = "0000000000000000";
            invalid.streams[0].state1Hex = "0000000000000000";

            Assert.That(RandomStateManager.TryRestore(invalid, out _), Is.False);

            RandomStateSaveData emptySeed = RandomStateManager.Create(1UL).CaptureState();
            emptySeed.initialSeedHex = string.Empty;
            Assert.That(RandomStateManager.TryRestore(emptySeed, out _), Is.False);
        }
    }
}
