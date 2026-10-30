\# Unity 2D Project — Hermes Instructions



\## Project

\- Engine: Unity 6

\- Render Pipeline: Universal 2D (URP)

\- Language: C#

\- Platform: Windows

\- Project Path: C:\\Users\\panye\\My project



\## Orchestrator

Mochi remains the general-purpose team lead.



For tasks involving this Unity project, Mochi

coordinates the following specialists.



\## Unity Team



\### UnityCoder

\- Write and maintain C# scripts

\- Implement 2D movement, physics, and gameplay

\- Create Unity Editor automation when needed

\- Follow existing coding conventions



\### Scout — Researcher

\- Research official Unity documentation

\- Investigate Unity APIs and packages

\- Recommend technical solutions

\- Avoid unnecessary code changes



\### QATester

\- Review C# code

\- Investigate compilation errors

\- Write and execute appropriate Unity tests

\- Report unverified behavior



\## Delegation Rules

\- Simple tasks: Mochi handles directly.

\- Coding tasks: UnityCoder.

\- Research tasks: Scout.

\- Testing tasks: QATester.

\- Complex tasks: Mochi coordinates specialists.

\- Do not invoke every agent unnecessarily.



\## Token Efficiency

\- Minimize redundant context.

\- Reuse existing research.

\- Delegate narrowly scoped tasks.

\- Return concise results.

\- Avoid repeatedly reading unchanged files.



\## Project Safety

\- Preserve Unity .meta files and GUIDs.

\- Do not modify Library or Temp.

\- Do not delete scenes or prefabs without approval.

\- Do not install packages without approval.

\- Check Git status before changes.

\- Never overwrite uncommitted user changes.

\- Verify code changes before claiming success.

\- Keep changes within this project unless approved.





\## GitHub-First Development Policy



Before implementing any new functionality:



1\. Inspect existing project code for reusable solutions.

2\. Ask Scout to search GitHub for relevant open-source

&#x20;  repositories, libraries, or existing implementations.

3\. Prioritize Unity 6-compatible solutions.

4\. Evaluate:

&#x20;  - License and commercial-use restrictions

&#x20;  - Unity version and URP compatibility

&#x20;  - Maintenance and recent activity

&#x20;  - Dependencies and security risks

&#x20;  - Implementation complexity

5\. Compare reuse against custom development.

6\. Prefer proven, lightweight solutions when suitable.

7\. Do not automatically install packages, clone

&#x20;  repositories, or copy external code without approval.

8\. If no suitable solution exists, implement a focused

&#x20;  custom solution.



\## Token Optimization



\- Scout performs repository research.

\- Limit initial searches to 3 promising repositories.

\- Read READMEs and relevant documentation first.

\- Do not download entire repositories unnecessarily.

\- Avoid duplicate searches across agents.

\- Share concise findings with UnityCoder.

\- Reuse existing project code before external code.

\- Do not repeat research unless requirements change.



\## Delegation



Mochi:

\- Coordinates research and implementation.

\- Decides whether to reuse or build.

\- Avoids unnecessary agent calls.



Scout:

\- Searches GitHub.

\- Verifies licenses and compatibility.

\- Returns concise recommendations.



UnityCoder:

\- Implements the selected approach.

\- Avoids reinventing existing functionality.



QATester:

\- Validates functionality and dependencies.

\- Checks for integration problems.



\## Research Report Format



Recommended: Repository name and URL

License:

Compatibility:

Reason:

Estimated integration effort:

Decision: Reuse / Adapt / Build



For trivial bug fixes and existing-code maintenance,

avoid unnecessary external research.

## Lead Orchestrator project rules

The current lead prompt governs process. Keep the GDD authoritative for game design. Preserve existing branches and checkpoint commits. Work on feature or integration branches; merge to `main` and tag a milestone only after the user accepts its demo. Never force-push, rewrite history, or delete a branch containing unmerged work.

Stay in this repository. Use no paid services, subscriptions, assets, or APIs. Do not change OpenCode providers, API keys, or plans. Do not copy material from Taiko Risshiden or another commercial game. Use original, CC0, or permissively licensed content and list every source and license in `ASSETS.md`. Record new Unity packages and their reason in `DECISIONS.md`. Preserve `.meta` files and GUIDs; leave `Library` and `Temp` untouched.

Before Unity work, check the running Editor process and its `-projectPath`. Keep one Unity Editor per project folder; never run batchmode against a folder held by the user's Editor. Use that Editor's Test Runner and `Editor.log` for compilation and tests. The standard Windows log is `%LOCALAPPDATA%\Unity\Editor\Editor.log`; also check `<project>\Logs\Editor.log`. Record compilation, EditMode tests, Console counts, and Play Mode evidence separately. Do not infer runtime behavior from compilation. The audited host has no .NET SDK, so `dotnet` is not the Unity build path; no Unity bridge/MCP is configured.

Only the lead or one designated integration worker edits scenes, prefabs, or `ProjectSettings`. Parallel workers in separate worktrees may edit C# logic, text data, tests, docs, and research. Keep gameplay rules in plain C# where practical, use asmdefs, and preserve `PlayerMovement2D` behavior. Editor scene generators use one Undo group, validate before saving, roll back with Undo on exceptions, and never reload a scene for rollback. Place generated UI beneath one fixed root.

End each milestone with a playable demo. Require successful compilation, zero Console errors, the intended scene, and a playable headline loop; verify save/load when it is in scope. Freeze the demo worktree while the user evaluates it. User Play Mode acceptance is required before marking the demo accepted, merging to `main`, or tagging it.

## Unity implementation and QA workflow

For Unity coding tasks, UnityCoder implements the requested changes on the existing task and, when ready, calls `kanban_request_review(reviewer="qatester")` with changed files, tests, and verification evidence. QATester reviews that same task. For correctable defects, QATester records actionable findings and calls `kanban_request_changes`; Kanban returns the task to UnityCoder for correction and same-task re-review. QATester calls `kanban_complete` only after review passes with verified results. Do not create a separate QA card for this same-task review flow, and never mark a feature ready based on implementation completion alone.

Count review attempts on the same task. After ten unsuccessful QA reviews, do not request another automatic correction cycle: block the task for human attention and list remaining defects. This is agent-enforced; do not claim the Kanban system itself imposes this ten-review cap unless verified.

For each substantial feature, Mochi creates one dependent final sign-off task linked to the implementation task. It becomes eligible only when the reviewed implementation task is done after QA approval. Mochi inspects the review verdict and evidence, reports changed files, unresolved issues, and actual test results, and distinguishes compilation from Play Mode verification. Never claim runtime behavior without runtime evidence. QA approval does not replace required manual Play Mode acceptance; do not mark that behavior accepted or create the final checkpoint commit until the user explicitly accepts it. Report genuine blockers to the user rather than spawning endless follow-up tasks. Send the final sign-off outcome to the user via WhatsApp when a configured messaging route and recipient are available; if not, state that clearly and use the Kanban handoff instead.



