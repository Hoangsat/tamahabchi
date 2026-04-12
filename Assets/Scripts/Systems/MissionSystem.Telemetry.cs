using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public partial class MissionSystem
{
    private MissionGenerationDebugSnapshot lastGenerationDebugSnapshot;
    private MissionGenerationDebugSnapshot activeGenerationDebugSnapshot;
    private string activeCompositionAttemptLabel = string.Empty;
    private bool missionDebugLoggingEnabled;

    private void BeginGenerationDebugSnapshot(string resetKey, string generationReason, List<SkillEntry> availableSkills, MissionDecayDebugSummary decaySummary)
    {
        activeGenerationDebugSnapshot = new MissionGenerationDebugSnapshot
        {
            generationKey = Guid.NewGuid().ToString("N"),
            resetKey = resetKey ?? string.Empty,
            generatedAtUtc = TimeService.GetUtcNow().ToString("O"),
            generationReason = generationReason ?? string.Empty,
            personalizationSummary = CreatePersonalizationSummary(availableSkills, decaySummary != null && decaySummary.decayApplied),
            decaySummary = decaySummary ?? new MissionDecayDebugSummary()
        };
    }

    private void FinalizeGenerationDebugSnapshot()
    {
        if (activeGenerationDebugSnapshot == null)
        {
            return;
        }

        activeGenerationDebugSnapshot.compactReport = BuildCompactDebugReport(activeGenerationDebugSnapshot);
        lastGenerationDebugSnapshot = activeGenerationDebugSnapshot.Clone();
        LogGenerationDebugReportIfEnabled(lastGenerationDebugSnapshot);
        activeGenerationDebugSnapshot = null;
        activeCompositionAttemptLabel = string.Empty;
    }

    private void RecordDifficultyPlan(List<MissionDifficulty> difficulties)
    {
        if (activeGenerationDebugSnapshot == null)
        {
            return;
        }

        activeGenerationDebugSnapshot.difficultyPlan.Clear();
        if (difficulties == null)
        {
            return;
        }

        for (int i = 0; i < difficulties.Count; i++)
        {
            activeGenerationDebugSnapshot.difficultyPlan.Add(difficulties[i].ToString());
        }
    }

    private void RecordDebugStep(string phase, string detail)
    {
        if (activeGenerationDebugSnapshot == null)
        {
            return;
        }

        activeGenerationDebugSnapshot.steps.Add(new MissionGenerationStepDebugInfo
        {
            phase = phase ?? string.Empty,
            detail = detail ?? string.Empty
        });
    }

    private MissionCandidateDebugInfo CreateCandidateDebugInfo(
        string phase,
        int slotIndex,
        string candidateKey,
        string missionType,
        MissionCategory category,
        MissionDifficulty difficulty,
        SkillEntry skill,
        MissionWeightBreakdown weightBreakdown,
        string notes)
    {
        MissionCandidateDebugInfo info = new MissionCandidateDebugInfo
        {
            phase = phase ?? string.Empty,
            slotIndex = slotIndex,
            candidateKey = candidateKey ?? string.Empty,
            missionType = missionType ?? string.Empty,
            category = category.ToString(),
            difficulty = difficulty.ToString(),
            targetSkillId = skill != null ? skill.id ?? string.Empty : string.Empty,
            targetSkillName = skill != null ? GetSafeSkillName(skill) : string.Empty,
            baseWeight = weightBreakdown.baseWeight,
            categoryMultiplier = weightBreakdown.categoryMultiplier,
            skillMultiplier = weightBreakdown.skillMultiplier,
            contextMultiplier = weightBreakdown.contextMultiplier,
            postClampMultiplier = weightBreakdown.postClampMultiplier,
            rawWeight = weightBreakdown.rawWeight,
            clampedWeight = weightBreakdown.clampedWeight,
            personalizationMultiplier = weightBreakdown.personalizationMultiplier,
            finalWeight = weightBreakdown.finalWeight,
            outcome = "eligible",
            notes = notes ?? string.Empty
        };

        if (activeGenerationDebugSnapshot != null)
        {
            activeGenerationDebugSnapshot.candidates.Add(info);
        }

        return info;
    }

    private void RecordCandidateSelection(int slotIndex, MissionDifficulty difficulty, MissionCandidate picked, MissionEntryData mission)
    {
        if (activeGenerationDebugSnapshot == null)
        {
            return;
        }

        string pickedKey = picked != null ? picked.key ?? string.Empty : string.Empty;
        for (int i = 0; i < activeGenerationDebugSnapshot.candidates.Count; i++)
        {
            MissionCandidateDebugInfo candidate = activeGenerationDebugSnapshot.candidates[i];
            if (candidate == null || candidate.slotIndex != slotIndex)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(pickedKey) && string.Equals(candidate.candidateKey, pickedKey, StringComparison.Ordinal))
            {
                candidate.picked = true;
                candidate.outcome = "picked";
                continue;
            }

            if (string.IsNullOrEmpty(candidate.outcome) || string.Equals(candidate.outcome, "eligible", StringComparison.Ordinal))
            {
                candidate.outcome = "rejected";
            }
        }

        bool usedFallback = picked == null && mission != null;
        string fallbackReason = usedFallback
            ? $"No weighted candidate selected for slot {slotIndex}; used slot fallback."
            : string.Empty;
        if (usedFallback)
        {
            RecordFallbackPath(fallbackReason);
        }

        MissionDebugMissionInfo missionInfo = CreateMissionDebugInfo(mission, usedFallback ? "fallback" : "picked");
        activeGenerationDebugSnapshot.chosenBaseMissions.Add(missionInfo);
        activeGenerationDebugSnapshot.picks.Add(new MissionGenerationPickDebugInfo
        {
            slotIndex = slotIndex,
            slotDifficulty = difficulty.ToString(),
            candidateKey = pickedKey,
            usedFallback = usedFallback,
            fallbackReason = fallbackReason,
            outcome = usedFallback ? "fallback" : "picked",
            mission = missionInfo
        });
    }

    private void RecordFinalMissions(List<MissionEntryData> missions)
    {
        if (activeGenerationDebugSnapshot == null)
        {
            return;
        }

        activeGenerationDebugSnapshot.finalMissions.Clear();
        if (missions == null)
        {
            return;
        }

        for (int i = 0; i < missions.Count; i++)
        {
            activeGenerationDebugSnapshot.finalMissions.Add(CreateMissionDebugInfo(missions[i], "final"));
        }
    }

    private void RecordCompositionAttempt(string attemptLabel, CompositionSettings settings, bool validAfterAttempt)
    {
        if (activeGenerationDebugSnapshot == null)
        {
            return;
        }

        activeGenerationDebugSnapshot.compositionAttempts.Add(new MissionCompositionAttemptDebugInfo
        {
            attemptLabel = attemptLabel ?? string.Empty,
            enforceUniqueSkill = settings.enforceUniqueSkill,
            maxFocusBucket = settings.maxFocusBucket,
            requireNonFocus = settings.requireNonFocus,
            usedRelaxedFallbackProfile = IsRelaxedCompositionSettings(settings),
            validAfterAttempt = validAfterAttempt
        });

        if (IsRelaxedCompositionSettings(settings))
        {
            RecordFallbackPath($"Composition fallback attempt '{attemptLabel}' enabled.");
        }
    }

    private void MarkCompositionAttemptValid(string attemptLabel, bool validAfterAttempt)
    {
        if (activeGenerationDebugSnapshot == null)
        {
            return;
        }

        for (int i = activeGenerationDebugSnapshot.compositionAttempts.Count - 1; i >= 0; i--)
        {
            MissionCompositionAttemptDebugInfo attempt = activeGenerationDebugSnapshot.compositionAttempts[i];
            if (attempt != null && string.Equals(attempt.attemptLabel, attemptLabel, StringComparison.Ordinal))
            {
                attempt.validAfterAttempt = validAfterAttempt;
                return;
            }
        }
    }

    private void LogCompositionRepair(
        string rule,
        int replaceIndex,
        MissionEntryData replacedMission,
        MissionEntryData replacementMission,
        CompositionSettings settings,
        string detail)
    {
        if (activeGenerationDebugSnapshot == null)
        {
            return;
        }

        activeGenerationDebugSnapshot.repairs.Add(new MissionCompositionRepairDebugInfo
        {
            attemptLabel = activeCompositionAttemptLabel ?? string.Empty,
            rule = rule ?? string.Empty,
            replaceIndex = replaceIndex,
            replacedMission = CreateMissionDebugInfo(replacedMission, "replaced"),
            replacementMission = CreateMissionDebugInfo(replacementMission, "replacement"),
            sameDifficulty = replacedMission != null && replacementMission != null && replacedMission.difficulty == replacementMission.difficulty,
            usedRelaxedFallbackProfile = IsRelaxedCompositionSettings(settings),
            detail = detail ?? string.Empty
        });

        if (replaceIndex >= 0 && replaceIndex < activeGenerationDebugSnapshot.chosenBaseMissions.Count)
        {
            activeGenerationDebugSnapshot.chosenBaseMissions[replaceIndex].outcome = "replaced";
        }

        for (int i = 0; i < activeGenerationDebugSnapshot.picks.Count; i++)
        {
            MissionGenerationPickDebugInfo pick = activeGenerationDebugSnapshot.picks[i];
            if (pick != null && pick.slotIndex == replaceIndex)
            {
                pick.outcome = "replaced by composition";
                break;
            }
        }

        string replacedMissionId = replacedMission != null ? replacedMission.missionId ?? string.Empty : string.Empty;
        for (int i = 0; i < activeGenerationDebugSnapshot.candidates.Count; i++)
        {
            MissionCandidateDebugInfo candidate = activeGenerationDebugSnapshot.candidates[i];
            if (candidate == null || !candidate.picked)
            {
                continue;
            }

            MissionGenerationPickDebugInfo pick = FindPickForCandidate(candidate.candidateKey);
            if (pick != null && pick.slotIndex == replaceIndex)
            {
                candidate.replaced = true;
                candidate.outcome = "replaced";
            }
            else if (!string.IsNullOrEmpty(replacedMissionId) && pick != null && pick.mission != null && string.Equals(pick.mission.missionId, replacedMissionId, StringComparison.Ordinal))
            {
                candidate.replaced = true;
                candidate.outcome = "replaced";
            }
        }
    }

    private void RecordFallbackPath(string detail)
    {
        if (activeGenerationDebugSnapshot == null || string.IsNullOrEmpty(detail))
        {
            return;
        }

        activeGenerationDebugSnapshot.fallbackPath.Add(detail);
    }

    private MissionGenerationPickDebugInfo FindPickForCandidate(string candidateKey)
    {
        if (activeGenerationDebugSnapshot == null || string.IsNullOrEmpty(candidateKey))
        {
            return null;
        }

        for (int i = 0; i < activeGenerationDebugSnapshot.picks.Count; i++)
        {
            MissionGenerationPickDebugInfo pick = activeGenerationDebugSnapshot.picks[i];
            if (pick != null && string.Equals(pick.candidateKey, candidateKey, StringComparison.Ordinal))
            {
                return pick;
            }
        }

        return null;
    }

    private MissionDebugMissionInfo CreateMissionDebugInfo(MissionEntryData mission, string outcome)
    {
        if (mission == null)
        {
            return new MissionDebugMissionInfo { outcome = outcome ?? string.Empty };
        }

        return new MissionDebugMissionInfo
        {
            missionId = mission.missionId ?? string.Empty,
            title = GetMissionTitle(mission),
            missionType = mission.missionType ?? string.Empty,
            category = GetMissionCategory(mission).ToString(),
            difficulty = mission.difficulty.ToString(),
            targetSkillId = mission.targetSkillId ?? string.Empty,
            targetSkillName = mission.targetSkillName ?? string.Empty,
            skillMissionMode = mission.skillMissionMode ?? string.Empty,
            outcome = outcome ?? string.Empty
        };
    }

    private MissionPersonalizationDebugSummary CreatePersonalizationSummary(List<SkillEntry> availableSkills, bool decayAppliedSincePreviousCycle)
    {
        NormalizePersonalizationProfile();
        MissionPersonalizationProfileData profile = missionData.personalizationProfile;
        MissionPersonalizationDebugSummary summary = new MissionPersonalizationDebugSummary
        {
            feedScore = profile.feedScore,
            workScore = profile.workScore,
            focusScore = profile.focusScore,
            skillScore = profile.skillScore,
            recentFocusMinutes = profile.recentFocusMinutes,
            decayAppliedSincePreviousCycle = decayAppliedSincePreviousCycle
        };

        Dictionary<string, string> skillNamesById = new Dictionary<string, string>(StringComparer.Ordinal);
        if (availableSkills != null)
        {
            for (int i = 0; i < availableSkills.Count; i++)
            {
                SkillEntry skill = availableSkills[i];
                if (skill == null || string.IsNullOrEmpty(skill.id) || skillNamesById.ContainsKey(skill.id))
                {
                    continue;
                }

                skillNamesById[skill.id] = GetSafeSkillName(skill);
            }
        }

        List<MissionSkillPreferenceData> sortedPreferences = new List<MissionSkillPreferenceData>();
        if (profile.skillPreferences != null)
        {
            for (int i = 0; i < profile.skillPreferences.Count; i++)
            {
                if (profile.skillPreferences[i] != null && !string.IsNullOrEmpty(profile.skillPreferences[i].skillId))
                {
                    sortedPreferences.Add(profile.skillPreferences[i]);
                }
            }
        }

        sortedPreferences.Sort((a, b) => b.score.CompareTo(a.score));
        int topCount = Mathf.Min(3, sortedPreferences.Count);
        for (int i = 0; i < topCount; i++)
        {
            MissionSkillPreferenceData preference = sortedPreferences[i];
            summary.topSkillPreferences.Add(new MissionSkillPreferenceDebugInfo
            {
                skillId = preference.skillId ?? string.Empty,
                skillName = skillNamesById.ContainsKey(preference.skillId) ? skillNamesById[preference.skillId] : preference.skillId ?? string.Empty,
                score = preference.score
            });
        }

        return summary;
    }

    private MissionWeightBreakdown BuildWeightBreakdown(MissionCategory category, string skillId, float contextMultiplier, float postClampMultiplier)
    {
        float safeContextMultiplier = Mathf.Max(0.01f, contextMultiplier);
        float safePostClampMultiplier = Mathf.Max(0f, postClampMultiplier);
        float categoryMultiplier = GetCategoryWeight(category);
        float skillMultiplier = category == MissionCategory.Skill && !string.IsNullOrEmpty(skillId)
            ? GetSkillWeight(skillId)
            : 1f;
        float rawWeight = categoryMultiplier * skillMultiplier * safeContextMultiplier;
        float clampedWeight = Mathf.Clamp(rawWeight, MinPersonalizationWeight, MaxPersonalizationWeight);
        float finalWeight = clampedWeight * safePostClampMultiplier;

        return new MissionWeightBreakdown
        {
            baseWeight = 1f,
            categoryMultiplier = categoryMultiplier,
            skillMultiplier = skillMultiplier,
            contextMultiplier = safeContextMultiplier,
            postClampMultiplier = safePostClampMultiplier,
            rawWeight = rawWeight,
            clampedWeight = clampedWeight,
            personalizationMultiplier = finalWeight,
            finalWeight = finalWeight
        };
    }

    private int GetCandidateDebugCount()
    {
        return activeGenerationDebugSnapshot != null && activeGenerationDebugSnapshot.candidates != null
            ? activeGenerationDebugSnapshot.candidates.Count
            : 0;
    }

    private int GetRepairDebugCount()
    {
        return activeGenerationDebugSnapshot != null && activeGenerationDebugSnapshot.repairs != null
            ? activeGenerationDebugSnapshot.repairs.Count
            : 0;
    }

    private int GetCompositionAttemptDebugCount()
    {
        return activeGenerationDebugSnapshot != null && activeGenerationDebugSnapshot.compositionAttempts != null
            ? activeGenerationDebugSnapshot.compositionAttempts.Count
            : 0;
    }

    private string GetFallbackDebugSummary()
    {
        if (activeGenerationDebugSnapshot == null || activeGenerationDebugSnapshot.fallbackPath == null || activeGenerationDebugSnapshot.fallbackPath.Count == 0)
        {
            return "No fallback required.";
        }

        return string.Join(" | ", activeGenerationDebugSnapshot.fallbackPath);
    }

    private string FormatDifficultyPlan(List<MissionDifficulty> difficulties)
    {
        if (difficulties == null || difficulties.Count == 0)
        {
            return "(empty)";
        }

        List<string> labels = new List<string>();
        for (int i = 0; i < difficulties.Count; i++)
        {
            labels.Add(difficulties[i].ToString());
        }

        return string.Join(", ", labels);
    }

    private string FormatDifficultyPlan(List<string> difficulties)
    {
        if (difficulties == null || difficulties.Count == 0)
        {
            return "(empty)";
        }

        return string.Join(", ", difficulties);
    }

    private string BuildCompactDebugReport(MissionGenerationDebugSnapshot snapshot)
    {
        if (snapshot == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Mission Generation Debug");
        builder.AppendLine($"ResetKey: {snapshot.resetKey}");
        builder.AppendLine($"GenerationKey: {snapshot.generationKey}");
        builder.AppendLine($"Reason: {snapshot.generationReason}");
        builder.AppendLine($"DifficultyPlan: {FormatDifficultyPlan(snapshot.difficultyPlan)}");

        MissionPersonalizationDebugSummary profile = snapshot.personalizationSummary ?? new MissionPersonalizationDebugSummary();
        builder.AppendLine($"Profile: feed={profile.feedScore:0.##} work={profile.workScore:0.##} focus={profile.focusScore:0.##} skill={profile.skillScore:0.##} recentFocusMin={profile.recentFocusMinutes:0.##}");
        builder.AppendLine($"DecayApplied: {(profile.decayAppliedSincePreviousCycle ? "yes" : "no")}");
        if (snapshot.decaySummary != null)
        {
            builder.AppendLine(
                $"ResetTriggered: {(snapshot.decaySummary.resetTriggered ? "yes" : "no")} ignoredPenalty={(snapshot.decaySummary.ignoredMissionPenaltyApplied ? "yes" : "no")} lastDecayResetKey={snapshot.decaySummary.newLastDecayResetKey}");
        }

        if (profile.topSkillPreferences != null && profile.topSkillPreferences.Count > 0)
        {
            List<string> topSkills = new List<string>();
            for (int i = 0; i < profile.topSkillPreferences.Count; i++)
            {
                MissionSkillPreferenceDebugInfo preference = profile.topSkillPreferences[i];
                topSkills.Add($"{preference.skillName}={preference.score:0.##}");
            }

            builder.AppendLine($"TopSkills: {string.Join(", ", topSkills)}");
        }
        else
        {
            builder.AppendLine("TopSkills: none");
        }

        builder.AppendLine("Steps:");
        for (int i = 0; i < snapshot.steps.Count; i++)
        {
            MissionGenerationStepDebugInfo step = snapshot.steps[i];
            builder.AppendLine($"- {step.phase}: {step.detail}");
        }

        builder.AppendLine("Candidates:");
        if (snapshot.candidates.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            for (int i = 0; i < snapshot.candidates.Count; i++)
            {
                MissionCandidateDebugInfo candidate = snapshot.candidates[i];
                string skillPart = string.IsNullOrEmpty(candidate.targetSkillName) ? string.Empty : $" {candidate.targetSkillName}";
                builder.AppendLine(
                    $"- slot {candidate.slotIndex} {candidate.missionType}{skillPart} [{candidate.difficulty}] base={candidate.baseWeight:0.##} personalization={candidate.personalizationMultiplier:0.##} final={candidate.finalWeight:0.##} outcome={candidate.outcome}");
            }
        }

        builder.AppendLine("Repairs:");
        if (snapshot.repairs.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            for (int i = 0; i < snapshot.repairs.Count; i++)
            {
                MissionCompositionRepairDebugInfo repair = snapshot.repairs[i];
                builder.AppendLine(
                    $"- {repair.rule} [{repair.attemptLabel}] {repair.replacedMission.title} -> {repair.replacementMission.title} sameDifficulty={repair.sameDifficulty}");
            }
        }

        builder.AppendLine("Attempts:");
        if (snapshot.compositionAttempts.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            for (int i = 0; i < snapshot.compositionAttempts.Count; i++)
            {
                MissionCompositionAttemptDebugInfo attempt = snapshot.compositionAttempts[i];
                builder.AppendLine(
                    $"- {attempt.attemptLabel} uniqueSkill={attempt.enforceUniqueSkill} focusCap={attempt.maxFocusBucket} requireNonFocus={attempt.requireNonFocus} valid={attempt.validAfterAttempt}");
            }
        }

        builder.AppendLine("Fallback:");
        if (snapshot.fallbackPath.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            for (int i = 0; i < snapshot.fallbackPath.Count; i++)
            {
                builder.AppendLine($"- {snapshot.fallbackPath[i]}");
            }
        }

        builder.AppendLine("Final:");
        if (snapshot.finalMissions.Count == 0)
        {
            builder.AppendLine("- none");
        }
        else
        {
            for (int i = 0; i < snapshot.finalMissions.Count; i++)
            {
                MissionDebugMissionInfo mission = snapshot.finalMissions[i];
                builder.AppendLine($"- {mission.title} [{mission.difficulty}]");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private void LogGenerationDebugReportIfEnabled(MissionGenerationDebugSnapshot snapshot)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (missionDebugLoggingEnabled && snapshot != null)
        {
            Debug.Log(snapshot.compactReport);
        }
#endif
    }

}
