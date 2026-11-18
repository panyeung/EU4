# Unity 2D Project — Hermes Instructions

## Project

- Engine: Unity 6
- Render Pipeline: Universal 2D (URP)
- Language: C#
- Platform: Windows
- Project Path: `C:\Users\panye\My project`
- Game Direction: 《影之一族》按 `checkpoint.md` 的设计规划与里程碑执行。UI 与美术资产按 `ui设计规划.md`。旧 Sengoku Chronicle M1–M7 文档仅作历史档案。

## Team

Every role below is a Hermes agent. Orca and OpenCode are retired. No task, command, or instruction may depend on them.

| Role | Agent | How to dispatch | Owns |
|---|---|---|---|
| Lead | `default` profile (this session) | runs in-thread, dispatches the rest | planning, acceptance criteria, unit tests, technical sign-off, user-facing status |
| Implementer | `coden` profile | `delegate_task(option="code")` or a kanban card assigned to `coden` | gameplay and integration code, Unity test execution, scene updates through existing updaters |
| Verifier | `tester` profile | kanban card assigned to `tester`, or `delegate_task(option="review")` | independent review of code and evidence, `kanban_request_changes` verdicts |
| Researcher | `scout` profile | `delegate_task(option="fast")` or a kanban card | Unity API and package research, GitHub reuse checks, licence and compatibility checks |
| Analyst | `analyst` profile | `delegate_task(option="deep")` | multi-source investigation and synthesis, architecture questions, long-form findings |

Two dispatch shapes, and the choice matters.

- `delegate_task` runs a bounded task inside the current session and returns a summary. Use it for work that ends in minutes and needs no durable card.
- A kanban card runs a named profile in its own workspace and survives the session. Use it when the work must be reviewable, resumable, or handed to a different profile. `hermes kanban assignees` lists what is dispatchable.

Do not spawn a full team for a small task. The lead handles trivial work directly and delegates the rest.

## Project Safety

- Preserve Unity `.meta` files and GUIDs.
- Do not modify `Library` or `Temp`.
- Do not delete scenes or prefabs without approval.
- Do not install packages without approval.
- Check `git status` before changes.
- Never overwrite uncommitted user changes.
- Verify code changes before claiming success.
- Keep changes within this project unless approved.

## Verification

- Before Unity work, check the running Editor process and its `-projectPath`. Keep one Editor per project folder; never run batchmode against a folder held by the user's Editor.
- Use that Editor's Test Runner and `Editor.log` for compilation and tests. The standard log is `%LOCALAPPDATA%\Unity\Editor\Editor.log`; also check `<project>\Logs\Editor.log`.
- The host has no .NET SDK, so `dotnet` is not the build path. The Unity Pipeline bridge (`com.unity.pipeline` plus the `unity` CLI, Editor server on 7800–7849) is. Use `unity command editor_status`, `console`, `console_status`, `recompile`, `run_script`, `open_scene`, `editor_play`/`editor_stop`, `editor_focus`, and `capture_game_view` against the already-open Editor instead of GUI automation.
- Record compilation, EditMode, Validator, Console counts, Play Mode, save/load, and user acceptance as separate evidence. Do not infer runtime behaviour from compilation or from a test run. Never claim runtime behaviour without runtime evidence.
- Project verify recipes live in `.opencode/skills/` (`verify`, `succession-invariants`), and the 2026-11-10 Unity Pipeline entry in `checkpoint.md` 决策记录 records the working commands and their limits.
- Do not run competing test suites across worktrees. They share `TestResults.xml` and overwrite each other's evidence.
- End each milestone with a playable demo: successful compilation, zero Console errors, the intended scene, a playable headline loop, and save/load when it is in scope.

## Token efficiency

- Minimize redundant context.
- Reuse existing research.
- Delegate narrowly scoped tasks.
- Return concise results.
- Avoid repeatedly reading unchanged files.

## GitHub-first policy

Before implementing new functionality.

1. Inspect existing project code for a reusable solution.
2. Have `scout` search GitHub for relevant repositories, libraries, or existing implementations.
3. Prefer Unity 6 compatible options.
4. Evaluate licence and commercial-use terms, Unity and URP compatibility, maintenance activity, dependency and security risk, and implementation cost.
5. Compare reuse against custom development.
6. Prefer proven, lightweight solutions when they fit.
7. Do not install packages, clone repositories, or copy external code without approval.
8. When nothing suitable exists, implement a focused custom solution.

