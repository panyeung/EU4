using NUnit.Framework;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>
    /// Rule-level coverage for the fixed M06 operation: map bounds, movement legality,
    /// sprint noise, guard sight, cover, patrol reset, deterministic paths, extraction
    /// and the three terminal outcomes. Everything asserted here is player-visible.
    /// </summary>
    public sealed class TacticalMissionRulesTests
    {
        private static readonly PersonId First = new PersonId("person.test.first");
        private static readonly PersonId Second = new PersonId("person.test.second");

        private static TacticalMissionState NewState()
        {
            bool created = TacticalMissionState.TryCreate(First, Second,
                new GameDate(1560, 4, 1), out TacticalMissionState state, out string error);
            Assert.That(created, Is.True, error);
            return state;
        }

        [Test]
        public void Creation_RequiresTwoDistinctMembersAndAValidDate()
        {
            Assert.That(TacticalMissionState.TryCreate(default, Second,
                new GameDate(1560, 4, 1), out _, out string emptyError), Is.False);
            Assert.That(emptyError, Is.Not.Empty);
            Assert.That(TacticalMissionState.TryCreate(First, First,
                new GameDate(1560, 4, 1), out _, out _), Is.False);
            Assert.That(TacticalMissionState.TryCreate(First, Second, default,
                out _, out _), Is.False);

            TacticalMissionState state = NewState();
            Assert.That(state.Stage, Is.EqualTo(TacticalMissionStage.InProgress));
            Assert.That(state.OperativePosition(0),
                Is.EqualTo(new TacticalGridPoint(0, 0)));
            Assert.That(state.OperativePosition(1),
                Is.EqualTo(new TacticalGridPoint(1, 0)));
            Assert.That(state.GuardCount, Is.EqualTo(2));
            Assert.That(state.GuardPositionAt(1),
                Is.EqualTo(TacticalMissionScenario.StationaryGuardCell));
            Assert.That(state.GuardFacingAt(1), Is.EqualTo(TacticalDirection.North));
        }

        [Test]
        public void Move_RejectsBoundsWallsDiagonalsAndOccupiedCellsWithoutStateChange()
        {
            TacticalMissionState state = NewState();

            TacticalActionResult west = state.TryMove(First, TacticalDirection.West);
            Assert.That(west.Succeeded, Is.False);
            Assert.That(west.Feedback, Does.Contain("地图"));
            Assert.That(state.Turn, Is.Zero, "an invalid command must not consume a turn");
            Assert.That(state.OperativePosition(0),
                Is.EqualTo(new TacticalGridPoint(0, 0)));

            state.PlaceOperativeForTest(0, new TacticalGridPoint(2, 1));
            TacticalActionResult wall = state.TryMove(First, TacticalDirection.East);
            Assert.That(wall.Succeeded, Is.False);
            Assert.That(wall.Feedback, Does.Contain("墙"));
            Assert.That(state.OperativePosition(0),
                Is.EqualTo(new TacticalGridPoint(2, 1)));
            Assert.That(state.Turn, Is.Zero);

            state.PlaceOperativeForTest(0, new TacticalGridPoint(1, 1));
            state.PlaceOperativeForTest(1, new TacticalGridPoint(1, 2));
            TacticalActionResult occupied = state.TryMove(First, TacticalDirection.North);
            Assert.That(occupied.Succeeded, Is.False);
            Assert.That(occupied.Feedback, Does.Contain("同伴"));
            Assert.That(state.Turn, Is.Zero);

            // There is no diagonal command at all: the API only accepts N/E/S/W, so a
            // diagonal move is represented by two legal orthogonal steps.
            TacticalActionResult guardCell = state.TryMove(Second, TacticalDirection.North);
            Assert.That(guardCell.Succeeded, Is.True, guardCell.Feedback);
            Assert.That(state.OperativePosition(1),
                Is.EqualTo(new TacticalGridPoint(1, 3)));
        }

        [Test]
        public void Boundary_RejectsTheSouthAndEastEdgesOnMoveAndSprint()
        {
            TacticalMissionState state = NewState();
            Assert.That(state.TryMove(First, TacticalDirection.South).Succeeded, Is.False,
                "the cell south of (0,0) is outside the 8x6 map.");

            state.PlaceOperativeForTest(0, new TacticalGridPoint(7, 0));
            Assert.That(state.TryMove(First, TacticalDirection.East).Succeeded, Is.False,
                "the cell east of (7,0) is outside the 8x6 map.");
            Assert.That(state.TrySprint(First, TacticalDirection.East).Succeeded, Is.False,
                "a two-cell sprint cannot leave the map either.");
            Assert.That(state.OperativePosition(0),
                Is.EqualTo(new TacticalGridPoint(7, 0)));
            Assert.That(state.Turn, Is.Zero);
        }

        [Test]
        public void Sprint_NeedsTwoClearCellsAndEmitsNoiseAtTheDestination()
        {
            TacticalMissionState state = NewState();
            Assert.That(state.TrySprint(First, TacticalDirection.North).Succeeded, Is.True);
            Assert.That(state.OperativePosition(0),
                Is.EqualTo(new TacticalGridPoint(0, 2)));
            Assert.That(state.Turn, Is.EqualTo(1));

            state.PlaceOperativeForTest(0, new TacticalGridPoint(1, 1));
            TacticalActionResult wall = state.TrySprint(First, TacticalDirection.East);
            Assert.That(wall.Succeeded, Is.False);
            Assert.That(wall.Feedback, Does.Contain("墙"));
            Assert.That(state.Turn, Is.EqualTo(1));

            state.PlaceOperativeForTest(0, new TacticalGridPoint(7, 0));
            Assert.That(state.TrySprint(First, TacticalDirection.East).Succeeded, Is.False);

            state.PlaceOperativeForTest(0, new TacticalGridPoint(1, 0));
            state.PlaceOperativeForTest(1, new TacticalGridPoint(1, 2));
            Assert.That(state.TrySprint(First, TacticalDirection.North).Succeeded, Is.False,
                "the destination cell is occupied by the other operative.");
        }

        [Test]
        public void SprintNoise_IsHeardWithinThreeCellsAndRaisesInvestigating()
        {
            TacticalMissionState state = NewState();
            state.PlaceGuardForTest(0, new TacticalGridPoint(0, 5),
                TacticalDirection.East, TacticalGuardState.Patrol);

            Assert.That(state.TrySprint(First, TacticalDirection.North).Succeeded, Is.True);

            Assert.That(state.GuardStateAt(0), Is.EqualTo(TacticalGuardState.Investigating));
            Assert.That(state.GuardPositionAt(0),
                Is.EqualTo(new TacticalGridPoint(0, 4)),
                "a hearing guard moves one deterministic step toward the noise.");
            Assert.That(state.LastEvent, Does.Contain("疾行声"));
            Assert.That(state.Stage, Is.EqualTo(TacticalMissionStage.InProgress));

            TacticalMissionState quiet = NewState();
            quiet.PlaceGuardForTest(0, new TacticalGridPoint(4, 5),
                TacticalDirection.East, TacticalGuardState.Patrol);
            Assert.That(quiet.TrySprint(First, TacticalDirection.North).Succeeded, Is.True);
            Assert.That(quiet.GuardStateAt(0), Is.EqualTo(TacticalGuardState.Patrol),
                "noise farther than three cells must not raise the alert.");
        }

        [Test]
        public void SightCone_IsThreeWideFourDeepAndWallsBlockTheColumn()
        {
            // In the cone, out of the cone, beyond depth.
            Assert.That(TacticalMissionRules.CanSee(new TacticalGridPoint(2, 0),
                TacticalDirection.North, new TacticalGridPoint(2, 4), false, false), Is.True);
            Assert.That(TacticalMissionRules.CanSee(new TacticalGridPoint(2, 0),
                TacticalDirection.North, new TacticalGridPoint(2, 5), false, false), Is.False);
            Assert.That(TacticalMissionRules.CanSee(new TacticalGridPoint(2, 0),
                TacticalDirection.North, new TacticalGridPoint(4, 2), false, false), Is.False);
            Assert.That(TacticalMissionRules.CanSee(new TacticalGridPoint(2, 0),
                TacticalDirection.North, new TacticalGridPoint(2, -1), false, false), Is.False);

            // The authored wall (3,1) blocks the whole sight column beyond it.
            Assert.That(TacticalMissionRules.CanSee(new TacticalGridPoint(3, 0),
                TacticalDirection.North, new TacticalGridPoint(3, 2), false, false), Is.False);
            Assert.That(TacticalMissionRules.CanSee(new TacticalGridPoint(3, 0),
                TacticalDirection.North, new TacticalGridPoint(3, 1), false, false), Is.True,
                "the wall cell itself is visible before the wall blocks the line.");
        }

        [Test]
        public void Cover_HidesUnlessAdjacentAndSprintExposureOverridesCover()
        {
            var guard = new TacticalGridPoint(1, 0);
            var cover = new TacticalGridPoint(2, 2);
            Assert.That(TacticalMissionRules.CanSee(guard, TacticalDirection.North, cover,
                true, false), Is.False, "cover hides a non-adjacent operative.");
            Assert.That(TacticalMissionRules.CanSee(guard, TacticalDirection.North, cover,
                true, true), Is.True, "a sprinting operative is exposed even on cover.");

            Assert.That(TacticalMissionRules.CanSee(new TacticalGridPoint(2, 3),
                TacticalDirection.South, cover, true, false), Is.True,
                "an adjacent guard sees into a cover cell.");
        }

        [Test]
        public void SprintedOperative_OnCover_IsSpottedForThatResponse()
        {
            TacticalMissionState state = NewState();
            state.PlaceOperativeForTest(0, new TacticalGridPoint(2, 0));
            state.PlaceGuardForTest(1, new TacticalGridPoint(1, 0),
                TacticalDirection.North, TacticalGuardState.Patrol);

            Assert.That(state.TrySprint(First, TacticalDirection.North).Succeeded, Is.True);

            Assert.That(state.OperativePosition(0),
                Is.EqualTo(new TacticalGridPoint(2, 2)));
            Assert.That(state.GuardStateAt(1), Is.EqualTo(TacticalGuardState.Alert),
                "sprint exposure must override the cover hiding rule.");
            Assert.That(state.LastEvent, Does.Contain("警戒"));
        }

        [Test]
        public void StationaryOperative_OnCover_StaysHiddenFromTheSameGuard()
        {
            TacticalMissionState state = NewState();
            state.PlaceOperativeForTest(0, new TacticalGridPoint(5, 4));

            Assert.That(state.TryWait(First).Succeeded, Is.True);

            Assert.That(state.GuardStateAt(1), Is.EqualTo(TacticalGuardState.Patrol));
            Assert.That(state.GuardPositionAt(1),
                Is.EqualTo(TacticalMissionScenario.StationaryGuardCell));
            Assert.That(state.Stage, Is.EqualTo(TacticalMissionStage.InProgress));
        }

        [Test]
        public void GuardThatSeesAnOperativeRaisesAlertPursuesAndCaptures()
        {
            TacticalMissionState state = NewState();
            state.PlaceOperativeForTest(0,
                TacticalMissionScenario.DispatchCell);

            TacticalActionResult wait = state.TryWait(First);

            Assert.That(wait.Succeeded, Is.False);
            Assert.That(state.Stage, Is.EqualTo(TacticalMissionStage.Failed));
            Assert.That(state.Outcome, Is.EqualTo(TacticalMissionOutcome.Failed));
            Assert.That(state.GuardStateAt(1), Is.EqualTo(TacticalGuardState.Alert));
            Assert.That(state.Feedback, Does.Contain("抓住"));
            Assert.That(state.Turn, Is.EqualTo(1));
        }

        [Test]
        public void InvestigatingGuard_ReturnsToPatrolAfterTwoResponseTurns()
        {
            TacticalMissionState state = NewState();
            state.PlaceGuardForTest(0, new TacticalGridPoint(4, 5),
                TacticalDirection.East, TacticalGuardState.Investigating);

            Assert.That(state.TryWait(First).Succeeded, Is.True);
            Assert.That(state.GuardStateAt(0), Is.EqualTo(TacticalGuardState.Investigating));

            Assert.That(state.TryWait(Second).Succeeded, Is.True);
            Assert.That(state.GuardStateAt(0), Is.EqualTo(TacticalGuardState.Patrol));
            Assert.That(state.LastEvent, Does.Contain("返回巡逻"));
            Assert.That(state.Stage, Is.EqualTo(TacticalMissionStage.InProgress));
        }

        [Test]
        public void EqualLengthPaths_ResolveInFixedNorthEastSouthWestOrder()
        {
            Assert.That(TacticalMissionRules.TryFindNextStep(new TacticalGridPoint(0, 0),
                new TacticalGridPoint(1, 1), null, out TacticalGridPoint step), Is.True);
            Assert.That(step, Is.EqualTo(new TacticalGridPoint(0, 1)),
                "north is tried before east when both first steps are equally short.");

            Assert.That(TacticalMissionRules.TryFindNextStep(new TacticalGridPoint(2, 0),
                new TacticalGridPoint(2, 2), point => point == new TacticalGridPoint(2, 1),
                out TacticalGridPoint detour), Is.True);
            Assert.That(detour, Is.EqualTo(new TacticalGridPoint(1, 0)),
                "the east cell leads into the authored wall, so the detour starts west.");
        }

        [Test]
        public void Interact_RequiresOrthogonalAdjacencyAndCarriesTheDispatch()
        {
            TacticalMissionState state = NewState();
            Assert.That(state.TryInteract(First).Succeeded, Is.False);

            state.PlaceOperativeForTest(0, new TacticalGridPoint(5, 4));
            TacticalActionResult pickup = state.TryInteract(First);

            Assert.That(pickup.Succeeded, Is.True, pickup.Feedback);
            Assert.That(state.DispatchTaken, Is.True);
            Assert.That(state.OperativeCarryingDispatch(0), Is.True);
            Assert.That(state.Turn, Is.EqualTo(1));

            Assert.That(state.TryInteract(Second).Succeeded, Is.False,
                "the dispatch can be taken only once.");
            Assert.That(state.Turn, Is.EqualTo(1));
        }

        [Test]
        public void Extract_RequiresTheCarrierAndBothOperativesOnTheExtractionCells()
        {
            TacticalMissionState state = NewState();
            Assert.That(state.TryExtract().Succeeded, Is.False,
                "no dispatch means no extraction.");

            state.PlaceOperativeForTest(0, new TacticalGridPoint(5, 4));
            Assert.That(state.TryInteract(First).Succeeded, Is.True);
            Assert.That(state.TryExtract().Succeeded, Is.False,
                "both operatives must stand on the extraction cells.");

            state.PlaceOperativeForTest(0, new TacticalGridPoint(0, 0));
            state.PlaceOperativeForTest(1, new TacticalGridPoint(1, 0));
            TacticalActionResult extract = state.TryExtract();

            Assert.That(extract.Succeeded, Is.True, extract.Feedback);
            Assert.That(state.Stage, Is.EqualTo(TacticalMissionStage.Succeeded));
            Assert.That(state.Outcome, Is.EqualTo(TacticalMissionOutcome.Succeeded));
            Assert.That(state.Turn, Is.EqualTo(2));
        }

        [Test]
        public void Withdraw_IsAlwaysAvailableAndEndsTheOperationOnce()
        {
            TacticalMissionState state = NewState();
            TacticalActionResult withdraw = state.TryWithdraw();

            Assert.That(withdraw.Succeeded, Is.True);
            Assert.That(state.Stage, Is.EqualTo(TacticalMissionStage.Withdrawn));
            Assert.That(state.Outcome, Is.EqualTo(TacticalMissionOutcome.Withdrawn));

            int turn = state.Turn;
            TacticalGridPoint guard = state.GuardPositionAt(0);
            Assert.That(state.TryWait(First).Succeeded, Is.False);
            Assert.That(state.TryExtract().Succeeded, Is.False);
            Assert.That(state.TryWithdraw().Succeeded, Is.False);
            Assert.That(state.Turn, Is.EqualTo(turn));
            Assert.That(state.GuardPositionAt(0), Is.EqualTo(guard));
        }

        [Test]
        public void InvalidCommand_LeavesGuardsAndAlertStateUntouched()
        {
            TacticalMissionState state = NewState();
            Assert.That(state.TryMove(First, TacticalDirection.North).Succeeded, Is.True);
            TacticalGridPoint guard = state.GuardPositionAt(0);
            TacticalGuardState guardState = state.GuardStateAt(0);
            int turn = state.Turn;

            state.PlaceOperativeForTest(1, new TacticalGridPoint(7, 5));
            Assert.That(state.TryMove(Second, TacticalDirection.North).Succeeded, Is.False);

            Assert.That(state.Turn, Is.EqualTo(turn));
            Assert.That(state.GuardPositionAt(0), Is.EqualTo(guard));
            Assert.That(state.GuardStateAt(0), Is.EqualTo(guardState));
        }

        [Test]
        public void Snapshot_RendersTheAuthoredGridWithoutMutatingTheOperation()
        {
            TacticalMissionState state = NewState();
            TacticalMissionSnapshot snapshot = state.CreateSnapshot();

            Assert.That(snapshot.Tiles.Count, Is.EqualTo(48));
            Assert.That(snapshot.TileAt(3, 1).Kind, Is.EqualTo(TacticalTileKind.Wall));
            Assert.That(snapshot.TileAt(2, 2).Kind, Is.EqualTo(TacticalTileKind.Cover));
            Assert.That(snapshot.TileAt(0, 0).Kind, Is.EqualTo(TacticalTileKind.Extraction));
            Assert.That(snapshot.TileAt(6, 4).InGuardSight, Is.True,
                "the stationary guard faces the dispatch.");
            Assert.That(snapshot.Operatives.Count, Is.EqualTo(2));
            Assert.That(snapshot.Guards.Count, Is.EqualTo(2));

            Assert.That(state.Turn, Is.Zero);
            Assert.That(state.OperativePosition(0),
                Is.EqualTo(new TacticalGridPoint(0, 0)));
        }
    }
}
