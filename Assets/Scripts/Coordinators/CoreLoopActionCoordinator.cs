using System;
using UnityEngine;

public sealed class CoreLoopActionCoordinatorCallbacks
{
    public Action OnCoinsChanged;
    public Action OnPetChanged;
    public Action OnInventoryChanged;
    public Action ShowMissionRefresh;
    public Action RefreshOnboardingCompletion;
    public Action UpdateOnboardingUi;
    public Action UpdateUi;
    public Action SaveGame;
    public Action<int, bool> AddXp;
    public Action<string> ShowFeedback;
    public Action<RoomData> ApplyRoomVisuals;
    public Action<MissionClaimResult, bool> ApplyMissionRewards;
}

public sealed class CoreLoopActionCoordinator
{
    private readonly PetData petData;
    private readonly CurrencyData currencyData;
    private readonly ProgressionData progressionData;
    private readonly RoomData roomData;
    private readonly OnboardingData onboardingData;
    private readonly PetSystem petSystem;
    private readonly CurrencySystem currencySystem;
    private readonly InventorySystem inventorySystem;
    private readonly ShopSystem shopSystem;
    private readonly ProgressionSystem progressionSystem;
    private readonly MissionSystem missionSystem;
    private readonly BalanceConfig balanceConfig;
    private readonly CoreLoopActionCoordinatorCallbacks callbacks;
    private readonly int maxSupportedRoomLevel;

    public CoreLoopActionCoordinator(
        PetData petData,
        CurrencyData currencyData,
        ProgressionData progressionData,
        RoomData roomData,
        OnboardingData onboardingData,
        PetSystem petSystem,
        CurrencySystem currencySystem,
        InventorySystem inventorySystem,
        ShopSystem shopSystem,
        ProgressionSystem progressionSystem,
        MissionSystem missionSystem,
        BalanceConfig balanceConfig,
        int maxSupportedRoomLevel,
        CoreLoopActionCoordinatorCallbacks callbacks)
    {
        this.petData = petData;
        this.currencyData = currencyData;
        this.progressionData = progressionData;
        this.roomData = roomData;
        this.onboardingData = onboardingData;
        this.petSystem = petSystem;
        this.currencySystem = currencySystem;
        this.inventorySystem = inventorySystem;
        this.shopSystem = shopSystem;
        this.progressionSystem = progressionSystem;
        this.missionSystem = missionSystem;
        this.balanceConfig = balanceConfig;
        this.maxSupportedRoomLevel = maxSupportedRoomLevel;
        this.callbacks = callbacks ?? new CoreLoopActionCoordinatorCallbacks();
    }

    public void TryFeedItem(string itemId, float amount)
    {
        if (petData == null || petData.isDead)
        {
            callbacks.ShowFeedback?.Invoke("Pet is dead");
            return;
        }

        string itemName = FormatItemName(itemId);
        if (inventorySystem != null && inventorySystem.ConsumeItem(itemId, 1))
        {
            petSystem?.Feed(amount);
            petSystem?.AddMood(balanceConfig.feedMoodBonus);
            missionSystem?.RecordFeedAction();
            missionSystem?.IncrementGenericMissionProgress("generic_feed", 1);
            callbacks.ApplyMissionRewards?.Invoke(missionSystem?.CollectCompletedRoutineRewards(), false);
            callbacks.OnInventoryChanged?.Invoke();
            callbacks.OnPetChanged?.Invoke();
            callbacks.AddXp?.Invoke(balanceConfig.feedXpGain, true);

            if (onboardingData != null)
            {
                onboardingData.didFeed = true;
                callbacks.RefreshOnboardingCompletion?.Invoke();
                callbacks.UpdateOnboardingUi?.Invoke();
            }

            callbacks.ShowFeedback?.Invoke($"Fed {itemName}");
            callbacks.SaveGame?.Invoke();
        }
        else
        {
            callbacks.ShowFeedback?.Invoke($"No {itemName}");
        }

        callbacks.UpdateUi?.Invoke();
        callbacks.ShowMissionRefresh?.Invoke();
    }

    public void TryBuyItem(string itemId, int price)
    {
        if (petData == null || petData.isDead)
        {
            callbacks.ShowFeedback?.Invoke("Pet is dead");
            return;
        }

        if (progressionSystem == null || !progressionSystem.IsBuyUnlocked())
        {
            callbacks.ShowFeedback?.Invoke($"Unlocks at level {balanceConfig.buyUnlockLevel}");
            return;
        }

        string itemName = FormatItemName(itemId);
        if (shopSystem != null && shopSystem.BuyItem(itemId, price, 1))
        {
            callbacks.OnCoinsChanged?.Invoke();
            callbacks.OnInventoryChanged?.Invoke();
            callbacks.AddXp?.Invoke(balanceConfig.buyXpGain, true);

            if (onboardingData != null)
            {
                onboardingData.didBuyFood = true;
                callbacks.RefreshOnboardingCompletion?.Invoke();
                callbacks.UpdateOnboardingUi?.Invoke();
            }

            callbacks.ShowFeedback?.Invoke($"Bought {itemName}");
            callbacks.SaveGame?.Invoke();
        }
        else
        {
            callbacks.ShowFeedback?.Invoke("Not enough coins");
        }

        callbacks.UpdateUi?.Invoke();
    }

