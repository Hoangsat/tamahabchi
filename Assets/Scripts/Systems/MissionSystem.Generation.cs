using System;
using System.Collections.Generic;
using UnityEngine;

public partial class MissionSystem
{
    private void GenerateDailyMissions(List<SkillEntry> skills, string resetKey, MissionDecayDebugSummary decaySummary)
    {
        missionData.lastDailyResetKey = resetKey;
        missionData.skillBonusClaimed = false;
        missionData.customRoutineCreateCount = 0;
        missionData.missions.Clear();

        List<SkillEntry> availableSkills = OrderSkills(skills);
        BeginGenerationDebugSnapshot(resetKey, "daily_reset_generation", availableSkills, decaySummary);

        bool hasSkills = availableSkills.Count > 0;
        int totalMissionCount = hasSkills ? MaxDailyMissionCount : 3;
        List<MissionDifficulty> difficulties = BuildDifficultyPlan(totalMissionCount);
        RecordDifficultyPlan(difficulties);
        RecordDebugStep("difficulty plan built", $"Plan: {FormatDifficultyPlan(difficulties)}");
        if (!hasSkills)
        {
            RecordDebugStep("eligible candidates built", "0 skills available; skill candidates were absent for this cycle.");
        }

        List<MissionEntryData> generated = new List<MissionEntryData>();
        HashSet<string> usedCandidateKeys = new HashSet<string>(StringComparer.Ordinal);
        int totalCandidatesBuilt = 0;

        for (int i = 0; i < totalMissionCount; i++)
        {
            List<MissionCandidate> candidates = BuildEligibleMissionCandidates(
                availableSkills,
                difficulties[i],
                generated,
                usedCandidateKeys,
                i,
                "daily_generation"
            );
            totalCandidatesBuilt += candidates.Count;

            MissionCandidate picked = PickWeightedCandidate(candidates);
            MissionEntryData mission = picked != null
                ? picked.build()
                : CreateFallbackMissionForSlot(i, difficulties[i], availableSkills);

            RecordCandidateSelection(i, difficulties[i], picked, mission);

            if (mission == null)
            {
                continue;
            }

            generated.Add(mission);
            if (picked != null && !string.IsNullOrEmpty(picked.key))
            {
                usedCandidateKeys.Add(picked.key);
            }
        }

        RecordDebugStep("eligible candidates built", $"{totalCandidatesBuilt} candidates across {totalMissionCount} slots.");
        RecordDebugStep("personalization weights applied", $"{GetCandidateDebugCount()} weighted candidates captured.");
        RecordDebugStep("base missions picked", $"{generated.Count} base missions selected before composition repair.");

        ApplyCompositionRules(generated, availableSkills);
        RecordDebugStep("composition repairs applied", $"{GetRepairDebugCount()} repair events across {GetCompositionAttemptDebugCount()} composition attempts.");
        RecordDebugStep("fallback used if any", GetFallbackDebugSummary());

        generated = ReorderForHudSummary(generated);
        RecordDebugStep("final set finalized", $"{generated.Count} missions finalized after HUD ordering.");
        RecordFinalMissions(generated);

        for (int i = 0; i < generated.Count; i++)
        {
            NormalizeMission(generated[i]);
        }

        missionData.missions.AddRange(generated);
        FinalizeGenerationDebugSnapshot();
    }

    private void EnsureSkillMissionCoverage(List<SkillEntry> skills)
    {
        List<SkillEntry> availableSkills = OrderSkills(skills);
        if (availableSkills.Count == 0)
        {
            return;
        }

        int currentSkillMissionCount = 0;
        for (int i = 0; i < missionData.missions.Count; i++)
        {
            if (IsSkillMission(missionData.missions[i]))
            {
                currentSkillMissionCount++;
            }
        }

        if (currentSkillMissionCount >= MaxSkillMissionsPerDay || missionData.missions.Count >= MaxDailyMissionCount)
        {
            return;
        }

        List<MissionDifficulty> supplementalDifficulties = BuildDifficultyPlan(MaxSkillMissionsPerDay);
        List<MissionEntryData> additions = new List<MissionEntryData>();
        HashSet<string> usedCandidateKeys = CollectUsedCandidateKeys(missionData.missions);

        for (int i = currentSkillMissionCount; i < MaxSkillMissionsPerDay && additions.Count < MaxDailyMissionCount - missionData.missions.Count; i++)
        {
            MissionDifficulty difficulty = supplementalDifficulties[Mathf.Min(i, supplementalDifficulties.Count - 1)];
            List<MissionCandidate> candidates = BuildSkillMissionCandidates(
                availableSkills,
                difficulty,
                missionData.missions,
                usedCandidateKeys
            );

            MissionCandidate picked = PickWeightedCandidate(candidates);
            if (picked == null)
            {
                break;
            }

            MissionEntryData mission = picked.build();
            if (mission == null)
            {
                break;
            }

            additions.Add(mission);
            usedCandidateKeys.Add(picked.key);
        }

        for (int i = 0; i < additions.Count; i++)
        {
            NormalizeMission(additions[i]);
            missionData.missions.Add(additions[i]);
        }

        ApplyCompositionRules(missionData.missions, availableSkills);
        missionData.missions = ReorderForHudSummary(missionData.missions);
    }

    private List<MissionCandidate> BuildEligibleMissionCandidates(
        List<SkillEntry> availableSkills,
        MissionDifficulty difficulty,
        List<MissionEntryData> selectedMissions,
        HashSet<string> usedCandidateKeys,
        int slotIndex = -1,
        string phase = "eligible_candidates")
    {
        List<MissionCandidate> candidates = new List<MissionCandidate>
        {
            BuildGenericCandidate(GenericFeedMissionId, FeedMissionType, MissionCategory.Feed, difficulty, CreateGenericFeedMission, usedCandidateKeys, slotIndex, phase),
            BuildGenericCandidate(GenericWorkMissionId, WorkMissionType, MissionCategory.Work, difficulty, CreateGenericWorkMission, usedCandidateKeys, slotIndex, phase),
            BuildGenericCandidate(GenericFocusMissionId, FocusMissionType, MissionCategory.Focus, difficulty, CreateGenericFocusMission, usedCandidateKeys, slotIndex, phase)
        };

        AddSkillMissionCandidates(candidates, availableSkills, difficulty, selectedMissions, usedCandidateKeys, slotIndex, phase);
        candidates.RemoveAll(candidate => candidate == null || candidate.weight <= 0f || candidate.build == null);
        return candidates;
    }

