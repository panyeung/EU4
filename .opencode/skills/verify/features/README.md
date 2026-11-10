# 《影之一族》 verification map

This directory is the maintained source for verifying the user-facing behavior of
Sengoku Chronicle in `Assets/Scenes/FoundationScene.unity`. Read the index, then use the
matching feature file as the recipe. The harness is the Unity Pipeline bridge driving the
user's already-open Editor; see `../SKILL.md` for launch, doctor, evidence, and cleanup.

## Baseline preconditions

- Exactly one Editor is open for `C:\Users\panye\My project`, and `unity pipeline list`
  shows it with `服务器可达 = true`. Never start a second Editor and never use `-batchmode`.
- `unity command console_status` reports `groundTruth.compilationFailed=false`. With a
  compile error the Editor refuses Play Mode, so nothing else in this map is reachable.
- The active scene is `Assets/Scenes/FoundationScene.unity` (`VerifyUi.State` reports the path).
- Enter Play Mode, then reach a running campaign before anything else:
  `开始新游戏` → `固定三代剧本（SC-013）`. The title overlay and the scenario selector are not a
  campaign, so a run that stops there proves nothing. A run that inherits a previous run's
  campaign proves nothing either.
- Use one timestamped run id per session: `<stamp>` = `yyyyMMdd-HHmmss`.
- Scratch PNGs go under `Assets/Logs/verify/_scratch/` and are deleted with `Assets/Logs` at
  cleanup. The durable record is `Logs/verify/<stamp>-<feature>.md`.

## Driving conventions

- Start every recipe from the baseline state unless its preconditions say otherwise.
- Identify controls by their visible label in `buttons` output, and act on the ASCII hierarchy
  path, never on the label: the label does not survive the console code page.
- Run UI actions through the wrapper:
  `python .opencode/skills/verify/scripts/unity-run.py click <Canvas/.../Button>`.
  It builds the JSON argument in-process, which a bare `--args '["..."]'` cannot survive in
  PowerShell 5.1.
- `unity-run.py texts` lists every visible string with its path; `unity-run.py probe <path>`
  returns exactly one value, which is what an evidence file should quote.
- `unity command editor_focus` before every capture: Play Mode stalls while the Editor is
  unfocused and then `capture_game_view` returns a stale frame.
- Record console output with a cursor: take `cursor` and `session` from the first `console`
  call and pass them back as `--since` and `--since_session`.

## Proof and skip reporting

- Proof is the player action plus the observable state after it, not the final screen alone.
- A HUD label the player reads (head name, date, status text) is the proof target; a private
  field is not.
- Report an unreachable path with the attempted command and the unmet precondition; do not
  claim it through a different entry point.
- Keep compilation, EditMode test results, and Play Mode behavior as three separate claims.

## Feature entry contract

Each feature file starts with an H1 title and one paragraph of user-visible behavior, then
exactly four H2 sections in this order: `Sub-features`, `How to get to it (user POV)`,
`Driving it with VerifyUi`, `Gotchas`.

## Features

- [Title screen and new game](./title-new-game.md) covers the first overlay, starting a
  campaign, and continuing an existing one.
- [Month turn and daily clock](./month-turn.md) covers advancing a day, ending a month, and
  the pauses that block time.
- [World map travel](./world-map-travel.md) covers destination preview, departure, and cancel.
- [Succession](./succession.md) covers retiring the head, the candidate list, the handoff,
  and the no-heir crisis path.
- [Save and load](./save-load.md) covers saving, loading, and state fidelity across a load.
