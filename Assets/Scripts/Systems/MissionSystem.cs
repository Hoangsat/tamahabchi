using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class MissionClaimResult
{
    public bool success;
    public int rewardCoins;
    public int rewardXp;
    public int rewardMood;
    public int rewardEnergy;
    public float rewardSkillPercent;
    public string rewardSkillId = string.Empty;
    public int rewardChestCount;
    public int rewardedMissionCount;
    public string sourceTitle = string.Empty;
}

[Serializable]
public class SkillMissionProgressResult
{
    public bool anyProgress;
    public bool anyCompleted;
    public string title = string.Empty;
    public float currentValue;
    public float targetValue;
    public string unitLabel = string.Empty;
}

[Serializable]
public class MissionSelectionResult
{
    public bool success;
    public string message = string.Empty;
    public int selectedSkillMissionCount;
}

[Serializable]
public class MissionCreationResult
{
    public bool success;
    public string message = string.Empty;
    public int creationCost;
    public MissionEntryData createdMission;
}

[Serializable]
public class MissionBonusStatus
{
    public int selectedSkillMissionCount;
    public int completedSelectedSkillMissionCount;
    public bool isReady;
    public bool isClaimed;
}

public class MissionSystem
{
    private enum MissionCategory
    {
        Focus,
        Skill,
        Feed,
        Work,
        Other
    }

    private struct CompositionSettings
    {
        public bool enforceUniqueSkill;
        public int maxFocusBucket;
        public bool requireNonFocus;
    }

    private const string SkillFocusMissionType = "skill_focus";
    private const string FeedMissionType = "feed";
    private const string WorkMissionType = "work";
    private const string FocusMissionType = "focus";
    private const string RoutineMissionType = "routine";

    private const string SkillMissionModeMinutes = "minutes";
    private const string SkillMissionModeSessions = "sessions";

    private const string GenericFeedMissionId = "generic_feed";
    private const string GenericWorkMissionId = "generic_work";
    private const string GenericFocusMissionId = "generic_focus";

    private const int MaxDailyMissionCount = 5;
    private const int MaxHardMissionsPerDay = 1;
    private const int MaxSkillMissionsPerDay = 2;
    private const int MaxSameMissionTypeCount = 3;
    private const int MaxSelectedSkillMissions = 5;
    private const int FreeCustomRoutineCount = 3;
    private const int CustomRoutineCostCoins = 15;
    private const int SkillMissionBonusCoins = 30;
    private const int SkillMissionBonusChests = 1;

    private const float RewardPreviewSkillPercentPerMinute = 0.2f;
    private const float RewardPreviewSkillPercentPerSession = 3f;

    private const int EasyDifficultyWeight = 50;
    private const int MediumDifficultyWeight = 35;
    private const int HardDifficultyWeight = 15;

    private const float MediumRewardMultiplier = 1.25f;
    private const float HardRewardMultiplier = 1.5f;
    private const float ProfileDecayFactor = 0.65f;
    private const float PositiveActionSignal = 1f;
    private const float FocusMinutesSignalScale = 1f / 15f;
    private const float IgnoredMissionPenalty = 0.5f;
    private const float MinPersonalizationWeight = 0.75f;
    private const float MaxPersonalizationWeight = 1.35f;
    private const float CategoryWeightPerSignal = 0.08f;
    private const float SkillWeightPerSignal = 0.1f;

    private MissionData missionData;
    private MissionGenerationDebugSnapshot lastGenerationDebugSnapshot;
    private MissionGenerationDebugSnapshot activeGenerationDebugSnapshot;
    private string activeCompositionAttemptLabel = string.Empty;
    private bool missionDebugLoggingEnabled;

    public MissionSystem(bool debugLoggingEnabled = false)
    {
        missionDebugLoggingEnabled = debugLoggingEnabled;
    }

    public void Init(MissionData data)
    {
        missionData = data ?? new MissionData();
        if (missionData.missions == null)
        {
            missionData.missions = new List<MissionEntryData>();
        }

        NormalizeAllMissions();
        NormalizePersonalizationProfile();
    }

    public void SetDebugLoggingEnabled(bool enabled)
    {
        missionDebugLoggingEnabled = enabled;
    }

