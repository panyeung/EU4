# M08A family demography (annual deaths and NPC marriages)

At the December→January month boundary the family settles births first, then one annual
natural-death check per living family member, then one 15% autonomous-marriage check for
every eligible adult NPC. The listed age rates are the base rates; M08B adds a food-shortage
bonus. Deaths and marriages appear in the family chronicle, the family tree and the campaign
review without a new panel.

## Sub-features

- Natural death keeps the person, family membership, kinship and chronicle history; it
  clears industry posts and in-flight daily actions and cancels an unfinished pregnancy.
- A dead head enters the existing death-succession flow once; a pending succession
  decision skips autonomous marriage for that boundary.
- NPC marriage generates one stable spouse (opposite gender, adult, near the candidate's
  age), a symmetric `Spouse` kinship edge, a `Spouse` family role, a growth row and a
  daily-action row.
- `人物离世 · 享年 N 岁` and `自主婚姻` rows appear in the family chronicle; Campaign
  Review's People section lists them; the family tree shows the spouse and a `已故` label.
- A month that fails while writing these rows restores the date, family, pregnancy,
  industry, daily action, growth, RNG and chronicle state.

## How to get to it (user POV)

1. Enter Play Mode in `FoundationScene` and start `开始新游戏` → `固定三代剧本（SC-013）`.
2. Advance the calendar to the December→January boundary (the first one is
   `1561-01-01`, nine month turns from `1560-04-01`).
3. Open `家族编年史` and read the newest rows. Open `家族名册`/家谱 to see the spouse and
   any `已故` member.

## Driving it with VerifyUi

Lead Play Mode on 2026-11-13 used real month-end controls to reach 1561-01-01 and 1562-01-01; the family chronicle showed two natural deaths and two autonomous marriages. In-game Save/Load retained the 1562-01-01 date and lifecycle chronicle rows. This run did not check the generated spouse's serialized ID, gender or birth year, an unfinished pregnancy, or the RNG stream after loading. A hash comparison was reported after restoring the original save and backup, but the digest values were not retained. Full EditMode passed 1947/1947 and FoundationScene Validator passed with 0 errors. The Editor was recorded as ready/stopped with a clean scene; raw status output was not retained. The local evidence is `Logs/verify/20261113-m08a-family-demography.md`.

2026-11-14 follow-up used the existing Editor and a fresh SC-013 campaign, with real month-end UI and no save-slot access or injected state. By `1562-01-01`, the HUD population fell from 7 to 6. The family roster marked 内記 (49) and 明智 千恵 (68) `已故`; the Chronicle recorded both natural deaths and the autonomous marriage of 佐近 and 志乃. The head 明智 晴信 (72) remained in office and 明智 正綱 (48) remained designated heir, so head-death succession was not exercised. No birth was observed. The Editor was stopped with a clean `FoundationScene` and Console 0 errors / 0 warnings. No screenshot or save was made. Local evidence: `Logs/verify/20261114-m08a-natural-death-playtest.md`.

The first route verifies visible death markers and Chronicle projection. A later SC-013 route verifies head-death succession twice. Birth linkage, the full save/load contract and user milestone acceptance remain unverified in Play Mode.

2026-11-14 UI Save/Load follow-up: saved a fresh SC-013 session at `1562-01-01`, advanced to `1562-02-01`, then loaded through the map UI. The visible date returned to `1562-01-01`; population 6, deceased markers for 内記 (49) and 明智 千恵 (68), designated heir 明智 正綱 (48), and Chronicle rows for both deaths and the 佐近/志乃 autonomous marriage persisted. The UI showed `已保存当前进度。` and `已载入存档。` Both original save files were restored and their SHA-256 values matched the pre-test values exactly. This verifies those visible fields only; serialized spouse ID/gender/birth year, unfinished pregnancy, and RNG restoration remain unverified. No screenshot was captured. Evidence: `Logs/verify/20261114-m08a-natural-death-playtest.md`.

