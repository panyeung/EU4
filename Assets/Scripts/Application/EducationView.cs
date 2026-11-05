using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SengokuChronicle.Domain;

namespace SengokuChronicle.Application
{
    /// <summary>One eligible adult family member shown as a mentor option.</summary>
    public readonly struct EducationMentorOption
    {
        public EducationMentorOption(PersonId personId, string displayName, MentorSpecialty specialty)
        {
            PersonId = personId;
            DisplayName = displayName ?? string.Empty;
            Specialty = specialty;
        }

        public PersonId PersonId { get; }
        public string DisplayName { get; }
        public MentorSpecialty Specialty { get; }
        public string SpecialtyName => CharacterGrowthRules.SpecialtyName(Specialty);
    }

    /// <summary>Read-only SC-020 education snapshot for the panel and tests.</summary>
    public sealed class EducationView
    {
        public bool HasLearner { get; private set; }
        public PersonId LearnerId { get; private set; }
        public string LearnerName { get; private set; } = string.Empty;
        public int Age { get; private set; }
        public int AgeYear { get; private set; }
        public bool IsEligible { get; private set; }
        public bool CanEdit { get; private set; }
        public string BlockReason { get; private set; } = string.Empty;
        public CharacterGrowthValues Values { get; private set; }
        public bool HasPlan { get; private set; }
        public EducationYearPlan Plan { get; private set; }
        public string PlanMentorName { get; private set; } = string.Empty;
        public bool HasResult { get; private set; }
        public EducationYearResult Result { get; private set; }
        public string LastMessage { get; private set; } = string.Empty;

        public string ValuesLine => Values.ToString();

        public string OverviewLine
        {
            get
            {
                if (!HasLearner)
                    return "当前没有可展示的家族成员。";
                string ageLine = Age.ToString(CultureInfo.InvariantCulture) + " 岁";
                if (!IsEligible)
                    ageLine += "（已成年，儿童年度培养已结束）";
                return LearnerName + "（" + ageLine + "）｜" + Values.ToString();
            }
        }

        public string PlanLine
        {
            get
            {
                if (!HasLearner)
                    return string.Empty;
                if (HasPlan)
                {
                    string mentor = Plan.IsFreeGrowth
                        ? "无导师"
                        : PlanMentorName + "（" + CharacterGrowthRules
                            .SpecialtyName(EducationSpecialty(Plan.Mentor)) + "）";
                    return "本年计划：" + CharacterGrowthRules.DirectionName(Plan.Direction) +
                           "／导师 " + mentor;
                }
                if (!IsEligible)
                    return "儿童年度培养已结束。";
                return "本年尚未提交培养计划；未提交的年度按自由成长结算。";
            }
        }

        public string ResultLine
        {
            get
            {
                if (!HasResult)
                    return IsEligible ? "本年度尚未结算。" : "尚无培养结算记录。";
                string mentor = Result.Direction == EducationDirection.FreeGrowth ||
                                Result.Mentor.IsEmpty
                    ? "无导师"
                    : NameOf(Result.Mentor);
                return "最近结算：第 " + AgeYear.ToString(CultureInfo.InvariantCulture) + " 年 " +
                       CharacterGrowthRules.DirectionName(Result.Direction) + "／导师 " + mentor +
                       "｜" + Result.Explanation;
            }
        }

        public static EducationView Build(SuccessionState succession, CharacterDevelopmentState development,
            GameDate date, bool canEdit, string blockReason, string lastMessage)
        {
            var view = new EducationView
            {
                CanEdit = canEdit,
                BlockReason = blockReason ?? string.Empty,
                LastMessage = lastMessage ?? string.Empty
            };
            if (development == null || succession == null || succession.World == null)
                return view;

            PersonId learner = SelectLearner(succession, development, date, out int age);
            if (learner.IsEmpty)
                return view;

            view.HasLearner = true;
            view.LearnerId = learner;
            view.LearnerName = NameOf(succession, learner);
            view.Age = age;
            view.IsEligible = age >= SC020EducationScenario.MinimumLearnerAge &&
                              age <= SC020EducationScenario.MaximumLearnerAge;
            view.AgeYear = view.IsEligible ? age : Math.Min(Math.Max(age, 0),
                SC020EducationScenario.MaximumLearnerAge);
            development.TryGetValues(learner, out CharacterGrowthValues values);
            view.Values = values;

            if (development.TryGetPlan(learner, view.AgeYear, out EducationYearPlan plan) ||
                TryGetLatestPlan(development, learner, out plan))
            {
                view.HasPlan = true;
                view.Plan = plan;
                view.PlanMentorName = plan.Mentor.IsEmpty ? string.Empty : NameOf(succession, plan.Mentor);
            }

            if (development.TryGetResult(learner, view.AgeYear, out EducationYearResult result) ||
                TryGetLatestResult(development, learner, out result))
            {
                view.HasResult = true;
                view.Result = result;
            }

            return view;
        }

