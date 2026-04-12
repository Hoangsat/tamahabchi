using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[Serializable]
public class MissionClaimResult
{
    public bool success;
    public int rewardCoins;
    public int rewardMood;
    public int rewardEnergy;
    public int rewardSkillSP;
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

public partial class MissionSystem
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

    private const int RewardPreviewSkillSPPerMinute = 2;
    private const int RewardPreviewSkillSPPerSession = 30;

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

    public int GetAvailableClaimCount()
    {
        NormalizeAllMissions();

        int count = 0;
        for (int i = 0; i < missionData.missions.Count; i++)
        {
            MissionEntryData mission = missionData.missions[i];
            if (mission != null && mission.isCompleted && !mission.isClaimed)
            {
                count++;
            }
        }

        return count;
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
        List<MissionEntryData> missions = missionData.missions.FindAll(IsManualRoutineMission);
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

    public MissionCreationResult CreateSkillMission(string skillId, string skillName, int durationMinutes, int baseFocusReward)
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
            rewardSkillSP = Mathf.Max(5, clampedMinutes * RewardPreviewSkillSPPerMinute),
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

    public MissionCreationResult CreateRoutine(string title, int rewardCoins, int rewardMood, int rewardEnergy, int rewardSkillSP, string rewardSkillId)
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
            rewardMood = Mathf.Max(0, rewardMood),
            rewardEnergy = Mathf.Max(0, rewardEnergy),
            rewardSkillSP = Mathf.Max(0, rewardSkillSP),
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
        if (!IsManualRoutineMission(mission) || mission.isClaimed)
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

            AccumulateRewards(aggregate, ClaimAutoClaimMission(mission));
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

    private bool IsRoutineMission(MissionEntryData mission)
    {
        return mission != null && (mission.isRoutine || !IsSkillMission(mission));
    }

    private bool IsManualRoutineMission(MissionEntryData mission)
    {
        return mission != null
            && (mission.isManualRoutine || string.Equals(mission.missionType, RoutineMissionType, StringComparison.Ordinal));
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

        ConvertLegacyWorkMission(mission);

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
        mission.rewardMood = Mathf.Max(0, mission.rewardMood);
        mission.rewardEnergy = Mathf.Max(0, mission.rewardEnergy);
        if (mission.rewardSkillSP <= 0 && mission.rewardSkillPercent > 0f)
        {
            mission.rewardSkillSP = Mathf.Max(5, Mathf.RoundToInt(mission.rewardSkillPercent * 10f));
        }

        mission.rewardSkillSP = Mathf.Max(0, mission.rewardSkillSP);
        mission.rewardSkillId = mission.rewardSkillId ?? string.Empty;

        if (IsSkillMission(mission))
        {
            mission.isRoutine = false;
            if (!mission.hasSelectionState)
            {
                mission.isSelected = true;
                mission.hasSelectionState = true;
            }

            if (mission.rewardSkillSP <= 0)
            {
                if (string.Equals(mission.skillMissionMode, SkillMissionModeSessions, StringComparison.Ordinal))
                {
                    mission.rewardSkillSP = Mathf.Max(5, mission.targetProgress * RewardPreviewSkillSPPerSession);
                }
                else
                {
                    float previewMinutes = mission.requiredMinutes > 0f ? mission.requiredMinutes : mission.targetProgress;
                    mission.rewardSkillSP = Mathf.Max(5, Mathf.RoundToInt(previewMinutes * RewardPreviewSkillSPPerMinute));
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

    private void ConvertLegacyWorkMission(MissionEntryData mission)
    {
        if (mission == null || !string.Equals(mission.missionType, WorkMissionType, StringComparison.Ordinal))
        {
            return;
        }

        string previousMissionId = mission.missionId;
        int targetMinutes = mission.requiredMinutes > 0f
            ? Mathf.RoundToInt(mission.requiredMinutes)
            : GetFocusMinutesTarget(mission.difficulty);
        targetMinutes = Mathf.Max(15, targetMinutes);

        float progress01 = mission.targetProgress > 0
            ? Mathf.Clamp01(mission.currentProgress / (float)mission.targetProgress)
            : 0f;

        mission.missionType = FocusMissionType;
        mission.missionId = string.IsNullOrEmpty(previousMissionId)
            ? "legacy_focus_from_work"
            : $"legacy_focus_from_{previousMissionId}";
        mission.requiredMinutes = targetMinutes;
        mission.targetProgress = targetMinutes;
        mission.progressMinutes = Mathf.Clamp(targetMinutes * progress01, 0f, targetMinutes);
        mission.currentProgress = Mathf.RoundToInt(mission.progressMinutes);
        mission.title = string.Empty;
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
        if (!IsManualRoutineMission(mission) || mission.isClaimed || !mission.isCompleted)
        {
            return new MissionClaimResult();
        }

        mission.isClaimed = true;
        return BuildRewardResultFromMission(mission);
    }

    private MissionClaimResult ClaimAutoClaimMission(MissionEntryData mission)
    {
        if (!IsRoutineMission(mission) || mission.isClaimed || !mission.isCompleted || !mission.autoClaimOnComplete)
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
            rewardMood = Mathf.Max(0, mission.rewardMood),
            rewardEnergy = Mathf.Max(0, mission.rewardEnergy),
            rewardSkillSP = Mathf.Max(0, mission.rewardSkillSP),
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
        target.rewardMood += incoming.rewardMood;
        target.rewardEnergy += incoming.rewardEnergy;
        target.rewardChestCount += incoming.rewardChestCount;
        target.rewardedMissionCount += incoming.rewardedMissionCount;

        if (incoming.rewardSkillSP > 0 && string.IsNullOrEmpty(target.rewardSkillId))
        {
            target.rewardSkillId = incoming.rewardSkillId;
            target.rewardSkillSP += incoming.rewardSkillSP;
        }
        else if (!string.IsNullOrEmpty(incoming.rewardSkillId) && string.Equals(target.rewardSkillId, incoming.rewardSkillId, StringComparison.Ordinal))
        {
            target.rewardSkillSP += incoming.rewardSkillSP;
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