Scout report format. Repository name and URL, licence, compatibility, reason, estimated integration effort, decision `Reuse / Adapt / Build`. Limit the first search to three promising repositories and read READMEs before cloning. Skip external research for trivial fixes and existing-code maintenance.

## Git and delivery

- Work on feature or integration branches. Merge to `main` and tag a milestone only after the user accepts its demo.
- Never force-push, rewrite history, or delete a branch containing unmerged work.
- After each verification gate passes, commit only the files verified for that task, keep unrelated local changes out of the commit, and push the commit to `origin` on the current branch.
- After every checkpoint commit, milestone merge, or tag, push it and report the resulting remote refs.
- Only the lead or one designated integration agent edits scenes, prefabs, or `ProjectSettings`. Parallel agents in separate worktrees may edit C# logic, text data, tests, docs, and research.
- Keep gameplay rules in plain C# where practical, use asmdefs, and preserve `PlayerMovement2D` behaviour.
- Editor scene generators use one Undo group, validate before saving, roll back with Undo on exceptions, and never reload a scene for rollback. Place generated UI beneath one fixed root.

## Kanban review loop

- `coden` implements on the existing card and, when ready, calls `kanban_request_review(reviewer="tester")` with changed files, tests, and verification evidence.
- `tester` reviews that same card. For correctable defects it records actionable findings and calls `kanban_request_changes`, which returns the card to `coden` for correction and same-card re-review. It calls `kanban_complete` only after review passes with verified results.
- Do not create a separate QA card for this loop, and never mark a feature ready from implementation completion alone.
- Count review attempts on the same card. After ten unsuccessful reviews, stop the automatic cycle and block the card for the user with the remaining defects listed. This cap is agent-enforced; do not claim the Kanban system imposes it unless verified.
- For each substantial feature the lead creates one dependent final sign-off card linked to the implementation card. It becomes eligible only when the reviewed card is done after review approval. The lead inspects the verdict and evidence, reports changed files, unresolved issues, and actual test results, and keeps compilation separate from Play Mode verification.

## User collaboration boundaries

- The lead owns planning, acceptance criteria, and writing or updating unit tests. `coden` implements gameplay and integration code and executes tests, including EditMode, Play Mode, scene validation, and applicable regression checks.
- `coden` returns exact commands, result files, test totals, failures, Console counts, and runtime evidence. The lead reviews that evidence and owns technical sign-off. The lead does not execute tests unless the user explicitly changes this assignment.
- This testing assignment is the user's standing instruction and supersedes any earlier clause assigning test execution or Play Mode operation to another agent. `tester` may review tests and evidence without executing them.
- Computer Use and screenshots are authorised for Play Mode validation, including while the user is awake. Before each Unity session, inspect the running Editor process and verify its `-projectPath`; operate only the existing Editor for this project.
- Delete every Computer Use screenshot immediately after visual inspection. Do not retain screenshots in the project or backups, and clear the Computer Use temporary cache at the end of the task.
- The user retains final milestone acceptance. Never create the final milestone checkpoint, merge to `main`, or tag without it.

## Cost and dependencies

- Stay in this repository. Use no paid services, subscriptions, assets, or APIs.
- Do not change Hermes provider settings, API keys, or model routing.
- Do not copy material from Taiko Risshiden or another commercial game. Use original, CC0, or permissively licensed content, and list every source and licence in `ui设计规划.md` 第 10 节. Record new Unity packages and their reason in `checkpoint.md` 决策记录.
- Report genuine blockers to the user rather than spawning endless follow-up cards.

## Task completion notification

Every finished task ends with a notification to `openManusBot` on the UnityDH channel, and the message must name the kanban card (id and title) when the work came from one. Run `scripts/notify-openmanus.sh "<title>" "<body>" "<card id>"`. The script appends the notice to `Logs/notifications/outbox.txt`, then sends through `hermes send --to "$OPENMANUS_TARGET"` when that variable is set. `hermes send --list` shows whether any platform is configured. When no target exists, say so plainly instead of claiming the notice went out, and run `scripts/notify-openmanus.sh --drain` once a target is set.

## Art asset workflow

Follow `ui设计规划.md` 第 10 节. The lead generates original reference-matched art and records it in `ui设计规划.md`; `coden` implements and integrates it. The lead may take implementation over when `coden` is unavailable or cannot finish it.
