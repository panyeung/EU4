using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Stable, content-defined character identifier.</summary>
    public readonly struct CharacterId : IEquatable<CharacterId>
    {
        public static readonly CharacterId Player = new CharacterId("player.samurai");
        public static readonly CharacterId KurodaIori = new CharacterId("npc.kuroda.iori");
        public static readonly CharacterId TachibanaSuzu = new CharacterId("npc.tachibana.suzu");
        public static readonly CharacterId MoriGenzaemon = new CharacterId("npc.mori.genzaemon");
        public static readonly CharacterId HoshinoAya = new CharacterId("npc.hoshino.aya");
        public static readonly CharacterId SakaiJirobei = new CharacterId("npc.sakai.jirobei");

        private static readonly CharacterId[] KnownIds =
        {
            Player, KurodaIori, TachibanaSuzu, MoriGenzaemon, HoshinoAya, SakaiJirobei
        };

        private readonly string _value;
        public CharacterId(string value) { _value = value; }
        public string Value => _value;
        public bool IsEmpty => string.IsNullOrWhiteSpace(_value);
        public bool IsKnown
        {
            get
            {
                for (int i = 0; i < KnownIds.Length; i++)
                    if (Equals(KnownIds[i])) return true;
                return false;
            }
        }

        public static CharacterId Parse(string value) => new CharacterId(value);
        public static bool TryParse(string value, out CharacterId id)
        {
            id = new CharacterId(value);
            return id.IsKnown;
        }
        public bool Equals(CharacterId other) => string.Equals(_value, other._value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is CharacterId other && Equals(other);
        public override int GetHashCode() => _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        public override string ToString() => _value ?? string.Empty;
        public static bool operator ==(CharacterId left, CharacterId right) => left.Equals(right);
        public static bool operator !=(CharacterId left, CharacterId right) => !left.Equals(right);
    }

    public enum CharacterRole { Player, Npc }

    /// <summary>Immutable static identity data; contains no runtime relationship state.</summary>
    public sealed class CharacterDefinition
    {
        public CharacterDefinition(CharacterId id, string displayName, CharacterRole role, bool isFictional)
        {
            Id = id;
            DisplayName = displayName;
            Role = role;
            IsFictional = isFictional;
        }
        public CharacterId Id { get; }
        public string DisplayName { get; }
        public CharacterRole Role { get; }
        public bool IsFictional { get; }
    }

    /// <summary>Built-in roster and the explicit stable-ID-to-display-name mapping.</summary>
    public static class CharacterCatalog
    {
        public static readonly CharacterDefinition Player = new CharacterDefinition(CharacterId.Player, "明智 晴信", CharacterRole.Player, true);
        private static readonly CharacterDefinition[] NpcDefinitions =
        {
            new CharacterDefinition(CharacterId.KurodaIori, "黒田 伊織", CharacterRole.Npc, true),
            new CharacterDefinition(CharacterId.TachibanaSuzu, "橘 すず", CharacterRole.Npc, true),
            new CharacterDefinition(CharacterId.MoriGenzaemon, "森 源左衛門", CharacterRole.Npc, true),
            new CharacterDefinition(CharacterId.HoshinoAya, "星野 彩", CharacterRole.Npc, true),
            new CharacterDefinition(CharacterId.SakaiJirobei, "酒井 次郎兵衛", CharacterRole.Npc, true)
        };
        private static readonly CharacterDefinition[] AllDefinitions = CreateAll();
        private static readonly IReadOnlyList<CharacterDefinition> ReadOnlyAllDefinitions = Array.AsReadOnly(AllDefinitions);
        private static readonly IReadOnlyList<CharacterDefinition> ReadOnlyNpcDefinitions = Array.AsReadOnly(NpcDefinitions);
        private static CharacterDefinition[] CreateAll()
        {
            var all = new CharacterDefinition[NpcDefinitions.Length + 1];
            all[0] = Player;
            Array.Copy(NpcDefinitions, 0, all, 1, NpcDefinitions.Length);
            return all;
        }
        public static IReadOnlyList<CharacterDefinition> All => ReadOnlyAllDefinitions;
        public static IReadOnlyList<CharacterDefinition> Npcs => ReadOnlyNpcDefinitions;

        public static bool TryGet(CharacterId id, out CharacterDefinition definition)
        {
            for (int i = 0; i < AllDefinitions.Length; i++)
            {
                if (AllDefinitions[i].Id == id) { definition = AllDefinitions[i]; return true; }
            }
            definition = null;
            return false;
        }
    }

    public sealed class CharacterDefinitionDiagnostic
    {
        public CharacterDefinitionDiagnostic(string code, string message, CharacterId id)
        { Code = code; Message = message; Id = id; }
        public string Code { get; }
        public string Message { get; }
        public CharacterId Id { get; }
        public override string ToString() => Code + ": " + Message;
    }

    /// <summary>Validates untrusted definition collections without throwing on malformed entries.</summary>
    public static class CharacterDefinitionValidator
    {
        public static IReadOnlyList<CharacterDefinitionDiagnostic> Validate(IEnumerable<CharacterDefinition> definitions)
        {
            var diagnostics = new List<CharacterDefinitionDiagnostic>();
            if (definitions == null)
            {
                diagnostics.Add(new CharacterDefinitionDiagnostic("null_collection", "Definition collection is null.", default));
                return Array.AsReadOnly(diagnostics.ToArray());
            }

            var seen = new HashSet<CharacterId>();
            foreach (CharacterDefinition definition in definitions)
            {
                if (definition == null)
                {
                    diagnostics.Add(new CharacterDefinitionDiagnostic("null_definition", "Definition entry is null.", default));
                    continue;
                }
                CharacterId id = definition.Id;
                if (id.IsEmpty)
                    diagnostics.Add(new CharacterDefinitionDiagnostic("empty_id", "Character ID is empty.", id));
                else if (!seen.Add(id))
                    diagnostics.Add(new CharacterDefinitionDiagnostic("duplicate_id", "Character ID is duplicated.", id));
                if (!id.IsEmpty && !id.IsKnown)
                    diagnostics.Add(new CharacterDefinitionDiagnostic("unknown_id", "Character ID is not registered.", id));
                if (string.IsNullOrWhiteSpace(definition.DisplayName))
                    diagnostics.Add(new CharacterDefinitionDiagnostic("invalid_name", "Display name is empty.", id));
                if (!Enum.IsDefined(typeof(CharacterRole), definition.Role))
                    diagnostics.Add(new CharacterDefinitionDiagnostic("invalid_role", "Character role is invalid.", id));
                if (definition.Role == CharacterRole.Npc && !definition.IsFictional)
                    diagnostics.Add(new CharacterDefinitionDiagnostic("npc_not_fictional", "NPC must be explicitly marked fictional.", id));
            }
            return Array.AsReadOnly(diagnostics.ToArray());
        }
    }
}
