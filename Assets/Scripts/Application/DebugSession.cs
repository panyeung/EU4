// Unity 6.6 deprecates the DEVELOPMENT_BUILD directive (UAC0009). The approved
// Debug Mode plan requires a compile-time boundary that exists only in the Editor
// and development builds, so the directive stays and its deprecation notice is
// silenced for this file only.
#pragma warning disable UAC0009
#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;

namespace SengokuChronicle.Application
{
    /// <summary>Stable outcome of one debug command; the panel only displays it.</summary>
    public enum DebugCommandOutcome
    {
        Succeeded = 0,
        /// <summary>Validated input was refused; no state changed.</summary>
        Rejected = 1,
        /// <summary>The handle belongs to a session that already ended.</summary>
        NotActive = 2,
        /// <summary>A commit fault was injected or detected; the pre-command state was restored.</summary>
        Faulted = 3
    }

    /// <summary>Whether a command ran the normal rules or injected debug-only state.</summary>
    public enum DebugChangeKind
    {
        /// <summary>The command used the production rule path (for example a real month turn).</summary>
        NormalRule = 0,
        /// <summary>The command skipped a rule, simulation or settlement and must be labelled.</summary>
        DebugInjection = 1
    }

    /// <summary>Immutable result of one debug command, including a readable reason.</summary>
    public readonly struct DebugCommandResult
    {
        public DebugCommandResult(bool succeeded, DebugCommandOutcome outcome, string code,
            string message, DebugChangeKind kind)
        {
            Succeeded = succeeded;
            Outcome = outcome;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            Kind = kind;
        }

        public bool Succeeded { get; }
        public DebugCommandOutcome Outcome { get; }
        /// <summary>Stable machine code such as <c>person.invalid_name</c>.</summary>
        public string Code { get; }
        public string Message { get; }
        public DebugChangeKind Kind { get; }
        public bool IsDebugInjection => Kind == DebugChangeKind.DebugInjection;

        internal static DebugCommandResult Ok(string message, DebugChangeKind kind) =>
            new DebugCommandResult(true, DebugCommandOutcome.Succeeded, string.Empty, message, kind);

        internal static DebugCommandResult Reject(string code, string message) =>
            new DebugCommandResult(false, DebugCommandOutcome.Rejected, code, message,
                DebugChangeKind.NormalRule);

        internal static DebugCommandResult Ended() =>
            new DebugCommandResult(false, DebugCommandOutcome.NotActive, "session.ended",
                "调试会话已结束，请重新进入 Debug 模式。", DebugChangeKind.NormalRule);

        internal static DebugCommandResult Fault(string message) =>
            new DebugCommandResult(false, DebugCommandOutcome.Faulted, "commit.fault", message,
                DebugChangeKind.NormalRule);
    }

    /// <summary>Editable draft for one new debug-only test person.</summary>
    public sealed class DebugPersonDraft
    {
        public string DisplayName = string.Empty;
        public PersonGender Gender = PersonGender.Unspecified;
        public int BirthYear;
        public bool Alive = true;
        public bool JoinFamily = true;
        public FamilyMemberRole Role = FamilyMemberRole.DirectKin;
    }

    /// <summary>
    /// One complete edit of a registered test person. The caller passes every field, so a
    /// partial UI change is always submitted as an explicit full snapshot.
    /// </summary>
    public sealed class DebugPersonEdit
    {
        public PersonId PersonId;
        public string DisplayName = string.Empty;
        public PersonGender Gender = PersonGender.Unspecified;
        public int BirthYear;
        public bool Alive = true;
        public bool IsFamilyMember = true;
        public FamilyMemberRole Role = FamilyMemberRole.DirectKin;
    }

    /// <summary>Read-only roster row for the debug person list.</summary>
    public readonly struct DebugPersonRow
    {
        public DebugPersonRow(PersonId personId, string displayName, PersonGender gender,
            int birthYear, bool alive, bool retired, bool isMember, FamilyMemberRole role,
            bool isHead)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Gender = gender;
            BirthYear = birthYear;
            Alive = alive;
            Retired = retired;
            IsMember = isMember;
            Role = role;
            IsHead = isHead;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public PersonGender Gender { get; }
        public int BirthYear { get; }
        public bool Alive { get; }
        public bool Retired { get; }
        public bool IsMember { get; }
        public FamilyMemberRole Role { get; }
        public bool IsHead { get; }

        public string GenderName => DebugSession.GenderNameOf(Gender);
        public string RoleName => DebugSession.RoleNameOf(Role);

        public string Label =>
            DisplayName + (IsHead ? "（家主）" : IsMember ? "（" + RoleName + "）" : "（未入谱）") +
            " · " + BirthYear + " · " + (Alive ? "在世" : "已故");
    }

    /// <summary>
    /// Preview of one kinship command: whether it can be applied and the readable reason,
    /// including the existing relationship path between the two people.
    /// </summary>
    public sealed class DebugKinshipPreview
    {
        public bool CanApply { get; internal set; }
        public string Code { get; internal set; } = string.Empty;
        public string Reason { get; internal set; } = string.Empty;
        public string Path { get; internal set; } = string.Empty;
        public PersonId From { get; internal set; }
        public PersonId To { get; internal set; }
        public KinshipType Type { get; internal set; }
        public bool Add { get; internal set; }
    }

    /// <summary>Preview of one forward date jump and everything it deliberately skips.</summary>
    public sealed class DebugDatePreview
    {
        public bool CanJump { get; internal set; }
        public string Reason { get; internal set; } = string.Empty;
        public GameDate From { get; internal set; }
        public GameDate To { get; internal set; }
        public int SkippedDays { get; internal set; }
        public int SkippedMonths { get; internal set; }
        public string Summary { get; internal set; } = string.Empty;
    }

    /// <summary>One catalog event the debug session may inject.</summary>
    public readonly struct DebugEventRow
    {
        public DebugEventRow(string eventId, string title, bool pending)
        {
            EventId = eventId ?? string.Empty;
            Title = title ?? string.Empty;
            IsPending = pending;
        }

        public string EventId { get; }
        public string Title { get; }
        public bool IsPending { get; }

        public string Label => (IsPending ? "● " : string.Empty) + EventId + " · " + Title;
    }

    /// <summary>One atomic resource/market request; every field is applied or none is.</summary>
    public sealed class DebugResourceRequest
    {
        public int Funds;
        public int Supplies;
        public int Intelligence;
        public int FoodInventory;
        public int FoodPriceIndexPermille = MarketState.BaselinePriceIndexPermille;
        public int MaterialsPriceIndexPermille = MarketState.BaselinePriceIndexPermille;
    }

    /// <summary>
    /// Isolated, in-memory debug session over one <see cref="GameSession"/>. Every command
    /// validates its inputs first, then commits through the existing domain and session
    /// APIs inside a transaction that restores the complete pre-command state on failure.
    /// It never reads or writes <c>SaveSlotStore</c> and never records a debug change as a
    /// simulated or chronicled fact.
    /// <para>
    /// The whole type is compiled only for the Editor and development builds.
    /// </para>
    /// </summary>
    public sealed class DebugSession
    {
        private const string DebugPersonIdPrefix = "debug.person.";
        private const string InjectionNote = "调试注入：";
        private const string SkipNote = "未运行任何跳过的日结/月结、粮食消耗、人口结算、编年史或继承效果。";

        private readonly GameSession _session;
        private int _nextPersonNumber;

