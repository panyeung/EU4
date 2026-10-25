using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Result of reading or changing a player's relationship with one NPC.</summary>
    public readonly struct RelationshipResult
    {
        public RelationshipResult(CharacterId npcId, int previousValue, int currentValue, bool succeeded, string error)
        {
            NpcId = npcId;
            PreviousValue = previousValue;
            CurrentValue = currentValue;
            Succeeded = succeeded;
            Error = error ?? string.Empty;
        }

        public CharacterId NpcId { get; }
        public int PreviousValue { get; }
        public int CurrentValue { get; }
        public bool Succeeded { get; }
        public string Error { get; }
    }

    /// <summary>Runtime player-to-NPC relationship state; static character definitions remain untouched.</summary>
    public sealed class CharacterRelationships
    {
        public const int MinimumValue = -100;
        public const int MaximumValue = 100;
        private readonly Dictionary<CharacterId, int> _npcValues = new Dictionary<CharacterId, int>();

        public CharacterRelationships()
        {
            foreach (CharacterDefinition npc in CharacterCatalog.Npcs)
                _npcValues.Add(npc.Id, 0);
        }

        public RelationshipResult TryGet(CharacterId npcId)
        {
            if (!_npcValues.TryGetValue(npcId, out int value))
                return new RelationshipResult(npcId, 0, 0, false, "Relationship target must be a known NPC ID.");

            return new RelationshipResult(npcId, value, value, true, string.Empty);
        }

        public RelationshipResult TryChange(CharacterId npcId, int delta)
        {
            if (!_npcValues.TryGetValue(npcId, out int current))
                return new RelationshipResult(npcId, 0, 0, false, "Relationship target must be a known NPC ID.");

            long changed = (long)current + delta;
            int finalValue = changed < MinimumValue ? MinimumValue : changed > MaximumValue ? MaximumValue : (int)changed;
            _npcValues[npcId] = finalValue;
            return new RelationshipResult(npcId, current, finalValue, true, string.Empty);
        }
    }
}
