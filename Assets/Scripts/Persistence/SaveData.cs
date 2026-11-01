using System;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Flat, versioned root of the single Phase 1 save slot.
    /// Contains only plain data and stable IDs; never Unity objects or asset references.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public int saveFormatVersion;
        public int year;
        public int month;
        public int day;
        public string locationId;
        public int supplyShortageStage;
        public int sealedLetterStage;
        public int marketTallyStage;
        public RelationshipSaveEntry[] relationships;
    }

    /// <summary>One player-to-NPC relationship value, keyed by stable character ID.</summary>
    [Serializable]
    public sealed class RelationshipSaveEntry
    {
        public string characterId;
        public int value;
    }
}
