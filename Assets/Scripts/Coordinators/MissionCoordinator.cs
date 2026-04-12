using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class MissionCoordinatorCallbacks
{
    public Action OnCoinsChanged;
    public Action BroadcastPetStateChanged;
    public Action OnSkillsChanged;
    public Action<SkillProgressResult> OnSkillProgressAdded;
    public Action SaveGame;
    public Action UpdateMissionUi;
    public Action<string> ShowFeedback;
    public Func<int> GetCurrentResetBucket;
}

public sealed class MissionCoordinator
{
    private readonly MissionSystem missionSystem;
    private readonly SkillsSystem skillsSystem;
    private readonly ProgressionSystem progressionSystem;
    private readonly CurrencySystem currencySystem;
    private readonly PetSystem petSystem;
    private readonly BalanceConfig balanceConfig;
    private readonly MissionCoordinatorCallbacks callbacks;

    public MissionCoordinator(
        MissionSystem missionSystem,
        SkillsSystem skillsSystem,
        ProgressionSystem progressionSystem,
        CurrencySystem currencySystem,
        PetSystem petSystem,
        BalanceConfig balanceConfig,
        MissionCoordinatorCallbacks callbacks)
    {
        this.missionSystem = missionSystem;
        this.skillsSystem = skillsSystem;
        this.progressionSystem = progressionSystem;
        this.currencySystem = currencySystem;
        this.petSystem = petSystem;
        this.balanceConfig = balanceConfig;
        this.callbacks = callbacks ?? new MissionCoordinatorCallbacks();
    }