    public bool IsDebugLoggingEnabled()
    {
        return missionDebugLoggingEnabled;
    }

    public void RecordFeedAction()
    {
        NormalizePersonalizationProfile();
        missionData.personalizationProfile.feedScore += PositiveActionSignal;
        ClampProfileSignals();
    }

    public void RecordWorkAction()
    {
        NormalizePersonalizationProfile();
        missionData.personalizationProfile.workScore += PositiveActionSignal;
        ClampProfileSignals();
    }

    public void RecordFocusAction(string skillId, float completedMinutes)
    {
        NormalizePersonalizationProfile();

        float focusSignal = PositiveActionSignal + Mathf.Max(0f, completedMinutes) * FocusMinutesSignalScale;
        missionData.personalizationProfile.focusScore += focusSignal;
        missionData.personalizationProfile.recentFocusMinutes += Mathf.Max(0f, completedMinutes);

        if (!string.IsNullOrEmpty(skillId))
        {
            missionData.personalizationProfile.skillScore += PositiveActionSignal;
            MissionSkillPreferenceData preference = GetOrCreateSkillPreference(skillId);
            preference.score += PositiveActionSignal + Mathf.Max(0f, completedMinutes) * FocusMinutesSignalScale;
        }

        ClampProfileSignals();
    }

    public List<MissionEntryData> GetActiveMissions()
    {
        NormalizeAllMissions();
        return new List<MissionEntryData>(missionData.missions);
    }

    public List<MissionEntryData> GetSkillMissions()
    {
        NormalizeAllMissions();
        List<MissionEntryData> missions = missionData.missions.FindAll(IsSkillMission);
        missions.Sort(CompareSkillMissionsForDisplay);
        return missions;
    }

    public List<MissionEntryData> GetRoutineMissions()
    {
        NormalizeAllMissions();
        List<MissionEntryData> missions = missionData.missions.FindAll(IsRoutineMission);
        missions.Sort(CompareRoutineMissionsForDisplay);
        return missions;
    }

    public MissionSelectionResult SelectMission(string missionId)
    {
        NormalizeAllMissions();

        MissionSelectionResult result = new MissionSelectionResult();
        MissionEntryData mission = missionData.missions.Find(entry => entry != null && entry.missionId == missionId);
        if (!IsSkillMission(mission))
        {
            result.message = "Skill mission unavailable";
            result.selectedSkillMissionCount = GetSelectedSkillMissionCount();
            return result;
        }

        if (mission.isSelected)
        {
            result.success = true;
            result.selectedSkillMissionCount = GetSelectedSkillMissionCount();
            return result;
        }

        if (GetSelectedSkillMissionCount() >= MaxSelectedSkillMissions)
        {
            result.message = $"Only {MaxSelectedSkillMissions} skill missions can be active";
            result.selectedSkillMissionCount = GetSelectedSkillMissionCount();
            return result;
        }

        mission.isSelected = true;
        mission.hasSelectionState = true;
        result.success = true;
        result.selectedSkillMissionCount = GetSelectedSkillMissionCount();
        return result;
    }

    public MissionSelectionResult UnselectMission(string missionId)
    {
        NormalizeAllMissions();

        MissionSelectionResult result = new MissionSelectionResult();
        MissionEntryData mission = missionData.missions.Find(entry => entry != null && entry.missionId == missionId);
        if (!IsSkillMission(mission))
        {
            result.message = "Skill mission unavailable";
            result.selectedSkillMissionCount = GetSelectedSkillMissionCount();
            return result;
        }

        mission.isSelected = false;
        mission.hasSelectionState = true;
        result.success = true;
        result.selectedSkillMissionCount = GetSelectedSkillMissionCount();
        return result;
    }

