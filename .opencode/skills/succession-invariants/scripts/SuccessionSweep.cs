using System;
using System.Collections.Generic;
using System.Text;
using SengokuChronicle.Domain;

/// <summary>
/// Exhaustive decision sweep over the real succession code (SuccessionService + SuccessionState).
/// Run it through the Unity Pipeline bridge:
///   unity command run_script --file .opencode/skills/succession-invariants/scripts/SuccessionSweep.cs --entry SuccessionSweep.Run
/// There is no model here: every step calls the shipped SuccessionService on a real
/// SuccessionSample roster, so a pass covers the code the game runs.
/// </summary>
public static class SuccessionSweep
{
    private static readonly GameDate Start = new GameDate(1560, 4, 1);

    private enum Act
    {
        Begin,
        PickFirstCandidate,
        Confirm,
        FinishRegency,
    }

    private static readonly Act[] Alphabet =
    {
        Act.Begin, Act.PickFirstCandidate, Act.Confirm, Act.FinishRegency,
    };

    private sealed class Observation
    {
        public Act Act;
        public bool Succeeded;
        public string HeadId;
        public int SuccessionCount;
        public string FamilyId;
        public int PersonCount;
        public SuccessionPhase Phase;
    }

    private sealed class Violation
    {
        public string Invariant;
        public string Detail;
        public string Sequence;
    }

    private static int _steps;

    public static string Run()
    {
        var report = new StringBuilder();
        var violations = new List<Violation>();
        int sequences = 0;
        _steps = 0;

        foreach (string scenario in new[] { "standard", "crisis" })
        {
            foreach (int length in new[] { 1, 2, 3, 4 })
            {
                foreach (Act[] sequence in Enumerate(length))
                {
                    sequences++;
                    List<Observation> trace = Play(scenario, sequence);
                    violations.AddRange(Check(scenario, sequence, trace));
                    if (violations.Count > 200)
                        break;
                }
            }
        }

        report.AppendLine($"scenarios=2 sequences={sequences} steps={_steps}");
        report.AppendLine($"violations={violations.Count}");
        foreach (Violation v in violations)
        {
            if (violations.IndexOf(v) >= 5)
                break;
            report.AppendLine($"  [{v.Invariant}] {v.Detail}");
            report.AppendLine($"    sequence: {v.Sequence}");
        }

        report.AppendLine(SelfTest());
        return report.ToString();
    }

    private static List<Observation> Play(string scenario, Act[] sequence)
    {
        SuccessionState state = scenario == "standard"
            ? SuccessionSample.CreateStandard()
            : SuccessionSample.CreateCrisis();

        var trace = new List<Observation>();
        foreach (Act act in sequence)
        {
            bool succeeded = Apply(state, act);
            _steps++;
            trace.Add(new Observation
            {
                Act = act,
                Succeeded = succeeded,
                HeadId = state.HeadPersonId.Value,
                SuccessionCount = state.SuccessionCount,
                FamilyId = state.World.Family.Id.Value,
                PersonCount = state.World.PersonCount,
                Phase = state.Phase,
            });
        }

        return trace;
    }

    private static bool Apply(SuccessionState state, Act act)
    {
        switch (act)
        {
            case Act.Begin:
                return SuccessionService.BeginRetirement(state, Start).Succeeded;
            case Act.PickFirstCandidate:
            {
                System.Collections.Generic.IReadOnlyList<SuccessionCandidate> candidates =
                    SuccessionService.GetCandidates(state, Start);
                return candidates.Count > 0 &&
                       SuccessionService.SelectSuccessor(state, candidates[0].PersonId, Start).Succeeded;
            }
            case Act.Confirm:
                return SuccessionService.ConfirmSuccession(state, Start).Succeeded;
            case Act.FinishRegency:
                return SuccessionService.TryCompleteRegency(state, Start).Succeeded;
            default:
                return false;
        }
    }