    private List<MissionCandidate> BuildSkillMissionCandidates(
        List<SkillEntry> availableSkills,
        MissionDifficulty difficulty,
        List<MissionEntryData> selectedMissions,
        HashSet<string> usedCandidateKeys,
        int slotIndex = -1,
        string phase = "skill_candidates")
    {
        List<MissionCandidate> candidates = new List<MissionCandidate>();
        AddSkillMissionCandidates(candidates, availableSkills, difficulty, selectedMissions, usedCandidateKeys, slotIndex, phase);
        candidates.RemoveAll(candidate => candidate == null || candidate.weight <= 0f || candidate.build == null);
        return candidates;
    }

    private List<MissionEntryData> CreateSkillMissionSet(
        List<SkillEntry> availableSkills,
        List<MissionDifficulty> difficulties,
        int difficultyStartIndex,
        int existingSkillMissionCount = 0,
        int maxToAdd = MaxSkillMissionsPerDay)
    {
        List<MissionEntryData> skillMissions = new List<MissionEntryData>();
        if (availableSkills == null || availableSkills.Count == 0 || maxToAdd <= 0)
        {
            return skillMissions;
        }

        HashSet<string> usedSkillIds = new HashSet<string>();
        int missionIndex = existingSkillMissionCount;
        int difficultyIndex = difficultyStartIndex;

        while (skillMissions.Count < maxToAdd && missionIndex < MaxSkillMissionsPerDay && difficultyIndex < difficulties.Count)
        {
            SkillEntry skill = PickSkillForMission(availableSkills, usedSkillIds, missionIndex);
            if (skill == null)
            {
                break;
            }

            MissionDifficulty difficulty = difficulties[difficultyIndex];
            MissionEntryData mission = missionIndex % 2 == 0
                ? CreateSkillMinutesMission(skill, difficulty)
                : CreateSkillSessionsMission(skill, difficulty);

            skillMissions.Add(mission);
            usedSkillIds.Add(skill.id);
            missionIndex++;
            difficultyIndex++;
        }

        return skillMissions;
    }

    private void AddSkillMissionCandidates(
        List<MissionCandidate> candidates,
        List<SkillEntry> availableSkills,
        MissionDifficulty difficulty,
        List<MissionEntryData> selectedMissions,
        HashSet<string> usedCandidateKeys,
        int slotIndex = -1,
        string phase = "skill_candidates")
    {
        if (candidates == null || availableSkills == null || availableSkills.Count == 0)
        {
            return;
        }

        HashSet<string> selectedSkillIds = CollectSkillTargets(selectedMissions);
        for (int i = 0; i < availableSkills.Count; i++)
        {
            SkillEntry skill = availableSkills[i];
            if (skill == null || string.IsNullOrEmpty(skill.id))
            {
                continue;
            }

            bool duplicateSkillSelected = selectedSkillIds.Contains(skill.id);
            MissionWeightBreakdown weightBreakdown = BuildWeightBreakdown(
                MissionCategory.Skill,
                skill.id,
                duplicateSkillSelected ? 0.9f : 1f,
                duplicateSkillSelected ? 0.85f : 1f
            );
            float skillWeight = weightBreakdown.finalWeight;

            string minutesKey = $"skill_minutes:{difficulty}:{skill.id}";
            if (usedCandidateKeys == null || !usedCandidateKeys.Contains(minutesKey))
            {
                candidates.Add(new MissionCandidate
                {
                    key = minutesKey,
                    weight = skillWeight,
                    build = () => CreateSkillMinutesMission(skill, difficulty),
                    debugInfo = CreateCandidateDebugInfo(
                        phase,
                        slotIndex,
                        minutesKey,
                        SkillFocusMissionType,
                        MissionCategory.Skill,
                        difficulty,
                        skill,
                        weightBreakdown,
                        duplicateSkillSelected ? "Duplicate skill target already present in base picks." : string.Empty
                    )
                });
            }

            string sessionsKey = $"skill_sessions:{difficulty}:{skill.id}";
            if (usedCandidateKeys == null || !usedCandidateKeys.Contains(sessionsKey))
            {
                candidates.Add(new MissionCandidate
                {
                    key = sessionsKey,
                    weight = skillWeight,
                    build = () => CreateSkillSessionsMission(skill, difficulty),
                    debugInfo = CreateCandidateDebugInfo(
                        phase,
                        slotIndex,
                        sessionsKey,
                        SkillFocusMissionType,
                        MissionCategory.Skill,
                        difficulty,
                        skill,
                        weightBreakdown,
                        duplicateSkillSelected ? "Duplicate skill target already present in base picks." : string.Empty
                    )
                });
            }
        }
    }

    private MissionCandidate BuildGenericCandidate(
        string key,
        string missionType,
        MissionCategory category,
        MissionDifficulty difficulty,
        Func<MissionDifficulty, MissionEntryData> factory,
        HashSet<string> usedCandidateKeys,
        int slotIndex = -1,
        string phase = "eligible_candidates")
    {
        string candidateKey = $"{missionType}:{difficulty}";
        if (usedCandidateKeys != null && usedCandidateKeys.Contains(candidateKey))
        {
            return null;
        }

        MissionWeightBreakdown weightBreakdown = BuildWeightBreakdown(category, string.Empty, 1f, 1f);

        return new MissionCandidate
        {
            key = candidateKey,
            weight = weightBreakdown.finalWeight,
            build = () => factory(difficulty),
            debugInfo = CreateCandidateDebugInfo(
                phase,
                slotIndex,
                candidateKey,
                missionType,
                category,
                difficulty,
                null,
                weightBreakdown,
                string.Empty
            )
        };
    }

    private MissionEntryData CreateGenericFeedMission(MissionDifficulty difficulty)
    {
        int target = GetFeedTarget(difficulty);
        return CreateMission(
            GenericFeedMissionId,
            FeedMissionType,
            difficulty,
            $"Feed pet {target} times",
            target,
            0f,
            8
        );
    }

    private MissionEntryData CreateGenericWorkMission(MissionDifficulty difficulty)
    {
        int target = GetWorkTarget(difficulty);
        return CreateMission(
            GenericWorkMissionId,
            WorkMissionType,
            difficulty,
            $"Work {target} times",
            target,
            0f,
            10
        );
    }

    private MissionEntryData CreateGenericFocusMission(MissionDifficulty difficulty)
    {
        int minutes = GetFocusMinutesTarget(difficulty);
        return CreateMission(
            GenericFocusMissionId,
            FocusMissionType,
            difficulty,
            $"Focus {minutes} min",
            minutes,
            minutes,
            12
        );
    }

