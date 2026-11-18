using System.Collections.Generic;
using UnityEngine;
using SengokuChronicle.Presentation;

namespace SengokuChronicle.Editor
{
    /// <summary>
    /// One depth order for the full-screen Canvas overlays. Unity renders later Canvas siblings
    /// above earlier ones, and four generators place roots in that band:
    /// <c>AddM02DailyActionPanel</c> moves the game HUD beneath its page,
    /// <c>AddM03NpcAiInspector</c> opens the read-only inspector over that page,
    /// <c>AddSC010SuccessionPanel</c> raises a pending succession decision over the campaign
    /// review, and <c>AddM08CampaignReviewPanel</c> inserts the campaign review beneath the
    /// global title. Each used to carry its own idea of the band, so running the M02 updater
    /// after the M08 one moved <c>M08CampaignReviewRoot</c> beneath <c>M02DailyActionRoot</c>
    /// and an open review rendered under the page. Every updater now derives its placement
    /// from <see cref="RankOf"/>, so the resulting order depends on the scene, not on which
    /// updater ran last.
    /// </summary>
    public static class CanvasOverlayOrder
    {
        public const string DailyActionRootName = AddM02DailyActionPanel.RootName;
        public const string InspectorRootName = AddM03NpcAiInspector.RootName;
        public const string CampaignReviewRootName = CampaignReviewPanel.RootObjectName;
        public const string SuccessionRootName = AddSC010SuccessionPanel.RootName;
        public const string TitleRootName = AddM6TitleScreen.RootName;
        public const string EventChoiceRootName = AddM6TitleScreen.EventChoiceRootName;
        public const string TacticalOverlayName = TacticalMissionPanel.OverlayObjectName;

        /// <summary>
        /// Ranks from the HUD band upward. Every unranked Canvas child renders beneath all of
        /// these; the campaign review must stay above the M02 page and the M03 inspector, a
        /// pending succession decision renders above the review, and the full-screen M6 title
        /// keeps the event choice and the M06 tactical modal above it, with the tactical modal
        /// last. The succession panel's own updater places it immediately below the title, so
        /// this table and that updater agree without either calling the other.
        /// </summary>
        private static readonly string[] RankedRoots =
        {
            DailyActionRootName,
            InspectorRootName,
            CampaignReviewRootName,
            SuccessionRootName,
            TitleRootName,
            EventChoiceRootName,
            TacticalOverlayName
        };

        /// <summary>Shared error when two ranked roots are saved out of the depth order.</summary>
        public const string RankOrderError =
            "Canvas overlay roots must follow the shared depth order: " + DailyActionRootName +
            ", " + InspectorRootName + ", " + CampaignReviewRootName + ", " +
            SuccessionRootName + ", " + TitleRootName + ", " + EventChoiceRootName + ", " +
            TacticalOverlayName + ".";

        /// <summary>Shared errors for the campaign review's own part of the band.</summary>
        public const string CampaignReviewAboveDailyActionError =
            "Campaign review root must render above the M02 daily action page.";
        public const string CampaignReviewAboveInspectorError =
            "Campaign review root must render above the M03 read-only inspector.";

        /// <summary>Rank of a ranked overlay root, or -1 when the name is outside the band.</summary>
        public static int RankOf(string name)
        {
            for (int i = 0; i < RankedRoots.Length; i++)
                if (RankedRoots[i] == name)
                    return i;
            return -1;
        }

        public static bool IsRanked(Transform child) =>
            child != null && RankOf(child.name) >= 0;

        /// <summary>
        /// Places one ranked root in its rank slot and leaves every other child where it is.
        /// The campaign review and the M03 inspector use this when they are created, so moving
        /// them never shifts the title, the tactical modal or the Undo record of the roots
        /// around them.
        /// </summary>
        public static void PlaceAtRank(Transform canvas, Transform target)
        {
            if (canvas == null || target == null || target.parent != canvas)
                return;
            int rank = RankOf(target.name);
            if (rank < 0)
                return;

            var others = new List<Transform>(canvas.childCount);
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (child != target)
                    others.Add(child);
            }

            // Insert before the first root that renders above this rank, or append when it is
            // the highest-ranked root in the band. Transform.SetSiblingIndex takes the index in
            // the child list without the moving root, which is exactly this slot.
            int slot = others.Count;
            for (int i = 0; i < others.Count; i++)
            {
                if (RankOf(others[i].name) > rank)
                {
                    slot = i;
                    break;
                }
            }
            target.SetSiblingIndex(slot);
        }

        /// <summary>
        /// Puts every ranked root into rank order while leaving unranked roots in their current
        /// slots. The M02 generator calls this after it moves the HUD beneath its page, and the
        /// M08 repair path calls it to fix a root order saved by an older updater.
        /// </summary>
        public static void OrderRankedBand(Transform canvas)
        {
            if (canvas == null)
                return;
            int count = canvas.childCount;
            if (count < 2)
                return;

            var desired = new List<Transform>(count);
            var ranked = new List<Transform>(RankedRoots.Length);
            for (int i = 0; i < count; i++)
            {
                Transform child = canvas.GetChild(i);
                desired.Add(child);
                if (IsRanked(child))
                    ranked.Add(child);
            }
            if (ranked.Count < 2)
                return;

            ranked.Sort((left, right) => RankOf(left.name).CompareTo(RankOf(right.name)));
            int next = 0;
            for (int i = 0; i < desired.Count; i++)
                if (IsRanked(desired[i]))
                    desired[i] = ranked[next++];
            for (int i = 0; i < desired.Count; i++)
            {
                if (desired[i].GetSiblingIndex() == i)
                    continue;
                desired[i].SetSiblingIndex(i);
            }
        }