    public MissionCreationResult CreateSkillMission(string skillId, string skillName, int durationMinutes, int baseFocusReward, int focusXpGain)
    {
        NormalizeAllMissions();

        MissionCreationResult result = new MissionCreationResult();
        string normalizedSkillId = string.IsNullOrWhiteSpace(skillId) ? string.Empty : skillId.Trim();
        if (string.IsNullOrEmpty(normalizedSkillId))
        {
            result.message = "Choose a skill";
            return result;
        }

        int clampedMinutes = Mathf.Clamp(durationMinutes, 15, 120);
        MissionEntryData mission = new MissionEntryData
        {
            missionId = "custom_skill_" + Guid.NewGuid().ToString("N"),
            missionType = SkillFocusMissionType,
            skillMissionMode = SkillMissionModeMinutes,
            targetSkillId = normalizedSkillId,
            skillId = normalizedSkillId,
            targetSkillName = string.IsNullOrWhiteSpace(skillName) ? "Unknown Skill" : skillName.Trim(),
            requiredMinutes = clampedMinutes,
            targetProgress = clampedMinutes,
            rewardCoins = Mathf.Max(0, Mathf.RoundToInt(baseFocusReward * (clampedMinutes / 15f))),
            rewardXp = Mathf.Max(0, Mathf.RoundToInt(focusXpGain * (clampedMinutes / 15f))),
            rewardSkillPercent = Mathf.Max(0.5f, clampedMinutes * RewardPreviewSkillPercentPerMinute),
            isCustom = true,
            isRoutine = false,
            isSelected = GetSelectedSkillMissionCount() < MaxSelectedSkillMissions,
            hasSelectionState = true
        };

        mission.title = GetMissionTitle(mission);
        NormalizeMission(mission);
        missionData.missions.Add(mission);
        result.success = true;
        result.createdMission = mission;
        return result;
    }

    public MissionCreationResult CreateRoutine(string title, int rewardCoins, int rewardXp, int rewardMood, int rewardEnergy, float rewardSkillPercent, string rewardSkillId)
    {
        NormalizeAllMissions();

        MissionCreationResult result = new MissionCreationResult();
        result.creationCost = GetRoutineCreationCost();
        string normalizedTitle = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        if (string.IsNullOrEmpty(normalizedTitle))
        {
            result.message = "Enter a title";
            return result;
        }

        if (normalizedTitle.Length > 30)
        {
            normalizedTitle = normalizedTitle.Substring(0, 30);
        }

        MissionEntryData mission = new MissionEntryData
        {
            missionId = "custom_routine_" + Guid.NewGuid().ToString("N"),
            missionType = RoutineMissionType,
            title = normalizedTitle,
            targetProgress = 1,
            rewardCoins = Mathf.Max(0, rewardCoins),
            rewardXp = Mathf.Max(0, rewardXp),
            rewardMood = Mathf.Max(0, rewardMood),
            rewardEnergy = Mathf.Max(0, rewardEnergy),
            rewardSkillPercent = Mathf.Max(0f, rewardSkillPercent),
            rewardSkillId = string.IsNullOrWhiteSpace(rewardSkillId) ? string.Empty : rewardSkillId.Trim(),
            isRoutine = true,
            isCustom = true,
            isManualRoutine = true,
            autoClaimOnComplete = true
        };

        missionData.customRoutineCreateCount++;
        NormalizeMission(mission);
        missionData.missions.Add(mission);
        result.success = true;
        result.createdMission = mission;
        return result;
    }

    public int GetRoutineCreationCost()
    {
        NormalizeAllMissions();
        return missionData.customRoutineCreateCount < FreeCustomRoutineCount ? 0 : CustomRoutineCostCoins;
    }

    public MissionClaimResult CompleteRoutine(string missionId)
    {
        NormalizeAllMissions();

        MissionEntryData mission = missionData.missions.Find(entry => entry != null && entry.missionId == missionId);
        if (!IsRoutineMission(mission) || mission.isClaimed)
        {
            return new MissionClaimResult();
        }

        if (mission.requiredMinutes > 0f)
        {
            mission.progressMinutes = mission.requiredMinutes;
        }
        else
        {
            mission.currentProgress = mission.targetProgress;
        }

        RefreshCompletion(mission);
        return ClaimRoutineMission(mission);
    }

    public MissionClaimResult CollectCompletedRoutineRewards()
    {
        NormalizeAllMissions();

        MissionClaimResult aggregate = new MissionClaimResult();
        for (int i = 0; i < missionData.missions.Count; i++)
        {
            MissionEntryData mission = missionData.missions[i];
            if (!IsRoutineMission(mission) || !mission.autoClaimOnComplete || !mission.isCompleted || mission.isClaimed)
            {
                continue;
            }

            AccumulateRewards(aggregate, ClaimRoutineMission(mission));
        }

        aggregate.success = aggregate.rewardedMissionCount > 0;
        return aggregate;
    }

