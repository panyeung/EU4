using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>Plain result of one M06 command; never holds Unity objects.</summary>
    public readonly struct TacticalActionResult
    {
        public TacticalActionResult(bool succeeded, TacticalMissionStage stage,
            TacticalMissionOutcome outcome, string feedback, int turn, string lastEvent)
        {
            Succeeded = succeeded;
            Stage = stage;
            Outcome = outcome;
            Feedback = feedback ?? string.Empty;
            Turn = turn;
            LastEvent = lastEvent ?? string.Empty;
        }

        public bool Succeeded { get; }
        public TacticalMissionStage Stage { get; }
        public TacticalMissionOutcome Outcome { get; }
        public string Feedback { get; }
        public int Turn { get; }
        public string LastEvent { get; }
    }

    /// <summary>One operative as the UI sees it.</summary>
    public readonly struct TacticalOperativeView
    {
        public TacticalOperativeView(PersonId personId, TacticalGridPoint position,
            bool carryingDispatch)
        {
            PersonId = personId;
            Position = position;
            CarryingDispatch = carryingDispatch;
        }

        public PersonId PersonId { get; }
        public TacticalGridPoint Position { get; }
        public bool CarryingDispatch { get; }
    }

    /// <summary>One guard as the UI sees it.</summary>
    public readonly struct TacticalGuardView
    {
        public TacticalGuardView(int unitId, TacticalGridPoint position,
            TacticalDirection facing, TacticalGuardState state)
        {
            UnitId = unitId;
            Position = position;
            Facing = facing;
            State = state;
        }

        public int UnitId { get; }
        public TacticalGridPoint Position { get; }
        public TacticalDirection Facing { get; }
        public TacticalGuardState State { get; }
    }

    /// <summary>One map cell with its static kind and current guard sight.</summary>
    public readonly struct TacticalTileView
    {
        public TacticalTileView(int x, int y, TacticalTileKind kind, bool inGuardSight)
        {
            X = x;
            Y = y;
            Kind = kind;
            InGuardSight = inGuardSight;
        }

        public int X { get; }
        public int Y { get; }
        public TacticalTileKind Kind { get; }
        public bool InGuardSight { get; }
    }

    /// <summary>
    /// Immutable read-only projection of the operation for the panel. The snapshot is
    /// rebuilt on demand; rendering it never advances a turn or moves a guard.
    /// </summary>
    public sealed class TacticalMissionSnapshot
    {
        public TacticalMissionSnapshot(TacticalMissionStage stage, TacticalMissionOutcome outcome,
            int turn, GameDate launchDate, IReadOnlyList<TacticalTileView> tiles,
            IReadOnlyList<TacticalOperativeView> operatives,
            IReadOnlyList<TacticalGuardView> guards, TacticalGridPoint dispatchPosition,
            bool dispatchTaken, string lastEvent, string feedback)
        {
            Stage = stage;
            Outcome = outcome;
            Turn = turn;
            LaunchDate = launchDate;
            Tiles = tiles;
            Operatives = operatives;
            Guards = guards;
            DispatchPosition = dispatchPosition;
            DispatchTaken = dispatchTaken;
            LastEvent = lastEvent ?? string.Empty;
            Feedback = feedback ?? string.Empty;
        }

        public TacticalMissionStage Stage { get; }
        public TacticalMissionOutcome Outcome { get; }
        public int Turn { get; }
        public GameDate LaunchDate { get; }
        public IReadOnlyList<TacticalTileView> Tiles { get; }
        public IReadOnlyList<TacticalOperativeView> Operatives { get; }
        public IReadOnlyList<TacticalGuardView> Guards { get; }
        public TacticalGridPoint DispatchPosition { get; }
        public bool DispatchTaken { get; }
        public string LastEvent { get; }
        public string Feedback { get; }

        public bool IsActive => Stage == TacticalMissionStage.InProgress;

        public TacticalTileView TileAt(int x, int y)
        {
            for (int i = 0; i < Tiles.Count; i++)
                if (Tiles[i].X == x && Tiles[i].Y == y)
                    return Tiles[i];
            return default;
        }

        public int OperativeIndex(PersonId personId)
        {
            for (int i = 0; i < Operatives.Count; i++)
                if (Operatives[i].PersonId == personId)
                    return i;
            return -1;
        }
    }

    /// <summary>
    /// The single M06 tactical operation. The domain owns movement, guard perception,
    /// patrol, alert, extraction and the terminal result; it never advances campaign
    /// time, never rolls randomness and never touches campaign resources or the
    /// chronicle. Those are applied by <c>GameSession</c> as one transaction.
    /// </summary>
    public sealed class TacticalMissionState
    {
        private sealed class Operative
        {
            public PersonId PersonId;
            public TacticalGridPoint Position;
            public bool CarryingDispatch;
        }

        private sealed class Guard
        {
            public int UnitId;
            public TacticalGridPoint Position;
            public TacticalDirection Facing;
            public TacticalGuardState State;
            public TacticalGridPoint? Target;
            public int ContactTurns;
            public int RouteIndex;
        }

        private readonly List<Operative> _operatives = new List<Operative>();
        private readonly List<Guard> _guards = new List<Guard>();
        private readonly List<TacticalGridPoint> _patrolRoute =
            new List<TacticalGridPoint>(TacticalMissionScenario.PatrolRoute);

        public TacticalMissionStage Stage { get; private set; } = TacticalMissionStage.NotStarted;
        public TacticalMissionOutcome Outcome { get; private set; } = TacticalMissionOutcome.None;
        public int Turn { get; private set; }
        public GameDate LaunchDate { get; private set; }
        public bool DispatchTaken { get; private set; }
        /// <summary>True once any guard has reached alert during this operation.</summary>
        public bool AlertTriggered { get; private set; }
        /// <summary>M07 captain chosen from the two operatives; session-only.</summary>
        public PersonId CaptainId { get; private set; }
        /// <summary>M07 frozen preparation; session-only.</summary>
        public MissionPreparation Preparation { get; private set; }
        public string LastEvent { get; private set; } = string.Empty;
        public string Feedback { get; private set; } = string.Empty;

        public int OperativeCount => _operatives.Count;
        public int GuardCount => _guards.Count;
        public bool IsActive => Stage == TacticalMissionStage.InProgress;
        public bool IsTerminal => Stage == TacticalMissionStage.Succeeded ||
                                  Stage == TacticalMissionStage.Failed ||
                                  Stage == TacticalMissionStage.Withdrawn;

        public PersonId OperativeId(int slot) => _operatives[slot].PersonId;
        public TacticalGridPoint OperativePosition(int slot) => _operatives[slot].Position;
        public bool OperativeCarryingDispatch(int slot) => _operatives[slot].CarryingDispatch;
        public bool IsOperative(PersonId personId) => OperativeSlot(personId) >= 0;
        public TacticalGuardState GuardStateAt(int slot) => _guards[slot].State;
        public TacticalGridPoint GuardPositionAt(int slot) => _guards[slot].Position;
        public TacticalDirection GuardFacingAt(int slot) => _guards[slot].Facing;

        /// <summary>
        /// Creates the authored operation for exactly two distinct operatives. Positions
        /// begin on the two extraction cells and both guards start on their fixed posts.
        /// </summary>
        public static bool TryCreate(PersonId first, PersonId second, GameDate launchDate,
            out TacticalMissionState state, out string error) =>
            TryCreate(first, second, first, MissionPreparation.None, launchDate, out state,
                out error);

        /// <summary>
        /// M07 overload: freezes the briefing captain and preparation choice. Neither is
        /// consulted for rewards; they only identify the actor and the chosen utility.
        /// </summary>
        public static bool TryCreate(PersonId first, PersonId second, PersonId captain,
            MissionPreparation preparation, GameDate launchDate,
            out TacticalMissionState state, out string error)
        {
            state = null;
            error = string.Empty;
            if (first.IsEmpty || second.IsEmpty)
            {
                error = "潜入需要两名在世的家族成员。";
                return false;
            }
            if (first == second)
            {
                error = "潜入的两名成员不能是同一个人。";
                return false;
            }
            if (captain.IsEmpty || (captain != first && captain != second))
            {
                error = "潜入队长必须是两名出发成员之一。";
                return false;
            }
            if (!Enum.IsDefined(typeof(MissionPreparation), preparation))
            {
                error = "潜入准备选项无效。";
                return false;
            }
            if (!launchDate.IsValid)
            {
                error = "潜入开始日期无效。";
                return false;
            }

            var created = new TacticalMissionState
            {
                LaunchDate = launchDate,
                Stage = TacticalMissionStage.InProgress,
                CaptainId = captain,
                Preparation = preparation
            };
            created._operatives.Add(new Operative
            {
                PersonId = first,
                Position = TacticalMissionScenario.ExtractionCells[0]
            });
            created._operatives.Add(new Operative
            {
                PersonId = second,
                Position = TacticalMissionScenario.ExtractionCells[1]
            });
            created._guards.Add(new Guard
            {
                UnitId = 1,
                Position = created._patrolRoute[0],
                Facing = TacticalMissionScenario.PatrolGuardFacing,
                State = TacticalGuardState.Patrol,
                RouteIndex = 1
            });
            created._guards.Add(new Guard
            {
                UnitId = 2,
                Position = TacticalMissionScenario.StationaryGuardCell,
                Facing = TacticalMissionScenario.StationaryGuardFacing,
                State = TacticalGuardState.Patrol,
                RouteIndex = 0
            });
            created.LastEvent = "潜入开始：夺取密报后送两名潜入者到撤离点。";
            created.Feedback = created.LastEvent;
            state = created;
            return true;
        }

        public TacticalActionResult TryMove(PersonId operativeId, TacticalDirection direction)
        {
            if (Stage != TacticalMissionStage.InProgress)
                return Reject("潜入已经结束。");
            int slot = OperativeSlot(operativeId);
            if (slot < 0)
                return Reject("该人物不在此次潜入中。");

            TacticalGridPoint target = TacticalMissionRules.Step(
                _operatives[slot].Position, direction);
            string block = OperativeCellBlockReason(target, slot);
            if (block != null)
                return Reject(block);

            _operatives[slot].Position = target;
            LastEvent = OperativeName(slot) + "向" +
                        TacticalMissionScenario.DirectionName(direction) + "移动到 " + target + "。";
            return AdvanceTurn(-1, null);
        }

        public TacticalActionResult TrySprint(PersonId operativeId, TacticalDirection direction)
        {
            if (Stage != TacticalMissionStage.InProgress)
                return Reject("潜入已经结束。");
            int slot = OperativeSlot(operativeId);
            if (slot < 0)
                return Reject("该人物不在此次潜入中。");

            TacticalGridPoint middle = TacticalMissionRules.Step(
                _operatives[slot].Position, direction);
            string middleBlock = OperativeCellBlockReason(middle, slot);
            if (middleBlock != null)
                return Reject("疾行受阻：" + middleBlock);
            TacticalGridPoint destination = TacticalMissionRules.Step(middle, direction);
            string destinationBlock = OperativeCellBlockReason(destination, slot);
            if (destinationBlock != null)
                return Reject("疾行受阻：" + destinationBlock);

            _operatives[slot].Position = destination;
            LastEvent = OperativeName(slot) + "向" +
                        TacticalMissionScenario.DirectionName(direction) + "疾行到 " +
                        destination + "，发出响声。";
            // The sprint exposes this operative for the response even on cover, and the
            // destination is the noise every guard within radius 3 investigates.
            return AdvanceTurn(slot, destination);
        }

        public TacticalActionResult TryWait(PersonId operativeId)
        {
            if (Stage != TacticalMissionStage.InProgress)
                return Reject("潜入已经结束。");
            int slot = OperativeSlot(operativeId);
            if (slot < 0)
                return Reject("该人物不在此次潜入中。");

            LastEvent = OperativeName(slot) + "原地等待。";
            return AdvanceTurn(-1, null);
        }

        public TacticalActionResult TryInteract(PersonId operativeId)
        {
            if (Stage != TacticalMissionStage.InProgress)
                return Reject("潜入已经结束。");
            int slot = OperativeSlot(operativeId);
            if (slot < 0)
                return Reject("该人物不在此次潜入中。");
            if (DispatchTaken)
                return Reject("密报已经被取走。");
            if (TacticalMissionRules.ManhattanDistance(_operatives[slot].Position,
                    TacticalMissionScenario.DispatchCell) != 1)
                return Reject("只有与密报正交相邻的潜入者才能夺取。");

            DispatchTaken = true;
            _operatives[slot].CarryingDispatch = true;
            LastEvent = OperativeName(slot) + "夺取了密报。";
            return AdvanceTurn(-1, null);
        }

        /// <summary>
        /// Ends the operation in success only when the dispatch is carried and the two
        /// operatives stand on the two extraction cells.
        /// </summary>
        public TacticalActionResult TryExtract()
        {
            if (Stage != TacticalMissionStage.InProgress)
                return Reject("潜入已经结束。");
            if (!DispatchTaken)
                return Reject("尚未夺取密报，无法撤离。");

            IReadOnlyList<TacticalGridPoint> cells = TacticalMissionScenario.ExtractionCells;
            TacticalGridPoint first = _operatives[0].Position;
            TacticalGridPoint second = _operatives[1].Position;
            bool bothOnExtraction =
                (first == cells[0] && second == cells[1]) ||
                (first == cells[1] && second == cells[0]);
            if (!bothOnExtraction)
                return Reject("两名潜入者必须分别占据两个撤离格才能撤离。");

            Turn++;
            Stage = TacticalMissionStage.Succeeded;
            Outcome = TacticalMissionOutcome.Succeeded;
            LastEvent = "密报送达，潜入成功。";
            Feedback = "潜入成功：密报已送达（用时 " + Turn + " 回合）。";
            return Result(true, Feedback);
        }

        public TacticalActionResult TryWithdraw()
        {
            if (Stage != TacticalMissionStage.InProgress)
                return Reject("潜入已经结束。");

            Turn++;
            Stage = TacticalMissionStage.Withdrawn;
            Outcome = TacticalMissionOutcome.Withdrawn;
            LastEvent = "潜入者主动撤退。";
            Feedback = "潜入撤退：本次未取得密报（用时 " + Turn + " 回合）。";
            return Result(true, Feedback);
        }

        /// <summary>
        /// Reverts a terminal result that could not be committed (a rejected chronicle
        /// row) so the player can retry. The command's turn stays consumed; only the
        /// terminal state is undone. Production calls this from the session transaction.
        /// </summary>
        internal void RevertTerminalResultForRetry()
        {
            if (!IsTerminal)
                return;
            Stage = TacticalMissionStage.InProgress;
            Outcome = TacticalMissionOutcome.None;
            LastEvent = "潜入结果未能结算，潜入者仍在场上。";
            Feedback = LastEvent;
        }

        /// <summary>Rebuilds the read-only projection; never mutates gameplay state.</summary>
        public TacticalMissionSnapshot CreateSnapshot()
        {
            var tiles = new TacticalTileView[
                TacticalMissionScenario.Width * TacticalMissionScenario.Height];
            int index = 0;
            for (int y = 0; y < TacticalMissionScenario.Height; y++)
            {
                for (int x = 0; x < TacticalMissionScenario.Width; x++)
                {
                    var point = new TacticalGridPoint(x, y);
                    bool seen = false;
                    for (int g = 0; g < _guards.Count; g++)
                    {
                        Guard guard = _guards[g];
                        if (TacticalMissionRules.CanSee(guard.Position, guard.Facing, point,
                                TacticalMissionRules.IsCover(point), false))
                        {
                            seen = true;
                            break;
                        }
                    }
                    tiles[index++] = new TacticalTileView(x, y,
                        TacticalMissionRules.TileAt(point), seen);
                }
            }

            var operatives = new TacticalOperativeView[_operatives.Count];
            for (int i = 0; i < _operatives.Count; i++)
                operatives[i] = new TacticalOperativeView(_operatives[i].PersonId,
                    _operatives[i].Position, _operatives[i].CarryingDispatch);

            var guards = new TacticalGuardView[_guards.Count];
            for (int i = 0; i < _guards.Count; i++)
                guards[i] = new TacticalGuardView(_guards[i].UnitId, _guards[i].Position,
                    _guards[i].Facing, _guards[i].State);

            return new TacticalMissionSnapshot(Stage, Outcome, Turn, LaunchDate,
                Array.AsReadOnly(tiles), Array.AsReadOnly(operatives), Array.AsReadOnly(guards),
                TacticalMissionScenario.DispatchCell, DispatchTaken, LastEvent, Feedback);
        }

        /// <summary>Test seam: places one operative. Never called by production code.</summary>
        internal void PlaceOperativeForTest(int slot, TacticalGridPoint position) =>
            _operatives[slot].Position = position;

        /// <summary>Test seam: places one guard. Never called by production code.</summary>
        internal void PlaceGuardForTest(int slot, TacticalGridPoint position,
            TacticalDirection facing, TacticalGuardState state)
        {
            _guards[slot].Position = position;
            _guards[slot].Facing = facing;
            _guards[slot].State = state;
            _guards[slot].ContactTurns = 0;
            _guards[slot].Target = null;
            _guards[slot].RouteIndex = 0;
        }

        private TacticalActionResult AdvanceTurn(int sprintSlot, TacticalGridPoint? noise)
        {
            Turn++;
            RunGuardResponse(sprintSlot, noise);
            if (Stage == TacticalMissionStage.Failed)
                return Result(false, Feedback);

            Feedback = "第 " + Turn + " 回合：" + LastEvent;
            return Result(true, Feedback);
        }

        private TacticalActionResult Fail(string reason)
        {
            Stage = TacticalMissionStage.Failed;
            Outcome = TacticalMissionOutcome.Failed;
            LastEvent = reason;
            Feedback = "潜入失败：" + reason + "（用时 " + Turn + " 回合）。";
            return Result(false, Feedback);
        }

        private void RunGuardResponse(int sprintSlot, TacticalGridPoint? noise)
        {
            var events = new List<string>();
            for (int guardIndex = 0; guardIndex < _guards.Count; guardIndex++)
            {
                if (Stage != TacticalMissionStage.InProgress)
                    return;

                Guard guard = _guards[guardIndex];
                int visible = FindVisibleOperative(guard, sprintSlot);
                if (visible >= 0)
                {
                    guard.State = TacticalGuardState.Alert;
                    AlertTriggered = true;
                    guard.ContactTurns = 0;
                    guard.Target = _operatives[visible].Position;
                    events.Add(GuardName(guardIndex) + "发现" + OperativeName(visible) +
                               "，进入警戒并追击");
                    if (MoveGuard(guard, _operatives[visible].Position, visible) &&
                        CatchOperative(guard, guardIndex))
                        return;
                    continue;
                }

                if (noise.HasValue && TacticalMissionRules.CanHear(guard.Position, noise.Value))
                {
                    guard.State = TacticalGuardState.Investigating;
                    guard.ContactTurns = 0;
                    guard.Target = noise.Value;
                    events.Add(GuardName(guardIndex) + "听到疾行声，前往调查");
                    if (MoveGuard(guard, noise.Value, -1) && CatchOperative(guard, guardIndex))
                        return;
                    continue;
                }

                if (guard.State == TacticalGuardState.Alert)
                {
                    // Sight was lost. Search the last known position, displayed as 调查中,
                    // so a concealed operative is not chased forever.
                    guard.State = TacticalGuardState.Investigating;
                    guard.ContactTurns = 0;
                    TacticalGridPoint target = guard.Target ?? guard.Position;
                    events.Add(GuardName(guardIndex) + "失去目标，转向最后位置调查");
                    if (MoveGuard(guard, target, -1) && CatchOperative(guard, guardIndex))
                        return;
                    continue;
                }

                if (guard.State == TacticalGuardState.Investigating)
                {
                    guard.ContactTurns++;
                    if (guard.ContactTurns >= TacticalMissionScenario.PatrolResetTurns)
                    {
                        guard.State = TacticalGuardState.Patrol;
                        guard.ContactTurns = 0;
                        guard.Target = null;
                        events.Add(GuardName(guardIndex) + "搜索无果，返回巡逻");
                        if (MoveAlongPatrol(guard, guardIndex) &&
                            CatchOperative(guard, guardIndex))
                            return;
                    }
                    else
                    {
                        TacticalGridPoint target = guard.Target ?? guard.Position;
                        events.Add(GuardName(guardIndex) + "继续调查");
                        if (MoveGuard(guard, target, -1) && CatchOperative(guard, guardIndex))
                            return;
                    }
                    continue;
                }

                if (MoveAlongPatrol(guard, guardIndex) && CatchOperative(guard, guardIndex))
                    return;
            }

            LastEvent = events.Count > 0 ? string.Join("；", events) : LastEvent;
        }

        private bool CatchOperative(Guard guard, int guardIndex)
        {
            int captured = OperativeAt(guard.Position);
            if (captured < 0)
                return false;
            Fail(GuardName(guardIndex) + "在 " + guard.Position + " 抓住了" +
                 OperativeName(captured));
            return true;
        }

        private bool MoveAlongPatrol(Guard guard, int guardIndex)
        {
            if (guardIndex == 0)
            {
                if (guard.RouteIndex >= _patrolRoute.Count)
                    guard.RouteIndex = 0;
                if (guard.Position == _patrolRoute[guard.RouteIndex])
                    guard.RouteIndex = (guard.RouteIndex + 1) % _patrolRoute.Count;
                return MoveGuard(guard, _patrolRoute[guard.RouteIndex], -1);
            }

            // The second guard's fixed patrol is its authored post: after an
            // investigation it walks back to (6,3) instead of drifting away.
            if (guard.Position == TacticalMissionScenario.StationaryGuardCell)
                return false;
            return MoveGuard(guard, TacticalMissionScenario.StationaryGuardCell, -1);
        }

        private bool MoveGuard(Guard guard, TacticalGridPoint goal, int allowedOperativeSlot)
        {
            if (guard.Position == goal)
                return false;
            bool moved = TacticalMissionRules.TryFindNextStep(guard.Position, goal,
                point => IsBlockedForGuard(point, guard, allowedOperativeSlot),
                out TacticalGridPoint next);
            if (!moved)
                return false;
            if (TacticalMissionRules.TryDirectionFromStep(guard.Position, next,
                    out TacticalDirection direction))
                guard.Facing = direction;
            guard.Position = next;
            return true;
        }

        private bool IsBlockedForGuard(TacticalGridPoint point, Guard guard,
            int allowedOperativeSlot)
        {
            for (int i = 0; i < _guards.Count; i++)
                if (_guards[i] != guard && _guards[i].Position == point)
                    return true;
            int operative = OperativeAt(point);
            return operative >= 0 && operative != allowedOperativeSlot;
        }

        private int FindVisibleOperative(Guard guard, int sprintSlot)
        {
            int best = -1;
            int bestDistance = int.MaxValue;
            for (int slot = 0; slot < _operatives.Count; slot++)
            {
                TacticalGridPoint position = _operatives[slot].Position;
                bool onCover = TacticalMissionRules.IsCover(position);
                if (!TacticalMissionRules.CanSee(guard.Position, guard.Facing, position,
                        onCover, slot == sprintSlot))
                    continue;
                int distance = TacticalMissionRules.ManhattanDistance(guard.Position, position);
                if (distance < bestDistance ||
                    (distance == bestDistance && best >= 0 &&
                     string.CompareOrdinal(_operatives[slot].PersonId.Value,
                         _operatives[best].PersonId.Value) < 0))
                {
                    best = slot;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private int OperativeSlot(PersonId personId)
        {
            for (int i = 0; i < _operatives.Count; i++)
                if (_operatives[i].PersonId == personId)
                    return i;
            return -1;
        }

        private int OperativeAt(TacticalGridPoint point)
        {
            for (int i = 0; i < _operatives.Count; i++)
                if (_operatives[i].Position == point)
                    return i;
            return -1;
        }

        private string OperativeCellBlockReason(TacticalGridPoint point, int movingSlot)
        {
            if (!TacticalMissionRules.IsInside(point))
                return "目标格越出 8×6 地图。";
            if (TacticalMissionRules.IsWall(point))
                return "目标格是墙壁。";
            for (int i = 0; i < _operatives.Count; i++)
                if (i != movingSlot && _operatives[i].Position == point)
                    return "目标格已被同伴占据。";
            for (int i = 0; i < _guards.Count; i++)
                if (_guards[i].Position == point)
                    return "目标格被守卫占据。";
            return null;
        }

        private TacticalActionResult Reject(string reason) =>
            new TacticalActionResult(false, Stage, Outcome,
                string.IsNullOrEmpty(reason) ? "无法执行该指令。" : reason, Turn, LastEvent);

        private TacticalActionResult Result(bool succeeded, string feedback) =>
            new TacticalActionResult(succeeded, Stage, Outcome,
                feedback ?? string.Empty, Turn, LastEvent);

        private static string OperativeName(int slot) => slot == 0 ? "潜入者一" : "潜入者二";

        private static string GuardName(int slot) => slot == 0 ? "巡逻守卫" : "驻守守卫";
    }
}
