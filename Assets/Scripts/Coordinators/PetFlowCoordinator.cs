using System;

public sealed class PetFlowCoordinatorCallbacks
{
    public Action OnPetChanged;
    public Action OnPetFlowChanged;
    public Action OnFocusSessionChanged;
    public Action SaveGame;
    public Action UpdateUi;
    public Action<string> ShowFeedback;
}

public sealed class PetFlowCoordinator
{
    private readonly PetSystem petSystem;
    private readonly FocusCoordinator focusCoordinator;
    private readonly BalanceConfig balanceConfig;
    private readonly PetFlowCoordinatorCallbacks callbacks;

    private bool lastKnownNeglectedState;

    public PetFlowCoordinator(
        PetSystem petSystem,
        FocusCoordinator focusCoordinator,
        BalanceConfig balanceConfig,
        PetFlowCoordinatorCallbacks callbacks)
    {
        this.petSystem = petSystem;
        this.focusCoordinator = focusCoordinator;
        this.balanceConfig = balanceConfig;
        this.callbacks = callbacks ?? new PetFlowCoordinatorCallbacks();
    }

    public void ResetRuntimeState(bool isNeglected)
    {
        lastKnownNeglectedState = isNeglected;
    }

    public void HandleStateTransitions()
    {
        bool isNeglected = petSystem != null && petSystem.IsNeglected();
        if (isNeglected == lastKnownNeglectedState)
        {
            return;
        }

        lastKnownNeglectedState = isNeglected;
        if (isNeglected)
        {
            focusCoordinator?.CancelForForcedStop();
            callbacks.ShowFeedback?.Invoke("Pet neglected. Care first.");
        }
        else
        {
            callbacks.ShowFeedback?.Invoke("Pet recovered. Skill decay stopped.");
        }

        callbacks.OnPetChanged?.Invoke();
        callbacks.OnPetFlowChanged?.Invoke();
        callbacks.OnFocusSessionChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.SaveGame?.Invoke();
    }

    public PetStatusSummary GetStatusSummary()
    {
        if (petSystem == null || balanceConfig == null)
        {
            return new PetStatusSummary
            {
                flowState = PetFlowState.Warning,
                priorityStatus = PetPriorityStatus.None,
                headline = "Pet status unavailable",
                guidance = "Try reopening the scene.",
                needsAttention = true
            };
        }

        return petSystem.GetStatusSummary(
            balanceConfig.lowHungerMoodThreshold,
            balanceConfig.lowEnergyMoodThreshold);
    }
}
