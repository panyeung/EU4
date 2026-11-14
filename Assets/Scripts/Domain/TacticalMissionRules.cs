using System;
using System.Collections.Generic;

namespace SengokuChronicle.Domain
{
    /// <summary>One cell on the fixed M06 map. X increases east, Y increases north.</summary>
    public readonly struct TacticalGridPoint : IEquatable<TacticalGridPoint>
    {
        public TacticalGridPoint(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public bool Equals(TacticalGridPoint other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is TacticalGridPoint other && Equals(other);
        public override int GetHashCode() => unchecked((X * 397) ^ Y);
        public override string ToString() => "(" + X + "," + Y + ")";

        public static bool operator ==(TacticalGridPoint left, TacticalGridPoint right) =>
            left.Equals(right);

        public static bool operator !=(TacticalGridPoint left, TacticalGridPoint right) =>
            !left.Equals(right);
    }

    /// <summary>Stable map tile kinds of the authored M06 operation.</summary>
    public enum TacticalTileKind
    {
        Floor = 0,
        Wall = 1,
        Cover = 2,
        Extraction = 3
    }

    /// <summary>One orthogonal direction; doubles as a guard facing.</summary>
    public enum TacticalDirection
    {
        North = 0,
        East = 1,
        South = 2,
        West = 3
    }

    /// <summary>What a guard is doing during one response.</summary>
    public enum TacticalGuardState
    {
        Patrol = 0,
        Investigating = 1,
        Alert = 2
    }

    /// <summary>Lifecycle of the single M06 operation; values feed chronicle validation.</summary>
    public enum TacticalMissionStage
    {
        NotStarted = 0,
        InProgress = 1,
        Succeeded = 2,
        Failed = 3,
        Withdrawn = 4
    }

    /// <summary>Terminal mission result; <see cref="None"/> means the operation is still live.</summary>
    public enum TacticalMissionOutcome
    {
        None = 0,
        Succeeded = 1,
        Failed = 2,
        Withdrawn = 3
    }

    /// <summary>
    /// The fixed authored terrain and patrol of <c>mission.m06.intercepted-dispatch</c>.
    /// Every value is a constant of the scenario, never generated and never random.
    /// </summary>
    public static class TacticalMissionScenario
    {
        public const int Width = 8;
        public const int Height = 6;
        public const int SightDepth = 4;
        public const int SightHalfWidth = 1;
        public const int NoiseRadius = 3;
        public const int PatrolResetTurns = 2;
        public const int SupplyCost = 2;
        public const int SuccessFunds = 4;
        public const int SuccessIntelligence = 6;
        public const int OperativeCount = 2;

        public static readonly TacticalGridPoint DispatchCell = new TacticalGridPoint(6, 4);
        public static readonly TacticalGridPoint StationaryGuardCell = new TacticalGridPoint(6, 3);
        public static readonly TacticalDirection StationaryGuardFacing = TacticalDirection.North;
        public static readonly TacticalDirection PatrolGuardFacing = TacticalDirection.East;

        private static readonly TacticalGridPoint[] WallCells =
        {
            new TacticalGridPoint(3, 1),
            new TacticalGridPoint(3, 2),
            new TacticalGridPoint(3, 3),
            new TacticalGridPoint(5, 2)
        };

        private static readonly TacticalGridPoint[] CoverCells =
        {
            new TacticalGridPoint(2, 2),
            new TacticalGridPoint(4, 4),
            new TacticalGridPoint(5, 4)
        };

        private static readonly TacticalGridPoint[] ExtractionCellValues =
        {
            new TacticalGridPoint(0, 0),
            new TacticalGridPoint(1, 0)
        };

        /// <summary>
        /// One authored loop for the patrol guard. The route is walked in order and wraps.
        /// It sweeps the eastern flank of the dispatch, so a player who lingers in the
        /// guard's north-facing cone is seen while the concealed western approach stays
        /// open.
        /// </summary>
        private static readonly TacticalGridPoint[] PatrolRouteValues =
        {
            new TacticalGridPoint(7, 1),
            new TacticalGridPoint(7, 0)
        };

        public static IReadOnlyList<TacticalGridPoint> Walls => WallCells;
        public static IReadOnlyList<TacticalGridPoint> Cover => CoverCells;
        public static IReadOnlyList<TacticalGridPoint> ExtractionCells => ExtractionCellValues;
        public static IReadOnlyList<TacticalGridPoint> PatrolRoute => PatrolRouteValues;

        public static string DirectionName(TacticalDirection direction)
        {
            switch (direction)
            {
                case TacticalDirection.North: return "北";
                case TacticalDirection.East: return "东";
                case TacticalDirection.South: return "南";
                default: return "西";
            }
        }

        public static string GuardStateName(TacticalGuardState state)
        {
            switch (state)
            {
                case TacticalGuardState.Alert: return "警戒";
                case TacticalGuardState.Investigating: return "调查中";
                default: return "巡逻";
            }
        }

        public static string OutcomeName(TacticalMissionOutcome outcome)
        {
            switch (outcome)
            {
                case TacticalMissionOutcome.Succeeded: return "成功";
                case TacticalMissionOutcome.Failed: return "失败";
                case TacticalMissionOutcome.Withdrawn: return "撤退";
                default: return "未开始";
            }
        }
    }

    /// <summary>Pure map, vision, noise and path rules for the fixed M06 scenario.</summary>
    public static class TacticalMissionRules
    {
        public static bool IsInside(TacticalGridPoint point) =>
            point.X >= 0 && point.X < TacticalMissionScenario.Width &&
            point.Y >= 0 && point.Y < TacticalMissionScenario.Height;

        public static bool IsWall(TacticalGridPoint point)
        {
            for (int i = 0; i < TacticalMissionScenario.Walls.Count; i++)
                if (TacticalMissionScenario.Walls[i] == point)
                    return true;
            return false;
        }

        public static bool IsCover(TacticalGridPoint point)
        {
            for (int i = 0; i < TacticalMissionScenario.Cover.Count; i++)
                if (TacticalMissionScenario.Cover[i] == point)
                    return true;
            return false;
        }

        public static bool IsExtraction(TacticalGridPoint point)
        {
            for (int i = 0; i < TacticalMissionScenario.ExtractionCells.Count; i++)
                if (TacticalMissionScenario.ExtractionCells[i] == point)
                    return true;
            return false;
        }

        public static TacticalTileKind TileAt(TacticalGridPoint point)
        {
            if (!IsInside(point)) return TacticalTileKind.Floor;
            if (IsWall(point)) return TacticalTileKind.Wall;
            if (IsExtraction(point)) return TacticalTileKind.Extraction;
            if (IsCover(point)) return TacticalTileKind.Cover;
            return TacticalTileKind.Floor;
        }

        public static int ManhattanDistance(TacticalGridPoint first, TacticalGridPoint second) =>
            Math.Abs(first.X - second.X) + Math.Abs(first.Y - second.Y);

        public static TacticalGridPoint Step(TacticalGridPoint point, TacticalDirection direction)
        {
            switch (direction)
            {
                case TacticalDirection.North: return new TacticalGridPoint(point.X, point.Y + 1);
                case TacticalDirection.East: return new TacticalGridPoint(point.X + 1, point.Y);
                case TacticalDirection.South: return new TacticalGridPoint(point.X, point.Y - 1);
                default: return new TacticalGridPoint(point.X - 1, point.Y);
            }
        }

        public static bool TryDirectionFromStep(TacticalGridPoint from, TacticalGridPoint to,
            out TacticalDirection direction)
        {
            direction = default;
            if (to.X == from.X && to.Y == from.Y + 1) { direction = TacticalDirection.North; return true; }
            if (to.X == from.X + 1 && to.Y == from.Y) { direction = TacticalDirection.East; return true; }
            if (to.X == from.X && to.Y == from.Y - 1) { direction = TacticalDirection.South; return true; }
            if (to.X == from.X - 1 && to.Y == from.Y) { direction = TacticalDirection.West; return true; }
            return false;
        }

        /// <summary>
        /// True when the target lies inside the guard's 3x4 facing rectangle. A wall in a
        /// sight column blocks that column beyond the wall. Cover hides the target unless
        /// the target is adjacent, and a sprinting operative is exposed even on cover.
        /// </summary>
        public static bool CanSee(TacticalGridPoint guard, TacticalDirection facing,
            TacticalGridPoint target, bool targetOnCover, bool sprintExposed)
        {
            if (guard == target)
                return false;
            int dx = target.X - guard.X;
            int dy = target.Y - guard.Y;
            int depth;
            switch (facing)
            {
                case TacticalDirection.North:
                    if (dy < 1 || dy > TacticalMissionScenario.SightDepth ||
                        Math.Abs(dx) > TacticalMissionScenario.SightHalfWidth)
                        return false;
                    depth = dy;
                    break;
                case TacticalDirection.East:
                    if (dx < 1 || dx > TacticalMissionScenario.SightDepth ||
                        Math.Abs(dy) > TacticalMissionScenario.SightHalfWidth)
                        return false;
                    depth = dx;
                    break;
                case TacticalDirection.South:
                    if (dy > -1 || dy < -TacticalMissionScenario.SightDepth ||
                        Math.Abs(dx) > TacticalMissionScenario.SightHalfWidth)
                        return false;
                    depth = -dy;
                    break;
                default:
                    if (dx > -1 || dx < -TacticalMissionScenario.SightDepth ||
                        Math.Abs(dy) > TacticalMissionScenario.SightHalfWidth)
                        return false;
                    depth = -dx;
                    break;
            }

            if (targetOnCover && !sprintExposed &&
                ManhattanDistance(guard, target) > 1)
                return false;

            // Walk the straight sight column back towards the guard; a wall in between
            // blocks the whole column beyond it.
            for (int step = depth - 1; step >= 1; step--)
            {
                TacticalGridPoint between;
                switch (facing)
                {
                    case TacticalDirection.North:
                        between = new TacticalGridPoint(target.X, guard.Y + step);
                        break;
                    case TacticalDirection.East:
                        between = new TacticalGridPoint(guard.X + step, target.Y);
                        break;
                    case TacticalDirection.South:
                        between = new TacticalGridPoint(target.X, guard.Y - step);
                        break;
                    default:
                        between = new TacticalGridPoint(guard.X - step, target.Y);
                        break;
                }
                if (IsWall(between))
                    return false;
            }

            return true;
        }

        /// <summary>True when sprint noise reaches the guard (Manhattan distance 3).</summary>
        public static bool CanHear(TacticalGridPoint guard, TacticalGridPoint noise) =>
            ManhattanDistance(guard, noise) <= TacticalMissionScenario.NoiseRadius;

        /// <summary>
        /// Deterministic breadth-first step from <paramref name="start"/> towards
        /// <paramref name="goal"/>. Neighbour order is fixed N/E/S/W, so equal-length paths
        /// always resolve the same way. The goal itself is always enterable; the caller
        /// decides what the goal means (a waypoint, a noise cell, or an operative).
        /// </summary>
        public static bool TryFindNextStep(TacticalGridPoint start, TacticalGridPoint goal,
            Func<TacticalGridPoint, bool> blocked, out TacticalGridPoint next)
        {
            next = start;
            if (start == goal)
                return false;

            var parents = new Dictionary<TacticalGridPoint, TacticalGridPoint>();
            var visited = new HashSet<TacticalGridPoint> { start };
            var queue = new Queue<TacticalGridPoint>();
            queue.Enqueue(start);
            TacticalDirection[] order =
            {
                TacticalDirection.North, TacticalDirection.East,
                TacticalDirection.South, TacticalDirection.West
            };

            bool found = false;
            while (queue.Count > 0 && !found)
            {
                TacticalGridPoint current = queue.Dequeue();
                for (int i = 0; i < order.Length; i++)
                {
                    TacticalGridPoint neighbour = Step(current, order[i]);
                    if (!IsInside(neighbour) || IsWall(neighbour) || visited.Contains(neighbour))
                        continue;
                    if (!(neighbour == goal) && blocked != null && blocked(neighbour))
                        continue;

                    visited.Add(neighbour);
                    parents[neighbour] = current;
                    if (neighbour == goal)
                    {
                        found = true;
                        break;
                    }
                    queue.Enqueue(neighbour);
                }
            }

            if (!found)
                return false;

            TacticalGridPoint step = goal;
            while (parents.TryGetValue(step, out TacticalGridPoint parent) && parent != start)
                step = parent;
            if (!parents.ContainsKey(step))
                return false;
            next = step;
            return true;
        }
    }
}