    public MissionBonusStatus GetSkillMissionBonusStatus()
    {
        NormalizeAllMissions();

        int selectedCount = GetSelectedSkillMissionCount();
        int completedSelectedCount = GetCompletedSelectedSkillMissionCount();
        return new MissionBonusStatus
        {
            selectedSkillMissionCount = selectedCount,
            completedSelectedSkillMissionCount = completedSelectedCount,
            isReady = selectedCount == MaxSelectedSkillMissions && completedSelectedCount == MaxSelectedSkillMissions && !missionData.skillBonusClaimed,
            isClaimed = missionData.skillBonusClaimed
        };
    }

    public MissionClaimResult ClaimSkillMissionBonus()
    {
        NormalizeAllMissions();

        MissionBonusStatus status = GetSkillMissionBonusStatus();
        if (!status.isReady)
        {
            return new MissionClaimResult();
        }

        missionData.skillBonusClaimed = true;
        return new MissionClaimResult
        {
            success = true,
            rewardCoins = SkillMissionBonusCoins,
            rewardChestCount = SkillMissionBonusChests,
            sourceTitle = "5/5 Skill Bonus"
        };
    }

    public List<MissionEntryData> GetVisibleMissions(int maxCount)
    {
        NormalizeAllMissions();

        List<MissionEntryData> visible = new List<MissionEntryData>();
        if (maxCount <= 0)
        {
            return visible;
        }

        for (int i = 0; i < missionData.missions.Count && visible.Count < maxCount; i++)
        {
            if (missionData.missions[i] != null)
            {
                visible.Add(missionData.missions[i]);
            }
        }

        return visible;
    }

    public MissionGenerationDebugSnapshot GetLastGenerationDebugSnapshot()
    {
        return lastGenerationDebugSnapshot != null ? lastGenerationDebugSnapshot.Clone() : null;
    }

    public string GetMissionDebugSummary()
    {
        return lastGenerationDebugSnapshot != null ? lastGenerationDebugSnapshot.compactReport ?? string.Empty : string.Empty;
    }

    public void EnsureDailySkillMissions(List<SkillEntry> skills, int currentResetBucket)
    {
        NormalizeAllMissions();
        NormalizePersonalizationProfile();

        string currentResetKey = GetDailyResetKey(currentResetBucket);
        if (string.IsNullOrEmpty(currentResetKey))
        {
            return;
        }

        bool needsFullGeneration = missionData.missions.Count == 0 || !string.Equals(missionData.lastDailyResetKey, currentResetKey, StringComparison.Ordinal);

        if (needsFullGeneration)
        {
            MissionDecayDebugSummary decaySummary = AdvancePersonalizationCycle(currentResetKey, skills);
            GenerateDailyMissions(skills, currentResetKey, decaySummary);
            return;
        }

        EnsureSkillMissionCoverage(skills);
    }

    public bool IncrementGenericMissionProgress(string missionIdOrType, int amount = 1)
    {
        if (string.IsNullOrEmpty(missionIdOrType) || amount <= 0)
        {
            return false;
        }

        NormalizeAllMissions();

        bool anyProgress = false;
        for (int i = 0; i < missionData.missions.Count; i++)
        {
            MissionEntryData mission = missionData.missions[i];
            if (!CanProgressMission(mission) || !MatchesGenericMission(mission, missionIdOrType))
            {
                continue;
            }

            if (mission.requiredMinutes > 0f)
            {
                continue;
            }

            mission.currentProgress = Mathf.Min(mission.targetProgress, mission.currentProgress + amount);
            RefreshCompletion(mission);
            anyProgress = true;
        }

        return anyProgress;
    }

