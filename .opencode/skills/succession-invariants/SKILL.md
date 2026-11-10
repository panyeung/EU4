---
name: succession-invariants
description: Check the succession loop's promises against the real SuccessionService code — every decision sequence up to four steps, both sample rosters. Use after touching SuccessionService, SuccessionState, SuccessionSample, or the succession wrappers in GameSession, and before claiming a succession change is safe.
---

# Succession invariants

**Property.** Handing the headship to a successor never invents people, never renames the
family, transfers control only on a confirmed retirement, counts each transfer exactly once,
and never transfers twice inside one retirement cycle.

**Harm if it breaks.** A player's family identity and head-of-house continuity are the whole
campaign; a double transfer or an uncounted one corrupts chronicle records, regency state, and
every later succession, and nothing in the EditMode suite walks the sequences that expose it.

**Scope.** Exhaustive over every sequence of up to four succession actions, on both shipped
sample rosters (`SuccessionSample.CreateStandard()` and `CreateCrisis()`). It calls the shipped
`SuccessionService` directly — there is no model of the rules anywhere in this check.

## Run it

From `C:\Users\panye\My project`, with the Editor open on this project:

```powershell
unity command run_script --file .opencode/skills/succession-invariants/scripts/SuccessionSweep.cs --entry SuccessionSweep.Run
```

Expect, in roughly a second:

```
scenarios=2 sequences=680 steps=2504
violations=0
self-test=PASS (4/4 broken traces caught)
```

`violations=0` is a pass. `self-test=PASS` proves the checker still discriminates: it feeds four
known-broken traces (a correct one, a double transfer, a roster change, a family change) and
requires the bad ones to be flagged. A run with `self-test=FAIL` means the checker, not the
code, is broken — fix the script before reading `violations`.

If the bridge is unreachable, `unity pipeline list` first; if the Editor reports compile errors,
fix those first, because `run_script` compiles against the project assemblies.

## Invariants

| Name | Promise |
|---|---|
| `family-identity-stable` | `state.World.Family.Id` never changes during succession |
| `roster-identity-stable` | `state.World.PersonCount` never changes; succession creates no people |
| `control-transfers-only-on-confirm` | `HeadPersonId` changes only on a succeeded `ConfirmSuccession` |
| `no-confirm-without-retirement` | a head change is always preceded by a succeeded `BeginRetirement` |
| `single-transfer-per-cycle` | at most one head change between a `BeginRetirement` and the next one |
| `every-transfer-is-counted` | each head change moves `SuccessionCount` by exactly one |
| `succession-count-matches-transfers` | the count never moves except by that one confirm |

The action alphabet is `BeginRetirement`, `SelectSuccessor` (first listed candidate),
`ConfirmSuccession`, and `TryCompleteRegency`. Crisis adoption and explicit regent selection are
**not** in the alphabet; see "What this does not cover".

## Reading a failure

Each violation prints the invariant, the concrete ids, and the action sequence that produced it:

```
[every-transfer-is-counted] head changed person.akechi.harunobu -> person.akechi.masatsuna but count went 0 -> 0
  sequence: standard: Begin -> PickFirstCandidate -> Confirm
```

That sequence is a player path: retire, choose the first lawful candidate, confirm. Replay it
against the code to decide the cause:

- A **real defect** shows the wrong value in the code's own terms, like the example above: the
  head changed and the count did not.
- A **checker error** usually shows up as an invariant firing on a path the code considers
  legal, or `self-test=FAIL`. Update the script, not the invariants, unless the promise itself
  was wrong — then say so and change the promise deliberately.

Do not weaken an invariant to get a green run. Report the finding with its sequence.

## Coverage record

Verified against the code at commit `663a1b0` (`docs: record final M04B scene validation`).

Files this check exercises:

- `Assets/Scripts/Domain/SuccessionService.cs` — `BeginRetirement`, `SelectSuccessor`,
  `ConfirmSuccession`, `TryCompleteRegency`, `GetCandidates`, `IsEligibleSuccessor`
- `Assets/Scripts/Domain/SuccessionState.cs` — `HeadPersonId`, `SuccessionCount`, `Phase`,
  `World.Family.Id`, `World.PersonCount`
- `Assets/Scripts/Domain/SuccessionSample.cs` — the standard and crisis rosters

Move the commit forward only after you have re-read those functions and confirmed the sweep
still describes them.

## The check can fail

Proven on 2026-11-10: changing `state.SuccessionCount = previousCount + 1;` to
`state.SuccessionCount = previousCount;` in `ConfirmSuccession` produced 14 violations, the
shortest being `standard: Begin -> PickFirstCandidate -> Confirm`; restoring the line returned
the run to `violations=0`. A checker nobody has seen fail proves nothing.

## What this does not cover

- **Crisis adoption and extinction.** `AcceptCrisisAdoption` and `DeclineCrisis` are excluded
  deliberately: adoption is *supposed* to add a person, so `roster-identity-stable` does not
  apply to it.
- **Regency legality.** `TryCompleteRegency` is in the alphabet, but no invariant yet ties the
  minor's age to the handoff date; the sweep only checks it cannot move the count or the family.
- **The UI and the session layer.** `Canvas/SuccessionRoot` bindings and
  `GameSession.ApplySuccessionResult` side effects (daily-action interrupts, industry vacancies,
  chronicle records) are not exercised here; the `verify` skill covers the player-visible half.
- **Sequences longer than four actions.** Two retirements back to back are reachable in the game
  and are not enumerated. Extend `Enumerate` lengths only with the runtime cost in mind
  (five actions roughly triples the step count).
- **Other rosters.** Only the two sample rosters are swept, not a campaign that has drifted
  through years of play.
