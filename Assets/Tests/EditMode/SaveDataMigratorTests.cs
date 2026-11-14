using System;
using NUnit.Framework;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Persistence;
using UnityEngine;

namespace SengokuChronicle.Tests.EditMode
{
    /// <summary>SC-021 Task 3: migration-chain coverage for the v8 research block.</summary>
    public sealed class SaveDataMigratorTests
    {
        [Test]
        public void V13ToV14Migration_PreservesPregnancyRowsAndExplicitGenders()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 13;
                legacy.rulesVersion = 7;
                legacy.commission = null;
                // A runtime-authored explicit gender must survive: the legacy
                // normalization would resolve this known ID back to Female.
                Person(legacy, M2FixedScenario.TokihimeId).gender =
                    (int)PersonGender.Unspecified;
                // GameDate.MinYear is 1560, so the fixture conceives inside the
                // supported calendar, before the 1560-04-01 NewGame save, and falls
                // due nine monthly steps later.
                legacy.succession.pregnancies = new[]
                {
                    new FamilyPregnancySaveEntry
                    {
                        gestatingPersonId = M2FixedScenario.ChieId.Value,
                        otherParentId = M2FixedScenario.HarunobuId.Value,
                        conceptionYear = 1560,
                        conceptionMonth = 1,
                        conceptionDay = 1,
                        dueYear = 1560,
                        dueMonth = 10,
                        dueDay = 1
                    }
                };

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.succession.pregnancies, Has.Length.EqualTo(1),
                    "v13 already owns pregnancy rows and they must not be wiped.");
                FamilyPregnancySaveEntry row = current.succession.pregnancies[0];
                Assert.That(row.gestatingPersonId, Is.EqualTo(M2FixedScenario.ChieId.Value));
                Assert.That(row.otherParentId, Is.EqualTo(M2FixedScenario.HarunobuId.Value));
                Assert.That(row.conceptionYear, Is.EqualTo(1560));
                Assert.That(row.conceptionMonth, Is.EqualTo(1));
                Assert.That(row.dueYear, Is.EqualTo(1560));
                Assert.That(row.dueMonth, Is.EqualTo(10));
                Assert.That(FamilyPregnancyRecord.TryCreate(
                    new PersonId(row.gestatingPersonId), new PersonId(row.otherParentId),
                    GameDate.Parse(row.conceptionYear, row.conceptionMonth,
                        row.conceptionDay),
                    GameDate.Parse(row.dueYear, row.dueMonth, row.dueDay),
                    out _, out string dateError), Is.True, dateError);
                Assert.That(Person(current, M2FixedScenario.TokihimeId).gender,
                    Is.EqualTo((int)PersonGender.Unspecified),
                    "v13 genders are authoritative and must not be re-resolved.");
                Assert.That(current.commission.stage,
                    Is.EqualTo((int)ShadowEscortStage.NotAccepted));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(9)]
        public void LegacyVersions_NormalizeAnOutstandingCommissionToPristine(int version)
        {
            GameSession session = GameSession.NewGame();
            try
            {
                Assert.That(session.TryAcceptCommission().Succeeded, Is.True);
                SaveData legacy = session.CaptureSave();
                Assert.That(legacy.commission.stage,
                    Is.EqualTo((int)ShadowEscortStage.Accepted),
                    "the fixture must start from a non-pristine commission row");
                legacy.saveFormatVersion = version;
                legacy.npcAutonomy = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.commission.stage,
                    Is.EqualTo((int)ShadowEscortStage.NotAccepted),
                    "every legacy version must migrate to a pristine commission row.");
                Assert.That(current.commission.investedFunds, Is.Zero);
                Assert.That(current.commission.assigneePersonId, Is.Empty);
                Assert.That(current.commission.outcome,
                    Is.EqualTo((int)CommissionOutcome.None));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V16Presence_RequiresTheCampaignBlock()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData data = session.CaptureSave();
                string json = JsonUtility.ToJson(data);
                Assert.That(json, Does.Contain("\"campaign\""));

                SaveDataPresenceProbe probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);
                SaveOperationResult valid = SaveDataValidator.ValidatePresence(probe, json);
                Assert.That(valid.Succeeded, Is.True, valid.Message);

                string omitted = RemoveRootProperty(json, "campaign");
                Assert.That(omitted, Does.Not.Contain("\"campaign\""));
                SaveDataPresenceProbe omittedProbe =
                    JsonUtility.FromJson<SaveDataPresenceProbe>(omitted);
                SaveOperationResult malformed =
                    SaveDataValidator.ValidatePresence(omittedProbe, omitted);
                Assert.That(malformed.Succeeded, Is.False);
                Assert.That(malformed.Message, Does.Contain("campaign"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V15Presence_AcceptsTheM07BlocksWithoutCampaignOrMarket()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData data = session.CaptureSave();
                data.saveFormatVersion = 15;
                data.rulesVersion = SaveData.CurrentRulesVersion;
                data.campaign = null;
                data.market = null;

                // Non-default M07 state. Reset any block to its migration default
                // (fame 0 / concealment 50 / no relations, no injuries, empty history)
                // and the value checks plus the before/after comparison below fail.
                data.familyStanding = new FamilyStandingSaveData
                {
                    fame = 37,
                    concealment = 61,
                    relations = new[]
                    {
                        new FamilyRelationSaveEntry
                        {
                            relationId = MissionRelationIds.ShadowEscortMerchant,
                            value = 12
                        }
                    }
                };
                data.injury = new MissionInjurySaveData
                {
                    persons = new[]
                    {
                        new MissionInjuryPersonSaveEntry
                        {
                            personId = M2FixedScenario.ChieId.Value
                        }
                    }
                };

                HistoryLedger ledger = HistoryLedger.CreateEmpty();
                Assert.That(ledger.TryAppendMission(1560, 4,
                    FamilyChronicleEntry.ShadowEscortCommissionId, "settle",
                    (int)ShadowEscortStage.InProgress, (int)ShadowEscortStage.PartialSuccess,
                    "护送委托", "v15 presence fixture", out FamilyChronicleEntry linked,
                    out string chronicleError), Is.True, chronicleError);
                data.chronicle = ChronicleSaveCodec.Capture(ledger);

                PersonId escort = M2FixedScenario.HarunobuId;
                Assert.That(MissionRecord.TryCreate("run.v15.fixture",
                    FamilyChronicleEntry.ShadowEscortCommissionId, MissionKind.ShadowEscort,
                    MissionOperationMode.Manual, new GameDate(1560, 4, 1),
                    SharedMissionOutcome.PartialSuccess, escort, new[] { escort },
                    new PersonId[0], MissionPreparation.None, 0, 0, 0, 0, 32, 42,
                    36, 37, 60, 61,
                    new[]
                    {
                        new MissionStandingChange(
                            MissionRelationIds.ShadowEscortMerchant, 11, 12)
                    },
                    7, 1, 0,
                    new[] { new MissionParticipantState(escort, true, false, true, false) },
                    linked.EntryId, out MissionRecord record, out string recordError), Is.True,
                    recordError);
                data.missionHistory = new[] { MissionHistorySaveEntry.Capture(record) };

                string json = RemoveRootProperty(
                    RemoveRootProperty(JsonUtility.ToJson(data), "campaign"), "market");
                Assert.That(json, Does.Contain("\"familyStanding\":"));
                Assert.That(json, Does.Contain("\"injury\":"));
                Assert.That(json, Does.Contain("\"missionHistory\":"));
                Assert.That(json, Does.Not.Contain("\"campaign\":"));
                Assert.That(json, Does.Not.Contain("\"market\":"));

                SaveDataPresenceProbe probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);
                Assert.That(probe.familyStanding, Is.Not.Null);
                Assert.That(probe.injury, Is.Not.Null);
                Assert.That(probe.missionHistory, Is.Not.Null);
                SaveOperationResult valid = SaveDataValidator.ValidatePresence(probe, json);
                Assert.That(valid.Succeeded, Is.True,
                    "a v15 payload owns the three M07 blocks; " + valid.Message);

                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                string standingBefore = JsonUtility.ToJson(parsed.familyStanding);
                string injuryBefore = JsonUtility.ToJson(parsed.injury);
                Assert.That(standingBefore, Is.Not.EqualTo(
                        JsonUtility.ToJson(FamilyStandingSaveData.CreateInitial())),
                    "the fixture must not use the migration default standing");
                Assert.That(injuryBefore, Is.Not.EqualTo(
                        JsonUtility.ToJson(MissionInjurySaveData.CreateEmpty())),
                    "the fixture must not use the empty injury default");
                // JsonUtility writes a root array as {}, so the history comparison has
                // to run on the entry object: assert the row is present, then pin its
                // serialized fields as the before-image for the post-migration check.
                Assert.That(parsed.missionHistory, Has.Length.EqualTo(1),
                    "the round-tripped v15 payload must carry the one terminal row it wrote");
                MissionHistorySaveEntry historyRow = parsed.missionHistory[0];
                Assert.That(historyRow.runId, Is.EqualTo("run.v15.fixture"),
                    "the round-tripped v15 history row must be the fixture row");
                Assert.That(historyRow.chronicleEntryId, Is.EqualTo(linked.EntryId));
                string historyRowBefore = JsonUtility.ToJson(historyRow);
                Assert.That(historyRowBefore, Is.Not.EqualTo(
                        JsonUtility.ToJson(new MissionHistorySaveEntry())),
                    "the fixture must not use an empty history row");

                Assert.That(SaveDataMigrator.TryMigrate(parsed, json, out SaveData migrated,
                    out SaveOperationResult migration), Is.True,
                    "a valid v15 file must load; " + migration.Message);
                Assert.That(migrated.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(JsonUtility.ToJson(migrated.familyStanding),
                    Is.EqualTo(standingBefore),
                    "migration must preserve the exact v15 fame, concealment and relation");
                Assert.That(JsonUtility.ToJson(migrated.injury), Is.EqualTo(injuryBefore),
                    "migration must preserve the exact v15 injured roster");
                Assert.That(migrated.missionHistory, Has.Length.EqualTo(1),
                    "migration must preserve the one v15 terminal history row");
                MissionHistorySaveEntry historyAfter = migrated.missionHistory[0];
                Assert.That(JsonUtility.ToJson(historyAfter), Is.EqualTo(historyRowBefore),
                    "migration must preserve every serialized field of the v15 history row");
                Assert.That(historyAfter.runId, Is.EqualTo("run.v15.fixture"));
                Assert.That(historyAfter.missionId,
                    Is.EqualTo(FamilyChronicleEntry.ShadowEscortCommissionId));
                Assert.That(historyAfter.kind, Is.EqualTo((int)MissionKind.ShadowEscort));
                Assert.That(historyAfter.mode,
                    Is.EqualTo((int)MissionOperationMode.Manual));
                Assert.That(historyAfter.year, Is.EqualTo(1560));
                Assert.That(historyAfter.month, Is.EqualTo(4));
                Assert.That(historyAfter.day, Is.EqualTo(1));
                Assert.That(historyAfter.outcome,
                    Is.EqualTo((int)SharedMissionOutcome.PartialSuccess));
                Assert.That(historyAfter.captainPersonId, Is.EqualTo(escort.Value));
                Assert.That(historyAfter.participantPersonIds,
                    Is.EqualTo(new[] { escort.Value }));
                Assert.That(historyAfter.roll, Is.EqualTo(32));
                Assert.That(historyAfter.score, Is.EqualTo(42));
                Assert.That(historyAfter.fameBefore, Is.EqualTo(36));
                Assert.That(historyAfter.fameAfter, Is.EqualTo(37));
                Assert.That(historyAfter.concealmentBefore, Is.EqualTo(60));
                Assert.That(historyAfter.concealmentAfter, Is.EqualTo(61));
                Assert.That(historyAfter.relations, Has.Length.EqualTo(1));
                Assert.That(historyAfter.relations[0].relationId,
                    Is.EqualTo(MissionRelationIds.ShadowEscortMerchant));
                Assert.That(historyAfter.relations[0].before, Is.EqualTo(11));
                Assert.That(historyAfter.relations[0].after, Is.EqualTo(12));
                Assert.That(historyAfter.fundsDelta, Is.EqualTo(7));
                Assert.That(historyAfter.suppliesDelta, Is.EqualTo(1));
                Assert.That(historyAfter.participantStates, Has.Length.EqualTo(1));
                Assert.That(historyAfter.participantStates[0].personId,
                    Is.EqualTo(escort.Value));
                Assert.That(historyAfter.participantStates[0].aliveAfter, Is.True);
                Assert.That(historyAfter.participantStates[0].injuredAfter, Is.False);
                Assert.That(historyAfter.chronicleEntryId, Is.EqualTo(linked.EntryId));
                Assert.That(historyAfter.TryRestore(out MissionRecord restored,
                    out string restoreError), Is.True,
                    "the migrated history row must still rebuild a legal mission record: " +
                    restoreError);
                Assert.That(restored.RunId, Is.EqualTo("run.v15.fixture"));
                Assert.That(restored.Outcome,
                    Is.EqualTo(SharedMissionOutcome.PartialSuccess));
                Assert.That(migrated.campaign, Is.Not.Null,
                    "v15 predates campaign, so migration adds the pristine block");
                Assert.That(migrated.market, Is.Not.Null,
                    "v15 predates market, so migration applies the v17 opening block");


                foreach (string block in new[] { "familyStanding", "injury", "missionHistory" })
                {
                    string omitted = RemoveRootProperty(json, block);
                    SaveDataPresenceProbe omittedProbe =
                        JsonUtility.FromJson<SaveDataPresenceProbe>(omitted);
                    SaveOperationResult malformed =
                        SaveDataValidator.ValidatePresence(omittedProbe, omitted);
                    Assert.That(malformed.Succeeded, Is.False, block);
                    Assert.That(malformed.Message, Does.Contain(block));
                }

                // v14 predates the M07 blocks: the same payload under v14 must be
                // rejected as carrying state its version cannot own.
                SaveData atV14 = session.CaptureSave();
                atV14.saveFormatVersion = 14;
                atV14.rulesVersion = 7;
                atV14.campaign = null;
                atV14.market = null;
                string v14Json = RemoveRootProperty(
                    RemoveRootProperty(JsonUtility.ToJson(atV14), "campaign"), "market");
                SaveDataPresenceProbe v14Probe =
                    JsonUtility.FromJson<SaveDataPresenceProbe>(v14Json);
                SaveOperationResult v14Result =
                    SaveDataValidator.ValidatePresence(v14Probe, v14Json);
                Assert.That(v14Result.Succeeded, Is.False);
                Assert.That(v14Result.Message, Does.Contain("familyStanding"));

                // v15 predates campaign: a payload that carries it must not be migrated
                // over silently.
                SaveData carrying = session.CaptureSave();
                carrying.saveFormatVersion = 15;
                carrying.rulesVersion = SaveData.CurrentRulesVersion;
                carrying.market = null;
                string carryingJson = RemoveRootProperty(JsonUtility.ToJson(carrying), "market");
                SaveDataPresenceProbe carryingProbe =
                    JsonUtility.FromJson<SaveDataPresenceProbe>(carryingJson);
                SaveOperationResult rejected =
                    SaveDataValidator.ValidatePresence(carryingProbe, carryingJson);
                Assert.That(rejected.Succeeded, Is.False);
                Assert.That(rejected.Message, Does.Contain("campaign"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V13Presence_RequiresPregnancyRowsAndKeepsValidOnes()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData data = session.CaptureSave();
                data.saveFormatVersion = 13;
                data.rulesVersion = 7;
                data.commission = null;
                data.succession.pregnancies = new[]
                {
                    new FamilyPregnancySaveEntry
                    {
                        gestatingPersonId = M2FixedScenario.ChieId.Value,
                        otherParentId = M2FixedScenario.HarunobuId.Value,
                        conceptionYear = 1560,
                        conceptionMonth = 1,
                        conceptionDay = 1,
                        dueYear = 1560,
                        dueMonth = 10,
                        dueDay = 1
                    }
                };
                string json = RemoveCommissionIfPresent(JsonUtility.ToJson(data));
                json = RemoveRootProperty(RemoveRootProperty(RemoveRootProperty(json,
                    "familyStanding"), "injury"), "missionHistory");
                json = RemoveRootProperty(json, "campaign");
                json = RemoveRootProperty(json, "market");

                SaveDataPresenceProbe probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);
                SaveOperationResult valid = SaveDataValidator.ValidatePresence(probe, json);
                Assert.That(valid.Succeeded, Is.True, valid.Message);
                Assert.That(probe.succession.pregnancies, Has.Length.EqualTo(1));
                FamilyPregnancySaveEntry row = probe.succession.pregnancies[0];
                Assert.That(GameDate.TryParse(row.conceptionYear, row.conceptionMonth,
                    row.conceptionDay, out GameDate conception), Is.True,
                    "the fixture conception must be a legal game date");
                Assert.That(GameDate.TryParse(row.dueYear, row.dueMonth, row.dueDay,
                    out GameDate due), Is.True, "the fixture due date must be a legal game date");
                Assert.That(FamilyPregnancyRecord.TryCreate(
                    new PersonId(row.gestatingPersonId), new PersonId(row.otherParentId),
                    conception, due, out _, out string dateError), Is.True, dateError);
                Assert.That(conception.CompareTo(new GameDate(data.year, data.month, data.day)),
                    Is.LessThanOrEqualTo(0),
                    "a v13 pregnancy cannot start after the save date");

                // The contract covers a file that omits the field, not a probe nulled by
                // hand: scrub the nested key from the raw JSON and reparse it.
                string omittedJson = RemoveNestedProperty(json, "succession", "pregnancies");
                Assert.That(omittedJson, Does.Not.Contain("\"pregnancies\""));
                SaveDataPresenceProbe omitted =
                    JsonUtility.FromJson<SaveDataPresenceProbe>(omittedJson);
                SaveOperationResult malformed =
                    SaveDataValidator.ValidatePresence(omitted, omittedJson);
                Assert.That(malformed.Succeeded, Is.False);
                Assert.That(malformed.Message, Does.Contain("pregnancies"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        private static string RemoveCommissionIfPresent(string json) =>
            json.Contains("\"commission\":") ? RemoveRootProperty(json, "commission") : json;

        /// <summary>
        /// Removes one direct property of a nested root object from compact JsonUtility
        /// output, so a test can produce the raw omission a real file would carry.
        /// </summary>
        private static string RemoveNestedProperty(string json, string parentName,
            string propertyName)
        {
            int valueStart = RootPropertyValueStart(json, parentName);
            if (valueStart < 0)
                throw new InvalidOperationException("Root property was not found: " + parentName);
            int valueEnd = SkipJsonValue(json, valueStart);
            string nested = json.Substring(valueStart, valueEnd - valueStart);
            return json.Substring(0, valueStart) + RemoveRootProperty(nested, propertyName) +
                   json.Substring(valueEnd);
        }

        /// <summary>Index of the first character of a direct root property value, or -1.</summary>
        private static int RootPropertyValueStart(string json, string propertyName)
        {
            int depth = 0;
            int index = 0;
            while (index < json.Length)
            {
                char current = json[index];
                if (current == '"')
                {
                    int contentStart = ++index;
                    bool escaped = false;
                    while (index < json.Length)
                    {
                        char character = json[index];
                        if (escaped) escaped = false;
                        else if (character == '\\') escaped = true;
                        else if (character == '"') break;
                        index++;
                    }
                    if (index >= json.Length)
                        break;

                    int contentLength = index - contentStart;
                    index++;
                    if (depth == 1 && contentLength == propertyName.Length &&
                        string.CompareOrdinal(json, contentStart, propertyName, 0,
                            contentLength) == 0)
                    {
                        int lookahead = index;
                        while (lookahead < json.Length && char.IsWhiteSpace(json[lookahead]))
                            lookahead++;
                        if (lookahead < json.Length && json[lookahead] == ':')
                        {
                            int valueStart = lookahead + 1;
                            while (valueStart < json.Length &&
                                   char.IsWhiteSpace(json[valueStart]))
                                valueStart++;
                            return valueStart;
                        }
                    }
                    continue;
                }

                if (current == '{' || current == '[')
                    depth++;
                else if (current == '}' || current == ']')
                    depth--;
                index++;
            }
            return -1;
        }

        /// <summary>Removes one direct root property from compact JsonUtility output.</summary>
        private static string RemoveRootProperty(string json, string propertyName)
        {
            int depth = 0;
            int index = 0;
            while (index < json.Length)
            {
                char current = json[index];
                if (current == '"')
                {
                    int keyStart = index;
                    int contentStart = ++index;
                    bool escaped = false;
                    while (index < json.Length)
                    {
                        char character = json[index];
                        if (escaped) escaped = false;
                        else if (character == '\\') escaped = true;
                        else if (character == '"') break;
                        index++;
                    }
                    if (index >= json.Length)
                        break;

                    int contentLength = index - contentStart;
                    index++;
                    if (depth == 1 && contentLength == propertyName.Length &&
                        string.CompareOrdinal(json, contentStart, propertyName, 0,
                            contentLength) == 0)
                    {
                        int lookahead = index;
                        while (lookahead < json.Length && char.IsWhiteSpace(json[lookahead]))
                            lookahead++;
                        if (lookahead < json.Length && json[lookahead] == ':')
                        {
                            int valueStart = lookahead + 1;
                            while (valueStart < json.Length && char.IsWhiteSpace(json[valueStart]))
                                valueStart++;
                            int valueEnd = SkipJsonValue(json, valueStart);
                            int removeStart = keyStart;
                            int before = keyStart - 1;
                            while (before >= 0 && char.IsWhiteSpace(json[before]))
                                before--;
                            if (before >= 0 && json[before] == ',')
                            {
                                removeStart = before;
                            }
                            else
                            {
                                int after = valueEnd;
                                while (after < json.Length && char.IsWhiteSpace(json[after]))
                                    after++;
                                if (after < json.Length && json[after] == ',')
                                    valueEnd = after + 1;
                            }

                            return json.Substring(0, removeStart) + json.Substring(valueEnd);
                        }
                    }
                    continue;
                }

                if (current == '{' || current == '[')
                    depth++;
                else if (current == '}' || current == ']')
                    depth--;
                index++;
            }

            throw new InvalidOperationException("Root property was not found: " + propertyName);
        }

        private static int SkipJsonValue(string json, int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index]))
                index++;
            if (index >= json.Length)
                return index;

            if (json[index] == '"')
            {
                index++;
                bool escaped = false;
                while (index < json.Length)
                {
                    char character = json[index];
                    if (escaped) escaped = false;
                    else if (character == '\\') escaped = true;
                    else if (character == '"') return index + 1;
                    index++;
                }
                return index;
            }

            if (json[index] == '{' || json[index] == '[')
            {
                int depth = 0;
                while (index < json.Length)
                {
                    char character = json[index];
                    if (character == '"')
                    {
                        index = SkipJsonString(json, index);
                        continue;
                    }
                    if (character == '{' || character == '[')
                        depth++;
                    else if (character == '}' || character == ']')
                    {
                        depth--;
                        if (depth == 0)
                            return index + 1;
                    }
                    index++;
                }
                return index;
            }

            while (index < json.Length && json[index] != ',' && json[index] != '}' &&
                   json[index] != ']')
                index++;
            return index;
        }

        private static int SkipJsonString(string json, int index)
        {
            index++;
            bool escaped = false;
            while (index < json.Length)
            {
                char character = json[index];
                if (escaped) escaped = false;
                else if (character == '\\') escaped = true;
                else if (character == '"') return index + 1;
                index++;
            }
            return index;
        }

        [Test]
        public void V14Rules7_InProgressCommission_GetsLegacyRunIdAndInitialStanding()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                Assert.That(session.TryAcceptCommission().Succeeded, Is.True);
                Assert.That(session.TrySetCommissionMode(MissionOperationMode.Manual, out _),
                    Is.True);
                PersonId assignee = default;
                foreach (CommissionAssigneeOption option in session.CommissionAssignees)
                    if (option.Eligible)
                    {
                        assignee = option.PersonId;
                        break;
                    }
                Assert.That(session.TryAssignCommission(assignee).Succeeded, Is.True);
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 14;
                legacy.rulesVersion = 7;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
                Assert.That(current.commission.runId, Is.EqualTo("legacy.m05.current"));
                Assert.That(current.commission.mode,
                    Is.EqualTo((int)MissionOperationMode.LegacyUnknown));
                Assert.That(current.familyStanding.fame,
                    Is.EqualTo(FamilyStandingState.InitialFame));
                Assert.That(current.familyStanding.concealment,
                    Is.EqualTo(FamilyStandingState.InitialConcealment));
                Assert.That(current.familyStanding.relations, Is.Empty);
                Assert.That(current.injury.persons, Is.Empty);
                Assert.That(current.missionHistory, Is.Empty,
                    "an in-progress run has no terminal history yet.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V14Rules7_SettledCommission_BackfillsOneLegacyHistoryRow()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                Assert.That(session.TryAcceptCommission().Succeeded, Is.True);
                Assert.That(session.TrySetCommissionMode(MissionOperationMode.Manual, out _),
                    Is.True);
                PersonId assignee = default;
                foreach (CommissionAssigneeOption option in session.CommissionAssignees)
                    if (option.Eligible)
                    {
                        assignee = option.PersonId;
                        break;
                    }
                Assert.That(session.TryAssignCommission(assignee).Succeeded, Is.True);
                for (int i = 0; i < ShadowEscortCommission.DurationDays; i++)
                    session.AdvanceOneDay();
                Assert.That(session.TrySettleCommissionManually().Succeeded, Is.True);
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 14;
                legacy.rulesVersion = 7;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.commission.runId, Is.EqualTo("legacy.m05.current"));
                Assert.That(current.missionHistory, Has.Length.EqualTo(1));
                MissionHistorySaveEntry row = current.missionHistory[0];
                Assert.That(row.runId, Is.EqualTo("legacy.m05.current"));
                Assert.That(row.mode, Is.EqualTo((int)MissionOperationMode.LegacyUnknown));
                Assert.That(row.kind, Is.EqualTo((int)MissionKind.ShadowEscort));
                Assert.That(row.chronicleEntryId, Is.Not.Empty);
                Assert.That(current.familyStanding.fame,
                    Is.EqualTo(FamilyStandingState.InitialFame),
                    "migration never replays fame from a pre-M07 result.");
                Assert.That(current.familyStanding.concealment,
                    Is.EqualTo(FamilyStandingState.InitialConcealment));

                int settleRows = 0;
                foreach (ChronicleSaveEntry entry in current.chronicle)
                    if (entry != null && entry.category == (int)ChronicleCategory.Mission &&
                        entry.subjectId == FamilyChronicleEntry.ShadowEscortCommissionId &&
                        entry.sourceId == "settle")
                        settleRows++;
                Assert.That(settleRows, Is.EqualTo(1),
                    "the legacy terminal row is linked exactly once, never duplicated.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V14Rules7_MigratedInProgressCommission_AbandonsIntoASaveableRow()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitStandardDemo();
                PersonId assignee = session.Succession.HeadPersonId;
                Assert.That(session.TryAcceptCommission().Succeeded, Is.True);
                Assert.That(session.TryAssignCommission(assignee).Succeeded, Is.True,
                    session.LastCommissionMessage);
                Assert.That(session.AdvanceDays(1), Is.True);
                Assert.That(session.CommissionStage,
                    Is.EqualTo(ShadowEscortStage.InProgress));

                // Retirement does not interrupt the running trip, but the retired member
                // can no longer execute it: the due date is where settlement notices.
                Assert.That(session.BeginSuccessionRetirement().Succeeded, Is.True,
                    session.LastSuccessionMessage);
                if (session.Succession.Phase == SuccessionPhase.AwaitingSuccessionChoice)
                    Assert.That(session.SelectSuccessionSuccessor(
                        new PersonId("person.akechi.masatsuna")).Succeeded, Is.True,
                        session.LastSuccessionMessage);
                Assert.That(session.ConfirmSuccession().Succeeded, Is.True,
                    session.LastSuccessionMessage);
                Assert.That(session.CommissionStage,
                    Is.EqualTo(ShadowEscortStage.InProgress),
                    "retirement alone must not interrupt the trip.");
                Assert.That(session.CommissionAssignee, Is.EqualTo(assignee));

                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 14;
                legacy.rulesVersion = 7;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData migrated,
                    out SaveOperationResult migration), Is.True, migration.Message);
                Assert.That(migrated.commission.runId, Is.EqualTo("legacy.m05.current"));
                Assert.That(migrated.commission.mode,
                    Is.EqualTo((int)MissionOperationMode.LegacyUnknown));

                GameSession loaded = GameSession.NewGame();
                try
                {
                    SaveOperationResult applied = loaded.TryApplySave(migrated);
                    Assert.That(applied.Succeeded, Is.True, applied.Message);
                    Assert.That(loaded.CommissionMode,
                        Is.EqualTo(MissionOperationMode.LegacyUnknown));
                    Assert.That(loaded.AdvanceDays(loaded.CommissionRemainingDays), Is.True);
                    Assert.That(loaded.CommissionStage,
                        Is.EqualTo(ShadowEscortStage.Abandoned),
                        "an unavailable assignee closes the migrated run once.");
                    Assert.That(loaded.CommissionOutcome,
                        Is.EqualTo(CommissionOutcome.None));
                    Assert.That(loaded.MissionHistory, Is.Empty,
                        "abandonment writes no shared-result history row.");

                    SaveData abandoned = loaded.CaptureSave();
                    Assert.That(abandoned.commission.runId, Is.Empty,
                        "abandonment clears the legacy run ID.");
                    Assert.That(abandoned.commission.mode,
                        Is.EqualTo((int)MissionOperationMode.LegacyUnknown),
                        "the migrated mode is preserved instead of guessed.");
                    SaveOperationResult validated = SaveDataValidator.Validate(abandoned);
                    Assert.That(validated.Succeeded, Is.True,
                        "a migrated legacy abandoned row must stay saveable: " +
                        validated.Message);

                    GameSession reloaded = GameSession.NewGame();
                    try
                    {
                        SaveOperationResult reloadedApplied =
                            reloaded.TryApplySave(abandoned);
                        Assert.That(reloadedApplied.Succeeded, Is.True,
                            reloadedApplied.Message);
                        Assert.That(reloaded.CommissionStage,
                            Is.EqualTo(ShadowEscortStage.Abandoned));
                        Assert.That(reloaded.CommissionStatus.Feedback,
                            Does.Contain("无法执行护送委托"),
                            "the abandon reason must survive save and load.");
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(reloaded.gameObject);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(loaded.gameObject);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void CurrentVersionSave_WithMissingInjuryBlock_IsRejectedNotRepaired()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData data = session.CaptureSave();
                data.injury = null;

                Assert.That(SaveDataMigrator.TryMigrate(data, null, out SaveData current,
                        out SaveOperationResult result), Is.False,
                    "a malformed current payload must not be silently repaired.");
                Assert.That(current, Is.Null);
                Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
                Assert.That(result.Message, Does.Contain("injury"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V14Rules8_IsRejectedBecauseV14RequiresRules7()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 14;
                legacy.rulesVersion = 8;
                Assert.That(SaveDataMigrator.TryMigrate(legacy, out _,
                    out SaveOperationResult result), Is.False);
                Assert.That(result.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V15Rules7_IsRejected()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 15;
                legacy.rulesVersion = 7;
                legacy.campaign = null;
                legacy.market = null;

                Assert.That(SaveDataValidator.Validate(legacy).Succeeded, Is.False,
                    "Validate only accepts the current schema; the v15 rules check runs in the migrator");
                Assert.That(SaveDataMigrator.TryMigrate(legacy, out _,
                    out SaveOperationResult result), Is.False);
                Assert.That(result.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
                Assert.That(result.Message, Does.Contain("v15"));
                Assert.That(result.Message, Does.Contain("7"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void CurrentVersionRules7_IsRejected()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData current = session.CaptureSave();
                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                current.rulesVersion = 7;

                Assert.That(SaveDataValidator.Validate(current).Succeeded, Is.False);
                Assert.That(SaveDataMigrator.TryMigrate(current, out _,
                    out SaveOperationResult result), Is.False);
                Assert.That(result.Error, Is.EqualTo(SaveErrorCode.UnsupportedVersion));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V15Migration_RejectsANullFamilyStandingBlock()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 15;
                legacy.rulesVersion = SaveData.CurrentRulesVersion;
                legacy.campaign = null;
                legacy.market = null;
                legacy.familyStanding = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, out SaveData current,
                    out SaveOperationResult result), Is.False,
                    "direct migration must not recreate a missing v15 block");
                Assert.That(current, Is.Null);
                Assert.That(result.Error, Is.EqualTo(SaveErrorCode.InvalidData));
                Assert.That(result.Message, Does.Contain("familyStanding"));

                // The same v15 shape with the key scrubbed from raw JSON must also be
                // rejected by the migrator, matching the SaveSlotStore presence guard.
                SaveData carrying = session.CaptureSave();
                carrying.saveFormatVersion = 15;
                carrying.rulesVersion = SaveData.CurrentRulesVersion;
                carrying.campaign = null;
                carrying.market = null;
                string json = RemoveRootProperty(JsonUtility.ToJson(carrying),
                    "familyStanding");
                json = RemoveRootProperty(RemoveRootProperty(json, "campaign"), "market");
                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                Assert.That(SaveDataMigrator.TryMigrate(parsed, json, out SaveData rawCurrent,
                    out SaveOperationResult rawResult), Is.False,
                    "raw JSON without familyStanding must not be silently repaired");
                Assert.That(rawCurrent, Is.Null);
                Assert.That(rawResult.Message, Does.Contain("familyStanding"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        private static SuccessionPersonSaveEntry Person(SaveData data, PersonId personId)
        {
            foreach (SuccessionPersonSaveEntry entry in data.succession.persons)
                if (entry != null && entry.personId == personId.Value)
                    return entry;
            Assert.Fail("Missing saved person " + personId.Value);
            return null;
        }

        [Test]
        public void V13ToV14Migration_AddsPristineCommissionAndRejectsCarryingOne()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 13;
                legacy.rulesVersion = 7;
                legacy.commission = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);
                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.commission, Is.Not.Null);
                Assert.That(current.commission.stage,
                    Is.EqualTo((int)ShadowEscortStage.NotAccepted));
                Assert.That(current.commission.investedFunds, Is.Zero);
                Assert.That(current.commission.assigneePersonId, Is.Empty);
                Assert.That(current.commission.outcome, Is.EqualTo((int)CommissionOutcome.None));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }

            GameSession carryingSession = GameSession.NewGame();
            try
            {
                SaveData carrying = carryingSession.CaptureSave();
                carrying.saveFormatVersion = 13;
                carrying.rulesVersion = 7;
                string json = JsonUtility.ToJson(carrying);
                string withoutCampaign = RemoveRootProperty(
                    RemoveRootProperty(json, "campaign"), "market");
                SaveData parsed = JsonUtility.FromJson<SaveData>(withoutCampaign);

                Assert.That(SaveDataMigrator.TryMigrate(parsed, withoutCampaign, out _,
                    out SaveOperationResult rejected), Is.False);
                Assert.That(rejected.Message, Does.Contain("commission"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(carryingSession.gameObject);
            }
        }

        [Test]
        public void V7ToV8Migration_InitializesEmptyResearchAtThePriorMonth()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitResearchDemoScenario();
                SaveData legacy = session.CaptureSave();
                GameDate date = session.WorldClock.CurrentDate;
                legacy.saveFormatVersion = 7;
                legacy.rulesVersion = 3;
                legacy.research = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
                Assert.That(current.research.researchId, Is.Empty);
                Assert.That(current.research.knownTechniqueIds, Is.Empty);
                Assert.That(current.research.lastSettledMonthIndex,
                    Is.EqualTo(IndustryCalendar.MonthIndex(date) - 1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V7ToV8Migration_AcceptsLegitimateDevelopmentJsonAndPreservesIt()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                // SC-020's fixed scenario gives the v7 development block a real,
                // non-default payload so this test would fail if the generic legacy
                // "development" rejection were applied to v7 again.
                session.InitEducationDemoScenario();
                SaveData captured = session.CaptureSave();
                captured.saveFormatVersion = 7;
                captured.rulesVersion = 3;
                string json = LegitimateV7Json(captured);

                Assert.That(json, Does.Not.Contain("\"research\":"));
                Assert.That(json, Does.Contain("\"development\":"));
                Assert.That(json, Does.Contain("\"industry\":"));

                SaveOperationResult presence = SaveDataValidator.ValidatePresence(
                    JsonUtility.FromJson<SaveDataPresenceProbe>(json), json);
                Assert.That(presence.Succeeded, Is.True, presence.Message);

                // Load the file the way SaveSlotStore does: JsonUtility fills the probe
                // and parsed data, while the raw JSON remains the presence authority.
                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                Assert.That(SaveDataMigrator.TryMigrate(parsed, json, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.rulesVersion, Is.EqualTo(SaveData.CurrentRulesVersion));
                Assert.That(current.research, Is.Not.Null);
                Assert.That(current.research.researchId, Is.Empty);
                Assert.That(current.research.researcherPersonId, Is.Empty);
                Assert.That(current.research.progress, Is.Zero);
                Assert.That(current.research.knownTechniqueIds, Is.Empty);
                Assert.That(current.research.lastSettledMonthIndex,
                    Is.EqualTo(current.industry.lastSettledMonthIndex),
                    "migration initializes the prior research month without replaying one");

                Assert.That(JsonUtility.ToJson(current.development),
                    Is.EqualTo(JsonUtility.ToJson(parsed.development)),
                    "the existing v7 growth rows must survive migration unchanged");
                CharacterDevelopmentPersonSaveEntry child = null;
                foreach (CharacterDevelopmentPersonSaveEntry person in current.development.persons)
                    if (person.personId == SC020EducationScenario.ChildIdValue)
                        child = person;
                Assert.That(child, Is.Not.Null);
                Assert.That(child.ninjutsu, Is.EqualTo(SC020EducationScenario.ChildNinjutsu));
                Assert.That(child.scholarship, Is.EqualTo(SC020EducationScenario.ChildScholarship));
                Assert.That(child.plans, Is.Empty);
                Assert.That(child.results, Is.Empty);

                Assert.That(JsonUtility.ToJson(current.industry.slots),
                    Is.EqualTo(JsonUtility.ToJson(parsed.industry.slots)));
                Assert.That(current.industry.funds, Is.EqualTo(parsed.industry.funds));
                Assert.That(current.industry.intelligence,
                    Is.EqualTo(parsed.industry.intelligence));
                Assert.That(current.industry.supplies,
                    Is.EqualTo(parsed.industry.supplies - current.market.foodInventory),
                    "v17 migration moves the one-month food demand out of the existing stock");
                Assert.That(JsonUtility.ToJson(current.familyRelationships),
                    Is.EqualTo(JsonUtility.ToJson(parsed.familyRelationships)));
                Assert.That(JsonUtility.ToJson(current.chronicle),
                    Is.EqualTo(JsonUtility.ToJson(parsed.chronicle)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void CurrentVersionMigration_PreservesGendersAndPregnancies()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitFamilyFormationDemoScenario();
                GameDate date = session.WorldClock.CurrentDate;
                Assert.That(FamilyPregnancyRecord.TryCreate(
                    SC022FamilyFormationScenario.SpouseId,
                    SC022FamilyFormationScenario.HeadId, date,
                    FamilyPregnancyRecord.AddMonths(date,
                        FamilyPregnancyRecord.GestationMonths),
                    out FamilyPregnancyRecord record, out string error), Is.True, error);
                Assert.That(session.Succession.World.Pregnancies.TryAdd(record, out error),
                    Is.True, error);

                SaveData current = session.CaptureSave();
                Assert.That(SaveDataMigrator.TryMigrate(current, null, out SaveData migrated,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(migrated.succession.pregnancies, Has.Length.EqualTo(1));
                Assert.That(migrated.succession.pregnancies[0].gestatingPersonId,
                    Is.EqualTo(SC022FamilyFormationScenario.SpouseId.Value));
                Assert.That(GenderOf(migrated, SC022FamilyFormationScenario.SpouseId),
                    Is.EqualTo(PersonGender.Female));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V12ToV13Migration_ResolvesOnlyAuthoredGendersAndAddsNoPregnancies()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitFamilyFormationDemoScenario();
                var unknownId = new PersonId("person.sc022.custom.unknown");
                Assert.That(session.Succession.World.TryAddPerson(
                    new PersonState(unknownId, "无名氏"),
                    new SuccessionPersonLife(1540, true, false), out string addError),
                    Is.True, addError);
                Assert.That(session.Development.TryAddPerson(unknownId,
                    CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0),
                    out string developmentError), Is.True, developmentError);

                SaveData legacy = session.CaptureSave();
                int persons = legacy.succession.persons.Length;
                int kinship = legacy.succession.kinship.Length;
                legacy.saveFormatVersion = 12;
                // v12 shipped under rules 7; rules 8 belongs to the current format only.
                legacy.rulesVersion = 7;
                foreach (SuccessionPersonSaveEntry person in legacy.succession.persons)
                    person.gender = (int)PersonGender.Unspecified;
                legacy.succession.pregnancies = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.succession.persons, Has.Length.EqualTo(persons));
                Assert.That(current.succession.kinship, Has.Length.EqualTo(kinship),
                    "v12 migration must preserve every kinship fact");
                Assert.That(current.succession.pregnancies, Is.Empty);

                Assert.That(GenderOf(current, SC022FamilyFormationScenario.HeadId),
                    Is.EqualTo(PersonGender.Male));
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.SpouseId),
                    Is.EqualTo(PersonGender.Female));
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.MarriageCandidateOneId),
                    Is.EqualTo(PersonGender.Female));
                Assert.That(GenderOf(current, unknownId),
                    Is.EqualTo(PersonGender.Unspecified),
                    "unknown IDs must never be inferred from names or kinship");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V12ToV13Migration_InitializesEmptyPregnanciesEvenWhenBlockMaterializes()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitFamilyFormationDemoScenario();
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 12;
                // v12 shipped under rules 7; rules 8 belongs to the current format only.
                legacy.rulesVersion = 7;
                foreach (SuccessionPersonSaveEntry person in legacy.succession.persons)
                    person.gender = (int)PersonGender.Unspecified;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.succession.pregnancies, Is.Empty);
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.HeadId),
                    Is.EqualTo(PersonGender.Male));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [TestCase(9)]
        [TestCase(6)]
        public void SupportedPreV13Versions_ResolveCatalogGendersAndKeepUnknownUnspecified(
            int version)
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitFamilyFormationDemoScenario();
                var unknownId = new PersonId("person.sc022.legacy.unknown");
                Assert.That(session.Succession.World.TryAddPerson(
                    new PersonState(unknownId, "旧档无名"),
                    new SuccessionPersonLife(1540, true, false), out string addError),
                    Is.True, addError);
                Assert.That(session.Development.TryAddPerson(unknownId,
                    CharacterGrowthValues.CreateClamped(0, 0, 0, 0, 0),
                    out string developmentError), Is.True, developmentError);

                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = version;
                legacy.rulesVersion = version >= 8
                    ? SaveData.CurrentRulesVersion
                    : 2;
                foreach (SuccessionPersonSaveEntry person in legacy.succession.persons)
                    person.gender = (int)PersonGender.Unspecified;
                legacy.succession.pregnancies = null;
                if (version == 6)
                {
                    legacy.development = null;
                    legacy.research = null;
                    legacy.familyDoctrine = null;
                }

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.succession.pregnancies, Is.Empty);
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.HeadId),
                    Is.EqualTo(PersonGender.Male), "authored ID resolves from catalog");
                Assert.That(GenderOf(current, SC022FamilyFormationScenario.SpouseId),
                    Is.EqualTo(PersonGender.Female));
                Assert.That(GenderOf(current, unknownId),
                    Is.EqualTo(PersonGender.Unspecified),
                    "unknown IDs must not be inferred");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        private static PersonGender GenderOf(SaveData data, PersonId personId)
        {
            SuccessionPersonSaveEntry entry = Array.Find(data.succession.persons,
                person => person.personId == personId.Value);
            Assert.That(entry, Is.Not.Null, personId.Value);
            return (PersonGender)entry.gender;
        }

        [Test]
        public void V11ToV12Migration_AddsOnlyEmptyMemories()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.AdvanceOneDay();
                SaveData legacy = session.CaptureSave();
                string dailyActionsJson = JsonUtility.ToJson(legacy.dailyActions);
                legacy.saveFormatVersion = 11;
                // v11 shipped under rules 7; rules 8 belongs to the current format only.
                legacy.rulesVersion = 7;
                foreach (NpcAutonomyPersonSaveEntry entry in legacy.npcAutonomy.persons)
                    entry.memories = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.npcAutonomy.persons, Has.Length.EqualTo(2));
                foreach (NpcAutonomyPersonSaveEntry entry in current.npcAutonomy.persons)
                    Assert.That(entry.memories, Is.Empty);
                Assert.That(JsonUtility.ToJson(current.dailyActions),
                    Is.EqualTo(dailyActionsJson),
                    "v11 migration must not replay or alter daily actions");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V6ToV8Migration_AddsResearchAndPreservesTheRest()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitResearchDemoScenario();
                SaveData legacy = session.CaptureSave();
                int persons = legacy.succession.persons.Length;
                legacy.saveFormatVersion = 6;
                legacy.rulesVersion = 2;
                legacy.development = null;
                legacy.research = null;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion, Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.development.persons.Length, Is.EqualTo(persons));
                Assert.That(current.research, Is.Not.Null);
                Assert.That(current.research.knownTechniqueIds, Is.Empty);
                Assert.That(current.research.lastSettledMonthIndex,
                    Is.EqualTo(current.industry.lastSettledMonthIndex));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V7Migration_RejectsAConflictingResearchPayload()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                session.InitResearchDemoScenario();
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 7;
                legacy.rulesVersion = 3;
                string json = UnityEngine.JsonUtility.ToJson(legacy, true);

                Assert.That(SaveDataMigrator.TryMigrate(legacy, json, out SaveData current,
                    out SaveOperationResult result), Is.False);
                Assert.That(result.Message, Does.Contain("research"));
                Assert.That(current, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V16ToV17Migration_MovesOneMonthOfFoodOutOfLegacySupplies()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 16;
                legacy.rulesVersion = SaveData.CurrentRulesVersion;
                legacy.market = null;
                int funds = legacy.industry.funds;
                int supplies = legacy.industry.supplies;
                int intelligence = legacy.industry.intelligence;
                int settled = legacy.industry.lastSettledMonthIndex;
                int rulesVersion = legacy.rulesVersion;
                string successionJson = JsonUtility.ToJson(legacy.succession);
                string commissionJson = JsonUtility.ToJson(legacy.commission);
                string campaignJson = JsonUtility.ToJson(legacy.campaign);
                string standingJson = JsonUtility.ToJson(legacy.familyStanding);
                // JsonUtility writes a root array as {}, so the slot and chronicle checks
                // compare rows by value; a fresh NewGame ledger is empty, so seed one real
                // terminal chronicle row for the survival check to mean anything.
                Assert.That(legacy.industry.slots, Is.Not.Empty,
                    "the fixture must own industry slot rows");
                IndustrySlotSaveEntry[] slotsBefore = legacy.industry.slots;
                HistoryLedger ledger = HistoryLedger.CreateEmpty();
                Assert.That(ledger.TryAppendMission(1560, 4,
                    FamilyChronicleEntry.ShadowEscortCommissionId, "settle",
                    (int)ShadowEscortStage.InProgress, (int)ShadowEscortStage.PartialSuccess,
                    "护送委托", "v16 slot fixture", out FamilyChronicleEntry chronicleRow,
                    out string chronicleError), Is.True, chronicleError);
                legacy.chronicle = ChronicleSaveCodec.Capture(ledger);
                ChronicleSaveEntry[] chronicleBefore = legacy.chronicle;
                Assert.That(chronicleBefore, Has.Length.EqualTo(1),
                    "the fixture must own one chronicle row");

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(SaveDataValidator.CurrentVersion, Is.EqualTo(17));
                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.rulesVersion, Is.EqualTo(rulesVersion),
                    "the v16 rulesVersion is preserved by the migration");
                Assert.That(current.market, Is.Not.Null);
                Assert.That(current.market.foodInventory, Is.EqualTo(2),
                    "the six-person family's one-month food demand is carved out of supplies");
                Assert.That(current.industry.supplies, Is.EqualTo(supplies - 2));
                Assert.That(current.market.foodInventory + current.industry.supplies,
                    Is.EqualTo(supplies), "migration creates no inventory");
                Assert.That(current.industry.funds, Is.EqualTo(funds));
                Assert.That(current.industry.intelligence, Is.EqualTo(intelligence));
                Assert.That(current.industry.lastSettledMonthIndex, Is.EqualTo(settled));
                Assert.That(current.market.lastSettledMonthIndex, Is.EqualTo(settled));
                Assert.That(current.market.foodPriceIndexPermille,
                    Is.EqualTo(MarketState.BaselinePriceIndexPermille));
                Assert.That(current.market.materialsPriceIndexPermille,
                    Is.EqualTo(MarketState.BaselinePriceIndexPermille));
                Assert.That(current.market.consecutiveFoodShortageMonths, Is.Zero,
                    "migration never invents a famine history");
                Assert.That(current.market.compositeHistory, Is.Empty);

                Assert.That(JsonUtility.ToJson(current.succession), Is.EqualTo(successionJson));
                Assert.That(current.industry.slots, Has.Length.EqualTo(slotsBefore.Length),
                    "migration must keep every v16 industry slot row");
                for (int i = 0; i < slotsBefore.Length; i++)
                {
                    Assert.That(current.industry.slots[i].industryId,
                        Is.EqualTo(slotsBefore[i].industryId), "slot " + i);
                    Assert.That(current.industry.slots[i].assignedPersonId,
                        Is.EqualTo(slotsBefore[i].assignedPersonId), "slot " + i);
                }

                Assert.That(current.chronicle, Has.Length.EqualTo(chronicleBefore.Length),
                    "migration must keep every v16 chronicle row");
                for (int i = 0; i < chronicleBefore.Length; i++)
                {
                    ChronicleSaveEntry before = chronicleBefore[i];
                    ChronicleSaveEntry after = current.chronicle[i];
                    Assert.That(after.entryId, Is.EqualTo(before.entryId), "chronicle " + i);
                    Assert.That(after.sequence, Is.EqualTo(before.sequence), "chronicle " + i);
                    Assert.That(after.year, Is.EqualTo(before.year), "chronicle " + i);
                    Assert.That(after.month, Is.EqualTo(before.month), "chronicle " + i);
                    Assert.That(after.category, Is.EqualTo(before.category), "chronicle " + i);
                    Assert.That(after.subjectId, Is.EqualTo(before.subjectId), "chronicle " + i);
                    Assert.That(after.sourceId, Is.EqualTo(before.sourceId), "chronicle " + i);
                    Assert.That(after.title, Is.EqualTo(before.title), "chronicle " + i);
                    Assert.That(after.summary, Is.EqualTo(before.summary), "chronicle " + i);
                    Assert.That(after.fromStage, Is.EqualTo(before.fromStage), "chronicle " + i);
                    Assert.That(after.toStage, Is.EqualTo(before.toStage), "chronicle " + i);
                }
                Assert.That(current.chronicle[0].entryId,
                    Is.EqualTo(chronicleRow.EntryId));
                Assert.That(JsonUtility.ToJson(current.commission), Is.EqualTo(commissionJson));
                Assert.That(JsonUtility.ToJson(current.campaign), Is.EqualTo(campaignJson),
                    "v16 already owns the campaign block and keeps its acknowledgements");
                Assert.That(JsonUtility.ToJson(current.familyStanding),
                    Is.EqualTo(standingJson));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V16ToV17Migration_WithLowSupplies_MovesOnlyTheExistingStock()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData legacy = session.CaptureSave();
                legacy.saveFormatVersion = 16;
                legacy.rulesVersion = SaveData.CurrentRulesVersion;
                legacy.market = null;
                legacy.industry.supplies = 1;

                Assert.That(SaveDataMigrator.TryMigrate(legacy, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.market.foodInventory, Is.EqualTo(1),
                    "at most the one-month demand is moved, and never more than the stock");
                Assert.That(current.industry.supplies, Is.Zero);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V16RawJsonCarryingMarket_IsRejectedByPresenceAndMigrator()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData data = session.CaptureSave();
                data.saveFormatVersion = 16;
                data.rulesVersion = SaveData.CurrentRulesVersion;
                string json = JsonUtility.ToJson(data);
                Assert.That(json, Does.Contain("\"market\":"));

                SaveDataPresenceProbe probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);
                SaveOperationResult presence = SaveDataValidator.ValidatePresence(probe, json);
                Assert.That(presence.Succeeded, Is.False);
                Assert.That(presence.Message, Does.Contain("market"),
                    "a v16 file must never lose a market block silently");

                SaveData parsed = JsonUtility.FromJson<SaveData>(json);
                Assert.That(SaveDataMigrator.TryMigrate(parsed, json, out _,
                    out SaveOperationResult result), Is.False);
                Assert.That(result.Message, Does.Contain("market"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void V17Presence_RequiresTheMarketBlock()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData data = session.CaptureSave();
                string json = JsonUtility.ToJson(data);
                Assert.That(json, Does.Contain("\"market\":"));

                SaveDataPresenceProbe probe = JsonUtility.FromJson<SaveDataPresenceProbe>(json);
                Assert.That(SaveDataValidator.ValidatePresence(probe, json).Succeeded, Is.True);

                string omitted = RemoveRootProperty(json, "market");
                Assert.That(omitted, Does.Not.Contain("\"market\":"));
                SaveDataPresenceProbe omittedProbe =
                    JsonUtility.FromJson<SaveDataPresenceProbe>(omitted);
                SaveOperationResult malformed =
                    SaveDataValidator.ValidatePresence(omittedProbe, omitted);
                Assert.That(malformed.Succeeded, Is.False);
                Assert.That(malformed.Message, Does.Contain("market"));

                SaveData missing = session.CaptureSave();
                missing.market = null;
                Assert.That(SaveDataValidator.Validate(missing).Succeeded, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        [Test]
        public void CurrentVersionMigration_PreservesTheMarketBlock()
        {
            GameSession session = GameSession.NewGame();
            try
            {
                SaveData captured = session.CaptureSave();
                captured.market.foodInventory = 7;
                captured.market.foodPriceIndexPermille = 1200;
                captured.market.materialsPriceIndexPermille = 950;
                captured.market.consecutiveFoodShortageMonths = 2;

                Assert.That(SaveDataMigrator.TryMigrate(captured, null, out SaveData current,
                    out SaveOperationResult result), Is.True, result.Message);

                Assert.That(current.saveFormatVersion,
                    Is.EqualTo(SaveDataValidator.CurrentVersion));
                Assert.That(current.market.foodInventory, Is.EqualTo(7));
                Assert.That(current.market.foodPriceIndexPermille, Is.EqualTo(1200));
                Assert.That(current.market.materialsPriceIndexPermille, Is.EqualTo(950));
                Assert.That(current.market.consecutiveFoodShortageMonths, Is.EqualTo(2));
                Assert.That(current.market.lastSettledMonthIndex,
                    Is.EqualTo(captured.market.lastSettledMonthIndex));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(session.gameObject);
            }
        }

        /// <summary>
        /// Builds the exact v7 shape a real file has: every block v7 owned, with the
        /// development payload intact and no research root property at all.
        /// </summary>
        private static string LegitimateV7Json(SaveData data)
        {
            // Serialize the whole object so Unity emits the real array/object shape,
            // then drop the trailing research property that did not exist in v7.
            string json = JsonUtility.ToJson(data, true);
            int research = json.LastIndexOf("\"research\":", StringComparison.Ordinal);
            Assert.That(research, Is.GreaterThan(0));
            string prefix = json.Substring(0, research).TrimEnd();
            Assert.That(prefix.EndsWith(","), Is.True);
            return prefix.Substring(0, prefix.Length - 1) + "\n}";
        }
    }
}