    public List<MissionEntryData> GetActiveMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetActiveMissions() : new List<MissionEntryData>();
    }

    public int GetAvailableClaimCount()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetAvailableClaimCount() : 0;
    }

    public List<MissionEntryData> GetSkillMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetSkillMissions() : new List<MissionEntryData>();
    }

    public List<MissionEntryData> GetRoutineMissions()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetRoutineMissions() : new List<MissionEntryData>();
    }

    public MissionBonusStatus GetSkillMissionBonusStatus()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetSkillMissionBonusStatus() : new MissionBonusStatus();
    }

    public int GetRoutineCreationCost()
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetRoutineCreationCost() : 0;
    }

    public string GetMissionResetCountdownLabel()
    {
        DateTime localNow = TimeService.GetLocalNow();
        DateTime nextReset = new DateTime(localNow.Year, localNow.Month, localNow.Day, TimeService.DailyResetHourLocal, 0, 0);
        if (localNow >= nextReset)
        {
            nextReset = nextReset.AddDays(1);
        }

        TimeSpan remaining = nextReset - localNow;
        if (remaining.TotalHours >= 1d)
        {
            return remaining.ToString(@"hh\:mm");
        }

        return remaining.ToString(@"mm\:ss");
    }

    public bool SelectMission(string missionId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionSelectionResult result = missionSystem.SelectMission(missionId);
        message = result.message;
        if (!result.success)
        {
            return false;
        }

        callbacks.SaveGame?.Invoke();
        callbacks.UpdateMissionUi?.Invoke();
        return true;
    }

    public bool UnselectMission(string missionId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionSelectionResult result = missionSystem.UnselectMission(missionId);
        message = result.message;
        if (!result.success)
        {
            return false;
        }

        callbacks.SaveGame?.Invoke();
        callbacks.UpdateMissionUi?.Invoke();
        return true;
    }

    public bool CompleteRoutineMission(string missionId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionClaimResult result = missionSystem.CompleteRoutine(missionId);
        if (!result.success)
        {
            message = "Routine unavailable";
            return false;
        }

        message = result.sourceTitle;
        ApplyClaimResult(result, true);
        return true;
    }

    public bool ClaimSkillMissionBonus(out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionClaimResult result = missionSystem.ClaimSkillMissionBonus();
        if (!result.success)
        {
            message = "Bonus not ready";
            return false;
        }

        message = result.sourceTitle;
        ApplyClaimResult(result, true);
        return true;
    }

    public bool CreateCustomSkillMission(string skillId, int durationMinutes, out string message)
    {
        message = string.Empty;
        if (missionSystem == null || skillsSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        SkillEntry skill = skillsSystem.GetSkillById(skillId);
        int baseFocusReward = balanceConfig != null ? balanceConfig.baseFocusReward : 0;
        MissionCreationResult result = missionSystem.CreateSkillMission(
            skillId,
            skill != null ? skill.name : string.Empty,
            durationMinutes,
            progressionSystem != null ? progressionSystem.GetFocusReward(baseFocusReward) : baseFocusReward);

        message = result.message;
        if (!result.success)
        {
            return false;
        }

        callbacks.SaveGame?.Invoke();
        callbacks.UpdateMissionUi?.Invoke();
        return true;
    }

    public bool CreateRoutineMission(string title, int rewardCoins, int rewardMood, int rewardEnergy, int rewardSkillSP, string rewardSkillId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null || currencySystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        int creationCost = missionSystem.GetRoutineCreationCost();
        if (creationCost > 0 && !currencySystem.SpendCoins(creationCost))
        {
            message = $"Need {creationCost} coins";
            return false;
        }

        MissionCreationResult result = missionSystem.CreateRoutine(title, rewardCoins, rewardMood, rewardEnergy, rewardSkillSP, rewardSkillId);
        message = result.message;
        if (!result.success)
        {
            if (creationCost > 0)
            {
                currencySystem.AddCoins(creationCost);
            }

            return false;
        }

        if (creationCost > 0)
        {
            callbacks.OnCoinsChanged?.Invoke();
        }

        callbacks.SaveGame?.Invoke();
        callbacks.UpdateMissionUi?.Invoke();
        return true;
    }

    public bool TryClaimMission(string missionId, out string message)
    {
        message = string.Empty;
        if (missionSystem == null)
        {
            message = "Mission system unavailable";
            return false;
        }

        MissionClaimResult claimResult = missionSystem.ClaimMission(missionId);
        if (!claimResult.success)
        {
            message = "Mission not ready";
            return false;
        }

        message = claimResult.sourceTitle;
        ApplyClaimResult(claimResult, true);
        return true;
    }

    public void ApplyClaimResult(MissionClaimResult claimResult, bool saveAfter)
    {
        if (claimResult == null || !claimResult.success)
        {
            return;
        }

        if (claimResult.rewardCoins > 0 && currencySystem != null)
        {
            currencySystem.AddCoins(claimResult.rewardCoins);
            callbacks.OnCoinsChanged?.Invoke();
        }

        if (claimResult.rewardMood > 0)
        {
            petSystem?.AddMood(claimResult.rewardMood);
            callbacks.BroadcastPetStateChanged?.Invoke();
        }

        if (claimResult.rewardEnergy > 0)
        {
            petSystem?.ApplyCare(claimResult.rewardEnergy);
            callbacks.BroadcastPetStateChanged?.Invoke();
        }

        if (claimResult.rewardSkillSP > 0 && skillsSystem != null && !string.IsNullOrEmpty(claimResult.rewardSkillId))
        {
            SkillProgressResult skillProgressResult = skillsSystem.ApplySkillPoints(
                claimResult.rewardSkillId,
                claimResult.rewardSkillSP,
                0f,
                TimeService.GetUtcNow().ToString("O"),
                balanceConfig != null ? balanceConfig.goldenSkillFocusXpBonus : 0f);

            if (skillProgressResult.success && skillProgressResult.deltaSP > 0)
            {
                callbacks.OnSkillProgressAdded?.Invoke(skillProgressResult);
                callbacks.OnSkillsChanged?.Invoke();
            }
        }

        callbacks.UpdateMissionUi?.Invoke();
        callbacks.ShowFeedback?.Invoke(BuildRewardFeedback(claimResult));

        if (saveAfter)
        {
            callbacks.SaveGame?.Invoke();
        }
    }

    public List<MissionEntryData> GetVisibleMissions(int maxCount)
    {
        EnsureDailySkillMissionsCurrent();
        return missionSystem != null ? missionSystem.GetVisibleMissions(maxCount) : new List<MissionEntryData>();
    }

    public void EnsureDailySkillMissionsCurrent()
    {
        if (missionSystem == null || skillsSystem == null)
        {
            return;
        }

        int resetBucket = callbacks.GetCurrentResetBucket != null ? callbacks.GetCurrentResetBucket.Invoke() : 0;
        missionSystem.EnsureDailySkillMissions(skillsSystem.GetSkills(), resetBucket);
    }

    private string BuildRewardFeedback(MissionClaimResult claimResult)
    {
        string rewardSummary = $"+{claimResult.rewardCoins} Coins";
        if (claimResult.rewardMood > 0)
        {
            rewardSummary += $", +{claimResult.rewardMood} Mood";
        }

        if (claimResult.rewardEnergy > 0)
        {
            rewardSummary += $", +{claimResult.rewardEnergy} Care";
        }

        if (claimResult.rewardSkillSP > 0)
        {
            rewardSummary += $", +{claimResult.rewardSkillSP} SP";
        }

        if (claimResult.rewardChestCount > 0)
        {
            rewardSummary += $", +{claimResult.rewardChestCount} Chest";
        }

        string label = string.IsNullOrEmpty(claimResult.sourceTitle) ? "Mission" : claimResult.sourceTitle;
        return $"{label}: {rewardSummary}";
    }
}
