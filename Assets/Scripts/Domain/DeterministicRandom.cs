using System;
using System.Globalization;

namespace SengokuChronicle.Domain
{
    /// <summary>Stable stream IDs; numeric values are part of the save compatibility contract.</summary>
    public enum RandomStreamKind
    {
        CharacterGrowth = 1,
        Events = 2,
        Diplomacy = 3,
        Missions = 4,
        WorldSimulation = 5
    }

    /// <summary>JsonUtility-friendly, versioned state for one deterministic stream.</summary>
    [Serializable]
    public sealed class RandomStreamSaveEntry
    {
        public string streamId;
        public int algorithmVersion;
        public string state0Hex;
        public string state1Hex;
    }

    /// <summary>Complete random state in a fixed seed + stream-ID order.</summary>
    [Serializable]
    public sealed class RandomStateSaveData
    {
        public string initialSeedHex;
        public RandomStreamSaveEntry[] streams;
    }

    /// <summary>
    /// A deterministic xorshift128+ stream (algorithm version 1). Its two 64-bit
    /// words are captured as invariant, fixed-width hexadecimal strings so Unity's
    /// JsonUtility never narrows them to a signed JSON number.
    /// </summary>
    public sealed class DeterministicRandomStream
    {
        public const int CurrentAlgorithmVersion = 1;
        private ulong _state0;
        private ulong _state1;

        private DeterministicRandomStream(ulong state0, ulong state1)
        {
            _state0 = state0;
            _state1 = state1;
        }

        internal static DeterministicRandomStream Create(ulong seed, RandomStreamKind kind)
        {
            ulong split = seed ^ unchecked(0x9E3779B97F4A7C15UL * (ulong)(int)kind);
            ulong first = NextSplitMix64(ref split);
            ulong second = NextSplitMix64(ref split);
            if ((first | second) == 0UL)
                second = 0xD1B54A32D192ED03UL;
            return new DeterministicRandomStream(first, second);
        }

        /// <summary>Returns the next xorshift128+ output and advances only this stream.</summary>
        public ulong NextUInt64()
        {
            unchecked
            {
                ulong first = _state0;
                ulong second = _state1;
                ulong value = first + second;
                second ^= first;
                _state0 = RotateLeft(first, 55) ^ second ^ (second << 14);
                _state1 = RotateLeft(second, 36);
                return value;
            }
        }

        /// <summary>
        /// Unbiased integer in [0, exclusiveMax). False for non-positive bounds;
        /// a rejected bound does not consume any random state.
        /// </summary>
        public bool TryNextInt(int exclusiveMax, out int value)
        {
            value = 0;
            if (exclusiveMax <= 0)
                return false;

            ulong bound = (uint)exclusiveMax;
            ulong threshold = unchecked(0UL - bound) % bound;
            ulong sample;
            do
            {
                sample = NextUInt64();
            }
            while (sample < threshold);

            value = (int)(sample % bound);
            return true;
        }

        /// <summary>Returns an unbiased integer or throws without consuming state for an invalid bound.</summary>
        public int NextInt(int exclusiveMax)
        {
            if (!TryNextInt(exclusiveMax, out int value))
                throw new ArgumentOutOfRangeException(nameof(exclusiveMax), "The exclusive maximum must be positive.");
            return value;
        }

        public RandomStreamSaveEntry CaptureState(RandomStreamKind kind)
        {
            return new RandomStreamSaveEntry
            {
                streamId = RandomStateManager.StableId(kind),
                algorithmVersion = CurrentAlgorithmVersion,
                state0Hex = _state0.ToString("X16", CultureInfo.InvariantCulture),
                state1Hex = _state1.ToString("X16", CultureInfo.InvariantCulture)
            };
        }

        internal static bool TryRestore(RandomStreamSaveEntry entry, out DeterministicRandomStream stream)
        {
            stream = null;
            if (entry == null || entry.algorithmVersion != CurrentAlgorithmVersion ||
                !TryParseStateWord(entry.state0Hex, out ulong state0) ||
                !TryParseStateWord(entry.state1Hex, out ulong state1) ||
                (state0 | state1) == 0UL)
                return false;

            stream = new DeterministicRandomStream(state0, state1);
            return true;
        }

