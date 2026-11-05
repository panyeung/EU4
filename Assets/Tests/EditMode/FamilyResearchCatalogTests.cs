using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 1: stable IDs and the one authored deterministic project.</summary>
    public sealed class FamilyResearchCatalogTests
    {
        [Test]
        public void Catalog_AuthorsExactlyOneStableTechnique()
        {
            Assert.That(FamilyResearchCatalog.All, Has.Count.EqualTo(1));
            Assert.That(FamilyResearchCatalog.TryGet(FamilyResearchCatalog.SmokeMixingResearchId,
                out FamilyResearchDefinition definition), Is.True);

            Assert.That(definition.Id, Is.EqualTo("research.smoke-mixing"));
            Assert.That(definition.TechniqueId, Is.EqualTo("technique.smoke-mixing"));
            Assert.That(definition.TechniqueDisplayName, Is.EqualTo("烟幕调制术"));
            Assert.That(definition.ProjectDisplayName, Is.EqualTo("烟幕调制术研究"));
            Assert.That(definition.RequiredProgress, Is.EqualTo(2));
            Assert.That(definition.MonthlyFundsCost, Is.EqualTo(1));
            Assert.That(definition.MonthlySuppliesCost, Is.EqualTo(1));
            Assert.That(definition.MonthlyProgress, Is.EqualTo(1));
        }

        [Test]
        public void Catalog_ResolvesResearchAndTechniqueIdsBothWays()
        {
            Assert.That(FamilyResearchCatalog.TryGetByTechnique(
                FamilyResearchCatalog.SmokeMixingTechniqueId, out FamilyResearchDefinition byTechnique),
                Is.True);
            Assert.That(byTechnique.Id, Is.EqualTo(FamilyResearchCatalog.SmokeMixingResearchId));
            Assert.That(FamilyResearchCatalog.IsKnownTechniqueId(
                FamilyResearchCatalog.SmokeMixingTechniqueId), Is.True);
            Assert.That(FamilyResearchCatalog.TechniqueDisplayName(
                FamilyResearchCatalog.SmokeMixingTechniqueId), Is.EqualTo("烟幕调制术"));
            Assert.That(FamilyResearchCatalog.MaximumRequiredProgress, Is.EqualTo(2));
        }

        [Test]
        public void Catalog_RejectsUnknownAndEmptyIds()
        {
            Assert.That(FamilyResearchCatalog.TryGet("research.unknown", out _), Is.False);
            Assert.That(FamilyResearchCatalog.TryGet(null, out _), Is.False);
            Assert.That(FamilyResearchCatalog.TryGet(string.Empty, out _), Is.False);
            Assert.That(FamilyResearchCatalog.TryGetByTechnique("technique.unknown", out _), Is.False);
        }
    }
}
