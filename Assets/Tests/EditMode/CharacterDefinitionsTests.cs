using System.Collections.Generic;
using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    public class CharacterDefinitionsTests
    {
        [Test]
        public void Catalog_ContainsOnePlayerAndFiveFictionalNpcsWithStableIdsAndNames()
        {
            IReadOnlyList<CharacterDefinition> all = CharacterCatalog.All;
            Assert.That(all.Count, Is.EqualTo(6));
            Assert.That(CharacterCatalog.Player.Role, Is.EqualTo(CharacterRole.Player));
            Assert.That(CharacterCatalog.Npcs.Count, Is.EqualTo(5));
            Assert.That(CharacterCatalog.Player.Id.Value, Is.EqualTo("player.samurai"));
            Assert.That(CharacterCatalog.Player.DisplayName, Is.EqualTo("明智 晴信"));
            Assert.That(CharacterCatalog.Npcs[0].Id.Value, Is.EqualTo("npc.kuroda.iori"));
            Assert.That(CharacterCatalog.Npcs[0].DisplayName, Is.EqualTo("黒田 伊織"));
            Assert.That(CharacterCatalog.Npcs[1].Id.Value, Is.EqualTo("npc.tachibana.suzu"));
            Assert.That(CharacterCatalog.Npcs[1].DisplayName, Is.EqualTo("橘 すず"));
            Assert.That(CharacterCatalog.Npcs[2].Id.Value, Is.EqualTo("npc.mori.genzaemon"));
            Assert.That(CharacterCatalog.Npcs[2].DisplayName, Is.EqualTo("森 源左衛門"));
            Assert.That(CharacterCatalog.Npcs[3].Id.Value, Is.EqualTo("npc.hoshino.aya"));
            Assert.That(CharacterCatalog.Npcs[3].DisplayName, Is.EqualTo("星野 彩"));
            Assert.That(CharacterCatalog.Npcs[4].Id.Value, Is.EqualTo("npc.sakai.jirobei"));
            Assert.That(CharacterCatalog.Npcs[4].DisplayName, Is.EqualTo("酒井 次郎兵衛"));
            Assert.That(CharacterCatalog.TryGet(CharacterId.Parse("npc.kuroda.iori"), out CharacterDefinition npc), Is.True);
            Assert.That(npc.DisplayName, Is.EqualTo("黒田 伊織"));
            var ids = new HashSet<string>();
            foreach (CharacterDefinition definition in all)
            {
                Assert.That(ids.Add(definition.Id.Value), Is.True, "IDs must be unique");
            }
            foreach (CharacterDefinition definition in CharacterCatalog.Npcs)
                Assert.That(definition.IsFictional, Is.True);
        }

        [Test]
        public void Catalog_CollectionsCannotExposeOrMutateTheirBackingArrays()
        {
            IReadOnlyList<CharacterDefinition> all = CharacterCatalog.All;
            IReadOnlyList<CharacterDefinition> npcs = CharacterCatalog.Npcs;
            Assert.That(all, Is.Not.InstanceOf<CharacterDefinition[]>());
            Assert.That(npcs, Is.Not.InstanceOf<CharacterDefinition[]>());

            var allList = all as IList<CharacterDefinition>;
            var npcList = npcs as IList<CharacterDefinition>;
            Assert.That(allList, Is.Not.Null);
            Assert.That(npcList, Is.Not.Null);
            Assert.That(allList.IsReadOnly, Is.True);
            Assert.That(npcList.IsReadOnly, Is.True);
            Assert.Throws<System.NotSupportedException>(() => allList[0] = CharacterCatalog.Npcs[0]);
            Assert.Throws<System.NotSupportedException>(() => npcList[0] = CharacterCatalog.Player);

            string[] expectedIds =
            {
                "player.samurai", "npc.kuroda.iori", "npc.tachibana.suzu",
                "npc.mori.genzaemon", "npc.hoshino.aya", "npc.sakai.jirobei"
            };
            string[] expectedNames =
            {
                "明智 晴信", "黒田 伊織", "橘 すず", "森 源左衛門", "星野 彩", "酒井 次郎兵衛"
            };
            Assert.That(all.Count, Is.EqualTo(expectedIds.Length));
            for (int i = 0; i < expectedIds.Length; i++)
            {
                Assert.That(all[i].Id.Value, Is.EqualTo(expectedIds[i]));
                Assert.That(all[i].DisplayName, Is.EqualTo(expectedNames[i]));
                Assert.That(CharacterCatalog.TryGet(CharacterId.Parse(expectedIds[i]), out CharacterDefinition found), Is.True);
                Assert.That(found, Is.SameAs(all[i]));
            }
            Assert.That(npcs.Count, Is.EqualTo(5));
            for (int i = 0; i < npcs.Count; i++)
                Assert.That(npcs[i], Is.SameAs(all[i + 1]));
        }

        [Test]
        public void Validator_ReportsEmptyDuplicateUnknownIdsAndInvalidDefinitionsWithoutThrowing()
        {
            var definitions = new List<CharacterDefinition>
            {
                CharacterCatalog.Player,
                CharacterCatalog.Player,
                new CharacterDefinition(default, "Unnamed", CharacterRole.Npc, true),
                new CharacterDefinition(new CharacterId("npc.unknown"), "Unknown", CharacterRole.Npc, true),
                new CharacterDefinition(new CharacterId("npc.test"), " ", CharacterRole.Npc, true),
                null
            };

            IReadOnlyList<CharacterDefinitionDiagnostic> diagnostics = CharacterDefinitionValidator.Validate(definitions);
            Assert.That(diagnostics, Is.Not.Empty);
            var diagnosticList = diagnostics as IList<CharacterDefinitionDiagnostic>;
            Assert.That(diagnosticList, Is.Not.Null);
            Assert.That(diagnosticList.IsReadOnly, Is.True);
            Assert.Throws<System.NotSupportedException>(() => diagnosticList[0] = null);
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("duplicate_id"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("empty_id"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("unknown_id"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("invalid_name"));
            Assert.That(diagnostics, Has.Some.Property("Code").EqualTo("null_definition"));
        }

        [Test]
        public void CatalogValidation_AcceptsAllSixDefinitions()
        {
            Assert.That(CharacterDefinitionValidator.Validate(CharacterCatalog.All), Is.Empty);
        }
    }
}
