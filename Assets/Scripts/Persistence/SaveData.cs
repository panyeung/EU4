using System;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Persistence
{
    /// <summary>
    /// Flat, versioned root of the single Phase 1 save slot.
    /// Contains only plain data and stable IDs; never Unity objects or asset references.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>Single schema-version source; mirrors <see cref="SaveDataValidator.CurrentVersion"/>.</summary>
        public const int CurrentGameSchemaVersion = SaveDataValidator.CurrentVersion;

        /// <summary>Rules contract version shared by capture, validation and migration.</summary>
        public const int CurrentRulesVersion = 1;

        /// <summary>
        /// Fallback event-content version used only when no catalog is loaded. The
        /// active catalog's <c>contentVersion</c> is authoritative for capture/load.
        /// </summary>
        public const int CurrentEventContentVersion = 1;

        /// <summary>Displayed game build version. Older values are not rejected on load.</summary>
        public const string CurrentGameVersion = "0.1.0";

        public int saveFormatVersion;
        public string gameVersion;
        public int rulesVersion;
        public int eventContentVersion;
        public int year;
        public int month;
        public int day;
        public string locationId;
        public int supplyShortageStage;
        public int sealedLetterStage;
        public int marketTallyStage;
        public RelationshipSaveEntry[] relationships;
        public RandomStateSaveData randomState;
        public EventRuntimeSaveData eventRuntime;

        public SaveData()
        {
            saveFormatVersion = SaveDataValidator.CurrentVersion;
            gameVersion = CurrentGameVersion;
            rulesVersion = CurrentRulesVersion;
            eventContentVersion = CurrentEventContentVersion;
            randomState = RandomStateManager.Create(0UL).CaptureState();
            eventRuntime = new EventRuntimeSaveData();
        }
    }

    /// <summary>
    /// Presence probe parsed from raw save JSON before <see cref="SaveData"/>.
    /// Unity's <c>JsonUtility.FromJson</c> runs the parameterless constructor and
    /// field initializers, so a v2 file that omits a field would otherwise be
    /// indistinguishable from that field's constructor default. Scalar fields here
    /// stay zero/empty when absent. Nested <c>[Serializable]</c> classes are still
    /// materialized by JsonUtility even when their property is absent, so callers
    /// must pair this probe with a root-property check on the raw JSON.
    /// </summary>
    [Serializable]
    public sealed class SaveDataPresenceProbe
    {
        public int saveFormatVersion;
        public string gameVersion;
        public int rulesVersion;
        public int eventContentVersion;
        public RandomStateSaveData randomState;
        public EventRuntimeSaveData eventRuntime;
    }

    /// <summary>One player-to-NPC relationship value, keyed by stable character ID.</summary>
    [Serializable]
    public sealed class RelationshipSaveEntry
    {
        public string characterId;
        public int value;
    }

    /// <summary>Stable phases in which a save is allowed.</summary>
    public enum StableGamePhase
    {
        Management = 0,
        AwaitingEventChoice = 1
    }

    /// <summary>Version-neutral persistence DTO for deterministic event runtime state.</summary>
    [Serializable]
    public sealed class EventRuntimeSaveData
    {
        public int phase;
        public string[] pendingEventIds;
        public EventCooldownSaveEntry[] cooldowns;
        public string[] onceEventIds;
        public EventHistorySaveEntry[] history;
        public string[] committedResultIds;

        public EventRuntimeSaveData()
        {
            phase = (int)StableGamePhase.Management;
            pendingEventIds = new string[0];
            cooldowns = new EventCooldownSaveEntry[0];
            onceEventIds = new string[0];
            history = new EventHistorySaveEntry[0];
            committedResultIds = new string[0];
        }
    }

    [Serializable]
    public sealed class EventCooldownSaveEntry
    {
        public string eventId;
        public int availableAtMonth;
    }

    [Serializable]
    public sealed class EventHistorySaveEntry
    {
        public string eventId;
        public string optionId;
        public string resultId;
        public int year;
        public int month;
        public string summary;
    }
}