    private MissionEntryData CreateFallbackMissionForSlot(int slotIndex, MissionDifficulty difficulty, List<SkillEntry> availableSkills)
    {
        if (slotIndex <= 0)
        {
            return CreateGenericFeedMission(difficulty);
        }

        if (slotIndex == 1)
        {
            return CreateGenericFocusMission(difficulty);
        }

        if (availableSkills != null && availableSkills.Count > 0 && slotIndex >= 3)
        {
            SkillEntry fallbackSkill = availableSkills[slotIndex % availableSkills.Count];
            return slotIndex % 2 == 0
                ? CreateSkillMinutesMission(fallbackSkill, difficulty)
                : CreateSkillSessionsMission(fallbackSkill, difficulty);
        }

        return CreateGenericFocusMission(difficulty);
    }

    private MissionEntryData CreateSkillMinutesMission(SkillEntry skill, MissionDifficulty difficulty)
    {
        int minutes = GetFocusMinutesTarget(difficulty);
        return CreateMission(
            $"skill_focus:minutes:{skill.id}",
            SkillFocusMissionType,
            difficulty,
            $"Focus {minutes} min on {GetSafeSkillName(skill)}",
            minutes,
            minutes,
            14,
            skill.id,
            GetSafeSkillName(skill),
            SkillMissionModeMinutes
        );
    }

    private MissionEntryData CreateSkillSessionsMission(SkillEntry skill, MissionDifficulty difficulty)
    {
        int sessions = GetSkillSessionTarget(difficulty);
        return CreateMission(
            $"skill_focus:sessions:{skill.id}",
            SkillFocusMissionType,
            difficulty,
            $"Complete {sessions} focus sessions on {GetSafeSkillName(skill)}",
            sessions,
            0f,
            16,
            skill.id,
            GetSafeSkillName(skill),
            SkillMissionModeSessions
        );
    }

    private MissionEntryData CreateMission(
        string missionId,
        string missionType,
        MissionDifficulty difficulty,
        string title,
        int targetProgress,
        float requiredMinutes,
        int baseCoins,
        string targetSkillId = "",
        string targetSkillName = "",
        string skillMissionMode = "")
    {
        float rewardMultiplier = GetDifficultyRewardMultiplier(difficulty);
        int finalCoins = Mathf.Max(1, Mathf.RoundToInt(baseCoins * rewardMultiplier));

        return new MissionEntryData
        {
            missionId = missionId,
            missionType = missionType,
            difficulty = difficulty,
            title = title,
            currentProgress = 0,
            targetProgress = Mathf.Max(1, targetProgress),
            requiredMinutes = Mathf.Max(0f, requiredMinutes),
            progressMinutes = 0f,
            rewardCoins = finalCoins,
            isCompleted = false,
            isClaimed = false,
            targetSkillId = targetSkillId ?? string.Empty,
            skillId = targetSkillId ?? string.Empty,
            targetSkillName = targetSkillName ?? string.Empty,
            skillMissionMode = skillMissionMode ?? string.Empty
        };
    }

    private List<MissionEntryData> ReorderForHudSummary(List<MissionEntryData> generated)
    {
        if (generated == null || generated.Count <= 3)
        {
            return generated ?? new List<MissionEntryData>();
        }

        MissionEntryData genericFocus = generated.Find(IsGenericFocusMission);
        MissionEntryData genericFeed = generated.Find(m => string.Equals(m.missionType, FeedMissionType, StringComparison.Ordinal));
        MissionEntryData secondGenericFocus = null;
        for (int i = 0; i < generated.Count; i++)
        {
            MissionEntryData entry = generated[i];
            if (entry == null || !string.Equals(entry.missionType, FocusMissionType, StringComparison.Ordinal) || ReferenceEquals(entry, genericFocus))
            {
                continue;
            }

            secondGenericFocus = entry;
            break;
        }
        List<MissionEntryData> skillMissions = generated.FindAll(IsSkillMission);

        List<MissionEntryData> ordered = new List<MissionEntryData>();
        if (genericFocus != null) ordered.Add(genericFocus);
        if (skillMissions.Count > 0) ordered.Add(skillMissions[0]);
        if (secondGenericFocus != null) ordered.Add(secondGenericFocus);
        if (skillMissions.Count > 1) ordered.Add(skillMissions[1]);
        if (genericFeed != null) ordered.Add(genericFeed);

        for (int i = 0; i < generated.Count; i++)
        {
            if (!ordered.Contains(generated[i]))
            {
                ordered.Add(generated[i]);
            }
        }

        return ordered;
    }

    private void ApplyCompositionRules(List<MissionEntryData> missions, List<SkillEntry> availableSkills)
    {
        if (missions == null || missions.Count == 0)
        {
            return;
        }

        CompositionSettings[] attempts =
        {
            new CompositionSettings { enforceUniqueSkill = true, maxFocusBucket = 2, requireNonFocus = true },
            new CompositionSettings { enforceUniqueSkill = false, maxFocusBucket = 2, requireNonFocus = true },
            new CompositionSettings { enforceUniqueSkill = false, maxFocusBucket = 3, requireNonFocus = true },
            new CompositionSettings { enforceUniqueSkill = false, maxFocusBucket = 3, requireNonFocus = false }
        };

        for (int i = 0; i < attempts.Length; i++)
        {
            activeCompositionAttemptLabel = GetCompositionAttemptLabel(i);
            RecordCompositionAttempt(activeCompositionAttemptLabel, attempts[i], false);
            RepairDuplicateSkillTargets(missions, availableSkills, attempts[i]);
            RepairFocusBucket(missions, attempts[i]);
            RepairMissingNonFocus(missions, attempts[i]);
            RepairTypeOverflows(missions, availableSkills, attempts[i]);
            RepairCategoryDiversity(missions, availableSkills, attempts[i]);
            EnsureUniqueMissionIds(missions);

            if (IsCompositionValid(missions, attempts[i]))
            {
                MarkCompositionAttemptValid(activeCompositionAttemptLabel, true);
                if (i > 0)
                {
                    RecordFallbackPath($"Composition valid at fallback level '{activeCompositionAttemptLabel}'.");
                }

                activeCompositionAttemptLabel = string.Empty;
                return;
            }

            MarkCompositionAttemptValid(activeCompositionAttemptLabel, false);
        }

        EnsureUniqueMissionIds(missions);
        RecordFallbackPath("Composition exited without a fully valid strict profile; unique mission ids still enforced.");
        activeCompositionAttemptLabel = string.Empty;
    }