        internal DebugSession(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Commit fault seam: invoked after the first mutation of a compound command so an
        /// EditMode test can prove the transaction restores the whole snapshot.
        /// </summary>
        internal Action CommitFaultInjection { get; set; }

        public bool IsActive { get; private set; }

        /// <summary>Last readable result of any command; the panel mirrors it.</summary>
        public string LastMessage { get; private set; } = string.Empty;

        public GameSession Session => _session;

        public GameDate Today =>
            _session.WorldClock != null ? _session.WorldClock.CurrentDate
                : SengokuChronicle.Domain.WorldClock.NewGameDate;

        internal void Activate() => IsActive = true;

        internal void Deactivate() => IsActive = false;

        /// <summary>Current industry resources for the panel projection.</summary>
        public IndustryResources Resources =>
            _session.DebugIndustryState.Resources;

        /// <summary>Current immutable market snapshot for the panel projection.</summary>
        public MarketState Market => _session.DebugMarketState;

        // ---------------------------------------------------------------- roster

        /// <summary>Deterministic roster snapshot in ordinal person-ID order.</summary>
        public IReadOnlyList<DebugPersonRow> Roster
        {
            get
            {
                var rows = new List<DebugPersonRow>();
                if (!IsActive)
                    return rows.AsReadOnly();
                SuccessionWorld world = _session.Succession.World;
                foreach (PersonState person in world.Persons)
                    rows.Add(RowOf(world, person));
                return rows.AsReadOnly();
            }
        }

        /// <summary>Reads one roster row without changing anything.</summary>
        public bool TryGetRow(PersonId personId, out DebugPersonRow row)
        {
            row = default;
            if (!IsActive || personId.IsEmpty)
                return false;
            SuccessionWorld world = _session.Succession.World;
            if (!world.TryGetPerson(personId, out PersonState person))
                return false;
            row = RowOf(world, person);
            return true;
        }

        private static DebugPersonRow RowOf(SuccessionWorld world, PersonState person)
        {
            bool hasLife = world.TryGetLife(person.Id, out SuccessionPersonLife life);
            bool isMember = world.Family.TryGetRole(person.Id, out FamilyMemberRole role);
            return new DebugPersonRow(person.Id, person.DisplayName, person.Gender,
                hasLife ? life.BirthYear : 0, hasLife && life.Alive, hasLife && life.Retired,
                isMember, role, isMember && role == FamilyMemberRole.Head);
        }

        // ---------------------------------------------------------------- person commands

        /// <summary>
        /// Creates one test person. The session generates the permanent ID, so a caller
        /// can never choose or collide with an existing identity.
        /// </summary>
        public DebugCommandResult CreatePerson(DebugPersonDraft draft, out PersonId created)
        {
            created = default;
            if (!IsActive)
                return DebugCommandResult.Ended();
            if (draft == null)
                return Reject("person.null", "缺少人物草稿。");

            string name = (draft.DisplayName ?? string.Empty).Trim();
            DebugCommandResult invalid = ValidatePersonFacts(name, draft.Gender, draft.BirthYear);
            if (!invalid.Succeeded)
                return invalid;
            if (!Enum.IsDefined(typeof(FamilyMemberRole), draft.Role))
                return Reject("person.invalid_role", "家族身份无效。");
            if (draft.JoinFamily && draft.Role == FamilyMemberRole.Head)
                return Reject("person.head_role",
                    "新建人物不能直接成为家主：请先创建，再用「转移家主」。");

            PersonId id = NextPersonId();
            if (id.IsEmpty)
                return Reject("person.id_exhausted", "调试人物 ID 序号已耗尽。");

            Transaction transaction = BeginTransaction();
            try
            {
                SuccessionWorld world = _session.Succession.World;
                if (!TryCreatePersonState(id, name, draft.Gender, out PersonState createdPerson))
                {
                    Rollback(transaction);
                    return Reject("person.invalid_name", "姓名不能为空。");
                }
                if (!world.TryAddPerson(createdPerson,
                        new SuccessionPersonLife(draft.BirthYear, draft.Alive, false),
                        out string error))
                {
                    Rollback(transaction);
                    return Reject("person.add_failed", error);
                }
                CommitFaultInjection?.Invoke();
                if (draft.JoinFamily &&
                    !world.TryAddMember(id, draft.Role, out error))
                {
                    Rollback(transaction);
                    return Reject("person.membership_failed", error);
                }
            }
            catch (Exception exception)
            {
                Rollback(transaction);
                return DebugCommandResult.Fault("创建人物提交失败，状态已回滚：" + exception.Message);
            }

            created = id;
            EndCommand(true, "已创建测试人物：" + name + "（" + id.Value + "）。" + InjectionNote +
                             "不会写入编年史或人口统计。");
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        /// <summary>
        /// Edits one test person's display facts, life facts and family identity as one
        /// atomic command. The permanent ID never changes and gender stays a defined fact.
        /// </summary>
        public DebugCommandResult EditPerson(DebugPersonEdit edit)
        {
            if (!IsActive)
                return DebugCommandResult.Ended();
            if (edit == null || edit.PersonId.IsEmpty)
                return Reject("person.missing_id", "请选择要编辑的人物。");

            SuccessionWorld world = _session.Succession.World;
            if (!world.TryGetPerson(edit.PersonId, out PersonState current))
                return Reject("person.unknown", "人物不存在：" + edit.PersonId.Value + "。");

            string name = (edit.DisplayName ?? string.Empty).Trim();
            DebugCommandResult invalid = ValidatePersonFacts(name, edit.Gender, edit.BirthYear);
            if (!invalid.Succeeded)
                return invalid;
            if (!world.TryGetLife(edit.PersonId, out SuccessionPersonLife life))
                return Reject("person.life_missing", "人物生命状态缺失。");
            if (!Enum.IsDefined(typeof(FamilyMemberRole), edit.Role))
                return Reject("person.invalid_role", "家族身份无效。");

            bool isMember = world.Family.IsMember(edit.PersonId);
            bool isHead = world.Family.TryGetRole(edit.PersonId, out FamilyMemberRole currentRole) &&
                          currentRole == FamilyMemberRole.Head;
            if (isHead && (!edit.IsFamilyMember || edit.Role != FamilyMemberRole.Head))
                return Reject("person.head_role",
                    "当前家主不能通过人物编辑改换身份或移出家族名册：请先用「转移家主」，" +
                    "以保持唯一家主与继承状态一致。");
            if (!isHead && edit.IsFamilyMember && edit.Role == FamilyMemberRole.Head)
                return Reject("person.head_role",
                    "家族身份不能直接设为家主：请使用「转移家主」。");
            if (!edit.IsFamilyMember && isMember)
            {
                if (_session.Succession.DesignatedHeirId == edit.PersonId ||
                    _session.Succession.PendingSuccessorId == edit.PersonId ||
                    _session.Succession.PendingRegentId == edit.PersonId)
                    return Reject("person.membership_succession",
                        "该人物仍被继承状态引用，不能移出家族名册。");
            }
            if (edit.Gender != current.Gender && IsPregnancyParticipant(edit.PersonId))
                return Reject("person.pregnant_gender",
                    "活动孕期的参与者不能修改性别：请先完成或处理该孕期。");

            Transaction transaction = BeginTransaction();
            try
            {
                if (!TryCreatePersonState(edit.PersonId, name, edit.Gender,
                        out PersonState editedPerson))
                {
                    Rollback(transaction);
                    return Reject("person.invalid_name", "姓名不能为空。");
                }
                if (!world.TryReplacePerson(editedPerson, out string error))
                {
                    Rollback(transaction);
                    return Reject("person.replace_failed", error);
                }
                CommitFaultInjection?.Invoke();
                if (!world.TrySetLife(edit.PersonId,
                        new SuccessionPersonLife(edit.BirthYear, edit.Alive, life.Retired),
                        out error))
                {
                    Rollback(transaction);
                    return Reject("person.life_failed", error);
                }
                CommitFaultInjection?.Invoke();
                if (edit.IsFamilyMember)
                {
                    bool applied = isMember
                        ? world.TrySetRole(edit.PersonId, edit.Role, out error)
                        : world.TryAddMember(edit.PersonId, edit.Role, out error);
                    if (!applied)
                    {
                        Rollback(transaction);
                        return Reject("person.membership_failed", error);
                    }
                }
                else if (isMember && !world.TryRemoveMember(edit.PersonId, out string removeError))
                {
                    Rollback(transaction);
                    return Reject("person.membership_failed", removeError);
                }
            }
            catch (Exception exception)
            {
                Rollback(transaction);
                return DebugCommandResult.Fault("编辑人物提交失败，状态已回滚：" + exception.Message);
            }

            var notes = new StringBuilder();
            notes.Append("已更新测试人物：").Append(name).Append("。").Append(InjectionNote);
            notes.Append("直接设置存活状态不会模拟死亡或复活、编年史、人口结算或继承效果。");
            EndCommand(true, notes.ToString());
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        /// <summary>
        /// Hands the head role to one living family member. Exactly one member keeps
        /// <see cref="FamilyMemberRole.Head"/> and <see cref="SuccessionState.HeadPersonId"/>
        /// is updated in the same commit. No chronicle entry is written and no succession
        /// count is advanced.
        /// </summary>
        public DebugCommandResult TransferHeadship(PersonId targetId)
        {
            if (!IsActive)
                return DebugCommandResult.Ended();
            SuccessionState succession = _session.Succession;
            if (succession.Phase != SuccessionPhase.None)
                return Reject("head.pending",
                    "当前有继承或摄政待决：" + PhaseName(succession.Phase) + "，不能转移家主。");
            if (!string.IsNullOrEmpty(_session.PendingEventId))
                return Reject("head.pending_event", "仍有待处理事件，请先完成当前事件。");

            SuccessionWorld world = succession.World;
            int headCount = 0;
            PersonId seatedHead = default;
            foreach (FamilyMembership membership in world.Family.Members)
            {
                if (membership.Role != FamilyMemberRole.Head)
                    continue;
                headCount++;
                seatedHead = membership.PersonId;
            }
            if (headCount > 1)
                return Reject("head.multiple", "家族存在多个家主，无法安全转移。");
            if (headCount == 0)
                return Reject("head.missing", "当前家族没有家主，无法转移。");
            if (targetId.IsEmpty || targetId == seatedHead)
                return Reject("head.invalid_target", "请选择另一位家族成员作为新家主。");
            if (!world.Family.TryGetRole(targetId, out FamilyMemberRole targetRole))
                return Reject("head.not_member", "目标人物不是家族成员。");
            if (targetRole == FamilyMemberRole.Head)
                return Reject("head.multiple", "目标人物已经是家主。");
            if (!world.TryGetLife(targetId, out SuccessionPersonLife targetLife) || !targetLife.Alive)
                return Reject("head.not_alive", "目标人物必须是在世家族成员。");

            string oldName = world.TryGetPerson(seatedHead, out PersonState oldHead)
                ? oldHead.DisplayName
                : seatedHead.Value;
            string targetName = world.TryGetPerson(targetId, out PersonState target)
                ? target.DisplayName
                : targetId.Value;

            Transaction transaction = BeginTransaction();
            try
            {
                if (!world.TrySetRole(seatedHead, FamilyMemberRole.DirectKin, out string error))
                {
                    Rollback(transaction);
                    return Reject("head.old_role_failed", error);
                }
                CommitFaultInjection?.Invoke();
                if (!world.TrySetRole(targetId, FamilyMemberRole.Head, out error))
                {
                    Rollback(transaction);
                    return Reject("head.new_role_failed", error);
                }
                succession.HeadPersonId = targetId;
                if (succession.DesignatedHeirId == targetId)
                    succession.DesignatedHeirId = default;
            }
            catch (Exception exception)
            {
                Rollback(transaction);
                return DebugCommandResult.Fault("转移家主提交失败，状态已回滚：" + exception.Message);
            }

            EndCommand(true, "已转移家主：" + oldName + " → " + targetName + "。" + InjectionNote +
                             "不增加编年史继承记录，不改变继承计数。");
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        /// <summary>
        /// Removes one test person only when no session or domain fact still references it.
        /// Every remaining reference is reported so the operator can dissolve it first.
        /// </summary>
        public DebugCommandResult RemovePerson(PersonId personId)
        {
            if (!IsActive)
                return DebugCommandResult.Ended();
            if (personId.IsEmpty)
                return Reject("person.missing_id", "请选择要移除的人物。");
            SuccessionWorld world = _session.Succession.World;
            if (!world.TryGetPerson(personId, out PersonState person))
                return Reject("person.unknown", "人物不存在：" + personId.Value + "。");

            IReadOnlyList<string> blockers = CollectRemovalBlockers(personId);
            if (blockers.Count > 0)
                return Reject("person.referenced",
                    "无法移除 " + person.DisplayName + "，仍有未解除引用：" +
                    string.Join("；", blockers) + "。");

            Transaction transaction = BeginTransaction();
            try
            {
                if (!world.TryRemovePerson(personId))
                {
                    Rollback(transaction);
                    return Reject("person.remove_failed", "人物状态已变化，无法移除。");
                }
                CommitFaultInjection?.Invoke();
            }
            catch (Exception exception)
            {
                Rollback(transaction);
                return DebugCommandResult.Fault("移除人物提交失败，状态已回滚：" + exception.Message);
            }

            EndCommand(true, "已移除测试人物：" + person.DisplayName + "。" + InjectionNote +
                             "该操作不是正常死亡或离队流程。");
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        private IReadOnlyList<string> CollectRemovalBlockers(PersonId personId)
        {
            var blockers = new List<string>();
            SuccessionState succession = _session.Succession;
            SuccessionWorld world = succession.World;

            if (world.Family.TryGetRole(personId, out FamilyMemberRole role))
                blockers.Add("仍在家族名册（" + RoleNameOf(role) + "）");
            if (succession.HeadPersonId == personId)
                blockers.Add("当前家主");
            if (succession.DesignatedHeirId == personId)
                blockers.Add("指定继承人");
            if (succession.PendingSuccessorId == personId)
                blockers.Add("待决继承人");
            if (succession.PendingRegentId == personId)
                blockers.Add("待决摄政");
            if (world.Kinship.LinksFor(personId).Count > 0)
                blockers.Add("仍有亲属关系");
            if (HasFamilyRelationshipValue(personId))
                blockers.Add("仍有家训议事关系值");
            if (IsPregnancyParticipant(personId))
                blockers.Add("仍在活动孕期中");
            if (_session.Development.Contains(personId) ||
                DevelopmentPlans(personId) || IsMentorForSomeone(personId))
                blockers.Add("仍有成长或教育记忆");
            if (_session.ResearchState.HasPlan && _session.ResearchState.ResearcherId == personId)
                blockers.Add("仍在家族研究任命中");
            if (IsIndustryAssignee(personId))
                blockers.Add("仍在产业任命中");
            if (HasDailyAction(personId))
                blockers.Add("仍有每日行动或旅行");
            if (_session.DebugInjuries.IsInjured(personId))
                blockers.Add("仍有伤势记录");
            if (_session.CommissionStage == ShadowEscortStage.InProgress &&
                _session.CommissionAssignee == personId)
                blockers.Add("仍在影子护卫委托中");
            if (IsTacticalOperative(personId))
                blockers.Add("仍在战术任务中");
            if (IsMissionHistoryParticipant(personId))
                blockers.Add("出现在任务终局历史中");
            if (_session.TryGetNpcAutonomy(personId, out _))
                blockers.Add("仍有 NPC 自主状态");
            return blockers.AsReadOnly();
        }

        private bool HasFamilyRelationshipValue(PersonId personId)
        {
            IReadOnlyList<FamilyRelationshipEntry> entries = _session.DebugFamilyRelationships;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].HeadId == personId || entries[i].MemberId == personId)
                    return true;
            return false;
        }

        private bool DevelopmentPlans(PersonId personId)
        {
            IReadOnlyList<int> planned = _session.Development.PlannedAgeYears(personId);
            if (planned.Count > 0)
                return true;
            IReadOnlyList<int> settled = _session.Development.SettledAgeYears(personId);
            return settled.Count > 0;
        }

        private bool IsMentorForSomeone(PersonId personId)
        {
            CharacterDevelopmentState development = _session.Development;
            IReadOnlyList<PersonId> learners = development.PersonIds;
            for (int i = 0; i < learners.Count; i++)
            {
                IReadOnlyList<int> years = development.PlannedAgeYears(learners[i]);
                for (int y = 0; y < years.Count; y++)
                {
                    if (development.TryGetPlan(learners[i], years[y], out EducationYearPlan plan) &&
                        plan.Mentor == personId)
                        return true;
                }
            }
            return false;
        }

        private bool IsIndustryAssignee(PersonId personId)
        {
            IReadOnlyList<IndustryAssignment> assignments = _session.DebugIndustryState.Assignments;
            for (int i = 0; i < assignments.Count; i++)
                if (assignments[i].PersonId == personId)
                    return true;
            return false;
        }

        private bool HasDailyAction(PersonId personId)
        {
            IReadOnlyList<DailyActionPersonState> persons = _session.DebugDailyActions.Persons;
            for (int i = 0; i < persons.Count; i++)
                if (persons[i].PersonId == personId && persons[i].HasActiveAction)
                    return true;
            return false;
        }

        private bool IsTacticalOperative(PersonId personId)
        {
            TacticalMissionState mission = _session.DebugTacticalMission;
            if (mission == null)
                return false;
            TacticalMissionSnapshot snapshot = mission.CreateSnapshot();
            if (snapshot == null)
                return false;
            for (int i = 0; i < snapshot.Operatives.Count; i++)
                if (snapshot.Operatives[i].PersonId == personId)
                    return true;
            return false;
        }

        private bool IsMissionHistoryParticipant(PersonId personId)
        {
            IReadOnlyList<MissionRecord> records = _session.DebugMissionHistory.Records;
            for (int i = 0; i < records.Count; i++)
            {
                MissionRecord record = records[i];
                if (record.CaptainId == personId)
                    return true;
                for (int p = 0; p < record.Participants.Count; p++)
                    if (record.Participants[p] == personId)
                        return true;
            }
            return false;
        }

        private bool IsPregnancyParticipant(PersonId personId)
        {
            IReadOnlyList<FamilyPregnancyRecord> records = _session.Succession.World.Pregnancies.Records;
            for (int i = 0; i < records.Count; i++)
                if (records[i].GestatingPersonId == personId ||
                    records[i].OtherParentId == personId)
                    return true;
            return false;
        }

        // ---------------------------------------------------------------- kinship commands

        /// <summary>
        /// Previews one kinship add or remove: the structural validator result, the SC-022
        /// semantic rules and the existing relationship path. No state changes.
        /// </summary>
        public DebugKinshipPreview PreviewKinship(PersonId from, PersonId to, KinshipType type,
            bool add)
        {
            var preview = new DebugKinshipPreview
            {
                From = from,
                To = to,
                Type = type,
                Add = add
            };
            if (!IsActive)
            {
                preview.Reason = "调试会话已结束。";
                return preview;
            }
            SuccessionWorld world = _session.Succession.World;
            if (from.IsEmpty || to.IsEmpty)
            {
                preview.Reason = "请选择两位人物。";
                return preview;
            }
            if (!world.TryGetPerson(from, out _) || !world.TryGetPerson(to, out _))
            {
                preview.Reason = "关系端点必须是已登记人物。";
                return preview;
            }
            if (!KinshipLink.TryCreate(from, to, type, out KinshipLink link, out string error))
            {
                preview.Reason = error;
                return preview;
            }

            preview.Path = DescribeRelationshipPath(world, from, to);
            if (add)
            {
                if (world.Kinship.Contains(link))
                {
                    preview.Reason = "这条关系已经存在。";
                    return preview;
                }
                string semantic = ValidateKinshipAddition(world, link);
                if (!string.IsNullOrEmpty(semantic))
                {
                    preview.Reason = semantic;
                    return preview;
                }
            }
            else
            {
                if (!TryResolveStoredLink(world, from, to, type, out KinshipLink stored))
                {
                    preview.Reason = "这条关系不存在，无法移除。";
                    return preview;
                }
                if (type == KinshipType.Spouse &&
                    world.Pregnancies.ContainsPair(from, to))
                {
                    preview.Reason = "该配偶对仍有活动孕期，不能解除配偶关系。";
                    return preview;
                }
                preview.Reason = "可以解除：" + TypeNameOf(type) + "（" +
                                 NameOf(world, stored.From) + " → " + NameOf(world, stored.To) +
                                 "）。";
                preview.CanApply = true;
                return preview;
            }

            preview.CanApply = true;
            preview.Reason = "可以登记：" + TypeNameOf(type) + "。";
            return preview;
        }

        /// <summary>
        /// Finds the fact exactly as it is stored, so a symmetric spouse or sibling fact can
        /// be dissolved from either endpoint order. Directed facts keep their orientation.
        /// </summary>
        private static bool TryResolveStoredLink(SuccessionWorld world, PersonId from,
            PersonId to, KinshipType type, out KinshipLink stored)
        {
            IReadOnlyList<KinshipLink> links = world.Kinship.Links;
            for (int i = 0; i < links.Count; i++)
            {
                KinshipLink link = links[i];
                if (link.Type != type)
                    continue;
                if (link.From == from && link.To == to)
                {
                    stored = link;
                    return true;
                }
                if (link.IsSymmetric && link.From == to && link.To == from)
                {
                    stored = link;
                    return true;
                }
            }
            stored = default;
            return false;
        }

        /// <summary>Adds one kinship fact through the previewed and validated transaction.</summary>
        public DebugCommandResult AddKinship(PersonId from, PersonId to, KinshipType type) =>
            CommitKinship(from, to, type, true);

        /// <summary>Removes one kinship fact through the previewed and validated transaction.</summary>
        public DebugCommandResult RemoveKinship(PersonId from, PersonId to, KinshipType type) =>
            CommitKinship(from, to, type, false);

        private DebugCommandResult CommitKinship(PersonId from, PersonId to, KinshipType type,
            bool add)
        {
            if (!IsActive)
                return DebugCommandResult.Ended();
            DebugKinshipPreview preview = PreviewKinship(from, to, type, add);
            if (!preview.CanApply)
                return Reject("kinship.rejected", preview.Reason);

            SuccessionWorld world = _session.Succession.World;
            KinshipLink.TryCreate(from, to, type, out KinshipLink link, out _);
            Transaction transaction = BeginTransaction();
            try
            {
                if (add)
                {
                    if (!world.TryAddKinship(link, out string error))
                    {
                        Rollback(transaction);
                        return Reject("kinship.add_failed", error);
                    }
                }
                else
                {
                    if (!TryResolveStoredLink(world, from, to, type, out KinshipLink stored))
                    {
                        Rollback(transaction);
                        return Reject("kinship.remove_failed", "这条关系不存在。");
                    }
                    if (!world.Kinship.TryRemove(stored, out string removeError))
                    {
                        Rollback(transaction);
                        return Reject("kinship.remove_failed", removeError);
                    }
                }
                CommitFaultInjection?.Invoke();
            }
            catch (Exception exception)
            {
                Rollback(transaction);
                return DebugCommandResult.Fault("亲属关系提交失败，状态已回滚：" + exception.Message);
            }

            string fromName = NameOf(world, from);
            string toName = NameOf(world, to);
            EndCommand(true, (add ? "已登记亲属关系：" : "已解除亲属关系：") + fromName + " — " +
                             TypeNameOf(type) + " — " + toName + "。" + InjectionNote +
                             "不修改编年史，也不重算家族关系值。");
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        private static string ValidateKinshipAddition(SuccessionWorld world, KinshipLink link)
        {
            switch (link.Type)
            {
                case KinshipType.Spouse:
                    if (FamilyFormationRules.IsDirectLineRelation(world.Kinship, link.From, link.To))
                        return "直系血亲或直系养亲不能登记婚姻。";
                    if (HasKinship(world, link.From, link.To, KinshipType.Spouse))
                        return "这两位已经登记为正式配偶。";
                    return string.Empty;
                case KinshipType.Adoption:
                    if (HasKinship(world, link.From, link.To, KinshipType.Spouse))
                        return "不能收养自己的正式配偶。";
                    if (FamilyFormationRules.WouldCreateAdoptionCycle(world.Kinship, link.From,
                            link.To))
                        return "该收养会使收养与亲子关系形成循环。";
                    return string.Empty;
                case KinshipType.ParentChild:
                    if (FamilyFormationRules.IsDirectLineRelation(world.Kinship, link.From, link.To))
                        return "该亲子关系会形成直系循环。";
                    return string.Empty;
                case KinshipType.Sibling:
                    if (FamilyFormationRules.IsDirectLineRelation(world.Kinship, link.From, link.To))
                        return "直系亲属不能同时登记为兄弟姐妹。";
                    return string.Empty;
                default:
                    return "亲属关系类型无效。";
            }
        }

        private static bool HasKinship(SuccessionWorld world, PersonId left, PersonId right,
            KinshipType type)
        {
            return world.Kinship.Contains(new KinshipLink(left, right, type)) ||
                   world.Kinship.Contains(new KinshipLink(right, left, type));
        }

        private static string DescribeRelationshipPath(SuccessionWorld world, PersonId from,
            PersonId to)
        {
            var visited = new HashSet<PersonId> { from };
            var queue = new Queue<KeyValuePair<PersonId, string>>();
            queue.Enqueue(new KeyValuePair<PersonId, string>(from, string.Empty));
            int guard = 0;
            while (queue.Count > 0 && guard++ < 512)
            {
                KeyValuePair<PersonId, string> current = queue.Dequeue();
                IReadOnlyList<KinshipLink> links = world.Kinship.LinksFor(current.Key);
                for (int i = 0; i < links.Count; i++)
                {
                    KinshipLink link = links[i];
                    PersonId next = link.From == current.Key ? link.To : link.From;
                    if (next == to)
                    {
                        string step = NameOf(world, current.Key) + " —[" + TypeNameOf(link.Type) +
                                      "]→ " + NameOf(world, to);
                        return string.IsNullOrEmpty(current.Value)
                            ? "现存关系：" + step
                            : "现存关系：" + current.Value + " → " + step;
                    }
                    if (!visited.Add(next))
                        continue;
                    string trail = string.IsNullOrEmpty(current.Value)
                        ? NameOf(world, current.Key) + " —[" + TypeNameOf(link.Type) + "]→ " +
                          NameOf(world, next)
                        : current.Value + " → " + NameOf(world, current.Key) + " —[" +
                          TypeNameOf(link.Type) + "]→ " + NameOf(world, next);
                    queue.Enqueue(new KeyValuePair<PersonId, string>(next, trail));
                }
            }
            return "现存关系：两人之间没有亲属路径。";
        }

        // ---------------------------------------------------------------- complex preset

        /// <summary>
        /// Adds the fixed 16-person, six-generation family preset with collateral branches,
        /// spouses, adoptions and one disconnected retainer.
        /// </summary>
        public DebugCommandResult ApplyComplexFamilyPreset()
        {
            if (!IsActive)
                return DebugCommandResult.Ended();

            var specs = new List<PresetPerson>(16);
            var links = new List<PresetLink>(24);
            int year = Today.Year;
            AddGeneration(specs, links, year);

            var pendingKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (PresetPerson spec in specs)
            {
                DebugCommandResult invalid = ValidatePersonFacts(spec.Name, spec.Gender,
                    spec.BirthYear);
                if (!invalid.Succeeded)
                    return Reject("preset.invalid_person", "预设人物无效：" + spec.Name + "。");
                PersonId id = NextPersonId();
                if (id.IsEmpty)
                    return Reject("person.id_exhausted", "调试人物 ID 序号已耗尽。");
                spec.Id = id;
                pendingKeys.Add(spec.Key);
                if (_session.Succession.World.TryGetPerson(id, out _))
                    return Reject("preset.id_collision", "预设人物 ID 冲突：" + id.Value + "。");
            }
            foreach (PresetLink link in links)
            {
                if (!pendingKeys.Contains(link.FromKey) || !pendingKeys.Contains(link.ToKey))
                    return Reject("preset.invalid_link", "预设关系端点缺失。");
            }

            Transaction transaction = BeginTransaction();
            try
            {
                SuccessionWorld world = _session.Succession.World;
                foreach (PresetPerson spec in specs)
                {
                    if (!TryCreatePersonState(spec.Id, spec.Name, spec.Gender,
                            out PersonState presetPerson))
                    {
                        Rollback(transaction);
                        return Reject("preset.add_failed", "预设人物姓名无效：" + spec.Name + "。");
                    }
                    if (!world.TryAddPerson(presetPerson,
                            new SuccessionPersonLife(spec.BirthYear, spec.Alive, false),
                            out string error))
                    {
                        Rollback(transaction);
                        return Reject("preset.add_failed", error);
                    }
                    if (spec.Role.HasValue &&
                        !world.TryAddMember(spec.Id, spec.Role.Value, out error))
                    {
                        Rollback(transaction);
                        return Reject("preset.member_failed", error);
                    }
                }
                CommitFaultInjection?.Invoke();
                Dictionary<string, PersonId> byKey = ResolvePresetIds(specs);
                foreach (PresetLink link in links)
                {
                    if (!KinshipLink.TryCreate(byKey[link.FromKey], byKey[link.ToKey],
                            link.Type, out KinshipLink fact, out string error))
                    {
                        Rollback(transaction);
                        return Reject("preset.link_failed", error);
                    }
                    if (!world.TryAddKinship(fact, out error))
                    {
                        Rollback(transaction);
                        return Reject("preset.link_failed", error);
                    }
                }
                CommitFaultInjection?.Invoke();
            }
            catch (Exception exception)
            {
                Rollback(transaction);
                return DebugCommandResult.Fault("复杂家谱预设提交失败，已整份回滚：" +
                                                exception.Message);
            }

            EndCommand(true, "已生成复杂家谱预设：" + specs.Count + " 人、6 代，" +
                             links.Count + " 条关系（含旁系、配偶、收养与断开成员）。" +
                             InjectionNote + SkipNote);
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        private Dictionary<string, PersonId> ResolvePresetIds(List<PresetPerson> specs)
        {
            var byKey = new Dictionary<string, PersonId>(StringComparer.Ordinal);
            for (int i = 0; i < specs.Count; i++)
                byKey[specs[i].Key] = specs[i].Id;
            return byKey;
        }

        private static void AddGeneration(List<PresetPerson> specs, List<PresetLink> links, int year)
        {
            specs.Add(new PresetPerson("g1a", "预设 初代当主", PersonGender.Male, year - 90, false,
                FamilyMemberRole.CollateralKin));
            specs.Add(new PresetPerson("g1b", "预设 初代之妻", PersonGender.Female, year - 92,
                false, FamilyMemberRole.Spouse));
            specs.Add(new PresetPerson("g2a", "预设 二代直系", PersonGender.Male, year - 72, false,
                FamilyMemberRole.CollateralKin));
            specs.Add(new PresetPerson("g2b", "预设 二代配偶", PersonGender.Female, year - 74,
                false, FamilyMemberRole.Spouse));
            specs.Add(new PresetPerson("g2c", "预设 二代旁系", PersonGender.Female, year - 75,
                false, FamilyMemberRole.CollateralKin));
            specs.Add(new PresetPerson("g3a", "预设 三代直系", PersonGender.Male, year - 54, true,
                FamilyMemberRole.CollateralKin));
            specs.Add(new PresetPerson("g3b", "预设 三代配偶", PersonGender.Female, year - 56,
                true, FamilyMemberRole.Spouse));
            specs.Add(new PresetPerson("g3c", "预设 三代养子", PersonGender.Male, year - 52, true,
                FamilyMemberRole.AdoptedChild));
            specs.Add(new PresetPerson("g3d", "预设 三代旁系", PersonGender.Female, year - 55,
                false, FamilyMemberRole.CollateralKin));
            specs.Add(new PresetPerson("g4a", "预设 四代直系", PersonGender.Female, year - 36,
                true, FamilyMemberRole.DirectKin));
            specs.Add(new PresetPerson("g4b", "预设 四代配偶", PersonGender.Male, year - 38, true,
                FamilyMemberRole.Spouse));
            specs.Add(new PresetPerson("g4c", "预设 四代养女", PersonGender.Female, year - 34,
                true, FamilyMemberRole.AdoptedChild));
            specs.Add(new PresetPerson("g5a", "预设 五代直系", PersonGender.Male, year - 18, true,
                FamilyMemberRole.DirectKin));
            specs.Add(new PresetPerson("g6a", "预设 六代直系", PersonGender.Female, year - 2,
                true, FamilyMemberRole.DirectKin));
            specs.Add(new PresetPerson("g6b", "预设 六代配偶", PersonGender.Male, year - 4, true,
                FamilyMemberRole.Spouse));
            specs.Add(new PresetPerson("out", "预设 断开家臣", PersonGender.Male, year - 40, true,
                FamilyMemberRole.Retainer));

            links.Add(new PresetLink("g1a", "g1b", KinshipType.Spouse));
            links.Add(new PresetLink("g1a", "g2a", KinshipType.ParentChild));
            links.Add(new PresetLink("g1b", "g2a", KinshipType.ParentChild));
            links.Add(new PresetLink("g1a", "g2c", KinshipType.ParentChild));
            links.Add(new PresetLink("g1b", "g2c", KinshipType.ParentChild));
            links.Add(new PresetLink("g2a", "g2c", KinshipType.Sibling));
            links.Add(new PresetLink("g2a", "g2b", KinshipType.Spouse));
            links.Add(new PresetLink("g2a", "g3a", KinshipType.ParentChild));
            links.Add(new PresetLink("g2b", "g3a", KinshipType.ParentChild));
            links.Add(new PresetLink("g2a", "g3c", KinshipType.Adoption));
            links.Add(new PresetLink("g2c", "g3d", KinshipType.ParentChild));
            links.Add(new PresetLink("g3a", "g3b", KinshipType.Spouse));
            links.Add(new PresetLink("g3a", "g4a", KinshipType.ParentChild));
            links.Add(new PresetLink("g3b", "g4a", KinshipType.ParentChild));
            links.Add(new PresetLink("g3a", "g4c", KinshipType.Adoption));
            links.Add(new PresetLink("g4a", "g4b", KinshipType.Spouse));
            links.Add(new PresetLink("g4a", "g5a", KinshipType.ParentChild));
            links.Add(new PresetLink("g4b", "g5a", KinshipType.ParentChild));
            links.Add(new PresetLink("g5a", "g6a", KinshipType.ParentChild));
            links.Add(new PresetLink("g6b", "g6a", KinshipType.ParentChild));
            links.Add(new PresetLink("g5a", "g6b", KinshipType.Spouse));
        }

        private sealed class PresetPerson
        {
            internal PresetPerson(string key, string name, PersonGender gender, int birthYear,
                bool alive, FamilyMemberRole? role)
            {
                Key = key;
                Name = name;
                Gender = gender;
                BirthYear = birthYear;
                Alive = alive;
                Role = role;
            }

            internal string Key { get; }
            internal string Name { get; }
            internal PersonGender Gender { get; }
            internal int BirthYear { get; }
            internal bool Alive { get; }
            internal FamilyMemberRole? Role { get; }
            internal PersonId Id { get; set; }
        }

        private sealed class PresetLink
        {
            internal PresetLink(string fromKey, string toKey, KinshipType type)
            {
                FromKey = fromKey;
                ToKey = toKey;
                Type = type;
            }

            internal string FromKey { get; }
            internal string ToKey { get; }
            internal KinshipType Type { get; }
        }

        // ---------------------------------------------------------------- date commands

        /// <summary>Runs one real month turn through <see cref="GameSession.TryAdvanceToNextMonth"/>.</summary>
        public DebugCommandResult AdvanceOneMonth()
        {
            if (!IsActive)
                return DebugCommandResult.Ended();
            string before = Today.ToString();
            bool advanced = _session.TryAdvanceToNextMonth();
            string message = _session.LastEventMessage;
            if (advanced)
            {
                LastMessage = "正常月结：" + before + " → " + Today + "。" +
                              (string.IsNullOrEmpty(message) ? string.Empty : " " + message);
                return DebugCommandResult.Ok(LastMessage, DebugChangeKind.NormalRule);
            }
            LastMessage = "月结被拒绝（" + _session.LastMonthTurnOutcome + "）：" +
                          (string.IsNullOrEmpty(message) ? "状态不允许推进。" : message);
            return DebugCommandResult.Reject("date.month_rejected", LastMessage);
        }

        /// <summary>
        /// Previews a forward-only date jump. It reports the exact target, the skipped span
        /// and every settlement that will not run, or the in-flight state that blocks it.
        /// </summary>
        public DebugDatePreview PreviewDateJump(int year, int month, int day)
        {
            var preview = new DebugDatePreview { From = Today };
            if (!IsActive)
            {
                preview.Reason = "调试会话已结束。";
                return preview;
            }
            if (!GameDate.TryParse(year, month, day, out GameDate target))
            {
                preview.Reason = "目标日期无效：必须在 1560-01-01 至 2000-12-31 之间。";
                return preview;
            }
            preview.To = target;
            if (target <= Today)
            {
                preview.Reason = "不能倒退或停留在当前日期；请选择更晚的合法日期。";
                return preview;
            }
            string blocked = DateJumpBlockReason();
            if (!string.IsNullOrEmpty(blocked))
            {
                preview.Reason = blocked;
                return preview;
            }
            if (!TryDaysBetween(Today, target, out int days))
            {
                preview.Reason = "目标日期超出支持范围。";
                return preview;
            }

            preview.CanJump = true;
            preview.SkippedDays = days;
            preview.SkippedMonths = ((target.Year * 12) + target.Month) -
                                    ((Today.Year * 12) + Today.Month);
            preview.Summary = "调试日期注入：" + Today + " → " + target + "，跳过 " + days +
                              " 天（约 " + preview.SkippedMonths + " 个月）。" + SkipNote +
                              "下一次正常月结只会结算一次。";
            return preview;
        }

        /// <summary>
        /// Commits one forward date jump after re-running the same validation the preview
        /// used. Industry, research and market month ledgers are realigned; no skipped
        /// settlement is replayed and no transient month report survives.
        /// </summary>
        public DebugCommandResult JumpToDate(int year, int month, int day)
        {
            if (!IsActive)
                return DebugCommandResult.Ended();
            DebugDatePreview preview = PreviewDateJump(year, month, day);
            if (!preview.CanJump)
                return Reject("date.jump_rejected", preview.Reason);

            WorldClock clock = _session.WorldClock;
            IndustryState industryBefore = _session.DebugIndustryState.Clone();
            MarketState marketBefore = _session.DebugMarketState;
            FamilyResearchState researchBefore = _session.DebugResearchState.Clone();
            int monthIndex = IndustryCalendar.MonthIndex(preview.To) - 1;
            if (!MarketState.TryCreate(marketBefore.FoodInventory,
                    marketBefore.FoodPriceIndexPermille, marketBefore.MaterialsPriceIndexPermille,
                    monthIndex, 0, null, out MarketState marketAfter, out string marketError))
                return Reject("date.market_failed", marketError);

            try
            {
                CommitFaultInjection?.Invoke();
                if (!clock.AdvanceDays(preview.SkippedDays))
                    throw new InvalidOperationException("日期推进失败。");
                CommitFaultInjection?.Invoke();
                if (!_session.DebugIndustryState.TrySetLastSettledMonthIndex(monthIndex,
                        out string industryError))
                    throw new InvalidOperationException(industryError);
                if (!_session.DebugResearchState.TrySetLastSettledMonthIndex(monthIndex,
                        out string researchError))
                    throw new InvalidOperationException(researchError);
                _session.DebugReplaceMarketState(marketAfter);
            }
            catch (Exception exception)
            {
                clock.TryRestoreDate(preview.From);
                _session.DebugReplaceIndustryState(industryBefore);
                _session.DebugReplaceResearchState(researchBefore);
                _session.DebugReplaceMarketState(marketBefore);
                return DebugCommandResult.Fault("日期注入提交失败，状态已回滚：" +
                                                exception.Message);
            }

            _session.DebugClearMonthReports();
            EndCommand(true, preview.Summary);
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        private string DateJumpBlockReason()
        {
            SuccessionState succession = _session.Succession;
            if (succession.Phase == SuccessionPhase.Extinct)
                return "家族已经灭亡，不能跳转日期。";
            if (succession.IsPending || succession.Phase == SuccessionPhase.Regency)
                return "仍有待决继承或摄政流程，不能跳过结算。";
            if (!string.IsNullOrEmpty(_session.PendingEventId))
                return "仍有待处理事件，请先完成当前事件。";
            if (_session.IsTacticalMissionActive)
                return "战术任务正在进行，不能跳过日期。";
            if (_session.CommissionStage == ShadowEscortStage.InProgress)
                return "影子护卫委托正在途中，不能跳过日期。";
            if (_session.FamilyPregnancies.Count > 0)
                return "存在活动孕期，不能跳过日期。";
            IReadOnlyList<DailyActionPersonState> persons = _session.DebugDailyActions.Persons;
            for (int i = 0; i < persons.Count; i++)
                if (persons[i].HasActiveAction)
                    return "有每日行动或旅行尚未结束，不能跳过日期。";
            return string.Empty;
        }

        private static bool TryDaysBetween(GameDate from, GameDate to, out int days)
        {
            days = 0;
            if (to <= from)
                return false;
            int low = 1;
            int high = 200000;
            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                if (!GameDate.TryAddDays(from, middle, out GameDate probe))
                {
                    high = middle - 1;
                    continue;
                }
                if (probe == to)
                {
                    days = middle;
                    return true;
                }
                if (probe < to)
                    low = middle + 1;
                else
                    high = middle - 1;
            }
            return false;
        }

        // ---------------------------------------------------------------- resource commands

        /// <summary>
        /// Validates and applies the six resource and price fields as one atomic request.
        /// Any invalid field rejects the whole request and leaves every value unchanged.
        /// </summary>
        public DebugCommandResult SetResourcesAndMarket(DebugResourceRequest request)
        {
            if (!IsActive)
                return DebugCommandResult.Ended();
            if (request == null)
                return Reject("resource.null", "缺少资源请求。");
            return ApplyResources(request.Funds, request.Supplies, request.Intelligence,
                request.FoodInventory, request.FoodPriceIndexPermille,
                request.MaterialsPriceIndexPermille, true);
        }

        private DebugCommandResult ApplyResources(int funds, int supplies, int intelligence,
            int foodInventory, int foodPriceIndexPermille, int materialsPriceIndexPermille,
            bool applyMarket)
        {
            if (!IndustryResources.TryCreate(funds, supplies, intelligence,
                    out IndustryResources resources, out string resourceError))
                return Reject("resource.invalid", resourceError);
            MarketState marketBefore = _session.DebugMarketState;
            MarketState marketAfter = marketBefore;
            if (applyMarket && !MarketState.TryCreate(foodInventory, foodPriceIndexPermille,
                    materialsPriceIndexPermille, marketBefore.LastSettledMonthIndex,
                    marketBefore.ConsecutiveFoodShortageMonths,
                    marketBefore.RecentCompositeIndices, out marketAfter, out string marketError))
                return Reject("market.invalid", marketError);

            IndustryResources before = _session.DebugIndustryState.Resources;
            try
            {
                if (!_session.DebugIndustryState.TrySetResources(resources, out string setError))
                    throw new InvalidOperationException(setError);
                CommitFaultInjection?.Invoke();
                if (applyMarket)
                    _session.DebugReplaceMarketState(marketAfter);
            }
            catch (Exception exception)
            {
                _session.DebugIndustryState.TrySetResources(before, out _);
                _session.DebugReplaceMarketState(marketBefore);
                return DebugCommandResult.Fault("资源注入提交失败，所有字段已回滚：" +
                                                exception.Message);
            }

            var message = new StringBuilder();
            message.Append("已注入资源：资金 ").Append(before.Funds).Append(" → ")
                .Append(resources.Funds).Append("，物资 ").Append(before.Supplies).Append(" → ")
                .Append(resources.Supplies).Append("，情报 ").Append(before.Intelligence)
                .Append(" → ").Append(resources.Intelligence).Append("。");
            if (applyMarket)
            {
                message.Append("粮食 ").Append(marketBefore.FoodInventory).Append(" → ")
                    .Append(foodInventory).Append("，粮食价格 ").Append(marketBefore.FoodPriceIndexPermille)
                    .Append(" → ").Append(foodPriceIndexPermille).Append("‰，物资价格 ")
                    .Append(marketBefore.MaterialsPriceIndexPermille).Append(" → ")
                    .Append(materialsPriceIndexPermille).Append("‰。");
            }
            message.Append(InjectionNote).Append("未运行任何产出、消耗或结算。");
            EndCommand(true, message.ToString());
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        // ---------------------------------------------------------------- event commands

        /// <summary>Current event catalog rows in stable catalog order.</summary>
        public IReadOnlyList<DebugEventRow> EventCatalogRows
        {
            get
            {
                var rows = new List<DebugEventRow>();
                if (!IsActive || _session.EventCatalog == null)
                    return rows.AsReadOnly();
                IReadOnlyList<string> pending = PendingEventIds;
                foreach (EventDefinitionData definition in _session.EventCatalog.events)
                {
                    if (definition == null || string.IsNullOrEmpty(definition.eventId))
                        continue;
                    rows.Add(new DebugEventRow(definition.eventId,
                        TitleOf(definition.textId), Contains(pending, definition.eventId)));
                }
                return rows.AsReadOnly();
            }
        }

        public IReadOnlyList<string> PendingEventIds =>
            IsActive ? _session.DebugEventRuntime.PendingEventIds : Array.Empty<string>();

        /// <summary>
        /// Validates one event injection without queueing it, so the window can require an
        /// explicit confirmation before it bypasses the natural trigger conditions.
        /// </summary>
        public DebugCommandResult PreviewEventInjection(string eventId)
        {
            if (!IsActive)
                return DebugCommandResult.Ended();
            if (string.IsNullOrWhiteSpace(eventId))
                return Reject("event.empty", "请选择要注入的事件。");
            if (!_session.EventCatalogReady)
                return Reject("event.catalog_unavailable",
                    string.IsNullOrEmpty(_session.LastEventMessage)
                        ? "事件目录不可用。"
                        : _session.LastEventMessage);
            if (!CatalogContains(eventId))
                return Reject("event.unknown", "事件不在当前目录中：" + eventId + "。");
            if (!_session.DebugEventRuntime.CanInjectDebugEvent(eventId, out string queueError))
                return Reject("event.pending", queueError);
            return DebugCommandResult.Ok("可以注入：" + eventId + " · " + TitleOfEvent(eventId) +
                                         "（未检查自然触发条件）。", DebugChangeKind.DebugInjection);
        }

        /// <summary>
        /// Injects one already-known catalog event into the runtime queue. Natural trigger
        /// conditions are not checked, and the result is labelled as a debug injection.
        /// Option resolution still uses the real choice and effect pipeline.
        /// </summary>
        public DebugCommandResult InjectEvent(string eventId)
        {
            DebugCommandResult preview = PreviewEventInjection(eventId);
            if (!preview.Succeeded)
            {
                LastMessage = preview.Message;
                return preview;
            }
            if (!_session.DebugEventRuntime.TryInjectDebugEvent(eventId, out string error))
                return Reject("event.rejected", error);

            EndCommand(true, "已注入事件：" + eventId + " · " + TitleOfEvent(eventId) +
                             "（调试注入：未检查自然触发条件）。" +
                             "请在事件面板按正式选项与效果结算。");
            return DebugCommandResult.Ok(LastMessage, DebugChangeKind.DebugInjection);
        }

        private bool CatalogContains(string eventId)
        {
            if (_session.EventCatalog == null)
                return false;
            foreach (EventDefinitionData definition in _session.EventCatalog.events)
                if (definition != null &&
                    string.Equals(definition.eventId, eventId, StringComparison.Ordinal))
                    return true;
            return false;
        }

        private string TitleOfEvent(string eventId)
        {
            if (_session.EventCatalog == null)
                return string.Empty;
            foreach (EventDefinitionData definition in _session.EventCatalog.events)
                if (definition != null &&
                    string.Equals(definition.eventId, eventId, StringComparison.Ordinal))
                    return TitleOf(definition.textId);
            return string.Empty;
        }

        private string TitleOf(string textId)
        {
            EventTextCatalog text = _session.EventText;
            if (text == null || text.entries == null || string.IsNullOrEmpty(textId))
                return string.Empty;
            foreach (EventTextEntry entry in text.entries)
                if (entry != null && string.Equals(entry.textId, textId, StringComparison.Ordinal))
                    return entry.title ?? string.Empty;
            return string.Empty;
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            for (int i = 0; i < values.Count; i++)
                if (string.Equals(values[i], value, StringComparison.Ordinal))
                    return true;
            return false;
        }

        // ---------------------------------------------------------------- shared helpers

        /// <summary>
        /// Complete debug-relevant fingerprint. EditMode tests compare it before and after
        /// a rejected or faulted command to prove no partial commit survived.
        /// </summary>
        public string CaptureFingerprint()
        {
            var builder = new StringBuilder();
            if (!IsActive)
                return "inactive";
            SuccessionState succession = _session.Succession;
            SuccessionWorld world = succession.World;
            builder.Append("date=").Append(Today).Append(';');
            builder.Append("phase=").Append(succession.Phase).Append(';');
            builder.Append("head=").Append(succession.HeadPersonId.Value).Append(';');
            builder.Append("heir=").Append(succession.DesignatedHeirId.Value).Append(';');
            builder.Append("pending=").Append(succession.PendingSuccessorId.Value).Append('/')
                .Append(succession.PendingRegentId.Value).Append(';');
            builder.Append("count=").Append(succession.SuccessionCount).Append(';');
            foreach (PersonState person in world.Persons)
            {
                world.TryGetLife(person.Id, out SuccessionPersonLife life);
                bool member = world.Family.TryGetRole(person.Id, out FamilyMemberRole role);
                builder.Append("P:").Append(person.Id.Value).Append('|')
                    .Append(person.DisplayName).Append('|').Append((int)person.Gender).Append('|')
                    .Append(life.BirthYear).Append('|').Append(life.Alive ? 1 : 0).Append('|')
                    .Append(life.Retired ? 1 : 0).Append('|')
                    .Append(member ? ((int)role).ToString() : "-").Append(';');
            }
            foreach (KinshipLink link in world.Kinship.Links)
                builder.Append("K:").Append(link.From.Value).Append('>').Append(link.To.Value)
                    .Append(':').Append((int)link.Type).Append(';');
            foreach (FamilyPregnancyRecord record in world.Pregnancies.Records)
                builder.Append("G:").Append(record.GestatingPersonId.Value).Append('>')
                    .Append(record.OtherParentId.Value).Append(';');
            IndustryResources resources = _session.DebugIndustryState.Resources;
            builder.Append("R:").Append(resources.Funds).Append('/').Append(resources.Supplies)
                .Append('/').Append(resources.Intelligence).Append(';');
            MarketState market = _session.DebugMarketState;
            builder.Append("M:").Append(market.FoodInventory).Append('/')
                .Append(market.FoodPriceIndexPermille).Append('/')
                .Append(market.MaterialsPriceIndexPermille).Append('/')
                .Append(market.LastSettledMonthIndex).Append(';');
            builder.Append("I:").Append(_session.DebugIndustryState.LastSettledMonthIndex)
                .Append(';');
            builder.Append("E:").Append(string.Join(",", PendingEventIds)).Append(';');
            builder.Append("H:").Append((int)succession.Phase).Append(';');
            return builder.ToString();
        }

        private void EndCommand(bool succeeded, string message)
        {
            LastMessage = message ?? string.Empty;
            if (succeeded)
                _session.DebugPublishStateChanged();
        }

        private static DebugCommandResult Reject(string code, string message) =>
            DebugCommandResult.Reject(code, message);

        private PersonId NextPersonId()
        {
            SuccessionWorld world = _session.Succession.World;
            for (int guard = 0; guard < 100000; guard++)
            {
                _nextPersonNumber++;
                var candidate = new PersonId(DebugPersonIdPrefix +
                                             _nextPersonNumber.ToString(
                                                 System.Globalization.CultureInfo.InvariantCulture));
                if (!world.TryGetPerson(candidate, out _))
                    return candidate;
            }
            return default;
        }

        /// <summary>
        /// Builds one replacement person through the domain rename rule, so a blank name is
        /// rejected by the same <see cref="PersonState.TryRename"/> contract every other
        /// rename uses. The new instance keeps the permanent ID and never mutates the live
        /// person, which is what makes a rollback exact.
        /// </summary>
        private static bool TryCreatePersonState(PersonId id, string name, PersonGender gender,
            out PersonState person)
        {
            person = new PersonState(id, string.Empty, gender);
            if (!person.TryRename(name ?? string.Empty, out _))
            {
                person = null;
                return false;
            }
            return true;
        }

        private DebugCommandResult ValidatePersonFacts(string name, PersonGender gender,
            int birthYear)
        {
            if (!TryCreatePersonState(new PersonId(DebugPersonIdPrefix + "validate"), name, gender,
                    out _))
                return DebugCommandResult.Reject("person.invalid_name", "姓名不能为空。");
            if (!Enum.IsDefined(typeof(PersonGender), gender))
                return DebugCommandResult.Reject("person.invalid_gender", "性别无效。");
            if (birthYear <= 0)
                return DebugCommandResult.Reject("person.invalid_birth", "出生年必须是正整数。");
            if (birthYear > Today.Year)
                return DebugCommandResult.Reject("person.future_birth",
                    "出生年不能晚于当前游戏年（" + Today.Year + "）。");
            return DebugCommandResult.Ok(string.Empty, DebugChangeKind.DebugInjection);
        }

        private static string NameOf(SuccessionWorld world, PersonId personId) =>
            world.TryGetPerson(personId, out PersonState person)
                ? person.DisplayName
                : personId.Value ?? string.Empty;

        public static string GenderNameOf(PersonGender gender)
        {
            switch (gender)
            {
                case PersonGender.Male: return "男";
                case PersonGender.Female: return "女";
                default: return "未定";
            }
        }

        public static string RoleNameOf(FamilyMemberRole role)
        {
            switch (role)
            {
                case FamilyMemberRole.Head: return "家主";
                case FamilyMemberRole.Spouse: return "配偶";
                case FamilyMemberRole.DirectKin: return "直系亲族";
                case FamilyMemberRole.CollateralKin: return "旁系亲族";
                case FamilyMemberRole.AdoptedChild: return "养子/养女";
                case FamilyMemberRole.Retainer: return "家臣";
                case FamilyMemberRole.Disciple: return "弟子";
                case FamilyMemberRole.Guest: return "客将";
                default: return role.ToString();
            }
        }

        public static string TypeNameOf(KinshipType type)
        {
            switch (type)
            {
                case KinshipType.ParentChild: return "亲子";
                case KinshipType.Sibling: return "兄弟姐妹";
                case KinshipType.Spouse: return "配偶";
                case KinshipType.Adoption: return "收养";
                default: return type.ToString();
            }
        }

        private static string PhaseName(SuccessionPhase phase)
        {
            switch (phase)
            {
                case SuccessionPhase.AwaitingSuccessionChoice: return "待选继承人";
                case SuccessionPhase.AwaitingRegentChoice: return "待选摄政";
                case SuccessionPhase.Regency: return "摄政中";
                case SuccessionPhase.CrisisDecision: return "危机决策";
                case SuccessionPhase.Extinct: return "家族灭亡";
                default: return "无";
            }
        }

        // ---------------------------------------------------------------- transaction

        private sealed class Transaction
        {
            internal SuccessionWorldSnapshot World;
            internal PersonId HeadId;
            internal PersonId DesignatedHeirId;
            internal PersonId PendingSuccessorId;
            internal PersonId PendingRegentId;
            internal SuccessionPhase Phase;
            internal int SuccessionCount;
            internal bool CrisisAdoptionUsed;
            internal bool RegencyCompleted;
            internal string CompletedResultId = string.Empty;
        }

        private Transaction BeginTransaction()
        {
            SuccessionState succession = _session.Succession;
            return new Transaction
            {
                World = succession.World.CaptureSnapshot(),
                HeadId = succession.HeadPersonId,
                DesignatedHeirId = succession.DesignatedHeirId,
                PendingSuccessorId = succession.PendingSuccessorId,
                PendingRegentId = succession.PendingRegentId,
                Phase = succession.Phase,
                SuccessionCount = succession.SuccessionCount,
                CrisisAdoptionUsed = succession.CrisisAdoptionUsed,
                RegencyCompleted = succession.RegencyCompleted,
                CompletedResultId = succession.CompletedResultId ?? string.Empty
            };
        }

        private void Rollback(Transaction transaction)
        {
            if (transaction == null)
                return;
            SuccessionState succession = _session.Succession;
            succession.World.TryRestoreSnapshot(transaction.World);
            succession.HeadPersonId = transaction.HeadId;
            succession.DesignatedHeirId = transaction.DesignatedHeirId;
            succession.PendingSuccessorId = transaction.PendingSuccessorId;
            succession.PendingRegentId = transaction.PendingRegentId;
            succession.Phase = transaction.Phase;
            succession.SuccessionCount = transaction.SuccessionCount;
            succession.CrisisAdoptionUsed = transaction.CrisisAdoptionUsed;
            succession.RegencyCompleted = transaction.RegencyCompleted;
            succession.CompletedResultId = transaction.CompletedResultId;
        }
    }
}
#endif
#pragma warning restore UAC0009
