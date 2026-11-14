using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Read-only presentation status for one autonomous pilot.</summary>
    public enum NpcAutonomyStatus
    {
        Idle = 0,
        Executing = 1,
        Completed = 2,
        Interrupted = 3
    }

    /// <summary>The four approved autonomy command results.</summary>
    public enum NpcAutonomyCommandOutcomeKind
    {
        Accepted = 0,
        Deferred = 1,
        Rejected = 2,
        Interrupted = 3
    }

    /// <summary>Why a pilot was (or was not) evaluated.</summary>
    public enum NpcAutonomyTrigger
    {
        Initialization = 0,
        ActionCompleted = 1,
        ActionInterrupted = 2,
        EligibilityChanged = 3,
        DailyCheck = 4
    }

    /// <summary>The two bounded memory kinds; at most one of each per pilot.</summary>
    public enum NpcActionMemoryKind
    {
        Completed = 0,
        Interrupted = 1
    }

    /// <summary>
    /// Three clamped integer needs. Values are durable simulation state; the approved
    /// growth and completion reductions live in <see cref="NpcAutonomyRules"/>.
    /// </summary>
    public readonly struct NpcNeedValues : IEquatable<NpcNeedValues>
    {
        public const int Minimum = 0;
        public const int Maximum = 100;

        public NpcNeedValues(int rest, int learning, int duty)
        {
            if (rest < Minimum || rest > Maximum)
                throw new ArgumentOutOfRangeException(nameof(rest));
            if (learning < Minimum || learning > Maximum)
                throw new ArgumentOutOfRangeException(nameof(learning));
            if (duty < Minimum || duty > Maximum)
                throw new ArgumentOutOfRangeException(nameof(duty));
            Rest = rest;
            Learning = learning;
            Duty = duty;
        }

        public int Rest { get; }
        public int Learning { get; }
        public int Duty { get; }

        public static NpcNeedValues Clamped(int rest, int learning, int duty) =>
            new NpcNeedValues(Clamp(rest), Clamp(learning), Clamp(duty));

        public NpcNeedValues WithDailyGrowth() =>
            WithDailyGrowth(1);

        public NpcNeedValues WithDailyGrowth(int days)
        {
            if (days <= 0)
                return this;
            return Clamped(Rest + NpcAutonomyRules.DailyRestGrowth * days,
                Learning + NpcAutonomyRules.DailyLearningGrowth * days,
                Duty + NpcAutonomyRules.DailyDutyGrowth * days);
        }

        public NpcNeedValues WithCompletedAction(DailyActionKind kind)
        {
            int rest = Rest;
            int learning = Learning;
            int duty = Duty;
            switch (kind)
            {
                case DailyActionKind.Rest:
                    rest = Clamp(rest - NpcAutonomyRules.CompletedActionNeedReduction);
                    break;
                case DailyActionKind.Training:
                    learning = Clamp(learning - NpcAutonomyRules.CompletedActionNeedReduction);
                    break;
                case DailyActionKind.Work:
                    duty = Clamp(duty - NpcAutonomyRules.CompletedActionNeedReduction);
                    break;
            }
            return new NpcNeedValues(rest, learning, duty);
        }

        /// <summary>The need matched by one action kind; Travel matches no need.</summary>
        public int For(DailyActionKind kind)
        {
            switch (kind)
            {
                case DailyActionKind.Rest: return Rest;
                case DailyActionKind.Training: return Learning;
                case DailyActionKind.Work: return Duty;
                default: return 0;
            }
        }

        public string Describe() => "休息 " + Rest + "／学艺 " + Learning + "／家务 " + Duty;

        public bool Equals(NpcNeedValues other) =>
            Rest == other.Rest && Learning == other.Learning && Duty == other.Duty;

        public override bool Equals(object obj) => obj is NpcNeedValues other && Equals(other);
        public override int GetHashCode() => (Rest * 101 + Learning) * 101 + Duty;
        public static bool operator ==(NpcNeedValues left, NpcNeedValues right) => left.Equals(right);
        public static bool operator !=(NpcNeedValues left, NpcNeedValues right) => !left.Equals(right);

        private static int Clamp(int value) => value < Minimum ? Minimum : value > Maximum ? Maximum : value;
    }

    /// <summary>Fixed authored preference weights for one pilot.</summary>
    public readonly struct NpcAutonomyProfile
    {
        public NpcAutonomyProfile(PersonId personId, string displayName, int restWeight,
            int trainingWeight, int workWeight)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            RestWeight = restWeight;
            TrainingWeight = trainingWeight;
            WorkWeight = workWeight;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public int RestWeight { get; }
        public int TrainingWeight { get; }
        public int WorkWeight { get; }

        public int WeightFor(DailyActionKind kind)
        {
            switch (kind)
            {
                case DailyActionKind.Rest: return RestWeight;
                case DailyActionKind.Training: return TrainingWeight;
                case DailyActionKind.Work: return WorkWeight;
                default: return 0;
            }
        }

        public string PreferenceSummary =>
            "偏好 休息×" + RestWeight + "／训练×" + TrainingWeight + "／工作×" + WorkWeight;
    }

    /// <summary>One candidate in the approved Rest, Training, Work order.</summary>
    public readonly struct NpcActionCandidate
    {
        public NpcActionCandidate(DailyActionKind kind, bool isEligible, int baseScore,
            int memoryAdjustment, string explanation)
        {
            Kind = kind;
            IsEligible = isEligible;
            BaseScore = baseScore;
            MemoryAdjustment = memoryAdjustment;
            Score = baseScore + memoryAdjustment;
            Explanation = explanation ?? string.Empty;
        }

        public DailyActionKind Kind { get; }
        public bool IsEligible { get; }

        /// <summary>Final integer score including the current memory adjustment.</summary>
        public int Score { get; }

        public int BaseScore { get; }
        public int MemoryAdjustment { get; }
        public int FinalScore => Score;
        public string KindName => DailyActionState.KindName(Kind);

        /// <summary>Score explanation for an eligible candidate or the exclusion reason.</summary>
        public string Explanation { get; }
    }

    /// <summary>Complete read-only result of one deterministic evaluation.</summary>
    public sealed class NpcAutonomyEvaluation
    {
        internal NpcAutonomyEvaluation(PersonId personId, NpcNeedValues needs,
            NpcAutonomyProfile profile, bool isRegistered, bool hasKnownLocation,
            bool hasActiveAction, IReadOnlyList<NpcActionCandidate> candidates,
            bool hasSelection, NpcActionCandidate selected, string summary)
        {
            PersonId = personId;
            Needs = needs;
            Profile = profile;
            IsRegistered = isRegistered;
            HasKnownLocation = hasKnownLocation;
            HasActiveAction = hasActiveAction;
            Candidates = candidates ?? Array.Empty<NpcActionCandidate>();
            HasSelection = hasSelection;
            Selected = selected;
            Summary = summary ?? string.Empty;
        }

        public PersonId PersonId { get; }
        public NpcNeedValues Needs { get; }
        public NpcAutonomyProfile Profile { get; }
        public bool IsRegistered { get; }
        public bool HasKnownLocation { get; }
        public bool HasActiveAction { get; }
        public IReadOnlyList<NpcActionCandidate> Candidates { get; }
        public bool HasSelection { get; }
        public NpcActionCandidate Selected { get; }
        public string Summary { get; }
    }

    /// <summary>
    /// Session-only terminal outcome of the most recently finished autonomous action.
    /// A new plan can begin immediately afterwards, so the Inspector needs this value
    /// alongside the current execution state.
    /// </summary>
    public readonly struct NpcAutonomyOutcomeRecord
    {
        public NpcAutonomyOutcomeRecord(NpcAutonomyStatus status, DailyActionKind action,
            GameDate date, string reason)
        {
            Status = status;
            Action = action;
            Date = date;
            Reason = reason ?? string.Empty;
        }

        public NpcAutonomyStatus Status { get; }
        public DailyActionKind Action { get; }
        public GameDate Date { get; }
        public string Reason { get; }
        public string StatusName => NpcAutonomyRules.StatusName(Status);
        public string ActionName => DailyActionState.KindName(Action);
    }

    /// <summary>
    /// Immutable personal action memory. Only the kind, action, date and an optional
    /// interruption reason are stored; display text is always generated from them.
    /// </summary>
    public readonly struct NpcActionMemory : IEquatable<NpcActionMemory>
    {
        public NpcActionMemory(PersonId personId, NpcActionMemoryKind kind,
            DailyActionKind action, GameDate date, string reason)
        {
            PersonId = personId;
            Kind = kind;
            Action = action;
            Date = date;
            Reason = reason ?? string.Empty;
        }

        public PersonId PersonId { get; }
        public NpcActionMemoryKind Kind { get; }
        public DailyActionKind Action { get; }
        public GameDate Date { get; }
        public string Reason { get; }
        public string KindName => NpcAutonomyRules.MemoryKindName(Kind);
        public string ActionName => DailyActionState.KindName(Action);

        public bool Equals(NpcActionMemory other) =>
            PersonId == other.PersonId && Kind == other.Kind && Action == other.Action &&
            Date == other.Date && string.Equals(Reason, other.Reason, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is NpcActionMemory other && Equals(other);

        public override int GetHashCode() =>
            ((PersonId.GetHashCode() * 31 + (int)Kind) * 31 + (int)Action) * 31 +
            Date.GetHashCode();

        public static bool operator ==(NpcActionMemory left, NpcActionMemory right) =>
            left.Equals(right);

        public static bool operator !=(NpcActionMemory left, NpcActionMemory right) =>
            !left.Equals(right);
    }

    /// <summary>One bounded, session-only decision log entry.</summary>
    public readonly struct NpcAutonomyDecisionRecord
    {
        public NpcAutonomyDecisionRecord(GameDate date, PersonId personId,
            NpcAutonomyCommandOutcomeKind outcome, DailyActionKind action, bool hasAction,
            NpcAutonomyTrigger trigger, string reason)
        {
            Date = date;
            PersonId = personId;
            Outcome = outcome;
            Action = action;
            HasAction = hasAction;
            Trigger = trigger;
            Reason = reason ?? string.Empty;
        }

        public GameDate Date { get; }
        public PersonId PersonId { get; }
        public NpcAutonomyCommandOutcomeKind Outcome { get; }
        public DailyActionKind Action { get; }
        public bool HasAction { get; }
        public NpcAutonomyTrigger Trigger { get; }
        public string Reason { get; }

        public string OutcomeName => NpcAutonomyRules.OutcomeName(Outcome);
        public string TriggerName => NpcAutonomyRules.TriggerName(Trigger);
        public string StatusName => NpcAutonomyRules.StatusName(StatusFor(Outcome));

        private static NpcAutonomyStatus StatusFor(NpcAutonomyCommandOutcomeKind outcome)
        {
            switch (outcome)
            {
                case NpcAutonomyCommandOutcomeKind.Accepted: return NpcAutonomyStatus.Executing;
                case NpcAutonomyCommandOutcomeKind.Interrupted: return NpcAutonomyStatus.Interrupted;
                default: return NpcAutonomyStatus.Idle;
            }
        }
    }

    /// <summary>Durable needs plus transient diagnostics for one pilot.</summary>
    public sealed class NpcAutonomyPersonState
    {
        internal NpcAutonomyPersonState(PersonId personId, NpcNeedValues needs)
        {
            PersonId = personId;
            Needs = needs;
            Status = NpcAutonomyStatus.Idle;
        }

        private NpcActionMemory? _completedMemory;
        private NpcActionMemory? _interruptedMemory;

        public PersonId PersonId { get; }
        public NpcNeedValues Needs { get; internal set; }
        public NpcAutonomyStatus Status { get; internal set; }

        /// <summary>
        /// At most two durable memories in stable kind order: last completed action,
        /// then last interrupted action.
        /// </summary>
        public IReadOnlyList<NpcActionMemory> Memories
        {
            get
            {
                var snapshot = new List<NpcActionMemory>(2);
                if (_completedMemory.HasValue)
                    snapshot.Add(_completedMemory.Value);
                if (_interruptedMemory.HasValue)
                    snapshot.Add(_interruptedMemory.Value);
                return snapshot.AsReadOnly();
            }
        }

        /// <summary>Reads one memory kind without exposing mutable storage.</summary>
        public bool TryGetMemory(NpcActionMemoryKind kind, out NpcActionMemory memory)
        {
            switch (kind)
            {
                case NpcActionMemoryKind.Completed when _completedMemory.HasValue:
                    memory = _completedMemory.Value;
                    return true;
                case NpcActionMemoryKind.Interrupted when _interruptedMemory.HasValue:
                    memory = _interruptedMemory.Value;
                    return true;
                default:
                    memory = default;
                    return false;
            }
        }

        /// <summary>Replaces only the matching kind; the other kind is preserved.</summary>
        internal void RecordMemory(NpcActionMemory memory)
        {
            switch (memory.Kind)
            {
                case NpcActionMemoryKind.Completed:
                    _completedMemory = memory;
                    break;
                case NpcActionMemoryKind.Interrupted:
                    _interruptedMemory = memory;
                    break;
            }
        }

        /// <summary>
        /// Last evaluation used for goal selection (or the last rejection reason when
        /// no candidate was eligible). Ordinary day growth never overwrites it.
        /// </summary>
        public NpcAutonomyEvaluation LatestEvaluation { get; internal set; }

        /// <summary>Most recent completed or interrupted action, if any.</summary>
        public NpcAutonomyOutcomeRecord LastOutcome { get; internal set; }

        public bool HasLastOutcome { get; internal set; }

        internal NpcAutonomyPersonState Clone() =>
            new NpcAutonomyPersonState(PersonId, Needs)
            {
                Status = Status,
                LatestEvaluation = LatestEvaluation,
                LastOutcome = LastOutcome,
                HasLastOutcome = HasLastOutcome,
                _completedMemory = _completedMemory,
                _interruptedMemory = _interruptedMemory
            };
    }

    /// <summary>
    /// Session-owned autonomy state. Needs are durable; evaluations, statuses and the
    /// bounded decision list are intentionally session-only. People are kept in ordinal
    /// person-ID order so iteration never depends on insertion or dictionary order.
    /// </summary>
    public sealed class NpcAutonomyState
    {
        public const int MaxDecisionRecords = 24;

        private readonly SortedDictionary<string, NpcAutonomyPersonState> _persons =
            new SortedDictionary<string, NpcAutonomyPersonState>(StringComparer.Ordinal);
        private readonly List<NpcAutonomyDecisionRecord> _decisions =
            new List<NpcAutonomyDecisionRecord>();

        public static NpcAutonomyState CreateEmpty() => new NpcAutonomyState();

        /// <summary>Creates rows only for the two approved pilots present in the roster.</summary>
        public static NpcAutonomyState CreateForRoster(IEnumerable<PersonId> registeredPilotIds)
        {
            var state = new NpcAutonomyState();
            state.Reconcile(registeredPilotIds);
            return state;
        }

        public IReadOnlyList<NpcAutonomyPersonState> Persons
        {
            get
            {
                var snapshot = new List<NpcAutonomyPersonState>(_persons.Count);
                foreach (NpcAutonomyPersonState person in _persons.Values)
                    snapshot.Add(person);
                return snapshot.AsReadOnly();
            }
        }

        public bool Contains(PersonId personId) =>
            !personId.IsEmpty && _persons.ContainsKey(personId.Value);

        public bool TryGetPerson(PersonId personId, out NpcAutonomyPersonState person) =>
            _persons.TryGetValue(personId.Value ?? string.Empty, out person);

        public bool TryGetNeeds(PersonId personId, out NpcNeedValues needs)
        {
            if (_persons.TryGetValue(personId.Value ?? string.Empty, out NpcAutonomyPersonState person))
            {
                needs = person.Needs;
                return true;
            }
            needs = default;
            return false;
        }

        /// <summary>Adds missing valid pilots and removes rows that are no longer valid.</summary>
        public bool Reconcile(IEnumerable<PersonId> registeredPilotIds)
        {
            var registered = new HashSet<PersonId>();
            if (registeredPilotIds != null)
                foreach (PersonId personId in registeredPilotIds)
                    if (NpcAutonomyRules.IsPilot(personId))
                        registered.Add(personId);

            bool changed = false;
            var remove = new List<string>();
            foreach (KeyValuePair<string, NpcAutonomyPersonState> pair in _persons)
                if (!registered.Contains(pair.Value.PersonId))
                    remove.Add(pair.Key);
            foreach (string key in remove)
            {
                _persons.Remove(key);
                changed = true;
            }

            var ordered = new List<PersonId>(registered);
            ordered.Sort((left, right) =>
                string.Compare(left.Value, right.Value, StringComparison.Ordinal));
            foreach (PersonId personId in ordered)
            {
                if (_persons.ContainsKey(personId.Value))
                    continue;
                _persons.Add(personId.Value,
                    new NpcAutonomyPersonState(personId, NpcAutonomyRules.InitialNeedsFor(personId)));
                changed = true;
            }
            return changed;
        }

        /// <summary>Persistence support: add one already-validated pilot row.</summary>
        public bool TryAddRestoredPerson(PersonId personId, NpcNeedValues needs)
        {
            if (!NpcAutonomyRules.IsPilot(personId) || _persons.ContainsKey(personId.Value))
                return false;
            _persons.Add(personId.Value, new NpcAutonomyPersonState(personId, needs));
            return true;
        }

        public void GrowAll(int days)
        {
            if (days <= 0)
                return;
            foreach (NpcAutonomyPersonState person in _persons.Values)
                person.Needs = person.Needs.WithDailyGrowth(days);
        }

        public bool ApplyCompletedAction(PersonId personId, DailyActionKind kind,
            GameDate date, string reason)
        {
            if (!_persons.TryGetValue(personId.Value ?? string.Empty, out NpcAutonomyPersonState person))
                return false;
            person.Needs = person.Needs.WithCompletedAction(kind);
            person.Status = NpcAutonomyStatus.Completed;
            person.LastOutcome = new NpcAutonomyOutcomeRecord(NpcAutonomyStatus.Completed,
                kind, date, reason);
            person.HasLastOutcome = true;
            if (Enum.IsDefined(typeof(DailyActionKind), kind) && date.IsValid)
                person.RecordMemory(new NpcActionMemory(personId, NpcActionMemoryKind.Completed,
                    kind, date, string.Empty));
            return true;
        }

        /// <summary>
        /// Records one successfully committed interruption. Needs are unchanged; the
        /// interrupted memory replaces only the previous interrupted record.
        /// </summary>
        public bool ApplyInterruptedAction(PersonId personId, DailyActionKind kind,
            GameDate date, string reason)
        {
            if (!_persons.TryGetValue(personId.Value ?? string.Empty, out NpcAutonomyPersonState person))
                return false;
            person.Status = NpcAutonomyStatus.Interrupted;
            person.LastOutcome = new NpcAutonomyOutcomeRecord(NpcAutonomyStatus.Interrupted,
                kind, date, reason);
            person.HasLastOutcome = true;
            if (Enum.IsDefined(typeof(DailyActionKind), kind) && date.IsValid)
                person.RecordMemory(new NpcActionMemory(personId, NpcActionMemoryKind.Interrupted,
                    kind, date, reason));
            return true;
        }

        /// <summary>Persistence support: restore one already-validated memory.</summary>
        public bool TryRestoreMemory(PersonId personId, NpcActionMemoryKind kind,
            DailyActionKind action, GameDate date, string reason)
        {
            if (!Enum.IsDefined(typeof(NpcActionMemoryKind), kind) ||
                !Enum.IsDefined(typeof(DailyActionKind), action) || !date.IsValid ||
                !_persons.TryGetValue(personId.Value ?? string.Empty, out NpcAutonomyPersonState person))
                return false;
            person.RecordMemory(new NpcActionMemory(personId, kind, action, date, reason));
            return true;
        }

        /// <summary>Replaces one memory kind for a registered pilot.</summary>
        public bool RecordMemory(PersonId personId, NpcActionMemoryKind kind,
            DailyActionKind action, GameDate date, string reason) =>
            TryRestoreMemory(personId, kind, action, date, reason);

        public bool SetLastOutcome(PersonId personId, NpcAutonomyOutcomeRecord outcome)
        {
            if (!_persons.TryGetValue(personId.Value ?? string.Empty, out NpcAutonomyPersonState person))
                return false;
            person.LastOutcome = outcome;
            person.HasLastOutcome = true;
            return true;
        }

        public bool SetStatus(PersonId personId, NpcAutonomyStatus status)
        {
            if (!_persons.TryGetValue(personId.Value ?? string.Empty, out NpcAutonomyPersonState person))
                return false;
            person.Status = status;
            return true;
        }

        public bool SetLatestEvaluation(PersonId personId, NpcAutonomyEvaluation evaluation)
        {
            if (!_persons.TryGetValue(personId.Value ?? string.Empty, out NpcAutonomyPersonState person))
                return false;
            person.LatestEvaluation = evaluation;
            return true;
        }

        public void RecordDecision(NpcAutonomyDecisionRecord record)
        {
            _decisions.Add(record);
            if (_decisions.Count > MaxDecisionRecords)
                _decisions.RemoveRange(0, _decisions.Count - MaxDecisionRecords);
        }

        /// <summary>Newest-first read-only decision snapshot.</summary>
        public IReadOnlyList<NpcAutonomyDecisionRecord> Decisions
        {
            get
            {
                var snapshot = new List<NpcAutonomyDecisionRecord>(_decisions.Count);
                for (int i = _decisions.Count - 1; i >= 0; i--)
                    snapshot.Add(_decisions[i]);
                return snapshot.AsReadOnly();
            }
        }

        public IReadOnlyList<NpcAutonomyDecisionRecord> RecentDecisionsFor(PersonId personId,
            int maximum)
        {
            var snapshot = new List<NpcAutonomyDecisionRecord>();
            if (maximum <= 0)
                return snapshot.AsReadOnly();
            for (int i = _decisions.Count - 1; i >= 0 && snapshot.Count < maximum; i--)
                if (_decisions[i].PersonId == personId)
                    snapshot.Add(_decisions[i]);
            return snapshot.AsReadOnly();
        }

        public NpcAutonomyState Clone()
        {
            var clone = new NpcAutonomyState();
            foreach (NpcAutonomyPersonState person in _persons.Values)
                clone._persons.Add(person.PersonId.Value, person.Clone());
            clone._decisions.AddRange(_decisions);
            return clone;
        }
    }

    /// <summary>Immutable read-only projection consumed by the Inspector.</summary>
    public sealed class NpcAutonomySnapshot
    {
        public NpcAutonomySnapshot(PersonId personId, string displayName, NpcNeedValues needs,
            NpcAutonomyProfile profile, NpcAutonomyStatus status, DailyActionTask activeAction,
            NpcAutonomyEvaluation latestEvaluation, NpcAutonomyEvaluation currentEvaluation,
            NpcAutonomyOutcomeRecord lastOutcome, bool hasLastOutcome,
            IReadOnlyList<NpcActionMemory> memories,
            IReadOnlyList<NpcAutonomyDecisionRecord> recentDecisions)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Needs = needs;
            Profile = profile;
            Status = status;
            ActiveAction = activeAction;
            LatestEvaluation = latestEvaluation;
            CurrentEvaluation = currentEvaluation;
            LastOutcome = lastOutcome;
            HasLastOutcome = hasLastOutcome;
            Memories = memories ?? Array.Empty<NpcActionMemory>();
            RecentDecisions = recentDecisions ?? Array.Empty<NpcAutonomyDecisionRecord>();
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public NpcNeedValues Needs { get; }
        public NpcAutonomyProfile Profile { get; }
        public NpcAutonomyStatus Status { get; }
        public DailyActionTask ActiveAction { get; }
        public bool HasActiveAction => ActiveAction != null;
        public int RemainingDays => ActiveAction != null ? ActiveAction.RemainingDays : 0;

        /// <summary>Evaluation that selected (or rejected) the current goal.</summary>
        public NpcAutonomyEvaluation LatestEvaluation { get; }

        /// <summary>
        /// Pure projection of the current needs and execution state. This is what the
        /// Inspector labels as current diagnostics; it is never stored back on the state.
        /// </summary>
        public NpcAutonomyEvaluation CurrentEvaluation { get; }

        /// <summary>Last finished action, retained across an immediate replan.</summary>
        public NpcAutonomyOutcomeRecord LastOutcome { get; }

        public bool HasLastOutcome { get; }
        public IReadOnlyList<NpcActionMemory> Memories { get; }
        public IReadOnlyList<NpcAutonomyDecisionRecord> RecentDecisions { get; }
        public string StatusName => NpcAutonomyRules.StatusName(Status);
    }

    /// <summary>
    /// Facts outside one pilot's needs and memories that decide daily-action eligibility:
    /// an injury, an existing industry duty and an empty supply stock. Every false value
    /// means "no restriction", so the default value never blocks Work.
    /// </summary>
    public readonly struct NpcActionEligibility
    {
        public NpcActionEligibility(bool isInjured, bool hasIndustryDuty, bool hasNoSupplies)
        {
            IsInjured = isInjured;
            HasIndustryDuty = hasIndustryDuty;
            HasNoSupplies = hasNoSupplies;
        }

        /// <summary>Not injured, no industry duty and supplies available.</summary>
        public static NpcActionEligibility Unrestricted => default;

        /// <summary>An injured pilot may only select Rest.</summary>
        public bool IsInjured { get; }

        /// <summary>The pilot already holds an industry post, so Work conflicts with it.</summary>
        public bool HasIndustryDuty { get; }

        /// <summary>Industry supplies are exactly zero, so Work is unavailable.</summary>
        public bool HasNoSupplies { get; }
    }

    /// <summary>
    /// Pure deterministic Utility rules for the two approved pilots. Scores are the
    /// matching need multiplied by the fixed integer profile weight; equal scores break
    /// by <see cref="DailyActionKind"/> ascending. No randomness or floating point.
    /// </summary>
    public static class NpcAutonomyRules
    {
        public const string MasatsunaIdValue = "person.sc013.masatsuna";
        public const string NobumasaIdValue = "person.sc013.nobumasa";

        public const int DailyRestGrowth = 4;
        public const int DailyLearningGrowth = 3;
        public const int DailyDutyGrowth = 3;
        public const int CompletedActionNeedReduction = 40;
        public const int MemoryEffectDays = 30;
        public const int CompletedMemoryAdjustment = 12;
        public const int InterruptedMemoryAdjustment = -12;

        public static readonly PersonId MasatsunaId = new PersonId(MasatsunaIdValue);
        public static readonly PersonId NobumasaId = new PersonId(NobumasaIdValue);

        private static readonly DailyActionKind[] CandidateKinds =
        {
            DailyActionKind.Rest,
            DailyActionKind.Training,
            DailyActionKind.Work
        };

        private static readonly NpcAutonomyProfile[] AuthoredProfiles =
        {
            new NpcAutonomyProfile(MasatsunaId, "明智 正綱", 1, 2, 1),
            new NpcAutonomyProfile(NobumasaId, "明智 信正", 1, 1, 2)
        };

        /// <summary>Stable ordinal profile order.</summary>
        public static IReadOnlyList<NpcAutonomyProfile> Profiles => AuthoredProfiles;

        public static IReadOnlyList<DailyActionKind> Candidates => CandidateKinds;

        public static bool IsPilot(PersonId personId) =>
            personId == MasatsunaId || personId == NobumasaId;

        public static bool TryGetProfile(PersonId personId, out NpcAutonomyProfile profile)
        {
            for (int i = 0; i < AuthoredProfiles.Length; i++)
            {
                if (AuthoredProfiles[i].PersonId != personId)
                    continue;
                profile = AuthoredProfiles[i];
                return true;
            }
            profile = default;
            return false;
        }

        public static NpcNeedValues InitialNeedsFor(PersonId personId)
        {
            if (personId == MasatsunaId)
                return new NpcNeedValues(40, 75, 45);
            if (personId == NobumasaId)
                return new NpcNeedValues(45, 40, 75);
            return new NpcNeedValues(0, 0, 0);
        }

        /// <summary>Approved planned duration; Travel is outside M03 and returns zero.</summary>
        public static int PlannedDuration(DailyActionKind kind)
        {
            switch (kind)
            {
                case DailyActionKind.Rest: return 1;
                case DailyActionKind.Training: return 2;
                case DailyActionKind.Work: return 3;
                default: return 0;
            }
        }

        public static NpcAutonomyEvaluation Evaluate(PersonId personId, NpcNeedValues needs,
            bool isRegistered, bool hasKnownLocation, bool hasActiveAction) =>
            Evaluate(personId, needs, null, default, isRegistered, hasKnownLocation,
                hasActiveAction);

        public static NpcAutonomyEvaluation Evaluate(PersonId personId, NpcNeedValues needs,
            IReadOnlyList<NpcActionMemory> memories, GameDate currentDate, bool isRegistered,
            bool hasKnownLocation, bool hasActiveAction) =>
            Evaluate(personId, needs, memories, currentDate, isRegistered, hasKnownLocation,
                hasActiveAction, NpcActionEligibility.Unrestricted);

        /// <summary>
        /// Evaluation with the pilot's outside eligibility facts. Blocked candidates are
        /// rejected here, before ranking, so a high-scoring Work candidate that is
        /// unavailable falls back to the best eligible action instead of being selected.
        /// </summary>
        public static NpcAutonomyEvaluation Evaluate(PersonId personId, NpcNeedValues needs,
            IReadOnlyList<NpcActionMemory> memories, GameDate currentDate, bool isRegistered,
            bool hasKnownLocation, bool hasActiveAction, NpcActionEligibility eligibility)
        {
            bool profileKnown = TryGetProfile(personId, out NpcAutonomyProfile profile);
            string exclusion = null;
            if (!isRegistered || !profileKnown)
                exclusion = "人物未登记为自主 NPC。";
            else if (hasActiveAction)
                exclusion = "已有进行中的行动；行动期间不重新评估。";
            else if (!hasKnownLocation)
                exclusion = "地点未知或在旅途中，无法安排行动。";

            var candidates = new List<NpcActionCandidate>(CandidateKinds.Length);
            NpcActionCandidate selected = default;
            bool hasSelection = false;
            int bestScore = int.MinValue;
            foreach (DailyActionKind kind in CandidateKinds)
            {
                int need = needs.For(kind);
                int weight = profileKnown ? profile.WeightFor(kind) : 0;
                int baseScore = need * weight;
                int memoryAdjustment = MemoryAdjustmentFor(memories, kind, currentDate);
                string memoryText = DescribeMemoryAdjustment(memories, kind, currentDate);
                int finalScore = baseScore + memoryAdjustment;
                string blockedReason = exclusion ?? BlockReasonFor(kind, eligibility);
                if (blockedReason != null)
                {
                    // Excluded candidates never compete for selection, but their
                    // integer score remains visible for read-only diagnostics.
                    string reason = profileKnown
                        ? blockedReason + " 当前分数 " + finalScore + "（基础分数 " + baseScore +
                          "，需求 " + need + " × 偏好 " + weight + memoryText +
                          "；最终分数 " + finalScore + "，仅供参考）。"
                        : blockedReason;
                    candidates.Add(new NpcActionCandidate(kind, false, baseScore,
                        memoryAdjustment, reason));
                    continue;
                }

                candidates.Add(new NpcActionCandidate(kind, true, baseScore,
                    memoryAdjustment,
                    "基础分数 " + baseScore + "（需求 " + need + " × 偏好 " + weight + "）" +
                    memoryText + "；最终分数 " + finalScore + "。"));
                // CandidateKinds is ascending, so strict '>' keeps the lower kind on ties.
                if (finalScore > bestScore)
                {
                    bestScore = finalScore;
                    selected = candidates[candidates.Count - 1];
                    hasSelection = true;
                }
            }

            string summary;
            if (hasSelection)
                summary = "选择" + selected.KindName + "：分数 " + selected.Score + "。";
            else
                summary = exclusion ?? "没有可执行候选。";

            return new NpcAutonomyEvaluation(personId, needs, profile, isRegistered,
                hasKnownLocation, hasActiveAction, candidates.AsReadOnly(), hasSelection,
                selected, summary);
        }

        /// <summary>
        /// Per-kind eligibility reason, or null when the kind is available. Rest stays
        /// available to an injured pilot; Work is additionally blocked by an industry
        /// duty or an empty supply stock. Rules are evaluated before ranking.
        /// </summary>
        public static string BlockReasonFor(DailyActionKind kind,
            NpcActionEligibility eligibility)
        {
            if (eligibility.IsInjured && kind != DailyActionKind.Rest)
                return "伤员只能选择休息：训练与工作暂不可用。";
            if (kind != DailyActionKind.Work)
                return null;
            if (eligibility.HasNoSupplies)
                return "产业物资为 0，工作暂不可用。";
            if (eligibility.HasIndustryDuty)
                return "已担任产业职务，工作与产业职责冲突。";
            return null;
        }

        /// <summary>
        /// True from the memory date through its 30th following day inclusive. The
        /// 31st day and later are expired. A 30-day window that would pass the game
        /// date maximum is treated as active for every representable later date.
        /// </summary>
        public static bool IsMemoryActive(NpcActionMemory memory, GameDate currentDate)
        {
            if (!memory.Date.IsValid || !currentDate.IsValid)
                return false;
            if (currentDate < memory.Date)
                return false;
            if (!GameDate.TryAddDays(memory.Date, MemoryEffectDays, out GameDate lastActiveDay))
                return true;
            return currentDate <= lastActiveDay;
        }

        /// <summary>Signed current contribution of one memory for one action kind.</summary>
        public static int MemoryAdjustmentFor(NpcActionMemory memory, DailyActionKind kind,
            GameDate currentDate)
        {
            if (memory.Action != kind || !IsMemoryActive(memory, currentDate))
                return 0;
            switch (memory.Kind)
            {
                case NpcActionMemoryKind.Completed: return CompletedMemoryAdjustment;
                case NpcActionMemoryKind.Interrupted: return InterruptedMemoryAdjustment;
                default: return 0;
            }
        }

        private static int MemoryAdjustmentFor(IReadOnlyList<NpcActionMemory> memories,
            DailyActionKind kind, GameDate currentDate)
        {
            int adjustment = 0;
            if (memories == null)
                return adjustment;
            foreach (NpcActionMemory memory in memories)
                adjustment += MemoryAdjustmentFor(memory, kind, currentDate);
            return adjustment;
        }

        private static string DescribeMemoryAdjustment(IReadOnlyList<NpcActionMemory> memories,
            DailyActionKind kind, GameDate currentDate)
        {
            var builder = new System.Text.StringBuilder();
            if (memories != null)
            {
                foreach (NpcActionMemory memory in memories)
                {
                    if (memory.Action != kind)
                        continue;
                    int adjustment = MemoryAdjustmentFor(memory, kind, currentDate);
                    builder.Append("；记忆修正 ").Append(Signed(adjustment))
                        .Append("（").Append(memory.KindName).Append(' ')
                        .Append(memory.ActionName).Append(' ')
                        .Append(memory.Date.ToString());
                    if (adjustment == 0)
                        builder.Append("，已过期");
                    builder.Append('）');
                }
            }
            if (builder.Length == 0)
                builder.Append("；记忆修正 +0");
            return builder.ToString();
        }

        public static string Signed(int value) =>
            value >= 0 ? "+" + value : value.ToString(
                System.Globalization.CultureInfo.InvariantCulture);

        public static string MemoryKindName(NpcActionMemoryKind kind)
        {
            switch (kind)
            {
                case NpcActionMemoryKind.Completed: return "完成记忆";
                case NpcActionMemoryKind.Interrupted: return "中断记忆";
                default: return "未知记忆";
            }
        }

        public static string OutcomeName(NpcAutonomyCommandOutcomeKind outcome)
        {
            switch (outcome)
            {
                case NpcAutonomyCommandOutcomeKind.Accepted: return "接受";
                case NpcAutonomyCommandOutcomeKind.Deferred: return "延期";
                case NpcAutonomyCommandOutcomeKind.Rejected: return "拒绝";
                case NpcAutonomyCommandOutcomeKind.Interrupted: return "中断";
                default: return "结果";
            }
        }

        public static string TriggerName(NpcAutonomyTrigger trigger)
        {
            switch (trigger)
            {
                case NpcAutonomyTrigger.Initialization: return "首次初始化";
                case NpcAutonomyTrigger.ActionCompleted: return "行动完成";
                case NpcAutonomyTrigger.ActionInterrupted: return "行动中断";
                case NpcAutonomyTrigger.EligibilityChanged: return "资格变化";
                case NpcAutonomyTrigger.DailyCheck: return "每日检查";
                default: return "未知触发";
            }
        }

        public static string StatusName(NpcAutonomyStatus status)
        {
            switch (status)
            {
                case NpcAutonomyStatus.Idle: return "空闲";
                case NpcAutonomyStatus.Executing: return "执行中";
                case NpcAutonomyStatus.Completed: return "已完成";
                case NpcAutonomyStatus.Interrupted: return "被中断";
                default: return "未知";
            }
        }
    }
}
