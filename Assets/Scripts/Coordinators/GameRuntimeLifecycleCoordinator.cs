using System;
using UnityEngine;

public sealed class GameRuntimeLifecycleCallbacks
{
    public Action NotifyAllUi;
    public Action UpdateUi;
    public Action UpdateMissionUi;
    public Action UpdateOnboardingUi;
    public Action SaveGame;
    public Action OnPetChanged;
    public Action OnPetFlowChanged;
    public Action OnIdleChanged;
    public Func<string, bool> ApplyDailyResetWindowIfNeeded;
}

public sealed class GameRuntimeLifecycleCoordinator
{
    private readonly PetSystem petSystem;
    private readonly FocusSystem focusSystem;
    private readonly SkillDecaySystem skillDecaySystem;
    private readonly PetData petData;
    private readonly BalanceConfig balanceConfig;
    private readonly FocusCoordinator focusCoordinator;
    private readonly PetFlowCoordinator petFlowCoordinator;
    private readonly IdleCoordinator idleCoordinator;
    private readonly GameRuntimeLifecycleCallbacks callbacks;

    private float petUiBroadcastTimer;
    private bool hasBroadcastPetFlowState;
    private PetFlowState lastBroadcastPetFlowState = PetFlowState.Healthy;

    public GameRuntimeLifecycleCoordinator(
        PetSystem petSystem,
        FocusSystem focusSystem,
        SkillDecaySystem skillDecaySystem,
        PetData petData,
        BalanceConfig balanceConfig,
        FocusCoordinator focusCoordinator,
        PetFlowCoordinator petFlowCoordinator,
        IdleCoordinator idleCoordinator,
        GameRuntimeLifecycleCallbacks callbacks)
    {
        this.petSystem = petSystem;
        this.focusSystem = focusSystem;
        this.skillDecaySystem = skillDecaySystem;
        this.petData = petData;
        this.balanceConfig = balanceConfig;
        this.focusCoordinator = focusCoordinator;
        this.petFlowCoordinator = petFlowCoordinator;
        this.idleCoordinator = idleCoordinator;
        this.callbacks = callbacks ?? new GameRuntimeLifecycleCallbacks();
    }

    public void SyncUiFromRuntime()
    {
        callbacks.NotifyAllUi?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.UpdateMissionUi?.Invoke();
        callbacks.UpdateOnboardingUi?.Invoke();
        callbacks.OnIdleChanged?.Invoke();
        CachePetUiBroadcastState();
    }

    public void BroadcastPetChanged()
    {
        petUiBroadcastTimer = 0f;
        callbacks.OnPetChanged?.Invoke();
    }

    public void BroadcastPetFlowChanged()
    {
        CachePetUiBroadcastState();
        callbacks.OnPetFlowChanged?.Invoke();
    }

    public void BroadcastPetStateChanged()
    {
        BroadcastPetChanged();
        BroadcastPetFlowChanged();
    }

    public void Tick(float deltaTime, float unscaledDeltaTime, ref bool justReset)
    {
        if (!HasRuntimePrerequisites())
        {
            return;
        }

        if (justReset)
        {
            justReset = false;
            return;
        }

        ApplyDailyResetAndRefresh("update");

        bool petVitalsChanged = petSystem.UpdateHunger(deltaTime, balanceConfig.hungerDrainPerSecond);
        petVitalsChanged |= petSystem.UpdateMoodDecay(
            deltaTime,
            balanceConfig.lowHungerMoodThreshold,
            balanceConfig.lowEnergyMoodThreshold,
            balanceConfig.moodDecayPerSecondWhenHungry,
            balanceConfig.moodDecayPerSecondWhenTired
        );

        bool petStatusChanged = petSystem.UpdateStatus();
        if (petSystem.IsNeglected() && skillDecaySystem != null)
        {
            skillDecaySystem.ApplyNeglectDecay(deltaTime, ref petData.neglectDecayCarrySeconds);
        }

        bool shouldBroadcastPetChanged = false;
        if (petStatusChanged)
        {
            shouldBroadcastPetChanged = true;
            petUiBroadcastTimer = 0f;
        }

        if (petVitalsChanged)
        {
            petUiBroadcastTimer += deltaTime;
            if (petUiBroadcastTimer >= 0.25f)
            {
                shouldBroadcastPetChanged = true;
                petUiBroadcastTimer = 0f;
            }
        }
        else if (!petStatusChanged)
        {
            petUiBroadcastTimer = 0f;
        }

        PetFlowState currentPetFlowState = GetCurrentPetFlowState();
        bool petFlowChanged = !hasBroadcastPetFlowState || currentPetFlowState != lastBroadcastPetFlowState;
        if (petFlowChanged)
        {
            shouldBroadcastPetChanged = true;
            BroadcastPetFlowChanged();
        }

        if (shouldBroadcastPetChanged)
        {
            BroadcastPetChanged();
        }

        petFlowCoordinator?.HandleStateTransitions();
        bool focusChanged = focusCoordinator != null && focusCoordinator.Update(unscaledDeltaTime, petSystem.IsNeglected());
        IdleRuntimeUpdate idleUpdate = idleCoordinator != null
            ? idleCoordinator.Tick(TimeService.GetUtcNow())
            : new IdleRuntimeUpdate(false, false, 0);

        if (idleUpdate.StateChanged)
        {
            callbacks.OnIdleChanged?.Invoke();
        }

        if (idleUpdate.SaveRequired)
        {
            callbacks.SaveGame?.Invoke();
        }

        if (shouldBroadcastPetChanged || petFlowChanged || focusChanged || idleUpdate.StateChanged)
        {
            callbacks.UpdateUi?.Invoke();
        }
    }

    public void HandleApplicationResume(bool isResetting, bool isInitialized, string reason)
    {
        if (isResetting || !isInitialized)
        {
            return;
        }

        ApplyDailyResetAndRefresh(reason);
    }

    private bool HasRuntimePrerequisites()
    {
        return petSystem != null &&
               focusSystem != null &&
               balanceConfig != null &&
               petData != null;
    }

    private void ApplyDailyResetAndRefresh(string reason)
    {
        if (!(callbacks.ApplyDailyResetWindowIfNeeded?.Invoke(reason) ?? false))
        {
            return;
        }

        callbacks.SaveGame?.Invoke();
        callbacks.UpdateMissionUi?.Invoke();
        callbacks.UpdateUi?.Invoke();
    }

    private void CachePetUiBroadcastState()
    {
        petUiBroadcastTimer = 0f;
        if (petSystem == null || balanceConfig == null)
        {
            hasBroadcastPetFlowState = false;
            return;
        }

        lastBroadcastPetFlowState = GetCurrentPetFlowState();
        hasBroadcastPetFlowState = true;
    }

    private PetFlowState GetCurrentPetFlowState()
    {
        if (petSystem == null || balanceConfig == null)
        {
            return PetFlowState.Healthy;
        }

        PetStatusSummary summary = petSystem.GetStatusSummary(
            balanceConfig.lowHungerMoodThreshold,
            balanceConfig.lowEnergyMoodThreshold);
        return summary != null ? summary.flowState : PetFlowState.Healthy;
    }
}