    public bool ApplyGenericFocusProgress(float completedMinutes, bool sessionCompleted)
    {
        if (completedMinutes <= 0f && !sessionCompleted)
        {
            return false;
        }

        NormalizeAllMissions();

        bool anyProgress = false;
        for (int i = 0; i < missionData.missions.Count; i++)
        {
            MissionEntryData mission = missionData.missions[i];
            if (!CanProgressMission(mission) || !IsGenericFocusMission(mission))
            {
                continue;
            }

            if (mission.requiredMinutes > 0f)
            {
                float nextMinutes = mission.progressMinutes + Mathf.Max(0f, completedMinutes);
                mission.progressMinutes = Mathf.Min(mission.requiredMinutes, nextMinutes);
                mission.currentProgress = Mathf.Min(mission.targetProgress, Mathf.FloorToInt(mission.progressMinutes));
            }
            else if (sessionCompleted)
            {
                mission.currentProgress = Mathf.Min(mission.targetProgress, mission.currentProgress + 1);
            }

            RefreshCompletion(mission);
            anyProgress = true;
        }

        return anyProgress;
    }

    public SkillMissionProgressResult ApplySkillFocusProgress(string completedSkillId, float completedMinutes, bool sessionCompleted)
    {
        SkillMissionProgressResult result = new SkillMissionProgressResult();
        if (string.IsNullOrEmpty(completedSkillId))
        {
            return result;
        }

        NormalizeAllMissions();

        for (int i = 0; i < missionData.missions.Count; i++)
        {
            MissionEntryData mission = missionData.missions[i];
            if (!CanProgressMission(mission) || !IsSkillMission(mission) || !mission.isSelected)
            {
                continue;
            }

            if (!string.Equals(mission.targetSkillId, completedSkillId, StringComparison.Ordinal))
            {
                continue;
            }

            bool progressedThisMission = false;
            if (string.Equals(mission.skillMissionMode, SkillMissionModeSessions, StringComparison.Ordinal))
            {
                if (sessionCompleted)
                {
                    mission.currentProgress = Mathf.Min(mission.targetProgress, mission.currentProgress + 1);
                    progressedThisMission = true;
                }
            }
            else
            {
                if (completedMinutes > 0f)
                {
                    mission.progressMinutes = Mathf.Min(mission.requiredMinutes, mission.progressMinutes + completedMinutes);
                    mission.currentProgress = Mathf.Min(mission.targetProgress, Mathf.FloorToInt(mission.progressMinutes));
                    progressedThisMission = true;
                }
            }

            if (!progressedThisMission)
            {
                continue;
            }

            bool wasCompleted = mission.isCompleted;
            RefreshCompletion(mission);

            result.anyProgress = true;
            result.anyCompleted |= !wasCompleted && mission.isCompleted;
            if (string.IsNullOrEmpty(result.title))
            {
                result.title = GetMissionTitle(mission);
                if (string.Equals(mission.skillMissionMode, SkillMissionModeSessions, StringComparison.Ordinal))
                {
                    result.currentValue = mission.currentProgress;
                    result.targetValue = mission.targetProgress;
                    result.unitLabel = "sessions";
                }
                else
                {
                    result.currentValue = mission.progressMinutes;
                    result.targetValue = mission.requiredMinutes;
                    result.unitLabel = "min";
                }
            }
        }

        return result;
    }

