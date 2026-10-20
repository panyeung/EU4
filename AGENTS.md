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

## Unity implementation and QA workflow

For Unity coding tasks, UnityCoder implements the requested changes on the existing task and, when ready, calls `kanban_request_review(reviewer="qatester")` with changed files, tests, and verification evidence. QATester reviews that same task. For correctable defects, QATester records actionable findings and calls `kanban_request_changes`; Kanban returns the task to UnityCoder for correction and same-task re-review. QATester calls `kanban_complete` only after review passes with verified results. Do not create a separate QA card for this same-task review flow, and never mark a feature ready based on implementation completion alone.

Count review attempts on the same task. After ten unsuccessful QA reviews, do not request another automatic correction cycle: block the task for human attention and list remaining defects. This is agent-enforced; do not claim the Kanban system itself imposes this ten-review cap unless verified.

For each substantial feature, Mochi creates one dependent final sign-off task linked to the implementation task. It becomes eligible only when the reviewed implementation task is done after QA approval. Mochi inspects the review verdict and evidence, reports changed files, unresolved issues, and actual test results, and distinguishes compilation from Play Mode verification. Never claim runtime behavior without runtime evidence. Report genuine blockers to the user rather than spawning endless follow-up tasks. Send the final sign-off outcome to the user via WhatsApp when a configured messaging route and recipient are available; if not, state that clearly and use the Kanban handoff instead.