    public void TryWork()
    {
        if (petData == null)
        {
            return;
        }

        int workReward = GetCurrentWorkReward();
        currencySystem?.AddCoins(workReward);
        missionSystem?.RecordWorkAction();
        missionSystem?.IncrementGenericMissionProgress("generic_work", 1);
        callbacks.ApplyMissionRewards?.Invoke(missionSystem?.CollectCompletedRoutineRewards(), false);
        callbacks.OnCoinsChanged?.Invoke();
        callbacks.AddXp?.Invoke(balanceConfig.workXpGain, false);

        if (onboardingData != null)
        {
            onboardingData.didWork = true;
            callbacks.RefreshOnboardingCompletion?.Invoke();
            callbacks.UpdateOnboardingUi?.Invoke();
        }

        callbacks.ShowFeedback?.Invoke($"+{workReward} Coins");
        callbacks.SaveGame?.Invoke();
        callbacks.UpdateUi?.Invoke();
        callbacks.ShowMissionRefresh?.Invoke();
    }

    public void TryUpgradeRoom()
    {
        if (petData == null || petData.isDead)
        {
            callbacks.ShowFeedback?.Invoke("Pet is dead");
            return;
        }

        if (roomData != null && roomData.roomLevel >= maxSupportedRoomLevel)
        {
            callbacks.ShowFeedback?.Invoke("Room is max level");
            return;
        }

        if (progressionData != null && progressionData.level < GetCurrentRoomUnlockLevel())
        {
            callbacks.ShowFeedback?.Invoke($"Unlocks at level {GetCurrentRoomUnlockLevel()}");
            return;
        }

        int cost = GetCurrentRoomUpgradeCost();
        if (currencyData == null || currencyData.coins < cost)
        {
            callbacks.ShowFeedback?.Invoke("Not enough coins");
            return;
        }

        if (currencySystem != null && currencySystem.SpendCoins(cost))
        {
            roomData.roomLevel++;
            petSystem?.AddMood(balanceConfig.roomUpgradeMoodBonus);
            callbacks.ApplyRoomVisuals?.Invoke(roomData);
            callbacks.OnCoinsChanged?.Invoke();
            callbacks.OnPetChanged?.Invoke();
            callbacks.SaveGame?.Invoke();
            callbacks.ShowFeedback?.Invoke($"Room level {roomData.roomLevel}");
        }
    }

    public bool CanUpgradeRoom()
    {
        if (roomData == null || progressionData == null || currencyData == null)
        {
            return false;
        }

        if (roomData.roomLevel >= maxSupportedRoomLevel)
        {
            return false;
        }

        if (progressionData.level < GetCurrentRoomUnlockLevel())
        {
            return false;
        }

        return currencyData.coins >= GetCurrentRoomUpgradeCost();
    }

    public int GetCurrentWorkReward()
    {
        return progressionSystem != null ? progressionSystem.GetWorkReward(balanceConfig.baseWorkReward) : 0;
    }

    public int GetCurrentRoomUnlockLevel()
    {
        if (roomData == null)
        {
            return 999;
        }

        if (roomData.roomLevel == 0) return balanceConfig.roomUpgrade1UnlockLevel;
        if (roomData.roomLevel == 1) return balanceConfig.roomUpgrade2UnlockLevel;
        if (roomData.roomLevel == 2 && maxSupportedRoomLevel > 2) return balanceConfig.roomUpgrade3UnlockLevel;
        return 999;
    }

    private int GetCurrentRoomUpgradeCost()
    {
        if (roomData == null)
        {
            return 0;
        }

        if (roomData.roomLevel == 0) return balanceConfig.roomUpgrade1Cost;
        if (roomData.roomLevel == 1) return balanceConfig.roomUpgrade2Cost;
        if (roomData.roomLevel == 2 && maxSupportedRoomLevel > 2) return balanceConfig.roomUpgrade3Cost;
        return 0;
    }

    private string FormatItemName(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return string.Empty;
        }

        string name = itemId.Replace("food_", string.Empty);
        return name.Length > 0 ? char.ToUpper(name[0]) + name.Substring(1) : string.Empty;
    }
}
