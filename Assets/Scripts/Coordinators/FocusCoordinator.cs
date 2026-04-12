using System;
using UnityEngine;

public sealed class FocusCoordinatorCallbacks
{
    public Action OnCoinsChanged;
    public Action OnPetChanged;
    public Action OnPetFlowChanged;
    public Action OnProgressionChanged;
    public Action OnSkillsChanged;
    public Action<SkillProgressResult> OnSkillProgressAdded;
    public Action OnFocusSessionChanged;
    public Action<FocusSessionResultData> OnFocusResultReady;
    public Func<int> GetCurrentResetBucket;
    public Action SaveGame;
    public Action UpdateUi;
    public Action UpdateMissionUi;
    public Action RefreshOnboardingCompletion;
    public Action UpdateOnboardingUi;
    public Action<string> ShowFeedback;
    public Action<MissionClaimResult, bool> ApplyMissionRewards;
}

public sealed class FocusCoordinator
{
    private readonly FocusSystem focusSystem;
    private readonly SkillsSystem skillsSystem;
    private readonly ProgressionSystem progressionSystem;
    private readonly PetSystem petSystem;
    private readonly MissionSystem missionSystem;
    private readonly CurrencySystem currencySystem;
    private readonly ProgressionData progressionData;
    private readonly OnboardingData onboardingData;
    private readonly BalanceConfig balanceConfig;
    private readonly FocusCoordinatorCallbacks callbacks;

    private string selectedFocusSkillId = string.Empty;
    private FocusSessionResultData lastFocusSessionResult;

    public FocusCoordinator(
        FocusSystem focusSystem,
        SkillsSystem skillsSystem,
        ProgressionSystem progressionSystem,
        PetSystem petSystem,
        MissionSystem missionSystem,
        CurrencySystem currencySystem,
        ProgressionData progressionData,
        OnboardingData onboardingData,
        BalanceConfig balanceConfig,
        FocusCoordinatorCallbacks callbacks)
    {
        this.focusSystem = focusSystem;
        this.skillsSystem = skillsSystem;
        this.progressionSystem = progressionSystem;
        this.petSystem = petSystem;
        this.missionSystem = missionSystem;
        this.currencySystem = currencySystem;
        this.progressionData = progressionData;
        this.onboardingData = onboardingData;
        this.balanceConfig = balanceConfig;
        this.callbacks = callbacks ?? new FocusCoordinatorCallbacks();
    }

    public void ResetRuntimeState()
    {
        selectedFocusSkillId = string.Empty;
        lastFocusSessionResult = null;
    }

    public bool Update(float deltaTime, bool focusBlockedByNeglect)
    {
        if (focusSystem == null)
        {
            return false;
        }

        bool focusCompleted = false;
        if (focusSystem.IsRunning && !focusBlockedByNeglect)
        {
            focusCompleted = focusSystem.Update(deltaTime);
            if (focusCompleted)
            {
                HandleFocusCompleted();
            }
        }

        return focusSystem.HasActiveSession || focusCompleted;
    }

    public bool TryStartSession(string skillId, int durationMinutes)
    {
        if (focusSystem == null || skillsSystem == null)
        {
            callbacks.ShowFeedback?.Invoke("Focus unavailable");
            return false;
        }

        if (focusSystem.HasActiveSession)
        {
            callbacks.ShowFeedback?.Invoke("Focus already active");
            return false;
        }

        SkillEntry skill = skillsSystem.GetSkillById(skillId);
        if (skill == null)
        {
            callbacks.ShowFeedback?.Invoke("Select a valid skill");
            return false;
        }

        if (durationMinutes <= 0)
        {
            callbacks.ShowFeedback?.Invoke("Select a duration");
            return false;
        }

        if (petSystem != null && petSystem.IsNeglected())
        {
            callbacks.ShowFeedback?.Invoke("Pet neglected. Care first.");
            return false;
        }

        petSystem?.ReduceMood(1f);
        callbacks.OnPetChanged?.Invoke();
        callbacks.OnPetFlowChanged?.Invoke();

        selectedFocusSkillId = skill.id;
        lastFocusSessionResult = null;
        focusSystem.StartFocus(durationMinutes * 60f, skill.id);
        callbacks.OnSkillsChanged?.Invoke();
        callbacks.OnFocusSessionChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.ShowFeedback?.Invoke($"Focus started: {skill.name}");
        return true;
    }