        private static bool TryParseStateWord(string text, out ulong value)
        {
            value = 0UL;
            return text != null && text.Length == 16 &&
                   ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
        }

        private static ulong RotateLeft(ulong value, int shift) =>
            (value << shift) | (value >> (64 - shift));

        private static ulong NextSplitMix64(ref ulong state)
        {
            unchecked
            {
                ulong value = state += 0x9E3779B97F4A7C15UL;
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }
    }

    /// <summary>Owns the fixed, independent random streams used by game systems.</summary>
    public sealed class RandomStateManager
    {
        private static readonly RandomStreamKind[] StableKinds =
        {
            RandomStreamKind.CharacterGrowth,
            RandomStreamKind.Events,
            RandomStreamKind.Diplomacy,
            RandomStreamKind.Missions,
            RandomStreamKind.WorldSimulation
        };

        private readonly DeterministicRandomStream[] _streams;

        private RandomStateManager(ulong initialSeed)
        {
            InitialSeed = initialSeed;
            _streams = new DeterministicRandomStream[StableKinds.Length];
            for (int i = 0; i < StableKinds.Length; i++)
                _streams[i] = DeterministicRandomStream.Create(initialSeed, StableKinds[i]);
        }

        public ulong InitialSeed { get; }

        public static RandomStateManager Create(ulong initialSeed) => new RandomStateManager(initialSeed);

        public DeterministicRandomStream GetStream(RandomStreamKind kind)
        {
            if (!TryIndex(kind, out int index))
                throw new ArgumentOutOfRangeException(nameof(kind), "Unknown random stream kind.");
            return _streams[index];
        }

        public RandomStateSaveData CaptureState()
        {
            var entries = new RandomStreamSaveEntry[StableKinds.Length];
            for (int i = 0; i < StableKinds.Length; i++)
                entries[i] = _streams[i].CaptureState(StableKinds[i]);

            return new RandomStateSaveData
            {
                initialSeedHex = InitialSeed.ToString("X16", CultureInfo.InvariantCulture),
                streams = entries
            };
        }

        public static bool TryRestore(RandomStateSaveData save, out RandomStateManager manager)
        {
            manager = null;
            if (save == null || save.initialSeedHex == null || save.initialSeedHex.Length != 16 ||
                !ulong.TryParse(save.initialSeedHex, NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out ulong initialSeed) ||
                save.streams == null || save.streams.Length != StableKinds.Length)
                return false;

            var restored = new RandomStateManager(initialSeed);
            var seen = new bool[StableKinds.Length];
            for (int i = 0; i < save.streams.Length; i++)
            {
                RandomStreamSaveEntry entry = save.streams[i];
                if (entry == null || !TryParseStableId(entry.streamId, out RandomStreamKind kind) ||
                    !TryIndex(kind, out int index) || seen[index] ||
                    !DeterministicRandomStream.TryRestore(entry, out DeterministicRandomStream stream))
                    return false;

                restored._streams[index] = stream;
                seen[index] = true;
            }

            for (int i = 0; i < seen.Length; i++)
                if (!seen[i]) return false;

            manager = restored;
            return true;
        }

        public static string StableId(RandomStreamKind kind)
        {
            switch (kind)
            {
                case RandomStreamKind.CharacterGrowth: return "character-growth";
                case RandomStreamKind.Events: return "events";
                case RandomStreamKind.Diplomacy: return "diplomacy";
                case RandomStreamKind.Missions: return "missions";
                case RandomStreamKind.WorldSimulation: return "world-simulation";
                default: return string.Empty;
            }
        }

        private static bool TryParseStableId(string stableId, out RandomStreamKind kind)
        {
            for (int i = 0; i < StableKinds.Length; i++)
            {
                if (string.Equals(stableId, StableId(StableKinds[i]), StringComparison.Ordinal))
                {
                    kind = StableKinds[i];
                    return true;
                }
            }
            kind = default;
            return false;
        }

        private static bool TryIndex(RandomStreamKind kind, out int index)
        {
            for (int i = 0; i < StableKinds.Length; i++)
            {
                if (StableKinds[i] == kind)
                {
                    index = i;
                    return true;
                }
            }
            index = -1;
            return false;
        }
    }
}