    private void RepairDuplicateSkillTargets(List<MissionEntryData> missions, List<SkillEntry> availableSkills, CompositionSettings settings)
    {
        if (!settings.enforceUniqueSkill || missions == null)
        {
            return;
        }

        HashSet<string> seenSkillIds = new HashSet<string>();
        for (int i = 0; i < missions.Count; i++)
        {
            MissionEntryData mission = missions[i];
            if (!IsSkillMission(mission) || string.IsNullOrEmpty(mission.targetSkillId))
            {
                continue;
            }

            if (seenSkillIds.Add(mission.targetSkillId))
            {
                continue;
            }

            SkillEntry replacementSkill = FindAlternativeSkill(availableSkills, seenSkillIds);
            if (replacementSkill == null)
            {
                continue;
            }

            MissionEntryData replacement = string.Equals(mission.skillMissionMode, SkillMissionModeSessions, StringComparison.Ordinal)
                ? CreateSkillSessionsMission(replacementSkill, mission.difficulty)
                : CreateSkillMinutesMission(replacementSkill, mission.difficulty);

            LogCompositionRepair("duplicate skill target repaired", i, mission, replacement, settings, $"Duplicate targetSkillId '{mission.targetSkillId}' replaced.");
            ReplaceMissionAt(missions, i, replacement);
            seenSkillIds.Add(replacementSkill.id);
        }
    }

    private void RepairFocusBucket(List<MissionEntryData> missions, CompositionSettings settings)
    {
        if (missions == null)
        {
            return;
        }

        while (CountFocusBucketMissions(missions) > settings.maxFocusBucket)
        {
            int replaceIndex = FindReplaceableFocusIndex(missions);
            if (replaceIndex < 0)
            {
                break;
            }

            MissionEntryData current = missions[replaceIndex];
            MissionEntryData replacement = CreateBalancedNonFocusMission(missions, current.difficulty, replaceIndex);
            if (replacement == null)
            {
                break;
            }

            LogCompositionRepair("focus bucket overflow repaired", replaceIndex, current, replacement, settings, $"Focus bucket count exceeded {settings.maxFocusBucket}.");
            ReplaceMissionAt(missions, replaceIndex, replacement);
        }
    }

    private void RepairMissingNonFocus(List<MissionEntryData> missions, CompositionSettings settings)
    {
        if (!settings.requireNonFocus || missions == null || HasNonFocusMission(missions))
        {
            return;
        }

        int replaceIndex = FindReplaceableFocusIndex(missions);
        if (replaceIndex < 0)
        {
            return;
        }

        MissionEntryData replacement = CreateBalancedNonFocusMission(missions, missions[replaceIndex].difficulty, replaceIndex);
        if (replacement != null)
        {
            LogCompositionRepair("missing non-focus repaired", replaceIndex, missions[replaceIndex], replacement, settings, "Composition required at least one non-focus mission.");
            ReplaceMissionAt(missions, replaceIndex, replacement);
        }
    }

    private void RepairTypeOverflows(List<MissionEntryData> missions, List<SkillEntry> availableSkills, CompositionSettings settings)
    {
        if (missions == null)
        {
            return;
        }

        while (TryFindOverflowType(missions, out string overflowType))
        {
            int replaceIndex = FindLastMissionIndexByType(missions, overflowType);
            if (replaceIndex < 0)
            {
                break;
            }

            MissionEntryData replacement = CreateReplacementForOverflow(missions, replaceIndex, availableSkills, settings);
            if (replacement == null)
            {
                break;
            }

            LogCompositionRepair("type overflow repaired", replaceIndex, missions[replaceIndex], replacement, settings, $"Mission type '{overflowType}' exceeded cap {MaxSameMissionTypeCount}.");
            ReplaceMissionAt(missions, replaceIndex, replacement);
        }
    }

    private void RepairCategoryDiversity(List<MissionEntryData> missions, List<SkillEntry> availableSkills, CompositionSettings settings)
    {
        if (missions == null || missions.Count < 2 || CountDistinctCategories(missions) >= 2)
        {
            return;
        }

        int replaceIndex = missions.Count - 1;
        MissionEntryData replacement = CreateMissionFromDifferentCategory(missions, replaceIndex, availableSkills, settings);
        if (replacement != null)
        {
            LogCompositionRepair("category diversity repaired", replaceIndex, missions[replaceIndex], replacement, settings, "Composition needed at least two distinct mission categories.");
            ReplaceMissionAt(missions, replaceIndex, replacement);
        }
    }

    private bool IsCompositionValid(List<MissionEntryData> missions, CompositionSettings settings)
    {
        if (missions == null || missions.Count == 0)
        {
            return true;
        }

        if (CountFocusBucketMissions(missions) > settings.maxFocusBucket)
        {
            return false;
        }

        if (CountDistinctCategories(missions) < Mathf.Min(2, missions.Count))
        {
            return false;
        }

        if (settings.requireNonFocus && !HasNonFocusMission(missions))
        {
            return false;
        }

        if (settings.enforceUniqueSkill && HasDuplicateSkillTargets(missions))
        {
            return false;
        }

        if (HasTypeOverflow(missions))
        {
            return false;
        }

        return true;
    }

    private MissionEntryData CreateReplacementForOverflow(List<MissionEntryData> missions, int replaceIndex, List<SkillEntry> availableSkills, CompositionSettings settings)
    {
        MissionEntryData current = missions[replaceIndex];
        MissionCategory currentCategory = GetMissionCategory(current);
        MissionDifficulty difficulty = current != null ? current.difficulty : MissionDifficulty.Easy;

        if (currentCategory == MissionCategory.Focus || currentCategory == MissionCategory.Skill)
        {
            return CreateBalancedNonFocusMission(missions, difficulty, replaceIndex);
        }

        if (currentCategory == MissionCategory.Feed)
        {
            return CreateGenericFocusMission(difficulty);
        }

        if (currentCategory == MissionCategory.Work)
        {
            if (CountFocusBucketMissions(missions) < settings.maxFocusBucket)
            {
                MissionEntryData focusReplacement = CreateFocusOrSkillMission(missions, difficulty, availableSkills, settings, replaceIndex);
                if (focusReplacement != null)
                {
                    return focusReplacement;
                }
            }

            return CreateGenericFeedMission(difficulty);
        }

        return CreateBalancedNonFocusMission(missions, difficulty, replaceIndex);
    }