    public bool PauseSession()
    {
        if (focusSystem == null || !focusSystem.PauseFocus())
        {
            return false;
        }

        callbacks.OnFocusSessionChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.ShowFeedback?.Invoke("Focus paused");
        return true;
    }

    public bool ResumeSession()
    {
        if (focusSystem == null || !focusSystem.ResumeFocus())
        {
            return false;
        }

        callbacks.OnFocusSessionChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.ShowFeedback?.Invoke("Focus resumed");
        return true;
    }

    public bool CancelSession()
    {
        if (focusSystem == null || !focusSystem.CancelFocus())
        {
            return false;
        }

        lastFocusSessionResult = null;
        callbacks.OnFocusSessionChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.ShowFeedback?.Invoke("Focus cancelled");
        callbacks.SaveGame?.Invoke();
        return true;
    }

    public bool CancelForForcedStop()
    {
        if (focusSystem == null || !focusSystem.CancelFocus())
        {
            return false;
        }

        lastFocusSessionResult = null;
        callbacks.OnFocusSessionChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        return true;
    }

    public bool PauseForAppPause()
    {
        if (focusSystem == null || !focusSystem.IsRunning)
        {
            return false;
        }

        if (!focusSystem.PauseFocus())
        {
            return false;
        }

        callbacks.OnFocusSessionChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        return true;
    }

    public bool FinishSessionEarly()
    {
        if (focusSystem == null || !focusSystem.FinishFocusEarly())
        {
            return false;
        }

        HandleFocusCompleted();
        callbacks.UpdateUi?.Invoke();
        return true;
    }

    public FocusSessionSnapshot GetSnapshot()
    {
        return focusSystem != null ? focusSystem.GetSnapshot() : new FocusSessionSnapshot();
    }

    public FocusSessionResultData GetLastResult()
    {
        return lastFocusSessionResult != null ? lastFocusSessionResult.Clone() : null;
    }

    public bool ClearLastResult(bool notify = true)
    {
        if (lastFocusSessionResult == null)
        {
            return false;
        }

        lastFocusSessionResult = null;
        if (notify)
        {
            callbacks.OnFocusSessionChanged?.Invoke();
        }

        return true;
    }

    public bool SetSelectedSkill(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            selectedFocusSkillId = string.Empty;
            callbacks.OnSkillsChanged?.Invoke();
            return true;
        }

        if (skillsSystem == null)
        {
            return false;
        }

        SkillEntry skill = skillsSystem.GetSkillById(skillId);
        if (skill == null)
        {
            return false;
        }

