using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>
    /// The first M02 daily-action kinds. Every action is a duration-bound activity;
    /// the foundation deliberately owns no stat or economy reward for any of them.
    /// </summary>
    public enum DailyActionKind
    {
        Rest = 0,
        Training = 1,
        Work = 2,
        Travel = 3
    }

    /// <summary>Stable lifecycle result for one action command or one day of progress.</summary>
    public enum DailyActionOutcomeKind
    {
        Planned = 0,
        Completed = 1,
        Interrupted = 2,
        Replanned = 3,
        Rejected = 4
    }

    /// <summary>Plain result value; never holds Unity objects.</summary>
    public readonly struct DailyActionResult
    {
        public DailyActionResult(PersonId personId, DailyActionKind kind,
            DailyActionOutcomeKind outcome, GameDate date, string reason)
        {
            PersonId = personId;
            Kind = kind;
            Outcome = outcome;
            Date = date;
            Reason = reason ?? string.Empty;
        }

        public PersonId PersonId { get; }
        public DailyActionKind Kind { get; }
        public DailyActionOutcomeKind Outcome { get; }
        public GameDate Date { get; }
        public string Reason { get; }
        public bool Succeeded => Outcome != DailyActionOutcomeKind.Rejected;
    }

    /// <summary>
    /// One immutable, duration-bound task. A travel task keeps both endpoints but no
    /// current location while it is active, which is how the model represents being
    /// in transit rather than standing at either endpoint.
    /// </summary>
    public sealed class DailyActionTask
    {
        internal DailyActionTask(PersonId personId, DailyActionKind kind, GameDate startDate,
            int durationDays, int elapsedDays, LocationId origin, LocationId destination,
            string reason)
        {
            PersonId = personId;
            Kind = kind;
            StartDate = startDate;
            DurationDays = durationDays;
            ElapsedDays = elapsedDays;
            Origin = origin;
            Destination = destination;
            Reason = reason ?? string.Empty;
        }

        public PersonId PersonId { get; }
        public DailyActionKind Kind { get; }
        public GameDate StartDate { get; }
        public int DurationDays { get; }
        public int ElapsedDays { get; }
        public LocationId Origin { get; }
        public LocationId Destination { get; }
        public string Reason { get; }

        public int RemainingDays => DurationDays - ElapsedDays;
        public bool IsTravel => Kind == DailyActionKind.Travel;

        /// <summary>True while a travel action has not reached its destination.</summary>
        public bool IsInTransit => IsTravel;

        public string KindName => DailyActionState.KindName(Kind);

        internal DailyActionTask AdvanceOneDay() =>
            new DailyActionTask(PersonId, Kind, StartDate, DurationDays, ElapsedDays + 1,
                Origin, Destination, Reason);

        internal DailyActionTask Clone() =>
            new DailyActionTask(PersonId, Kind, StartDate, DurationDays, ElapsedDays,
                Origin, Destination, Reason);
    }

    /// <summary>
    /// One person's daily state. <see cref="HasLocation"/> is false while the person is
    /// in transit or when no trustworthy location is known; <see cref="ActiveAction"/>
    /// is at most one task, enforcing mutual exclusion by construction.
    /// </summary>
    public sealed class DailyActionPersonState
    {
        internal DailyActionPersonState(PersonId personId, bool hasLocation, LocationId location)
        {
            PersonId = personId;
            HasLocation = hasLocation;
            Location = location;
        }

        public PersonId PersonId { get; }
        public bool HasLocation { get; internal set; }
        public LocationId Location { get; internal set; }
        public DailyActionTask ActiveAction { get; internal set; }

        public bool HasActiveAction => ActiveAction != null;
        public bool IsInTransit => ActiveAction != null && ActiveAction.IsTravel;

        internal DailyActionPersonState Clone() =>
            new DailyActionPersonState(PersonId, HasLocation, Location)
            {
                ActiveAction = ActiveAction != null ? ActiveAction.Clone() : null
            };
    }

    /// <summary>
    /// Deterministic daily-action timeline for the family. It owns exactly one active
    /// task per person, advances only when the caller supplies explicit game dates, and
    /// stores people in ordinal person-ID order. It never uses frame time or randomness.
    /// </summary>
    public sealed class DailyActionState
    {
        private readonly SortedDictionary<string, DailyActionPersonState> _persons =
            new SortedDictionary<string, DailyActionPersonState>(StringComparer.Ordinal);

        public static DailyActionState CreateEmpty() => new DailyActionState();

        public static DailyActionState CreateForRoster(IEnumerable<PersonId> personIds,
            LocationId defaultLocation)
        {
            var state = new DailyActionState();
            if (personIds == null)
                return state;

            var seen = new HashSet<PersonId>();
            var ordered = new List<PersonId>();
            foreach (PersonId personId in personIds)
            {
                if (personId.IsEmpty || !seen.Add(personId))
                    continue;
                ordered.Add(personId);
            }
            ordered.Sort((a, b) => string.Compare(a.Value, b.Value, StringComparison.Ordinal));
            foreach (PersonId personId in ordered)
                state.AddPersonInternal(personId, true, defaultLocation);
            return state;
        }

        /// <summary>Stable ordinal person-ID order.</summary>
        public IReadOnlyList<DailyActionPersonState> Persons
        {
            get
            {
                var snapshot = new List<DailyActionPersonState>(_persons.Count);
                foreach (DailyActionPersonState person in _persons.Values)
                    snapshot.Add(person);
                return snapshot.AsReadOnly();
            }
        }

        public bool TryGetPerson(PersonId personId, out DailyActionPersonState person) =>
            _persons.TryGetValue(personId.Value ?? string.Empty, out person);

        public bool TryGetActive(PersonId personId, out DailyActionTask task)
        {
            task = null;
            if (!_persons.TryGetValue(personId.Value ?? string.Empty, out DailyActionPersonState person) ||
                person.ActiveAction == null)
                return false;
            task = person.ActiveAction;
            return true;
        }

        public bool HasActiveAction(PersonId personId) => TryGetActive(personId, out _);

        /// <summary>
        /// Updates a non-transiting person's confirmed location. While an action is
        /// active the action owns the location, so the update is rejected.
        /// </summary>
        public bool TrySetLocation(PersonId personId, LocationId location)
        {
            if (!location.IsValid || personId.IsEmpty ||
                !_persons.TryGetValue(personId.Value, out DailyActionPersonState person) ||
                person.ActiveAction != null)
                return false;
            person.HasLocation = true;
            person.Location = location;
            return true;
        }

        /// <summary>
        /// Returns a usable location only when the person is not in transit and a
        /// location is known. A traveller therefore cannot be reported at either endpoint.
        /// </summary>
        public bool TryGetLocation(PersonId personId, out LocationId location)
        {
            location = default;
            if (!_persons.TryGetValue(personId.Value ?? string.Empty, out DailyActionPersonState person))
                return false;
            if (!person.HasLocation || person.IsInTransit)
                return false;
            location = person.Location;
            return true;
        }

        /// <summary>Adds a missing roster row without changing an existing row.</summary>
        public bool EnsurePerson(PersonId personId, LocationId defaultLocation)
        {
            if (personId.IsEmpty || _persons.ContainsKey(personId.Value))
                return false;
            AddPersonInternal(personId, true, defaultLocation);
            return true;
        }

        /// <summary>
        /// Plans exactly one duration-bound action. A person with an active action is
        /// rejected instead of having two tasks run in parallel.
        /// </summary>
        public bool TryPlan(PersonId personId, DailyActionKind kind, int durationDays,
            LocationId location, LocationId destination, GameDate startDate, string reason,
            out DailyActionResult result)
        {
            if (personId.IsEmpty || !_persons.TryGetValue(personId.Value, out DailyActionPersonState person))
            {
                result = Rejected(personId, kind, startDate, "未登记的人物无法安排行动。");
                return false;
            }
            if (!Enum.IsDefined(typeof(DailyActionKind), kind))
            {
                result = Rejected(personId, kind, startDate, "行动类型无效。");
                return false;
            }
            if (person.ActiveAction != null)
            {
                result = Rejected(personId, kind, startDate,
                    "该人物已有进行中的行动；不能同时执行两个行动。");
                return false;
            }
            if (!startDate.IsValid)
            {
                result = Rejected(personId, kind, startDate, "行动开始日期无效。");
                return false;
            }
            if (durationDays <= 0)
            {
                result = Rejected(personId, kind, startDate, "行动持续时间必须至少为一天。");
                return false;
            }

            string planReason = string.IsNullOrWhiteSpace(reason)
                ? "已安排" + KindName(kind) + "行动。"
                : reason;

            if (kind == DailyActionKind.Travel)
            {
                if (!location.IsValid || !destination.IsValid)
                {
                    result = Rejected(personId, kind, startDate, "旅行起点或目的地无效。");
                    return false;
                }
                if (location == destination)
                {
                    result = Rejected(personId, kind, startDate, "旅行目的地必须不同于出发地。");
                    return false;
                }
                if (!WorldMapRules.TryGetTravelDays(location, destination, out int routeDays))
                {
                    result = Rejected(personId, kind, startDate, "两地之间没有直达路线。");
                    return false;
                }
                if (routeDays != durationDays)
                {
                    result = Rejected(personId, kind, startDate,
                        "旅行持续时间必须等于世界地图规则给出的天数。");
                    return false;
                }

                person.HasLocation = false;
                person.Location = default;
                person.ActiveAction = new DailyActionTask(personId, kind, startDate,
                    durationDays, 0, location, destination, planReason);
                result = new DailyActionResult(personId, kind, DailyActionOutcomeKind.Planned,
                    startDate, planReason);
                return true;
            }

            if (!location.IsValid)
            {
                result = Rejected(personId, kind, startDate, "行动地点无效。");
                return false;
            }
            if (destination.IsValid)
            {
                result = Rejected(personId, kind, startDate, "非旅行行动不应指定目的地。");
                return false;
            }

            person.HasLocation = true;
            person.Location = location;
            person.ActiveAction = new DailyActionTask(personId, kind, startDate,
                durationDays, 0, location, default, planReason);
            result = new DailyActionResult(personId, kind, DailyActionOutcomeKind.Planned,
                startDate, planReason);
            return true;
        }

        /// <summary>Travel convenience that derives duration from <see cref="WorldMapRules"/>.</summary>
        public bool TryPlanTravel(PersonId personId, LocationId from, LocationId to,
            GameDate startDate, string reason, out DailyActionResult result)
        {
            if (!WorldMapRules.TryGetTravelDays(from, to, out int days))
            {
                result = Rejected(personId, DailyActionKind.Travel, startDate,
                    from == to ? "旅行目的地必须不同于出发地。" : "两地之间没有直达路线。");
                return false;
            }
            return TryPlan(personId, DailyActionKind.Travel, days, from, to, startDate,
                reason, out result);
        }

        /// <summary>
        /// Interrupts one active action. A traveller returns to the departure point because
        /// the prototype has no multi-node path or mid-route position model.
        /// </summary>
        public bool TryInterrupt(PersonId personId, string reason, GameDate date,
            out DailyActionResult result)
        {
            if (personId.IsEmpty || !_persons.TryGetValue(personId.Value, out DailyActionPersonState person) ||
                person.ActiveAction == null)
            {
                result = Rejected(personId, DailyActionKind.Rest, date, "该人物没有进行中的行动。");
                return false;
            }

            DailyActionTask task = person.ActiveAction;
            string interruptReason = string.IsNullOrWhiteSpace(reason)
                ? "行动被中断。"
                : reason;
            if (task.IsTravel)
            {
                person.HasLocation = true;
                person.Location = task.Origin;
                interruptReason += "（旅行中断，已返回" + LocationId.GetChineseName(task.Origin) + "）";
            }
            person.ActiveAction = null;
            result = new DailyActionResult(personId, task.Kind, DailyActionOutcomeKind.Interrupted,
                date, interruptReason);
            return true;
        }

        /// <summary>
        /// Advances the timeline by explicit calendar days. Completion is evaluated in
        /// stable person-ID order on every simulated day, never by frame time.
        /// </summary>
        public IReadOnlyList<DailyActionResult> AdvanceDays(GameDate fromDate, int days)
        {
            var outcomes = new List<DailyActionResult>();
            if (days <= 0 || !fromDate.IsValid)
                return outcomes.AsReadOnly();

            GameDate date = fromDate;
            for (int day = 0; day < days; day++)
            {
                if (!GameDate.TryNext(date, out GameDate next))
                    break;
                date = next;

                foreach (DailyActionPersonState person in _persons.Values)
                {
                    DailyActionTask task = person.ActiveAction;
                    if (task == null)
                        continue;

                    DailyActionTask advanced = task.AdvanceOneDay();
                    if (advanced.ElapsedDays < advanced.DurationDays)
                    {
                        person.ActiveAction = advanced;
                        continue;
                    }

                    person.ActiveAction = null;
                    if (advanced.IsTravel)
                    {
                        person.HasLocation = true;
                        person.Location = advanced.Destination;
                        outcomes.Add(new DailyActionResult(person.PersonId, advanced.Kind,
                            DailyActionOutcomeKind.Completed, date,
                            "抵达" + LocationId.GetChineseName(advanced.Destination) + "，旅行完成。"));
                    }
                    else
                    {
                        outcomes.Add(new DailyActionResult(person.PersonId, advanced.Kind,
                            DailyActionOutcomeKind.Completed, date,
                            advanced.KindName + "行动按计划完成。"));
                    }
                }
            }

            return outcomes.AsReadOnly();
        }

        public DailyActionState Clone()
        {
            var clone = new DailyActionState();
            foreach (DailyActionPersonState person in _persons.Values)
                clone._persons.Add(person.PersonId.Value, person.Clone());
            return clone;
        }

        /// <summary>Persistence support: add one validated roster row.</summary>
        internal bool TryAddPersonForRestore(PersonId personId, bool hasLocation,
            LocationId location, out string error)
        {
            if (personId.IsEmpty)
            {
                error = "每日行动人物 ID 为空。";
                return false;
            }
            if (_persons.ContainsKey(personId.Value))
            {
                error = "每日行动人物重复：" + personId.Value + "。";
                return false;
            }
            if (hasLocation && !location.IsValid)
            {
                error = "每日行动人物地点无效：" + personId.Value + "。";
                return false;
            }
            AddPersonInternal(personId, hasLocation, location);
            error = string.Empty;
            return true;
        }

        /// <summary>Persistence support: restore an already validated in-flight action.</summary>
        internal bool TryRestoreActive(PersonId personId, DailyActionKind kind, int durationDays,
            int elapsedDays, GameDate startDate, LocationId origin, LocationId destination,
            string reason, out string error)
        {
            if (!_persons.TryGetValue(personId.Value ?? string.Empty, out DailyActionPersonState person))
            {
                error = "每日行动人物未登记：" + personId.Value + "。";
                return false;
            }
            if (person.ActiveAction != null)
            {
                error = "同一人物不能恢复两个进行中的行动。";
                return false;
            }
            if (!Enum.IsDefined(typeof(DailyActionKind), kind) || durationDays <= 0 ||
                elapsedDays < 0 || elapsedDays >= durationDays || !startDate.IsValid)
            {
                error = "每日行动持续时间或开始日期无效。";
                return false;
            }

            if (kind == DailyActionKind.Travel)
            {
                if (!origin.IsValid || !destination.IsValid || origin == destination ||
                    !WorldMapRules.TryGetTravelDays(origin, destination, out int routeDays) ||
                    routeDays != durationDays)
                {
                    error = "每日旅行行动的起点、目的地或耗时不符合世界地图规则。";
                    return false;
                }
                person.HasLocation = false;
                person.Location = default;
                person.ActiveAction = new DailyActionTask(personId, kind, startDate,
                    durationDays, elapsedDays, origin, destination, reason);
                error = string.Empty;
                return true;
            }

            if (!origin.IsValid || destination.IsValid)
            {
                error = "非旅行每日行动的地点无效。";
                return false;
            }
            person.HasLocation = true;
            person.Location = origin;
            person.ActiveAction = new DailyActionTask(personId, kind, startDate,
                durationDays, elapsedDays, origin, default, reason);
            error = string.Empty;
            return true;
        }

        public static string KindName(DailyActionKind kind)
        {
            switch (kind)
            {
                case DailyActionKind.Rest: return "休息";
                case DailyActionKind.Training: return "训练";
                case DailyActionKind.Work: return "工作";
                case DailyActionKind.Travel: return "旅行";
                default: return "未知行动";
            }
        }

        private void AddPersonInternal(PersonId personId, bool hasLocation, LocationId location)
        {
            _persons.Add(personId.Value,
                new DailyActionPersonState(personId, hasLocation, location));
        }

        private static DailyActionResult Rejected(PersonId personId, DailyActionKind kind,
            GameDate date, string reason) =>
            new DailyActionResult(personId, kind, DailyActionOutcomeKind.Rejected, date, reason);
    }
}