    public MissionClaimResult ClaimMission(string missionId)
    {
        MissionClaimResult result = new MissionClaimResult();
        if (string.IsNullOrEmpty(missionId))
        {
            return result;
        }

        NormalizeAllMissions();

        MissionEntryData mission = missionData.missions.Find(entry => entry != null && entry.missionId == missionId);
        if (mission == null || !mission.isCompleted || mission.isClaimed)
        {
            return result;
        }

        mission.isClaimed = true;
        return BuildRewardResultFromMission(mission);
    }

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
            8,
            10
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
            10,
            12
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
            12,
            15
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
            return CreateGenericWorkMission(difficulty);
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
            18,
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
            20,
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
        int baseXp,
        string targetSkillId = "",
        string targetSkillName = "",
        string skillMissionMode = "")
    {
        float rewardMultiplier = GetDifficultyRewardMultiplier(difficulty);
        int finalCoins = Mathf.Max(1, Mathf.RoundToInt(baseCoins * rewardMultiplier));
        int finalXp = Mathf.Max(1, Mathf.RoundToInt(baseXp * rewardMultiplier));

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
            rewardXp = finalXp,
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
        MissionEntryData genericWork = generated.Find(m => string.Equals(m.missionType, WorkMissionType, StringComparison.Ordinal));
        MissionEntryData genericFeed = generated.Find(m => string.Equals(m.missionType, FeedMissionType, StringComparison.Ordinal));
        List<MissionEntryData> skillMissions = generated.FindAll(IsSkillMission);

        List<MissionEntryData> ordered = new List<MissionEntryData>();
        if (genericFocus != null) ordered.Add(genericFocus);
        if (skillMissions.Count > 0) ordered.Add(skillMissions[0]);
        if (genericWork != null) ordered.Add(genericWork);
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
            return CreateGenericWorkMission(difficulty);
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
            return CreateGenericWorkMission(difficulty);
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
            return CreateGenericWorkMission(difficulty);
        }

        if (currentCategory == MissionCategory.Work)
        {
            return CreateGenericFeedMission(difficulty);
        }

        return feedCount <= workCount
            ? CreateGenericFeedMission(difficulty)
            : CreateGenericWorkMission(difficulty);
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
            int percentComparison = b.skill.percent.CompareTo(a.skill.percent);
            if (percentComparison != 0)
            {
                return percentComparison;
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

    private bool IsRoutineMission(MissionEntryData mission)
    {
        return mission != null && (mission.isRoutine || !IsSkillMission(mission));
    }

    private bool IsSkillMission(MissionEntryData mission)
    {
        return mission != null
            && string.Equals(mission.missionType, SkillFocusMissionType, StringComparison.Ordinal);
    }

    private bool CanProgressMission(MissionEntryData mission)
    {
        return mission != null && !mission.isCompleted && !mission.isClaimed;
    }

    private void NormalizeAllMissions()
    {
        if (missionData == null)
        {
            missionData = new MissionData();
        }

        if (missionData.missions == null)
        {
            missionData.missions = new List<MissionEntryData>();
        }

        for (int i = 0; i < missionData.missions.Count; i++)
        {
            NormalizeMission(missionData.missions[i]);
        }

        int selectedCount = 0;
        for (int i = 0; i < missionData.missions.Count; i++)
        {
            MissionEntryData mission = missionData.missions[i];
            if (!IsSkillMission(mission) || !mission.isSelected)
            {
                continue;
            }

            selectedCount++;
            if (selectedCount > MaxSelectedSkillMissions)
            {
                mission.isSelected = false;
            }
        }
    }

    private void NormalizeMission(MissionEntryData mission)
    {
        if (mission == null)
        {
            return;
        }

        mission.missionId = mission.missionId ?? string.Empty;
        mission.missionType = mission.missionType ?? string.Empty;
        mission.skillMissionMode = mission.skillMissionMode ?? string.Empty;
        mission.title = mission.title ?? string.Empty;
        mission.skillId = mission.skillId ?? string.Empty;
        mission.targetSkillId = mission.targetSkillId ?? string.Empty;
        mission.targetSkillName = mission.targetSkillName ?? string.Empty;

        if (string.IsNullOrEmpty(mission.targetSkillId) && !string.IsNullOrEmpty(mission.skillId))
        {
            mission.targetSkillId = mission.skillId;
        }

        if (string.IsNullOrEmpty(mission.skillId) && !string.IsNullOrEmpty(mission.targetSkillId))
        {
            mission.skillId = mission.targetSkillId;
        }

        if (IsSkillMission(mission) && string.IsNullOrEmpty(mission.skillMissionMode))
        {
            mission.skillMissionMode = mission.requiredMinutes > 0f ? SkillMissionModeMinutes : SkillMissionModeSessions;
        }

        mission.targetProgress = Mathf.Max(1, mission.targetProgress);
        mission.requiredMinutes = Mathf.Max(0f, mission.requiredMinutes);
        mission.progressMinutes = Mathf.Max(0f, mission.progressMinutes);
        mission.currentProgress = Mathf.Max(0, mission.currentProgress);
        mission.rewardCoins = Mathf.Max(0, mission.rewardCoins);
        mission.rewardXp = Mathf.Max(0, mission.rewardXp);
        mission.rewardMood = Mathf.Max(0, mission.rewardMood);
        mission.rewardEnergy = Mathf.Max(0, mission.rewardEnergy);
        mission.rewardSkillPercent = Mathf.Max(0f, mission.rewardSkillPercent);
        mission.rewardSkillId = mission.rewardSkillId ?? string.Empty;

        if (IsSkillMission(mission))
        {
            mission.isRoutine = false;
            if (!mission.hasSelectionState)
            {
                mission.isSelected = true;
                mission.hasSelectionState = true;
            }

            if (mission.rewardSkillPercent <= 0f)
            {
                if (string.Equals(mission.skillMissionMode, SkillMissionModeSessions, StringComparison.Ordinal))
                {
                    mission.rewardSkillPercent = Mathf.Max(0.5f, mission.targetProgress * RewardPreviewSkillPercentPerSession);
                }
                else
                {
                    float previewMinutes = mission.requiredMinutes > 0f ? mission.requiredMinutes : mission.targetProgress;
                    mission.rewardSkillPercent = Mathf.Max(0.5f, previewMinutes * RewardPreviewSkillPercentPerMinute);
                }
            }
        }
        else
        {
            mission.isRoutine = true;
            mission.isSelected = false;
            mission.hasSelectionState = false;
            if (!mission.isManualRoutine)
            {
                mission.autoClaimOnComplete = true;
            }
        }

        if (string.IsNullOrEmpty(mission.title))
        {
            mission.title = GetMissionTitle(mission);
        }

        RefreshCompletion(mission);
    }

    private void RefreshCompletion(MissionEntryData mission)
    {
        if (mission == null)
        {
            return;
        }

        bool completed;
        if (mission.requiredMinutes > 0f)
        {
            mission.progressMinutes = Mathf.Min(mission.requiredMinutes, mission.progressMinutes);
            completed = mission.progressMinutes >= mission.requiredMinutes;
        }
        else
        {
            mission.currentProgress = Mathf.Min(mission.targetProgress, mission.currentProgress);
            completed = mission.currentProgress >= mission.targetProgress;
        }

        if (completed)
        {
            mission.isCompleted = true;
        }
    }

    private string GetMissionTitle(MissionEntryData mission)
    {
        if (mission == null)
        {
            return "Mission";
        }

        if (IsSkillMission(mission))
        {
            string skillName = !string.IsNullOrEmpty(mission.targetSkillName) ? mission.targetSkillName : "Unknown Skill";
            if (string.Equals(mission.skillMissionMode, SkillMissionModeSessions, StringComparison.Ordinal))
            {
                return $"Complete {mission.targetProgress} focus sessions on {skillName}";
            }

            float targetMinutes = mission.requiredMinutes > 0f ? mission.requiredMinutes : mission.targetProgress;
            return $"Focus {targetMinutes:0.#} min on {skillName}";
        }

        switch (mission.missionType)
        {
            case FeedMissionType:
                return $"Feed pet {mission.targetProgress} times";
            case WorkMissionType:
                return $"Work {mission.targetProgress} times";
            case FocusMissionType:
                if (mission.requiredMinutes > 0f)
                {
                    return $"Focus {mission.requiredMinutes:0.#} min";
                }

                return $"Complete {mission.targetProgress} focus sessions";
            case RoutineMissionType:
                return !string.IsNullOrEmpty(mission.title) ? mission.title : "Routine";
            default:
                return !string.IsNullOrEmpty(mission.title) ? mission.title : "Mission";
        }
    }

    private int GetSelectedSkillMissionCount()
    {
        int count = 0;
        for (int i = 0; i < missionData.missions.Count; i++)
        {
            if (IsSkillMission(missionData.missions[i]) && missionData.missions[i].isSelected)
            {
                count++;
            }
        }

        return count;
    }

    private int GetCompletedSelectedSkillMissionCount()
    {
        int count = 0;
        for (int i = 0; i < missionData.missions.Count; i++)
        {
            MissionEntryData mission = missionData.missions[i];
            if (IsSkillMission(mission) && mission.isSelected && mission.isCompleted)
            {
                count++;
            }
        }

        return count;
    }

    private int CompareSkillMissionsForDisplay(MissionEntryData left, MissionEntryData right)
    {
        int selectedCompare = right.isSelected.CompareTo(left.isSelected);
        if (selectedCompare != 0) return selectedCompare;

        int claimedCompare = left.isClaimed.CompareTo(right.isClaimed);
        if (claimedCompare != 0) return claimedCompare;

        int completedCompare = right.isCompleted.CompareTo(left.isCompleted);
        if (completedCompare != 0) return completedCompare;

        return string.Compare(GetMissionTitle(left), GetMissionTitle(right), StringComparison.Ordinal);
    }

    private int CompareRoutineMissionsForDisplay(MissionEntryData left, MissionEntryData right)
    {
        int claimedCompare = left.isClaimed.CompareTo(right.isClaimed);
        if (claimedCompare != 0) return claimedCompare;

        int completedCompare = right.isCompleted.CompareTo(left.isCompleted);
        if (completedCompare != 0) return completedCompare;

        return string.Compare(GetMissionTitle(left), GetMissionTitle(right), StringComparison.Ordinal);
    }

    private MissionClaimResult ClaimRoutineMission(MissionEntryData mission)
    {
        if (!IsRoutineMission(mission) || mission.isClaimed || !mission.isCompleted)
        {
            return new MissionClaimResult();
        }

        mission.isClaimed = true;
        return BuildRewardResultFromMission(mission);
    }

    private MissionClaimResult BuildRewardResultFromMission(MissionEntryData mission)
    {
        return new MissionClaimResult
        {
            success = true,
            rewardCoins = Mathf.Max(0, mission.rewardCoins),
            rewardXp = Mathf.Max(0, mission.rewardXp),
            rewardMood = Mathf.Max(0, mission.rewardMood),
            rewardEnergy = Mathf.Max(0, mission.rewardEnergy),
            rewardSkillPercent = Mathf.Max(0f, mission.rewardSkillPercent),
            rewardSkillId = mission.rewardSkillId ?? string.Empty,
            rewardedMissionCount = 1,
            sourceTitle = GetMissionTitle(mission)
        };
    }

    private void AccumulateRewards(MissionClaimResult target, MissionClaimResult incoming)
    {
        if (target == null || incoming == null || !incoming.success)
        {
            return;
        }

        target.rewardCoins += incoming.rewardCoins;
        target.rewardXp += incoming.rewardXp;
        target.rewardMood += incoming.rewardMood;
        target.rewardEnergy += incoming.rewardEnergy;
        target.rewardChestCount += incoming.rewardChestCount;
        target.rewardedMissionCount += incoming.rewardedMissionCount;

        if (incoming.rewardSkillPercent > 0f && string.IsNullOrEmpty(target.rewardSkillId))
        {
            target.rewardSkillId = incoming.rewardSkillId;
            target.rewardSkillPercent += incoming.rewardSkillPercent;
        }
        else if (!string.IsNullOrEmpty(incoming.rewardSkillId) && string.Equals(target.rewardSkillId, incoming.rewardSkillId, StringComparison.Ordinal))
        {
            target.rewardSkillPercent += incoming.rewardSkillPercent;
        }

        if (string.IsNullOrEmpty(target.sourceTitle))
        {
            target.sourceTitle = incoming.sourceTitle;
        }
    }

    private string GetSafeSkillName(SkillEntry skill)
    {
        if (skill == null || string.IsNullOrEmpty(skill.name))
        {
            return "Unknown Skill";
        }

        return skill.name;
    }

    private string GetDailyResetKey(int resetBucket)
    {
        return TimeService.FormatResetBucket(resetBucket);
    }

    private struct IndexedSkill
    {
        public SkillEntry skill;
        public int sourceIndex;
    }

    private struct MissionWeightBreakdown
    {
        public float baseWeight;
        public float categoryMultiplier;
        public float skillMultiplier;
        public float contextMultiplier;
        public float postClampMultiplier;
        public float rawWeight;
        public float clampedWeight;
        public float personalizationMultiplier;
        public float finalWeight;
    }

    private class MissionCandidate
    {
        public string key;
        public float weight;
        public Func<MissionEntryData> build;
        public MissionCandidateDebugInfo debugInfo;
    }
}
