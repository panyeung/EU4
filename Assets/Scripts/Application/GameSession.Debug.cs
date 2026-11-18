// Unity 6.6 deprecates the DEVELOPMENT_BUILD directive (UAC0009). The approved
// Debug Mode plan requires a compile-time boundary that exists only in the Editor
// and development builds, so the directive stays and its deprecation notice is
// silenced for this file only.
#pragma warning disable UAC0009
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;

namespace SengokuChronicle.Application
{
    /// <summary>
    /// Debug Mode lifecycle and the minimal session-internal seams the isolated debug
    /// session needs. Every member of this file is compiled only for the Editor and
    /// development builds, so a release player contains neither the entry points nor the
    /// seam methods.
    /// <para>
    /// The debug session never touches <c>SaveSlotStore</c>: it starts from the existing
    /// demo initialisation and lives only in memory. Any normal start path clears the
    /// debug marker, so the save/load blocks below can never outlive the session.
    /// </para>
    /// </summary>
    public sealed partial class GameSession
    {
        private DebugSession _debugSession;
        private int _debugBroadcastSuppression;

        /// <summary>True while the live session is the isolated in-memory debug session.</summary>
        public bool IsDebugSessionActive => _debugSession != null && _debugSession.IsActive;

        /// <summary>
        /// The live debug session, or null. A caller that keeps an older reference must
        /// check <see cref="DebugSession.IsActive"/> before submitting a command.
        /// </summary>
        public DebugSession CurrentDebugSession => _debugSession;

        /// <summary>
        /// Starts an isolated debug session from the existing SC-010 demo roster. It never
        /// reads, writes or backs up a player save.
        /// </summary>
        public DebugSession BeginDebugSession() => BeginDebugSession(DefaultRandomSeed);

        /// <summary>Starts an isolated debug session with a caller-selected deterministic seed.</summary>
        public DebugSession BeginDebugSession(ulong initialSeed)
        {
            // The demo initialisation runs with the marker cleared, so its state change must
            // not be broadcast as "no debug session": listeners would see a false inactive
            // window and could close themselves mid-entry.
            RunInitWithoutBroadcast(InitStandardDemo, initialSeed);
            var created = new DebugSession(this);
            _debugSession = created;
            created.Activate();
            SessionStateChanged?.Invoke();
            return created;
        }

        /// <summary>
        /// Restarts the isolated debug session from the demo roster with fresh debug IDs.
        /// The previous <see cref="DebugSession"/> handle becomes inactive.
        /// </summary>
        public void ResetDebugSession()
        {
            RunInitWithoutBroadcast(InitStandardDemo, DefaultRandomSeed);
            var created = new DebugSession(this);
            _debugSession = created;
            created.Activate();
            SessionStateChanged?.Invoke();
        }

        /// <summary>
        /// Leaves the debug session and discards every injected person, kinship, date,
        /// resource and event change by resetting to the ordinary new-game state.
        /// </summary>
        public void EndDebugSession()
        {
            bool wasActive = IsDebugSessionActive;
            if (_debugSession != null)
                _debugSession.Deactivate();
            _debugSession = null;
            RunInitWithoutBroadcast(InitNewGame);
            if (wasActive)
                SessionStateChanged?.Invoke();
        }

        /// <summary>
        /// Runs one session initialisation while holding back its state broadcast, so a debug
        /// entry, reset or exit publishes exactly one coherent state instead of a transient
        /// inactive one. The broadcast is restored even when the initialisation throws.
        /// </summary>
        private void RunInitWithoutBroadcast<T>(Action<T> init, T argument)
        {
            _debugBroadcastSuppression++;
            try
            {
                init(argument);
            }
            finally
            {
                _debugBroadcastSuppression--;
            }
        }

        private void RunInitWithoutBroadcast(Action init)
        {
            _debugBroadcastSuppression++;
            try
            {
                init();
            }
            finally
            {
                _debugBroadcastSuppression--;
            }
        }

        /// <summary>
        /// Publishes the session state unless an init still owns the one coherent broadcast.
        /// </summary>
        private void PublishSessionStateChanged()
        {
            if (_debugBroadcastSuppression > 0)
                return;
            SessionStateChanged?.Invoke();
        }

        /// <summary>Called by every session (re)start so no debug marker can survive it.</summary>
        private void ClearDebugSessionOnSessionStart()
        {
            if (_debugSession != null)
                _debugSession.Deactivate();
            _debugSession = null;
        }

        /// <summary>EditMode seam: counts capture attempts so a test can prove a read never builds save data.</summary>
        internal Action CaptureSaveObserved { get; set; }

        internal IndustryState DebugIndustryState => EnsureIndustryState();

        internal void DebugReplaceIndustryState(IndustryState state) => _industryState = state;

        internal FamilyResearchState DebugResearchState => EnsureResearch();

        internal void DebugReplaceResearchState(FamilyResearchState state) => _researchState = state;

        internal MarketState DebugMarketState => EnsureMarket();

        internal void DebugReplaceMarketState(MarketState state) => _marketState = state;

        internal EventRuntimeState DebugEventRuntime => EnsureEventRuntimeState();

        internal ShadowEscortCommission DebugCommission => _commission;

        internal DailyActionState DebugDailyActions => EnsureDailyActions();

        internal MissionInjuryState DebugInjuries => EnsureInjuries();

        internal MissionHistoryState DebugMissionHistory => EnsureMissionHistory();

        internal TacticalMissionState DebugTacticalMission => _tacticalMission;

        internal IReadOnlyList<FamilyRelationshipEntry> DebugFamilyRelationships =>
            _familyRelationships != null
                ? _familyRelationships.Entries
                : (IReadOnlyList<FamilyRelationshipEntry>)Array.Empty<FamilyRelationshipEntry>();

        internal void DebugClearMonthReports()
        {
            LastIndustryReport = null;
            LastIndustryMessage = string.Empty;
            LastMarketReport = null;
            LastResearchReport = null;
            LastResearchMessage = string.Empty;
            LastEventMessage = string.Empty;
            LastMonthTurnOutcome = MonthTurnOutcome.Succeeded;
        }

        internal void DebugPublishStateChanged() => SessionStateChanged?.Invoke();
    }
}
#endif
#pragma warning restore UAC0009
