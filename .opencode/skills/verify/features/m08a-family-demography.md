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

The post-death tree's `已故` marker was not independently checked. The roster was viewed, but its names, ages and generation labels were not transcribed into the evidence. Head-death succession and birth linkage also remain unverified in Play Mode.

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