        selectedFocusSkillId = skill.id;
        callbacks.OnSkillsChanged?.Invoke();
        return true;
    }

    public string GetSelectedSkill()
    {
        return selectedFocusSkillId;
    }

    public FocusStateData CreateSaveData(string savedAtUtc)
    {
        return new FocusStateData
        {
            selectedSkillId = selectedFocusSkillId ?? string.Empty,
            activeSession = focusSystem != null ? focusSystem.CreateSaveData(savedAtUtc) : null,
            lastResult = lastFocusSessionResult != null ? lastFocusSessionResult.Clone() : null
        };
    }

    public void RestoreState(FocusStateData focusStateData, double offlineElapsedSeconds)
    {
        selectedFocusSkillId = focusStateData != null ? (focusStateData.selectedSkillId ?? string.Empty) : string.Empty;
        lastFocusSessionResult = focusStateData != null && focusStateData.lastResult != null
            ? focusStateData.lastResult.Clone()
            : null;

        if (focusSystem == null || focusStateData == null || focusStateData.activeSession == null)
        {
            return;
        }

        bool completedWhileOffline;
        if (!focusSystem.RestoreSession(focusStateData.activeSession, offlineElapsedSeconds, out completedWhileOffline))
        {
            return;
        }

        if (completedWhileOffline)
        {
            HandleFocusCompleted(false, false, false);
        }
    }

    private void HandleFocusCompleted()
    {
        HandleFocusCompleted(true, true, true);
    }

    private void HandleFocusCompleted(bool notifyUi, bool showFeedback, bool saveAfter)
    {
        if (focusSystem == null || !focusSystem.TryConsumeCompletedSession(out FocusSessionCompletionData completionData) || completionData == null)
        {
            return;
        }

        string completedSkillId = completionData.skillId ?? string.Empty;
        float completedDurationSeconds = completionData.actualDurationSeconds;
        float completedMinutes = completedDurationSeconds / 60f;
        float plannedDurationSeconds = Mathf.Max(0f, completionData.plannedDurationSeconds);
        float baselineSeconds = Mathf.Max(60f, balanceConfig.baseFocusDuration * 60f);

        int basePlannedCoinsReward = CalculateScaledReward(
            progressionSystem != null ? progressionSystem.GetFocusReward(balanceConfig.baseFocusReward) : 0,
            plannedDurationSeconds,
            baselineSeconds);
        int basePlannedXpReward = 0;

        FocusRewardData rewardData = focusSystem.BuildReward(
            plannedDurationSeconds,
            completedDurationSeconds,
            basePlannedCoinsReward,
            basePlannedXpReward,
            0f,
            0f);

        int focusReward = rewardData.coins;
        int focusXpReward = 0;
        SkillProgressResult skillProgressResult = null;
        SkillMissionProgressResult missionProgressResult = null;

        if (!string.IsNullOrEmpty(completedSkillId) && skillsSystem != null)
        {
            int skillPointsReward = skillsSystem.CalculateSkillPointsFromFocusDuration(completedDurationSeconds);
            skillProgressResult = skillsSystem.ApplySkillPoints(
                completedSkillId,
                skillPointsReward,
                completedDurationSeconds,
                TimeService.GetUtcNow().ToString("O"),
                balanceConfig.goldenSkillFocusXpBonus);

            if (skillProgressResult.success && skillProgressResult.deltaSP > 0)
            {
                callbacks.OnSkillProgressAdded?.Invoke(skillProgressResult);
                callbacks.OnSkillsChanged?.Invoke();
            }

            int currentResetBucket = callbacks.GetCurrentResetBucket != null
                ? callbacks.GetCurrentResetBucket()
                : TimeService.GetCurrentResetBucketLocal();
            missionSystem?.EnsureDailySkillMissions(skillsSystem.GetSkills(), currentResetBucket);
            if (missionSystem != null)
            {
                missionProgressResult = missionSystem.ApplySkillFocusProgress(completedSkillId, completedMinutes, true);
            }
        }

        currencySystem?.AddCoins(focusReward);
        callbacks.OnCoinsChanged?.Invoke();

        missionSystem?.RecordFocusAction(completedSkillId, completedMinutes);
        missionSystem?.ApplyGenericFocusProgress(completedMinutes, true);
        callbacks.ApplyMissionRewards?.Invoke(missionSystem?.CollectCompletedRoutineRewards(), false);

        if (onboardingData != null)
        {
            onboardingData.didFocus = true;
            callbacks.RefreshOnboardingCompletion?.Invoke();
            callbacks.UpdateOnboardingUi?.Invoke();
        }

        lastFocusSessionResult = BuildFocusSessionResult(
            completionData,
            focusReward,
            focusXpReward,
            rewardData,
            skillProgressResult,
            completedSkillId);

        string feedbackMessage = completionData.completedEarly
            ? $"Focus completed early: +{focusReward} Coins"
            : $"Focus complete: +{focusReward} Coins";

        if (skillProgressResult != null && skillProgressResult.deltaSP > 0)
        {
            feedbackMessage += $" | +{skillProgressResult.deltaSP} SP";
        }

        if (skillProgressResult != null && skillProgressResult.leveledUp)
        {
            feedbackMessage += $" | Lv.{skillProgressResult.previousLevel} -> Lv.{skillProgressResult.newLevel}";
        }

        if (skillProgressResult != null && skillProgressResult.becameGolden)
        {
            feedbackMessage += " | Skill Golden";
        }

        if (missionProgressResult != null)
        {
            if (missionProgressResult.anyCompleted)
            {
                feedbackMessage += " | Mission ready to claim";
            }
            else if (missionProgressResult.anyProgress)
            {
                feedbackMessage += $" | {missionProgressResult.title} {missionProgressResult.currentValue:0.#}/{missionProgressResult.targetValue:0.#} {missionProgressResult.unitLabel}";
            }
        }

        if (showFeedback)
        {
            callbacks.ShowFeedback?.Invoke(feedbackMessage);
        }

        if (notifyUi)
        {
            callbacks.OnFocusSessionChanged?.Invoke();
            callbacks.OnFocusResultReady?.Invoke(lastFocusSessionResult != null ? lastFocusSessionResult.Clone() : null);
            callbacks.UpdateMissionUi?.Invoke();
        }

        if (saveAfter)
        {
            callbacks.SaveGame?.Invoke();
        }
    }

    private int CalculateScaledReward(int baseReward, float plannedDurationSeconds, float baselineSeconds)
    {
        if (baseReward <= 0 || plannedDurationSeconds <= 0f || baselineSeconds <= 0f)
        {
            return 0;
        }

        float durationScale = Mathf.Max(0f, plannedDurationSeconds / baselineSeconds);
        return Mathf.Max(0, Mathf.RoundToInt(baseReward * durationScale));
    }

    private FocusSessionResultData BuildFocusSessionResult(
        FocusSessionCompletionData completionData,
        int coinsReward,
        int xpReward,
        FocusRewardData rewardData,
        SkillProgressResult skillProgressResult,
        string completedSkillId)
    {
        SkillEntry skill = skillsSystem != null ? skillsSystem.GetSkillById(completedSkillId) : null;
        return new FocusSessionResultData
        {
            outcome = completionData.completedEarly ? FocusSessionOutcome.CompletedEarly : FocusSessionOutcome.Completed,
            skillId = completedSkillId ?? string.Empty,
            skillName = skill != null ? skill.name : "Unknown Skill",
            skillIcon = skill != null ? skill.icon : string.Empty,
            plannedDurationSeconds = completionData.plannedDurationSeconds,
            actualDurationSeconds = completionData.actualDurationSeconds,
            skillSpReward = skillProgressResult != null ? skillProgressResult.deltaSP : 0,
            previousTotalSP = skillProgressResult != null ? skillProgressResult.previousTotalSP : 0,
            newTotalSP = skillProgressResult != null ? skillProgressResult.newTotalSP : 0,
            previousLevel = skillProgressResult != null ? skillProgressResult.previousLevel : 0,
            newLevel = skillProgressResult != null ? skillProgressResult.newLevel : 0,
            previousAxisPercent = skillProgressResult != null ? skillProgressResult.previousAxisPercent : 0f,
            newAxisPercent = skillProgressResult != null ? skillProgressResult.newAxisPercent : 0f,
            previousProgressInLevel01 = skillProgressResult != null ? skillProgressResult.previousProgressInLevel01 : 0f,
            newProgressInLevel01 = skillProgressResult != null ? skillProgressResult.newProgressInLevel01 : 0f,
            previousPercent = skillProgressResult != null ? skillProgressResult.previousAxisPercent : 0f,
            newPercent = skillProgressResult != null ? skillProgressResult.newAxisPercent : 0f,
            deltaProgress = skillProgressResult != null ? skillProgressResult.newAxisPercent - skillProgressResult.previousAxisPercent : 0f,
            coinsReward = coinsReward,
            xpReward = xpReward,
            energyReward = 0f,
            moodReward = 0f,
            energyBefore = petSystem != null ? petSystem.GetEnergyPercent() : 0f,
            energyAfter = petSystem != null ? petSystem.GetEnergyPercent() : 0f,
            petReaction = "Steady",
            lowEnergyPenaltyApplied = false,
            becameGolden = skillProgressResult != null && skillProgressResult.becameGolden,
            isGolden = skillProgressResult != null && skillProgressResult.isGolden
        };
    }
}