        /// <summary>
        /// Puts every ranked root in rank order after every unranked root, preserving the
        /// relative order inside each group. This is the layout
        /// <c>AddM02DailyActionPanel.PlaceAboveGameHud</c> produces and the one the scene
        /// validator expects: game HUD roots render beneath the full-screen overlay band, so
        /// the M02 page still covers the HUD it owns. The scene generator calls this after a
        /// create or a legacy-page replacement; the minimal repair path uses
        /// <see cref="RepairBand"/> instead.
        /// </summary>
        public static void PlaceBandAboveUnranked(Transform canvas)
        {
            if (canvas == null)
                return;
            var unranked = new List<Transform>(canvas.childCount);
            var ranked = new List<Transform>(canvas.childCount);
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (IsRanked(child))
                    ranked.Add(child);
                else
                    unranked.Add(child);
            }
            ranked.Sort((left, right) => RankOf(left.name).CompareTo(RankOf(right.name)));

            int next = 0;
            for (int i = 0; i < unranked.Count; i++)
            {
                if (unranked[i].GetSiblingIndex() == next)
                {
                    next++;
                    continue;
                }
                unranked[i].SetSiblingIndex(next++);
            }
            for (int i = 0; i < ranked.Count; i++)
            {
                if (ranked[i].GetSiblingIndex() == next)
                {
                    next++;
                    continue;
                }
                ranked[i].SetSiblingIndex(next++);
            }
        }

        /// <summary>
        /// Puts only the ranked roots that are out of order back into rank order. A scene can
        /// carry one misplaced root (an older updater saved the review above the title, or the
        /// succession panel predates the shared band) while every other root is correct; moving
        /// all of them would bury the M02 page beneath game HUD it must cover and would shift
        /// roots a user placed on purpose. The longest already-ascending run of ranked roots
        /// stays where it is and each remaining root is inserted in its rank slot, so the
        /// smallest number of roots moves. Unranked roots never move.
        /// </summary>
        public static void RepairBand(Transform canvas)
        {
            if (canvas == null)
                return;
            var ranked = new List<Transform>(canvas.childCount);
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                if (IsRanked(child))
                    ranked.Add(child);
            }
            if (ranked.Count < 2)
                return;

            bool[] keep = LongestAscendingRanks(ranked);
            var moving = new List<Transform>(ranked.Count);
            for (int i = 0; i < ranked.Count; i++)
                if (!keep[i])
                    moving.Add(ranked[i]);
            moving.Sort((left, right) => RankOf(left.name).CompareTo(RankOf(right.name)));
            for (int i = 0; i < moving.Count; i++)
                PlaceAtRank(canvas, moving[i]);
        }

        /// <summary>
        /// Marks the longest run of ranked children whose ranks already ascend, so
        /// <see cref="RepairBand"/> can leave exactly those roots untouched.
        /// </summary>
        private static bool[] LongestAscendingRanks(List<Transform> ranked)
        {
            int count = ranked.Count;
            var length = new int[count];
            var previous = new int[count];
            int best = 0;
            for (int i = 0; i < count; i++)
            {
                length[i] = 1;
                previous[i] = -1;
                int rank = RankOf(ranked[i].name);
                for (int j = 0; j < i; j++)
                {
                    if (RankOf(ranked[j].name) < rank && length[j] + 1 > length[i])
                    {
                        length[i] = length[j] + 1;
                        previous[i] = j;
                    }
                }
                if (length[i] > length[best])
                    best = i;
            }

            var keep = new bool[count];
            for (int index = best; index >= 0; index = previous[index])
                keep[index] = true;
            return keep;
        }

        /// <summary>True when the ranked roots already render in the shared order.</summary>
        public static bool IsBandOrdered(Transform canvas)
        {
            if (canvas == null)
                return true;
            int previous = -1;
            for (int i = 0; i < canvas.childCount; i++)
            {
                int rank = RankOf(canvas.GetChild(i).name);
                if (rank < 0)
                    continue;
                if (rank < previous)
                    return false;
                previous = rank;
            }
            return true;
        }

        /// <summary>
        /// Reports the first pair of ranked roots saved in the wrong depth order. The message is
        /// shared so the scene validator and both panel validators agree on the contract.
        /// </summary>
        public static bool ValidateBand(Transform canvas, out string error)
        {
            error = null;
            if (canvas == null)
                return true;
            int previous = -1;
            string previousName = null;
            for (int i = 0; i < canvas.childCount; i++)
            {
                Transform child = canvas.GetChild(i);
                int rank = RankOf(child.name);
                if (rank < 0)
                    continue;
                if (rank < previous)
                {
                    error = RankOrderError + " " + child.name + " renders above " +
                            previousName + ".";
                    return false;
                }
                previous = rank;
                previousName = child.name;
            }
            return true;
        }
    }
}