    private MissionEntryData CreateMissionFromDifferentCategory(List<MissionEntryData> missions, int replaceIndex, List<SkillEntry> availableSkills, CompositionSettings settings)
    {
        MissionEntryData current = missions[replaceIndex];
        MissionCategory currentCategory = GetMissionCategory(current);
        MissionDifficulty difficulty = current != null ? current.difficulty : MissionDifficulty.Easy;

        if (currentCategory == MissionCategory.Feed)
        {
            return CreateGenericFocusMission(difficulty);
        }

        if (currentCategory == MissionCategory.Work)
        {
            return CreateGenericFeedMission(difficulty);
        }

        if (CountFocusBucketMissions(missions) < settings.maxFocusBucket)
        {
            MissionEntryData focusReplacement = CreateFocusOrSkillMission(missions, difficulty, availableSkills, settings, replaceIndex);
            if (focusReplacement != null)
            {
                return focusReplacement;
            }
        }

        return CreateBalancedNonFocusMission(missions, difficulty, replaceIndex);
    }

    private MissionEntryData CreateFocusOrSkillMission(List<MissionEntryData> missions, MissionDifficulty difficulty, List<SkillEntry> availableSkills, CompositionSettings settings, int replaceIndex)
    {
        if (CountMissionType(missions, FocusMissionType) <= CountMissionType(missions, SkillFocusMissionType))
        {
            return CreateGenericFocusMission(difficulty);
        }

        SkillEntry skill = FindAlternativeSkill(availableSkills, settings.enforceUniqueSkill ? CollectSkillTargets(missions, replaceIndex) : null);
        if (skill == null)
        {
            return CreateGenericFocusMission(difficulty);
        }

        bool preferSessions = CountSkillMode(missions, SkillMissionModeMinutes) > CountSkillMode(missions, SkillMissionModeSessions);
        return preferSessions
            ? CreateSkillSessionsMission(skill, difficulty)
            : CreateSkillMinutesMission(skill, difficulty);
    }

    private MissionEntryData CreateBalancedNonFocusMission(List<MissionEntryData> missions, MissionDifficulty difficulty, int replaceIndex)
    {
        int feedCount = CountMissionType(missions, FeedMissionType);
        int workCount = CountMissionType(missions, WorkMissionType);
        MissionCategory currentCategory = replaceIndex >= 0 && replaceIndex < missions.Count
            ? GetMissionCategory(missions[replaceIndex])
            : MissionCategory.Other;

        if (currentCategory == MissionCategory.Feed)
        {
            return CreateGenericFocusMission(difficulty);
        }

        if (currentCategory == MissionCategory.Work)
        {
            return CreateGenericFeedMission(difficulty);
        }

        return feedCount <= workCount
            ? CreateGenericFeedMission(difficulty)
            : CreateGenericFocusMission(difficulty);
    }

    private MissionCategory GetMissionCategory(MissionEntryData mission)
    {
        if (mission == null)
        {
            return MissionCategory.Other;
        }

        if (IsSkillMission(mission))
        {
            return MissionCategory.Skill;
        }

        if (string.Equals(mission.missionType, FeedMissionType, StringComparison.Ordinal))
        {
            return MissionCategory.Feed;
        }

        if (string.Equals(mission.missionType, WorkMissionType, StringComparison.Ordinal))
        {
            return MissionCategory.Work;
        }

        if (string.Equals(mission.missionType, FocusMissionType, StringComparison.Ordinal))
        {
            return MissionCategory.Focus;
        }

        return MissionCategory.Other;
    }

    private int CountDistinctCategories(List<MissionEntryData> missions)
    {
        HashSet<MissionCategory> categories = new HashSet<MissionCategory>();
        for (int i = 0; i < missions.Count; i++)
        {
            categories.Add(GetMissionCategory(missions[i]));
        }

        return categories.Count;
    }

    private int CountFocusBucketMissions(List<MissionEntryData> missions)
    {
        int count = 0;
        for (int i = 0; i < missions.Count; i++)
        {
            MissionCategory category = GetMissionCategory(missions[i]);
            if (category == MissionCategory.Focus || category == MissionCategory.Skill)
            {
                count++;
            }
        }

        return count;
    }

