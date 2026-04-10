using System;
using UnityEngine;

public sealed class FocusCoordinatorCallbacks
{
    public Action OnCoinsChanged;
    public Action OnPetChanged;
    public Action OnPetFlowChanged;
    public Action OnProgressionChanged;
    public Action OnSkillsChanged;
    public Action<string, float, float> OnSkillProgressAdded;
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

    public bool Update(float deltaTime, bool petIsDead)
    {
        if (focusSystem == null)
        {
            return false;
        }

        bool focusCompleted = false;
        if (focusSystem.IsRunning && !petIsDead)
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

    public void ClearLastResult(bool notify = true)
    {
        lastFocusSessionResult = null;
        if (notify)
        {
            callbacks.OnFocusSessionChanged?.Invoke();
        }
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
        float energyBeforeFocusReward = petSystem != null ? petSystem.GetEnergyPercent() : 0f;
        float plannedDurationSeconds = Mathf.Max(0f, completionData.plannedDurationSeconds);
        float baselineSeconds = Mathf.Max(60f, balanceConfig.baseFocusDuration * 60f);

        int basePlannedCoinsReward = CalculateScaledReward(
            progressionSystem != null ? progressionSystem.GetFocusReward(balanceConfig.baseFocusReward) : 0,
            plannedDurationSeconds,
            baselineSeconds);
        int basePlannedXpReward = CalculateScaledReward(
            balanceConfig.focusXpGain,
            plannedDurationSeconds,
            baselineSeconds);
        float plannedEnergyReward = Mathf.Max(0f, plannedDurationSeconds / 60f) * Mathf.Max(0f, balanceConfig.focusEnergyRewardPerMinute);
        float plannedMoodReward = Mathf.Max(0f, plannedDurationSeconds / 60f) * Mathf.Max(0f, balanceConfig.focusMoodRewardPerMinute);

        FocusRewardData rewardData = focusSystem.BuildReward(
            plannedDurationSeconds,
            completedDurationSeconds,
            basePlannedCoinsReward,
            basePlannedXpReward,
            plannedEnergyReward,
            plannedMoodReward);

        int focusReward = rewardData.coins;
        int focusXpReward = rewardData.xp;
        SkillProgressResult skillProgressResult = null;
        SkillMissionProgressResult missionProgressResult = null;
        bool lowEnergyPenaltyApplied = false;

        if (!string.IsNullOrEmpty(completedSkillId) && skillsSystem != null)
        {
            focusXpReward = skillsSystem.ApplyFocusXpBonus(completedSkillId, focusXpReward);
        }

        if (energyBeforeFocusReward < balanceConfig.lowEnergyThreshold)
        {
            float rewardMultiplier = Mathf.Max(0f, balanceConfig.lowEnergyRewardMultiplier);
            focusReward = Mathf.Max(0, Mathf.FloorToInt(focusReward * rewardMultiplier));
            focusXpReward = Mathf.Max(0, Mathf.FloorToInt(focusXpReward * rewardMultiplier));
            lowEnergyPenaltyApplied = true;
        }

        if (petSystem != null)
        {
            if (rewardData.mood > 0f)
            {
                petSystem.AddMood(rewardData.mood);
            }

            if (rewardData.energy > 0f)
            {
                petSystem.AddEnergy(rewardData.energy);
            }

            callbacks.OnPetChanged?.Invoke();
            callbacks.OnPetFlowChanged?.Invoke();
        }

        if (!string.IsNullOrEmpty(completedSkillId) && skillsSystem != null)
        {
            float progressAmount = skillsSystem.CalculateProgressFromFocusDuration(
                completedDurationSeconds,
                progressionSystem != null ? progressionSystem.GetLevel() : 1,
                petSystem != null ? petSystem.GetMoodPercent() : 0f,
                balanceConfig.skillMinutesPerStep,
                balanceConfig.skillLevelMultiplierStep,
                balanceConfig.skillMoodBaseBonus,
                balanceConfig.skillMoodScale);

            skillProgressResult = skillsSystem.ApplyFocusProgress(
                completedSkillId,
                progressAmount,
                completedDurationSeconds,
                TimeService.GetUtcNow().ToString("O"),
                balanceConfig.goldenSkillFocusXpBonus);

            if (skillProgressResult.success && skillProgressResult.deltaApplied > 0f)
            {
                callbacks.OnSkillProgressAdded?.Invoke(completedSkillId, skillProgressResult.deltaApplied, skillProgressResult.newPercent);
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
        AddXp(focusXpReward, false);

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
            energyBeforeFocusReward,
            petSystem != null ? petSystem.GetEnergyPercent() : energyBeforeFocusReward,
            rewardData,
            lowEnergyPenaltyApplied,
            skillProgressResult,
            completedSkillId);

        string feedbackMessage = completionData.completedEarly
            ? $"Focus completed early: +{focusReward} Coins, +{focusXpReward} XP"
            : $"Focus complete: +{focusReward} Coins, +{focusXpReward} XP";

        if (skillProgressResult != null && skillProgressResult.becameGolden)
        {
            feedbackMessage += " | Skill Golden";
        }

        if (lowEnergyPenaltyApplied)
        {
            feedbackMessage += " | Low energy penalty";
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

    private void AddXp(int amount, bool saveAfter = true)
    {
        if (progressionSystem == null || progressionData == null)
        {
            return;
        }

        int oldLevel = progressionData.level;
        progressionSystem.AddXp(amount);
        if (progressionData.level > oldLevel)
        {
            callbacks.ShowFeedback?.Invoke($"Level {progressionData.level} reached");
        }

        callbacks.OnProgressionChanged?.Invoke();
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
        float energyBefore,
        float energyAfter,
        FocusRewardData rewardData,
        bool lowEnergyPenaltyApplied,
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
            previousPercent = skillProgressResult != null ? skillProgressResult.previousPercent : 0f,
            newPercent = skillProgressResult != null ? skillProgressResult.newPercent : 0f,
            deltaProgress = skillProgressResult != null ? skillProgressResult.deltaApplied : 0f,
            coinsReward = coinsReward,
            xpReward = xpReward,
            energyReward = rewardData != null ? rewardData.energy : 0f,
            moodReward = rewardData != null ? rewardData.mood : 0f,
            energyBefore = energyBefore,
            energyAfter = energyAfter,
            petReaction = rewardData != null && (rewardData.energy > 0f || rewardData.mood > 0f) ? "Happy" : "Steady",
            lowEnergyPenaltyApplied = lowEnergyPenaltyApplied,
            becameGolden = skillProgressResult != null && skillProgressResult.becameGolden,
            isGolden = skillProgressResult != null && skillProgressResult.isGolden
        };
    }
}