    /// <summary>The promises the succession loop makes to the player.</summary>
    private static IEnumerable<Violation> Check(string scenario, Act[] sequence, List<Observation> trace)
    {
        string path = scenario + ": " + string.Join(" -> ", Array.ConvertAll(sequence, a => a.ToString()));

        string family = trace[0].FamilyId;
        int people = trace[0].PersonCount;
        string head = trace[0].HeadId;
        int count = trace[0].SuccessionCount;
        bool began = false;
        bool confirmedSinceBegin = false;

        foreach (Observation step in trace)
        {
            if (step.FamilyId != family)
                yield return new Violation
                {
                    Invariant = "family-identity-stable",
                    Detail = $"family id changed {family} -> {step.FamilyId}",
                    Sequence = path,
                };

            if (step.PersonCount != people)
                yield return new Violation
                {
                    Invariant = "roster-identity-stable",
                    Detail = $"person count changed {people} -> {step.PersonCount}; succession must not create people",
                    Sequence = path,
                };

            if (step.HeadId != head)
            {
                if (!(step.Act == Act.Confirm && step.Succeeded))
                    yield return new Violation
                    {
                        Invariant = "control-transfers-only-on-confirm",
                        Detail = $"head changed {head} -> {step.HeadId} on {step.Act} (succeeded={step.Succeeded})",
                        Sequence = path,
                    };
                if (!began)
                    yield return new Violation
                    {
                        Invariant = "no-confirm-without-retirement",
                        Detail = $"head changed {head} -> {step.HeadId} without a successful retirement request",
                        Sequence = path,
                    };
                if (confirmedSinceBegin)
                    yield return new Violation
                    {
                        Invariant = "single-transfer-per-cycle",
                        Detail = $"second head change {head} -> {step.HeadId} inside one retirement cycle",
                        Sequence = path,
                    };

                if (step.SuccessionCount != count + 1)
                    yield return new Violation
                    {
                        Invariant = "every-transfer-is-counted",
                        Detail = $"head changed {head} -> {step.HeadId} but count went {count} -> {step.SuccessionCount}",
                        Sequence = path,
                    };

                head = step.HeadId;
                confirmedSinceBegin = true;
            }

            if (step.SuccessionCount != count)
            {
                if (step.SuccessionCount != count + 1 || !(step.Act == Act.Confirm && step.Succeeded))
                    yield return new Violation
                    {
                        Invariant = "succession-count-matches-transfers",
                        Detail = $"count moved {count} -> {step.SuccessionCount} on {step.Act} (succeeded={step.Succeeded})",
                        Sequence = path,
                    };
                count = step.SuccessionCount;
            }

            if (step.Act == Act.Begin && step.Succeeded)
            {
                began = true;
                confirmedSinceBegin = false;
            }
        }
    }

    /// <summary>A checker that cannot fail proves nothing: feed it known-broken traces.</summary>
    private static string SelfTest()
    {
        var failures = new List<string>();
        var sequence = new[] { Act.Begin, Act.Confirm };

        var ok = new List<Observation>
        {
            new Observation { Act = Act.Begin, Succeeded = true, HeadId = "head", SuccessionCount = 0, FamilyId = "family", PersonCount = 7, Phase = SuccessionPhase.AwaitingSuccessionChoice },
            new Observation { Act = Act.Confirm, Succeeded = true, HeadId = "heir", SuccessionCount = 1, FamilyId = "family", PersonCount = 7, Phase = SuccessionPhase.None },
        };
        if (Count(ok, sequence) != 0)
            failures.Add("a correct trace was flagged");

        var doubleTransfer = new List<Observation>(ok)
        {
            new Observation { Act = Act.Confirm, Succeeded = true, HeadId = "third", SuccessionCount = 2, FamilyId = "family", PersonCount = 7, Phase = SuccessionPhase.None },
        };
        if (Count(doubleTransfer, sequence) == 0)
            failures.Add("a second transfer inside one cycle was not flagged");

        var rosterChurn = new List<Observation>
        {
            ok[0],
            new Observation { Act = Act.Confirm, Succeeded = true, HeadId = "heir", SuccessionCount = 1, FamilyId = "family", PersonCount = 8, Phase = SuccessionPhase.None },
        };
        if (Count(rosterChurn, sequence) == 0)
            failures.Add("a roster change was not flagged");

        var familyChurn = new List<Observation>
        {
            ok[0],
            new Observation { Act = Act.Confirm, Succeeded = true, HeadId = "heir", SuccessionCount = 1, FamilyId = "other", PersonCount = 7, Phase = SuccessionPhase.None },
        };
        if (Count(familyChurn, sequence) == 0)
            failures.Add("a family id change was not flagged");

        return failures.Count == 0
            ? "self-test=PASS (4/4 broken traces caught)"
            : "self-test=FAIL: " + string.Join("; ", failures);
    }

    private static int Count(List<Observation> trace, Act[] sequence)
    {
        int found = 0;
        foreach (Violation unused in Check("self-test", sequence, trace))
            found++;
        return found;
    }

    private static IEnumerable<Act[]> Enumerate(int length)
    {
        var current = new Act[length];
        foreach (Act[] sequence in Enumerate(current, 0))
            yield return sequence;
    }

    private static IEnumerable<Act[]> Enumerate(Act[] current, int index)
    {
        if (index == current.Length)
        {
            yield return (Act[])current.Clone();
            yield break;
        }

        foreach (Act act in Alphabet)
        {
            current[index] = act;
            foreach (Act[] sequence in Enumerate(current, index + 1))
                yield return sequence;
        }
    }
}