- Later on 2026-11-14, a separate SC-013 route focused on repeated succession used real month and succession UI through `1564-01-01`. At `1563-01-01`, head Harunobu (73) and Tokihime (47) were deceased; the player selected Masatsuna (49), and the HUD showed him as head with Nobumasa (25) as heir. At `1564-01-01`, Masatsuna (50) was deceased; the player selected Nobumasa (26), and the HUD and Chronicle showed the new head and succession. This verifies head-death succession twice in one Play Mode route. No birth was observed; Nobumasa had no fixed heir and the HUD showed the third-generation finale prompt.
- A separate SC-022 route started with Masamune (36), Chiyo (32), child Sayo (10), and collateral Yajiro (24). Roster snapshots through `1561-12-01` showed no pregnancy label. At `1562-01-01`, population fell from 4 to 2, the head and spouse were marked deceased, and succession paused the month. The player selected adult collateral Yajiro and confirmed. No birth was observed, and this route did not inspect the death Chronicle category.
- Both runs ended with `editor_stop`; the existing Editor was `ready/stopped`, `FoundationScene` was clean, and Console ground truth showed 0 errors / 0 warnings. The latest sample contained one log. Neither run saved the scene or captured a screenshot. The fixed seed was not captured. Birth behavior and the full save/load contract remain unverified. The user has not given final M08A acceptance. Evidence: `Logs/verify/20261114-m08a-natural-death-playtest.md`.

- Doctor: `unity pipeline list`, `unity command editor_status`, `unity command console_status`,
  `unity command list_open_scenes`. Require the single Editor for this project,
  `compilationFailed=false`, and `FoundationScene` active and clean.
- Start from the title overlay: `python .opencode/skills/verify/scripts/unity-run.py texts`
  to find the labels, then click the ASCII paths for `开始新游戏` and
  `固定三代剧本（SC-013）`.
- Advance month turns through a later December-to-January boundary with the real `结束本月` control
  (`Canvas/M02DailyActionRoot/...`); resolve any pending event through its option buttons.
  Do not inject a date or a private field.
- At a December-to-January boundary, probe the date HUD, open the family chronicle
  (`Canvas/FamilyChronicleRoot/...`) and quote the newest row text. The base annual death
  rates are 0.5–25%. M08B adds 50‰ for each three consecutive shortage months, capped at
  +250‰; at age 80 or above, the effective rate can reach 50%. A death is not guaranteed
  with the default seed; a marriage is a 15% roll per eligible adult. Deterministic EditMode
  covers the hit and miss cases explicitly.
- A guaranteed-marriage check needs a seed that the deterministic tests already proved.
  The seed can only be applied through a code-driven session; that is not a real title-page
  Play Mode route, so do not claim it as one.
- The game has one save slot. Before using the save/load panel, copy both
  `save-slot-1.json` and its `.bak` to a temporary location and record their SHA-256 values.
  Save and reload through the UI, inspect the serialized spouse ID, gender, birth year,
  marriage, pregnancy and RNG state, then restore both original files and verify the hashes.
  Never delete the slot or overwrite a user's only save without a restorable copy.
- Teardown: `unity command editor_stop`, never save the scene. Delete any scratch capture.

## Gotchas

- Natural deaths and autonomous marriages are checked only at the December→January boundary.
  Births and other monthly systems may still change the roster or append chronicle rows in
  other months.
- A birth due in the same settlement resolves first; a gestating parent who dies after the
  birth still dies in that month.
- The head is current-head controlled and never auto-marries; minors, people with an
  unspecified gender, and people with a living spouse are excluded; widowed people are not
  excluded.
- The generated spouse joins the roster as `Spouse`; it is not an M03 autonomy pilot and
  gets no personal relationship value.
- A pending succession decision pauses time advance after the month commits, which is the
  expected behaviour after a head's death, not a broken month.
- Different right-hand profiles: the deterministic EditMode suites
  (`FamilyLifeCycleRulesTests`, `FamilyMarriageSettlementTests`,
  `GameSessionFamilyLifeCycleTests`, `FamilyLifeCycleChronicleTests`) are the authoritative
  evidence for probabilities, ordering and rollback. Play Mode only proves the visible flow.