    private bool HasNonFocusMission(List<MissionEntryData> missions)
    {
        for (int i = 0; i < missions.Count; i++)
        {
            MissionCategory category = GetMissionCategory(missions[i]);
            if (category == MissionCategory.Feed || category == MissionCategory.Work)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasDuplicateSkillTargets(List<MissionEntryData> missions)
    {
        HashSet<string> skillIds = new HashSet<string>();
        for (int i = 0; i < missions.Count; i++)
        {
            if (!IsSkillMission(missions[i]) || string.IsNullOrEmpty(missions[i].targetSkillId))
            {
                continue;
            }

            if (!skillIds.Add(missions[i].targetSkillId))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasTypeOverflow(List<MissionEntryData> missions)
    {
        return TryFindOverflowType(missions, out _);
    }

    private bool TryFindOverflowType(List<MissionEntryData> missions, out string overflowType)
    {
        Dictionary<string, int> typeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < missions.Count; i++)
        {
            string missionType = missions[i] != null ? missions[i].missionType ?? string.Empty : string.Empty;
            if (string.IsNullOrEmpty(missionType))
            {
                continue;
            }

            if (!typeCounts.ContainsKey(missionType))
            {
                typeCounts[missionType] = 0;
            }

            typeCounts[missionType]++;
            if (typeCounts[missionType] > MaxSameMissionTypeCount)
            {
                overflowType = missionType;
                return true;
            }
        }

        overflowType = string.Empty;
        return false;
    }

    private int FindLastMissionIndexByType(List<MissionEntryData> missions, string missionType)
    {
        for (int i = missions.Count - 1; i >= 0; i--)
        {
            if (missions[i] != null && string.Equals(missions[i].missionType, missionType, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindReplaceableFocusIndex(List<MissionEntryData> missions)
    {
        for (int i = missions.Count - 1; i >= 0; i--)
        {
            if (GetMissionCategory(missions[i]) == MissionCategory.Skill)
            {
                return i;
            }
        }

        for (int i = missions.Count - 1; i >= 0; i--)
        {
            if (GetMissionCategory(missions[i]) == MissionCategory.Focus)
            {
                return i;
            }
        }

        return -1;
    }

    private int CountMissionType(List<MissionEntryData> missions, string missionType)
    {
        int count = 0;
        for (int i = 0; i < missions.Count; i++)
        {
            if (missions[i] != null && string.Equals(missions[i].missionType, missionType, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    private int CountSkillMode(List<MissionEntryData> missions, string skillMissionMode)
    {
        int count = 0;
        for (int i = 0; i < missions.Count; i++)
        {
            if (missions[i] != null &&
                string.Equals(missions[i].missionType, SkillFocusMissionType, StringComparison.Ordinal) &&
                string.Equals(missions[i].skillMissionMode, skillMissionMode, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    private HashSet<string> CollectSkillTargets(List<MissionEntryData> missions, int ignoreIndex = -1)
    {
        HashSet<string> targets = new HashSet<string>();
        for (int i = 0; i < missions.Count; i++)
        {
            if (i == ignoreIndex || !IsSkillMission(missions[i]) || string.IsNullOrEmpty(missions[i].targetSkillId))
            {
                continue;
            }

            targets.Add(missions[i].targetSkillId);
        }

        return targets;
    }

    private SkillEntry FindAlternativeSkill(List<SkillEntry> availableSkills, HashSet<string> excludedSkillIds)
    {
        if (availableSkills == null)
        {
            return null;
        }

        for (int i = 0; i < availableSkills.Count; i++)
        {
            SkillEntry skill = availableSkills[i];
            if (skill == null || string.IsNullOrEmpty(skill.id))
            {
                continue;
            }

            if (excludedSkillIds == null || !excludedSkillIds.Contains(skill.id))
            {
                return skill;
            }
        }

        return null;
    }

    private void ReplaceMissionAt(List<MissionEntryData> missions, int index, MissionEntryData replacement)
    {
        if (missions == null || replacement == null || index < 0 || index >= missions.Count)
        {
            return;
        }

        missions[index] = replacement;
        NormalizeMission(missions[index]);
        EnsureUniqueMissionIds(missions);
    }

    private void EnsureUniqueMissionIds(List<MissionEntryData> missions)
    {
        if (missions == null)
        {
            return;
        }

        Dictionary<string, int> seen = new Dictionary<string, int>(StringComparer.Ordinal);
        for (int i = 0; i < missions.Count; i++)
        {
            MissionEntryData mission = missions[i];
            if (mission == null)
            {
                continue;
            }

            string baseId = string.IsNullOrEmpty(mission.missionId) ? $"mission_{i}" : mission.missionId;
            if (!seen.ContainsKey(baseId))
            {
                seen[baseId] = 1;
                mission.missionId = baseId;
                continue;
            }

            int suffix = ++seen[baseId];
            mission.missionId = $"{baseId}_{suffix}";
        }
    }

    private MissionDecayDebugSummary AdvancePersonalizationCycle(string currentResetKey, List<SkillEntry> availableSkills)
    {
        MissionDecayDebugSummary summary = new MissionDecayDebugSummary();
        MissionPersonalizationProfileData profile = missionData.personalizationProfile;
        if (profile == null)
        {
            NormalizePersonalizationProfile();
            profile = missionData.personalizationProfile;
        }

        summary.previousLastDecayResetKey = profile.lastDecayResetKey ?? string.Empty;
        summary.newLastDecayResetKey = summary.previousLastDecayResetKey;
        summary.beforeProfile = CreatePersonalizationSummary(availableSkills, false);

        if (string.Equals(profile.lastDecayResetKey, currentResetKey, StringComparison.Ordinal))
        {
            summary.afterProfile = CreatePersonalizationSummary(availableSkills, false);
            return summary;
        }

        summary.resetTriggered = true;
        if (!string.IsNullOrEmpty(missionData.lastDailyResetKey) && missionData.missions != null && missionData.missions.Count > 0)
        {
            ApplyExpiredMissionSignals(missionData.missions, summary);
        }

        ApplyProfileDecay();
        summary.decayApplied = true;
        profile.lastDecayResetKey = currentResetKey;
        summary.newLastDecayResetKey = currentResetKey;
        summary.afterProfile = CreatePersonalizationSummary(availableSkills, true);
        return summary;
    }

    private void ApplyExpiredMissionSignals(List<MissionEntryData> expiredMissions, MissionDecayDebugSummary summary)
    {
        if (expiredMissions == null)
        {
            return;
        }

        for (int i = 0; i < expiredMissions.Count; i++)
        {
            MissionEntryData mission = expiredMissions[i];
            if (mission == null || mission.isCompleted || mission.isClaimed)
            {
                continue;
            }

            AdjustCategorySignal(GetMissionCategory(mission), -IgnoredMissionPenalty);
            if (IsSkillMission(mission) && !string.IsNullOrEmpty(mission.targetSkillId))
            {
                AdjustSkillSignal(mission.targetSkillId, -IgnoredMissionPenalty);
            }

            if (summary != null)
            {
                summary.ignoredMissionPenaltyApplied = true;
                summary.ignoredMissionPenalties.Add(new MissionIgnoredMissionPenaltyDebugInfo
                {
                    missionId = mission.missionId ?? string.Empty,
                    title = GetMissionTitle(mission),
                    category = GetMissionCategory(mission).ToString(),
                    targetSkillId = mission.targetSkillId ?? string.Empty,
                    categoryPenalty = IgnoredMissionPenalty,
                    skillPenalty = IsSkillMission(mission) && !string.IsNullOrEmpty(mission.targetSkillId) ? IgnoredMissionPenalty : 0f
                });
            }
        }

        ClampProfileSignals();
    }

    private void ApplyProfileDecay()
    {
        NormalizePersonalizationProfile();

        MissionPersonalizationProfileData profile = missionData.personalizationProfile;
        profile.feedScore *= ProfileDecayFactor;
        profile.workScore *= ProfileDecayFactor;
        profile.focusScore *= ProfileDecayFactor;
        profile.skillScore *= ProfileDecayFactor;
        profile.recentFocusMinutes *= ProfileDecayFactor;

        if (profile.skillPreferences == null)
        {
            profile.skillPreferences = new List<MissionSkillPreferenceData>();
            return;
        }

        for (int i = profile.skillPreferences.Count - 1; i >= 0; i--)
        {
            MissionSkillPreferenceData preference = profile.skillPreferences[i];
            if (preference == null)
            {
                profile.skillPreferences.RemoveAt(i);
                continue;
            }

            preference.score *= ProfileDecayFactor;
            if (Mathf.Abs(preference.score) < 0.05f)
            {
                profile.skillPreferences.RemoveAt(i);
            }
        }
    }

    private bool IsRelaxedCompositionSettings(CompositionSettings settings)
    {
        return !settings.enforceUniqueSkill || settings.maxFocusBucket > 2 || !settings.requireNonFocus;
    }

    private string GetCompositionAttemptLabel(int attemptIndex)
    {
        switch (attemptIndex)
        {
            case 0:
                return "strict";
            case 1:
                return "relaxed skill uniqueness";
            case 2:
                return "relaxed focus cap";
            case 3:
                return "relaxed non-focus requirement";
            default:
                return $"attempt {attemptIndex + 1}";
        }
    }

    private float GetPersonalizedWeight(MissionCategory category, string skillId, float contextMultiplier)
    {
        float weight = GetCategoryWeight(category) * Mathf.Max(0.01f, contextMultiplier);

        if (category == MissionCategory.Skill && !string.IsNullOrEmpty(skillId))
        {
            weight *= GetSkillWeight(skillId);
        }

        return Mathf.Clamp(weight, MinPersonalizationWeight, MaxPersonalizationWeight);
    }

    private float GetCategoryWeight(MissionCategory category)
    {
        NormalizePersonalizationProfile();
        MissionPersonalizationProfileData profile = missionData.personalizationProfile;
        float score = 0f;

        switch (category)
        {
            case MissionCategory.Feed:
                score = profile.feedScore;
                break;
            case MissionCategory.Work:
                score = profile.workScore;
                break;
            case MissionCategory.Focus:
                score = profile.focusScore + profile.recentFocusMinutes * 0.05f;
                break;
            case MissionCategory.Skill:
                score = profile.skillScore + profile.recentFocusMinutes * 0.04f;
                break;
        }

        return Mathf.Clamp(1f + score * CategoryWeightPerSignal, MinPersonalizationWeight, MaxPersonalizationWeight);
    }

    private float GetSkillWeight(string skillId)
    {
        NormalizePersonalizationProfile();
        if (string.IsNullOrEmpty(skillId))
        {
            return 1f;
        }

        MissionSkillPreferenceData preference = GetSkillPreference(skillId);
        float score = preference != null ? preference.score : 0f;
        return Mathf.Clamp(1f + score * SkillWeightPerSignal, MinPersonalizationWeight, MaxPersonalizationWeight);
    }

    private void AdjustCategorySignal(MissionCategory category, float delta)
    {
        NormalizePersonalizationProfile();
        MissionPersonalizationProfileData profile = missionData.personalizationProfile;

        switch (category)
        {
            case MissionCategory.Feed:
                profile.feedScore += delta;
                break;
            case MissionCategory.Work:
                profile.workScore += delta;
                break;
            case MissionCategory.Focus:
                profile.focusScore += delta;
                break;
            case MissionCategory.Skill:
                profile.skillScore += delta;
                break;
        }
    }

    private void AdjustSkillSignal(string skillId, float delta)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return;
        }

        MissionSkillPreferenceData preference = GetOrCreateSkillPreference(skillId);
        preference.score += delta;
    }

    private MissionSkillPreferenceData GetOrCreateSkillPreference(string skillId)
    {
        NormalizePersonalizationProfile();
        MissionSkillPreferenceData existing = GetSkillPreference(skillId);
        if (existing != null)
        {
            return existing;
        }

        MissionSkillPreferenceData created = new MissionSkillPreferenceData { skillId = skillId, score = 0f };
        missionData.personalizationProfile.skillPreferences.Add(created);
        return created;
    }

    private MissionSkillPreferenceData GetSkillPreference(string skillId)
    {
        NormalizePersonalizationProfile();
        if (string.IsNullOrEmpty(skillId) || missionData.personalizationProfile.skillPreferences == null)
        {
            return null;
        }

        for (int i = 0; i < missionData.personalizationProfile.skillPreferences.Count; i++)
        {
            MissionSkillPreferenceData preference = missionData.personalizationProfile.skillPreferences[i];
            if (preference != null && string.Equals(preference.skillId, skillId, StringComparison.Ordinal))
            {
                return preference;
            }
        }

        return null;
    }

    private void NormalizePersonalizationProfile()
    {
        if (missionData.personalizationProfile == null)
        {
            missionData.personalizationProfile = new MissionPersonalizationProfileData();
        }

        if (missionData.personalizationProfile.skillPreferences == null)
        {
            missionData.personalizationProfile.skillPreferences = new List<MissionSkillPreferenceData>();
        }
    }

    private void ClampProfileSignals()
    {
        NormalizePersonalizationProfile();

        MissionPersonalizationProfileData profile = missionData.personalizationProfile;
        profile.feedScore = Mathf.Clamp(profile.feedScore, -3f, 4.5f);
        profile.workScore = Mathf.Clamp(profile.workScore, -3f, 4.5f);
        profile.focusScore = Mathf.Clamp(profile.focusScore, -3f, 4.5f);
        profile.skillScore = Mathf.Clamp(profile.skillScore, -3f, 4.5f);
        profile.recentFocusMinutes = Mathf.Clamp(profile.recentFocusMinutes, 0f, 120f);

        for (int i = profile.skillPreferences.Count - 1; i >= 0; i--)
        {
            MissionSkillPreferenceData preference = profile.skillPreferences[i];
            if (preference == null || string.IsNullOrEmpty(preference.skillId))
            {
                profile.skillPreferences.RemoveAt(i);
                continue;
            }

            preference.score = Mathf.Clamp(preference.score, -3f, 4.5f);
        }
    }

    private HashSet<string> CollectUsedCandidateKeys(List<MissionEntryData> missions)
    {
        HashSet<string> keys = new HashSet<string>(StringComparer.Ordinal);
        if (missions == null)
        {
            return keys;
        }

        for (int i = 0; i < missions.Count; i++)
        {
            MissionEntryData mission = missions[i];
            if (mission == null)
            {
                continue;
            }

            string key;
            if (IsSkillMission(mission))
            {
                string mode = string.IsNullOrEmpty(mission.skillMissionMode) ? SkillMissionModeMinutes : mission.skillMissionMode;
                key = $"skill_{mode}:{mission.difficulty}:{mission.targetSkillId}";
            }
            else
            {
                key = $"{mission.missionType}:{mission.difficulty}";
            }

            keys.Add(key);
        }

        return keys;
    }

    private MissionCandidate PickWeightedCandidate(List<MissionCandidate> candidates)
    {
        if (candidates == null || candidates.Count == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += Mathf.Max(0f, candidates[i].weight);
        }

        if (totalWeight <= 0f)
        {
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= Mathf.Max(0f, candidates[i].weight);
            if (roll <= 0f)
            {
                return candidates[i];
            }
        }

        return candidates[candidates.Count - 1];
    }

    private List<MissionDifficulty> BuildDifficultyPlan(int missionCount)
    {
        List<MissionDifficulty> plan = new List<MissionDifficulty>();
        if (missionCount <= 0)
        {
            return plan;
        }

        plan.Add(MissionDifficulty.Easy);
        int hardCount = 0;

        for (int i = 1; i < missionCount; i++)
        {
            MissionDifficulty next = PickWeightedDifficulty(hardCount < MaxHardMissionsPerDay);
            if (next == MissionDifficulty.Hard)
            {
                hardCount++;
            }

            plan.Add(next);
        }

        bool hasMedium = false;
        for (int i = 0; i < plan.Count; i++)
        {
            if (plan[i] == MissionDifficulty.Medium)
            {
                hasMedium = true;
                break;
            }
        }

        if (missionCount >= 3 && !hasMedium)
        {
            int replaceIndex = plan.Count - 1;
            if (replaceIndex > 0)
            {
                if (plan[replaceIndex] == MissionDifficulty.Hard)
                {
                    hardCount = Mathf.Max(0, hardCount - 1);
                }

                plan[replaceIndex] = MissionDifficulty.Medium;
            }
        }

        return plan;
    }

    private MissionDifficulty PickWeightedDifficulty(bool allowHard)
    {
        int easyWeight = EasyDifficultyWeight;
        int mediumWeight = MediumDifficultyWeight;
        int hardWeight = allowHard ? HardDifficultyWeight : 0;
        int totalWeight = easyWeight + mediumWeight + hardWeight;
        if (totalWeight <= 0)
        {
            return MissionDifficulty.Easy;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        if (roll < easyWeight)
        {
            return MissionDifficulty.Easy;
        }

        roll -= easyWeight;
        if (roll < mediumWeight)
        {
            return MissionDifficulty.Medium;
        }

        return MissionDifficulty.Hard;
    }

    private List<SkillEntry> OrderSkills(List<SkillEntry> skills)
    {
        List<SkillEntry> ordered = new List<SkillEntry>();
        if (skills == null)
        {
            return ordered;
        }

        List<IndexedSkill> indexed = new List<IndexedSkill>();
        for (int i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && !string.IsNullOrEmpty(skills[i].id))
            {
                indexed.Add(new IndexedSkill { skill = skills[i], sourceIndex = i });
            }
        }

        indexed.Sort((a, b) =>
        {
            float axisA = SkillProgressionModel.GetAxisPercent(a.skill.totalSP);
            float axisB = SkillProgressionModel.GetAxisPercent(b.skill.totalSP);
            int axisComparison = axisB.CompareTo(axisA);
            if (axisComparison != 0)
            {
                return axisComparison;
            }

            int minutesComparison = b.skill.totalFocusMinutes.CompareTo(a.skill.totalFocusMinutes);
            if (minutesComparison != 0)
            {
                return minutesComparison;
            }

            return a.sourceIndex.CompareTo(b.sourceIndex);
        });

        for (int i = 0; i < indexed.Count; i++)
        {
            ordered.Add(indexed[i].skill);
        }

        return ordered;
    }

    private SkillEntry PickSkillForMission(List<SkillEntry> availableSkills, HashSet<string> usedSkillIds, int missionIndex)
    {
        if (availableSkills == null || availableSkills.Count == 0)
        {
            return null;
        }

        for (int i = 0; i < availableSkills.Count; i++)
        {
            SkillEntry candidate = availableSkills[(missionIndex + i) % availableSkills.Count];
            if (candidate == null || string.IsNullOrEmpty(candidate.id))
            {
                continue;
            }

            if (usedSkillIds == null || !usedSkillIds.Contains(candidate.id) || usedSkillIds.Count >= availableSkills.Count)
            {
                return candidate;
            }
        }

        return availableSkills[0];
    }

    private float GetDifficultyRewardMultiplier(MissionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MissionDifficulty.Hard:
                return HardRewardMultiplier;
            case MissionDifficulty.Medium:
                return MediumRewardMultiplier;
            default:
                return 1f;
        }
    }

    private int GetFeedTarget(MissionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MissionDifficulty.Hard:
                return 3;
            case MissionDifficulty.Medium:
                return 2;
            default:
                return 1;
        }
    }

    private int GetWorkTarget(MissionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MissionDifficulty.Hard:
                return 5;
            case MissionDifficulty.Medium:
                return 3;
            default:
                return 1;
        }
    }

    private int GetFocusMinutesTarget(MissionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MissionDifficulty.Hard:
                return 45;
            case MissionDifficulty.Medium:
                return 30;
            default:
                return 15;
        }
    }

    private int GetSkillSessionTarget(MissionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case MissionDifficulty.Hard:
                return 3;
            case MissionDifficulty.Medium:
                return 2;
            default:
                return 1;
        }
    }

    private bool MatchesGenericMission(MissionEntryData mission, string missionIdOrType)
    {
        if (mission == null)
        {
            return false;
        }

        if (string.Equals(mission.missionId, missionIdOrType, StringComparison.Ordinal))
        {
            return true;
        }

        if ((string.Equals(missionIdOrType, GenericFeedMissionId, StringComparison.Ordinal) || missionIdOrType.StartsWith("feed_", StringComparison.Ordinal)) &&
            string.Equals(mission.missionType, FeedMissionType, StringComparison.Ordinal))
        {
            return true;
        }

        if ((string.Equals(missionIdOrType, GenericWorkMissionId, StringComparison.Ordinal) || missionIdOrType.StartsWith("work_", StringComparison.Ordinal)) &&
            string.Equals(mission.missionType, WorkMissionType, StringComparison.Ordinal))
        {
            return true;
        }

        if ((string.Equals(missionIdOrType, GenericFocusMissionId, StringComparison.Ordinal) || missionIdOrType.StartsWith("focus_", StringComparison.Ordinal)) &&
            IsGenericFocusMission(mission))
        {
            return true;
        }

        return false;
    }

    private bool IsGenericFocusMission(MissionEntryData mission)
    {
        return mission != null
            && !IsSkillMission(mission)
            && string.Equals(mission.missionType, FocusMissionType, StringComparison.Ordinal);
    }

}
