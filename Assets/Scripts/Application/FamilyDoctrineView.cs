using System;
using System.Collections.Generic;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>Read-only presentation of one of the three family creeds.</summary>
    public readonly struct FamilyCreedOption
    {
        public FamilyCreedOption(FamilyCreedKind kind, string id, string name, string councilName,
            string councilEffect, bool isCurrent)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            CouncilName = councilName ?? string.Empty;
            CouncilEffect = councilEffect ?? string.Empty;
            IsCurrent = isCurrent;
        }

        public FamilyCreedKind Kind { get; }
        public string Id { get; }
        public string Name { get; }
        public string CouncilName { get; }
        public string CouncilEffect { get; }
        public bool IsCurrent { get; }

        public string Label => IsCurrent ? Name + "（当前）" : Name;
    }

    /// <summary>Read-only presentation of one personal ambition choice.</summary>
    public readonly struct FamilyAmbitionOption
    {
        public FamilyAmbitionOption(FamilyAmbitionKind kind, string id, string name,
            string description, int target, bool isSelected, int progress, bool isComplete)
        {
            Kind = kind;
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Description = description ?? string.Empty;
            Target = target;
            IsSelected = isSelected;
            Progress = progress;
            IsComplete = isComplete;
        }

        public FamilyAmbitionKind Kind { get; }
        public string Id { get; }
        public string Name { get; }
        public string Description { get; }
        public int Target { get; }
        public bool IsSelected { get; }
        public int Progress { get; }
        public bool IsComplete { get; }

        public string Label => IsSelected ? Name + "（当前）" : Name;

        public string ProgressLine
        {
            get
            {
                if (!IsSelected)
                    return "未选定 · 目标 " + Target;
                return (IsComplete ? "已完成 " : "进行中 ") + Progress + "/" + Target;
            }
        }
    }

    /// <summary>
    /// Read-only SC-024 projection for the family-doctrine panel. It only maps domain and
    /// session state into Chinese display text; it owns no rules, save state, or command
    /// execution. The panel calls the same <see cref="GameSession"/> command the view
    /// describes, so the projection can never change creed or ambition by itself.
    /// </summary>
    public sealed class FamilyDoctrineView
    {
        public bool IsScenarioActive { get; private set; }
        public FamilyCreedKind Creed { get; private set; }
        public string CreedId { get; private set; } = string.Empty;
        public string CreedName { get; private set; } = string.Empty;
        public string CouncilName { get; private set; } = string.Empty;
        public string CouncilEffect { get; private set; } = string.Empty;
        public PersonId HeadId { get; private set; }
        public string HeadDisplayName { get; private set; } = string.Empty;
        public string LastMessage { get; private set; } = string.Empty;
        public bool HasCurrentAmbition { get; private set; }
        public bool CanStartAmbition { get; private set; }
        public string BlockReason { get; private set; } = string.Empty;
        public IReadOnlyList<FamilyCreedOption> Creeds { get; private set; } =
            Array.Empty<FamilyCreedOption>();
        public IReadOnlyList<FamilyAmbitionOption> Ambitions { get; private set; } =
            Array.Empty<FamilyAmbitionOption>();

        public string AmbitionProgressLine
        {
            get
            {
                for (int i = 0; i < Ambitions.Count; i++)
                {
                    FamilyAmbitionOption option = Ambitions[i];
                    if (option.IsSelected)
                        return "当前抱负：" + option.Name + " " + option.ProgressLine;
                }
                return "当前抱负：尚未选定。";
            }
        }

        /// <summary>
        /// Builds the projection from a live session through its public read-only surface.
        /// The route is read from a capture so a normal SC-013 game cannot present the
        /// SC-024 command as available.
        /// </summary>
        public static FamilyDoctrineView Build(GameSession session)
        {
            if (session == null)
            {
                FamilyDoctrineView missing = Build(null, FamilyCreedKind.RighteousnessFirst, null,
                    true, "缺少 GameSession。");
                missing.CanStartAmbition = false;
                missing.BlockReason = "缺少 GameSession。";
                return missing;
            }

            bool active = string.Equals(session.CaptureSave()?.activeEventScenarioId,
                SC024FamilyDoctrineScenario.ScenarioId, StringComparison.Ordinal);
            return Build(session.Succession, session.CurrentFamilyCreed,
                session.CurrentHeadAmbition, active, session.LastFamilyDoctrineMessage);
        }

        /// <summary>Pure projection seam used by tests and UI code that already holds state.</summary>
        public static FamilyDoctrineView Build(SuccessionState succession, FamilyCreedKind creed,
            FamilyAmbitionRecord currentHeadAmbition, bool isScenarioActive, string lastMessage)
        {
            var view = new FamilyDoctrineView
            {
                Creed = creed,
                CreedId = CreedIdOf(creed),
                CreedName = CreedNameOf(creed),
                CouncilName = CouncilNameOf(creed),
                CouncilEffect = CouncilEffectOf(creed),
                IsScenarioActive = isScenarioActive,
                LastMessage = lastMessage ?? string.Empty,
                HasCurrentAmbition = currentHeadAmbition != null
            };

            view.Creeds = BuildCreedOptions(creed);
            PersonId headId = succession != null ? succession.HeadPersonId : default;
            view.HeadId = headId;
            view.HeadDisplayName = PersonDisplayName(succession, headId);
            view.Ambitions = BuildAmbitionOptions(currentHeadAmbition);

            if (!isScenarioActive)
            {
                view.CanStartAmbition = false;
                view.BlockReason = "当前不在 SC-024 家训示范中。";
            }
            else if (headId.IsEmpty)
            {
                view.CanStartAmbition = false;
                view.BlockReason = "当前没有可选定抱负的家主。";
            }
            else if (currentHeadAmbition != null)
            {
                view.CanStartAmbition = false;
                view.BlockReason = "当前家主已选定抱负，不能更换。";
            }
            else
            {
                view.CanStartAmbition = true;
                view.BlockReason = string.Empty;
            }

            return view;
        }

        public static string CreedIdOf(FamilyCreedKind creed) =>
            Enum.IsDefined(typeof(FamilyCreedKind), creed)
                ? Enum.GetName(typeof(FamilyCreedKind), creed)
                : "UnknownCreed";

        public static string CreedNameOf(FamilyCreedKind creed)
        {
            switch (creed)
            {
                case FamilyCreedKind.SecrecyFirst: return "守秘为先";
                case FamilyCreedKind.RighteousnessFirst: return "义理至上";
                case FamilyCreedKind.MeritFirst: return "唯才是举";
                default: return "未知家训";
            }
        }

        public static string CouncilNameOf(FamilyCreedKind creed)
        {
            switch (creed)
            {
                case FamilyCreedKind.SecrecyFirst: return "小范围议事";
                case FamilyCreedKind.RighteousnessFirst: return "各房先开口";
                case FamilyCreedKind.MeritFirst: return "按所长分工";
                default: return "未知议事方式";
            }
        }

        public static string CouncilEffectOf(FamilyCreedKind creed)
        {
            switch (creed)
            {
                case FamilyCreedKind.SecrecyFirst: return "正綱关系 +5，信正关系 -5";
                case FamilyCreedKind.RighteousnessFirst: return "正綱关系 +5，信正关系 +5";
                case FamilyCreedKind.MeritFirst: return "正綱关系 -5，信正关系 +5";
                default: return "无议事效果";
            }
        }

        public static string AmbitionNameOf(FamilyAmbitionKind kind)
        {
            switch (kind)
            {
                case FamilyAmbitionKind.DevelopHousehold: return "发展家业";
                case FamilyAmbitionKind.PassDownTechnique: return "传承家技";
                case FamilyAmbitionKind.EducateHeir: return "培养继承人";
                default: return "未知抱负";
            }
        }

        public static string AmbitionIdOf(FamilyAmbitionKind kind) =>
            Enum.IsDefined(typeof(FamilyAmbitionKind), kind)
                ? Enum.GetName(typeof(FamilyAmbitionKind), kind)
                : "UnknownAmbition";

        public static string AmbitionDescriptionOf(FamilyAmbitionKind kind)
        {
            switch (kind)
            {
                case FamilyAmbitionKind.DevelopHousehold:
                    return "通过产业月结推进，累计 3 个有效月份完成。";
                case FamilyAmbitionKind.PassDownTechnique:
                    return "通过新完成家族技艺推进，目标 1 项。";
                case FamilyAmbitionKind.EducateHeir:
                    return "通过指定继承人的教育成长推进，目标 5 点。";
                default:
                    return "未知目标。";
            }
        }

        public static int AmbitionTargetOf(FamilyAmbitionKind kind)
        {
            switch (kind)
            {
                case FamilyAmbitionKind.DevelopHousehold:
                    return FamilyDoctrineState.DevelopHouseholdTarget;
                case FamilyAmbitionKind.PassDownTechnique:
                    return FamilyDoctrineState.PassDownTechniqueTarget;
                case FamilyAmbitionKind.EducateHeir:
                    return FamilyDoctrineState.EducateHeirTarget;
                default:
                    return 0;
            }
        }

        private static IReadOnlyList<FamilyCreedOption> BuildCreedOptions(FamilyCreedKind current)
        {
            var options = new List<FamilyCreedOption>(3)
            {
                CreateCreedOption(FamilyCreedKind.SecrecyFirst, current),
                CreateCreedOption(FamilyCreedKind.RighteousnessFirst, current),
                CreateCreedOption(FamilyCreedKind.MeritFirst, current)
            };
            return options.AsReadOnly();
        }

        private static FamilyCreedOption CreateCreedOption(FamilyCreedKind kind,
            FamilyCreedKind current) =>
            new FamilyCreedOption(kind, CreedIdOf(kind), CreedNameOf(kind), CouncilNameOf(kind),
                CouncilEffectOf(kind), kind == current);

        private static IReadOnlyList<FamilyAmbitionOption> BuildAmbitionOptions(
            FamilyAmbitionRecord current)
        {
            var options = new List<FamilyAmbitionOption>(3);
            AddAmbition(options, FamilyAmbitionKind.DevelopHousehold, current);
            AddAmbition(options, FamilyAmbitionKind.PassDownTechnique, current);
            AddAmbition(options, FamilyAmbitionKind.EducateHeir, current);
            return options.AsReadOnly();
        }

        private static void AddAmbition(List<FamilyAmbitionOption> options,
            FamilyAmbitionKind kind, FamilyAmbitionRecord current)
        {
            bool selected = current != null && current.Kind == kind;
            int progress = selected ? current.Progress : 0;
            bool complete = selected && current.IsComplete;
            options.Add(new FamilyAmbitionOption(kind, AmbitionIdOf(kind), AmbitionNameOf(kind),
                AmbitionDescriptionOf(kind), AmbitionTargetOf(kind), selected, progress, complete));
        }

        private static string PersonDisplayName(SuccessionState succession, PersonId personId)
        {
            if (succession == null || succession.World == null || personId.IsEmpty)
                return string.Empty;
            return succession.World.TryGetPerson(personId, out PersonState person) &&
                   !string.IsNullOrWhiteSpace(person.DisplayName)
                ? person.DisplayName
                : string.Empty;
        }
    }
}
