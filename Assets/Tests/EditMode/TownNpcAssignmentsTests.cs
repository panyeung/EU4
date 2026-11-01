using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests
{
    public sealed class TownNpcAssignmentsTests
    {
        [TestCase("owari.kiyosu", 2)]
        [TestCase("owari.atsuta", 1)]
        [TestCase("owari.nagoya", 2)]
        public void TownHasExpectedNpcCount(string town, int count)
        {
            Assert.AreEqual(count, TownNpcAssignments.GetNpcs(town).Count);
        }

        [Test]
        public void TownMappingsAreIsolatedAndUnknownTownIsEmpty()
        {
            Assert.AreEqual(CharacterId.TachibanaSuzu, TownNpcAssignments.GetNpcs("owari.kiyosu")[0]);
            Assert.AreEqual(CharacterId.MoriGenzaemon, TownNpcAssignments.GetNpcs("owari.kiyosu")[1]);
            Assert.AreEqual(CharacterId.KurodaIori, TownNpcAssignments.GetNpcs("owari.atsuta")[0]);
            Assert.AreEqual(CharacterId.HoshinoAya, TownNpcAssignments.GetNpcs("owari.nagoya")[0]);
            Assert.AreEqual(CharacterId.SakaiJirobei, TownNpcAssignments.GetNpcs("owari.nagoya")[1]);
            Assert.AreEqual(0, TownNpcAssignments.GetNpcs("unknown").Count);
        }
    }
}
