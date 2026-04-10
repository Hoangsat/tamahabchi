using System;
using UnityEngine;

public sealed class PetFlowCoordinatorCallbacks
{
    public Action OnCoinsChanged;
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
    private readonly CurrencySystem currencySystem;
    private readonly FocusCoordinator focusCoordinator;
    private readonly PetData petData;
    private readonly CurrencyData currencyData;
    private readonly BalanceConfig balanceConfig;
    private readonly PetFlowCoordinatorCallbacks callbacks;

    private bool lastKnownPetDeadState;

    public PetFlowCoordinator(
        PetSystem petSystem,
        CurrencySystem currencySystem,
        FocusCoordinator focusCoordinator,
        PetData petData,
        CurrencyData currencyData,
        BalanceConfig balanceConfig,
        PetFlowCoordinatorCallbacks callbacks)
    {
        this.petSystem = petSystem;
        this.currencySystem = currencySystem;
        this.focusCoordinator = focusCoordinator;
        this.petData = petData;
        this.currencyData = currencyData;
        this.balanceConfig = balanceConfig;
        this.callbacks = callbacks ?? new PetFlowCoordinatorCallbacks();
        lastKnownPetDeadState = petData != null && petData.isDead;
    }

    public void ResetRuntimeState(bool isDead)
    {
        lastKnownPetDeadState = isDead;
    }

    public void HandleStateTransitions()
    {
        if (petData == null)
        {
            return;
        }

        if (petData.isDead && !lastKnownPetDeadState)
        {
            focusCoordinator?.CancelForForcedStop();
            callbacks.ShowFeedback?.Invoke("Pet died. Revive to continue.");
            callbacks.OnPetFlowChanged?.Invoke();
        }

        lastKnownPetDeadState = petData.isDead;
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

    public int GetReviveCost()
    {
        return balanceConfig != null ? Mathf.Max(0, balanceConfig.reviveCost) : 0;
    }

    public bool CanRevivePet()
    {
        if (petData == null || !petData.isDead)
        {
            return false;
        }

        return currencyData != null && currencyData.coins >= GetReviveCost();
    }

    public bool TryRevivePet()
    {
        if (petData == null || !petData.isDead)
        {
            callbacks.ShowFeedback?.Invoke("Pet is already alive");
            return false;
        }

        int reviveCost = GetReviveCost();
        if (currencyData == null || currencyData.coins < reviveCost)
        {
            callbacks.ShowFeedback?.Invoke($"Need {reviveCost} coins to revive");
            return false;
        }

        if (currencySystem == null || !currencySystem.SpendCoins(reviveCost))
        {
            callbacks.ShowFeedback?.Invoke($"Need {reviveCost} coins to revive");
            return false;
        }

        bool revived = petSystem != null && petSystem.Revive(
            balanceConfig.startingHunger,
            balanceConfig.startingMood,
            balanceConfig.startingEnergy);

        if (!revived)
        {
            currencySystem.AddCoins(reviveCost);
            callbacks.ShowFeedback?.Invoke("Revive failed");
            return false;
        }

        focusCoordinator?.ClearLastResult(false);
        lastKnownPetDeadState = false;
        callbacks.OnCoinsChanged?.Invoke();
        callbacks.OnPetChanged?.Invoke();
        callbacks.OnPetFlowChanged?.Invoke();
        callbacks.OnFocusSessionChanged?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.SaveGame?.Invoke();
        callbacks.ShowFeedback?.Invoke("Pet revived");
        return true;
    }
}