        private static PersonId SelectLearner(SuccessionState succession,
            CharacterDevelopmentState development, GameDate date, out int age)
        {
            PersonId planOrResultLearner = default;
            PersonId minorLearner = default;
            PersonId firstLearner = default;
            int minorAge = 0;

            foreach (PersonId personId in development.PersonIds)
            {
                if (!succession.World.Family.IsMember(personId) ||
                    !succession.World.TryGetLife(personId, out SuccessionPersonLife life) ||
                    !life.Alive)
                    continue;
                if (firstLearner.IsEmpty)
                    firstLearner = personId;

                int personAge = life.AgeAt(date);
                bool hasPlan = development.PlannedAgeYears(personId).Count > 0;
                bool hasResult = development.SettledAgeYears(personId).Count > 0;
                if (planOrResultLearner.IsEmpty && (hasPlan || hasResult))
                    planOrResultLearner = personId;
                if (minorLearner.IsEmpty &&
                    personAge >= SC020EducationScenario.MinimumLearnerAge &&
                    personAge <= SC020EducationScenario.MaximumLearnerAge)
                {
                    minorLearner = personId;
                    minorAge = personAge;
                }
            }

            if (!planOrResultLearner.IsEmpty)
            {
                succession.World.TryGetLife(planOrResultLearner, out SuccessionPersonLife life);
                age = life.AgeAt(date);
                return planOrResultLearner;
            }
            if (!minorLearner.IsEmpty)
            {
                age = minorAge;
                return minorLearner;
            }
            if (!firstLearner.IsEmpty)
            {
                succession.World.TryGetLife(firstLearner, out SuccessionPersonLife life);
                age = life.AgeAt(date);
                return firstLearner;
            }

            age = 0;
            return default;
        }

        private static bool TryGetLatestPlan(CharacterDevelopmentState development, PersonId personId,
            out EducationYearPlan plan)
        {
            IReadOnlyList<int> years = development.PlannedAgeYears(personId);
            for (int i = years.Count - 1; i >= 0; i--)
                if (development.TryGetPlan(personId, years[i], out plan))
                    return true;
            plan = default;
            return false;
        }

        private static bool TryGetLatestResult(CharacterDevelopmentState development, PersonId personId,
            out EducationYearResult result)
        {
            IReadOnlyList<int> years = development.SettledAgeYears(personId);
            for (int i = years.Count - 1; i >= 0; i--)
                if (development.TryGetResult(personId, years[i], out result))
                    return true;
            result = default;
            return false;
        }

        private static MentorSpecialty EducationSpecialty(PersonId mentorId) =>
            SC020EducationScenario.SpecialtyOf(mentorId);

        private static string NameOf(SuccessionState succession, PersonId personId) =>
            succession.World.TryGetPerson(personId, out PersonState person) &&
            !string.IsNullOrWhiteSpace(person.DisplayName)
                ? person.DisplayName
                : personId.Value;

        private static string NameOf(PersonId personId) =>
            SC020EducationScenario.MentorName(personId);
    }

    /// <summary>Read-only preview of one education direction and mentor combination.</summary>
    public readonly struct EducationPreview
    {
        public EducationPreview(bool succeeded, string error, EducationDirection direction,
            PersonId mentor, string mentorName, CharacterGrowthValues before,
            CharacterGrowthValues after, string explanation)
        {
            Succeeded = succeeded;
            Error = error ?? string.Empty;
            Direction = direction;
            Mentor = mentor;
            MentorName = mentorName ?? string.Empty;
            Before = before;
            After = after;
            Explanation = explanation ?? string.Empty;
        }

        public bool Succeeded { get; }
        public string Error { get; }
        public EducationDirection Direction { get; }
        public PersonId Mentor { get; }
        public string MentorName { get; }
        public CharacterGrowthValues Before { get; }
        public CharacterGrowthValues After { get; }
        public string Explanation { get; }

        public string PreviewLine
        {
            get
            {
                if (!Succeeded)
                    return string.IsNullOrEmpty(Error) ? "无法预览当前培养方案。" : Error;
                var builder = new StringBuilder(160);
                builder.Append("预计：" )
                    .Append(CharacterGrowthRules.DirectionName(Direction))
                    .Append("｜导师 ")
                    .Append(Direction == EducationDirection.FreeGrowth || Mentor.IsEmpty
                        ? "无导师"
                        : MentorName)
                    .Append('｜');
                AppendAttribute(builder, GrowthAttribute.Ninjutsu);
                AppendAttribute(builder, GrowthAttribute.MartialArts);
                AppendAttribute(builder, GrowthAttribute.Scholarship);
                AppendAttribute(builder, GrowthAttribute.Management);
                AppendAttribute(builder, GrowthAttribute.Social);
                builder.Append('｜').Append(Explanation);
                return builder.ToString();
            }
        }

        private void AppendAttribute(StringBuilder builder, GrowthAttribute attribute)
        {
            int before = Before.Get(attribute);
            int after = After.Get(attribute);
            builder.Append(CharacterGrowthRules.AttributeName(attribute)).Append(' ')
                .Append(before.ToString(CultureInfo.InvariantCulture));
            if (after != before)
                builder.Append(" → ").Append(after.ToString(CultureInfo.InvariantCulture));
            builder.Append("　");
        }
    }

    /// <summary>Explicit result code for one plan submission; never a silent failure.</summary>
    public enum EducationPlanOutcome
    {
        Succeeded = 0,
        Blocked = 1,
        InvalidLearner = 2,
        InvalidAge = 3,
        InvalidDirection = 4,
        InvalidMentor = 5,
        AlreadySettled = 6,
        InvalidState = 7
    }

    public readonly struct EducationPlanCommandResult
    {
        public EducationPlanCommandResult(bool succeeded, EducationPlanOutcome outcome, string message,
            EducationYearPlan plan)
        {
            Succeeded = succeeded;
            Outcome = outcome;
            Message = message ?? string.Empty;
            Plan = plan;
        }

        public bool Succeeded { get; }
        public EducationPlanOutcome Outcome { get; }
        public string Message { get; }
        public EducationYearPlan Plan { get; }

        public static EducationPlanCommandResult Failure(EducationPlanOutcome outcome, string message) =>
            new EducationPlanCommandResult(false, outcome, message, default);
    }
}
