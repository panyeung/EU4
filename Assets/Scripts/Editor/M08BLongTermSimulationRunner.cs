using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SengokuChronicle.Application;
using SengokuChronicle.Domain;
using SengokuChronicle.Events;
using SengokuChronicle.Persistence;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SengokuChronicle.Editor
{
    /// <summary>Why a long-term seed stopped before or at its requested horizon.</summary>
    public enum M08BTerminationReason
    {
        Completed = 0,
        Extinction = 1,
        PendingSuccession = 2,
        UnresolvedBlockingEvent = 3,
        Timeout = 4,
        DateOverflow = 5,
        /// <summary>An unexpected exception escaped the simulator; never a rule stop and never reusable.</summary>
        SimulatorFault = 6
    }

    /// <summary>
    /// Simulation-only industry policy. These policies never change player rules; they decide
    /// what the harness does at a management-stable point so two runs can be compared.
    /// </summary>
    public enum M08BIndustryPolicy
    {
        /// <summary>
        /// Accepted baseline: the harness never appoints an industry, so both posts stay vacant
        /// unless a real session rule fills them. <see cref="IndustryState.CreateInitial"/> leaves
        /// both posts vacant, so a fresh new game stays unmanaged for its whole run.
        /// </summary>
        Unmanaged = 0,

        /// <summary>
        /// Deterministic simulation-only comparison: keep Trading House vacant and fill a vacant
        /// Farm through the public session command, choosing the living family member with the
        /// lowest ordinal <see cref="PersonId.Value"/> that the real rules accept.
        /// </summary>
        FarmManagedV1 = 1
    }

    /// <summary>One GDD 14.2 checkpoint. Unreached checkpoints keep <see cref="Reached"/> false.</summary>
    public sealed class M08BCheckpointResult
    {
        public int Year;
        public int MonthsRequired;
        public bool Reached;
        public bool Alive;
        public int LivingMembers;
        /// <summary>Mean age of living family members in tenths of a year, half-up.</summary>
        public int MeanLivingAgeTenths;
        public int Funds;
        public int FoodInventory;
        public int Supplies;
        public int FoodPriceIndexPermille;
        public int MaterialsPriceIndexPermille;
        public int CompositePriceIndexPermille;
        /// <summary>Funds plus food and materials valued at this checkpoint's settled prices.</summary>
        public int Wealth;
        public bool HeirGap;
        public int EventCount;
        public int EventsPer1000Months;
        /// <summary>True only when the 12-month famine condition holds at this checkpoint.</summary>
        public bool FamineCollapse;
        /// <summary>True only when the 12-month industry-stall condition holds at this checkpoint.</summary>
        public bool IndustryCollapse;
        /// <summary><see cref="FamineCollapse"/> or <see cref="IndustryCollapse"/> at this checkpoint.</summary>
        public bool EconomicCollapse;
        /// <summary>True only when this checkpoint month is at least double its own year-ago index.</summary>
        public bool ExtremeInflation;
        /// <summary>
        /// GDD 14.2 家族关系异常: current-head to living-member relations held at the documented
        /// hostile bound (<see cref="FamilyRelationships.MinimumValue"/>).
        /// </summary>
        public int FamilyRelationshipAnomalies;
        /// <summary>Current-head to living-member relations inspected for the anomaly metric.</summary>
        public int FamilyRelationshipsSampled;
        /// <summary>Persisted relationship invariant violations in this checkpoint's snapshot.</summary>
        public int PersistedRelationshipViolations;
        /// <summary>Progression deadlocks recorded before this checkpoint.</summary>
        public int ProgressionDeadlocks;
    }

    /// <summary>Everything one real SC-013 seed produced, including its exact stop reason.</summary>
    public sealed class M08BSeedResult
    {
        public ulong Seed;
        public string SeedHex;
        public M08BTerminationReason Termination;
        /// <summary>Rule-level stop detail; never a replacement for the termination category.</summary>
        public string TerminationDetail;
        /// <summary>
        /// Simulation policy that produced this row. Rows from different policies are never
        /// comparable and never reusable for each other.
        /// </summary>
        public string PolicyId = M08BLongTermSimulationRunner.UnmanagedPolicyId;
        public int StartYear;
        public int StartMonth;
        public int EndYear;
        public int EndMonth;
        public int MonthsSimulated;
        public string InitialRandomStateHex;
        public int EventsTriggered;
        public int EventDeadlocks;
        public bool FamineCollapse;
        public int LongestFamineMonths;
        public bool IndustryCollapse;
        public int LongestIndustryStallMonths;
        public bool EconomicCollapse;
        public bool ExtremeInflation;
        public int ExtremeInflationMonths;
        /// <summary>
        /// Peak <see cref="M08BCheckpointResult.FamilyRelationshipAnomalies"/> seen in any sample
        /// (checkpoints and final state). A peak, not a sum: one unchanged anomaly must not count
        /// once per sample.
        /// </summary>
        public int PeakFamilyRelationshipAnomalies;
        /// <summary>Family relationship anomalies observed in the final state.</summary>
        public int FinalFamilyRelationshipAnomalies;
        /// <summary>Peak number of head-to-living-member relations inspected in any sample.</summary>
        public int FamilyRelationshipsSampled;
        /// <summary>Persisted relationship invariant violations in the final state (integrity guard).</summary>
        public int PersistedRelationshipViolations;
        /// <summary>
        /// World-progression deadlocks that stopped the run: an unresolved blocking event that
        /// still prevents the calendar advancing. A rule-required pending succession keeps its
        /// own stop category and is not a deadlock.
        /// </summary>
        public int ProgressionDeadlocks;
        public int MaxFoodPriceIndexPermille;
        public int MinFoodPriceIndexPermille = MarketState.MinimumPriceIndexPermille;
        public int MaxMaterialsPriceIndexPermille;
        public int MinMaterialsPriceIndexPermille = MarketState.MinimumPriceIndexPermille;
        public int MaxCompositePriceIndexPermille;
        public int FinalFunds;
        public int FinalFoodInventory;
        public int FinalSupplies;
        public int FinalLivingMembers;
        /// <summary>Successful Farm appointments the active policy made during the run.</summary>
        public int FarmAppointments;
        /// <summary>
        /// Management-stable points where the policy found the Farm post vacant, whether or not
        /// a candidate was accepted. It is the opportunity count behind <see cref="FarmAppointments"/>.
        /// </summary>
        public int FarmVacancies;
        /// <summary>Farm assignee at the final state, or empty when the post ended vacant.</summary>
        public string FinalFarmAssignee = string.Empty;
        /// <summary>
        /// True when the real succession phase ended the family, including an extinction reached
        /// while a succession decision was resolving. Extinct families keep their roster rows, so
        /// this is the only truthful way to separate them from a live stopped family.
        /// </summary>
        public bool FinalFamilyExtinct;
        public bool FinalHeirGap;
        public List<M08BCheckpointResult> Checkpoints = new List<M08BCheckpointResult>();
        /// <summary>Stable event/option decisions in resolution order; diagnostics and tests only.</summary>
        public List<string> EventChoices = new List<string>();
        /// <summary>Stable succession decisions in resolution order; diagnostics and tests only.</summary>
        public List<string> SuccessionDecisions = new List<string>();
        /// <summary>Stable industry decisions in resolution order; diagnostics and tests only.</summary>
        public List<string> IndustryDecisions = new List<string>();
        /// <summary>Wall-clock cost for the batch log; never part of normalized artifacts.</summary>
        public long DurationMilliseconds;
    }

    /// <summary>
    /// Real-rule long-term simulation for GDD 14.2. Every seed starts a fresh SC-013
    /// <see cref="GameSession"/> through the public new-game entry and advances the same
    /// month transaction a player uses. The only automated policies are the approved stable
    /// option-ID event resolver, the approved simulation-only succession policy (designated
    /// heir, else lowest stable PersonId, with a stable-ID regent), and the optional
    /// simulation-only <see cref="M08BIndustryPolicy.FarmManagedV1"/> comparison policy;
    /// extinction, a crisis adoption the player alone may accept, and every other pause keep
    /// their own explicit stop category and are never discarded.
    /// </summary>
    public static class M08BLongTermSimulationRunner
    {
        public const int ShortBatchYears = 5;
        public const int FullBatchYears = 100;
        public const int MaxConsecutiveBlockedAttempts = 3;
        public const int MaxResolverIterationsPerTurn = 4096;
        /// <summary>Bound on succession commands per pending decision; each step changes state.</summary>
        public const int MaxSuccessionStepsPerStop = 16;
        /// <summary>Approved collapse run: 12 consecutive failing months.</summary>
        public const int CollapseConsecutiveMonths = 12;

        private const string OutputRootRelativePath = "Logs/verify/m08b";
        private const string SourceRelativePath = "Assets/Scripts/Editor/M08BLongTermSimulationRunner.cs";
        private const string NormalizedSchema = "m08b.long-term-simulation/v6";
        private const string RuleVersion = "M08B-long-term-v4";
        private const string NoSeedRowsError = "CSV has no seed rows.";
        private const string RuleInputManifestHeader = "m08b-rule-inputs/v1";

        /// <summary>Policy ID of the accepted unmanaged baseline.</summary>
        public const string UnmanagedPolicyId = "unmanaged";
        /// <summary>Policy ID of the simulation-only vacant-Farm comparison.</summary>
        public const string FarmManagedV1PolicyId = "farm-managed-v1";

        /// <summary>
        /// Directories swept into the rule-input manifest, in dependency order. They hold the
        /// gameplay, settlement, event and persistence code the real month transaction runs
        /// plus the event data it loads. A newly added file in one of them joins the manifest
        /// automatically, so reused rows can never outlive a new or changed rule input. Editor
        /// tooling, presentation and Library/Temp are deliberately excluded because they cannot
        /// change a simulated outcome.
        /// </summary>
        private static readonly string[] RuleInputDirectoryRoots =
        {
            "Assets/Scripts/Application",
            "Assets/Scripts/Domain",
            "Assets/Scripts/Events",
            "Assets/Scripts/Persistence",
            "Assets/Resources/Events",
            "Assets/Resources/Localization"
        };

        /// <summary>Extensions swept inside each rule-input directory.</summary>
        private static readonly string[] RuleInputFileExtensions = { ".cs", ".json" };

        /// <summary>Tracked rule inputs the directory sweep cannot reach: the runner source.</summary>
        private static readonly string[] RuleInputStandaloneFiles =
        {
            "Assets/Scripts/Editor/M08BLongTermSimulationRunner.cs"
        };

        private static readonly int[] DefaultCheckpointYears = { 10, 25, 50, 100 };

        /// <summary>
        /// Legacy artifact root: Logs/verify/m08b under the project root. It holds the accepted
        /// schema-v5 artifacts, so it is only the parent of <see cref="PolicyOutputRoot"/> and
        /// never a destination for a new run.
        /// </summary>
        public static string DefaultOutputRoot =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", OutputRootRelativePath));

        /// <summary>
        /// Policy-specific artifact directory below <see cref="DefaultOutputRoot"/>: the destination
        /// of every new run, including the no-policy defaults. Each policy owns its own files, so no
        /// run can rewrite the accepted historical artifacts at the legacy root.
        /// </summary>
        public static string PolicyOutputRoot(M08BIndustryPolicy policy) =>
            Path.Combine(DefaultOutputRoot, PolicyIdOf(policy));

        /// <summary>
        /// Artifact root a run writes to: a blank root resolves to the policy directory, so no
        /// default run can target the historical root. An explicit root is kept as given unless its
        /// normalized full path is <see cref="DefaultOutputRoot"/> itself, which is rejected so no
        /// caller can rewrite the accepted historical artifacts. Directories below the historical
        /// root stay usable.
        /// </summary>
        public static string ResolveArtifactRoot(string outputRoot, M08BIndustryPolicy policy)
        {
            if (string.IsNullOrWhiteSpace(outputRoot))
                return PolicyOutputRoot(policy);
            if (IsLegacyOutputRoot(outputRoot))
                throw new ArgumentException(
                    "Refusing to write simulation artifacts to the historical root " +
                    DefaultOutputRoot + "; pass a policy directory or another explicit root.",
                    nameof(outputRoot));
            return outputRoot;
        }

        /// <summary>
        /// True when <paramref name="outputRoot"/> is the historical artifact root itself. The
        /// comparison uses normalized full paths with trailing separators trimmed and ordinal
        /// case-insensitive matching, so alternate spellings of the legacy root are rejected too.
        /// </summary>
        private static bool IsLegacyOutputRoot(string outputRoot) =>
            string.Equals(NormalizeRootPath(outputRoot), NormalizeRootPath(DefaultOutputRoot),
                StringComparison.OrdinalIgnoreCase);

        private static string NormalizeRootPath(string root) =>
            Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        /// <summary>Stable artifact and provenance label for one policy.</summary>
        public static string PolicyIdOf(M08BIndustryPolicy policy) =>
            policy == M08BIndustryPolicy.FarmManagedV1 ? FarmManagedV1PolicyId : UnmanagedPolicyId;

        /// <summary>True only for a documented policy ID.</summary>
        public static bool TryParsePolicyId(string policyId, out M08BIndustryPolicy policy)
        {
            if (string.Equals(policyId, FarmManagedV1PolicyId, StringComparison.Ordinal))
            {
                policy = M08BIndustryPolicy.FarmManagedV1;
                return true;
            }
            if (string.Equals(policyId, UnmanagedPolicyId, StringComparison.Ordinal))
            {
                policy = M08BIndustryPolicy.Unmanaged;
                return true;
            }
            policy = M08BIndustryPolicy.Unmanaged;
            return false;
        }

        public static IReadOnlyList<int> ApprovedCheckpointYears =>
            Array.AsReadOnly(DefaultCheckpointYears);

        /// <summary>The exact tracked rule inputs hashed for reuse provenance.</summary>
        public static IReadOnlyList<string> RuleInputRelativePaths =>
            BuildRuleInputPaths(ProjectRoot);

        /// <summary>
        /// Deterministic dependency manifest: the runner source plus every swept rule-input
        /// file, in ordinal order. Directories that do not exist under
        /// <paramref name="projectRoot"/> contribute nothing, which lets tests build a small
        /// synthetic project while the real project is additionally guarded by
        /// <see cref="TryBuildProjectRuleInputPaths"/>.
        /// </summary>
        public static IReadOnlyList<string> BuildRuleInputPaths(string projectRoot)
        {
            var paths = new List<string>();
            if (string.IsNullOrEmpty(projectRoot))
                return paths.AsReadOnly();
            string root = Path.GetFullPath(projectRoot);
            var seen = new HashSet<string>(StringComparer.Ordinal);

            foreach (string relative in RuleInputStandaloneFiles)
            {
                string normalized = NormalizeRelativePath(relative);
                if (normalized.Length == 0)
                    continue;
                if (File.Exists(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar))))
                    AddRuleInputPath(paths, seen, normalized);
            }

            foreach (string directoryRelative in RuleInputDirectoryRoots)
            {
                string normalizedRoot = NormalizeRelativePath(directoryRelative);
                if (normalizedRoot.Length == 0)
                    continue;
                string directory = Path.Combine(root,
                    normalizedRoot.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(directory))
                    continue;
                foreach (string file in Directory.GetFiles(directory, "*", SearchOption.AllDirectories))
                {
                    if (!IsRuleInputFile(file))
                        continue;
                    string normalized = NormalizeRelativePath(
                        Path.GetRelativePath(root, file).Replace('\\', '/'));
                    if (normalized.Length > 0)
                        AddRuleInputPath(paths, seen, normalized);
                }
            }

            paths.Sort(StringComparer.Ordinal);
            return paths.AsReadOnly();
        }

        private static void AddRuleInputPath(List<string> paths, HashSet<string> seen, string path)
        {
            if (seen.Add(path))
                paths.Add(path);
        }

        private static bool IsRuleInputFile(string path)
        {
            string extension = Path.GetExtension(path);
            foreach (string allowed in RuleInputFileExtensions)
                if (string.Equals(extension, allowed, StringComparison.OrdinalIgnoreCase))
                    return true;
            return false;
        }

        /// <summary>
        /// Builds the real project manifest and refuses to weaken it: every declared root must
        /// exist and contribute at least one file, and the runner source must exist. Any gap
        /// means the dependency set cannot be proven, which rejects reuse.
        /// </summary>
        private static bool TryBuildProjectRuleInputPaths(out IReadOnlyList<string> paths)
        {
            paths = Array.Empty<string>();
            string root = ProjectRoot;
            foreach (string directoryRelative in RuleInputDirectoryRoots)
            {
                string normalized = NormalizeRelativePath(directoryRelative);
                if (normalized.Length == 0 || !Directory.Exists(Path.Combine(root,
                        normalized.Replace('/', Path.DirectorySeparatorChar))))
                    return false;
            }
            foreach (string relative in RuleInputStandaloneFiles)
            {
                string normalized = NormalizeRelativePath(relative);
                if (normalized.Length == 0 || !File.Exists(Path.Combine(root,
                        normalized.Replace('/', Path.DirectorySeparatorChar))))
                    return false;
            }

            IReadOnlyList<string> built = BuildRuleInputPaths(root);
            foreach (string directoryRelative in RuleInputDirectoryRoots)
            {
                string prefix = NormalizeRelativePath(directoryRelative) + "/";
                bool contributed = false;
                foreach (string path in built)
                {
                    if (path.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        contributed = true;
                        break;
                    }
                }
                if (!contributed)
                    return false;
            }
            if (built.Count == 0)
                return false;
            paths = built;
            return true;
        }

        /// <summary>SHA-256 of the runner source alone; reported separately from the rule inputs.</summary>
        public static string ComputeSourceSha256()
        {
            try
            {
                string path = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,
                    "..", SourceRelativePath));
                if (!File.Exists(path))
                    return string.Empty;
                using (SHA256 sha = SHA256.Create())
                using (FileStream stream = File.OpenRead(path))
                    return ToHex(sha.ComputeHash(stream));
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// SHA-256 over the whole rule-input dependency set. Any missing input, or a manifest
        /// that cannot be proven complete, returns an empty hash, which rejects reuse instead
        /// of trusting an unprovable artifact.
        /// </summary>
        public static string ComputeRuleInputSha256() =>
            TryComputeRuleInputProvenance(out string sha256, out _) ? sha256 : string.Empty;

        /// <summary>
        /// Hash plus file count of the real project manifest, or empty values when the manifest
        /// cannot be proven complete.
        /// </summary>
        private static bool TryComputeRuleInputProvenance(out string sha256, out int fileCount)
        {
            sha256 = string.Empty;
            fileCount = 0;
            if (!TryBuildProjectRuleInputPaths(out IReadOnlyList<string> paths))
                return false;
            sha256 = ComputeRuleInputSha256(ProjectRoot, paths);
            if (sha256.Length != 64)
                return false;
            fileCount = paths.Count;
            return true;
        }

        /// <summary>
        /// Deterministic dependency-set hash: the manifest header and every normalized relative
        /// path plus byte length and bytes are hashed in the given order. Exposed so tests can
        /// prove that changing one rule input invalidates the set while the runner stays fixed.
        /// </summary>
        public static string ComputeRuleInputSha256(string projectRoot,
            IReadOnlyList<string> relativePaths)
        {
            if (string.IsNullOrEmpty(projectRoot) || relativePaths == null ||
                relativePaths.Count == 0)
                return string.Empty;
            try
            {
                using (var manifest = new MemoryStream())
                {
                    byte[] header = Encoding.UTF8.GetBytes(RuleInputManifestHeader + "\n" +
                        relativePaths.Count.ToString(CultureInfo.InvariantCulture) + "\n");
                    manifest.Write(header, 0, header.Length);
                    foreach (string relativePath in relativePaths)
                    {
                        string normalized = NormalizeRelativePath(relativePath);
                        if (normalized.Length == 0)
                            return string.Empty;
                        string fullPath = Path.GetFullPath(Path.Combine(projectRoot,
                            normalized.Replace('/', Path.DirectorySeparatorChar)));
                        if (!File.Exists(fullPath))
                            return string.Empty;
                        byte[] bytes = File.ReadAllBytes(fullPath);
                        byte[] entry = Encoding.UTF8.GetBytes(normalized + "\n" +
                            bytes.Length.ToString(CultureInfo.InvariantCulture) + "\n");
                        manifest.Write(entry, 0, entry.Length);
                        manifest.Write(bytes, 0, bytes.Length);
                    }

                    using (SHA256 sha = SHA256.Create())
                        return ToHex(sha.ComputeHash(manifest.ToArray()));
                }
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static string ProjectRoot =>
            Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));

        /// <summary>Forward-slash relative path inside the project; empty when the path escapes it.</summary>
        private static string NormalizeRelativePath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;
            string normalized = relativePath.Replace('\\', '/').Trim();
            while (normalized.StartsWith("./", StringComparison.Ordinal))
                normalized = normalized.Substring(2);
            normalized = normalized.TrimStart('/');
            if (normalized.Length == 0 || normalized.Contains("..") ||
                Path.IsPathRooted(normalized) || normalized.Contains(":"))
                return string.Empty;
            return normalized;
        }

        private static string ToHex(byte[] hash)
        {
            var text = new StringBuilder(hash.Length * 2);
            foreach (byte value in hash)
                text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return text.ToString();
        }

        /// <summary>Runs one seed for up to <paramref name="years"/> simulated years.</summary>
        public static M08BSeedResult RunSeed(ulong seed, int years) =>
            RunSeed(seed, years, DefaultCheckpointYears, M08BIndustryPolicy.Unmanaged);

        /// <summary>Runs one unmanaged seed with an explicit checkpoint list.</summary>
        public static M08BSeedResult RunSeed(ulong seed, int years, IReadOnlyList<int> checkpointYears) =>
            RunSeed(seed, years, checkpointYears, M08BIndustryPolicy.Unmanaged);

        /// <summary>
        /// Runs one seed with the requested simulation policy in a throwaway preview scene, so the
        /// user's active scene and its dirty flag stay untouched. The scene is closed in
        /// <c>finally</c>, which destroys the holder. Tests pass a one-year checkpoint so small
        /// real runs still exercise the checkpoint path.
        /// </summary>
        public static M08BSeedResult RunSeed(ulong seed, int years, IReadOnlyList<int> checkpointYears,
            M08BIndustryPolicy policy)
        {
            Scene scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var holder = new GameObject("M08BLongTermSimulation");
                SceneManager.MoveGameObjectToScene(holder, scene);
                GameSession session = holder.AddComponent<GameSession>();
                session.InitNewGame(seed);
                return RunExistingSession(session, seed, years, checkpointYears, policy);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>
        /// Runs an already initialized session without destroying it. The caller keeps
        /// ownership so tests can construct a real session and stop it at a rule-pending
        /// state before invocation.
        /// </summary>
        public static M08BSeedResult RunExistingSession(GameSession session, ulong seed, int years) =>
            RunExistingSession(session, seed, years, DefaultCheckpointYears, M08BIndustryPolicy.Unmanaged);

        public static M08BSeedResult RunExistingSession(GameSession session, ulong seed, int years,
            IReadOnlyList<int> checkpointYears) =>
            RunExistingSession(session, seed, years, checkpointYears, M08BIndustryPolicy.Unmanaged);

        public static M08BSeedResult RunExistingSession(GameSession session, ulong seed, int years,
            IReadOnlyList<int> checkpointYears, M08BIndustryPolicy policy)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (session.WorldClock == null)
                throw new ArgumentException("The session has no initialized world clock.", nameof(session));
            if (years <= 0)
                throw new ArgumentOutOfRangeException(nameof(years), "years must be positive.");
            if (years > 400)
                throw new ArgumentOutOfRangeException(nameof(years),
                    "years exceeds the supported 1560-2000 calendar span.");

            List<int> checkpoints = NormalizeCheckpointYears(checkpointYears);
            int targetMonths = checked(years * 12);
            GameDate start = session.WorldClock.CurrentDate;

            var result = new M08BSeedResult
            {
                Seed = seed,
                SeedHex = FormatSeedHex(seed),
                PolicyId = PolicyIdOf(policy),
                StartYear = start.Year,
                StartMonth = start.Month,
                EndYear = start.Year,
                EndMonth = start.Month,
                InitialRandomStateHex = SafeInitialRandomStateHex(session),
                MaxFoodPriceIndexPermille = session.Market.FoodPriceIndexPermille,
                MinFoodPriceIndexPermille = session.Market.FoodPriceIndexPermille,
                MaxMaterialsPriceIndexPermille = session.Market.MaterialsPriceIndexPermille,
                MinMaterialsPriceIndexPermille = session.Market.MaterialsPriceIndexPermille,
                MaxCompositePriceIndexPermille = session.Market.CompositePriceIndexPermille
            };
            foreach (int year in checkpoints)
                result.Checkpoints.Add(new M08BCheckpointResult
                {
                    Year = year,
                    MonthsRequired = checked(year * 12)
                });

            int consecutiveBlocked = 0;
            int currentFamineRun = 0;
            int currentStallRun = 0;

            while (true)
            {
                if (result.MonthsSimulated >= targetMonths)
                {
                    result.Termination = M08BTerminationReason.Completed;
                    result.TerminationDetail = "reached requested horizon of " +
                        years.ToString(CultureInfo.InvariantCulture) + " years";
                    break;
                }

                if (session.IsFamilyExtinct)
                {
                    result.Termination = M08BTerminationReason.Extinction;
                    result.TerminationDetail = "family phase is extinct before the next month";
                    break;
                }

                // A queued event is resolved before the head can change. The month transaction
                // schedules next month's event before its annual death settlement, so a head
                // death can leave both a pending event and a pending succession at the same
                // stable point. The session's own play order lets the player choose the event
                // first, while the outgoing head is still current, so its family effects still
                // address a real (head, member) pair. Resolving succession first would hand the
                // event to the incoming head and reject every option that targets that person.
                if (!TryResolvePendingEvents(session, result.EventChoices, out string eventFailure))
                {
                    result.EventDeadlocks++;
                    result.ProgressionDeadlocks++;
                    result.Termination = M08BTerminationReason.UnresolvedBlockingEvent;
                    result.TerminationDetail = eventFailure;
                    break;
                }

                if (session.Succession.IsPending ||
                    session.Succession.IsRegencyHandoffDue(session.WorldClock.CurrentDate))
                {
                    if (!TryResolveSuccession(session, result.SuccessionDecisions,
                            out string successionFailure))
                    {
                        // The death settlement may have ended the family while the decision was
                        // resolving. That is an extinction, not a decision the harness must make.
                        if (session.IsFamilyExtinct)
                        {
                            result.Termination = M08BTerminationReason.Extinction;
                            result.TerminationDetail =
                                "family became extinct during succession resolution: " +
                                successionFailure;
                            break;
                        }

                        // No lawful candidate, a crisis adoption, or a rejected command: keep the
                        // explicit stop instead of inventing a continuation. A pending succession
                        // is a rule-required decision, not a world-progression deadlock.
                        result.Termination = M08BTerminationReason.PendingSuccession;
                        result.TerminationDetail = successionFailure;
                        break;
                    }
                }

                // Management-stable point: pending events and any succession decision are resolved
                // and the next month has not started, which is exactly when a player may change
                // industry appointments. The unmanaged policy does nothing here, so the accepted
                // baseline stays byte-identical.
                if (policy == M08BIndustryPolicy.FarmManagedV1)
                    ApplyFarmManagement(session, result);

                if (!session.TryAdvanceToNextMonth())
                {
                    MonthTurnOutcome outcome = session.LastMonthTurnOutcome;
                    string detail = outcome + ": " + session.LastEventMessage;
                    bool extinct = session.IsFamilyExtinct;
                    bool pending = outcome == MonthTurnOutcome.PendingSuccession ||
                                   session.Succession.IsPending ||
                                   session.Succession.IsRegencyHandoffDue(session.WorldClock.CurrentDate);
                    bool unresolvedEvent = outcome == MonthTurnOutcome.PendingEvent;
                    bool dateOverflow = outcome == MonthTurnOutcome.DateOutOfRange;
                    if (!extinct && !pending && !unresolvedEvent && !dateOverflow)
                        consecutiveBlocked++;
                    if (!extinct && !pending && !unresolvedEvent && !dateOverflow &&
                        consecutiveBlocked < MaxConsecutiveBlockedAttempts)
                        continue;

                    M08BTerminationReason reason = ClassifyStop(false, extinct, pending,
                        unresolvedEvent, dateOverflow, consecutiveBlocked);
                    result.Termination = reason;
                    switch (reason)
                    {
                        case M08BTerminationReason.Extinction:
                            result.TerminationDetail = "month turn reported extinction: " + detail;
                            break;
                        case M08BTerminationReason.PendingSuccession:
                            result.TerminationDetail = detail +
                                " (rule-required succession decision; not a progression deadlock)";
                            break;
                        case M08BTerminationReason.DateOverflow:
                            result.TerminationDetail = detail;
                            break;
                        case M08BTerminationReason.UnresolvedBlockingEvent:
                            result.EventDeadlocks++;
                            result.ProgressionDeadlocks++;
                            result.TerminationDetail =
                                "month still reports a pending event after the resolver: " + detail;
                            break;
                        case M08BTerminationReason.Timeout:
                            result.TerminationDetail = "advance refused " +
                                consecutiveBlocked.ToString(CultureInfo.InvariantCulture) +
                                " consecutive times without date progress: " + detail;
                            break;
                    }
                    break;
                }

                consecutiveBlocked = 0;
                result.MonthsSimulated++;
                UpdateMonthlyMetrics(session, result, ref currentFamineRun, ref currentStallRun);
                CaptureDueCheckpoints(session, result, currentFamineRun, currentStallRun);
            }

            CaptureFinalState(session, result);
            return result;
        }

        /// <summary>
        /// Documented deterministic event policy: process every queued event, choosing the
        /// available option with the smallest ordinal option ID. Failure to choose while an
        /// event still blocks progression is a deadlock, never a skipped seed.
        /// </summary>
        public static bool TryResolvePendingEvents(GameSession session, IList<string> resolvedChoices,
            out string failure)
        {
            if (session == null)
            {
                failure = "session is null";
                return false;
            }

            int iterations = 0;
            while (!string.IsNullOrEmpty(session.PendingEventId))
            {
                if (++iterations > MaxResolverIterationsPerTurn)
                {
                    failure = "event queue did not drain after " +
                        MaxResolverIterationsPerTurn.ToString(CultureInfo.InvariantCulture) +
                        " stable-order choices; pending event " + session.PendingEventId;
                    return false;
                }

                string eventId = session.PendingEventId;
                if (!session.EventCatalogReady || session.EventCatalog == null ||
                    session.EventCatalog.events == null)
                {
                    failure = "event catalog is not ready; pending event " + eventId;
                    return false;
                }

                EventDefinitionData definition = null;
                foreach (EventDefinitionData candidate in session.EventCatalog.events)
                {
                    if (string.Equals(candidate.eventId, eventId, StringComparison.Ordinal))
                    {
                        definition = candidate;
                        break;
                    }
                }
                if (definition == null || definition.options == null || definition.options.Length == 0)
                {
                    failure = "event " + eventId + " has no selectable options";
                    return false;
                }

                EventOptionData[] ordered = definition.options
                    .OrderBy(option => option.optionId ?? string.Empty, StringComparer.Ordinal)
                    .ToArray();

                bool committed = false;
                string lastFailure = string.Empty;
                foreach (EventOptionData option in ordered)
                {
                    if (!session.TryGetEventOptionAvailability(eventId, option.optionId,
                            out bool enabled, out string availabilityReason))
                    {
                        lastFailure = availabilityReason;
                        continue;
                    }
                    if (!enabled)
                    {
                        lastFailure = availabilityReason;
                        continue;
                    }

                    string resultId = session.CreateEventResultId(eventId, option.optionId);
                    EventChoiceResult choice = session.TryChooseEventOption(eventId, option.optionId, resultId);
                    if (!choice.Succeeded)
                    {
                        lastFailure = choice.Message;
                        continue;
                    }

                    resolvedChoices?.Add(eventId + "=" + option.optionId);
                    committed = true;
                    break;
                }

                if (!committed)
                {
                    failure = "no available option could be committed for pending event " + eventId +
                        (string.IsNullOrEmpty(lastFailure) ? string.Empty : ": " + lastFailure);
                    return false;
                }
            }

            failure = string.Empty;
            return true;
        }

        /// <summary>
        /// Approved simulation-only succession policy: prefer the valid designated heir, else the
        /// eligible candidate with the lowest stable PersonId; pick the regent by the same stable
        /// ID rule; complete a due regency through the public session command. It never accepts a
        /// crisis adoption and never chooses when no lawful candidate exists, so those states keep
        /// the existing stop classification. Player-facing succession rules are unchanged.
        /// </summary>
        public static bool TryResolveSuccession(GameSession session, IList<string> decisions,
            out string failure)
        {
            failure = string.Empty;
            if (session == null)
            {
                failure = "session is null";
                return false;
            }

            int steps = 0;
            while (true)
            {
                if (++steps > MaxSuccessionStepsPerStop)
                {
                    failure = "succession did not settle after " +
                        MaxSuccessionStepsPerStop.ToString(CultureInfo.InvariantCulture) + " commands";
                    return false;
                }

                SuccessionState state = session.Succession;
                if (session.IsFamilyExtinct)
                {
                    failure = "family became extinct while resolving succession";
                    return false;
                }

                if (state.Phase == SuccessionPhase.Regency)
                {
                    if (!state.IsRegencyHandoffDue(session.WorldClock.CurrentDate))
                        return true;
                    SuccessionResult regency = session.TryCompleteSuccessionRegency();
                    if (!regency.Succeeded)
                    {
                        failure = "regency completion refused: " + regency.Message;
                        return false;
                    }
                    decisions?.Add("regency.complete=" + state.HeadPersonId.Value);
                    continue;
                }

                if (state.Phase == SuccessionPhase.CrisisDecision)
                {
                    failure = "no lawful successor: succession phase CrisisDecision offers a crisis " +
                        "adoption that only the player may accept";
                    return false;
                }

                if (state.Phase == SuccessionPhase.AwaitingRegentChoice &&
                    !state.PendingSuccessorId.IsEmpty)
                {
                    IReadOnlyList<SuccessionCandidate> regents = session.SuccessionRegentCandidates;
                    if (!TryChooseLowestPersonId(regents, out SuccessionCandidate regent))
                    {
                        failure = "no eligible regent for minor successor " +
                            state.PendingSuccessorId.Value;
                        return false;
                    }
                    SuccessionResult selected = session.SelectSuccessionRegent(regent.PersonId);
                    if (!selected.Succeeded)
                    {
                        failure = "regent selection refused: " + selected.Message;
                        return false;
                    }
                    decisions?.Add("regent=" + regent.PersonId.Value);
                    SuccessionResult confirmed = session.ConfirmSuccession();
                    if (!confirmed.Succeeded)
                    {
                        failure = "succession confirmation refused: " + confirmed.Message;
                        return false;
                    }
                    decisions?.Add("succession.confirm=" + regent.PersonId.Value);
                    continue;
                }

                if (state.Phase == SuccessionPhase.AwaitingSuccessionChoice ||
                    state.Phase == SuccessionPhase.AwaitingRegentChoice)
                {
                    if (!state.PendingSuccessorId.IsEmpty)
                    {
                        SuccessionResult confirmed = session.ConfirmSuccession();
                        if (!confirmed.Succeeded)
                        {
                            failure = "succession confirmation refused: " + confirmed.Message;
                            return false;
                        }
                        decisions?.Add("succession.confirm=" + state.PendingSuccessorId.Value);
                        continue;
                    }

                    IReadOnlyList<SuccessionCandidate> candidates = session.SuccessionCandidates;
                    if (!TryChooseSuccessor(candidates, state.DesignatedHeirId,
                            out SuccessionCandidate chosen))
                    {
                        failure = "no eligible succession candidate; a crisis adoption is a player " +
                            "decision the simulation must not make";
                        return false;
                    }
                    SuccessionResult selected = session.SelectSuccessionSuccessor(chosen.PersonId);
                    if (!selected.Succeeded)
                    {
                        failure = "successor selection refused: " + selected.Message;
                        return false;
                    }
                    decisions?.Add("successor=" + chosen.PersonId.Value +
                        (chosen.PersonId == state.DesignatedHeirId ? " (designated)" : " (lowest-id)"));
                    continue;
                }

                return true;
            }
        }

        /// <summary>
        /// Designated heir when it is still a valid candidate, otherwise the candidate with the
        /// lowest stable PersonId. Order-independent: the input sequence never decides the pick.
        /// </summary>
        public static bool TryChooseSuccessor(IReadOnlyList<SuccessionCandidate> candidates,
            PersonId designatedHeirId, out SuccessionCandidate chosen)
        {
            chosen = default;
            if (candidates == null || candidates.Count == 0)
                return false;
            if (!designatedHeirId.IsEmpty)
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (candidates[i].PersonId == designatedHeirId)
                    {
                        chosen = candidates[i];
                        return true;
                    }
                }
            }
            return TryChooseLowestPersonId(candidates, out chosen);
        }

        private static bool TryChooseLowestPersonId(IReadOnlyList<SuccessionCandidate> candidates,
            out SuccessionCandidate lowest)
        {
            lowest = default;
            bool found = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                SuccessionCandidate candidate = candidates[i];
                if (!found || string.CompareOrdinal(candidate.PersonId.Value ?? string.Empty,
                        lowest.PersonId.Value ?? string.Empty) < 0)
                {
                    lowest = candidate;
                    found = true;
                }
            }
            return found;
        }

        /// <summary>
        /// farm-managed-v1, the simulation-only comparison policy. At a management-stable point
        /// it inspects the Farm post: an occupied post is never replaced or unassigned, and a
        /// vacant one is filled through <see cref="GameSession.TryAssignIndustry"/> by trying the
        /// living family members in ordinal <see cref="PersonId.Value"/> order until one
        /// appointment succeeds. When no member is accepted the post stays vacant. Trading House
        /// is never touched, and no random state is read or advanced.
        /// </summary>
        /// <param name="appointments">
        /// Receives one <c>farm=&lt;personId&gt;</c> entry per accepted appointment, in order.
        /// </param>
        /// <param name="farmWasVacant">
        /// True only when the policy inspected the Farm post and found it vacant, so a caller can
        /// report the opportunity count behind the accepted appointments.
        /// </param>
        /// <returns>True only when this call itself appointed someone to the Farm.</returns>
        public static bool TryApplyFarmManagement(GameSession session, IList<string> appointments,
            out bool farmWasVacant, out string detail)
        {
            farmWasVacant = false;
            detail = string.Empty;
            if (session == null)
            {
                detail = "session is null";
                return false;
            }
            if (!session.CanEditIndustryAssignments)
            {
                detail = "not a management-stable point: " + session.IndustryBlockReason;
                return false;
            }
            if (!TryGetIndustryAssignee(session, IndustryCatalog.FarmId, out PersonId current))
            {
                detail = "the industry view has no " + IndustryCatalog.FarmId + " slot";
                return false;
            }
            if (!current.IsEmpty)
            {
                detail = "Farm keeps its valid assignee " + (current.Value ?? string.Empty);
                return false;
            }

            farmWasVacant = true;
            List<PersonId> candidates = LivingFamilyMembersByOrdinalId(session.Succession);
            string lastRefusal = string.Empty;
            foreach (PersonId candidate in candidates)
            {
                IndustryAssignmentResult assignment =
                    session.TryAssignIndustry(IndustryCatalog.FarmId, candidate);
                if (!assignment.Succeeded)
                {
                    lastRefusal = assignment.Outcome + ": " + assignment.Message;
                    continue;
                }

                appointments?.Add("farm=" + (candidate.Value ?? string.Empty));
                detail = "Farm assigned to " + (candidate.Value ?? string.Empty);
                return true;
            }

            detail = "Farm left vacant: no living family member was accepted" +
                (string.IsNullOrEmpty(lastRefusal) ? string.Empty : " (" + lastRefusal + ")");
            return false;
        }

        /// <summary>
        /// Adapter that applies farm-managed-v1 to one seed row and records its own counts. The
        /// unmanaged policy never calls it, so the accepted baseline is unaffected.
        /// </summary>
        private static void ApplyFarmManagement(GameSession session, M08BSeedResult result)
        {
            bool appointed = TryApplyFarmManagement(session, result.IndustryDecisions,
                out bool farmWasVacant, out _);
            if (appointed)
                result.FarmAppointments++;
            if (farmWasVacant)
                result.FarmVacancies++;
        }

        /// <summary>
        /// Living current-family members in ordinal stable-ID order, the documented order of the
        /// farm-managed-v1 candidate scan. Ordinal, not culture-aware: the order must not depend
        /// on the host locale.
        /// </summary>
        private static List<PersonId> LivingFamilyMembersByOrdinalId(SuccessionState succession)
        {
            var candidates = new List<PersonId>();
            if (succession == null || succession.World == null)
                return candidates;
            IReadOnlyList<FamilyMembership> members = succession.World.Family.Members;
            for (int i = 0; i < members.Count; i++)
            {
                PersonId personId = members[i].PersonId;
                if (succession.World.IsLiving(personId))
                    candidates.Add(personId);
            }
            candidates.Sort((left, right) => string.CompareOrdinal(
                left.Value ?? string.Empty, right.Value ?? string.Empty));
            return candidates;
        }

        /// <summary>Reads one slot assignee from the public industry view.</summary>
        private static bool TryGetIndustryAssignee(GameSession session, string industryId,
            out PersonId assignee)
        {
            assignee = default;
            IReadOnlyList<IndustrySlotView> slots = session.Industry.Slots;
            if (slots == null)
                return false;
            for (int i = 0; i < slots.Count; i++)
            {
                if (!string.Equals(slots[i].IndustryId, industryId, StringComparison.Ordinal))
                    continue;
                assignee = slots[i].AssigneeId;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Nearest-rank percentile over ascending values: rank = ceil(n * p / 100), clamped to
        /// [1, n]. p0 returns the minimum, p100 the maximum, and ties return the lower rank.
        /// </summary>
        public static int PercentileNearestRank(IReadOnlyList<int> ascendingValues, int percentile)
        {
            if (ascendingValues == null || ascendingValues.Count == 0)
                throw new ArgumentException("At least one value is required.", nameof(ascendingValues));
            if (percentile < 0 || percentile > 100)
                throw new ArgumentOutOfRangeException(nameof(percentile));

            long rank = ((long)ascendingValues.Count * percentile + 99L) / 100L;
            if (rank < 1L)
                rank = 1L;
            if (rank > ascendingValues.Count)
                rank = ascendingValues.Count;
            return ascendingValues[(int)(rank - 1L)];
        }

        /// <summary>Half-up integer division used by wealth and mean-age metrics.</summary>
        public static int RoundHalfUp(long numerator, long denominator)
        {
            if (denominator <= 0)
                throw new ArgumentOutOfRangeException(nameof(denominator));
            if (numerator >= 0)
                return (int)((numerator + denominator / 2) / denominator);
            return (int)(-((-numerator + denominator / 2) / denominator));
        }

        /// <summary>Funds plus food and materials at their settled checkpoint prices.</summary>
        public static int ComputeWealth(int funds, int foodInventory, int foodPriceIndexPermille,
            int supplies, int materialsPriceIndexPermille)
        {
            long foodValue = RoundHalfUp((long)foodInventory * foodPriceIndexPermille,
                MarketState.BaselinePriceIndexPermille);
            long materialsValue = RoundHalfUp((long)supplies * materialsPriceIndexPermille,
                MarketState.BaselinePriceIndexPermille);
            long total = funds + foodValue + materialsValue;
            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        /// <summary>
        /// Pure stop mapping shared by the runner and tests. Succession keeps its own
        /// termination category and is not a progression deadlock; only an unresolved blocking
        /// event or an exhausted no-progress budget is a deadlock/timeout.
        /// </summary>
        public static M08BTerminationReason ClassifyStop(bool targetReached, bool extinct,
            bool pendingSuccession, bool unresolvedEvent, bool dateOverflow, int consecutiveBlocked)
        {
            if (targetReached) return M08BTerminationReason.Completed;
            if (extinct) return M08BTerminationReason.Extinction;
            if (pendingSuccession) return M08BTerminationReason.PendingSuccession;
            if (unresolvedEvent) return M08BTerminationReason.UnresolvedBlockingEvent;
            if (dateOverflow) return M08BTerminationReason.DateOverflow;
            if (consecutiveBlocked >= MaxConsecutiveBlockedAttempts) return M08BTerminationReason.Timeout;
            throw new ArgumentException("No terminal condition matches the supplied state.");
        }

        /// <summary>
        /// Runs a short seed range under the unmanaged policy in that policy's own artifact
        /// directory, so the accepted historical root artifacts are never rewritten.
        /// </summary>
        public static IReadOnlyList<M08BSeedResult> RunShortBatch(int firstSeed, int count, int years) =>
            RunPolicyBatch(M08BIndustryPolicy.Unmanaged, firstSeed, count, years);

        /// <summary>
        /// Runs a full-length seed range under the unmanaged policy in that policy's own artifact
        /// directory, so the accepted historical root artifacts are never rewritten.
        /// </summary>
        public static IReadOnlyList<M08BSeedResult> RunFullBatch(int firstSeed, int count) =>
            RunPolicyBatch(M08BIndustryPolicy.Unmanaged, firstSeed, count, FullBatchYears);

        /// <summary>
        /// Runs a resumable seed range for one policy into its own artifact directory, so two
        /// policies never share rows, summaries or logs.
        /// </summary>
        public static IReadOnlyList<M08BSeedResult> RunPolicyBatch(M08BIndustryPolicy policy,
            int firstSeed, int count, int years) =>
            RunBatch(firstSeed, count, years, PolicyOutputRoot(policy), DefaultCheckpointYears, policy);

        /// <summary>
        /// Runs a resumable seed range under the unmanaged policy. A blank
        /// <paramref name="outputRoot"/> resolves to that policy's own directory, so the accepted
        /// historical root artifacts are never rewritten, and an explicit
        /// <paramref name="outputRoot"/> that normalizes to the historical root is rejected.
        /// Completed rows from this exact range are kept, and deterministic rows from other
        /// completed ranges with the same year count may be reused; no other range file is ever
        /// written or deleted.
        /// </summary>
        public static IReadOnlyList<M08BSeedResult> RunBatch(int firstSeed, int count, int years,
            string outputRoot, IReadOnlyList<int> checkpointYears) =>
            RunBatch(firstSeed, count, years, outputRoot, checkpointYears,
                M08BIndustryPolicy.Unmanaged);

        /// <summary>
        /// Runs a resumable seed range for one policy. Rows may only be reused from artifacts
        /// whose companion summary proves the same policy, so the two policies stay separate.
        /// </summary>
        public static IReadOnlyList<M08BSeedResult> RunBatch(int firstSeed, int count, int years,
            string outputRoot, IReadOnlyList<int> checkpointYears, M08BIndustryPolicy policy)
        {
            if (firstSeed < 0)
                throw new ArgumentOutOfRangeException(nameof(firstSeed));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (years <= 0)
                throw new ArgumentOutOfRangeException(nameof(years));
            if ((long)firstSeed + count - 1 > uint.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count), "seed range exceeds uint range.");

            List<int> checkpoints = NormalizeCheckpointYears(checkpointYears);
            string root = ResolveArtifactRoot(outputRoot, policy);
            Directory.CreateDirectory(root);

            string seedsPath = Path.Combine(root, SeedsFileName(firstSeed, count, years));
            string checkpointsPath = Path.Combine(root, CheckpointsFileName(firstSeed, count, years));
            string summaryPath = Path.Combine(root, SummaryFileName(firstSeed, count, years));
            string logPath = Path.Combine(root, BatchLogFileName(firstSeed, count, years));

            var bySeed = new Dictionary<ulong, M08BSeedResult>();
            string loadWarning = string.Empty;
            TryLoadRange(root, seedsPath, firstSeed, count, years, checkpoints, policy, bySeed,
                out loadWarning);

            int reused = bySeed.Count;
            int ran = 0;
            long startedTicks = DateTime.UtcNow.Ticks;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var runDetails = new StringBuilder();

            foreach (M08BSeedResult loaded in bySeed.Values.OrderBy(item => item.Seed))
            {
                runDetails.AppendLine(FormatSeedLogLine(loaded, "reused"));
            }

            for (int offset = 0; offset < count; offset++)
            {
                ulong seed = (ulong)(firstSeed + offset);
                if (bySeed.ContainsKey(seed))
                    continue;

                var seedWatch = System.Diagnostics.Stopwatch.StartNew();
                M08BSeedResult seedResult;
                try
                {
                    seedResult = RunSeed(seed, years, checkpoints, policy);
                }
                catch (Exception exception)
                {
                    // Preserve the seed as an explicit stopped sample instead of
                    // aborting the whole range; the category stays in the closed set.
                    seedResult = CreateAbortedSeedResult(seed, years, checkpoints, exception);
                    seedResult.PolicyId = PolicyIdOf(policy);
                }
                seedWatch.Stop();
                seedResult.DurationMilliseconds = seedWatch.ElapsedMilliseconds;
                bySeed[seed] = seedResult;
                ran++;
                runDetails.AppendLine(FormatSeedLogLine(seedResult, "ran"));

                if (ran % 10 == 0)
                    WriteArtifacts(seedsPath, checkpointsPath, summaryPath, bySeed.Values,
                        firstSeed, count, years, checkpoints, policy);
            }

            List<M08BSeedResult> ordered = bySeed.Values.OrderBy(item => item.Seed).ToList();
            WriteArtifacts(seedsPath, checkpointsPath, summaryPath, ordered,
                firstSeed, count, years, checkpoints, policy);

            stopwatch.Stop();
            WriteBatchLog(logPath, firstSeed, count, years, reused, ran, startedTicks,
                stopwatch.ElapsedMilliseconds, ordered, runDetails.ToString(), loadWarning,
                seedsPath, checkpointsPath, summaryPath, policy);

            return ordered.AsReadOnly();
        }

        /// <summary>
        /// Pipeline entry: 20 short runs, one 100-year seed, 200, then 1000, all under the unmanaged
        /// policy, so every artifact lands in <see cref="PolicyOutputRoot"/> and the accepted
        /// historical root artifacts are never rewritten.
        /// </summary>
        public static string RunStagedBatches()
        {
            var report = new StringBuilder();
            report.AppendLine(RunStageShort());
            report.AppendLine(RunStageSingleFull());
            report.AppendLine(RunStageTwoHundred());
            report.AppendLine(RunStageOneThousand());
            return report.ToString();
        }

        public static string RunStageShort() => DescribeStage(
            RunShortBatch(1, 20, ShortBatchYears), "short", 1, 20, ShortBatchYears);

        public static string RunStageSingleFull() => DescribeStage(
            RunFullBatch(1, 1), "full-single", 1, 1, FullBatchYears);

        public static string RunStageTwoHundred() => DescribeStage(
            RunFullBatch(1, 200), "full-200", 1, 200, FullBatchYears);

        public static string RunStageOneThousand() => DescribeStage(
            RunFullBatch(1, 1000), "full-1000", 1, 1000, FullBatchYears);

        /// <summary>
        /// Re-runs every stage for one policy. Files go to that policy's own directory, so the
        /// accepted historical artifacts at <see cref="DefaultOutputRoot"/> are never rewritten.
        /// </summary>
        public static string RunStagedBatches(M08BIndustryPolicy policy)
        {
            var report = new StringBuilder();
            report.AppendLine(RunStageShort(policy));
            report.AppendLine(RunStageSingleFull(policy));
            report.AppendLine(RunStageTwoHundred(policy));
            report.AppendLine(RunStageOneThousand(policy));
            return report.ToString();
        }

        public static string RunStageShort(M08BIndustryPolicy policy) => DescribeStage(
            RunPolicyBatch(policy, 1, 20, ShortBatchYears),
            PolicyIdOf(policy) + "-short", 1, 20, ShortBatchYears);

        public static string RunStageSingleFull(M08BIndustryPolicy policy) => DescribeStage(
            RunPolicyBatch(policy, 1, 1, FullBatchYears),
            PolicyIdOf(policy) + "-full-single", 1, 1, FullBatchYears);

        public static string RunStageTwoHundred(M08BIndustryPolicy policy) => DescribeStage(
            RunPolicyBatch(policy, 1, 200, FullBatchYears),
            PolicyIdOf(policy) + "-full-200", 1, 200, FullBatchYears);

        public static string RunStageOneThousand(M08BIndustryPolicy policy) => DescribeStage(
            RunPolicyBatch(policy, 1, 1000, FullBatchYears),
            PolicyIdOf(policy) + "-full-1000", 1, 1000, FullBatchYears);

        /// <summary>
        /// Keeps an escaped simulator exception as an explicit stopped sample. The category is
        /// <see cref="M08BTerminationReason.SimulatorFault"/>, never <c>Timeout</c>: a rule-level
        /// advance stall and a harness exception are different failures, and a faulted row is
        /// never reused from CSV.
        /// </summary>
        public static M08BSeedResult CreateAbortedSeedResult(ulong seed, int years,
            IReadOnlyList<int> checkpointYears, Exception exception)
        {
            var result = new M08BSeedResult
            {
                Seed = seed,
                SeedHex = FormatSeedHex(seed),
                Termination = M08BTerminationReason.SimulatorFault,
                TerminationDetail = "simulator exception before a rule stop: " +
                    (exception == null ? "unknown exception" : exception.GetType().Name + ": " +
                        exception.Message),
                StartYear = WorldClock.NewGameDate.Year,
                StartMonth = WorldClock.NewGameDate.Month,
                EndYear = WorldClock.NewGameDate.Year,
                EndMonth = WorldClock.NewGameDate.Month,
                MonthsSimulated = 0,
                // No month settled, so the new-game baseline is the only truthful price
                // observation. Leaving these at 0 would serialize a max below the documented
                // 100..100000 index minimum.
                MaxFoodPriceIndexPermille = MarketState.BaselinePriceIndexPermille,
                MinFoodPriceIndexPermille = MarketState.BaselinePriceIndexPermille,
                MaxMaterialsPriceIndexPermille = MarketState.BaselinePriceIndexPermille,
                MinMaterialsPriceIndexPermille = MarketState.BaselinePriceIndexPermille,
                MaxCompositePriceIndexPermille = MarketState.BaselinePriceIndexPermille
            };
            foreach (int year in checkpointYears)
                result.Checkpoints.Add(new M08BCheckpointResult
                {
                    Year = year,
                    MonthsRequired = year * 12
                });
            return result;
        }

        private static string DescribeStage(IReadOnlyList<M08BSeedResult> seeds, string label,
            int firstSeed, int count, int years)
        {
            var summary = new StringBuilder();
            summary.Append("M08B ").Append(label).Append(": seeds ")
                .Append(firstSeed.ToString(CultureInfo.InvariantCulture)).Append("..")
                .Append((firstSeed + count - 1).ToString(CultureInfo.InvariantCulture))
                .Append(" years=").Append(years.ToString(CultureInfo.InvariantCulture))
                .Append(" completed=").Append(seeds.Count.ToString(CultureInfo.InvariantCulture));
            foreach (M08BTerminationReason reason in Enum.GetValues(typeof(M08BTerminationReason)))
            {
                int value = seeds.Count(item => item.Termination == reason);
                if (value > 0)
                    summary.Append(' ').Append(reason).Append('=')
                        .Append(value.ToString(CultureInfo.InvariantCulture));
            }
            return summary.ToString();
        }

        private static void UpdateMonthlyMetrics(GameSession session, M08BSeedResult result,
            ref int currentFamineRun, ref int currentStallRun)
        {
            MarketState market = session.Market;
            int composite = market.CompositePriceIndexPermille;
            if (result.MaxCompositePriceIndexPermille < composite)
                result.MaxCompositePriceIndexPermille = composite;

            if (market.FoodPriceIndexPermille > result.MaxFoodPriceIndexPermille)
                result.MaxFoodPriceIndexPermille = market.FoodPriceIndexPermille;
            if (market.FoodPriceIndexPermille < result.MinFoodPriceIndexPermille)
                result.MinFoodPriceIndexPermille = market.FoodPriceIndexPermille;
            if (market.MaterialsPriceIndexPermille > result.MaxMaterialsPriceIndexPermille)
                result.MaxMaterialsPriceIndexPermille = market.MaterialsPriceIndexPermille;
            if (market.MaterialsPriceIndexPermille < result.MinMaterialsPriceIndexPermille)
                result.MinMaterialsPriceIndexPermille = market.MaterialsPriceIndexPermille;

            if (IsExtremeInflation(market, composite))
            {
                if (!result.ExtremeInflation)
                    result.ExtremeInflation = true;
                result.ExtremeInflationMonths++;
            }

            MarketMonthReport marketReport = session.LastMarketReport;
            if (marketReport != null && marketReport.FoodUnmet > 0)
            {
                currentFamineRun++;
                if (currentFamineRun > result.LongestFamineMonths)
                    result.LongestFamineMonths = currentFamineRun;
                if (currentFamineRun >= CollapseConsecutiveMonths)
                    result.FamineCollapse = true;
            }
            else
            {
                currentFamineRun = 0;
            }

            IndustryMonthReport industryReport = session.LastIndustryReport;
            if (industryReport != null && AllAppointedLinesStalled(industryReport))
            {
                currentStallRun++;
                if (currentStallRun > result.LongestIndustryStallMonths)
                    result.LongestIndustryStallMonths = currentStallRun;
                if (currentStallRun >= CollapseConsecutiveMonths)
                    result.IndustryCollapse = true;
            }
            else
            {
                currentStallRun = 0;
            }

            result.EconomicCollapse = result.FamineCollapse || result.IndustryCollapse;
        }

        private static bool IsExtremeInflation(MarketState market, int composite)
        {
            int monthIndex = market.LastSettledMonthIndex;
            if (monthIndex < 12)
                return false;
            if (!market.TryGetCompositeIndexForMonth(monthIndex - 12, out int yearAgo))
                return false;
            return (long)composite >= (long)yearAgo * 2L;
        }

        private static bool AllAppointedLinesStalled(IndustryMonthReport report)
        {
            bool anyAppointed = false;
            foreach (IndustryLineResult line in report.Lines)
            {
                if (line.AssigneeId.IsEmpty)
                    continue;
                anyAppointed = true;
                if (line.Status != IndustryLineStatus.Stalled)
                    return false;
            }
            return anyAppointed;
        }

        private static void CaptureDueCheckpoints(GameSession session, M08BSeedResult result,
            int currentFamineRun, int currentStallRun)
        {
            foreach (M08BCheckpointResult checkpoint in result.Checkpoints)
            {
                if (checkpoint.Reached || checkpoint.MonthsRequired != result.MonthsSimulated)
                    continue;
                CaptureCheckpoint(session, result, checkpoint, currentFamineRun, currentStallRun);
            }
        }

        private static void CaptureCheckpoint(GameSession session, M08BSeedResult result,
            M08BCheckpointResult checkpoint, int currentFamineRun, int currentStallRun)
        {
            SaveData save = session.CaptureSave();
            GameDate date = session.WorldClock.CurrentDate;
            int living = CountLivingFamilyMembers(session.Succession);
            bool alive = !session.IsFamilyExtinct && living > 0;
            int foodInventory = ResolveFoodInventory(save, session);
            int foodPrice = ResolveFoodPrice(save, session);
            int materialsPrice = ResolveMaterialsPrice(save, session);
            int supplies = save.industry != null ? save.industry.supplies : 0;
            int funds = save.industry != null ? save.industry.funds : 0;
            int eventCount = CountScheduledEvents(save);

            checkpoint.Reached = true;
            checkpoint.Alive = alive;
            checkpoint.LivingMembers = living;
            checkpoint.MeanLivingAgeTenths = MeanLivingAgeTenths(session.Succession, date);
            checkpoint.Funds = funds;
            checkpoint.FoodInventory = foodInventory;
            checkpoint.Supplies = supplies;
            checkpoint.FoodPriceIndexPermille = foodPrice;
            checkpoint.MaterialsPriceIndexPermille = materialsPrice;
            checkpoint.CompositePriceIndexPermille = session.Market.CompositePriceIndexPermille;
            checkpoint.Wealth = ComputeWealth(funds, foodInventory, foodPrice, supplies, materialsPrice);
            checkpoint.HeirGap = alive &&
                SuccessionService.GetCandidates(session.Succession, date).Count == 0;
            checkpoint.EventCount = eventCount;
            checkpoint.EventsPer1000Months = result.MonthsSimulated > 0
                ? RoundHalfUp((long)eventCount * 1000L, result.MonthsSimulated)
                : 0;
            // These flags describe this checkpoint only. The run-level flags stay on the seed
            // row, so a later collapse can never retroactively mark an earlier checkpoint.
            checkpoint.FamineCollapse = currentFamineRun >= CollapseConsecutiveMonths;
            checkpoint.IndustryCollapse = currentStallRun >= CollapseConsecutiveMonths;
            checkpoint.EconomicCollapse = checkpoint.FamineCollapse || checkpoint.IndustryCollapse;
            checkpoint.ExtremeInflation = IsExtremeInflation(session.Market,
                session.Market.CompositePriceIndexPermille);
            HashSet<string> livingIds = LivingFamilyMemberIds(session.Succession);
            checkpoint.FamilyRelationshipAnomalies = CountFamilyRelationshipAnomalies(save,
                session.Succession.HeadPersonId.Value, livingIds, out int sampled);
            checkpoint.FamilyRelationshipsSampled = sampled;
            checkpoint.PersistedRelationshipViolations = CountPersistedRelationshipViolations(save);
            checkpoint.ProgressionDeadlocks = result.ProgressionDeadlocks;
            if (checkpoint.FamilyRelationshipAnomalies > result.PeakFamilyRelationshipAnomalies)
                result.PeakFamilyRelationshipAnomalies = checkpoint.FamilyRelationshipAnomalies;
            if (checkpoint.FamilyRelationshipsSampled > result.FamilyRelationshipsSampled)
                result.FamilyRelationshipsSampled = checkpoint.FamilyRelationshipsSampled;
        }

        private static void CaptureFinalState(GameSession session, M08BSeedResult result)
        {
            SaveData save = session.CaptureSave();
            GameDate date = session.WorldClock.CurrentDate;
            int living = CountLivingFamilyMembers(session.Succession);
            result.EndYear = date.Year;
            result.EndMonth = date.Month;
            result.FinalFunds = save.industry != null ? save.industry.funds : 0;
            result.FinalFoodInventory = ResolveFoodInventory(save, session);
            result.FinalSupplies = save.industry != null ? save.industry.supplies : 0;
            result.FinalLivingMembers = living;
            result.FinalFarmAssignee = TryGetIndustryAssignee(session, IndustryCatalog.FarmId,
                    out PersonId farmAssignee) && !farmAssignee.IsEmpty
                ? farmAssignee.Value ?? string.Empty
                : string.Empty;
            result.FinalFamilyExtinct = session.IsFamilyExtinct;
            result.FinalHeirGap = !result.FinalFamilyExtinct && living > 0 &&
                SuccessionService.GetCandidates(session.Succession, date).Count == 0;
            HashSet<string> livingIds = LivingFamilyMemberIds(session.Succession);
            result.FinalFamilyRelationshipAnomalies = CountFamilyRelationshipAnomalies(save,
                session.Succession.HeadPersonId.Value, livingIds, out int sampled);
            if (sampled > result.FamilyRelationshipsSampled)
                result.FamilyRelationshipsSampled = sampled;
            if (result.FinalFamilyRelationshipAnomalies > result.PeakFamilyRelationshipAnomalies)
                result.PeakFamilyRelationshipAnomalies = result.FinalFamilyRelationshipAnomalies;
            result.PersistedRelationshipViolations = CountPersistedRelationshipViolations(save);
            result.EventsTriggered = CountScheduledEvents(save);
            result.EconomicCollapse = result.FamineCollapse || result.IndustryCollapse;
            if (result.MaxCompositePriceIndexPermille < session.Market.CompositePriceIndexPermille)
                result.MaxCompositePriceIndexPermille = session.Market.CompositePriceIndexPermille;
        }

        /// <summary>Stable IDs of living current-family members; empty when the roster is absent.</summary>
        private static HashSet<string> LivingFamilyMemberIds(SuccessionState succession)
        {
            var living = new HashSet<string>(StringComparer.Ordinal);
            if (succession == null || succession.World == null)
                return living;
            IReadOnlyList<FamilyMembership> members = succession.World.Family.Members;
            for (int i = 0; i < members.Count; i++)
            {
                PersonId personId = members[i].PersonId;
                if (succession.World.IsLiving(personId))
                    living.Add(personId.Value);
            }
            return living;
        }

        private static int ResolveFoodInventory(SaveData save, GameSession session) =>
            save.market != null ? save.market.foodInventory : session.Market.FoodInventory;

        private static int ResolveFoodPrice(SaveData save, GameSession session) =>
            save.market != null ? save.market.foodPriceIndexPermille : session.Market.FoodPriceIndexPermille;

        private static int ResolveMaterialsPrice(SaveData save, GameSession session) =>
            save.market != null
                ? save.market.materialsPriceIndexPermille
                : session.Market.MaterialsPriceIndexPermille;

        private static int CountScheduledEvents(SaveData save)
        {
            if (save.eventRuntime == null)
                return 0;
            int history = save.eventRuntime.history != null ? save.eventRuntime.history.Length : 0;
            int pending = save.eventRuntime.pendingEventIds != null
                ? save.eventRuntime.pendingEventIds.Length
                : 0;
            return history + pending;
        }

        private static int CountLivingFamilyMembers(SuccessionState succession)
        {
            if (succession == null || succession.World == null)
                return 0;
            int living = 0;
            IReadOnlyList<FamilyMembership> members = succession.World.Family.Members;
            for (int i = 0; i < members.Count; i++)
            {
                if (succession.World.IsLiving(members[i].PersonId))
                    living++;
            }
            return living;
        }

        private static int MeanLivingAgeTenths(SuccessionState succession, GameDate date)
        {
            if (succession == null || succession.World == null)
                return 0;
            long ageSum = 0;
            int count = 0;
            IReadOnlyList<FamilyMembership> members = succession.World.Family.Members;
            for (int i = 0; i < members.Count; i++)
            {
                PersonId personId = members[i].PersonId;
                if (!succession.World.TryGetLife(personId, out SuccessionPersonLife life) || !life.Alive)
                    continue;
                ageSum += life.AgeAt(date);
                count++;
            }
            return count == 0 ? 0 : RoundHalfUp(ageSum * 10L, count);
        }

        /// <summary>
        /// GDD 14.2 家族关系异常, as the only behavioural family-relation signal the domain
        /// records: a persisted relation from the current head to a living family member whose
        /// value sits at the documented hostile bound (<see cref="FamilyRelationships.MinimumValue"/>).
        /// <paramref name="sampled"/> reports how many head-to-living-member relations were
        /// inspected, so a zero sample means the metric is unavailable rather than healthy.
        /// Event content can drive these values through FamilyRelationshipDelta effects; this is
        /// not an invariant that validation guarantees.
        /// </summary>
        public static int CountFamilyRelationshipAnomalies(SaveData save, string headPersonId,
            ICollection<string> livingPersonIds, out int sampled)
        {
            sampled = 0;
            if (save == null || save.familyRelationships == null ||
                string.IsNullOrEmpty(headPersonId) || livingPersonIds == null)
                return 0;

            int anomalies = 0;
            foreach (FamilyRelationshipSaveEntry entry in save.familyRelationships)
            {
                if (entry == null || string.IsNullOrEmpty(entry.memberPersonId) ||
                    !string.Equals(entry.headPersonId, headPersonId, StringComparison.Ordinal) ||
                    !livingPersonIds.Contains(entry.memberPersonId))
                    continue;
                sampled++;
                if (entry.value <= FamilyRelationships.MinimumValue)
                    anomalies++;
            }
            return anomalies;
        }

        /// <summary>
        /// Integrity guard, not the GDD metric: counts persisted relationship entries that break
        /// their own documented invariant (empty/self endpoint, out-of-range value, duplicate
        /// family pair). A valid save never contains one, so a nonzero value means the artifact or
        /// harness is corrupt. Reported separately as <c>persistedRelationshipViolations</c>.
        /// </summary>
        public static int CountPersistedRelationshipViolations(SaveData save)
        {
            if (save == null)
                return 0;
            int anomalies = 0;
            if (save.relationships != null)
            {
                foreach (RelationshipSaveEntry entry in save.relationships)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.characterId) ||
                        entry.value < CharacterRelationships.MinimumValue ||
                        entry.value > CharacterRelationships.MaximumValue)
                        anomalies++;
                }
            }
            if (save.familyRelationships != null)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (FamilyRelationshipSaveEntry entry in save.familyRelationships)
                {
                    if (entry == null || string.IsNullOrEmpty(entry.headPersonId) ||
                        string.IsNullOrEmpty(entry.memberPersonId) ||
                        string.Equals(entry.headPersonId, entry.memberPersonId, StringComparison.Ordinal) ||
                        entry.value < FamilyRelationships.MinimumValue ||
                        entry.value > FamilyRelationships.MaximumValue)
                    {
                        anomalies++;
                        continue;
                    }
                    if (!seen.Add(entry.headPersonId + "|" + entry.memberPersonId))
                        anomalies++;
                }
            }
            return anomalies;
        }

        private static string SafeInitialRandomStateHex(GameSession session)
        {
            try
            {
                SaveData save = session.CaptureSave();
                return save.randomState != null && !string.IsNullOrEmpty(save.randomState.initialSeedHex)
                    ? save.randomState.initialSeedHex
                    : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private static List<int> NormalizeCheckpointYears(IReadOnlyList<int> checkpointYears)
        {
            List<int> source = checkpointYears == null
                ? new List<int>(DefaultCheckpointYears)
                : checkpointYears.Distinct().OrderBy(year => year).ToList();
            var normalized = new List<int>(source.Count);
            foreach (int year in source)
            {
                if (year <= 0)
                    throw new ArgumentOutOfRangeException(nameof(checkpointYears),
                        "checkpoint years must be positive.");
                if (year > 400)
                    throw new ArgumentOutOfRangeException(nameof(checkpointYears),
                        "checkpoint years exceed the supported calendar span.");
                normalized.Add(year);
            }
            return normalized;
        }

        private static string FormatSeedHex(ulong seed) =>
            "0x" + seed.ToString("X16", CultureInfo.InvariantCulture);

        private static string SeedsFileName(int firstSeed, int count, int years) =>
            "seeds-" + firstSeed.ToString(CultureInfo.InvariantCulture) + "-" +
            count.ToString(CultureInfo.InvariantCulture) + "-" +
            years.ToString(CultureInfo.InvariantCulture) + "y.csv";

        private static string CheckpointsFileName(int firstSeed, int count, int years) =>
            "checkpoints-" + firstSeed.ToString(CultureInfo.InvariantCulture) + "-" +
            count.ToString(CultureInfo.InvariantCulture) + "-" +
            years.ToString(CultureInfo.InvariantCulture) + "y.csv";

        private static string SummaryFileName(int firstSeed, int count, int years) =>
            "summary-" + firstSeed.ToString(CultureInfo.InvariantCulture) + "-" +
            count.ToString(CultureInfo.InvariantCulture) + "-" +
            years.ToString(CultureInfo.InvariantCulture) + "y.json";

        private static string BatchLogFileName(int firstSeed, int count, int years) =>
            "batch-" + firstSeed.ToString(CultureInfo.InvariantCulture) + "-" +
            count.ToString(CultureInfo.InvariantCulture) + "-" +
            years.ToString(CultureInfo.InvariantCulture) + "y.log";

        private static void WriteArtifacts(string seedsPath, string checkpointsPath, string summaryPath,
            IEnumerable<M08BSeedResult> seeds, int firstSeed, int count, int years,
            IReadOnlyList<int> checkpointYears, M08BIndustryPolicy policy)
        {
            List<M08BSeedResult> ordered = seeds.OrderBy(item => item.Seed).ToList();
            File.WriteAllText(seedsPath, SerializeSeedsCsv(ordered, checkpointYears), Utf8NoBom);
            File.WriteAllText(checkpointsPath,
                SerializeCheckpointsCsv(ordered, checkpointYears), Utf8NoBom);
            File.WriteAllText(summaryPath,
                BuildSummaryJson(ordered, firstSeed, count, years, checkpointYears, policy), Utf8NoBom);
        }

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        /// <summary>
        /// Reuses a completed CSV row range only when its companion summary proves the same
        /// runner source, normalized schema and rule version, and a compatible range and
        /// checkpoint set. Any doubt rejects the file instead of trusting a stale artifact.
        /// </summary>
        private static void TryLoadRange(string root, string seedsPath, int firstSeed, int count,
            int years, IReadOnlyList<int> checkpointYears, M08BIndustryPolicy policy,
            Dictionary<ulong, M08BSeedResult> bySeed, out string warning)
        {
            var warnings = new List<string>();
            List<int> checkpoints = NormalizeCheckpointYears(checkpointYears);
            string sourceSha = ComputeSourceSha256();
            string ruleInputSha = ComputeRuleInputSha256();
            string policyId = PolicyIdOf(policy);
            if (File.Exists(seedsPath))
            {
                if (!TryValidateReuseProvenance(seedsPath, years, checkpoints, sourceSha, ruleInputSha,
                        policyId, out string ownRejection))
                {
                    warnings.Add("own range not reused: " + ownRejection);
                }
                else if (TryParseSeedsCsv(File.ReadAllText(seedsPath), firstSeed, count, years,
                        checkpointYears, out List<M08BSeedResult> own, out string ownError))
                {
                    AcceptReusableRows(own, bySeed, warnings, "own range", policyId);
                }
                else if (!string.IsNullOrEmpty(ownError))
                {
                    warnings.Add("own range not fully reusable: " + ownError);
                }
            }

            foreach (string candidate in Directory.GetFiles(root, "seeds-*-" +
                         years.ToString(CultureInfo.InvariantCulture) + "y.csv"))
            {
                if (string.Equals(Path.GetFullPath(candidate), Path.GetFullPath(seedsPath),
                        StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!TryValidateReuseProvenance(candidate, years, checkpoints, sourceSha, ruleInputSha,
                        policyId, out string rejection))
                {
                    warnings.Add(Path.GetFileName(candidate) + " not reused: " + rejection);
                    continue;
                }
                if (!TryParseSeedsCsv(File.ReadAllText(candidate), firstSeed, count, years,
                        checkpointYears, out List<M08BSeedResult> imported, out string candidateError))
                {
                    if (!string.IsNullOrEmpty(candidateError) &&
                        !string.Equals(candidateError, NoSeedRowsError, StringComparison.Ordinal))
                        warnings.Add(Path.GetFileName(candidate) + " not reusable: " + candidateError);
                    continue;
                }
                AcceptReusableRows(imported, bySeed, warnings, Path.GetFileName(candidate), policyId);
            }

            warning = string.Join(" | ", warnings);
        }

        /// <summary>
        /// Adds parsed rows that are safe to reuse. A <see cref="M08BTerminationReason.SimulatorFault"/>
        /// row is never reused: the harness fault must be re-observed, not frozen into the range.
        /// </summary>
        private static void AcceptReusableRows(IReadOnlyList<M08BSeedResult> rows,
            Dictionary<ulong, M08BSeedResult> bySeed, List<string> warnings, string sourceLabel,
            string policyId)
        {
            var faulted = new List<ulong>();
            var wrongPolicy = new List<ulong>();
            foreach (M08BSeedResult item in rows)
            {
                if (item.Termination == M08BTerminationReason.SimulatorFault)
                {
                    faulted.Add(item.Seed);
                    continue;
                }
                if (!string.Equals(item.PolicyId, policyId, StringComparison.Ordinal))
                {
                    wrongPolicy.Add(item.Seed);
                    continue;
                }
                if (!bySeed.ContainsKey(item.Seed))
                    bySeed[item.Seed] = item;
            }
            if (faulted.Count > 0)
                warnings.Add(sourceLabel + " faulted rows re-run: " +
                    string.Join(",", faulted.Select(seed => seed.ToString(CultureInfo.InvariantCulture))));
            if (wrongPolicy.Count > 0)
                warnings.Add(sourceLabel + " rows from another policy re-run: " +
                    string.Join(",", wrongPolicy.Select(seed =>
                        seed.ToString(CultureInfo.InvariantCulture))));
        }

        /// <summary>
        /// Proves a seeds CSV may be reused. The companion summary must exist, name the same
        /// range as the file, and match the current schema, rule version, runner source hash,
        /// whole rule-input dependency hash, year count and checkpoint set. Empty current hashes
        /// cannot prove anything, so they reject.
        /// </summary>
        private static bool TryValidateReuseProvenance(string seedsPath, int years,
            IReadOnlyList<int> checkpointYears, string currentSourceSha, string currentRuleInputSha,
            string currentPolicyId, out string rejection)
        {
            rejection = string.Empty;
            string summaryPath = SummaryPathForSeedsPath(seedsPath);
            if (summaryPath == null)
            {
                rejection = "no companion summary path";
                return false;
            }
            if (!TryReadSummaryProvenance(summaryPath, out SummaryProvenance provenance,
                    out string readError))
            {
                rejection = readError;
                return false;
            }
            if (!string.Equals(provenance.Schema, NormalizedSchema, StringComparison.Ordinal))
            {
                rejection = "summary schema " + provenance.Schema + " does not match " + NormalizedSchema;
                return false;
            }
            if (!string.Equals(provenance.RuleVersion, RuleVersion, StringComparison.Ordinal))
            {
                rejection = "summary ruleVersion " + provenance.RuleVersion + " does not match " +
                    RuleVersion;
                return false;
            }
            if (!string.Equals(provenance.Policy, currentPolicyId, StringComparison.Ordinal))
            {
                rejection = "summary policy " + provenance.Policy + " does not match " +
                    currentPolicyId;
                return false;
            }
            if (string.IsNullOrEmpty(currentSourceSha) ||
                !string.Equals(provenance.SourceSha256, currentSourceSha, StringComparison.Ordinal))
            {
                rejection = "summary sourceSha256 does not match the current runner source";
                return false;
            }
            if (string.IsNullOrEmpty(currentRuleInputSha) ||
                !string.Equals(provenance.RuleInputSha256, currentRuleInputSha,
                    StringComparison.Ordinal))
            {
                rejection = "summary ruleInputSha256 does not match the current simulation rule inputs";
                return false;
            }
            if (!TryParseSeedsFileName(seedsPath, out int fileFirstSeed, out int fileCount,
                    out int fileYears))
            {
                rejection = "file name is not a normalized seed range";
                return false;
            }
            if (provenance.FirstSeed != fileFirstSeed || provenance.Count != fileCount ||
                provenance.Years != fileYears)
            {
                rejection = "summary range does not describe this file";
                return false;
            }
            if (provenance.Years != years)
            {
                rejection = "summary years " + provenance.Years + " does not match " + years;
                return false;
            }
            if (!SameCheckpoints(provenance.CheckpointYears, checkpointYears))
            {
                rejection = "summary checkpoint set is incompatible";
                return false;
            }
            return true;
        }

        private static bool SameCheckpoints(IReadOnlyList<int> left, IReadOnlyList<int> right)
        {
            if (left == null || right == null || left.Count != right.Count)
                return false;
            for (int i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }

        private static string SummaryPathForSeedsPath(string seedsPath)
        {
            string fileName = Path.GetFileName(seedsPath);
            const string prefix = "seeds-";
            const string suffix = ".csv";
            if (!fileName.StartsWith(prefix, StringComparison.Ordinal) ||
                !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return null;
            string core = fileName.Substring(prefix.Length,
                fileName.Length - prefix.Length - suffix.Length);
            return Path.Combine(Path.GetDirectoryName(seedsPath) ?? string.Empty,
                "summary-" + core + ".json");
        }

        private static bool TryParseSeedsFileName(string seedsPath, out int firstSeed, out int count,
            out int years)
        {
            firstSeed = 0;
            count = 0;
            years = 0;
            string fileName = Path.GetFileName(seedsPath);
            const string prefix = "seeds-";
            const string suffix = ".csv";
            if (!fileName.StartsWith(prefix, StringComparison.Ordinal) ||
                !fileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return false;
            string core = fileName.Substring(prefix.Length,
                fileName.Length - prefix.Length - suffix.Length);
            if (!core.EndsWith("y", StringComparison.Ordinal))
                return false;
            string[] parts = core.Substring(0, core.Length - 1).Split('-');
            if (parts.Length != 3)
                return false;
            return int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out firstSeed) &&
                int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out count) &&
                int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out years);
        }

        private sealed class SummaryProvenance
        {
            public string Schema;
            public string RuleVersion;
            public string Policy;
            public string SourceSha256;
            public string RuleInputSha256;
            public int FirstSeed;
            public int Count;
            public int Years;
            public List<int> CheckpointYears;
        }

        private static bool TryReadSummaryProvenance(string path, out SummaryProvenance provenance,
            out string error)
        {
            provenance = null;
            error = string.Empty;
            if (!File.Exists(path))
            {
                error = "companion summary is missing";
                return false;
            }

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                error = "companion summary is unreadable: " + exception.Message;
                return false;
            }

            if (!TryReadJsonString(json, "schema", out string schema) ||
                !TryReadJsonString(json, "ruleVersion", out string ruleVersion) ||
                !TryReadJsonString(json, "policy", out string policy) ||
                !TryReadJsonString(json, "sourceSha256", out string sourceSha256) ||
                !TryReadJsonString(json, "ruleInputSha256", out string ruleInputSha256) ||
                !TryReadJsonInt(json, "firstSeed", out int firstSeed) ||
                !TryReadJsonInt(json, "count", out int count) ||
                !TryReadJsonInt(json, "years", out int years) ||
                !TryReadJsonIntList(json, "checkpointYears", out List<int> checkpointYears))
            {
                error = "companion summary is not a readable provenance record";
                return false;
            }

            provenance = new SummaryProvenance
            {
                Schema = schema,
                RuleVersion = ruleVersion,
                Policy = policy,
                SourceSha256 = sourceSha256,
                RuleInputSha256 = ruleInputSha256,
                FirstSeed = firstSeed,
                Count = count,
                Years = years,
                CheckpointYears = checkpointYears
            };
            return true;
        }

        private static bool TryReadJsonString(string json, string key, out string value)
        {
            value = string.Empty;
            string marker = "\"" + key + "\":\"";
            int start = json.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
                return false;
            start += marker.Length;
            int end = json.IndexOf('"', start);
            if (end < 0)
                return false;
            value = json.Substring(start, end - start);
            return true;
        }

        private static bool TryReadJsonInt(string json, string key, out int value)
        {
            value = 0;
            string marker = "\"" + key + "\":";
            int start = json.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
                return false;
            start += marker.Length;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;
            int end = start;
            if (end < json.Length && (json[end] == '-' || json[end] == '+'))
                end++;
            while (end < json.Length && json[end] >= '0' && json[end] <= '9')
                end++;
            return end > start && int.TryParse(json.Substring(start, end - start),
                NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        private static bool TryReadJsonIntList(string json, string key, out List<int> values)
        {
            values = new List<int>();
            string marker = "\"" + key + "\":";
            int start = json.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
                return false;
            start += marker.Length;
            while (start < json.Length && char.IsWhiteSpace(json[start]))
                start++;
            if (start >= json.Length || json[start] != '[')
                return false;
            int end = json.IndexOf(']', start);
            if (end < 0)
                return false;
            foreach (string part in json.Substring(start + 1, end - start - 1).Split(','))
            {
                string text = part.Trim();
                if (text.Length == 0)
                    continue;
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out int value))
                    return false;
                values.Add(value);
            }
            return true;
        }

        private static string FormatSeedLogLine(M08BSeedResult seed, string disposition)
        {
            string duration = disposition == "reused"
                ? "-"
                : seed.DurationMilliseconds.ToString(CultureInfo.InvariantCulture);
            return "seed " + seed.Seed.ToString(CultureInfo.InvariantCulture) + " " +
                seed.Termination + " months=" + seed.MonthsSimulated.ToString(CultureInfo.InvariantCulture) +
                " ms=" + duration +
                " [" + disposition + "] " + seed.TerminationDetail;
        }

        /// <summary>
        /// Rewrites the batch log for this run only. Appending would leave earlier runs' headers
        /// and counters in the same file, so the log could no longer describe the run it names.
        /// </summary>
        private static void WriteBatchLog(string path, int firstSeed, int count, int years,
            int reused, int ran, long startedTicks, long totalMilliseconds,
            IReadOnlyList<M08BSeedResult> seeds, string runDetails, string warning,
            string seedsPath, string checkpointsPath, string summaryPath, M08BIndustryPolicy policy)
        {
            var log = new StringBuilder(8192);
            log.AppendLine("M08B LONG-TERM SIMULATION BATCH LOG");
            log.AppendLine("utcStart: " +
                new DateTime(startedTicks, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture));
            log.AppendLine("utcEnd: " + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            log.AppendLine("range: firstSeed=" + firstSeed.ToString(CultureInfo.InvariantCulture) +
                " count=" + count.ToString(CultureInfo.InvariantCulture) +
                " years=" + years.ToString(CultureInfo.InvariantCulture));
            log.AppendLine("sourceSha256=" + ComputeSourceSha256());
            TryComputeRuleInputProvenance(out string ruleInputSha256, out int ruleInputFileCount);
            log.AppendLine("ruleInputSha256=" + ruleInputSha256);
            log.AppendLine("ruleInputFileCount=" +
                ruleInputFileCount.ToString(CultureInfo.InvariantCulture));
            log.AppendLine("schema=" + NormalizedSchema + " ruleVersion=" + RuleVersion);
            log.AppendLine("policy=" + PolicyIdOf(policy));
            log.AppendLine("reused=" + reused.ToString(CultureInfo.InvariantCulture) +
                " ran=" + ran.ToString(CultureInfo.InvariantCulture) +
                " totalSeeds=" + seeds.Count.ToString(CultureInfo.InvariantCulture));
            log.AppendLine("durationMs=" + totalMilliseconds.ToString(CultureInfo.InvariantCulture));
            log.AppendLine("managedBytes=" +
                GC.GetTotalMemory(false).ToString(CultureInfo.InvariantCulture));
            log.AppendLine("seedDurationSumMs=" +
                seeds.Sum(item => item.DurationMilliseconds).ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(warning))
                log.AppendLine("warning: " + warning);
            foreach (M08BTerminationReason reason in Enum.GetValues(typeof(M08BTerminationReason)))
            {
                int value = seeds.Count(item => item.Termination == reason);
                if (value > 0)
                    log.AppendLine("termination " + reason + "=" +
                        value.ToString(CultureInfo.InvariantCulture));
            }
            log.AppendLine("stopped seeds:");
            foreach (M08BSeedResult seed in seeds.Where(item => item.Termination !=
                         M08BTerminationReason.Completed))
            {
                log.AppendLine("  seed " + seed.Seed.ToString(CultureInfo.InvariantCulture) + " " +
                    seed.Termination + " months=" +
                    seed.MonthsSimulated.ToString(CultureInfo.InvariantCulture) + " detail=" +
                    seed.TerminationDetail);
            }
            log.AppendLine("artifactSizes:");
            log.AppendLine("  " + Path.GetFileName(seedsPath) + "=" + FileSize(seedsPath));
            log.AppendLine("  " + Path.GetFileName(checkpointsPath) + "=" + FileSize(checkpointsPath));
            log.AppendLine("  " + Path.GetFileName(summaryPath) + "=" + FileSize(summaryPath));
            log.AppendLine("seedRuns:");
            log.Append(runDetails);
            File.WriteAllText(path, log.ToString(), Utf8NoBom);
        }

        private static long FileSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0L;

        // ---- normalized CSV -------------------------------------------------------------

        private static readonly string[] SeedColumns =
        {
            "seed", "seedHex", "policy", "termination", "terminationDetail", "startYear", "startMonth",
            "endYear", "endMonth", "monthsSimulated", "initialRandomStateHex", "eventsTriggered",
            "eventDeadlocks", "famineCollapse", "longestFamineMonths", "industryCollapse",
            "longestIndustryStallMonths", "economicCollapse", "extremeInflation",
            "extremeInflationMonths", "peakFamilyRelationshipAnomalies",
            "finalFamilyRelationshipAnomalies", "familyRelationshipsSampled",
            "persistedRelationshipViolations", "progressionDeadlocks",
            "maxFoodPriceIndex", "minFoodPriceIndex", "maxMaterialsPriceIndex",
            "minMaterialsPriceIndex", "maxCompositePriceIndex", "finalFunds",
            "finalFoodInventory", "finalSupplies", "finalLivingMembers",
            "finalFamilyExtinct", "finalHeirGap", "farmAppointments", "farmVacancies",
            "finalFarmAssignee"
        };

        private static readonly string[] CheckpointColumns =
        {
            "reached", "alive", "livingMembers", "meanAgeTenths", "funds", "foodInventory",
            "supplies", "foodPriceIndex", "materialsPriceIndex", "compositePriceIndex", "wealth",
            "heirGap", "eventCount", "eventsPer1000Months", "famineCollapse",
            "industryCollapse", "economicCollapse", "extremeInflation",
            "familyRelationshipAnomalies", "familyRelationshipsSampled",
            "persistedRelationshipViolations", "progressionDeadlocks"
        };

        public static string SerializeSeedsCsv(IReadOnlyList<M08BSeedResult> seeds,
            IReadOnlyList<int> checkpointYears)
        {
            List<int> years = NormalizeCheckpointYears(checkpointYears);
            var header = new List<string>(SeedColumns);
            foreach (int year in years)
                foreach (string column in CheckpointColumns)
                    header.Add("y" + year.ToString(CultureInfo.InvariantCulture) + "_" + column);

            var csv = new StringBuilder(8192);
            csv.Append(string.Join(",", header)).Append('\n');
            if (seeds == null)
                return csv.ToString();

            foreach (M08BSeedResult seed in seeds.OrderBy(item => item.Seed))
            {
                var row = new List<string>
                {
                    seed.Seed.ToString(CultureInfo.InvariantCulture),
                    seed.SeedHex ?? FormatSeedHex(seed.Seed),
                    seed.PolicyId ?? UnmanagedPolicyId,
                    seed.Termination.ToString(),
                    seed.TerminationDetail ?? string.Empty,
                    seed.StartYear.ToString(CultureInfo.InvariantCulture),
                    seed.StartMonth.ToString(CultureInfo.InvariantCulture),
                    seed.EndYear.ToString(CultureInfo.InvariantCulture),
                    seed.EndMonth.ToString(CultureInfo.InvariantCulture),
                    seed.MonthsSimulated.ToString(CultureInfo.InvariantCulture),
                    seed.InitialRandomStateHex ?? string.Empty,
                    seed.EventsTriggered.ToString(CultureInfo.InvariantCulture),
                    seed.EventDeadlocks.ToString(CultureInfo.InvariantCulture),
                    Bool(seed.FamineCollapse),
                    seed.LongestFamineMonths.ToString(CultureInfo.InvariantCulture),
                    Bool(seed.IndustryCollapse),
                    seed.LongestIndustryStallMonths.ToString(CultureInfo.InvariantCulture),
                    Bool(seed.EconomicCollapse),
                    Bool(seed.ExtremeInflation),
                    seed.ExtremeInflationMonths.ToString(CultureInfo.InvariantCulture),
                    seed.PeakFamilyRelationshipAnomalies.ToString(CultureInfo.InvariantCulture),
                    seed.FinalFamilyRelationshipAnomalies.ToString(CultureInfo.InvariantCulture),
                    seed.FamilyRelationshipsSampled.ToString(CultureInfo.InvariantCulture),
                    seed.PersistedRelationshipViolations.ToString(CultureInfo.InvariantCulture),
                    seed.ProgressionDeadlocks.ToString(CultureInfo.InvariantCulture),
                    seed.MaxFoodPriceIndexPermille.ToString(CultureInfo.InvariantCulture),
                    seed.MinFoodPriceIndexPermille.ToString(CultureInfo.InvariantCulture),
                    seed.MaxMaterialsPriceIndexPermille.ToString(CultureInfo.InvariantCulture),
                    seed.MinMaterialsPriceIndexPermille.ToString(CultureInfo.InvariantCulture),
                    seed.MaxCompositePriceIndexPermille.ToString(CultureInfo.InvariantCulture),
                    seed.FinalFunds.ToString(CultureInfo.InvariantCulture),
                    seed.FinalFoodInventory.ToString(CultureInfo.InvariantCulture),
                    seed.FinalSupplies.ToString(CultureInfo.InvariantCulture),
                    seed.FinalLivingMembers.ToString(CultureInfo.InvariantCulture),
                    Bool(seed.FinalFamilyExtinct),
                    Bool(seed.FinalHeirGap),
                    seed.FarmAppointments.ToString(CultureInfo.InvariantCulture),
                    seed.FarmVacancies.ToString(CultureInfo.InvariantCulture),
                    seed.FinalFarmAssignee ?? string.Empty
                };
                foreach (int year in years)
                {
                    M08BCheckpointResult checkpoint = FindCheckpoint(seed, year);
                    if (checkpoint == null || !checkpoint.Reached)
                    {
                        for (int i = 0; i < CheckpointColumns.Length; i++)
                            row.Add(string.Empty);
                        continue;
                    }
                    row.Add(Bool(checkpoint.Reached));
                    row.Add(Bool(checkpoint.Alive));
                    row.Add(checkpoint.LivingMembers.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.MeanLivingAgeTenths.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.Funds.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.FoodInventory.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.Supplies.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.FoodPriceIndexPermille.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.MaterialsPriceIndexPermille.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.CompositePriceIndexPermille.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.Wealth.ToString(CultureInfo.InvariantCulture));
                    row.Add(Bool(checkpoint.HeirGap));
                    row.Add(checkpoint.EventCount.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.EventsPer1000Months.ToString(CultureInfo.InvariantCulture));
                    row.Add(Bool(checkpoint.FamineCollapse));
                    row.Add(Bool(checkpoint.IndustryCollapse));
                    row.Add(Bool(checkpoint.EconomicCollapse));
                    row.Add(Bool(checkpoint.ExtremeInflation));
                    row.Add(checkpoint.FamilyRelationshipAnomalies.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.FamilyRelationshipsSampled.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.PersistedRelationshipViolations.ToString(CultureInfo.InvariantCulture));
                    row.Add(checkpoint.ProgressionDeadlocks.ToString(CultureInfo.InvariantCulture));
                }
                for (int i = 0; i < row.Count; i++)
                    row[i] = EscapeCsv(row[i]);
                csv.Append(string.Join(",", row)).Append('\n');
            }
            return csv.ToString();
        }

        public static string SerializeCheckpointsCsv(IReadOnlyList<M08BSeedResult> seeds,
            IReadOnlyList<int> checkpointYears)
        {
            List<int> years = NormalizeCheckpointYears(checkpointYears);
            var csv = new StringBuilder(8192);
            csv.Append("seed,seedHex,policy,termination,checkpointYear,monthsRequired,reached,alive," +
                       "livingMembers,meanAgeTenths,funds,foodInventory,supplies,foodPriceIndex," +
                       "materialsPriceIndex,compositePriceIndex,wealth,heirGap,eventCount," +
                       "eventsPer1000Months,famineCollapse,industryCollapse,economicCollapse," +
                       "extremeInflation,familyRelationshipAnomalies," +
                       "familyRelationshipsSampled,persistedRelationshipViolations," +
                       "progressionDeadlocks\n");
            if (seeds == null)
                return csv.ToString();

            foreach (M08BSeedResult seed in seeds.OrderBy(item => item.Seed))
            {
                foreach (int year in years)
                {
                    M08BCheckpointResult checkpoint = FindCheckpoint(seed, year);
                    bool reached = checkpoint != null && checkpoint.Reached;
                    var row = new List<string>
                    {
                        seed.Seed.ToString(CultureInfo.InvariantCulture),
                        seed.SeedHex ?? FormatSeedHex(seed.Seed),
                        seed.PolicyId ?? UnmanagedPolicyId,
                        seed.Termination.ToString(),
                        year.ToString(CultureInfo.InvariantCulture),
                        (year * 12).ToString(CultureInfo.InvariantCulture),
                        Bool(reached),
                        Bool(reached && checkpoint.Alive),
                        Number(checkpoint, c => c.LivingMembers, reached),
                        Number(checkpoint, c => c.MeanLivingAgeTenths, reached),
                        Number(checkpoint, c => c.Funds, reached),
                        Number(checkpoint, c => c.FoodInventory, reached),
                        Number(checkpoint, c => c.Supplies, reached),
                        Number(checkpoint, c => c.FoodPriceIndexPermille, reached),
                        Number(checkpoint, c => c.MaterialsPriceIndexPermille, reached),
                        Number(checkpoint, c => c.CompositePriceIndexPermille, reached),
                        Number(checkpoint, c => c.Wealth, reached),
                        Bool(reached && checkpoint.HeirGap),
                        Number(checkpoint, c => c.EventCount, reached),
                        Number(checkpoint, c => c.EventsPer1000Months, reached),
                        Bool(reached && checkpoint.FamineCollapse),
                        Bool(reached && checkpoint.IndustryCollapse),
                        Bool(reached && checkpoint.EconomicCollapse),
                        Bool(reached && checkpoint.ExtremeInflation),
                        Number(checkpoint, c => c.FamilyRelationshipAnomalies, reached),
                        Number(checkpoint, c => c.FamilyRelationshipsSampled, reached),
                        Number(checkpoint, c => c.PersistedRelationshipViolations, reached),
                        Number(checkpoint, c => c.ProgressionDeadlocks, reached)
                    };
                    for (int i = 0; i < row.Count; i++)
                        row[i] = EscapeCsv(row[i]);
                    csv.Append(string.Join(",", row)).Append('\n');
                }
            }
            return csv.ToString();
        }

        private static string Number(M08BCheckpointResult checkpoint, Func<M08BCheckpointResult, int> part,
            bool reached) =>
            reached && checkpoint != null
                ? part(checkpoint).ToString(CultureInfo.InvariantCulture)
                : string.Empty;

        private static M08BCheckpointResult FindCheckpoint(M08BSeedResult seed, int year)
        {
            if (seed.Checkpoints == null)
                return null;
            foreach (M08BCheckpointResult checkpoint in seed.Checkpoints)
            {
                if (checkpoint.Year == year)
                    return checkpoint;
            }
            return null;
        }

        private static string Bool(bool value) => value ? "1" : "0";

        /// <summary>Round-trips a normalized seeds CSV, rejecting malformed ranges or headers.</summary>
        public static bool TryParseSeedsCsv(string csv, int firstSeed, int count, int years,
            IReadOnlyList<int> checkpointYears, out List<M08BSeedResult> seeds, out string error)
        {
            seeds = new List<M08BSeedResult>();
            error = string.Empty;
            if (string.IsNullOrEmpty(csv))
            {
                error = "CSV is empty.";
                return false;
            }

            List<int> expectedYears = NormalizeCheckpointYears(checkpointYears);
            string[] lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int lineIndex = 0;
            while (lineIndex < lines.Length && lines[lineIndex].Length == 0)
                lineIndex++;
            if (lineIndex >= lines.Length)
            {
                error = "CSV has no header.";
                return false;
            }

            List<string> header = ParseCsvLine(lines[lineIndex++]);
            var index = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < header.Count; i++)
                index[header[i]] = i;
            foreach (string column in SeedColumns)
            {
                if (!index.ContainsKey(column))
                {
                    error = "Missing CSV column " + column + ".";
                    return false;
                }
            }
            foreach (int year in expectedYears)
            {
                foreach (string column in CheckpointColumns)
                {
                    if (!index.ContainsKey("y" + year.ToString(CultureInfo.InvariantCulture) + "_" + column))
                    {
                        error = "Missing CSV column y" + year.ToString(CultureInfo.InvariantCulture) +
                            "_" + column + ".";
                        return false;
                    }
                }
            }

            int lastSeed = firstSeed + count - 1;
            try
            {
            for (; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                if (line.Length == 0)
                    continue;
                List<string> fields = ParseCsvLine(line);
                if (fields.Count != header.Count)
                {
                    error = "CSV row " + (lineIndex + 1).ToString(CultureInfo.InvariantCulture) +
                        " has " + fields.Count.ToString(CultureInfo.InvariantCulture) +
                        " fields, expected " + header.Count.ToString(CultureInfo.InvariantCulture) + ".";
                    return false;
                }

                if (!ulong.TryParse(Field(fields, index, "seed"), NumberStyles.Integer,
                        CultureInfo.InvariantCulture, out ulong seedValue))
                {
                    error = "CSV row " + (lineIndex + 1).ToString(CultureInfo.InvariantCulture) +
                        " has an invalid seed.";
                    return false;
                }
                if (seedValue < (ulong)firstSeed || seedValue > (ulong)lastSeed)
                    continue;
                if (!Enum.TryParse(Field(fields, index, "termination"), false,
                        out M08BTerminationReason termination))
                {
                    error = "CSV row " + (lineIndex + 1).ToString(CultureInfo.InvariantCulture) +
                        " has an unknown termination.";
                    return false;
                }

                var item = new M08BSeedResult
                {
                    Seed = seedValue,
                    SeedHex = Field(fields, index, "seedHex"),
                    PolicyId = Field(fields, index, "policy"),
                    Termination = termination,
                    TerminationDetail = Field(fields, index, "terminationDetail"),
                    StartYear = ParseInt(fields, index, "startYear"),
                    StartMonth = ParseInt(fields, index, "startMonth"),
                    EndYear = ParseInt(fields, index, "endYear"),
                    EndMonth = ParseInt(fields, index, "endMonth"),
                    MonthsSimulated = ParseInt(fields, index, "monthsSimulated"),
                    InitialRandomStateHex = Field(fields, index, "initialRandomStateHex"),
                    EventsTriggered = ParseInt(fields, index, "eventsTriggered"),
                    EventDeadlocks = ParseInt(fields, index, "eventDeadlocks"),
                    FamineCollapse = ParseBool(fields, index, "famineCollapse"),
                    LongestFamineMonths = ParseInt(fields, index, "longestFamineMonths"),
                    IndustryCollapse = ParseBool(fields, index, "industryCollapse"),
                    LongestIndustryStallMonths = ParseInt(fields, index, "longestIndustryStallMonths"),
                    EconomicCollapse = ParseBool(fields, index, "economicCollapse"),
                    ExtremeInflation = ParseBool(fields, index, "extremeInflation"),
                    ExtremeInflationMonths = ParseInt(fields, index, "extremeInflationMonths"),
                    PeakFamilyRelationshipAnomalies =
                        ParseInt(fields, index, "peakFamilyRelationshipAnomalies"),
                    FinalFamilyRelationshipAnomalies =
                        ParseInt(fields, index, "finalFamilyRelationshipAnomalies"),
                    FamilyRelationshipsSampled =
                        ParseInt(fields, index, "familyRelationshipsSampled"),
                    PersistedRelationshipViolations =
                        ParseInt(fields, index, "persistedRelationshipViolations"),
                    ProgressionDeadlocks = ParseInt(fields, index, "progressionDeadlocks"),
                    MaxFoodPriceIndexPermille = ParseInt(fields, index, "maxFoodPriceIndex"),
                    MinFoodPriceIndexPermille = ParseInt(fields, index, "minFoodPriceIndex"),
                    MaxMaterialsPriceIndexPermille = ParseInt(fields, index, "maxMaterialsPriceIndex"),
                    MinMaterialsPriceIndexPermille = ParseInt(fields, index, "minMaterialsPriceIndex"),
                    MaxCompositePriceIndexPermille = ParseInt(fields, index, "maxCompositePriceIndex"),
                    FinalFunds = ParseInt(fields, index, "finalFunds"),
                    FinalFoodInventory = ParseInt(fields, index, "finalFoodInventory"),
                    FinalSupplies = ParseInt(fields, index, "finalSupplies"),
                    FinalLivingMembers = ParseInt(fields, index, "finalLivingMembers"),
                    FinalFamilyExtinct = ParseBool(fields, index, "finalFamilyExtinct"),
                    FinalHeirGap = ParseBool(fields, index, "finalHeirGap"),
                    FarmAppointments = ParseInt(fields, index, "farmAppointments"),
                    FarmVacancies = ParseInt(fields, index, "farmVacancies"),
                    FinalFarmAssignee = Field(fields, index, "finalFarmAssignee")
                };

                foreach (int year in expectedYears)
                {
                    string prefix = "y" + year.ToString(CultureInfo.InvariantCulture) + "_";
                    var checkpoint = new M08BCheckpointResult
                    {
                        Year = year,
                        MonthsRequired = checked(year * 12),
                        Reached = ParseBool(fields, index, prefix + "reached"),
                        Alive = ParseBool(fields, index, prefix + "alive"),
                        LivingMembers = ParseInt(fields, index, prefix + "livingMembers"),
                        MeanLivingAgeTenths = ParseInt(fields, index, prefix + "meanAgeTenths"),
                        Funds = ParseInt(fields, index, prefix + "funds"),
                        FoodInventory = ParseInt(fields, index, prefix + "foodInventory"),
                        Supplies = ParseInt(fields, index, prefix + "supplies"),
                        FoodPriceIndexPermille = ParseInt(fields, index, prefix + "foodPriceIndex"),
                        MaterialsPriceIndexPermille = ParseInt(fields, index, prefix + "materialsPriceIndex"),
                        CompositePriceIndexPermille = ParseInt(fields, index, prefix + "compositePriceIndex"),
                        Wealth = ParseInt(fields, index, prefix + "wealth"),
                        HeirGap = ParseBool(fields, index, prefix + "heirGap"),
                        EventCount = ParseInt(fields, index, prefix + "eventCount"),
                        EventsPer1000Months = ParseInt(fields, index, prefix + "eventsPer1000Months"),
                        FamineCollapse = ParseBool(fields, index, prefix + "famineCollapse"),
                        IndustryCollapse = ParseBool(fields, index, prefix + "industryCollapse"),
                        EconomicCollapse = ParseBool(fields, index, prefix + "economicCollapse"),
                        ExtremeInflation = ParseBool(fields, index, prefix + "extremeInflation"),
                        FamilyRelationshipAnomalies =
                            ParseInt(fields, index, prefix + "familyRelationshipAnomalies"),
                        FamilyRelationshipsSampled =
                            ParseInt(fields, index, prefix + "familyRelationshipsSampled"),
                        PersistedRelationshipViolations =
                            ParseInt(fields, index, prefix + "persistedRelationshipViolations"),
                        ProgressionDeadlocks = ParseInt(fields, index, prefix + "progressionDeadlocks")
                    };
                    item.Checkpoints.Add(checkpoint);
                }

                seeds.Add(item);
            }
            }
            catch (Exception exception)
            {
                error = "CSV row parse failed: " + exception.Message;
                seeds = new List<M08BSeedResult>();
                return false;
            }

            if (seeds.Count == 0)
            {
                error = "CSV has no seed rows.";
                return false;
            }
            seeds = seeds.OrderBy(item => item.Seed).ToList();
            return true;
        }

        private static string Field(List<string> fields, Dictionary<string, int> index, string name) =>
            fields[index[name]];

        private static int ParseInt(List<string> fields, Dictionary<string, int> index, string name)
        {
            string text = fields[index[name]];
            if (string.IsNullOrEmpty(text))
                return 0;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                throw new FormatException("CSV field " + name + " is not an integer: " + text);
            return value;
        }

        private static bool ParseBool(List<string> fields, Dictionary<string, int> index, string name)
        {
            string text = fields[index[name]];
            return text == "1" || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            if (value.IndexOf(',') < 0 && value.IndexOf('"') < 0 &&
                value.IndexOf('\n') < 0 && value.IndexOf('\r') < 0)
                return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char character = line[i];
                if (quoted)
                {
                    if (character == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        current.Append(character);
                    }
                    continue;
                }

                if (character == '"' && current.Length == 0)
                {
                    quoted = true;
                }
                else if (character == ',')
                {
                    fields.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(character);
                }
            }
            fields.Add(current.ToString());
            return fields;
        }

        // ---- normalized summary JSON ----------------------------------------------------

        /// <summary>
        /// Deterministic summary over the completed seed rows. Every ratio prints its
        /// numerator and denominator; wall-clock timing stays in the batch log so the same
        /// range always produces the same bytes.
        /// </summary>
        public static string BuildSummaryJson(IReadOnlyList<M08BSeedResult> seeds, int firstSeed,
            int count, int years, IReadOnlyList<int> checkpointYears) =>
            BuildSummaryJson(seeds, firstSeed, count, years, checkpointYears,
                M08BIndustryPolicy.Unmanaged);

        /// <summary>
        /// Deterministic summary over the completed seed rows for one policy. Every ratio prints
        /// its numerator and denominator; wall-clock timing stays in the batch log so the same
        /// range always produces the same bytes.
        /// </summary>
        public static string BuildSummaryJson(IReadOnlyList<M08BSeedResult> seeds, int firstSeed,
            int count, int years, IReadOnlyList<int> checkpointYears, M08BIndustryPolicy policy)
        {
            List<int> yearsList = NormalizeCheckpointYears(checkpointYears);
            List<M08BSeedResult> ordered = seeds == null
                ? new List<M08BSeedResult>()
                : seeds.OrderBy(item => item.Seed).ToList();

            var json = new StringBuilder(16384);
            json.Append("{\n");
            JsonProperty(json, 2, "schema", NormalizedSchema, true);
            JsonProperty(json, 2, "ruleVersion", RuleVersion, true);
            JsonProperty(json, 2, "policy", PolicyIdOf(policy), true);
            JsonProperty(json, 2, "sourceSha256", ComputeSourceSha256(), true);
            TryComputeRuleInputProvenance(out string ruleInputSha256, out int ruleInputFileCount);
            JsonProperty(json, 2, "ruleInputSha256", ruleInputSha256, true);
            json.Append("  \"ruleInputFileCount\":")
                .Append(ruleInputFileCount.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            json.Append("  \"range\": {");
            json.Append("\"firstSeed\":").Append(firstSeed.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"count\":").Append(count.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"years\":").Append(years.ToString(CultureInfo.InvariantCulture));
            json.Append("},\n");
            json.Append("  \"checkpointYears\": [");
            for (int i = 0; i < yearsList.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append(yearsList[i].ToString(CultureInfo.InvariantCulture));
            }
            json.Append("],\n");
            json.Append("  \"definitions\": {\n");
            JsonProperty(json, 4, "wealth",
                "funds + half-up(food inventory * food index / 1000) + half-up(supplies * materials index / 1000); extinct/empty checkpoints excluded",
                true);
            JsonProperty(json, 4, "percentile",
                "nearest rank on the ascending sample: rank = ceil(n * p / 100), clamped to [1, n]; p0 is the minimum and p100 the maximum",
                true);
            JsonProperty(json, 4, "heirGap",
                "living family at checkpoint with zero living eligible successor candidates from SuccessionService", true);
            JsonProperty(json, 4, "heirGapAtStop",
                "stopped seeds whose family is not extinct and still has living members, with zero living eligible successor candidates from SuccessionService. Numerator and denominator use the same live non-extinct population, so an extinct family is excluded from both", true);
            JsonProperty(json, 4, "population",
                "living current-family members from the real succession roster; extinct samples count as zero members", true);
            JsonProperty(json, 4, "meanAge",
                "mean age of living family members in tenths of a year, half-up; alive samples only", true);
            JsonProperty(json, 4, "economicCollapse",
                "12 consecutive months with food unmet >= 1 OR all appointed industries stalled for 12 consecutive months; famine and industry collapse also reported separately. The seed-level flag means the condition held at any point in the run; the per-checkpoint flag means it held in the 12 months ending at that checkpoint",
                true);
            JsonProperty(json, 4, "extremeInflation",
                "settled composite price index at least double its value 12 months earlier (>= +100%). The seed-level flag means any settled month qualified; the per-checkpoint flag means that checkpoint month qualified",
                true);
            JsonProperty(json, 4, "eventFrequency",
                "scheduled events (resolved history + still pending) per 1000 simulated months", true);
            JsonProperty(json, 4, "familyRelationshipAnomaly",
                "GDD 14.2 家族关系异常: a persisted relation from the current head to a living family member whose value sits at the documented hostile bound (-100). Event FamilyRelationshipDelta effects can drive these values, so this is a gameplay observation, not an invariant that validation guarantees. The denominator counts sampled head-to-living-member relations; a zero sample means the metric is unavailable for that sample rather than healthy. Seed-level values are the peak sample count, never a sum",
                true);
            JsonProperty(json, 4, "persistedRelationshipViolations",
                "integrity guard, not the GDD metric: persisted relationship entries that break their own invariant (empty/self endpoint, value outside [-100,100], duplicate family pair). A valid save never contains one, so a nonzero value means the artifact or harness is corrupt",
                true);
            JsonProperty(json, 4, "successionPolicy",
                "approved simulation-only policy: prefer the valid designated heir, otherwise the eligible candidate with the lowest stable PersonId; pick the regent by the same stable-ID rule and complete a due regency through the public session command. A crisis adoption is never accepted, so a family with no lawful candidate keeps the PendingSuccession stop",
                true);
            JsonProperty(json, 4, "industryPolicy",
                "farm-managed-v1 is a simulation-only comparison policy, not player behaviour and not a survival threshold. At each management-stable point after pending events and succession are resolved, a vacant Farm post is filled through GameSession.TryAssignIndustry by trying living family members in ordinal PersonId order until one real-rule appointment succeeds; the post stays vacant when nobody qualifies. Trading House stays vacant, an occupied post is never replaced or unassigned, and the policy consumes no random state. farmVacancyStablePoints is the opportunity count and farmAppointments the accepted appointments behind it",
                true);
            JsonProperty(json, 4, "progressionDeadlock",
                "world progression stopped by an unresolved blocking event that still prevents the calendar advancing. A rule-required pending succession keeps its own stop category and is not a deadlock. A three-refusal advance stall is reported separately as Timeout, and an escaped harness exception as SimulatorFault",
                false);
            json.Append("  },\n");

            json.Append("  \"seedCount\": ").Append(ordered.Count.ToString(CultureInfo.InvariantCulture))
                .Append(",\n");
            json.Append("  \"termination\": {");
            bool firstTermination = true;
            foreach (M08BTerminationReason reason in Enum.GetValues(typeof(M08BTerminationReason)))
            {
                int value = ordered.Count(item => item.Termination == reason);
                if (!firstTermination) json.Append(',');
                firstTermination = false;
                json.Append('"').Append(reason).Append("\":")
                    .Append(value.ToString(CultureInfo.InvariantCulture));
            }
            json.Append(",\"denominator\":").Append(ordered.Count.ToString(CultureInfo.InvariantCulture))
                .Append("},\n");

            json.Append("  \"stoppedSeeds\": [");
            List<M08BSeedResult> stopped = ordered
                .Where(item => item.Termination != M08BTerminationReason.Completed).ToList();
            for (int i = 0; i < stopped.Count; i++)
            {
                M08BSeedResult item = stopped[i];
                if (i > 0) json.Append(',');
                json.Append("\n    {\"seed\":").Append(item.Seed.ToString(CultureInfo.InvariantCulture));
                json.Append(",\"termination\":\"").Append(item.Termination).Append('"');
                json.Append(",\"detail\":\"").Append(JsonEscape(item.TerminationDetail)).Append("\"}");
            }
            if (stopped.Count > 0) json.Append('\n');
            json.Append("  ],\n");

            json.Append("  \"overall\": {\n");
            JsonRatio(json, 4, "famineCollapse", ordered.Count(item => item.FamineCollapse),
                ordered.Count, true);
            JsonRatio(json, 4, "industryCollapse", ordered.Count(item => item.IndustryCollapse),
                ordered.Count, true);
            JsonRatio(json, 4, "economicCollapse", ordered.Count(item => item.EconomicCollapse),
                ordered.Count, true);
            JsonRatio(json, 4, "extremeInflation", ordered.Count(item => item.ExtremeInflation),
                ordered.Count, true);
            JsonRatio(json, 4, "familyRelationshipAnomalySeeds",
                ordered.Count(item => item.PeakFamilyRelationshipAnomalies > 0),
                ordered.Count(item => item.FamilyRelationshipsSampled > 0), true);
            // Both sides describe the same population: stopped seeds whose family is still
            // alive. An extinct family keeps its roster rows, so a living-member filter alone
            // would count extinct seeds in the denominator but never in the numerator.
            JsonRatio(json, 4, "heirGapAtStop",
                ordered.Count(item => item.FinalHeirGap),
                ordered.Count(item => !item.FinalFamilyExtinct && item.FinalLivingMembers > 0),
                true);
            JsonRatio(json, 4, "progressionDeadlockSeeds",
                ordered.Count(item => item.ProgressionDeadlocks > 0), ordered.Count, true);
            int totalEvents = ordered.Sum(item => item.EventsTriggered);
            int totalMonths = ordered.Sum(item => item.MonthsSimulated);
            json.Append("    \"eventFrequency\": {\"events\":").Append(totalEvents)
                .Append(",\"months\":").Append(totalMonths)
                .Append(",\"eventsPer1000Months\":")
                .Append(totalMonths > 0 ? RoundHalfUp((long)totalEvents * 1000L, totalMonths) : 0)
                .Append("},\n");
            json.Append("    \"peakFamilyRelationshipAnomalies\":")
                .Append(ordered.Sum(item => item.PeakFamilyRelationshipAnomalies)).Append(",\n");
            json.Append("    \"persistedRelationshipViolations\":")
                .Append(ordered.Sum(item => item.PersistedRelationshipViolations)).Append(",\n");
            json.Append("    \"industryPolicy\": {\"farmAppointments\":")
                .Append(ordered.Sum(item => item.FarmAppointments))
                .Append(",\"farmVacancyStablePoints\":")
                .Append(ordered.Sum(item => item.FarmVacancies))
                .Append(",\"seedsWithFarmAppointment\":")
                .Append(ordered.Count(item => item.FarmAppointments > 0))
                .Append(",\"denominator\":").Append(ordered.Count).Append("}\n");
            json.Append("  },\n");

            json.Append("  \"checkpoints\": [");
            for (int i = 0; i < yearsList.Count; i++)
            {
                if (i > 0) json.Append(',');
                json.Append('\n');
                AppendCheckpointSummary(json, yearsList[i], ordered);
            }
            if (yearsList.Count > 0) json.Append('\n');
            json.Append("  ]\n");
            json.Append("}\n");
            return json.ToString();
        }

        private static void AppendCheckpointSummary(StringBuilder json, int year,
            IReadOnlyList<M08BSeedResult> ordered)
        {
            List<M08BCheckpointResult> reached = new List<M08BCheckpointResult>();
            foreach (M08BSeedResult seed in ordered)
            {
                M08BCheckpointResult checkpoint = FindCheckpoint(seed, year);
                if (checkpoint != null && checkpoint.Reached)
                    reached.Add(checkpoint);
            }

            List<M08BCheckpointResult> alive = reached.Where(item => item.Alive).ToList();
            List<int> wealth = alive.Select(item => item.Wealth).OrderBy(value => value).ToList();
            List<int> population = reached.Select(item => item.LivingMembers).OrderBy(value => value).ToList();
            List<int> ages = alive.Select(item => item.MeanLivingAgeTenths).OrderBy(value => value).ToList();

            json.Append("    {\"year\":").Append(year.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"monthsRequired\":").Append((year * 12).ToString(CultureInfo.InvariantCulture));
            json.Append(",\"reached\":").Append(reached.Count.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"notReached\":").Append((ordered.Count - reached.Count).ToString(CultureInfo.InvariantCulture));
            json.Append(",\"alive\":").Append(alive.Count.ToString(CultureInfo.InvariantCulture));
            json.Append(",\"emptyCheckpoints\":").Append((reached.Count - alive.Count).ToString(CultureInfo.InvariantCulture));
            json.Append(",\n     \"heirGap\":");
            AppendRatio(json, alive.Count(item => item.HeirGap), alive.Count);
            json.Append(",\n     \"population\":");
            AppendDistribution(json, population, true);
            json.Append(",\n     \"meanLivingAgeTenths\":");
            AppendDistribution(json, ages, false);
            json.Append(",\n     \"wealth\":");
            AppendDistribution(json, wealth, false);
            json.Append(",\n     \"economicCollapse\":");
            AppendRatio(json, reached.Count(item => item.EconomicCollapse), reached.Count);
            json.Append(",\n     \"famineCollapse\":");
            AppendRatio(json, reached.Count(item => item.FamineCollapse), reached.Count);
            json.Append(",\n     \"industryCollapse\":");
            AppendRatio(json, reached.Count(item => item.IndustryCollapse), reached.Count);
            json.Append(",\n     \"extremeInflation\":");
            AppendRatio(json, reached.Count(item => item.ExtremeInflation), reached.Count);
            json.Append(",\n     \"eventFrequency\":");
            int events = reached.Sum(item => item.EventCount);
            int months = reached.Sum(item => item.MonthsRequired);
            json.Append("{\"events\":").Append(events).Append(",\"months\":").Append(months)
                .Append(",\"eventsPer1000Months\":")
                .Append(months > 0 ? RoundHalfUp((long)events * 1000L, months) : 0).Append('}');
            json.Append(",\n     \"familyRelationshipAnomalies\":");
            AppendSampledRatio(json, reached.Count(item => item.FamilyRelationshipAnomalies > 0),
                reached.Count(item => item.FamilyRelationshipsSampled > 0),
                reached.Sum(item => item.FamilyRelationshipsSampled));
            json.Append(",\n     \"persistedRelationshipViolations\":");
            AppendRatio(json, reached.Count(item => item.PersistedRelationshipViolations > 0),
                reached.Count);
            json.Append(",\n     \"progressionDeadlocks\":");
            AppendRatio(json, reached.Count(item => item.ProgressionDeadlocks > 0), reached.Count);
            json.Append('}');
        }

        private static void AppendRatio(StringBuilder json, int numerator, int denominator) =>
            json.Append("{\"numerator\":").Append(numerator)
                .Append(",\"denominator\":").Append(denominator)
                .Append(",\"permille\":")
                .Append(denominator > 0 ? RoundHalfUp((long)numerator * 1000L, denominator) : 0)
                .Append('}');

        /// <summary>Ratio plus the raw sample size, so an empty sample reads as unavailable.</summary>
        private static void AppendSampledRatio(StringBuilder json, int numerator, int denominator,
            int sampled) =>
            json.Append("{\"numerator\":").Append(numerator)
                .Append(",\"denominator\":").Append(denominator)
                .Append(",\"permille\":")
                .Append(denominator > 0 ? RoundHalfUp((long)numerator * 1000L, denominator) : 0)
                .Append(",\"sampled\":").Append(sampled)
                .Append('}');

        private static void JsonRatio(StringBuilder json, int indent, string name, int numerator,
            int denominator, bool trailingComma)
        {
            JsonIndent(json, indent);
            json.Append('"').Append(name).Append("\":");
            AppendRatio(json, numerator, denominator);
            json.Append(trailingComma ? ",\n" : "\n");
        }

        private static void AppendDistribution(StringBuilder json, IReadOnlyList<int> ascending,
            bool histogram)
        {
            if (ascending.Count == 0)
            {
                json.Append("{\"sample\":0}");
                return;
            }
            long sum = 0;
            foreach (int value in ascending) sum += value;
            json.Append("{\"sample\":").Append(ascending.Count);
            json.Append(",\"min\":").Append(ascending[0]);
            json.Append(",\"max\":").Append(ascending[ascending.Count - 1]);
            json.Append(",\"mean\":").Append(RoundHalfUp(sum, ascending.Count));
            json.Append(",\"p10\":").Append(PercentileNearestRank(ascending, 10));
            json.Append(",\"p25\":").Append(PercentileNearestRank(ascending, 25));
            json.Append(",\"p50\":").Append(PercentileNearestRank(ascending, 50));
            json.Append(",\"p75\":").Append(PercentileNearestRank(ascending, 75));
            json.Append(",\"p90\":").Append(PercentileNearestRank(ascending, 90));
            if (histogram)
            {
                json.Append(",\"histogram\":[");
                var counts = new SortedDictionary<int, int>();
                foreach (int value in ascending)
                {
                    counts.TryGetValue(value, out int current);
                    counts[value] = current + 1;
                }
                bool first = true;
                foreach (KeyValuePair<int, int> pair in counts)
                {
                    if (!first) json.Append(',');
                    first = false;
                    json.Append("{\"members\":").Append(pair.Key)
                        .Append(",\"count\":").Append(pair.Value).Append('}');
                }
                json.Append(']');
            }
            json.Append('}');
        }

        private static void JsonProperty(StringBuilder json, int indent, string name, string value,
            bool trailingComma)
        {
            JsonIndent(json, indent);
            json.Append('"').Append(name).Append("\":\"").Append(JsonEscape(value)).Append('"');
            json.Append(trailingComma ? ",\n" : "\n");
        }

        private static void JsonIndent(StringBuilder json, int spaces) => json.Append(' ', spaces);

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var escaped = new StringBuilder(value.Length + 8);
            foreach (char character in value)
            {
                switch (character)
                {
                    case '"': escaped.Append("\\\""); break;
                    case '\\': escaped.Append("\\\\"); break;
                    case '\b': escaped.Append("\\b"); break;
                    case '\f': escaped.Append("\\f"); break;
                    case '\n': escaped.Append("\\n"); break;
                    case '\r': escaped.Append("\\r"); break;
                    case '\t': escaped.Append("\\t"); break;
                    default:
                        if (character < ' ')
                            escaped.Append("\\u").Append(((int)character).ToString("x4",
                                CultureInfo.InvariantCulture));
                        else
                            escaped.Append(character);
                        break;
                }
            }
            return escaped.ToString();
        }
    }
}
